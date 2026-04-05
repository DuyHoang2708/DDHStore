using Microsoft.EntityFrameworkCore;
using DDHSTORE.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 🔥 1. Kết nối Oracle
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleDb"))
           .EnableSensitiveDataLogging()
           .LogTo(Console.WriteLine)
);

// 🔥 2. MVC
builder.Services.AddControllersWithViews();

// 🔥 3. Session (giỏ hàng)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 🔥 4. Authentication (QUAN TRỌNG NHẤT)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
})
.AddGoogle(options =>
{
    options.ClientId = "463249022960-88iimqur32vrkrbrhul6ulh542ok3udc.apps.googleusercontent.com";
    options.ClientSecret = "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";

    options.Events.OnRedirectToAuthorizationEndpoint = context =>
    {
        var redirectUri = context.RedirectUri;

        // 🔥 ép Google hiện chọn account
        if (!redirectUri.Contains("prompt="))
        {
            redirectUri += "&prompt=select_account";
        }

        context.Response.Redirect(redirectUri);
        return Task.CompletedTask;
    };
})

.AddFacebook(options =>
{
    options.AppId = "YOUR_FACEBOOK_APP_ID";
    options.AppSecret = "YOUR_FACEBOOK_APP_SECRET";
});
// 🔥 5. Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// 🔥 6. Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 🔥 THỨ TỰ QUAN TRỌNG
app.UseSession();           // Session trước
app.UseAuthentication();    // 🔥 PHẢI CÓ (bạn đang thiếu)
app.UseAuthorization();

// 🔥 Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();