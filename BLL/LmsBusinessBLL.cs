using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BLL.BackgroundJobs;
using BLL.Caching;
using DAL;
using Models;

namespace BLL
{
    public class DanhMucBLL
    {
        private const string CategoriesCacheKey = "lms:categories:all";
        private readonly IDanhMucRepository _dal;
        private readonly ICacheService _cache;

        public DanhMucBLL(IDanhMucRepository? dal = null, ICacheService? cache = null)
        {
            _dal = dal ?? new DanhMucDAL();
            _cache = cache ?? HybridCacheService.Shared;
        }

        public List<DanhMucModel> GetAll()
        {
            return _cache.GetOrCreate(CategoriesCacheKey, () => _dal.GetAll(), TimeSpan.FromMinutes(30))
                ?? new List<DanhMucModel>();
        }

        public DanhMucModel? GetById(int id)
        {
            if (id <= 0) return null;
            return _dal.GetById(id);
        }

        public int Insert(DanhMucModel model)
        {
            if (string.IsNullOrWhiteSpace(model.TenDanhMuc))
                throw new ArgumentException("Tên danh mục không được để trống.");

            int id = _dal.Insert(model);
            _cache.Remove(CategoriesCacheKey);
            return id;
        }

        public bool Update(DanhMucModel model)
        {
            if (model.MaDanhMuc <= 0)
                throw new ArgumentException("Mã danh mục không hợp lệ.");
            if (string.IsNullOrWhiteSpace(model.TenDanhMuc))
                throw new ArgumentException("Tên danh mục không được để trống.");

            bool ok = _dal.Update(model);
            if (ok) _cache.Remove(CategoriesCacheKey);
            return ok;
        }

        public bool Delete(int id)
        {
            if (id <= 0) return false;
            bool ok = _dal.Delete(id);
            if (ok) _cache.Remove(CategoriesCacheKey);
            return ok;
        }
    }

    public class KhoaHocBLL
    {
        private const string CourseCachePrefix = "lms:courses:";
        private readonly IKhoaHocRepository _dal;
        private readonly ICacheService _cache;

        public KhoaHocBLL(IKhoaHocRepository? dal = null, ICacheService? cache = null)
        {
            _dal = dal ?? new KhoaHocDAL();
            _cache = cache ?? HybridCacheService.Shared;
        }

        public PagedResult<KhoaHocModel> GetAll(
            int? maDanhMuc,
            int? maGiangVien,
            string? tuKhoa,
            int trang = 1,
            int kichThuocTrang = 20,
            string? trangThaiDuyet = null,
            string? sortBy = null,
            string? sortOrder = null)
        {
            if (trang < 1) trang = 1;
            if (kichThuocTrang < 1) kichThuocTrang = 20;
            if (kichThuocTrang > 100) kichThuocTrang = 100;

            string cacheKey = $"{CourseCachePrefix}dm={maDanhMuc}:gv={maGiangVien}:kw={tuKhoa}:st={trangThaiDuyet}:sb={sortBy}:so={sortOrder}:p={trang}:s={kichThuocTrang}";
            return _cache.GetOrCreate(
                cacheKey,
                () => _dal.GetAll(maDanhMuc, maGiangVien, tuKhoa, trang, kichThuocTrang, trangThaiDuyet, sortBy, sortOrder),
                TimeSpan.FromMinutes(10)) ?? new PagedResult<KhoaHocModel>();
        }

        public KhoaHocModel? GetById(int id)
        {
            if (id <= 0) return null;
            return _dal.GetById(id);
        }

        public int Insert(KhoaHocModel model)
        {
            if (string.IsNullOrWhiteSpace(model.TenKhoaHoc))
                throw new ArgumentException("Tên khóa học không được để trống.");
            if (model.MaDanhMuc <= 0)
                throw new ArgumentException("Danh mục không hợp lệ.");
            if (model.MaGiangVien <= 0)
                throw new ArgumentException("Giảng viên không hợp lệ.");
            if (model.Gia < 0)
                throw new ArgumentException("Giá khóa học không thể âm.");

            int id = _dal.Insert(model);
            InvalidateCourseCaches();
            return id;
        }

        public bool Update(KhoaHocModel model, int maGiangVienYeuCau, bool isAdmin = false)
        {
            if (model.MaKhoaHoc <= 0)
                throw new ArgumentException("Mã khóa học không hợp lệ.");
            if (string.IsNullOrWhiteSpace(model.TenKhoaHoc))
                throw new ArgumentException("Tên khóa học không được để trống.");
            if (model.Gia < 0)
                throw new ArgumentException("Giá khóa học không thể âm.");

            var current = _dal.GetById(model.MaKhoaHoc);
            if (current == null)
                throw new InvalidOperationException("Khóa học không tồn tại.");

            if (!isAdmin && current.MaGiangVien != maGiangVienYeuCau)
                throw new InvalidOperationException("Bạn không có quyền chỉnh sửa khóa học của giảng viên khác.");

            bool ok = _dal.Update(model);
            if (ok) InvalidateCourseCaches();
            return ok;
        }

        public bool Delete(int id, int maGiangVienYeuCau, bool isAdmin = false)
        {
            if (id <= 0) return false;
            var current = _dal.GetById(id);
            if (current == null) return false;

            if (!isAdmin && current.MaGiangVien != maGiangVienYeuCau)
                throw new InvalidOperationException("Bạn không có quyền xóa khóa học của giảng viên khác.");

            bool ok = _dal.Delete(id);
            if (ok) InvalidateCourseCaches();
            return ok;
        }

        /// <summary>Luồng 5: Giảng viên gửi khóa học lên chờ Admin phê duyệt</summary>
        public bool GuiDuyet(int maKhoaHoc, int maGiangVienYeuCau)
        {
            var current = _dal.GetById(maKhoaHoc);
            if (current == null)
                throw new InvalidOperationException("Khóa học không tồn tại.");
            if (current.MaGiangVien != maGiangVienYeuCau)
                throw new InvalidOperationException("Bạn không có quyền gửi duyệt khóa học của giảng viên khác.");

            bool ok = _dal.UpdateTrangThaiDuyet(maKhoaHoc, "Chờ duyệt", null, maGiangVienYeuCau);
            if (ok) InvalidateCourseCaches();
            return ok;
        }

        /// <summary>Luồng 5: Admin phê duyệt hoặc từ chối khóa học</summary>
        public bool PheDuyet(int maKhoaHoc, string trangThaiDuyet, string? lyDoTuChoi, int maAdmin)
        {
            if (trangThaiDuyet != "Đã duyệt" && trangThaiDuyet != "Từ chối")
                throw new ArgumentException("Trạng thái phê duyệt chỉ nhận 'Đã duyệt' hoặc 'Từ chối'.");
            if (trangThaiDuyet == "Từ chối" && string.IsNullOrWhiteSpace(lyDoTuChoi))
                throw new ArgumentException("Vui lòng nhập lý do từ chối khóa học.");

            bool ok = _dal.UpdateTrangThaiDuyet(maKhoaHoc, trangThaiDuyet, lyDoTuChoi?.Trim(), maAdmin);
            if (ok) InvalidateCourseCaches();
            return ok;
        }

        private void InvalidateCourseCaches()
        {
            _cache.RemoveByPrefix(CourseCachePrefix);
            _cache.Remove("lms:reports:overview");
        }
    }

    public class BaiGiangBLL
    {
        private readonly IBaiGiangRepository _dal;
        private readonly IKhoaHocRepository _khoaHocDAL;
        private readonly ITienDoBaiGiangRepository _tienDoDAL;

        public BaiGiangBLL(
            IBaiGiangRepository? dal = null,
            IKhoaHocRepository? khoaHocDAL = null,
            ITienDoBaiGiangRepository? tienDoDAL = null)
        {
            _dal = dal ?? new BaiGiangDAL();
            _khoaHocDAL = khoaHocDAL ?? new KhoaHocDAL();
            _tienDoDAL = tienDoDAL ?? new TienDoBaiGiangDAL();
        }

        public List<BaiGiangModel> GetByKhoaHoc(int maKhoaHoc, int? maDangKy = null)
        {
            if (maKhoaHoc <= 0) return new List<BaiGiangModel>();
            var lessons = _dal.GetByKhoaHoc(maKhoaHoc);

            if (!maDangKy.HasValue || maDangKy.Value <= 0 || lessons.Count == 0)
                return lessons;

            var completedIds = _tienDoDAL.GetCompletedLessonIds(maDangKy.Value);
            bool previousCompleted = true;

            foreach (var lesson in lessons.OrderBy(x => x.SoThuTu))
            {
                lesson.DaHoanThanh = completedIds.Contains(lesson.MaBaiGiang);
                if (lesson.BaiGiangTienQuyetId.HasValue)
                {
                    lesson.DuocMoKhoa = completedIds.Contains(lesson.BaiGiangTienQuyetId.Value);
                }
                else
                {
                    lesson.DuocMoKhoa = previousCompleted;
                }
                previousCompleted = lesson.DaHoanThanh;
            }

            return lessons;
        }

        public BaiGiangModel? GetById(int id)
        {
            if (id <= 0) return null;
            return _dal.GetById(id);
        }

        public int Insert(BaiGiangModel model, int maGiangVienYeuCau, bool isAdmin = false)
        {
            if (model.MaKhoaHoc <= 0)
                throw new ArgumentException("Mã khóa học không hợp lệ.");
            if (string.IsNullOrWhiteSpace(model.TieuDe))
                throw new ArgumentException("Tiêu đề bài giảng không được để trống.");
            if (string.IsNullOrWhiteSpace(model.URL))
                throw new ArgumentException("URL tài liệu/video không được để trống.");

            var kh = _khoaHocDAL.GetById(model.MaKhoaHoc);
            if (kh == null)
                throw new InvalidOperationException("Khóa học không tồn tại.");

            if (!isAdmin && kh.MaGiangVien != maGiangVienYeuCau)
                throw new InvalidOperationException("Bạn không có quyền thêm bài giảng vào khóa học này.");

            return _dal.Insert(model);
        }

        public bool Update(BaiGiangModel model, int maGiangVienYeuCau, bool isAdmin = false)
        {
            if (model.MaBaiGiang <= 0)
                throw new ArgumentException("Mã bài giảng không hợp lệ.");
            if (string.IsNullOrWhiteSpace(model.TieuDe))
                throw new ArgumentException("Tiêu đề bài giảng không được để trống.");

            var current = _dal.GetById(model.MaBaiGiang);
            if (current == null)
                throw new InvalidOperationException("Bài giảng không tồn tại.");

            var kh = _khoaHocDAL.GetById(current.MaKhoaHoc);
            if (kh == null || (!isAdmin && kh.MaGiangVien != maGiangVienYeuCau))
                throw new InvalidOperationException("Bạn không có quyền chỉnh sửa bài giảng của khóa học này.");

            return _dal.Update(model);
        }

        public bool Delete(int id, int maGiangVienYeuCau, bool isAdmin = false)
        {
            if (id <= 0) return false;
            var current = _dal.GetById(id);
            if (current == null) return false;

            var kh = _khoaHocDAL.GetById(current.MaKhoaHoc);
            if (kh == null || (!isAdmin && kh.MaGiangVien != maGiangVienYeuCau))
                throw new InvalidOperationException("Bạn không có quyền xóa bài giảng của khóa học này.");

            return _dal.Delete(id);
        }
    }

    public class DangKyKhoaHocBLL
    {
        private readonly IDangKyKhoaHocRepository _dal;
        private readonly IKhoaHocRepository _khoaHocDAL;
        private readonly ITienDoBaiGiangRepository _tienDoDAL;
        private readonly ICacheService _cache;

        public DangKyKhoaHocBLL(
            IDangKyKhoaHocRepository? dal = null,
            IKhoaHocRepository? khoaHocDAL = null,
            ITienDoBaiGiangRepository? tienDoDAL = null,
            ICacheService? cache = null)
        {
            _dal = dal ?? new DangKyKhoaHocDAL();
            _khoaHocDAL = khoaHocDAL ?? new KhoaHocDAL();
            _tienDoDAL = tienDoDAL ?? new TienDoBaiGiangDAL();
            _cache = cache ?? HybridCacheService.Shared;
        }

        public PagedResult<DangKyKhoaHocModel> GetBySinhVien(int maSinhVien, int trang = 1, int kichThuocTrang = 20)
        {
            if (trang < 1) trang = 1;
            return _dal.GetBySinhVien(maSinhVien, trang, kichThuocTrang);
        }

        public PagedResult<DangKyKhoaHocModel> GetByKhoaHoc(int maKhoaHoc, int trang = 1, int kichThuocTrang = 20)
        {
            if (trang < 1) trang = 1;
            return _dal.GetByKhoaHoc(maKhoaHoc, trang, kichThuocTrang);
        }

        public DangKyKhoaHocModel? GetById(int id) => _dal.GetById(id);

        public int DangKy(int maSinhVien, int maKhoaHoc)
        {
            if (maSinhVien <= 0)
                throw new ArgumentException("Mã sinh viên không hợp lệ.");
            if (maKhoaHoc <= 0)
                throw new ArgumentException("Mã khóa học không hợp lệ.");

            var kh = _khoaHocDAL.GetById(maKhoaHoc);
            if (kh == null || !kh.TrangThai)
                throw new InvalidOperationException("Khóa học không khả dụng hoặc đã bị đóng.");

            var existing = _dal.GetBySinhVienAndKhoaHoc(maSinhVien, maKhoaHoc);
            if (existing != null)
                throw new InvalidOperationException("Sinh viên đã đăng ký khóa học này trước đó.");

            var model = new DangKyKhoaHocModel
            {
                MaSinhVien = maSinhVien,
                MaKhoaHoc = maKhoaHoc,
                TienDoPhanTram = 0,
                TrangThai = "Đang học"
            };
            int id = _dal.Insert(model);
            _cache.Remove("lms:reports:overview");
            return id;
        }

        public bool CapNhatTienDo(int maDangKy, decimal tienDo, int maSinhVien)
        {
            if (maDangKy <= 0) throw new ArgumentException("Mã đăng ký không hợp lệ.");
            if (tienDo < 0 || tienDo > 100) throw new ArgumentException("Tiến độ phải nằm trong khoảng 0 đến 100%.");

            var dk = _dal.GetById(maDangKy);
            if (dk == null) throw new InvalidOperationException("Thông tin đăng ký không tồn tại.");
            if (dk.MaSinhVien != maSinhVien) throw new InvalidOperationException("Bạn không thể cập nhật tiến độ của học viên khác.");

            bool ok = _dal.UpdateTienDo(maDangKy, tienDo);
            if (ok) _cache.Remove("lms:reports:overview");
            return ok;
        }

        /// <summary>Luồng 1: Học tuần tự (Prerequisites & Drip Content) - Hoàn thành bài giảng theo đúng thứ tự</summary>
        public DangKyKhoaHocModel HoanThanhBaiGiangTuanTu(int maDangKy, int maBaiGiang, int maSinhVien, int thoiLuongDaXem = 0)
        {
            if (maDangKy <= 0 || maBaiGiang <= 0)
                throw new ArgumentException("Thông tin bài giảng hoặc đăng ký không hợp lệ.");

            var updated = _tienDoDAL.HoanThanhBaiGiangTuanTu(maDangKy, maBaiGiang, maSinhVien, thoiLuongDaXem);
            _cache.Remove("lms:reports:overview");
            return updated;
        }
    }

    public class BaiKiemTraBLL
    {
        private readonly IBaiKiemTraRepository _dal;
        private readonly IKhoaHocRepository _khoaHocDAL;
        private readonly ICauHoiRepository _cauHoiDAL;

        public BaiKiemTraBLL(
            IBaiKiemTraRepository? dal = null,
            IKhoaHocRepository? khoaHocDAL = null,
            ICauHoiRepository? cauHoiDAL = null)
        {
            _dal = dal ?? new BaiKiemTraDAL();
            _khoaHocDAL = khoaHocDAL ?? new KhoaHocDAL();
            _cauHoiDAL = cauHoiDAL ?? new CauHoiDAL();
        }

        public List<BaiKiemTraModel> GetByKhoaHoc(int maKhoaHoc) => _dal.GetByKhoaHoc(maKhoaHoc);

        public BaiKiemTraModel? GetById(int id) => _dal.GetById(id);

        public List<CauHoiModel> GetCauHoiByBaiKiemTra(int maBaiKiemTra) => _cauHoiDAL.GetByBaiKiemTra(maBaiKiemTra);

        public int ThemCauHoi(CauHoiModel model, int maGiangVienYeuCau, bool isAdmin = false)
        {
            if (model.MaBaiKiemTra <= 0) throw new ArgumentException("Mã bài kiểm tra không hợp lệ.");
            if (string.IsNullOrWhiteSpace(model.NoiDung)) throw new ArgumentException("Nội dung câu hỏi không được để trống.");
            if (model.DanhSachLuaChon == null || model.DanhSachLuaChon.Count < 2)
                throw new ArgumentException("Câu hỏi trắc nghiệm phải có ít nhất 2 lựa chọn.");
            if (!model.DanhSachLuaChon.Any(x => x.LaDapAnDung))
                throw new ArgumentException("Phải có ít nhất 1 lựa chọn là đáp án đúng.");

            var bkt = _dal.GetById(model.MaBaiKiemTra);
            if (bkt == null) throw new InvalidOperationException("Bài kiểm tra không tồn tại.");

            var kh = _khoaHocDAL.GetById(bkt.MaKhoaHoc);
            if (kh == null || (!isAdmin && kh.MaGiangVien != maGiangVienYeuCau))
                throw new InvalidOperationException("Bạn không có quyền thêm câu hỏi vào bài kiểm tra của giảng viên khác.");

            return _cauHoiDAL.InsertWithOptions(model);
        }

        public int Insert(BaiKiemTraModel model, int maGiangVienYeuCau, bool isAdmin = false)
        {
            if (model.MaKhoaHoc <= 0) throw new ArgumentException("Mã khóa học không hợp lệ.");
            if (string.IsNullOrWhiteSpace(model.TieuDe)) throw new ArgumentException("Tiêu đề bài kiểm tra không được để trống.");
            if (model.ThoiGianLamBai <= 0) throw new ArgumentException("Thời gian làm bài phải lớn hơn 0 phút.");
            if (model.DiemToiDa <= 0) throw new ArgumentException("Điểm tối đa phải lớn hơn 0.");

            var kh = _khoaHocDAL.GetById(model.MaKhoaHoc);
            if (kh == null) throw new InvalidOperationException("Khóa học không tồn tại.");
            if (!isAdmin && kh.MaGiangVien != maGiangVienYeuCau)
                throw new InvalidOperationException("Bạn không có quyền thêm bài kiểm tra vào khóa học này.");

            return _dal.Insert(model);
        }

        public bool Update(BaiKiemTraModel model, int maGiangVienYeuCau, bool isAdmin = false)
        {
            if (model.MaBaiKiemTra <= 0) throw new ArgumentException("Mã bài kiểm tra không hợp lệ.");
            var current = _dal.GetById(model.MaBaiKiemTra);
            if (current == null) throw new InvalidOperationException("Bài kiểm tra không tồn tại.");

            var kh = _khoaHocDAL.GetById(current.MaKhoaHoc);
            if (kh == null || (!isAdmin && kh.MaGiangVien != maGiangVienYeuCau))
                throw new InvalidOperationException("Bạn không có quyền sửa bài kiểm tra của khóa học này.");

            return _dal.Update(model);
        }

        public bool Delete(int id, int maGiangVienYeuCau, bool isAdmin = false)
        {
            if (id <= 0) return false;
            var current = _dal.GetById(id);
            if (current == null) return false;

            var kh = _khoaHocDAL.GetById(current.MaKhoaHoc);
            if (kh == null || (!isAdmin && kh.MaGiangVien != maGiangVienYeuCau))
                throw new InvalidOperationException("Bạn không có quyền xóa bài kiểm tra của khóa học này.");

            return _dal.Delete(id);
        }
    }

    public class KetQuaKiemTraBLL
    {
        private readonly IKetQuaKiemTraRepository _dal;
        private readonly IBaiKiemTraRepository _baiKiemTraDAL;
        private readonly IDangKyKhoaHocRepository _dangKyDAL;
        private readonly IKhoaHocRepository _khoaHocDAL;
        private readonly ICauHoiRepository _cauHoiDAL;
        private readonly IPhienLamBaiRepository _phienDAL;

        public KetQuaKiemTraBLL(
            IKetQuaKiemTraRepository? dal = null,
            IBaiKiemTraRepository? baiKiemTraDAL = null,
            IDangKyKhoaHocRepository? dangKyDAL = null,
            IKhoaHocRepository? khoaHocDAL = null,
            ICauHoiRepository? cauHoiDAL = null,
            IPhienLamBaiRepository? phienDAL = null)
        {
            _dal = dal ?? new KetQuaKiemTraDAL();
            _baiKiemTraDAL = baiKiemTraDAL ?? new BaiKiemTraDAL();
            _dangKyDAL = dangKyDAL ?? new DangKyKhoaHocDAL();
            _khoaHocDAL = khoaHocDAL ?? new KhoaHocDAL();
            _cauHoiDAL = cauHoiDAL ?? new CauHoiDAL();
            _phienDAL = phienDAL ?? new PhienLamBaiDAL();
        }

        public List<KetQuaKiemTraModel> GetByDangKy(int maDangKy, int? maSinhVienYeuCau = null)
        {
            if (maDangKy <= 0) return new List<KetQuaKiemTraModel>();
            if (maSinhVienYeuCau.HasValue && maSinhVienYeuCau.Value > 0)
            {
                var dk = _dangKyDAL.GetById(maDangKy);
                if (dk == null) throw new InvalidOperationException("Thông tin đăng ký không tồn tại.");
                if (dk.MaSinhVien != maSinhVienYeuCau.Value)
                    throw new InvalidOperationException("Bạn không có quyền xem lịch sử làm bài của học viên khác.");
            }
            return _dal.GetByDangKy(maDangKy);
        }

        public PagedResult<KetQuaKiemTraModel> GetByKhoaHoc(int maKhoaHoc, int trang = 1, int kichThuocTrang = 20, int? maGiangVienYeuCau = null, bool isAdmin = false)
        {
            if (trang < 1) trang = 1;
            if (!isAdmin && maGiangVienYeuCau.HasValue)
            {
                var kh = _khoaHocDAL.GetById(maKhoaHoc);
                if (kh == null) throw new InvalidOperationException("Khóa học không tồn tại.");
                if (kh.MaGiangVien != maGiangVienYeuCau.Value)
                    throw new InvalidOperationException("Bạn không có quyền xem bảng điểm khóa học của giảng viên khác.");
            }
            return _dal.GetByKhoaHoc(maKhoaHoc, trang, kichThuocTrang);
        }

        public KetQuaKiemTraModel? GetById(int id) => _dal.GetById(id);

        /// <summary>
        /// Luồng 2: Bắt đầu phiên làm bài thi trắc nghiệm chống gian lận
        /// Trả về câu hỏi đã trộn ngẫu nhiên và tuyệt đối ẩn cờ LaDapAnDung
        /// </summary>
        public BatDauKiemTraResponse BatDauPhienLamBai(int maDangKy, int maBaiKiemTra, int maSinhVien)
        {
            var session = _phienDAL.BatDauPhien(maDangKy, maBaiKiemTra, maSinhVien);
            var rawQuestions = _cauHoiDAL.GetByBaiKiemTra(maBaiKiemTra);
            var rng = new Random();

            var orderedQuestions = rawQuestions.OrderBy(_ => rng.Next()).ToList();
            int stt = 1;
            foreach (var q in orderedQuestions)
            {
                session.DanhSachCauHoi.Add(new CauHoiThiDto
                {
                    MaCauHoi = q.MaCauHoi,
                    NoiDung = q.NoiDung,
                    Diem = q.Diem,
                    SoThuTu = stt++,
                    DanhSachLuaChon = q.DanhSachLuaChon
                        .OrderBy(_ => rng.Next())
                        .Select((opt, idx) => new LuaChonThiDto
                        {
                            MaLuaChon = opt.MaLuaChon,
                            MaCauHoi = opt.MaCauHoi,
                            NoiDung = opt.NoiDung,
                            SoThuTu = idx + 1
                        })
                        .ToList()
                });
            }

            return session;
        }

        /// <summary>
        /// Luồng 2: Nộp bài & Chấm điểm chống gian lận phía Server (hỗ trợ cả chấm từ DanhSachTraLoi lẫn DiemSo tương thích ngược)
        /// </summary>
        public int NopBaiChongGianLan(NopBaiKiemTraRequest request, int maSinhVien)
        {
            if (request.MaDangKy <= 0 || request.MaBaiKiemTra <= 0)
                throw new ArgumentException("Thông tin bài nộp không hợp lệ.");

            var dk = _dangKyDAL.GetById(request.MaDangKy);
            if (dk == null) throw new InvalidOperationException("Thông tin đăng ký khóa học không tồn tại.");
            if (dk.MaSinhVien != maSinhVien) throw new InvalidOperationException("Bạn không có quyền nộp bài cho lượt đăng ký này.");

            var bkt = _baiKiemTraDAL.GetById(request.MaBaiKiemTra);
            if (bkt == null) throw new InvalidOperationException("Bài kiểm tra không tồn tại.");
            if (bkt.MaKhoaHoc != dk.MaKhoaHoc) throw new InvalidOperationException("Bài kiểm tra không thuộc khóa học của đăng ký này.");

            // Kiểm tra phiên làm bài (nếu có MaPhien)
            if (request.MaPhien.HasValue && request.MaPhien.Value > 0)
            {
                var phien = _phienDAL.GetById(request.MaPhien.Value);
                if (phien == null || phien.MaDangKy != request.MaDangKy)
                    throw new InvalidOperationException("Phiên làm bài không hợp lệ.");
                if (phien.TrangThai == "Đã nộp")
                    throw new InvalidOperationException("Phiên làm bài này đã được nộp trước đó.");

                // Cho phép ân hạn trễ mạng tối đa 60 giây
                if (DateTime.Now > phien.ThoiGianHetHan.AddSeconds(60))
                    throw new InvalidOperationException("Đã quá thời gian làm bài quy định của hệ thống.");
            }

            int lanThu = _dal.GetLanThuTiepTheo(request.MaDangKy, request.MaBaiKiemTra);
            if (lanThu > bkt.SoLanLamToiDa)
                throw new InvalidOperationException($"Bạn đã vượt quá số lần làm bài tối đa ({bkt.SoLanLamToiDa} lần).");

            decimal diemSoTinhToan;
            int? soCauDung = null;
            int? tongSoCau = null;

            var bankQuestions = _cauHoiDAL.GetByBaiKiemTra(request.MaBaiKiemTra);
            if (request.DanhSachTraLoi != null && request.DanhSachTraLoi.Count > 0 && bankQuestions.Count > 0)
            {
                tongSoCau = bankQuestions.Count;
                int correctCount = 0;
                foreach (var q in bankQuestions)
                {
                    var ans = request.DanhSachTraLoi.FirstOrDefault(a => a.MaCauHoi == q.MaCauHoi);
                    if (ans != null && q.DanhSachLuaChon.Any(o => o.MaLuaChon == ans.MaLuaChon && o.LaDapAnDung))
                    {
                        correctCount++;
                    }
                }
                soCauDung = correctCount;
                diemSoTinhToan = Math.Round(bkt.DiemToiDa * correctCount / bankQuestions.Count, 2);
            }
            else if (request.DiemSo.HasValue)
            {
                if (request.DiemSo.Value < 0 || request.DiemSo.Value > bkt.DiemToiDa)
                    throw new ArgumentException($"Điểm số phải nằm trong khoảng từ 0 đến {bkt.DiemToiDa}.");
                diemSoTinhToan = request.DiemSo.Value;
            }
            else
            {
                diemSoTinhToan = 0;
            }

            string trangThaiKetQua = request.SoLanChuyenTab > bkt.GioiHanChuyenTab
                ? $"Đã chấm (Cảnh báo chuyển tab {request.SoLanChuyenTab} lần)"
                : "Đã chấm";

            string? jsonAnswers = request.DanhSachTraLoi != null ? JsonSerializer.Serialize(request.DanhSachTraLoi) : null;

            return _phienDAL.ChamDiemVaLuuKetQuaTransaction(
                request.MaDangKy,
                request.MaBaiKiemTra,
                request.MaPhien,
                lanThu,
                diemSoTinhToan,
                soCauDung,
                tongSoCau,
                request.SoLanChuyenTab,
                trangThaiKetQua,
                jsonAnswers);
        }

        public int NopBai(int maDangKy, int maBaiKiemTra, decimal diemSo, int maSinhVien)
        {
            return NopBaiChongGianLan(new NopBaiKiemTraRequest
            {
                MaDangKy = maDangKy,
                MaBaiKiemTra = maBaiKiemTra,
                DiemSo = diemSo
            }, maSinhVien);
        }
    }

    public class ThongBaoBLL
    {
        private readonly IThongBaoRepository _dal;
        private readonly INguoiDungRepository _nguoiDungDAL;
        private readonly IBackgroundTaskQueue _backgroundQueue;

        public ThongBaoBLL(
            IThongBaoRepository? dal = null,
            INguoiDungRepository? nguoiDungDAL = null,
            IBackgroundTaskQueue? backgroundQueue = null)
        {
            _dal = dal ?? new ThongBaoDAL();
            _nguoiDungDAL = nguoiDungDAL ?? new NguoiDungDAL();
            _backgroundQueue = backgroundQueue ?? BackgroundTaskQueue.Shared;
        }

        public List<ThongBaoModel> GetByNguoiDung(int maNguoiDung)
        {
            if (maNguoiDung <= 0) return new List<ThongBaoModel>();
            return _dal.GetByNguoiDung(maNguoiDung);
        }

        public int GuiThongBao(ThongBaoModel model)
        {
            if (model.MaNguoiDung <= 0) throw new ArgumentException("Người nhận không hợp lệ.");
            if (string.IsNullOrWhiteSpace(model.TieuDe)) throw new ArgumentException("Tiêu đề thông báo không được để trống.");
            if (string.IsNullOrWhiteSpace(model.NoiDung)) throw new ArgumentException("Nội dung thông báo không được để trống.");

            int id = _dal.Insert(model);

            // Đẩy tác vụ gửi email bất đồng bộ vào Background Queue (Luồng 4)
            var recipient = _nguoiDungDAL.GetById(model.MaNguoiDung);
            if (recipient != null && !string.IsNullOrWhiteSpace(recipient.Email))
            {
                _backgroundQueue.TryQueueEmailNotification(recipient.Email, model.TieuDe, $"<p>{model.NoiDung}</p>");
            }

            return id;
        }

        public bool DanhDauDaDoc(int id, int maNguoiDung)
        {
            if (id <= 0 || maNguoiDung <= 0) return false;
            return _dal.MarkAsRead(id, maNguoiDung);
        }

        public bool XoaThongBao(int id, int maNguoiDung)
        {
            if (id <= 0 || maNguoiDung <= 0) return false;
            return _dal.Delete(id, maNguoiDung);
        }
    }
}
