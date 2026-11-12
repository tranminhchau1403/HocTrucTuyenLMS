using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using BLL;
using BLL.Export;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace API_GiangVien.Controllers
{
    [Route("api/[controller]")]
    [Route("api/v1/instructor/courses")]
    [ApiController]
    [Authorize(Roles = "Giảng viên,Admin")]
    public class KhoaHocController : ControllerBase
    {
        private readonly KhoaHocBLL _bll;
        private readonly GiangVienBLL _giangVienBLL;

        public KhoaHocController(KhoaHocBLL bll, GiangVienBLL giangVienBLL)
        {
            _bll = bll;
            _giangVienBLL = giangVienBLL;
        }

        private int LayMaGiangVien()
        {
            var gvClaim = User.FindFirst("MaGiangVien")?.Value;
            if (int.TryParse(gvClaim, out int mgv) && mgv > 0) return mgv;

            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int uid))
            {
                var gv = _giangVienBLL.GetByMaNguoiDung(uid);
                return gv?.MaGiangVien ?? 0;
            }
            return 0;
        }

        [HttpGet("LayDanhSachCuaToi")]
        [HttpGet]
        public IActionResult LayDanhSachCuaToi(
            [FromQuery] int? maDanhMuc,
            [FromQuery] string? tuKhoa,
            [FromQuery] int trang = 1,
            [FromQuery] int kichThuocTrang = 20,
            [FromQuery] string? trangThaiDuyet = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = null)
        {
            bool isAdmin = User.IsInRole("Admin");
            int? mgv = isAdmin ? null : LayMaGiangVien();
            var result = _bll.GetAll(maDanhMuc, mgv, tuKhoa, trang, kichThuocTrang, trangThaiDuyet, sortBy, sortOrder);
            return Ok(ApiResponse<PagedResult<KhoaHocModel>>.Ok(result));
        }

        [HttpGet("LayTheoId/{id}")]
        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult LayTheoId(int id)
        {
            var item = _bll.GetById(id);
            if (item == null)
                return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy khóa học."));

            return Ok(ApiResponse<KhoaHocModel>.Ok(item));
        }

        [HttpPost("TaoKhoaHoc")]
        [HttpPost]
        public IActionResult TaoKhoaHoc([FromBody] KhoaHocModel model)
        {
            int mgv = LayMaGiangVien();
            if (mgv <= 0 && !User.IsInRole("Admin"))
                return Unauthorized(ApiResponse<object>.Fail(401, "Không tìm thấy hồ sơ giảng viên của bạn."));

            if (mgv > 0) model.MaGiangVien = mgv;
            int id = _bll.Insert(model);
            return Ok(ApiResponse<object>.Ok(new { MaKhoaHoc = id }, "Tạo khóa học thành công."));
        }

        [HttpPut("Sua/{id}")]
        [HttpPut("{id}")]
        public IActionResult Sua(int id, [FromBody] KhoaHocModel model)
        {
            int mgv = LayMaGiangVien();
            bool isAdmin = User.IsInRole("Admin");
            model.MaKhoaHoc = id;
            bool ok = _bll.Update(model, mgv, isAdmin);
            if (!ok) return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy khóa học."));
            return Ok(ApiResponse<object>.Ok(null, "Cập nhật khóa học thành công."));
        }

        /// <summary>Luồng 5: Giảng viên gửi khóa học chờ Admin phê duyệt</summary>
        [HttpPost("GuiDuyet/{id}")]
        [HttpPost("{id}/submit-approval")]
        public IActionResult GuiDuyet(int id)
        {
            int mgv = LayMaGiangVien();
            bool ok = _bll.GuiDuyet(id, mgv);
            if (!ok) return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy khóa học."));
            return Ok(ApiResponse<object>.Ok(null, "Đã gửi khóa học lên chờ Quản trị viên phê duyệt."));
        }

        [HttpDelete("Xoa/{id}")]
        [HttpDelete("{id}")]
        public IActionResult Xoa(int id)
        {
            int mgv = LayMaGiangVien();
            bool isAdmin = User.IsInRole("Admin");
            bool ok = _bll.Delete(id, mgv, isAdmin);
            if (!ok) return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy khóa học."));
            return Ok(ApiResponse<object>.Ok(null, "Xóa khóa học thành công."));
        }
    }

    [Route("api/[controller]")]
    [Route("api/v1/instructor/lessons")]
    [ApiController]
    [Authorize(Roles = "Giảng viên,Admin")]
    public class BaiGiangController : ControllerBase
    {
        private readonly BaiGiangBLL _bll;
        private readonly GiangVienBLL _giangVienBLL;

        public BaiGiangController(BaiGiangBLL bll, GiangVienBLL giangVienBLL)
        {
            _bll = bll;
            _giangVienBLL = giangVienBLL;
        }

        private int LayMaGiangVien()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int uid))
            {
                var gv = _giangVienBLL.GetByMaNguoiDung(uid);
                return gv?.MaGiangVien ?? 0;
            }
            return 0;
        }

        [HttpGet("LayTheoKhoaHoc/{maKhoaHoc}")]
        [AllowAnonymous]
        public IActionResult LayTheoKhoaHoc(int maKhoaHoc, [FromQuery] int? maDangKy = null)
        {
            var list = _bll.GetByKhoaHoc(maKhoaHoc, maDangKy);
            return Ok(ApiResponse<object>.Ok(list));
        }

        [HttpPost("Them")]
        [HttpPost]
        public IActionResult Them([FromBody] BaiGiangModel model)
        {
            int mgv = LayMaGiangVien();
            int id = _bll.Insert(model, mgv, isAdmin: User.IsInRole("Admin"));
            return Ok(ApiResponse<object>.Ok(new { MaBaiGiang = id }, "Thêm bài giảng thành công."));
        }

        [HttpPut("Sua/{id}")]
        [HttpPut("{id}")]
        public IActionResult Sua(int id, [FromBody] BaiGiangModel model)
        {
            int mgv = LayMaGiangVien();
            model.MaBaiGiang = id;
            bool ok = _bll.Update(model, mgv, isAdmin: User.IsInRole("Admin"));
            if (!ok) return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy bài giảng."));
            return Ok(ApiResponse<object>.Ok(null, "Cập nhật bài giảng thành công."));
        }

        [HttpDelete("Xoa/{id}")]
        [HttpDelete("{id}")]
        public IActionResult Xoa(int id)
        {
            int mgv = LayMaGiangVien();
            bool ok = _bll.Delete(id, mgv, isAdmin: User.IsInRole("Admin"));
            if (!ok) return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy bài giảng."));
            return Ok(ApiResponse<object>.Ok(null, "Xóa bài giảng thành công."));
        }

        /// <summary>Tải lên tệp tài liệu bài giảng (PDF, Video, Slide) an toàn (Bài 16 Giáo trình)</summary>
        [HttpPost("TaiLenTep")]
        [HttpPost("upload")]
        [RequestSizeLimit(100 * 1024 * 1024)] // Tối đa 100MB
        public async Task<IActionResult> TaiLenTep(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<object>.Fail(400, "Vui lòng chọn tệp để tải lên."));

            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            string[] allowedExts = { ".pdf", ".mp4", ".zip", ".docx", ".pptx", ".png", ".jpg" };
            if (Array.IndexOf(allowedExts, ext) < 0)
                return BadRequest(ApiResponse<object>.Fail(400, $"Định dạng tệp {ext} không được hỗ trợ. Chỉ cho phép: {string.Join(", ", allowedExts)}"));

            string uploadsDir = Path.Combine(AppContext.BaseDirectory, "Uploads", "Lessons");
            Directory.CreateDirectory(uploadsDir);

            string safeFileName = $"{Guid.NewGuid():N}{ext}";
            string filePath = Path.Combine(uploadsDir, safeFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string relativeUrl = $"/uploads/lessons/{safeFileName}";
            return Ok(ApiResponse<object>.Ok(new { FileUrl = relativeUrl, FileName = file.FileName, FileSize = file.Length }, "Tải tệp lên thành công."));
        }
    }

    [Route("api/[controller]")]
    [Route("api/v1/instructor/quizzes")]
    [ApiController]
    [Authorize(Roles = "Giảng viên,Admin")]
    public class BaiKiemTraController : ControllerBase
    {
        private readonly BaiKiemTraBLL _bll;
        private readonly GiangVienBLL _giangVienBLL;

        public BaiKiemTraController(BaiKiemTraBLL bll, GiangVienBLL giangVienBLL)
        {
            _bll = bll;
            _giangVienBLL = giangVienBLL;
        }

        private int LayMaGiangVien()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int uid))
            {
                var gv = _giangVienBLL.GetByMaNguoiDung(uid);
                return gv?.MaGiangVien ?? 0;
            }
            return 0;
        }

        [HttpGet("LayTheoKhoaHoc/{maKhoaHoc}")]
        public IActionResult LayTheoKhoaHoc(int maKhoaHoc)
        {
            var list = _bll.GetByKhoaHoc(maKhoaHoc);
            return Ok(ApiResponse<object>.Ok(list));
        }

        [HttpGet("LayCauHoi/{maBaiKiemTra}")]
        [HttpGet("{maBaiKiemTra}/questions")]
        public IActionResult LayCauHoi(int maBaiKiemTra)
        {
            var list = _bll.GetCauHoiByBaiKiemTra(maBaiKiemTra);
            return Ok(ApiResponse<object>.Ok(list));
        }

        [HttpPost("ThemCauHoi")]
        [HttpPost("questions")]
        public IActionResult ThemCauHoi([FromBody] CauHoiModel model)
        {
            int mgv = LayMaGiangVien();
            int id = _bll.ThemCauHoi(model, mgv, isAdmin: User.IsInRole("Admin"));
            return Ok(ApiResponse<object>.Ok(new { MaCauHoi = id }, "Thêm câu hỏi vào ngân hàng đề thành công."));
        }

        [HttpPost("TaoBaiKiemTra")]
        [HttpPost]
        public IActionResult TaoBaiKiemTra([FromBody] BaiKiemTraModel model)
        {
            int mgv = LayMaGiangVien();
            int id = _bll.Insert(model, mgv, isAdmin: User.IsInRole("Admin"));
            return Ok(ApiResponse<object>.Ok(new { MaBaiKiemTra = id }, "Tạo bài kiểm tra thành công."));
        }

        [HttpPut("Sua/{id}")]
        [HttpPut("{id}")]
        public IActionResult Sua(int id, [FromBody] BaiKiemTraModel model)
        {
            int mgv = LayMaGiangVien();
            model.MaBaiKiemTra = id;
            bool ok = _bll.Update(model, mgv, isAdmin: User.IsInRole("Admin"));
            if (!ok) return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy bài kiểm tra."));
            return Ok(ApiResponse<object>.Ok(null, "Cập nhật bài kiểm tra thành công."));
        }

        [HttpDelete("Xoa/{id}")]
        [HttpDelete("{id}")]
        public IActionResult Xoa(int id)
        {
            int mgv = LayMaGiangVien();
            bool ok = _bll.Delete(id, mgv, isAdmin: User.IsInRole("Admin"));
            if (!ok) return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy bài kiểm tra."));
            return Ok(ApiResponse<object>.Ok(null, "Xóa bài kiểm tra thành công."));
        }
    }

    [Route("api/[controller]")]
    [Route("api/v1/instructor/gradebook")]
    [ApiController]
    [Authorize(Roles = "Giảng viên,Admin")]
    public class BangDiemController : ControllerBase
    {
        private readonly KetQuaKiemTraBLL _bll;
        private readonly KhoaHocBLL _khoaHocBLL;
        private readonly GiangVienBLL _giangVienBLL;
        private readonly IExportService _exportService;

        public BangDiemController(
            KetQuaKiemTraBLL bll,
            KhoaHocBLL khoaHocBLL,
            GiangVienBLL giangVienBLL,
            IExportService exportService)
        {
            _bll = bll;
            _khoaHocBLL = khoaHocBLL;
            _giangVienBLL = giangVienBLL;
            _exportService = exportService;
        }

        private int LayMaGiangVien()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int uid))
            {
                var gv = _giangVienBLL.GetByMaNguoiDung(uid);
                return gv?.MaGiangVien ?? 0;
            }
            return 0;
        }

        [HttpGet("LayKetQuaTheoKhoaHoc/{maKhoaHoc}")]
        public IActionResult LayKetQuaTheoKhoaHoc(int maKhoaHoc, [FromQuery] int trang = 1, [FromQuery] int kichThuocTrang = 20)
        {
            bool isAdmin = User.IsInRole("Admin");
            int mgv = LayMaGiangVien();
            var result = _bll.GetByKhoaHoc(maKhoaHoc, trang, kichThuocTrang, mgv, isAdmin);
            return Ok(ApiResponse<PagedResult<KetQuaKiemTraModel>>.Ok(result));
        }

        /// <summary>Xuất bảng điểm sinh viên theo từng khóa học ra file Excel (.xlsx)</summary>
        [HttpGet("XuatExcel/{maKhoaHoc}")]
        [HttpGet("{maKhoaHoc}/excel")]
        public IActionResult XuatExcel(int maKhoaHoc)
        {
            bool isAdmin = User.IsInRole("Admin");
            int mgv = LayMaGiangVien();
            var kh = _khoaHocBLL.GetById(maKhoaHoc);
            if (kh == null) return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy khóa học."));

            var results = _bll.GetByKhoaHoc(maKhoaHoc, 1, 1000, mgv, isAdmin);
            byte[] fileBytes = _exportService.GenerateGradebookExcel(kh.TenKhoaHoc, results.Items);
            string fileName = $"BangDiem_{kh.MaKhoaHoc}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
