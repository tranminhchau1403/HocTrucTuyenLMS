using System;
using System.Collections.Generic;

namespace Models
{
    public class LoginRequest
    {
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RevokeTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    public class RegisterStudentRequest
    {
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public DateOnly? NgaySinh { get; set; }
        public string? Lop { get; set; }
        public string? NganhHoc { get; set; }
        public string? DiaChi { get; set; }
    }

    public class RegisterTeacherRequest
    {
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? TrinhDo { get; set; }
        public string? ChuyenMon { get; set; }
        public string? Khoa { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string MatKhauCu { get; set; } = string.Empty;
        public string MatKhauMoi { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiration { get; set; }
        public int MaNguoiDung { get; set; }
        public string TenDangNhap { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string QuyenHan { get; set; } = string.Empty;
        public int? MaSinhVien { get; set; }
        public int? MaGiangVien { get; set; }
    }

    public class UserProfileDto
    {
        public int MaNguoiDung { get; set; }
        public string TenDangNhap { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string QuyenHan { get; set; } = string.Empty;
        public DateTime NgayTao { get; set; }

        // Hồ sơ Học viên
        public int? MaSinhVien { get; set; }
        public DateOnly? NgaySinh { get; set; }
        public string? Lop { get; set; }
        public string? NganhHoc { get; set; }
        public string? DiaChi { get; set; }

        // Hồ sơ Giảng viên
        public int? MaGiangVien { get; set; }
        public string? TrinhDo { get; set; }
        public string? ChuyenMon { get; set; }
        public string? Khoa { get; set; }
    }

    public class UpdateStudentProfileRequest
    {
        public string HoTen { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public DateOnly? NgaySinh { get; set; }
        public string? Lop { get; set; }
        public string? NganhHoc { get; set; }
        public string? DiaChi { get; set; }
    }

    public class UpdateTeacherProfileRequest
    {
        public string HoTen { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? TrinhDo { get; set; }
        public string? ChuyenMon { get; set; }
        public string? Khoa { get; set; }
    }

    public class ChangeStatusRequest
    {
        public bool TrangThai { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TongSo { get; set; }
        public int Trang { get; set; } = 1;
        public int KichThuocTrang { get; set; } = 20;

        // Chuẩn Phụ lục A Giáo trình (Pagination Metadata)
        public int TotalItems => TongSo;
        public int PageIndex => Trang;
        public int PageSize => KichThuocTrang;
        public int TotalPages => KichThuocTrang > 0 ? (int)Math.Ceiling((double)TongSo / KichThuocTrang) : 0;
        public int TongSoTrang => TotalPages;
        public bool HasPreviousPage => Trang > 1;
        public bool HasNextPage => Trang < TotalPages;
    }
}
