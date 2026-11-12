using System;
using System.Security.Claims;
using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace API_Login.Controllers
{
    [Route("api/[controller]")]
    [Route("api/v1/notifications")]
    [ApiController]
    [Authorize]
    public class ThongBaoController : ControllerBase
    {
        private readonly ThongBaoBLL _bll;

        public ThongBaoController(ThongBaoBLL bll)
        {
            _bll = bll;
        }

        private int LayMaNguoiDung()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int uid) ? uid : 0;
        }

        [HttpGet("CuaToi")]
        [HttpGet]
        public IActionResult CuaToi()
        {
            int uid = LayMaNguoiDung();
            var list = _bll.GetByNguoiDung(uid);
            return Ok(ApiResponse<object>.Ok(list));
        }

        [HttpPut("DaDoc/{id}")]
        [HttpPatch("{id}/read")]
        public IActionResult DaDoc(int id)
        {
            int uid = LayMaNguoiDung();
            bool ok = _bll.DanhDauDaDoc(id, uid);
            if (!ok) return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy thông báo hoặc bạn không có quyền."));
            return Ok(ApiResponse<object>.Ok(null, "Đã đánh dấu là đã đọc."));
        }

        [HttpDelete("Xoa/{id}")]
        [HttpDelete("{id}")]
        public IActionResult Xoa(int id)
        {
            int uid = LayMaNguoiDung();
            bool ok = _bll.XoaThongBao(id, uid);
            if (!ok) return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy thông báo hoặc bạn không có quyền."));
            return Ok(ApiResponse<object>.Ok(null, "Xóa thông báo thành công."));
        }

        [HttpPost("Gui")]
        [HttpPost]
        [Authorize(Roles = "Admin,Giảng viên")]
        public IActionResult Gui([FromBody] ThongBaoModel model)
        {
            int id = _bll.GuiThongBao(model);
            return Ok(ApiResponse<object>.Ok(new { MaThongBao = id }, "Gửi thông báo thành công."));
        }
    }
}
