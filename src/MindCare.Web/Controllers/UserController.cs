using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Services;
using MindCare.Web.Infrastructure;
using MindCare.Web.Models;

namespace MindCare.Web.Controllers;

[Authorize(Policy = Policies.Admin)]
public sealed class UserController(IUserService users) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await users.ListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateInput input, CancellationToken ct)
    {
        await users.CreateAsync(input.Username, input.Password, input.AccessLevel, ct);
        return Success("Usuário criado.");
    }

    [HttpPut]
    public async Task<IActionResult> Update(int id, [FromBody] UserUpdateInput input, CancellationToken ct)
    {
        await users.UpdateAsync(id, input.Username, input.Password, input.AccessLevel, CurrentUserId, ct);
        return Success("Usuário atualizado.");
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await users.DeleteAsync(id, CurrentUserId, ct);
        return Success("Usuário excluído.");
    }
}
