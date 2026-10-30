using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Common;
using MindCare.Application.Services;
using MindCare.Web.Infrastructure;
using MindCare.Web.Models;

namespace MindCare.Web.Controllers;

public sealed class ProfileController(
    IUserService users,
    IProfessionalService professionals,
    IAuthService auth) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var user = await users.GetAsync(CurrentUserId, ct);
        if (user is null)
        {
            return RedirectToAction("Index", "Login");
        }

        return View(new ProfileViewModel(user, await professionals.GetByUserAsync(user.Id, ct)));
    }

    [HttpPut]
    public async Task<IActionResult> Password([FromBody] ChangePasswordInput input, CancellationToken ct)
    {
        await auth.ChangePasswordAsync(CurrentUserId, input.CurrentPassword, input.NewPassword, ct);

        // Renova o cookie para remover a marcação de senha temporária.
        if (await auth.GetSessionUserAsync(CurrentUserId, ct) is { } session)
        {
            await AuthCookie.SignInAsync(HttpContext, session);
        }

        return Success("Senha alterada com sucesso.");
    }

    [HttpPut]
    public async Task<IActionResult> Professional([FromBody] ProfessionalInput input, CancellationToken ct)
    {
        // O profissional só pode editar o próprio cadastro.
        var professional = await professionals.GetByUserAsync(CurrentUserId, ct)
            ?? throw new BusinessException("Seu usuário não está vinculado a um profissional.");

        await professionals.UpdateAsync(input.ToDomain(professional.Id), ct);
        return Success("Dados profissionais atualizados. O nome exibido muda no próximo login.");
    }
}
