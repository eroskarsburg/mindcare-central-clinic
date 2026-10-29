namespace MindCare.Application.Common;

public static class Phone
{
    /// <summary>
    /// Normaliza para o formato internacional usado pelo WhatsApp (somente dígitos, com DDI).
    /// Números brasileiros com DDD (10 ou 11 dígitos) recebem o 55 automaticamente;
    /// números digitados com "+" já trazem o DDI e são mantidos.
    /// </summary>
    public static string? Normalize(string? value)
    {
        var digits = Cpf.Normalize(value);
        var international = value?.TrimStart().StartsWith('+') == true;
        return digits.Length switch
        {
            0 => null,
            10 or 11 when !international => "55" + digits,
            _ => digits,
        };
    }

    public static bool IsValid(string? value) => Normalize(value) is { Length: >= 12 and <= 15 };

    /// <summary>Exibição amigável: +55 (51) 99999-8888.</summary>
    public static string Format(string? value)
    {
        var digits = Normalize(value);
        if (digits is null)
        {
            return string.Empty;
        }

        if (digits.StartsWith("55", StringComparison.Ordinal) && digits.Length is 12 or 13)
        {
            var local = digits[4..];
            var split = local.Length - 4;
            return $"+55 ({digits[2..4]}) {local[..split]}-{local[split..]}";
        }

        return "+" + digits;
    }
}

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
