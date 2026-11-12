using System;
using System.Collections.Generic;
using BLL;
using BLL.BackgroundJobs;
using BLL.Security;
using DAL;
using Models;
using Moq;
using Xunit;

namespace BTL_QuanLyHocTrucTuyen.Tests.Unit
{
    public class AuthAndUserServiceTests
    {
        private readonly Mock<INguoiDungRepository> _mockUserRepo = new();
        private readonly Mock<ISinhVienRepository> _mockStudentRepo = new();
        private readonly Mock<IGiangVienRepository> _mockTeacherRepo = new();
        private readonly Mock<IRefreshTokenRepository> _mockRefreshRepo = new();
        private readonly Mock<IAuditLogRepository> _mockAuditRepo = new();
        private readonly Mock<IJwtTokenService> _mockJwtService = new();
        private readonly Mock<IBackgroundTaskQueue> _mockQueue = new();

        private NguoiDungBLL CreateService()
        {
            return new NguoiDungBLL(
                _mockUserRepo.Object,
                _mockStudentRepo.Object,
                _mockTeacherRepo.Object,
                _mockRefreshRepo.Object,
                _mockAuditRepo.Object,
                _mockJwtService.Object,
                _mockQueue.Object);
        }

        [Fact]
        public void Login_WithCorrectCredentials_ReturnsUser()
        {
            var bll = CreateService();
            string hash = PasswordHasher.HashPassword("123456");
            _mockUserRepo.Setup(r => r.GetByTenDangNhap("hocvien01")).Returns(new NguoiDungModel
            {
                MaNguoiDung = 1,
                TenDangNhap = "hocvien01",
                MatKhau = hash,
                QuyenHan = "Học viên",
                TrangThai = true,
                DaXoa = false
            });

            var user = bll.Login("hocvien01", "123456");

            Assert.NotNull(user);
            Assert.Equal("hocvien01", user.TenDangNhap);
        }

        [Fact]
        public void Login_WithWrongPassword_ReturnsNull()
        {
            var bll = CreateService();
            string hash = PasswordHasher.HashPassword("123456");
            _mockUserRepo.Setup(r => r.GetByTenDangNhap("hocvien01")).Returns(new NguoiDungModel
            {
                MaNguoiDung = 1,
                TenDangNhap = "hocvien01",
                MatKhau = hash,
                TrangThai = true
            });

            var user = bll.Login("hocvien01", "WrongPassword");

            Assert.Null(user);
        }

        [Fact]
        public void Login_WithLockedUser_ReturnsNull()
        {
            var bll = CreateService();
            string hash = PasswordHasher.HashPassword("123456");
            _mockUserRepo.Setup(r => r.GetByTenDangNhap("locked_user")).Returns(new NguoiDungModel
            {
                MaNguoiDung = 2,
                TenDangNhap = "locked_user",
                MatKhau = hash,
                TrangThai = false
            });

            var user = bll.Login("locked_user", "123456");

            Assert.Null(user);
        }

        [Fact]
        public void RefreshAccessToken_WithValidToken_RotatesTokenSuccessfully()
        {
            var bll = CreateService();
            _mockJwtService.Setup(j => j.ComputeSha256Hash("valid-raw-refresh")).Returns("hashed-valid-refresh");
            _mockJwtService.Setup(j => j.GenerateAccessToken(It.IsAny<NguoiDungModel>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .Returns(("new-access-token", "jwt-id-999", DateTime.Now.AddHours(2)));
            _mockJwtService.Setup(j => j.GenerateRefreshToken())
                .Returns(("new-raw-refresh", "new-hashed-refresh", DateTime.UtcNow.AddDays(7)));

            _mockRefreshRepo.Setup(r => r.GetByTokenHash("hashed-valid-refresh")).Returns(new RefreshTokenModel
            {
                MaRefreshToken = 10,
                MaNguoiDung = 5,
                TokenHash = "hashed-valid-refresh",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(2),
                RevokedAtUtc = null
            });

            _mockUserRepo.Setup(r => r.GetById(5)).Returns(new NguoiDungModel
            {
                MaNguoiDung = 5,
                TenDangNhap = "student5",
                QuyenHan = "Học viên",
                TrangThai = true
            });

            var response = bll.RefreshAccessToken("valid-raw-refresh");

            Assert.NotNull(response);
            Assert.Equal("new-access-token", response.AccessToken);
            Assert.Equal("new-raw-refresh", response.RefreshToken);
            _mockRefreshRepo.Verify(r => r.RevokeToken("hashed-valid-refresh", "new-hashed-refresh", "Rotated"), Times.Once);
            _mockRefreshRepo.Verify(r => r.Insert(It.IsAny<RefreshTokenModel>()), Times.Once);
        }

        [Fact]
        public void RefreshAccessToken_WhenRevokedTokenReused_RevokesAllUserTokensAndThrowsUnauthorized()
        {
            var bll = CreateService();
            _mockJwtService.Setup(j => j.ComputeSha256Hash("stolen-token")).Returns("hashed-stolen-token");
            _mockRefreshRepo.Setup(r => r.GetByTokenHash("hashed-stolen-token")).Returns(new RefreshTokenModel
            {
                MaRefreshToken = 11,
                MaNguoiDung = 7,
                TokenHash = "hashed-stolen-token",
                RevokedAtUtc = DateTime.UtcNow.AddHours(-1), // Bị thu hồi trước đó
                ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
            });

            var ex = Assert.Throws<UnauthorizedAppException>(() => bll.RefreshAccessToken("stolen-token"));

            Assert.Contains("thu hồi", ex.Message);
            _mockRefreshRepo.Verify(r => r.RevokeAllUserTokens(7, It.Is<string>(s => s.Contains("Reuse Detection"))), Times.Once);
        }

        [Fact]
        public void DangKyHocVien_WhenValid_InsertsStudentAndEnqueuesWelcomeEmail()
        {
            var bll = CreateService();
            _mockUserRepo.Setup(r => r.GetByTenDangNhap("new_student")).Returns((NguoiDungModel?)null);
            _mockUserRepo.Setup(r => r.ExistsEmail("new_student@school.edu.vn", null)).Returns(false);
            _mockUserRepo.Setup(r => r.InsertSinhVien(It.IsAny<NguoiDungModel>(), It.IsAny<SinhVienModel>())).Returns((101, 202));

            var result = bll.DangKyHocVien(new RegisterStudentRequest
            {
                TenDangNhap = "new_student",
                MatKhau = "123456",
                HoTen = "Sinh Viên Mới",
                Email = "new_student@school.edu.vn",
                SoDienThoai = "0988776655"
            });

            Assert.Equal(101, result.MaNguoiDung);
            Assert.Equal(202, result.MaSinhVien);
            _mockQueue.Verify(q => q.TryQueueEmailNotification(
                "new_student@school.edu.vn",
                It.Is<string>(s => s.Contains("Chào mừng")),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void DangKyHocVien_WhenDuplicateUsername_ThrowsInvalidOperationException()
        {
            var bll = CreateService();
            _mockUserRepo.Setup(r => r.GetByTenDangNhap("dup_student")).Returns(new NguoiDungModel { MaNguoiDung = 99 });

            var ex = Assert.Throws<InvalidOperationException>(() => bll.DangKyHocVien(new RegisterStudentRequest
            {
                TenDangNhap = "dup_student",
                MatKhau = "123456",
                HoTen = "Tên",
                Email = "test@edu.vn"
            }));

            Assert.Contains("đã tồn tại", ex.Message);
        }
    }
}
