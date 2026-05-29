using Inventario.AplicacionWeb.Utilidades.AutoMapper;
using Inventario.BLL.Implementacion;
using Inventario.BLL.Interfaces;
using Inventario.IOC;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;

// #region agent log
void DebugLog(object data)
{
    var logPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "debug-4d7115.log"));
    try
    {
        var line = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["sessionId"] = "4d7115", ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["location"] = "Program.cs", ["message"] = "debug", ["data"] = data
        }) + "\n";
        File.AppendAllText(logPath, line);
    }
    catch { }
}
// #endregion
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Acceso/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

builder.Services.InyectarDependencias(builder.Configuration);

builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

var app = builder.Build();

// #region agent log
var env = app.Services.GetRequiredService<IWebHostEnvironment>();
var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
var imgDir = Path.Combine(webRoot, "img");
var corazonPath = Path.Combine(imgDir, "corazon.png");
var filesInImg = Directory.Exists(imgDir) ? string.Join(",", Directory.GetFiles(imgDir).Select(Path.GetFileName)) : "img_dir_missing";
DebugLog(new { hypothesisId = "A,E", webRoot, corazonExists = File.Exists(corazonPath), corazonPath, filesInImg });
// #endregion

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

// #region agent log
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value ?? "";
    if (path.Contains("corazon", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/img/", StringComparison.Ordinal))
    {
        var expectedPath = Path.Combine(env.WebRootPath ?? "", path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        DebugLog(new { hypothesisId = "B,C", requestPath = path, expectedPhysicalPath = expectedPath, fileExists = File.Exists(expectedPath) });
    }
    await next(ctx);
    if (path.Contains("corazon", StringComparison.OrdinalIgnoreCase))
        DebugLog(new { hypothesisId = "F", requestPath = path, responseStatusCode = ctx.Response.StatusCode });
});
// #endregion

app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Acceso}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
