using System;
using System.Text.Json.Serialization;

namespace Models
{
    public class NguoiDungModel
    {
        public int MaNguoiDung { get; set; }
        public string TenDangNhap { get; set; } = string.Empty;

        [JsonIgnore]
        public string MatKhau { get; set; } = string.Empty;

        public string HoTen { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string QuyenHan { get; set; } = "Học viên"; // Admin, Giảng viên, Học viên
        public bool TrangThai { get; set; } = true;
        public DateTime NgayTao { get; set; }
        public bool DaXoa { get; set; }
        public DateTime? NgayCapNhat { get; set; }
    }
}
