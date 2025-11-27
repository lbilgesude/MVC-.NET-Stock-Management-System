using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using StokTakip.Data;
using StokTakip.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<StokTakipBgcContext>(options =>
    options.UseSqlServer(connectionString));


// Kimlik do�rulama servislerini ekleyin
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Giriş sayfası yolu
        options.LogoutPath = "/Account/Logout"; // Kayıt sayfası yolu
    });

builder.Services.AddAuthorization(); // Yetkilendirme servisini ekleyin

builder.Services.AddControllersWithViews();

// StokDurumService'i ekle
builder.Services.AddScoped<IStokDurumService, StokDurumService>();

// UyariService'i ekle
builder.Services.AddScoped<IUyariService, UyariService>();

var app = builder.Build();

// Exception handling - tüm ortamlar için
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Kimlik doğrulama ve yetkilendirme middleware'lerini ekleyin
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Uygulama başlatılırken hata oluştu: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
    // Exception'ı tekrar fırlatma, uygulama düzgün kapanır
}
