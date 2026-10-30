using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;
using MindCare.Application.Services;
using MindCare.Web.Infrastructure;
using MindCare.Web.Models;

namespace MindCare.Web.Controllers;

[AllowAnonymous]
public sealed class LoginController(IAuthService auth) : AppController
{
    public const string RateLimitPolicy = "login";

    [HttpGet]
    public IActionResult Index(string? returnUrl = null, bool throttled = false)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        if (throttled)
        {
            ModelState.AddModelError(string.Empty, "Muitas tentativas em pouco tempo. Aguarde um minuto e tente novamente.");
        }

        return View(new LoginInput { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitPolicy)]
    public async Task<IActionResult> Index(LoginInput input, CancellationToken ct)
    {
        // Formulário HTML tradicional: o estado inválido volta para a própria view.
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var result = await auth.AuthenticateAsync(input.Username, input.Password, ct);
        if (!result.Succeeded)
        {
            var message = result.Failure == LoginFailure.LockedOut
                ? $"Conta bloqueada temporariamente por excesso de tentativas. Tente novamente às {result.LockedUntil:HH:mm}."
                : "Usuário ou senha inválidos.";
            ModelState.AddModelError(string.Empty, message);
            return View(input);
        }

        await AuthCookie.SignInAsync(HttpContext, result.User!);

        if (result.User!.MustChangePassword)
        {
            return RedirectToAction("Index", "Profile");
        }

        return Url.IsLocalUrl(input.ReturnUrl) ? LocalRedirect(input.ReturnUrl) : RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Index));
    }

    // O AppController devolve JSON para ModelState inválido; aqui queremos re-renderizar a view.
    public override void OnActionExecuting(ActionExecutingContext context) { }
}
