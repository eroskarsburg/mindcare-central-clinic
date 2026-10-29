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

/// <summary>Ciclo de vida de uma consulta.</summary>
public enum AppointmentStatus
{
    Agendada,
    Confirmada,
    Realizada,
    Faltou,
    Cancelada,
}

/// <summary>Repetição de uma consulta ao ser criada.</summary>
public enum RecurrenceFrequency
{
    Nenhuma,
    Semanal,
    Quinzenal,
}

/// <summary>Abrangência de uma alteração em consultas de uma série.</summary>
public enum SeriesScope
{
    ApenasEsta,
    EstaEProximas,
}
