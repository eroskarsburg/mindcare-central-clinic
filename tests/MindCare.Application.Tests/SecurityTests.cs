using Microsoft.Extensions.Options;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Security;
using MindCare.Application.Services;

namespace MindCare.Application.Tests;

public class PasswordGeneratorTests
{
    [Fact]
    public void Gera_senhas_fortes_e_diferentes()
    {
        var first = PasswordGenerator.Generate();
        var second = PasswordGenerator.Generate();

        Assert.Equal(12, first.Length);
        Assert.NotEqual(first, second);
        Assert.Contains(first, char.IsUpper);
        Assert.Contains(first, char.IsLower);
        Assert.Contains(first, char.IsDigit);
        Assert.Contains(first, c => !char.IsLetterOrDigit(c));
        Assert.DoesNotContain(first, c => "0O1lI".Contains(c, StringComparison.Ordinal));
    }
}

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
            var result = await Auth(db).AuthenticateAsync("admin", "123", TestContext.Current.CancellationToken);
            Assert.True(result.Succeeded);
            Assert.Equal(AccessLevel.Administrador, result.User!.AccessLevel);
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

        var result = await Auth(db).AuthenticateAsync("ana", "errada", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(LoginFailure.InvalidCredentials, result.Failure);
    }

    [Fact]
    public async Task Conta_e_bloqueada_apos_tentativas_e_liberada_depois_do_prazo()
    {
        var ct = TestContext.Current.CancellationToken;
        using var db = _database.CreateContext();
        db.Users.Add(new User { Username = "ana", PasswordHash = _passwords.Hash("correta-123") });
        await db.SaveChangesAsync(ct);

        var auth = Auth(db);
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(LoginFailure.InvalidCredentials, (await auth.AuthenticateAsync("ana", "errada", ct)).Failure);
        }

        var locked = await auth.AuthenticateAsync("ana", "errada", ct);
        Assert.Equal(LoginFailure.LockedOut, locked.Failure);
        Assert.Equal(_clock.GetLocalNow().DateTime.AddMinutes(15), locked.LockedUntil);

        // Mesmo com a senha certa, continua bloqueada até o prazo.
        Assert.Equal(LoginFailure.LockedOut, (await auth.AuthenticateAsync("ana", "correta-123", ct)).Failure);

        var later = new FixedClock(_clock.GetUtcNow().AddMinutes(16));
        var afterLockout = new AuthService(db, _passwords, Options.Create(new SecurityOptions()), new SystemActor(), later);
        Assert.True((await afterLockout.AuthenticateAsync("ana", "correta-123", ct)).Succeeded);

        Assert.Contains(db.AuditLogs, a => a.Action == AuditActions.LockedOut && a.Username == "ana");
    }

    [Fact]
    public async Task Troca_de_senha_remove_exigencia_e_rejeita_senha_igual()
    {
        var ct = TestContext.Current.CancellationToken;
        using var db = _database.CreateContext();
        var user = new User { Username = "ana", PasswordHash = _passwords.Hash("temporaria-1"), MustChangePassword = true };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var login = await Auth(db).AuthenticateAsync("ana", "temporaria-1", ct);
        Assert.True(login.User!.MustChangePassword);

        await Assert.ThrowsAsync<BusinessException>(() => Auth(db).ChangePasswordAsync(user.Id, "temporaria-1", "temporaria-1", ct));
        await Auth(db).ChangePasswordAsync(user.Id, "temporaria-1", "definitiva-2", ct);

        Assert.False((await Auth(db).GetSessionUserAsync(user.Id, ct))!.MustChangePassword);
    }

    private AuthService Auth(MindCareDbContext db) =>
        new(db, _passwords, Options.Create(new SecurityOptions()), new SystemActor(), _clock);

    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}
