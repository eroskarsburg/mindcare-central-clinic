using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MindCare.Web.Infrastructure;

/// <summary>Base dos controllers: padroniza respostas JSON e validação das ações de escrita.</summary>
public abstract class AppController : Controller
{
    protected int CurrentUserId => User.GetUserId();

    /// <summary>Resposta de sucesso consumida pelo front-end (exibida como toast).</summary>
    protected OkObjectResult Success(string message, object? data = null) => Ok(new { message, data });

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!HttpMethods.IsGet(context.HttpContext.Request.Method) && !context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => ToCamelCase(e.Key),
                    e => e.Value!.Errors.Select(x => string.IsNullOrEmpty(x.ErrorMessage) ? "Valor inválido." : x.ErrorMessage).ToArray());

            var message = errors.Values.SelectMany(x => x).FirstOrDefault() ?? "Dados inválidos.";
            context.Result = BadRequest(new { message, errors });
            return;
        }

        base.OnActionExecuting(context);
    }

    private static string ToCamelCase(string key)
    {
        var name = key.Split('.').Last().TrimStart('$');
        return name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
    }
}
