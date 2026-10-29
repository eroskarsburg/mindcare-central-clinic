// Agenda em calendário (FullCalendar 6). Reaproveita o formulário de CRUD da página (app.js).
'use strict';

(() => {
    const el = document.getElementById('calendar');
    if (!el || typeof FullCalendar === 'undefined') return;

    const section = el.closest('[data-crud]');
    const filter = document.getElementById('professional-filter');
    const pad = (n) => String(n).padStart(2, '0');

    // Horário local sem fuso: o servidor trabalha com o horário da clínica.
    const toLocal = (d) =>
        `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
    const minutesBetween = (a, b) => Math.round((b - a) / 60000);

    const statusIcon = {
        Agendada: 'bi-clock',
        Confirmada: 'bi-check2-circle',
        Realizada: 'bi-check-all',
        Faltou: 'bi-person-x',
        Cancelada: 'bi-x-circle',
    };

    const calendar = new FullCalendar.Calendar(el, {
        locale: 'pt-br',
        initialView: window.matchMedia('(max-width: 767.98px)').matches ? 'listWeek' : 'timeGridWeek',
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,timeGridDay,listWeek',
        },
        buttonText: { listWeek: 'lista' },
        firstDay: 1,
        allDaySlot: false,
        nowIndicator: true,
        height: 'auto',
        slotMinTime: '07:00:00',
        slotMaxTime: '21:00:00',
        slotDuration: '00:30:00',
        snapDuration: '00:05:00',
        scrollTime: '08:00:00',
        slotLabelFormat: { hour: '2-digit', minute: '2-digit' },
        eventTimeFormat: { hour: '2-digit', minute: '2-digit' },
        selectable: true,
        selectMirror: true,
        editable: true,
        dayMaxEvents: 4,

        events: async (info, success, failure) => {
            try {
                const params = new URLSearchParams({ start: toLocal(info.start), end: toLocal(info.end) });
                if (filter?.value) params.set('professionalId', filter.value);
                const response = await fetch(`/Appointment/Events?${params}`, { headers: { Accept: 'application/json' } });
                if (!response.ok) throw new Error('Falha ao carregar a agenda.');
                success(await response.json());
            } catch (error) {
                failure(error);
                MindCare.Toast.show(error.message, 'danger');
            }
        },

        select: (info) => {
            calendar.unselect();
            if (!section?.crud) return;
            const start = new Date(info.start);
            if (info.allDay) start.setHours(9, 0, 0, 0); // clique na visão mensal
            const duration = info.allDay ? null : minutesBetween(info.start, info.end);
            section.crud.openCreate({
                scheduledAt: toLocal(start),
                ...(duration && duration > 30 ? { durationMinutes: duration } : {}),
                ...(filter?.value ? { professionalId: filter.value } : {}),
            });
        },

        eventClick: (info) => {
            info.jsEvent.preventDefault();
            const item = info.event.extendedProps.item;
            if (item && section?.crud) section.crud.openEdit(item);
        },

        eventDrop: (info) => reschedule(info),
        eventResize: (info) => reschedule(info),

        eventContent: (arg) => {
            if (arg.event.display === 'background') {
                return { html: `<span class="ev-block-label"><i class="bi bi-slash-circle me-1"></i>${escapeHtml(arg.event.title)}</span>` };
            }
            const p = arg.event.extendedProps;
            const icons = [
                `<i class="bi ${statusIcon[p.status] ?? 'bi-clock'}" title="${p.status}"></i>`,
                p.modality === 'Teleconsulta' ? '<i class="bi bi-camera-video" title="Teleconsulta"></i>' : '',
                p.isSeries ? '<i class="bi bi-arrow-repeat" title="Sessão recorrente"></i>' : '',
            ].join('');
            return {
                html: `<div class="ev-inner">
                         <div class="ev-time">${escapeHtml(arg.timeText)} <span class="ev-icons">${icons}</span></div>
                         <div class="ev-title">${escapeHtml(arg.event.title ?? '')}</div>
                         ${p.professional ? `<div class="ev-sub">${escapeHtml(p.professional)}</div>` : ''}
                       </div>`,
            };
        },
    });

    async function reschedule(info) {
        try {
            const result = await MindCare.api('PUT', `/Appointment/Reschedule/${info.event.id}`, {
                start: toLocal(info.event.start),
                durationMinutes: minutesBetween(info.event.start, info.event.end),
            });
            const warned = result.data?.warnings?.length > 0;
            MindCare.Toast.show(result.message, warned ? 'info' : 'success');
            calendar.refetchEvents();
        } catch (error) {
            info.revert();
            MindCare.Toast.show(error.message, 'danger');
        }
    }

    function escapeHtml(value) {
        const div = document.createElement('div');
        div.textContent = value ?? '';
        return div.innerHTML;
    }

    calendar.render();
    filter?.addEventListener('change', () => {
        calendar.refetchEvents();
        try { localStorage.setItem('mindcare:calendar-professional', filter.value); } catch { /* ignore */ }
    });

    // O calendário fica numa aba: recalcula o tamanho quando ela aparece.
    document.querySelectorAll('[data-bs-toggle="tab"]').forEach(tab =>
        tab.addEventListener('shown.bs.tab', () => calendar.updateSize()));

    window.MindCare.calendar = calendar;
})();
