using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Models;

namespace DAL
{
    public interface IDangKyKhoaHocRepository
    {
        PagedResult<DangKyKhoaHocModel> GetBySinhVien(int maSinhVien, int trang, int kichThuocTrang);
        PagedResult<DangKyKhoaHocModel> GetByKhoaHoc(int maKhoaHoc, int trang, int kichThuocTrang);
        DangKyKhoaHocModel? GetById(int id);
        DangKyKhoaHocModel? GetBySinhVienAndKhoaHoc(int maSinhVien, int maKhoaHoc);
        int Insert(DangKyKhoaHocModel model);
        bool UpdateTienDo(int maDangKy, decimal tienDo);
    }

    public class DangKyKhoaHocDAL : IDangKyKhoaHocRepository
    {
        private const string SelectBase = @"
            SELECT d.MaDangKy, d.MaSinhVien, d.MaKhoaHoc, d.NgayDangKy, d.TienDoPhanTram,
                   d.TrangThai, d.NgayTao, d.DaXoa, d.NgayCapNhat,
                   k.TenKhoaHoc, n.HoTen AS HoTenSinhVien, s.Lop
            FROM DangKyKhoaHoc d
            JOIN KhoaHoc k ON d.MaKhoaHoc = k.MaKhoaHoc
            JOIN SinhVien s ON d.MaSinhVien = s.MaSinhVien
            JOIN NguoiDung n ON s.MaNguoiDung = n.MaNguoiDung";

        public PagedResult<DangKyKhoaHocModel> GetBySinhVien(int maSinhVien, int trang, int kichThuocTrang)
        {
            var result = new PagedResult<DangKyKhoaHocModel> { Trang = trang, KichThuocTrang = kichThuocTrang };
            string where = " WHERE d.MaSinhVien = @maSinhVien AND d.DaXoa = 0";

            using var conn = KetNoi.GetConnection();
            conn.Open();

            var countCmd = new SqlCommand("SELECT COUNT(*) FROM DangKyKhoaHoc d" + where, conn);
            countCmd.Parameters.AddWithValue("@maSinhVien", maSinhVien);
            result.TongSo = Convert.ToInt32(countCmd.ExecuteScalar());

            var cmd = new SqlCommand(SelectBase + where + " ORDER BY d.MaDangKy DESC OFFSET @offset ROWS FETCH NEXT @size ROWS ONLY", conn);
            cmd.Parameters.AddWithValue("@maSinhVien", maSinhVien);
            cmd.Parameters.AddWithValue("@offset", (trang - 1) * kichThuocTrang);
            cmd.Parameters.AddWithValue("@size", kichThuocTrang);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Items.Add(MapFromReader(reader));
            }
            return result;
        }

        public PagedResult<DangKyKhoaHocModel> GetByKhoaHoc(int maKhoaHoc, int trang, int kichThuocTrang)
        {
            var result = new PagedResult<DangKyKhoaHocModel> { Trang = trang, KichThuocTrang = kichThuocTrang };
            string where = " WHERE d.MaKhoaHoc = @maKhoaHoc AND d.DaXoa = 0";

            using var conn = KetNoi.GetConnection();
            conn.Open();

            var countCmd = new SqlCommand("SELECT COUNT(*) FROM DangKyKhoaHoc d" + where, conn);
            countCmd.Parameters.AddWithValue("@maKhoaHoc", maKhoaHoc);
            result.TongSo = Convert.ToInt32(countCmd.ExecuteScalar());

            var cmd = new SqlCommand(SelectBase + where + " ORDER BY d.MaDangKy DESC OFFSET @offset ROWS FETCH NEXT @size ROWS ONLY", conn);
            cmd.Parameters.AddWithValue("@maKhoaHoc", maKhoaHoc);
            cmd.Parameters.AddWithValue("@offset", (trang - 1) * kichThuocTrang);
            cmd.Parameters.AddWithValue("@size", kichThuocTrang);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Items.Add(MapFromReader(reader));
            }
            return result;
        }

        public DangKyKhoaHocModel? GetById(int id)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(SelectBase + " WHERE d.MaDangKy = @id AND d.DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@id", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapFromReader(reader) : null;
        }

        public DangKyKhoaHocModel? GetBySinhVienAndKhoaHoc(int maSinhVien, int maKhoaHoc)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(SelectBase + " WHERE d.MaSinhVien = @maSinhVien AND d.MaKhoaHoc = @maKhoaHoc AND d.DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@maSinhVien", maSinhVien);
            cmd.Parameters.AddWithValue("@maKhoaHoc", maKhoaHoc);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapFromReader(reader) : null;
        }

        public int Insert(DangKyKhoaHocModel model)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(@"
                INSERT INTO DangKyKhoaHoc (MaSinhVien, MaKhoaHoc, NgayDangKy, TienDoPhanTram, TrangThai)
                VALUES (@maSinhVien, @maKhoaHoc, GETDATE(), @tienDo, @trangThai);
                SELECT SCOPE_IDENTITY();", conn);
            cmd.Parameters.AddWithValue("@maSinhVien", model.MaSinhVien);
            cmd.Parameters.AddWithValue("@maKhoaHoc", model.MaKhoaHoc);
            cmd.Parameters.AddWithValue("@tienDo", model.TienDoPhanTram);
            cmd.Parameters.AddWithValue("@trangThai", model.TrangThai);
            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public bool UpdateTienDo(int maDangKy, decimal tienDo)
        {
            using var conn = KetNoi.GetConnection();
            string trangThai = tienDo >= 100 ? "Hoàn thành" : "Đang học";
            var cmd = new SqlCommand(@"
                UPDATE DangKyKhoaHoc
                SET TienDoPhanTram = @tienDo, TrangThai = @trangThai
                WHERE MaDangKy = @id AND DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@id", maDangKy);
            cmd.Parameters.AddWithValue("@tienDo", tienDo);
            cmd.Parameters.AddWithValue("@trangThai", trangThai);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        private static DangKyKhoaHocModel MapFromReader(SqlDataReader reader)
        {
            return new DangKyKhoaHocModel
            {
                MaDangKy = Convert.ToInt32(reader["MaDangKy"]),
                MaSinhVien = Convert.ToInt32(reader["MaSinhVien"]),
                MaKhoaHoc = Convert.ToInt32(reader["MaKhoaHoc"]),
                NgayDangKy = Convert.ToDateTime(reader["NgayDangKy"]),
                TienDoPhanTram = Convert.ToDecimal(reader["TienDoPhanTram"]),
                TrangThai = reader["TrangThai"].ToString() ?? "Đang học",
                NgayTao = Convert.ToDateTime(reader["NgayTao"]),
                DaXoa = Convert.ToBoolean(reader["DaXoa"]),
                NgayCapNhat = reader["NgayCapNhat"] != DBNull.Value ? Convert.ToDateTime(reader["NgayCapNhat"]) : null,
                TenKhoaHoc = reader["TenKhoaHoc"] != DBNull.Value ? reader["TenKhoaHoc"].ToString() : null,
                HoTenSinhVien = reader["HoTenSinhVien"] != DBNull.Value ? reader["HoTenSinhVien"].ToString() : null,
                Lop = reader["Lop"] != DBNull.Value ? reader["Lop"].ToString() : null
            };
        }
    }
}
