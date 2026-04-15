using Microsoft.AspNetCore.Http.Features;
using KrLensServer.API.Middleware;
using KrLensServer.API.Models;
using KrLensServer.Core.Filters;
using KrLensServer.Core.Logging;
using KrLensServer.Core.Msi;
using KrLensServer.Core.Services;

var builder = WebApplication.CreateBuilder(args);
var maxUploadBytes = builder.Configuration.GetValue<long>("Upload:MaxUploadBytes", 20 * 1024 * 1024);
builder.WebHost.UseUrls(builder.Configuration.GetValue<string>("Server:BaseUrl") ?? "http://localhost:5000");

builder.Services.AddControllers();
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection("Upload"));
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxUploadBytes;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("client", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<MsiEncoder>();
builder.Services.AddSingleton<MsiDecoder>();
builder.Services.AddSingleton<ImageService>();
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddSingleton<FilterLogger>();

builder.Services.AddSingleton<IFilter, GrayscaleFilter>();
builder.Services.AddSingleton<IFilter, InvertFilter>();
builder.Services.AddSingleton<IFilter, BrightnessFilter>();
builder.Services.AddSingleton<IFilter, ContrastFilter>();
builder.Services.AddSingleton<IFilter, GammaFilter>();
builder.Services.AddSingleton<IFilter, SmoothFilter>();
builder.Services.AddSingleton<IFilter, EdgeDetectHVFilter>();
builder.Services.AddSingleton<IFilter, FlipFilter>();
builder.Services.AddSingleton<IFilter, WaterFilter>();
builder.Services.AddSingleton<IFilter, StuckiFilter>();
builder.Services.AddSingleton<IFilter, HistogramEqualizingFilter>();
builder.Services.AddSingleton<FilterRegistry>();
builder.Services.AddSingleton<FilterPipeline>();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCors("client");
app.MapControllers();

app.Run();
