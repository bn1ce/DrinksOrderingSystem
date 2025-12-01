global using Demo;
using MenuOrderingSystem.Models;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Stripe Config
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

builder.Services.AddControllersWithViews();
builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\DB.mdf;
");
builder.Services.AddScoped<Helper>();
builder.Services.AddAuthentication().AddCookie();
builder.Services.AddHttpContextAccessor();
// Add session
builder.Services.AddSession();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization("en-MY");
// Use session
app.UseSession();

app.MapDefaultControllerRoute();
app.Run();
