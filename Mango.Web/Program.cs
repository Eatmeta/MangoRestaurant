using Mango.Web;
using Mango.Web.Services;
using Mango.Web.Services.IServices;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IProductService, ProductService>();
builder.Services.AddHttpClient<ICartService, CartService>();
Sd.ProductApiBase = builder.Configuration["ServiceUrls:ProductAPI"];
Sd.ShoppingCartApiBase = builder.Configuration["ServiceUrls:ShoppingCartAPI"];
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("Cookies", cookieOptions 
        => cookieOptions.ExpireTimeSpan = TimeSpan.FromMinutes(10))
    .AddOpenIdConnect("oidc", authenticationOptions =>
    {
        authenticationOptions.Authority = builder.Configuration["ServiceUrls:IdentityAPI"];
        authenticationOptions.GetClaimsFromUserInfoEndpoint = true;
        authenticationOptions.ClientId = "mango";
        authenticationOptions.ClientSecret = "secret";
        authenticationOptions.ResponseType = "code";
        authenticationOptions.ClaimActions.MapJsonKey("role", "role", "role");
        authenticationOptions.ClaimActions.MapJsonKey("sub", "sub", "sub");
        authenticationOptions.TokenValidationParameters.NameClaimType = "name";
        authenticationOptions.TokenValidationParameters.RoleClaimType = "role";
        authenticationOptions.Scope.Add("mango");
        authenticationOptions.SaveTokens = true;
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();