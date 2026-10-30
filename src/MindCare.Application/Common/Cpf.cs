namespace MindCare.Application.Common;

public static class Cpf
{
    /// <summary>Remove pontuação, mantendo apenas os dígitos.</summary>
    public static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : new string([.. value.Where(char.IsAsciiDigit)]);

    /// <summary>Valida tamanho e dígitos verificadores do CPF.</summary>
    public static bool IsValid(string? value)
    {
        var digits = Normalize(value);
        if (digits.Length != 11 || digits.All(c => c == digits[0]))
        {
            return false;
        }

        return CheckDigit(digits, 9) == digits[9] - '0' && CheckDigit(digits, 10) == digits[10] - '0';
    }

    /// <summary>Formata um CPF com 11 dígitos como 000.000.000-00.</summary>
    public static string Format(string? value)
    {
        var digits = Normalize(value);
        return digits.Length == 11
            ? $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}"
            : digits;
    }

    private static int CheckDigit(string digits, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += (digits[i] - '0') * (length + 1 - i);
        }

        var remainder = sum * 10 % 11;
        return remainder == 10 ? 0 : remainder;
    }
}
