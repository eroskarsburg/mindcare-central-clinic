namespace MindCare.Application.Common;

/// <summary>Valor monetário por extenso em português (ex.: "cento e cinquenta reais e trinta centavos").</summary>
public static class MoneyInWords
{
    private static readonly string[] Units =
        ["", "um", "dois", "três", "quatro", "cinco", "seis", "sete", "oito", "nove", "dez",
         "onze", "doze", "treze", "quatorze", "quinze", "dezesseis", "dezessete", "dezoito", "dezenove"];

    private static readonly string[] Tens = ["", "", "vinte", "trinta", "quarenta", "cinquenta", "sessenta", "setenta", "oitenta", "noventa"];

    private static readonly string[] Hundreds =
        ["", "cento", "duzentos", "trezentos", "quatrocentos", "quinhentos", "seiscentos", "setecentos", "oitocentos", "novecentos"];

    public static string Format(decimal value)
    {
        if (value is < 0 or >= 1_000_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        var reais = (long)decimal.Truncate(value);
        var centavos = (int)decimal.Round((value - reais) * 100, MidpointRounding.AwayFromZero);
        if (centavos == 100)
        {
            reais++;
            centavos = 0;
        }

        var parts = new List<string>();
        if (reais > 0)
        {
            // "um milhão de reais", "dois milhões de reais", mas "um milhão e duzentos mil reais".
            var exactMillions = reais >= 1_000_000 && reais % 1_000_000 == 0;
            parts.Add($"{Integer(reais)}{(exactMillions ? " de" : string.Empty)} {(reais == 1 ? "real" : "reais")}");
        }

        if (centavos > 0)
        {
            parts.Add($"{Integer(centavos)} {(centavos == 1 ? "centavo" : "centavos")}");
        }

        return parts.Count == 0 ? "zero real" : string.Join(" e ", parts);
    }

    /// <summary>
    /// Une os grupos (milhões, milhares, unidades) com "e" quando o grupo seguinte é menor que 100
    /// ou uma centena exata ("mil e cem", "mil e vinte"); nos demais casos, só com espaço ("mil duzentos e cinquenta").
    /// </summary>
    private static string Integer(long n)
    {
        if (n == 0)
        {
            return "zero";
        }

        var groups = new List<(long Value, string Words)>();
        var millions = n / 1_000_000;
        var thousands = n / 1000 % 1000;
        var units = n % 1000;

        if (millions > 0)
        {
            groups.Add((millions, millions == 1 ? "um milhão" : $"{UpTo999((int)millions)} milhões"));
        }

        if (thousands > 0)
        {
            groups.Add((thousands, thousands == 1 ? "mil" : $"{UpTo999((int)thousands)} mil"));
        }

        if (units > 0)
        {
            groups.Add((units, UpTo999((int)units)));
        }

        var result = groups[0].Words;
        for (var i = 1; i < groups.Count; i++)
        {
            var (value, words) = groups[i];
            result += value < 100 || value % 100 == 0 ? $" e {words}" : $" {words}";
        }

        return result;
    }

    private static string UpTo999(int n)
    {
        if (n == 100)
        {
            return "cem";
        }

        var hundreds = n / 100;
        var rest = n % 100;
        var words = new List<string>();
        if (hundreds > 0)
        {
            words.Add(Hundreds[hundreds]);
        }

        if (rest > 0)
        {
            words.Add(rest < 20 ? Units[rest] : Tens[rest / 10] + (rest % 10 > 0 ? " e " + Units[rest % 10] : string.Empty));
        }

        return string.Join(" e ", words);
    }
}
