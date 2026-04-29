using LogoRaporApp.Middleware;
using LogoRaporApp.Models;
using LogoRaporApp.Services;

var builder = WebApplication.CreateBuilder(args);


// Gerekli servisleri buraya ekliyoruz:
builder.Services.AddControllersWithViews(); // MVC için Controller ve View desteği
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});             // Kullanıcı oturumu (login bilgisi gibi) için Session servisi
builder.Services.AddHttpContextAccessor(); // HttpContext'e session üzerinden erişebilmek için
builder.Services.AddScoped<Db>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddSingleton<LoginProtectionService>();

var app = builder.Build();

// HTTP İstek İşleme Hattı (Request Pipeline) konfigürasyonu
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Hata durumunda Error sayfasına yönlendir
    app.UseHsts(); // Bölgelerde HTTPS kullanımını zorunlu kıl
}

app.UseHttpsRedirection(); // HTTP isteklerini HTTPS'e yönlendir
app.UseStaticFiles();      // CSS, JS, resim gibi statik dosyaları sun
app.UseRouting();          // URL eşleştirme için yönlendirmeyi aktif et
app.UseSession();          // Session middleware'ını aktif et (UseRouting ve UseAuthorization arasına gelmeli)
app.UseMiddleware<AuthMiddleware>();
app.UseAuthorization();    // Yetkilendirme middleware'ını aktif et

// Varsayılan rotayı belirleme: Uygulama açıldığında AccountController'daki Login action'ına gidecek.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
