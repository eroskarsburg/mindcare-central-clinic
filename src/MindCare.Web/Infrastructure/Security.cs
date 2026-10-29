using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MindCare.Application.Domain;
using MindCare.Application.Security;
using MindCare.Application.Services;

namespace MindCare.Web.Infrastructure;

public static class MindCareClaims
{
    public const string MustChangePassword = "mindcare:must_change_password";
}

/// <summary>Usuário da requisição atual, usado pela auditoria e pelas regras de acesso.</summary>
internal sealed class HttpCurrentActor(IHttpContextAccessor accessor) : ICurrentActor
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public int? UserId => Principal?.Identity?.IsAuthenticated == true ? Principal.GetUserId() : null;

    public string? Username => Principal?.Identity?.IsAuthenticated == true ? Principal.Identity.Name : null;

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public AccessLevel? AccessLevel =>
        Enum.TryParse<AccessLevel>(Principal?.FindFirstValue(ClaimTypes.Role), out var level) ? level : null;
}

public static class AuthCookie
{
    public static Task SignInAsync(HttpContext context, AuthenticatedUser user)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.DisplayName),
            new(ClaimTypes.Role, user.AccessLevel.ToString()),
        ];

        if (user.MustChangePassword)
        {
            claims.Add(new Claim(MindCareClaims.MustChangePassword, "true"));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    public static bool MustChangePassword(this ClaimsPrincipal user) => user.HasClaim(MindCareClaims.MustChangePassword, "true");
}

/// <summary>
/// Enquanto o usuário estiver com senha temporária, só o perfil (para trocá-la), o logout e as páginas de erro ficam acessíveis.
/// </summary>
internal sealed class RequirePasswordChangeFilter : IAsyncActionFilter
{
    private static readonly HashSet<string> AllowedControllers = new(StringComparer.OrdinalIgnoreCase) { "Profile", "Login", "Error" };

    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        var controller = context.RouteData.Values["controller"]?.ToString() ?? string.Empty;

        if (user.Identity?.IsAuthenticated == true && user.MustChangePassword() && !AllowedControllers.Contains(controller))
        {
            context.Result = HttpMethods.IsGet(context.HttpContext.Request.Method)
                ? new RedirectToActionResult("Index", "Profile", null)
                : new ObjectResult(new { message = "Troque sua senha temporária antes de continuar." })
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                };
            return Task.CompletedTask;
        }

        return next();
    }
}
