using System.Reflection;
using System.Text;
using System.Text.Json;
using BLL.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddLmsCoreServices("API_GiangVien");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new CustomDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new CustomNullableDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new CustomDateOnlyConverter());
        options.JsonSerializerOptions.Converters.Add(new CustomNullableDateOnlyConverter());
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API_GiangVien - Quản Lý Học Trực Tuyến", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token JWT theo định dạng: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? builder.Configuration["Jwt:SecretKey"]
    ?? Environment.GetEnvironmentVariable("Jwt__Key")
    ?? Environment.GetEnvironmentVariable("Jwt__SecretKey")
    ?? "QuanLyHocTrucTuyen_SecretKey_2026_Secure_JWT_Key_Antigravity!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "QuanLyHocTrucTuyen_API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "QuanLyHocTrucTuyen_Client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async ctx =>
        {
            ctx.HandleResponse();
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.Response.ContentType = "application/json; charset=utf-8";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                success = false,
                statusCode = 401,
                message = "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.",
                traceId = ctx.HttpContext.TraceIdentifier
            }));
        },
        OnForbidden = async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            ctx.Response.ContentType = "application/json; charset=utf-8";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                success = false,
                statusCode = 403,
                message = "Chỉ tài khoản Giảng viên mới có quyền thực hiện thao tác này.",
                traceId = ctx.HttpContext.TraceIdentifier
            }));
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseLmsCorePipeline();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
