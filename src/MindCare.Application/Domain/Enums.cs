namespace MindCare.Application.Domain;

/// <summary>Nível de acesso de um usuário. Os valores correspondem à tabela <c>access_level</c>.</summary>
public enum AccessLevel
{
    Administrador = 1,
    Profissional = 2,
}

/// <summary>Modalidade de atendimento de uma consulta.</summary>
public enum AppointmentModality
{
    Presencial,
    Teleconsulta,
}

/// <summary>Situação financeira de uma consulta.</summary>
public enum PaymentStatus
{
    Pendente,
    Confirmado,
    Parcial,
}
