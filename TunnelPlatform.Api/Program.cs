using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Serilog;
using TunnelPlatform.Api.Data;
using TunnelPlatform.Api.Middleware;
using TunnelPlatform.Api.Options;
using TunnelPlatform.Api.Services;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

var logsDirectory = Path.Combine(builder.Environment.ContentRootPath, "logs");
Directory.CreateDirectory(logsDirectory);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logsDirectory, "api-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = long.MaxValue;
});

builder.Services.AddDbContext<TunnelPlatformDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "隧道平台数据上传与查询 API",
        Version = "v1",
        Description = "用于工程台账同步、站点或区间数据上传、病害查询、灰度图查询、二维病害高清图查询和文件树浏览。",
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IFileTreeService, FileTreeService>();

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TunnelPlatformDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await DatabaseSchemaInitializer.InitializeAsync(dbContext);
}
catch (Exception ex)
{
    Log.Fatal(ex, "数据库初始化失败。");
    throw;
}

var storageOptions = app.Services.GetRequiredService<IOptions<StorageOptions>>().Value;
var storageRoot = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, storageOptions.RootPath));
Directory.CreateDirectory(storageRoot);

app.UseSerilogRequestLogging();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseCors("default");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storageRoot),
    RequestPath = "/storage",
});

app.MapControllers();
app.MapGet("/docs", () => Results.Redirect("/swagger"));

try
{
    app.Run();
}
catch (IOException ex) when (ex.Message.Contains("address already in use", StringComparison.OrdinalIgnoreCase))
{
    Log.Fatal(ex, "API 启动失败：5140 端口已被占用。请关闭已有 TunnelPlatform.Api 进程后再启动。");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
