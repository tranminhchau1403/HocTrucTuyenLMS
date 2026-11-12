using System;
using System.Collections.Generic;

namespace Models
{
    public class BaoCaoTongQuanModel
    {
        public int TongNguoiDung { get; set; }
        public int TongGiangVien { get; set; }
        public int TongHocVien { get; set; }
        public int TongKhoaHoc { get; set; }
        public int TongLuotDangKy { get; set; }
        public int TongLuotHoanThanh { get; set; }
        public decimal TongDoanhThu { get; set; }
        public List<KhoaHocPhoBienModel> TopKhoaHoc { get; set; } = new();
    }

    public class KhoaHocPhoBienModel
    {
        public int MaKhoaHoc { get; set; }
        public string TenKhoaHoc { get; set; } = string.Empty;
        public string TenGiangVien { get; set; } = string.Empty;
        public decimal Gia { get; set; }
        public int SoLuongHocVien { get; set; }
        public decimal DoanhThu { get; set; }
    }

    public class ChungChiModel
    {
        public int MaChungChi { get; set; }
        public int MaDangKy { get; set; }
        public int MaSinhVien { get; set; }
        public string HoTenSinhVien { get; set; } = string.Empty;
        public string Lop { get; set; } = string.Empty;
        public int MaKhoaHoc { get; set; }
        public string TenKhoaHoc { get; set; } = string.Empty;
        public string TenGiangVien { get; set; } = string.Empty;
        public DateTime NgayHoanThanh { get; set; }
        public decimal TienDoPhanTram { get; set; }
        public decimal? DiemTrungBinh { get; set; }
        public string SoChungChi { get; set; } = string.Empty;
        public string MaXacThucHash { get; set; } = string.Empty;
        public string XepLoai { get; set; } = "Hoàn thành xuất sắc";
        public string? DuongDanPdf { get; set; }
        public bool DaThuHoi { get; set; }
    }
}
