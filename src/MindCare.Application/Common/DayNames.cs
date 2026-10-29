namespace MindCare.Application.Common;

public static class DayNames
{
    /// <summary>Nome do dia da semana em português (ex.: "segunda-feira").</summary>
    public static string Of(DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => "domingo",
        DayOfWeek.Monday => "segunda-feira",
        DayOfWeek.Tuesday => "terça-feira",
        DayOfWeek.Wednesday => "quarta-feira",
        DayOfWeek.Thursday => "quinta-feira",
        DayOfWeek.Friday => "sexta-feira",
        _ => "sábado",
    };
}
