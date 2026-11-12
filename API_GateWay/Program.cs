using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.FileProviders;

var defaultWwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
if (!Directory.Exists(defaultWwwroot))
{
    try { Directory.CreateDirectory(defaultWwwroot); } catch { }
}

var builder = WebApplication.CreateBuilder(args);

var ocelotFile = Environment.GetEnvironmentVariable("OCELOT_CONFIG_FILE") ?? "ocelot.json";
builder.Configuration.AddJsonFile(ocelotFile, optional: false, reloadOnChange: true);

string jwtKey = builder.Configuration["Jwt:Key"] ?? "QuanLyHocTrucTuyen_SecretKey_2026_Secure_JWT_Key_Antigravity!";
string jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "QuanLyHocTrucTuyen_API";
string jwtAudience = builder.Configuration["Jwt:Audience"] ?? "QuanLyHocTrucTuyen_Client";

var authenticationProviderKey = "Bearer";

builder.Services.AddAuthentication()
    .AddJwtBearer(authenticationProviderKey, options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Gateway - Hệ Thống Quản Lý Học Trực Tuyến (CT LMS)",
        Version = "v1",
        Description = "Cổng kết nối tập trung (Ocelot). Mọi API được gọi qua tiền tố /gateway/{Controller}/{Action}"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token JWT theo định dạng: Bearer {token}"
    });
});

builder.Services.AddOcelot(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

// Correlation ID & Private Network Access Header
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var corrId) || string.IsNullOrWhiteSpace(corrId))
    {
        corrId = Guid.NewGuid().ToString("N");
        context.Request.Headers["X-Correlation-Id"] = corrId;
    }
    context.Response.Headers["X-Correlation-Id"] = corrId.ToString();

    if (context.Request.Headers.ContainsKey("Access-Control-Request-Private-Network") || HttpMethods.IsOptions(context.Request.Method))
    {
        context.Response.Headers["Access-Control-Allow-Private-Network"] = "true";
    }
    await next();
});

app.MapHealthChecks("/healthz");

var frontendPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "frontend"));
if (Directory.Exists(frontendPath))
{
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = new PhysicalFileProvider(frontendPath),
        RequestPath = ""
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(frontendPath),
        RequestPath = ""
    });
}
else
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.Use(async (context, next) =>
{
    var p = context.Request.Path.Value;
    if (string.IsNullOrEmpty(p) || p == "/")
    {
        context.Response.Redirect("/swagger");
        return;
    }
    await next();
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cổng API Gateway");
    c.SwaggerEndpoint("http://localhost:5201/swagger/v1/swagger.json", "1. API Login (Port 5201)");
    c.SwaggerEndpoint("http://localhost:5202/swagger/v1/swagger.json", "2. API Admin (Port 5202)");
    c.SwaggerEndpoint("http://localhost:5203/swagger/v1/swagger.json", "3. API Giảng Viên (Port 5203)");
    c.SwaggerEndpoint("http://localhost:5204/swagger/v1/swagger.json", "4. API Sinh Viên (Port 5204)");
    c.RoutePrefix = "swagger";
});

await app.UseOcelot();

app.Run();
