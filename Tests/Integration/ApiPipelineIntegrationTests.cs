using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using API_Admin;
using API_SinhVien;
using BLL.Security;
using DAL;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Models;
using Moq;
using Xunit;

namespace BTL_QuanLyHocTrucTuyen.Tests.Integration
{
    /// <summary>
    /// Kiểm thử tích hợp (Integration Tests) chuẩn Bài 14 Giáo trình:
    /// Sử dụng WebApplicationFactory khởi chạy toàn bộ HTTP Pipeline thực tế (CorrelationIdMiddleware,
    /// GlobalExceptionMiddleware RFC 7807, RateLimiter, JWT Bearer Auth, RBAC 3 vai trò, HealthChecks, Controllers)
    /// kết hợp Mock Repository ở tầng DAL để kiểm thử độc lập, có thể chạy trên mọi CI/CD runner.
    /// </summary>
    public class ApiPipelineIntegrationTests
    {
        private static WebApplicationFactory<Program> CreateLoginFactory(
            Mock<INguoiDungRepository>? mockUserRepo = null,
            Mock<IRefreshTokenRepository>? mockRefreshRepo = null,
            Mock<IAuditLogRepository>? mockAuditRepo = null)
        {
            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    if (mockUserRepo != null)
                    {
                        services.RemoveAll<INguoiDungRepository>();
                        services.AddScoped(_ => mockUserRepo.Object);
                    }
                    if (mockRefreshRepo != null)
                    {
                        services.RemoveAll<IRefreshTokenRepository>();
                        services.AddScoped(_ => mockRefreshRepo.Object);
                    }
                    if (mockAuditRepo != null)
                    {
                        services.RemoveAll<IAuditLogRepository>();
                        services.AddScoped(_ => mockAuditRepo.Object);
                    }
                });
            });
        }

        private static WebApplicationFactory<ApiAdminMarker> CreateAdminFactory(
            Mock<IDanhMucRepository>? mockDanhMucRepo = null,
            Mock<IKhoaHocRepository>? mockKhoaHocRepo = null)
        {
            return new WebApplicationFactory<ApiAdminMarker>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    if (mockDanhMucRepo != null)
                    {
                        services.RemoveAll<IDanhMucRepository>();
                        services.AddScoped(_ => mockDanhMucRepo.Object);
                    }
                    if (mockKhoaHocRepo != null)
                    {
                        services.RemoveAll<IKhoaHocRepository>();
                        services.AddScoped(_ => mockKhoaHocRepo.Object);
                    }
                });
            });
        }

        private static WebApplicationFactory<ApiSinhVienMarker> CreateSinhVienFactory(
            Mock<IBaoCaoRepository>? mockBaoCaoRepo = null)
        {
            return new WebApplicationFactory<ApiSinhVienMarker>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    if (mockBaoCaoRepo != null)
                    {
                        services.RemoveAll<IBaoCaoRepository>();
                        services.AddScoped(_ => mockBaoCaoRepo.Object);
                    }
                });
            });
        }

        [Fact]
        public async Task HealthLive_Endpoint_Returns200Healthy_And_CorrelationIdHeader()
        {
            await using var factory = CreateLoginFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Correlation-Id", "corr-test-12345");

            var response = await client.GetAsync("/health/live");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var values));
            Assert.Contains("corr-test-12345", values!);
        }

        [Fact]
        public async Task Login_WithValidBCryptCredentials_Returns200_AccessToken_And_RefreshToken()
        {
            var mockUserRepo = new Mock<INguoiDungRepository>();
            var mockRefreshRepo = new Mock<IRefreshTokenRepository>();
            var mockAuditRepo = new Mock<IAuditLogRepository>();

            string bcryptHash = PasswordHasher.HashPassword("123456");
            mockUserRepo.Setup(r => r.GetByTenDangNhap("admin"))
                .Returns(new NguoiDungModel
                {
                    MaNguoiDung = 1,
                    TenDangNhap = "admin",
                    MatKhau = bcryptHash,
                    HoTen = "Quản trị viên",
                    Email = "admin@ctlms.edu.vn",
                    QuyenHan = "Admin",
                    TrangThai = true,
                    DaXoa = false
                });

            await using var factory = CreateLoginFactory(mockUserRepo, mockRefreshRepo, mockAuditRepo);
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/XacThuc/DangNhap", new
            {
                TenDangNhap = "admin",
                MatKhau = "123456"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.GetProperty("success").GetBoolean());
            var data = json.GetProperty("data");
            Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("accessToken").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("refreshToken").GetString()));
            Assert.Equal("Admin", data.GetProperty("quyenHan").GetString());
        }

        [Fact]
        public async Task ProtectedEndpoint_WithoutToken_Returns401Unauthorized_WithUniformJson()
        {
            await using var factory = CreateLoginFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/XacThuc/LayThongTin");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(json.GetProperty("success").GetBoolean());
            Assert.Equal(401, json.GetProperty("statusCode").GetInt32());
        }

        [Fact]
        public async Task AdminEndpoint_WithStudentRoleToken_Returns403Forbidden()
        {
            await using var factory = CreateAdminFactory();
            using var client = factory.CreateClient();

            var jwtService = new JwtTokenService();
            var (studentToken, _, _) = jwtService.GenerateAccessToken(new NguoiDungModel
            {
                MaNguoiDung = 10,
                TenDangNhap = "sv_test",
                HoTen = "Sinh viên kiểm thử",
                Email = "sv@ctlms.edu.vn",
                QuyenHan = "Học viên"
            }, maSinhVien: 5);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", studentToken);

            var response = await client.GetAsync("/api/NguoiDung/LayDanhSach");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(json.GetProperty("success").GetBoolean());
            Assert.Equal(403, json.GetProperty("statusCode").GetInt32());
        }

        [Fact]
        public async Task GlobalExceptionMiddleware_MapsBusinessRuleException_ToRfc7807ProblemDetails()
        {
            var mockDanhMucRepo = new Mock<IDanhMucRepository>();
            await using var factory = CreateAdminFactory(mockDanhMucRepo);
            using var client = factory.CreateClient();

            var jwtService = new JwtTokenService();
            var (adminToken, _, _) = jwtService.GenerateAccessToken(new NguoiDungModel
            {
                MaNguoiDung = 1,
                TenDangNhap = "admin",
                HoTen = "Admin",
                Email = "admin@ctlms.edu.vn",
                QuyenHan = "Admin"
            });

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            // Gửi TenDanhMuc rỗng để kích hoạt ArgumentException trong DanhMucBLL -> GlobalExceptionMiddleware trả 400 ProblemDetails
            var response = await client.PostAsJsonAsync("/api/DanhMuc/Them", new DanhMucModel
            {
                TenDanhMuc = "   ",
                MoTa = "Danh mục lỗi"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(json.GetProperty("success").GetBoolean());
            Assert.Equal(400, json.GetProperty("status").GetInt32());
            Assert.Contains("Tên danh mục không được để trống", json.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task PublicCertificateVerification_AllowsAnonymous_And_ReturnsVerifiedCertificate()
        {
            var mockBaoCaoRepo = new Mock<IBaoCaoRepository>();
            mockBaoCaoRepo.Setup(r => r.GetChungChiByCode("CTLMS-2026-000001"))
                .Returns(new ChungChiModel
                {
                    MaChungChi = 1,
                    MaDangKy = 1,
                    MaSinhVien = 1,
                    HoTenSinhVien = "Nguyễn Văn Nam",
                    TenKhoaHoc = "Lập trình C# cơ bản đến OOP chuyên sâu",
                    SoChungChi = "CTLMS-2026-000001",
                    XepLoai = "Hoàn thành xuất sắc",
                    DiemTrungBinh = 9.5m
                });

            await using var factory = CreateSinhVienFactory(mockBaoCaoRepo);
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/ChungChi/XacThuc/CTLMS-2026-000001");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.Equal("CTLMS-2026-000001", json.GetProperty("data").GetProperty("soChungChi").GetString());
        }
    }
}
