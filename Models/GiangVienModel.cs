using System;

namespace Models
{
    public class GiangVienModel
    {
        public int MaGiangVien { get; set; }
        public int MaNguoiDung { get; set; }
        public string? TrinhDo { get; set; }
        public string? ChuyenMon { get; set; }
        public string? Khoa { get; set; }
        public bool TrangThai { get; set; } = true;
        public DateTime NgayTao { get; set; }
        public bool DaXoa { get; set; }
        public DateTime? NgayCapNhat { get; set; }
    }
}
