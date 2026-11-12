using System;
using System.Security.Claims;
using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Models;

namespace API_Login.Controllers
{
    /// <summary>Xác thực, đăng ký, xoay vòng Refresh Token và thông tin tài khoản (JWT + RBAC 3 vai trò).</summary>
    [Route("api/[controller]")]
    [Route("api/v1/auth")]
    [ApiController]
    public class XacThucController : ControllerBase
    {
        private readonly NguoiDungBLL _nguoiDungBLL;

        public XacThucController(NguoiDungBLL nguoiDungBLL)
        {
            _nguoiDungBLL = nguoiDungBLL;
        }

        /// <summary>Đăng nhập bằng tên đăng nhập và mật khẩu, trả về JWT AccessToken kèm RefreshToken.</summary>
        [HttpPost("DangNhap")]
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult DangNhap([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TenDangNhap) || string.IsNullOrWhiteSpace(request.MatKhau))
                return BadRequest(ApiResponse<object>.Fail(400, "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.", null, HttpContext.TraceIdentifier));

            var user = _nguoiDungBLL.Login(request.TenDangNhap, request.MatKhau);
            if (user == null)
                return Unauthorized(ApiResponse<object>.Fail(401, "Tên đăng nhập hoặc mật khẩu không chính xác, hoặc tài khoản đã bị khóa.", null, HttpContext.TraceIdentifier));

            string? remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = _nguoiDungBLL.IssueAuthResponse(user, remoteIp);

            return Ok(ApiResponse<AuthResponse>.Ok(response, "Đăng nhập thành công."));
        }

        /// <summary>Làm mới AccessToken bằng RefreshToken (Refresh Token Rotation - Bài 10 Giáo trình).</summary>
        [HttpPost("RefreshToken")]
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
        {
            string? remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = _nguoiDungBLL.RefreshAccessToken(request.RefreshToken, remoteIp);
            return Ok(ApiResponse<AuthResponse>.Ok(response, "Làm mới phiên đăng nhập thành công."));
        }

        /// <summary>Thu hồi RefreshToken (Revoke Token).</summary>
        [HttpPost("RevokeToken")]
        [HttpPost("revoke-token")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult RevokeToken([FromBody] RevokeTokenRequest request)
        {
            bool ok = _nguoiDungBLL.RevokeRefreshToken(request.RefreshToken, request.Reason ?? "Thu hồi theo yêu cầu người dùng");
            if (!ok)
                return BadRequest(ApiResponse<object>.Fail(400, "Refresh Token không hợp lệ hoặc đã bị thu hồi trước đó."));

            return Ok(ApiResponse<object>.Ok(null, "Đã thu hồi Refresh Token thành công."));
        }

        /// <summary>Học viên tự đăng ký tài khoản (quyền Học viên, kích hoạt ngay, băm mật khẩu BCrypt).</summary>
        [HttpPost("DangKyHocVien")]
        [HttpPost("register-student")]
        [AllowAnonymous]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public IActionResult DangKyHocVien([FromBody] RegisterStudentRequest request)
        {
            var (maNguoiDung, maSinhVien) = _nguoiDungBLL.DangKyHocVien(request);
            return Ok(ApiResponse<object>.Ok(
                new { MaNguoiDung = maNguoiDung, MaSinhVien = maSinhVien },
                "Đăng ký tài khoản học viên thành công."));
        }

        /// <summary>Admin tạo tài khoản giảng viên (quyền Giảng viên, kích hoạt ngay).</summary>
        [HttpPost("DangKyGiangVien")]
        [HttpPost("register-teacher")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult DangKyGiangVien([FromBody] RegisterTeacherRequest request)
        {
            var (maNguoiDung, maGiangVien) = _nguoiDungBLL.DangKyGiangVien(request);
            return Ok(ApiResponse<object>.Ok(
                new { MaNguoiDung = maNguoiDung, MaGiangVien = maGiangVien },
                "Tạo tài khoản giảng viên thành công."));
        }

        /// <summary>Đổi mật khẩu của chính người dùng đang đăng nhập (tự động thu hồi các RefreshToken cũ).</summary>
        [HttpPost("DoiMatKhau")]
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult DoiMatKhau([FromBody] ChangePasswordRequest request)
        {
            int maNguoiDung = LayMaNguoiDung();
            bool result = _nguoiDungBLL.ChangePassword(maNguoiDung, request.MatKhauCu, request.MatKhauMoi);
            if (result)
                return Ok(ApiResponse<object>.Ok(null, "Đổi mật khẩu thành công."));

            return BadRequest(ApiResponse<object>.Fail(400, "Đổi mật khẩu thất bại."));
        }

        /// <summary>Lấy hồ sơ của người dùng đang đăng nhập.</summary>
        [HttpGet("LayThongTin")]
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult LayThongTin()
        {
            var profile = _nguoiDungBLL.GetProfile(LayMaNguoiDung());
            if (profile == null)
                return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy người dùng."));

            return Ok(ApiResponse<UserProfileDto>.Ok(profile));
        }

        /// <summary>Đăng xuất và thu hồi RefreshToken nếu được gửi kèm.</summary>
        [HttpPost("DangXuat")]
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult DangXuat([FromBody] RevokeTokenRequest? request = null)
        {
            if (request != null && !string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                _nguoiDungBLL.RevokeRefreshToken(request.RefreshToken, "Đăng xuất");
            }
            return Ok(ApiResponse<object>.Ok(null, "Đăng xuất thành công."));
        }

        private int LayMaNguoiDung()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }
    }
}
