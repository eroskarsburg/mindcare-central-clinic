namespace MindCare.Application.Common;

/// <summary>
/// Violação de regra de negócio. A mensagem é segura para ser exibida ao usuário final.
/// </summary>
public sealed class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }

    public BusinessException() { }

    public BusinessException(string message, Exception innerException) : base(message, innerException) { }
}
