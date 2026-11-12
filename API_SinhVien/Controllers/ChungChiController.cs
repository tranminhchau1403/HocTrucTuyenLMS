using System;
using System.Security.Claims;
using BLL;
using BLL.Export;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace API_SinhVien.Controllers
{
    [Route("api/[controller]")]
    [Route("api/v1/certificates")]
    [ApiController]
    [Authorize(Roles = "Học viên,Admin")]
    public class ChungChiController : ControllerBase
    {
        private readonly BaoCaoBLL _bll;
        private readonly SinhVienBLL _sinhVienBLL;
        private readonly IExportService _exportService;

        public ChungChiController(BaoCaoBLL bll, SinhVienBLL sinhVienBLL, IExportService exportService)
        {
            _bll = bll;
            _sinhVienBLL = sinhVienBLL;
            _exportService = exportService;
        }

        private int LayMaSinhVien()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int uid))
            {
                var sv = _sinhVienBLL.GetByMaNguoiDung(uid);
                return sv?.MaSinhVien ?? 0;
            }
            return 0;
        }

        [HttpGet("XuatChungChi/{maDangKy}")]
        public IActionResult XuatChungChi(int maDangKy)
        {
            int msv = LayMaSinhVien();
            var cert = _bll.GetChungChi(maDangKy, msv, isAdmin: User.IsInRole("Admin"));
            if (cert == null)
                return BadRequest(ApiResponse<object>.Fail(400, "Bạn chưa hoàn thành 100% khóa học này hoặc không tìm thấy thông tin đăng ký."));

            return Ok(ApiResponse<ChungChiModel>.Ok(cert));
        }

        /// <summary>Luồng 3: Tải chứng chỉ hoàn thành dạng PDF (Electronic Certificate PDF Export)</summary>
        [HttpGet("TaiPdf/{maDangKy}")]
        [HttpGet("{maDangKy}/pdf")]
        public IActionResult TaiPdf(int maDangKy)
        {
            int msv = LayMaSinhVien();
            var cert = _bll.GetChungChi(maDangKy, msv, isAdmin: User.IsInRole("Admin"));
            if (cert == null)
                return BadRequest(ApiResponse<object>.Fail(400, "Bạn chưa đủ điều kiện nhận chứng chỉ hoặc không tìm thấy thông tin đăng ký."));

            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            byte[] pdfBytes = _exportService.GenerateCertificatePdf(cert, baseUrl);
            string safeCode = !string.IsNullOrWhiteSpace(cert.SoChungChi) ? cert.SoChungChi : $"CTLMS_{cert.MaChungChi}";
            string fileName = $"ChungChi_{safeCode}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        /// <summary>Luồng 3: Tra cứu và xác thực công khai chứng chỉ điện tử qua mã chứng chỉ (Electronic Certificate Verification)</summary>
        [HttpGet("XacThuc/{maChungChiCode}")]
        [HttpGet("verify/{maChungChiCode}")]
        [AllowAnonymous]
        public IActionResult XacThuc(string maChungChiCode)
        {
            var cert = _bll.XacThucChungChiCongKhai(maChungChiCode);
            if (cert == null)
                return NotFound(ApiResponse<object>.Fail(404, "Mã chứng chỉ không tồn tại hoặc đã bị thu hồi."));

            return Ok(ApiResponse<ChungChiModel>.Ok(cert, "Chứng chỉ hợp lệ và đã được xác thực bởi hệ thống CT LMS."));
        }
    }
}
