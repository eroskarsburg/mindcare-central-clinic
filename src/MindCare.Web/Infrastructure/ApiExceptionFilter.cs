using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using MindCare.Application.Common;

namespace MindCare.Web.Infrastructure;

/// <summary>
/// Converte exceções das ações JSON (POST/PUT/DELETE) em respostas <c>{ message }</c> que a interface exibe como toast.
/// Requisições GET seguem o fluxo padrão (página de erro).
/// </summary>
internal sealed partial class ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (HttpMethods.IsGet(context.HttpContext.Request.Method))
        {
            return;
        }

        (int status, string message) = context.Exception switch
        {
            BusinessException e => (StatusCodes.Status400BadRequest, e.Message),
            DbUpdateException => (StatusCodes.Status409Conflict,
                "Não foi possível salvar: o registro conflita com dados existentes."),
            _ => (StatusCodes.Status500InternalServerError,
                "Ocorreu um erro inesperado. Tente novamente em instantes."),
        };

        if (status >= StatusCodes.Status500InternalServerError || context.Exception is DbUpdateException)
        {
            LogUnhandled(logger, context.Exception, context.HttpContext.Request.Path);
        }

        context.Result = new ObjectResult(new { message }) { StatusCode = status };
        context.ExceptionHandled = true;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Falha ao processar {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string path);
}
