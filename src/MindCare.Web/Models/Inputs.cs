using System.ComponentModel.DataAnnotations;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Services;

namespace MindCare.Web.Models;

[AttributeUsage(AttributeTargets.Property)]
public sealed class CpfAttribute() : ValidationAttribute("Informe um CPF válido.")
{
    public override bool IsValid(object? value) => value is string s && Cpf.IsValid(s);
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

    public Client ToDomain(int id = 0) => new() { Id = id, Name = Name, Cpf = Cpf, Gender = Gender, Age = Age };
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

    public Professional ToDomain(int id = 0) =>
        new() { Id = id, Name = Name, Cpf = Cpf, Gender = Gender, Speciality = Speciality };
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

    public AppointmentInput ToInput() =>
        new(ClientId, Modality, ScheduledAt!.Value, Observation, Price, PaidPrice ?? 0);
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
