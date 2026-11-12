using System;
using System.Security.Claims;
using BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace API_SinhVien.Controllers
{
    [Route("api/[controller]")]
    [Route("api/v1/courses")]
    [ApiController]
    [Authorize(Roles = "Học viên,Giảng viên,Admin")]
    public class KhoaHocController : ControllerBase
    {
        private readonly KhoaHocBLL _bll;

        public KhoaHocController(KhoaHocBLL bll)
        {
            _bll = bll;
        }

        [HttpGet("TraCuu")]
        [HttpGet]
        [AllowAnonymous]
        public IActionResult TraCuu(
            [FromQuery] int? maDanhMuc,
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

            var result = _bll.GetAll(maDanhMuc, null, actualKeyword, actualPage, actualSize, "Đã duyệt", sortBy, sortOrder);
            return Ok(ApiResponse<PagedResult<KhoaHocModel>>.Ok(result));
        }

        [HttpGet("ChiTiet/{id}")]
        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult ChiTiet(int id)
        {
            var item = _bll.GetById(id);
            if (item == null)
                return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy khóa học."));

            return Ok(ApiResponse<KhoaHocModel>.Ok(item));
        }
    }

    [Route("api/[controller]")]
    [Route("api/v1/lessons")]
    [ApiController]
    [Authorize(Roles = "Học viên,Giảng viên,Admin")]
    public class BaiGiangController : ControllerBase
    {
        private readonly BaiGiangBLL _bll;

        public BaiGiangController(BaiGiangBLL bll)
        {
            _bll = bll;
        }

        [HttpGet("LayDanhSach/{maKhoaHoc}")]
        [AllowAnonymous]
        public IActionResult LayDanhSach(int maKhoaHoc, [FromQuery] int? maDangKy = null)
        {
            var list = _bll.GetByKhoaHoc(maKhoaHoc, maDangKy);
            return Ok(ApiResponse<object>.Ok(list));
        }
    }

    [Route("api/[controller]")]
    [Route("api/v1/enrollments")]
    [ApiController]
    [Authorize(Roles = "Học viên")]
    public class DangKyKhoaHocController : ControllerBase
    {
        private readonly DangKyKhoaHocBLL _bll;
        private readonly SinhVienBLL _sinhVienBLL;

        public DangKyKhoaHocController(DangKyKhoaHocBLL bll, SinhVienBLL sinhVienBLL)
        {
            _bll = bll;
            _sinhVienBLL = sinhVienBLL;
        }

        private int LayMaSinhVien()
        {
            var svClaim = User.FindFirst("MaSinhVien")?.Value;
            if (int.TryParse(svClaim, out int msv) && msv > 0) return msv;

            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int uid))
            {
                var sv = _sinhVienBLL.GetByMaNguoiDung(uid);
                return sv?.MaSinhVien ?? 0;
            }
            return 0;
        }

        [HttpGet("KhoaHocCuaToi")]
        [HttpGet("my-courses")]
        public IActionResult KhoaHocCuaToi([FromQuery] int trang = 1, [FromQuery] int kichThuocTrang = 20)
        {
            int msv = LayMaSinhVien();
            var result = _bll.GetBySinhVien(msv, trang, kichThuocTrang);
            return Ok(ApiResponse<PagedResult<DangKyKhoaHocModel>>.Ok(result));
        }

        [HttpPost("GhiDanh/{maKhoaHoc}")]
        [HttpPost("{maKhoaHoc}")]
        public IActionResult GhiDanh(int maKhoaHoc)
        {
            int msv = LayMaSinhVien();
            if (msv <= 0)
                return Unauthorized(ApiResponse<object>.Fail(401, "Không tìm thấy thông tin sinh viên của bạn."));

            int id = _bll.DangKy(msv, maKhoaHoc);
            return Ok(ApiResponse<object>.Ok(new { MaDangKy = id }, "Đăng ký khóa học thành công."));
        }

        public class CapNhatTienDoRequest
        {
            public decimal TienDoPhanTram { get; set; }
        }

        [HttpPut("CapNhatTienDo/{maDangKy}")]
        public IActionResult CapNhatTienDo(int maDangKy, [FromBody] CapNhatTienDoRequest request)
        {
            int msv = LayMaSinhVien();
            bool ok = _bll.CapNhatTienDo(maDangKy, request.TienDoPhanTram, msv);
            if (!ok) return BadRequest(ApiResponse<object>.Fail(400, "Cập nhật tiến độ thất bại."));
            return Ok(ApiResponse<object>.Ok(null, "Cập nhật tiến độ học tập thành công."));
        }

        /// <summary>Luồng 1: Học tuần tự (Prerequisites and Drip Content) - Hoàn thành bài giảng để mở khóa bài kế tiếp</summary>
        [HttpPost("HoanThanhBaiGiang")]
        [HttpPost("complete-lesson")]
        public IActionResult HoanThanhBaiGiang([FromBody] HoanThanhBaiGiangRequest request)
        {
            int msv = LayMaSinhVien();
            var updated = _bll.HoanThanhBaiGiangTuanTu(request.MaDangKy, request.MaBaiGiang, msv, request.ThoiLuongDaXem);
            return Ok(ApiResponse<DangKyKhoaHocModel>.Ok(updated, "Đã hoàn thành bài giảng và cập nhật tiến độ khóa học."));
        }
    }

    [Route("api/[controller]")]
    [Route("api/v1/quizzes")]
    [ApiController]
    [Authorize(Roles = "Học viên")]
    public class BaiKiemTraController : ControllerBase
    {
        private readonly BaiKiemTraBLL _bll;
        private readonly KetQuaKiemTraBLL _ketQuaBLL;
        private readonly SinhVienBLL _sinhVienBLL;

        public BaiKiemTraController(BaiKiemTraBLL bll, KetQuaKiemTraBLL ketQuaBLL, SinhVienBLL sinhVienBLL)
        {
            _bll = bll;
            _ketQuaBLL = ketQuaBLL;
            _sinhVienBLL = sinhVienBLL;
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

        [HttpGet("LayDanhSach/{maKhoaHoc}")]
        public IActionResult LayDanhSach(int maKhoaHoc)
        {
            var list = _bll.GetByKhoaHoc(maKhoaHoc);
            return Ok(ApiResponse<object>.Ok(list));
        }

        [HttpGet("ChiTiet/{id}")]
        [HttpGet("{id}")]
        public IActionResult ChiTiet(int id)
        {
            var item = _bll.GetById(id);
            if (item == null)
                return NotFound(ApiResponse<object>.Fail(404, "Không tìm thấy bài kiểm tra."));

            return Ok(ApiResponse<BaiKiemTraModel>.Ok(item));
        }

        /// <summary>Luồng 2: Bắt đầu phiên làm bài thi trắc nghiệm chống gian lận (trộn câu hỏi và đồng hồ phía Server)</summary>
        [HttpPost("BatDau/{maBaiKiemTra}")]
        [HttpPost("{maBaiKiemTra}/start")]
        public IActionResult BatDau(int maBaiKiemTra, [FromQuery] int maDangKy)
        {
            int msv = LayMaSinhVien();
            var session = _ketQuaBLL.BatDauPhienLamBai(maDangKy, maBaiKiemTra, msv);
            return Ok(ApiResponse<BatDauKiemTraResponse>.Ok(session, "Bắt đầu phiên làm bài kiểm tra thành công."));
        }
    }

    [Route("api/[controller]")]
    [Route("api/v1/quiz-submissions")]
    [ApiController]
    [Authorize(Roles = "Học viên")]
    public class KetQuaKiemTraController : ControllerBase
    {
        private readonly KetQuaKiemTraBLL _bll;
        private readonly SinhVienBLL _sinhVienBLL;

        public KetQuaKiemTraController(KetQuaKiemTraBLL bll, SinhVienBLL sinhVienBLL)
        {
            _bll = bll;
            _sinhVienBLL = sinhVienBLL;
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

        [HttpGet("LichSuLamBai/{maDangKy}")]
        public IActionResult LichSuLamBai(int maDangKy)
        {
            int msv = LayMaSinhVien();
            var list = _bll.GetByDangKy(maDangKy, msv);
            return Ok(ApiResponse<object>.Ok(list));
        }

        [HttpPost("NopBai")]
        [HttpPost]
        public IActionResult NopBai([FromBody] NopBaiKiemTraRequest request)
        {
            int msv = LayMaSinhVien();
            int id = _bll.NopBaiChongGianLan(request, msv);
            var kq = _bll.GetById(id);
            return Ok(ApiResponse<KetQuaKiemTraModel>.Ok(kq, "Nộp bài kiểm tra thành công."));
        }
    }
}
