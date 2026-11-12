using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Models
{
    public class RefreshTokenModel
    {
        public int MaRefreshToken { get; set; }
        public int MaNguoiDung { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public string JwtId { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
        public string? ReplacedByTokenHash { get; set; }
        public string? ReasonRevoked { get; set; }
        public string? RemoteIpAddress { get; set; }
        public bool DaXoa { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
        public bool IsRevoked => RevokedAtUtc.HasValue;
        public bool IsActive => !DaXoa && !IsRevoked && !IsExpired;
    }

    public class AuditLogModel
    {
        public long MaAuditLog { get; set; }
        public int? MaNguoiDung { get; set; }
        public string? TenDangNhap { get; set; }
        public string HanhDong { get; set; } = string.Empty;
        public string TenBang { get; set; } = string.Empty;
        public string? MaBanGhi { get; set; }
        public string? DuLieuCu { get; set; }
        public string? DuLieuMoi { get; set; }
        public string? DiaChiIp { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime ThoiGianUtc { get; set; } = DateTime.UtcNow;
    }

    public class CauHoiModel
    {
        public int MaCauHoi { get; set; }
        public int MaBaiKiemTra { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public decimal Diem { get; set; } = 1.0m;
        public string? GiaiThich { get; set; }
        public int SoThuTu { get; set; } = 1;
        public DateTime NgayTao { get; set; }
        public bool DaXoa { get; set; }
        public List<LuaChonModel> DanhSachLuaChon { get; set; } = new();
    }

    public class LuaChonModel
    {
        public int MaLuaChon { get; set; }
        public int MaCauHoi { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public bool LaDapAnDung { get; set; }
        public int SoThuTu { get; set; } = 1;
    }

    /// <summary>DTO trả về cho Học viên khi đang làm bài thi (Tuyệt đối ẩn LaDapAnDung và GiaiThich)</summary>
    public class CauHoiThiDto
    {
        public int MaCauHoi { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public decimal Diem { get; set; }
        public int SoThuTu { get; set; }
        public List<LuaChonThiDto> DanhSachLuaChon { get; set; } = new();
    }

    public class LuaChonThiDto
    {
        public int MaLuaChon { get; set; }
        public int MaCauHoi { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public int SoThuTu { get; set; }
    }

    public class PhienLamBaiModel
    {
        public int MaPhien { get; set; }
        public int MaDangKy { get; set; }
        public int MaBaiKiemTra { get; set; }
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianHetHan { get; set; }
        public DateTime? ThoiGianNop { get; set; }
        public int SoLanChuyenTab { get; set; }
        public string TrangThai { get; set; } = "Đang làm";
        public string? DuLieuBaiLamJson { get; set; }
        public int? MaKetQua { get; set; }
    }

    public class BatDauKiemTraResponse
    {
        public int MaPhien { get; set; }
        public int MaDangKy { get; set; }
        public int MaBaiKiemTra { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public int ThoiGianLamBai { get; set; }
        public decimal DiemToiDa { get; set; }
        public decimal DiemDat { get; set; }
        public int GioiHanChuyenTab { get; set; }
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianHetHan { get; set; }
        public int SoGiayConLai => Math.Max(0, (int)(ThoiGianHetHan - DateTime.Now).TotalSeconds);
        public List<CauHoiThiDto> DanhSachCauHoi { get; set; } = new();
    }

    public class CauTraLoiSinhVienDto
    {
        public int MaCauHoi { get; set; }
        public int MaLuaChon { get; set; }
    }

    public class NopBaiKiemTraRequest
    {
        public int MaDangKy { get; set; }
        public int MaBaiKiemTra { get; set; }
        public int? MaPhien { get; set; }
        public int SoLanChuyenTab { get; set; }
        public decimal? DiemSo { get; set; } // Hỗ trợ tương thích ngược nếu gửi trực tiếp điểm
        public List<CauTraLoiSinhVienDto>? DanhSachTraLoi { get; set; }
    }

    public class TienDoBaiGiangModel
    {
        public int MaTienDo { get; set; }
        public int MaDangKy { get; set; }
        public int MaBaiGiang { get; set; }
        public bool DaHoanThanh { get; set; } = true;
        public int ThoiLuongDaXem { get; set; }
        public DateTime NgayHoanThanh { get; set; }
    }

    public class HoanThanhBaiGiangRequest
    {
        public int MaDangKy { get; set; }
        public int MaBaiGiang { get; set; }
        public int ThoiLuongDaXem { get; set; }
    }

    public class PheDuyetKhoaHocRequest
    {
        public string TrangThaiDuyet { get; set; } = "Đã duyệt"; // "Đã duyệt" hoặc "Từ chối"
        public string? LyDoTuChoi { get; set; }
    }
}
