using System.Security.Cryptography;

namespace MindCare.Application.Security;

/// <summary>Gera senhas temporárias aleatórias, sem caracteres ambíguos (0/O, 1/l/I).</summary>
public static class PasswordGenerator
{
    private const string Lower = "abcdefghjkmnpqrstuvwxyz";
    private const string Upper = "ABCDEFGHJKMNPQRSTUVWXYZ";
    private const string Digits = "23456789";
    private const string Symbols = "@#$%&*!?";

    public static string Generate(int length = 12)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 8);

        // Garante ao menos um caractere de cada grupo.
        var chars = new List<char>
        {
            Pick(Lower), Pick(Upper), Pick(Digits), Pick(Symbols),
        };
        const string all = Lower + Upper + Digits + Symbols;
        while (chars.Count < length)
        {
            chars.Add(Pick(all));
        }

        // Embaralha (Fisher–Yates) com o gerador criptográfico.
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string([.. chars]);
    }

    private static char Pick(string source) => source[RandomNumberGenerator.GetInt32(source.Length)];
}
