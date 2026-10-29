using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using MindCare.Application;
using MindCare.Application.Domain;
using MindCare.Application.Security;
using MindCare.Web.Controllers;
using MindCare.Web.Infrastructure;

// Carrega o arquivo .env (se existir) sem sobrescrever variáveis já definidas no ambiente.
DotNetEnv.Env.TraversePath().NoClobber().Load();

var builder = WebApplication.CreateBuilder(args);

// Usuário da requisição: usado pela auditoria. Registrado antes para substituir o ator padrão (sistema).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActor, HttpCurrentActor>();
builder.Services.AddMindCareApplication(builder.Configuration);
builder.Services.Configure<ClinicOptions>(builder.Configuration.GetSection(ClinicOptions.Section));

// Pasta de e-mails de desenvolvimento relativa à raiz do app (ex.: App_Data/mail).
builder.Services.PostConfigure<MindCare.Application.Notifications.EmailOptions>(options =>
{
    if (!string.IsNullOrWhiteSpace(options.PickupDirectory) && !Path.IsPathRooted(options.PickupDirectory))
    {
        options.PickupDirectory = Path.Combine(builder.Environment.ContentRootPath, options.PickupDirectory);
    }
});

builder.Services
    .AddControllersWithViews(options =>
    {
        options.Filters.Add(new AuthorizeFilter());
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        options.Filters.Add<ApiExceptionFilter>();
        options.Filters.Add<RequirePasswordChangeFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    });

builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Login/Logout";
        options.AccessDeniedPath = "/Error/403";
        options.Cookie.Name = "mindcare.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.Admin, policy => policy.RequireRole(nameof(AccessLevel.Administrador)));

// Limita tentativas de login por IP (complementa o bloqueio por conta do AuthService).
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(LoginController.RateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status303SeeOther;
        context.HttpContext.Response.Headers.Location = "/Login?throttled=true";
        return ValueTask.CompletedTask;
    };
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Páginas de erro amigáveis apenas para navegação; chamadas JSON recebem o status puro.
app.UseWhen(
    context => HttpMethods.IsGet(context.Request.Method),
    branch => branch.UseStatusCodePagesWithReExecute("/Error/{0}"));

var culture = new CultureInfo("pt-BR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = [culture],
    SupportedUICultures = [culture],
});

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
