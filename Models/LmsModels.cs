using System;

namespace Models
{
    public class DanhMucModel
    {
        public int MaDanhMuc { get; set; }
        public string TenDanhMuc { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public bool TrangThai { get; set; } = true;
        public DateTime NgayTao { get; set; }
        public bool DaXoa { get; set; }
        public DateTime? NgayCapNhat { get; set; }
    }

    public class KhoaHocModel
    {
        public int MaKhoaHoc { get; set; }
        public int MaDanhMuc { get; set; }
        public int MaGiangVien { get; set; }
        public string TenKhoaHoc { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public decimal Gia { get; set; }
        public bool TrangThai { get; set; } = true;
        public string TrangThaiDuyet { get; set; } = "Đã duyệt"; // Bản nháp, Chờ duyệt, Đã duyệt, Từ chối
        public string? LyDoTuChoi { get; set; }
        public bool YeuCauHoanThanhBaiGiang { get; set; } = true;
        public DateTime NgayTao { get; set; }
        public bool DaXoa { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        // Bổ trợ hiển thị
        public string? TenDanhMuc { get; set; }
        public string? TenGiangVien { get; set; }
    }

    public class BaiGiangModel
    {
        public int MaBaiGiang { get; set; }
        public int MaKhoaHoc { get; set; }
        public int SoThuTu { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string LoaiBaiGiang { get; set; } = "Video"; // Video / PDF
        public int ThoiLuong { get; set; } // Phút
        public string URL { get; set; } = string.Empty;
        public int? BaiGiangTienQuyetId { get; set; }
        public bool TrangThai { get; set; } = true;
        public DateTime NgayTao { get; set; }
        public bool DaXoa { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        // Bổ trợ hiển thị trạng thái mở khóa & hoàn thành cho học viên
        public bool DaHoanThanh { get; set; }
        public bool DuocMoKhoa { get; set; } = true;
    }

    public class DangKyKhoaHocModel
    {
        public int MaDangKy { get; set; }
        public int MaSinhVien { get; set; }
        public int MaKhoaHoc { get; set; }
        public DateTime NgayDangKy { get; set; }
        public decimal TienDoPhanTram { get; set; }
        public string TrangThai { get; set; } = "Đang học";
        public DateTime NgayTao { get; set; }
        public bool DaXoa { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        // Bổ trợ hiển thị
        public string? TenKhoaHoc { get; set; }
        public string? HoTenSinhVien { get; set; }
        public string? Lop { get; set; }
    }

    public class BaiKiemTraModel
    {
        public int MaBaiKiemTra { get; set; }
        public int MaKhoaHoc { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public int ThoiGianLamBai { get; set; } // Phút
        public decimal DiemToiDa { get; set; } = 10;
        public decimal DiemDat { get; set; } // Computed column (50%)
        public int SoLanLamToiDa { get; set; } = 3;
        public bool TronCauHoi { get; set; } = true;
        public int GioiHanChuyenTab { get; set; } = 3;
        public decimal YeuCauTienDoToiThieu { get; set; } = 0;
        public bool TrangThai { get; set; } = true;
        public DateTime NgayTao { get; set; }
        public bool DaXoa { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        // Bổ trợ hiển thị
        public string? TenKhoaHoc { get; set; }
    }

    public class KetQuaKiemTraModel
    {
        public int MaKetQua { get; set; }
        public int MaDangKy { get; set; }
        public int MaBaiKiemTra { get; set; }
        public int LanThu { get; set; } = 1;
        public DateTime NgayNop { get; set; }
        public decimal DiemSo { get; set; }
        public decimal? DiemDat { get; set; }
        public string? DanhGia { get; set; } // Computed: "Đạt" / "Không đạt"
        public int? SoCauDung { get; set; }
        public int? TongSoCau { get; set; }
        public int SoLanChuyenTab { get; set; }
        public string TrangThai { get; set; } = "Đã chấm";
        public DateTime NgayTao { get; set; }
        public bool DaXoa { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        // Bổ trợ hiển thị
        public string? TieuDeBaiKiemTra { get; set; }
        public string? TenKhoaHoc { get; set; }
        public string? HoTenSinhVien { get; set; }
    }

    public class ThongBaoModel
    {
        public int MaThongBao { get; set; }
        public int MaNguoiDung { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public string LoaiThongBao { get; set; } = "Hệ thống";
        public DateTime NgayGui { get; set; }
        public bool TrangThai { get; set; } = false; // false: chưa đọc, true: đã đọc
        public bool DaGuiEmail { get; set; } = false;
        public DateTime NgayTao { get; set; }
        public bool DaXoa { get; set; }
        public DateTime? NgayCapNhat { get; set; }
    }
}
