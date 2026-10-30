using MindCare.Application.Domain;
using MindCare.Application.Security;
using MindCare.Application.Services;

namespace MindCare.Application.Tests;

public class PasswordServiceTests
{
    private readonly PasswordService _passwords = new();

    [Fact]
    public void Hash_nao_armazena_texto_puro_e_verifica_corretamente()
    {
        var user = new User { PasswordHash = _passwords.Hash("segredo-forte") };

        Assert.NotEqual("segredo-forte", user.PasswordHash);
        Assert.True(_passwords.Verify(user, "segredo-forte", out var rehash));
        Assert.False(rehash);
        Assert.False(_passwords.Verify(user, "outra-senha", out _));
    }

    [Fact]
    public void Senha_legada_em_texto_puro_e_aceita_e_marcada_para_rehash()
    {
        var user = new User { PasswordHash = "123" };

        Assert.True(_passwords.Verify(user, "123", out var rehash));
        Assert.True(rehash);
        Assert.False(_passwords.Verify(user, "1234", out _));
    }
}

public class AuthServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly PasswordService _passwords = new();
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Login_com_senha_legada_converte_para_hash()
    {
        using (var db = _database.CreateContext())
        {
            db.Users.Add(new User { Username = "admin", PasswordHash = "123", AccessLevel = AccessLevel.Administrador });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using (var db = _database.CreateContext())
        {
            var result = await new AuthService(db, _passwords, _clock).AuthenticateAsync("admin", "123", TestContext.Current.CancellationToken);
            Assert.NotNull(result);
            Assert.Equal(AccessLevel.Administrador, result.AccessLevel);
        }

        using (var db = _database.CreateContext())
        {
            var stored = db.Users.Single();
            Assert.NotEqual("123", stored.PasswordHash);
            Assert.Equal(_clock.GetLocalNow().DateTime, stored.LastActivity);
        }
    }

    [Fact]
    public async Task Login_com_senha_errada_retorna_nulo()
    {
        using var db = _database.CreateContext();
        db.Users.Add(new User { Username = "ana", PasswordHash = _passwords.Hash("correta-123") });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new AuthService(db, _passwords, _clock).AuthenticateAsync("ana", "errada", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}
