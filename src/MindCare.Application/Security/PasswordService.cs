using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using MindCare.Application.Domain;

namespace MindCare.Application.Security;

public interface IPasswordService
{
    string Hash(string password);

    /// <summary>Verifica a senha. <paramref name="needsRehash"/> indica que o hash armazenado deve ser atualizado.</summary>
    bool Verify(User user, string password, out bool needsRehash);
}

/// <summary>
/// Hash de senhas com PBKDF2 (formato do ASP.NET Core Identity).
/// Senhas legadas armazenadas em texto puro continuam aceitas e são convertidas no primeiro login.
/// </summary>
internal sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(User user, string password, out bool needsRehash)
    {
        needsRehash = false;
        if (string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(password))
        {
            return false;
        }

        if (!LooksHashed(user.PasswordHash))
        {
            var matches = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(user.PasswordHash), Encoding.UTF8.GetBytes(password));
            needsRehash = matches;
            return matches;
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;
        return result != PasswordVerificationResult.Failed;
    }

    // Hashes do Identity v3 são base64 e começam com o marcador 0x01.
    private static bool LooksHashed(string value)
    {
        Span<byte> buffer = stackalloc byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out var written) && written > 13 && buffer[0] == 0x01;
    }
}
