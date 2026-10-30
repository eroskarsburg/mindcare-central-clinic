using System.ComponentModel.DataAnnotations;
using System.Globalization;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Services;

namespace MindCare.Web.Models;

[AttributeUsage(AttributeTargets.Property)]
public sealed class CpfAttribute() : ValidationAttribute("Informe um CPF válido.")
{
    public override bool IsValid(object? value) => value is string s && Cpf.IsValid(s);
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class PhoneNumberAttribute() : ValidationAttribute("Informe o celular com DDD (ex.: 51 99999-8888).")
{
    public override bool IsValid(object? value) => value is not string s || string.IsNullOrWhiteSpace(s) || Phone.IsValid(s);
}

public static class Genders
{
    public static readonly string[] All = ["Feminino", "Masculino", "Outro"];
}

public sealed class LoginInput
{
    [Required(ErrorMessage = "Informe o usuário.")]
    [Display(Name = "Usuário")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public sealed class ClientInput
{
    [Required(ErrorMessage = "Informe o nome."), StringLength(100, ErrorMessage = "O nome deve ter até 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o CPF."), Cpf]
    public string Cpf { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecione o gênero.")]
    public string Gender { get; set; } = string.Empty;

    [Range(0, 130, ErrorMessage = "Idade inválida.")]
    public int? Age { get; set; }

    /// <summary>Termo de consentimento para tratamento de dados (LGPD) assinado.</summary>
    public bool ConsentGiven { get; set; }

    [PhoneNumber]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "E-mail inválido."), StringLength(120, ErrorMessage = "O e-mail deve ter até 120 caracteres.")]
    public string? Email { get; set; }

    public bool NotifyByWhatsApp { get; set; }
    public bool NotifyByEmail { get; set; }

    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }

    public ClientStatus Status { get; set; } = ClientStatus.Ativo;
    public int? ResponsibleProfessionalId { get; set; }

    [StringLength(100, ErrorMessage = "O nome do contato deve ter até 100 caracteres.")]
    public string? EmergencyContactName { get; set; }

    [PhoneNumber]
    public string? EmergencyContactPhone { get; set; }

    [StringLength(250, ErrorMessage = "O endereço deve ter até 250 caracteres.")]
    public string? Address { get; set; }

    [StringLength(80, ErrorMessage = "O convênio deve ter até 80 caracteres.")]
    public string? HealthInsurance { get; set; }

    [StringLength(1000, ErrorMessage = "As observações devem ter até 1000 caracteres.")]
    public string? Notes { get; set; }

    public Client ToDomain(int id = 0) => new()
    {
        Id = id,
        Name = Name,
        Cpf = Cpf,
        Gender = Gender,
        Age = Age,
        Phone = Phone,
        Email = Email,
        NotifyByWhatsApp = NotifyByWhatsApp,
        NotifyByEmail = NotifyByEmail,
        BirthDate = BirthDate,
        Status = Status,
        ResponsibleProfessionalId = ResponsibleProfessionalId,
        EmergencyContactName = EmergencyContactName,
        EmergencyContactPhone = EmergencyContactPhone,
        Address = Address,
        HealthInsurance = HealthInsurance,
        Notes = Notes,
    };
}

public sealed class ProfessionalInput
{
    [Required(ErrorMessage = "Informe o nome."), StringLength(100, ErrorMessage = "O nome deve ter até 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o CPF."), Cpf]
    public string Cpf { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecione o gênero.")]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a especialidade."), StringLength(60, ErrorMessage = "A especialidade deve ter até 60 caracteres.")]
    public string Speciality { get; set; } = string.Empty;

    [StringLength(40, ErrorMessage = "O registro deve ter até 40 caracteres.")]
    public string? RegistrationNumber { get; set; }

    [Range(0, 100, ErrorMessage = "O repasse deve ficar entre 0% e 100%.")]
    public decimal CommissionPercent { get; set; }

    public Professional ToDomain(int id = 0) => new()
    {
        Id = id,
        Name = Name,
        Cpf = Cpf,
        Gender = Gender,
        Speciality = Speciality,
        RegistrationNumber = RegistrationNumber,
        CommissionPercent = CommissionPercent,
    };
}

public sealed class AppointmentFormInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecione o paciente.")]
    public int ClientId { get; set; }

    public AppointmentModality Modality { get; set; }

    [Required(ErrorMessage = "Informe a data e o horário.")]
    public DateTime? ScheduledAt { get; set; }

    [StringLength(500, ErrorMessage = "A observação deve ter até 500 caracteres.")]
    public string? Observation { get; set; }

    [Range(0, 100_000, ErrorMessage = "Valor da consulta inválido.")]
    public decimal Price { get; set; }

    [Range(0, 100_000, ErrorMessage = "Valor pago inválido.")]
    public decimal? PaidPrice { get; set; }

    public int? ProfessionalId { get; set; }

    [Range(10, Appointment.MaxDurationMinutes, ErrorMessage = "Duração inválida.")]
    public int DurationMinutes { get; set; } = Appointment.DefaultDurationMinutes;

    public RecurrenceFrequency Recurrence { get; set; } = RecurrenceFrequency.Nenhuma;

    [Range(1, 52, ErrorMessage = "A série deve ter de 1 a 52 sessões.")]
    public int Occurrences { get; set; } = 1;

    /// <summary>Somente na edição.</summary>
    public AppointmentStatus? Status { get; set; }

    [StringLength(200, ErrorMessage = "O motivo deve ter até 200 caracteres.")]
    public string? CancellationReason { get; set; }

    /// <summary>Somente na edição de consultas em série.</summary>
    public SeriesScope Scope { get; set; } = SeriesScope.ApenasEsta;

    public int? ServiceTypeId { get; set; }
    public bool UsePackage { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Pix;

    public AppointmentInput ToInput() =>
        new(ClientId, Modality, ScheduledAt!.Value, Observation, Price, PaidPrice ?? 0)
        {
            ProfessionalId = ProfessionalId,
            DurationMinutes = DurationMinutes,
            Recurrence = Recurrence,
            Occurrences = Occurrences,
            Status = Status,
            CancellationReason = CancellationReason,
            ServiceTypeId = ServiceTypeId,
            UsePackage = UsePackage,
            PaymentMethod = PaymentMethod,
        };
}

public sealed class RescheduleInput
{
    [Required(ErrorMessage = "Informe o novo horário.")]
    public DateTime? Start { get; set; }

    [Range(10, Appointment.MaxDurationMinutes, ErrorMessage = "Duração inválida.")]
    public int? DurationMinutes { get; set; }
}

public sealed class StatusInput
{
    public AppointmentStatus Status { get; set; }

    [StringLength(200, ErrorMessage = "O motivo deve ter até 200 caracteres.")]
    public string? Reason { get; set; }

    public SeriesScope Scope { get; set; } = SeriesScope.ApenasEsta;
}

public sealed class AvailabilitySlotInput
{
    public DayOfWeek DayOfWeek { get; set; }

    [Required, RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Horário inválido.")]
    public string Start { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Horário inválido.")]
    public string End { get; set; } = string.Empty;
}

public sealed class AvailabilityInput
{
    public List<AvailabilitySlotInput> Slots { get; set; } = [];

    public IReadOnlyList<AvailabilitySlot> ToSlots() =>
        [.. Slots.Select(s => new AvailabilitySlot(s.DayOfWeek, TimeOnly.Parse(s.Start, CultureInfo.InvariantCulture), TimeOnly.Parse(s.End, CultureInfo.InvariantCulture)))];
}

public sealed class BlockFormInput
{
    public int? ProfessionalId { get; set; }

    /// <summary>Quando verdadeiro, bloqueia a clínica inteira (feriado).</summary>
    public bool WholeClinic { get; set; }

    [Required(ErrorMessage = "Informe o início.")]
    public DateTime? StartsAt { get; set; }

    [Required(ErrorMessage = "Informe o fim.")]
    public DateTime? EndsAt { get; set; }

    [Required(ErrorMessage = "Informe o motivo."), StringLength(120, ErrorMessage = "O motivo deve ter até 120 caracteres.")]
    public string Reason { get; set; } = string.Empty;

    public BlockInput ToInput() =>
        new(WholeClinic ? null : ProfessionalId, StartsAt!.Value, EndsAt!.Value, Reason);
}

public sealed class PaymentInput
{
    [Range(0, 100_000, ErrorMessage = "Valor pago inválido.")]
    public decimal PaidPrice { get; set; }

    public DateTime? PaidDate { get; set; }
}

public sealed class UserCreateInput
{
    [Required(ErrorMessage = "Informe o usuário."), StringLength(50, MinimumLength = 3, ErrorMessage = "O usuário deve ter entre 3 e 50 caracteres.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha."), StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    public string Password { get; set; } = string.Empty;

    public AccessLevel AccessLevel { get; set; } = AccessLevel.Profissional;
}

public sealed class UserUpdateInput
{
    [Required(ErrorMessage = "Informe o usuário."), StringLength(50, MinimumLength = 3, ErrorMessage = "O usuário deve ter entre 3 e 50 caracteres.")]
    public string Username { get; set; } = string.Empty;

    /// <summary>Opcional: em branco mantém a senha atual.</summary>
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    public string? Password { get; set; }

    public AccessLevel AccessLevel { get; set; } = AccessLevel.Profissional;
}

public sealed class ChangePasswordInput
{
    [Required(ErrorMessage = "Informe a senha atual.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha."), StringLength(100, MinimumLength = 8, ErrorMessage = "A nova senha deve ter pelo menos 8 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;

    [Compare(nameof(NewPassword), ErrorMessage = "As senhas não coincidem.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class NoteInput
{
    [Range(1, int.MaxValue)]
    public int ClientId { get; set; }

    public int? AppointmentId { get; set; }

    [Required(ErrorMessage = "Escreva a evolução."), StringLength(20_000, ErrorMessage = "A evolução deve ter até 20.000 caracteres.")]
    public string Content { get; set; } = string.Empty;
}

public sealed class DocumentInput
{
    [Range(1, int.MaxValue)]
    public int ClientId { get; set; }

    public DocumentCategory Category { get; set; } = DocumentCategory.Administrativo;

    [StringLength(200, ErrorMessage = "A descrição deve ter até 200 caracteres.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Selecione um arquivo.")]
    public IFormFile? File { get; set; }
}

public sealed class EntryFormInput
{
    [Range(0.01, 100_000, ErrorMessage = "Informe um valor maior que zero.")]
    public decimal Amount { get; set; }

    public DateTime? PaidAt { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Pix;

    [StringLength(200, ErrorMessage = "A observação deve ter até 200 caracteres.")]
    public string? Note { get; set; }

    public EntryInput ToInput() => new(Amount, PaidAt, Method, Note);
}

public sealed class PackageFormInput
{
    [Range(1, int.MaxValue)]
    public int ClientId { get; set; }

    [Required(ErrorMessage = "Informe o nome do pacote."), StringLength(80, ErrorMessage = "O nome deve ter até 80 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "O pacote deve ter de 1 a 100 sessões.")]
    public int TotalSessions { get; set; } = 4;

    [Range(0, 100_000, ErrorMessage = "Valor inválido.")]
    public decimal Price { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public PackageInput ToInput() => new(ClientId, Name, TotalSessions, Price, ExpiresAt);
}

public sealed class ServiceTypeInput
{
    [Required(ErrorMessage = "Informe o nome."), StringLength(80, ErrorMessage = "O nome deve ter até 80 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Range(10, Appointment.MaxDurationMinutes, ErrorMessage = "Duração inválida.")]
    public int DurationMinutes { get; set; } = Appointment.DefaultDurationMinutes;

    [Range(0, 100_000, ErrorMessage = "Valor inválido.")]
    public decimal Price { get; set; }

    public bool Active { get; set; } = true;

    public ServiceType ToDomain(int id = 0) =>
        new() { Id = id, Name = Name, DurationMinutes = DurationMinutes, Price = Price, Active = Active };
}
