using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Models;

namespace DAL
{
    public interface IBaiGiangRepository
    {
        List<BaiGiangModel> GetByKhoaHoc(int maKhoaHoc);
        BaiGiangModel? GetById(int id);
        int Insert(BaiGiangModel model);
        bool Update(BaiGiangModel model);
        bool Delete(int id);
    }

    public class BaiGiangDAL : IBaiGiangRepository
    {
        private const string SelectBase = @"
            SELECT MaBaiGiang, MaKhoaHoc, SoThuTu, TieuDe, LoaiBaiGiang, ThoiLuong, URL, BaiGiangTienQuyetId,
                   TrangThai, NgayTao, DaXoa, NgayCapNhat
            FROM BaiGiang";

        public List<BaiGiangModel> GetByKhoaHoc(int maKhoaHoc)
        {
            var list = new List<BaiGiangModel>();
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(SelectBase + " WHERE MaKhoaHoc = @maKhoaHoc AND DaXoa = 0 ORDER BY SoThuTu ASC", conn);
            cmd.Parameters.AddWithValue("@maKhoaHoc", maKhoaHoc);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapFromReader(reader));
            }
            return list;
        }

        public BaiGiangModel? GetById(int id)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(SelectBase + " WHERE MaBaiGiang = @id AND DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@id", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapFromReader(reader) : null;
        }

        public int Insert(BaiGiangModel model)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(@"
                INSERT INTO BaiGiang (MaKhoaHoc, SoThuTu, TieuDe, LoaiBaiGiang, ThoiLuong, URL, BaiGiangTienQuyetId, TrangThai)
                VALUES (@maKhoaHoc, @soThuTu, @tieuDe, @loai, @thoiLuong, @url, @tienQuyetId, @trangThai);
                SELECT SCOPE_IDENTITY();", conn);
            cmd.Parameters.AddWithValue("@maKhoaHoc", model.MaKhoaHoc);
            cmd.Parameters.AddWithValue("@soThuTu", model.SoThuTu);
            cmd.Parameters.AddWithValue("@tieuDe", model.TieuDe);
            cmd.Parameters.AddWithValue("@loai", model.LoaiBaiGiang);
            cmd.Parameters.AddWithValue("@thoiLuong", model.ThoiLuong);
            cmd.Parameters.AddWithValue("@url", model.URL);
            cmd.Parameters.AddWithValue("@tienQuyetId", (object?)model.BaiGiangTienQuyetId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@trangThai", model.TrangThai);
            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public bool Update(BaiGiangModel model)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(@"
                UPDATE BaiGiang
                SET SoThuTu = @soThuTu, TieuDe = @tieuDe, LoaiBaiGiang = @loai,
                    ThoiLuong = @thoiLuong, URL = @url, BaiGiangTienQuyetId = @tienQuyetId, TrangThai = @trangThai
                WHERE MaBaiGiang = @id AND DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@id", model.MaBaiGiang);
            cmd.Parameters.AddWithValue("@soThuTu", model.SoThuTu);
            cmd.Parameters.AddWithValue("@tieuDe", model.TieuDe);
            cmd.Parameters.AddWithValue("@loai", model.LoaiBaiGiang);
            cmd.Parameters.AddWithValue("@thoiLuong", model.ThoiLuong);
            cmd.Parameters.AddWithValue("@url", model.URL);
            cmd.Parameters.AddWithValue("@tienQuyetId", (object?)model.BaiGiangTienQuyetId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@trangThai", model.TrangThai);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Delete(int id)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand("UPDATE BaiGiang SET DaXoa = 1 WHERE MaBaiGiang = @id AND DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@id", id);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        private static BaiGiangModel MapFromReader(SqlDataReader reader)
        {
            return new BaiGiangModel
            {
                MaBaiGiang = Convert.ToInt32(reader["MaBaiGiang"]),
                MaKhoaHoc = Convert.ToInt32(reader["MaKhoaHoc"]),
                SoThuTu = Convert.ToInt32(reader["SoThuTu"]),
                TieuDe = reader["TieuDe"].ToString() ?? "",
                LoaiBaiGiang = reader["LoaiBaiGiang"].ToString() ?? "Video",
                ThoiLuong = Convert.ToInt32(reader["ThoiLuong"]),
                URL = reader["URL"].ToString() ?? "",
                BaiGiangTienQuyetId = reader["BaiGiangTienQuyetId"] != DBNull.Value ? Convert.ToInt32(reader["BaiGiangTienQuyetId"]) : null,
                TrangThai = Convert.ToBoolean(reader["TrangThai"]),
                NgayTao = Convert.ToDateTime(reader["NgayTao"]),
                DaXoa = Convert.ToBoolean(reader["DaXoa"]),
                NgayCapNhat = reader["NgayCapNhat"] != DBNull.Value ? Convert.ToDateTime(reader["NgayCapNhat"]) : null
            };
        }
    }
}
