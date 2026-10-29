// MindCare — comportamento da interface (sem dependência de jQuery).
'use strict';

const FLASH_KEY = 'mindcare:flash';
const THEME_KEY = 'mindcare:theme';

/* ------------------------------------------------------------------ */
/* Tema claro/escuro                                                   */
/* ------------------------------------------------------------------ */

const Theme = {
    current() {
        return document.documentElement.getAttribute('data-bs-theme') || 'light';
    },
    set(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        try { localStorage.setItem(THEME_KEY, theme); } catch { /* armazenamento indisponível */ }
        document.querySelectorAll('[data-theme-icon]').forEach(icon => {
            icon.className = theme === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
        });
    },
    toggle() {
        Theme.set(Theme.current() === 'dark' ? 'light' : 'dark');
    },
};

/* ------------------------------------------------------------------ */
/* Notificações                                                        */
/* ------------------------------------------------------------------ */

const Toast = {
    show(message, type = 'success', { sticky = false } = {}) {
        const container = document.getElementById('toasts');
        if (!container) return;

        const icons = { success: 'bi-check-circle-fill text-success', danger: 'bi-exclamation-octagon-fill text-danger', info: 'bi-info-circle-fill text-primary' };
        const el = document.createElement('div');
        el.className = 'toast align-items-center';
        el.setAttribute('role', type === 'danger' ? 'alert' : 'status');
        el.innerHTML = `
            <div class="d-flex align-items-start gap-2 p-3">
                <i class="bi ${icons[type] ?? icons.info} toast-icon"></i>
                <div class="flex-grow-1 small fw-medium" data-message></div>
                <button type="button" class="btn-close btn-close-sm ms-2" data-bs-dismiss="toast" aria-label="Fechar"></button>
            </div>`;
        el.querySelector('[data-message]').textContent = message; // textContent: nunca interpreta HTML
        container.appendChild(el);

        const toast = new bootstrap.Toast(el, { autohide: !sticky, delay: 4500 });
        el.addEventListener('hidden.bs.toast', () => el.remove());
        toast.show();
    },

    /** Guarda a mensagem para exibir após recarregar a página. */
    flash(message, options = {}) {
        try { sessionStorage.setItem(FLASH_KEY, JSON.stringify({ message, ...options })); } catch { /* ignore */ }
    },

    consumeFlash() {
        try {
            const raw = sessionStorage.getItem(FLASH_KEY);
            if (!raw) return;
            sessionStorage.removeItem(FLASH_KEY);
            const { message, type, sticky } = JSON.parse(raw);
            Toast.show(message, type ?? 'success', { sticky });
        } catch { /* ignore */ }
    },
};

/* ------------------------------------------------------------------ */
/* Requisições                                                         */
/* ------------------------------------------------------------------ */

class ApiError extends Error {
    constructor(message, errors) {
        super(message);
        this.errors = errors ?? {};
    }
}

async function api(method, url, body) {
    const token = document.querySelector('meta[name="csrf-token"]')?.content;
    let response;
    try {
        response = await fetch(url, {
            method,
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'X-CSRF-TOKEN': token ?? '',
            },
            body: body === undefined ? undefined : JSON.stringify(body),
            credentials: 'same-origin',
        });
    } catch {
        throw new ApiError('Não foi possível conectar ao servidor. Verifique sua conexão.');
    }

    if (response.status === 401) {
        window.location.href = '/Login';
        throw new ApiError('Sessão expirada.');
    }

    const payload = await response.json().catch(() => ({}));
    if (!response.ok) {
        const fallback = response.status === 403 ? 'Você não tem permissão para esta ação.' : 'Não foi possível concluir a operação.';
        throw new ApiError(payload.message ?? fallback, payload.errors);
    }
    return payload;
}

/* ------------------------------------------------------------------ */
/* Formulários                                                         */
/* ------------------------------------------------------------------ */

const Forms = {
    serialize(form) {
        const data = {};
        for (const el of form.elements) {
            if (!el.name || el.disabled) continue;
            if (el.type === 'radio') {
                if (el.checked) data[el.name] = el.value;
                continue;
            }
            if (el.type === 'checkbox') {
                data[el.name] = el.checked;
                continue;
            }
            const value = el.dataset.mask === 'cpf' ? el.value.replace(/\D/g, '') : el.value.trim();
            data[el.name] = value === '' ? null : value;
        }
        return data;
    },

    fill(form, item) {
        for (const el of form.elements) {
            if (!el.name || !(el.name in item)) continue;
            const value = item[el.name] ?? '';
            if (el.type === 'radio') {
                el.checked = String(value) === el.value;
            } else if (el.type === 'checkbox') {
                el.checked = Boolean(value);
            } else if (el.type === 'date') {
                el.value = String(value).slice(0, 10);
            } else if (el.type === 'datetime-local') {
                el.value = String(value).slice(0, 16);
            } else {
                el.value = value;
                if (el.dataset.mask === 'cpf') Masks.cpf(el);
            }
        }
    },

    reset(form) {
        form.reset();
        Forms.clearErrors(form);
        form.querySelectorAll('input[type="radio"][data-default]').forEach(r => { r.checked = true; });
    },

    clearErrors(form) {
        form.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));
        form.querySelectorAll('.invalid-feedback[data-generated]').forEach(el => el.remove());
    },

    showErrors(form, errors) {
        for (const [name, messages] of Object.entries(errors)) {
            const field = form.querySelector(`[name="${CSS.escape(name)}"]`);
            if (!field) continue;
            field.classList.add('is-invalid');
            const feedback = document.createElement('div');
            feedback.className = 'invalid-feedback';
            feedback.dataset.generated = '';
            feedback.textContent = messages[0];
            (field.closest('.input-group') ?? field).insertAdjacentElement('afterend', feedback);
        }
    },

    async submit(form, button, request) {
        Forms.clearErrors(form);
        if (!form.checkValidity()) {
            form.reportValidity();
            return null;
        }

        const label = button?.innerHTML;
        if (button) {
            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Salvando…';
        }

        try {
            return await request();
        } catch (error) {
            Forms.showErrors(form, error.errors ?? {});
            Toast.show(error.message, 'danger');
            return null;
        } finally {
            if (button) {
                button.disabled = false;
                button.innerHTML = label;
            }
        }
    },
};

const Masks = {
    cpf(input) {
        const d = input.value.replace(/\D/g, '').slice(0, 11);
        input.value = d
            .replace(/^(\d{3})(\d)/, '$1.$2')
            .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
            .replace(/\.(\d{3})(\d)/, '.$1-$2');
    },
};

/* ------------------------------------------------------------------ */
/* Diálogo de confirmação                                              */
/* ------------------------------------------------------------------ */

function confirmDialog(message, { confirmText = 'Excluir', tone = 'danger' } = {}) {
    const el = document.getElementById('confirmModal');
    const modal = bootstrap.Modal.getOrCreateInstance(el);
    el.querySelector('[data-confirm-message]').textContent = message;
    const ok = el.querySelector('[data-confirm-ok]');
    ok.textContent = confirmText;
    ok.className = `btn btn-${tone}`;

    return new Promise(resolve => {
        let confirmed = false;
        const onOk = () => { confirmed = true; modal.hide(); };
        ok.addEventListener('click', onOk, { once: true });
        el.addEventListener('hidden.bs.modal', () => {
            ok.removeEventListener('click', onOk);
            resolve(confirmed);
        }, { once: true });
        modal.show();
    });
}

/* ------------------------------------------------------------------ */
/* CRUD genérico                                                       */
/*   <section data-crud data-endpoint="/Client" data-modal="#clientModal">   */
/*     <button data-crud-create>  <button data-crud-edit data-item="{json}"> */
/*     <button data-crud-delete data-id="1" data-name="Ana">                 */
/* ------------------------------------------------------------------ */

function initCrud(section) {
    const endpoint = section.dataset.endpoint;
    const updateAction = section.dataset.updateAction ?? 'Update';
    const modalEl = document.querySelector(section.dataset.modal);
    const form = modalEl?.querySelector('form');
    const title = modalEl?.querySelector('.modal-title');
    const modal = modalEl ? bootstrap.Modal.getOrCreateInstance(modalEl) : null;
    let editingId = null;

    const setMode = (editing) => {
        if (!form) return;
        title.textContent = editing ? title.dataset.titleEdit : title.dataset.titleCreate;
        form.querySelectorAll('[data-create-only]').forEach(el => el.classList.toggle('d-none', editing));
        form.querySelectorAll('[data-edit-only]').forEach(el => el.classList.toggle('d-none', !editing));
        form.querySelectorAll('[data-required-on-create]').forEach(el => { el.required = !editing; });
        form.dispatchEvent(new CustomEvent('crud:mode', { detail: { editing } }));
    };

    section.addEventListener('click', async (event) => {
        const create = event.target.closest('[data-crud-create]');
        const edit = event.target.closest('[data-crud-edit]');
        const remove = event.target.closest('[data-crud-delete]');

        if (create) {
            editingId = null;
            Forms.reset(form);
            setMode(false);
            modal.show();
        } else if (edit) {
            const item = JSON.parse(edit.dataset.item);
            editingId = item.id;
            Forms.reset(form);
            Forms.fill(form, item);
            setMode(true);
            form.dispatchEvent(new CustomEvent('crud:filled', { detail: item }));
            modal.show();
        } else if (remove) {
            const ok = await confirmDialog(`Tem certeza que deseja excluir ${remove.dataset.name}? Esta ação não pode ser desfeita.`);
            if (!ok) return;
            try {
                const result = await api('DELETE', `${endpoint}/Delete/${remove.dataset.id}`);
                Toast.flash(result.message);
                window.location.reload();
            } catch (error) {
                Toast.show(error.message, 'danger');
            }
        }
    });

    form?.addEventListener('submit', async (event) => {
        event.preventDefault();
        const button = form.querySelector('[type="submit"]') ?? modalEl.querySelector('[type="submit"]');
        const body = Forms.serialize(form);
        const result = await Forms.submit(form, button, () => editingId === null
            ? api('POST', `${endpoint}/Create`, body)
            : api('PUT', `${endpoint}/${updateAction}/${editingId}`, body));

        if (result) {
            modal.hide();
            Toast.flash(result.message, { sticky: Boolean(result.data) });
            window.location.reload();
        }
    });
}

/* Formulários avulsos: <form data-api-form data-method="PUT" action="/Profile/Password"> */
function initApiForm(form) {
    form.addEventListener('submit', async (event) => {
        event.preventDefault();
        const button = form.querySelector('[type="submit"]');
        const result = await Forms.submit(form, button, () =>
            api(form.dataset.method ?? 'POST', form.getAttribute('action'), Forms.serialize(form)));

        if (!result) return;
        if (form.hasAttribute('data-reload')) {
            Toast.flash(result.message);
            window.location.reload();
        } else {
            Toast.show(result.message);
            if (form.hasAttribute('data-reset')) form.reset();
        }
    });
}

/* Botões de ação avulsos:
   <button data-api-action="/Client/Anonymize/5" data-method="POST" data-confirm="Tem certeza?" data-confirm-text="Anonimizar"> */
function initApiActions() {
    document.addEventListener('click', async (event) => {
        const button = event.target.closest('[data-api-action]');
        if (!button) return;
        event.preventDefault();

        if (button.dataset.confirm) {
            const ok = await confirmDialog(button.dataset.confirm, {
                confirmText: button.dataset.confirmText ?? 'Confirmar',
                tone: button.dataset.tone ?? 'danger',
            });
            if (!ok) return;
        }

        try {
            const result = await api(button.dataset.method ?? 'POST', button.dataset.apiAction);
            Toast.flash(result.message);
            window.location.reload();
        } catch (error) {
            Toast.show(error.message, 'danger');
        }
    });
}

/* ------------------------------------------------------------------ */
/* Tabelas                                                             */
/* ------------------------------------------------------------------ */

function initTable(table) {
    if (typeof DataTable === 'undefined') return;
    new DataTable(table, {
        language: { url: '/lib/datatables/pt-BR.json' },
        order: [],
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        columnDefs: [{ targets: 'no-sort', orderable: false, searchable: false }],
        layout: {
            topStart: 'search',
            topEnd: 'pageLength',
            bottomStart: 'info',
            bottomEnd: 'paging',
        },
    });
}

/* ------------------------------------------------------------------ */
/* Inicialização                                                       */
/* ------------------------------------------------------------------ */

document.addEventListener('DOMContentLoaded', () => {
    Theme.set(Theme.current());
    document.querySelectorAll('[data-theme-toggle]').forEach(btn => btn.addEventListener('click', Theme.toggle));

    document.querySelectorAll('table[data-table]').forEach(initTable);
    document.querySelectorAll('[data-crud]').forEach(initCrud);
    document.querySelectorAll('form[data-api-form]').forEach(initApiForm);
    initApiActions();
    document.querySelectorAll('[data-mask="cpf"]').forEach(input => input.addEventListener('input', () => Masks.cpf(input)));

    document.querySelectorAll('[data-password-toggle]').forEach(btn => {
        btn.addEventListener('click', () => {
            const input = document.querySelector(btn.dataset.passwordToggle);
            const show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            btn.querySelector('i').className = show ? 'bi bi-eye-slash' : 'bi bi-eye';
            btn.setAttribute('aria-label', show ? 'Ocultar senha' : 'Mostrar senha');
        });
    });

    Toast.consumeFlash();
});

window.MindCare = { api, Toast, Forms, confirmDialog };
