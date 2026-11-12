using System;
using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace API_Admin.Controllers
{
    [Route("api/[controller]")]
    [Route("api/v1/categories")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class DanhMucController : ControllerBase
    {
        private readonly DanhMucBLL _bll;

        public DanhMucController(DanhMucBLL bll)
        {
            _bll = bll;
        }

        [HttpGet("LayDanhSach")]
        [HttpGet]
        [AllowAnonymous]
        public IActionResult LayDanhSach()
        {
            var list = _bll.GetAll();
            return Ok(ApiResponse<object>.Ok(list));
        }

        [HttpGet("LayTheoId/{id}")]
        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult LayTheoId(int id)
        {
            var item = _bll.GetById(id);
            if (item == null)
                return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy danh mục."));

            return Ok(ApiResponse<DanhMucModel>.Ok(item));
        }

        [HttpPost("Them")]
        [HttpPost]
        public IActionResult Them([FromBody] DanhMucModel model)
        {
            int id = _bll.Insert(model);
            return Ok(ApiResponse<object>.Ok(new { MaDanhMuc = id }, "Thêm danh mục thành công."));
        }

        [HttpPut("Sua/{id}")]
        [HttpPut("{id}")]
        public IActionResult Sua(int id, [FromBody] DanhMucModel model)
        {
            model.MaDanhMuc = id;
            bool ok = _bll.Update(model);
            if (!ok) return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy danh mục."));
            return Ok(ApiResponse<object>.Ok(null, "Cập nhật danh mục thành công."));
        }

        [HttpDelete("Xoa/{id}")]
        [HttpDelete("{id}")]
        public IActionResult Xoa(int id)
        {
            bool ok = _bll.Delete(id);
            if (!ok) return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy danh mục."));
            return Ok(ApiResponse<object>.Ok(null, "Xóa danh mục thành công."));
        }
    }
}
