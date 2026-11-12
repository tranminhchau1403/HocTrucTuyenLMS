using System;

namespace Models
{
    public class SinhVienModel
    {
        public int MaSinhVien { get; set; }
        public int MaNguoiDung { get; set; }
        public DateOnly? NgaySinh { get; set; }
        public string? Lop { get; set; }
        public string? NganhHoc { get; set; }
        public string? DiaChi { get; set; }
        public bool TrangThai { get; set; } = true;
        public DateTime NgayTao { get; set; }
        public bool DaXoa { get; set; }
        public DateTime? NgayCapNhat { get; set; }
    }
}
