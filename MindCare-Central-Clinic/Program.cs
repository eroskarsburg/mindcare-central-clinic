using MindCare.Application.DataAccess.Repository;
using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Services;
using MindCare.Application.Services.IServices;
using MindCare.Application.DataAccess.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MindCare_Central_Clinic.Modules;
using MindCare_Central_Clinic.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register the DbContext with the appropriate options
builder.Services.AddScoped<DbContextBase>();

// Register other services/extensions
builder.Services.AddApplicationExtensions();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
