using System;
using System.Security.Claims;
using BLL;
using BLL.Export;
using DAL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace API_Admin.Controllers
{
    [Route("api/[controller]")]
    [Route("api/v1/reports")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class BaoCaoController : ControllerBase
    {
        private readonly BaoCaoBLL _bll;
        private readonly KhoaHocBLL _khoaHocBLL;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IExportService _exportService;

        public BaoCaoController(
            BaoCaoBLL bll,
            KhoaHocBLL khoaHocBLL,
            IAuditLogRepository auditLogRepo,
            IExportService exportService)
        {
            _bll = bll;
            _khoaHocBLL = khoaHocBLL;
            _auditLogRepo = auditLogRepo;
            _exportService = exportService;
        }

        [HttpGet("TongQuan")]
        [HttpGet("overview")]
        public IActionResult TongQuan()
        {
            var data = _bll.GetTongQuan();
            return Ok(ApiResponse<BaoCaoTongQuanModel>.Ok(data));
        }

        /// <summary>Xuất báo cáo tổng quan ra file Excel (.xlsx) chuẩn Phụ lục B</summary>
        [HttpGet("XuatExcel")]
        [HttpGet("excel")]
        public IActionResult XuatExcel()
        {
            var data = _bll.GetTongQuan();
            byte[] fileBytes = _exportService.GenerateOverviewExcel(data);
            string fileName = $"BaoCao_TongQuan_CTLMS_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>Luồng 5: Admin phê duyệt hoặc từ chối khóa học (Course Approval Workflow)</summary>
        [HttpPut("PheDuyetKhoaHoc/{maKhoaHoc}")]
        [HttpPatch("courses/{maKhoaHoc}/approval")]
        public IActionResult PheDuyetKhoaHoc(int maKhoaHoc, [FromBody] PheDuyetKhoaHocRequest request)
        {
            int maAdmin = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int uid) ? uid : 1;
            bool ok = _khoaHocBLL.PheDuyet(maKhoaHoc, request.TrangThaiDuyet, request.LyDoTuChoi, maAdmin);
            if (!ok) return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy khóa học."));
            return Ok(ApiResponse<object>.Ok(null, $"Đã cập nhật trạng thái duyệt khóa học thành: {request.TrangThaiDuyet}."));
        }

        /// <summary>Truy xuất nhật ký kiểm toán hệ thống (Audit Logs - Bài 17 Giáo trình)</summary>
        [HttpGet("NhatKyKiemToan")]
        [HttpGet("audit-logs")]
        public IActionResult NhatKyKiemToan([FromQuery] int trang = 1, [FromQuery] int kichThuocTrang = 20, [FromQuery] string? hanhDong = null)
        {
            var logs = _auditLogRepo.GetPaged(trang, kichThuocTrang, hanhDong);
            return Ok(ApiResponse<PagedResult<AuditLogModel>>.Ok(logs));
        }
    }
}
