using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindCare.Web.Models;

namespace MindCare.Web.Controllers;

[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class ErrorController : Controller
{
    [Route("/Error/{statusCode:int?}")]
    [IgnoreAntiforgeryToken]
    public IActionResult Index(int? statusCode)
    {
        var code = statusCode ?? StatusCodes.Status500InternalServerError;
        var (title, message) = code switch
        {
            StatusCodes.Status404NotFound => ("Página não encontrada", "O endereço que você procurou não existe ou foi movido."),
            StatusCodes.Status403Forbidden => ("Acesso restrito", "Seu perfil não tem permissão para acessar esta área."),
            _ => ("Algo deu errado", "Não conseguimos concluir sua solicitação. Tente novamente em instantes."),
        };

        Response.StatusCode = code;
        return View(new ErrorViewModel(code, title, message, Activity.Current?.Id ?? HttpContext.TraceIdentifier));
    }
}
