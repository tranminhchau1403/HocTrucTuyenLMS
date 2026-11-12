using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Models;

namespace DAL
{
    public interface IDanhMucRepository
    {
        List<DanhMucModel> GetAll();
        DanhMucModel? GetById(int id);
        int Insert(DanhMucModel model);
        bool Update(DanhMucModel model);
        bool Delete(int id);
    }

    public class DanhMucDAL : IDanhMucRepository
    {
        private const string SelectBase = @"
            SELECT MaDanhMuc, TenDanhMuc, MoTa, TrangThai, NgayTao, DaXoa, NgayCapNhat
            FROM DanhMuc";

        public List<DanhMucModel> GetAll()
        {
            var list = new List<DanhMucModel>();
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(SelectBase + " WHERE DaXoa = 0 ORDER BY MaDanhMuc DESC", conn);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapFromReader(reader));
            }
            return list;
        }

        public DanhMucModel? GetById(int id)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(SelectBase + " WHERE MaDanhMuc = @id AND DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@id", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapFromReader(reader) : null;
        }

        public int Insert(DanhMucModel model)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(@"
                INSERT INTO DanhMuc (TenDanhMuc, MoTa, TrangThai)
                VALUES (@ten, @mota, @trangthai);
                SELECT SCOPE_IDENTITY();", conn);
            cmd.Parameters.AddWithValue("@ten", model.TenDanhMuc);
            cmd.Parameters.AddWithValue("@mota", (object?)model.MoTa ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@trangthai", model.TrangThai);
            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public bool Update(DanhMucModel model)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(@"
                UPDATE DanhMuc
                SET TenDanhMuc = @ten, MoTa = @mota, TrangThai = @trangthai
                WHERE MaDanhMuc = @id AND DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@id", model.MaDanhMuc);
            cmd.Parameters.AddWithValue("@ten", model.TenDanhMuc);
            cmd.Parameters.AddWithValue("@mota", (object?)model.MoTa ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@trangthai", model.TrangThai);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Delete(int id)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand("UPDATE DanhMuc SET DaXoa = 1 WHERE MaDanhMuc = @id AND DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@id", id);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        private static DanhMucModel MapFromReader(SqlDataReader reader)
        {
            return new DanhMucModel
            {
                MaDanhMuc = Convert.ToInt32(reader["MaDanhMuc"]),
                TenDanhMuc = reader["TenDanhMuc"].ToString() ?? "",
                MoTa = reader["MoTa"] != DBNull.Value ? reader["MoTa"].ToString() : null,
                TrangThai = Convert.ToBoolean(reader["TrangThai"]),
                NgayTao = Convert.ToDateTime(reader["NgayTao"]),
                DaXoa = Convert.ToBoolean(reader["DaXoa"]),
                NgayCapNhat = reader["NgayCapNhat"] != DBNull.Value ? Convert.ToDateTime(reader["NgayCapNhat"]) : null
            };
        }
    }
}
