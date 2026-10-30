using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Rendering;
using MindCare.Application.Domain;

namespace MindCare.Web.Infrastructure;

public static class HtmlExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Serializa um objeto para uso em atributos <c>data-*</c>. Retorna string comum,
    /// portanto o Razor aplica HTML-encoding automaticamente (seguro contra XSS).
    /// </summary>
    public static string ToDataJson(this IHtmlHelper _, object value) => JsonSerializer.Serialize(value, JsonOptions);

    public static string StatusClass(this PaymentStatus status) => status switch
    {
        PaymentStatus.Confirmado => "success",
        PaymentStatus.Parcial => "warning",
        _ => "danger",
    };

    public static string StatusIcon(this PaymentStatus status) => status switch
    {
        PaymentStatus.Confirmado => "bi-check-circle-fill",
        PaymentStatus.Parcial => "bi-circle-half",
        _ => "bi-hourglass-split",
    };

    public static string StatusClass(this AppointmentStatus status) => status switch
    {
        AppointmentStatus.Confirmada => "slate",
        AppointmentStatus.Realizada => "success",
        AppointmentStatus.Faltou => "danger",
        AppointmentStatus.Cancelada => "muted",
        _ => "neutral",
    };

    public static string StatusIcon(this AppointmentStatus status) => status switch
    {
        AppointmentStatus.Confirmada => "bi-check2-circle",
        AppointmentStatus.Realizada => "bi-check-all",
        AppointmentStatus.Faltou => "bi-person-x",
        AppointmentStatus.Cancelada => "bi-x-circle",
        _ => "bi-clock",
    };

    public static string ModalityIcon(this AppointmentModality modality) =>
        modality == AppointmentModality.Teleconsulta ? "bi-camera-video" : "bi-door-open";

    /// <summary>Retorna "active" quando o controller atual corresponde ao informado (menu lateral).</summary>
    public static string ActiveWhen(this IHtmlHelper html, string controller) =>
        string.Equals(html.ViewContext.RouteData.Values["controller"]?.ToString(), controller, StringComparison.OrdinalIgnoreCase)
            ? "active"
            : string.Empty;
}
