using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Models;

namespace DAL
{
    public interface IThongBaoRepository
    {
        List<ThongBaoModel> GetByNguoiDung(int maNguoiDung);
        ThongBaoModel? GetById(int id);
        int Insert(ThongBaoModel model);
        bool MarkAsRead(int id, int maNguoiDung);
        bool Delete(int id, int maNguoiDung);
    }

    public class ThongBaoDAL : IThongBaoRepository
    {
        private const string SelectBase = @"
            SELECT MaThongBao, MaNguoiDung, TieuDe, NoiDung, LoaiThongBao, NgayGui, TrangThai, DaGuiEmail, NgayTao, DaXoa, NgayCapNhat
            FROM ThongBao";

        public List<ThongBaoModel> GetByNguoiDung(int maNguoiDung)
        {
            var list = new List<ThongBaoModel>();
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(SelectBase + " WHERE MaNguoiDung = @maNguoiDung AND DaXoa = 0 ORDER BY NgayGui DESC", conn);
            cmd.Parameters.AddWithValue("@maNguoiDung", maNguoiDung);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapFromReader(reader));
            }
            return list;
        }

        public ThongBaoModel? GetById(int id)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(SelectBase + " WHERE MaThongBao = @id AND DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@id", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapFromReader(reader) : null;
        }

        public int Insert(ThongBaoModel model)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(@"
                INSERT INTO ThongBao (MaNguoiDung, TieuDe, NoiDung, LoaiThongBao, NgayGui, TrangThai, DaGuiEmail)
                VALUES (@maNguoiDung, @tieuDe, @noiDung, @loai, GETDATE(), @trangThai, @daGuiEmail);
                SELECT SCOPE_IDENTITY();", conn);
            cmd.Parameters.AddWithValue("@maNguoiDung", model.MaNguoiDung);
            cmd.Parameters.AddWithValue("@tieuDe", model.TieuDe);
            cmd.Parameters.AddWithValue("@noiDung", model.NoiDung);
            cmd.Parameters.AddWithValue("@loai", string.IsNullOrWhiteSpace(model.LoaiThongBao) ? "Hệ thống" : model.LoaiThongBao);
            cmd.Parameters.AddWithValue("@trangThai", model.TrangThai);
            cmd.Parameters.AddWithValue("@daGuiEmail", model.DaGuiEmail);
            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public bool MarkAsRead(int id, int maNguoiDung)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(@"
                UPDATE ThongBao
                SET TrangThai = 1
                WHERE MaThongBao = @id AND MaNguoiDung = @maNguoiDung AND DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@maNguoiDung", maNguoiDung);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Delete(int id, int maNguoiDung)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(@"
                UPDATE ThongBao
                SET DaXoa = 1
                WHERE MaThongBao = @id AND MaNguoiDung = @maNguoiDung AND DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@maNguoiDung", maNguoiDung);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        private static ThongBaoModel MapFromReader(SqlDataReader reader)
        {
            return new ThongBaoModel
            {
                MaThongBao = Convert.ToInt32(reader["MaThongBao"]),
                MaNguoiDung = Convert.ToInt32(reader["MaNguoiDung"]),
                TieuDe = reader["TieuDe"].ToString() ?? "",
                NoiDung = reader["NoiDung"].ToString() ?? "",
                LoaiThongBao = reader["LoaiThongBao"] != DBNull.Value ? reader["LoaiThongBao"].ToString()! : "Hệ thống",
                NgayGui = Convert.ToDateTime(reader["NgayGui"]),
                TrangThai = Convert.ToBoolean(reader["TrangThai"]),
                DaGuiEmail = reader["DaGuiEmail"] != DBNull.Value && Convert.ToBoolean(reader["DaGuiEmail"]),
                NgayTao = Convert.ToDateTime(reader["NgayTao"]),
                DaXoa = Convert.ToBoolean(reader["DaXoa"]),
                NgayCapNhat = reader["NgayCapNhat"] != DBNull.Value ? Convert.ToDateTime(reader["NgayCapNhat"]) : null
            };
        }
    }
}
