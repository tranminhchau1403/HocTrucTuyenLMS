using System;
using System.Security.Claims;
using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace API_GiangVien.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Giảng viên")]
    public class HoSoController : ControllerBase
    {
        private readonly NguoiDungBLL _nguoiDungBLL;
        private readonly GiangVienBLL _giangVienBLL;

        public HoSoController(NguoiDungBLL nguoiDungBLL, GiangVienBLL giangVienBLL)
        {
            _nguoiDungBLL = nguoiDungBLL;
            _giangVienBLL = giangVienBLL;
        }

        [HttpGet("Lay")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Lay()
        {
            var profile = _nguoiDungBLL.GetProfile(LayMaNguoiDung());
            if (profile == null)
                return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy hồ sơ giảng viên."));

            return Ok(ApiResponse<UserProfileDto>.Ok(profile));
        }

        [HttpPut("CapNhat")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult CapNhat([FromBody] UpdateTeacherProfileRequest request)
        {
            if (!_giangVienBLL.UpdateHoSo(LayMaNguoiDung(), request))
                return BadRequest(ApiResponse<object>.Fail(400, "Cập nhật hồ sơ thất bại."));

            return Ok(ApiResponse<object>.Ok(null, "Cập nhật hồ sơ thành công."));
        }

        private int LayMaNguoiDung()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }
    }
}
