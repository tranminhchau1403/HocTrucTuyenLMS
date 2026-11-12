using System;
using System.Security.Claims;
using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace API_Admin.Controllers
{
    /// <summary>Admin quản lý người dùng: danh sách phân trang, tìm kiếm, sắp xếp, khóa/mở khóa, xóa mềm.</summary>
    [Route("api/[controller]")]
    [Route("api/v1/users")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class NguoiDungController : ControllerBase
    {
        private readonly NguoiDungBLL _bll;

        public NguoiDungController(NguoiDungBLL bll)
        {
            _bll = bll;
        }

        [HttpGet("LayDanhSach")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult LayDanhSach(
            [FromQuery] string? quyenHan,
            [FromQuery] string? tuKhoa,
            [FromQuery] string? search,
            [FromQuery] int trang = 1,
            [FromQuery] int? pageIndex = null,
            [FromQuery] int kichThuocTrang = 20,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = null)
        {
            int actualPage = pageIndex ?? trang;
            int actualSize = pageSize ?? kichThuocTrang;
            string? actualKeyword = !string.IsNullOrWhiteSpace(search) ? search : tuKhoa;

            var result = _bll.GetAll(quyenHan, actualKeyword, actualPage, actualSize, sortBy, sortOrder);
            foreach (var item in result.Items)
            {
                item.MatKhau = string.Empty;
            }

            return Ok(ApiResponse<PagedResult<NguoiDungModel>>.Ok(
                result,
                "Lấy danh sách người dùng thành công.",
                new PaginationMeta
                {
                    PageIndex = result.PageIndex,
                    PageSize = result.PageSize,
                    TotalItems = result.TotalItems,
                    TotalPages = result.TotalPages
                }));
        }

        [HttpGet("LayTheoId/{id}")]
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult LayTheoId(int id)
        {
            var profile = _bll.GetProfile(id);
            if (profile == null)
                return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy người dùng."));

            return Ok(ApiResponse<UserProfileDto>.Ok(profile));
        }

        [HttpPut("DoiTrangThai/{id}")]
        [HttpPatch("{id}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DoiTrangThai(int id, [FromBody] ChangeStatusRequest request)
        {
            if (!_bll.UpdateTrangThai(id, request.TrangThai, LayMaNguoiDung()))
                return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy người dùng."));

            return Ok(ApiResponse<object>.Ok(null, request.TrangThai ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản."));
        }

        [HttpDelete("Xoa/{id}")]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Xoa(int id)
        {
            if (!_bll.Delete(id, LayMaNguoiDung()))
                return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy người dùng."));

            return Ok(ApiResponse<object>.Ok(null, "Xóa người dùng thành công."));
        }

        private int LayMaNguoiDung()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : 0;
        }
    }
}
