using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Models;

namespace DAL
{
    public interface IBaiKiemTraRepository
    {
        List<BaiKiemTraModel> GetByKhoaHoc(int maKhoaHoc);
        BaiKiemTraModel? GetById(int id);
        int Insert(BaiKiemTraModel model);
        bool Update(BaiKiemTraModel model);
        bool Delete(int id);
    }

    public class BaiKiemTraDAL : IBaiKiemTraRepository
    {
        private const string SelectBase = @"
            SELECT b.MaBaiKiemTra, b.MaKhoaHoc, b.TieuDe, b.ThoiGianLamBai, b.DiemToiDa, b.DiemDat,
                   b.SoLanLamToiDa, b.TronCauHoi, b.GioiHanChuyenTab, b.YeuCauTienDoToiThieu,
                   b.TrangThai, b.NgayTao, b.DaXoa, b.NgayCapNhat,
                   k.TenKhoaHoc
            FROM BaiKiemTra b
            JOIN KhoaHoc k ON b.MaKhoaHoc = k.MaKhoaHoc";

        public List<BaiKiemTraModel> GetByKhoaHoc(int maKhoaHoc)
        {
            var list = new List<BaiKiemTraModel>();
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(SelectBase + " WHERE b.MaKhoaHoc = @maKhoaHoc AND b.DaXoa = 0 ORDER BY b.MaBaiKiemTra ASC", conn);
            cmd.Parameters.AddWithValue("@maKhoaHoc", maKhoaHoc);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapFromReader(reader));
            }
            return list;
        }

        public BaiKiemTraModel? GetById(int id)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(SelectBase + " WHERE b.MaBaiKiemTra = @id AND b.DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@id", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapFromReader(reader) : null;
        }

        public int Insert(BaiKiemTraModel model)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(@"
                INSERT INTO BaiKiemTra (MaKhoaHoc, TieuDe, ThoiGianLamBai, DiemToiDa, SoLanLamToiDa, TronCauHoi, GioiHanChuyenTab, YeuCauTienDoToiThieu, TrangThai)
                VALUES (@maKhoaHoc, @tieuDe, @thoiGian, @diemToiDa, @soLanToiDa, @tronCauHoi, @gioiHanTab, @yeuCauTienDo, @trangThai);
                SELECT SCOPE_IDENTITY();", conn);
            cmd.Parameters.AddWithValue("@maKhoaHoc", model.MaKhoaHoc);
            cmd.Parameters.AddWithValue("@tieuDe", model.TieuDe);
            cmd.Parameters.AddWithValue("@thoiGian", model.ThoiGianLamBai);
            cmd.Parameters.AddWithValue("@diemToiDa", model.DiemToiDa);
            cmd.Parameters.AddWithValue("@soLanToiDa", model.SoLanLamToiDa > 0 ? model.SoLanLamToiDa : 3);
            cmd.Parameters.AddWithValue("@tronCauHoi", model.TronCauHoi);
            cmd.Parameters.AddWithValue("@gioiHanTab", model.GioiHanChuyenTab > 0 ? model.GioiHanChuyenTab : 3);
            cmd.Parameters.AddWithValue("@yeuCauTienDo", model.YeuCauTienDoToiThieu);
            cmd.Parameters.AddWithValue("@trangThai", model.TrangThai);
            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public bool Update(BaiKiemTraModel model)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(@"
                UPDATE BaiKiemTra
                SET TieuDe = @tieuDe, ThoiGianLamBai = @thoiGian, DiemToiDa = @diemToiDa,
                    SoLanLamToiDa = @soLanToiDa, TronCauHoi = @tronCauHoi, GioiHanChuyenTab = @gioiHanTab,
                    YeuCauTienDoToiThieu = @yeuCauTienDo, TrangThai = @trangThai
                WHERE MaBaiKiemTra = @id AND DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@id", model.MaBaiKiemTra);
            cmd.Parameters.AddWithValue("@tieuDe", model.TieuDe);
            cmd.Parameters.AddWithValue("@thoiGian", model.ThoiGianLamBai);
            cmd.Parameters.AddWithValue("@diemToiDa", model.DiemToiDa);
            cmd.Parameters.AddWithValue("@soLanToiDa", model.SoLanLamToiDa > 0 ? model.SoLanLamToiDa : 3);
            cmd.Parameters.AddWithValue("@tronCauHoi", model.TronCauHoi);
            cmd.Parameters.AddWithValue("@gioiHanTab", model.GioiHanChuyenTab > 0 ? model.GioiHanChuyenTab : 3);
            cmd.Parameters.AddWithValue("@yeuCauTienDo", model.YeuCauTienDoToiThieu);
            cmd.Parameters.AddWithValue("@trangThai", model.TrangThai);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Delete(int id)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand("UPDATE BaiKiemTra SET DaXoa = 1 WHERE MaBaiKiemTra = @id AND DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@id", id);
            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        private static BaiKiemTraModel MapFromReader(SqlDataReader reader)
        {
            return new BaiKiemTraModel
            {
                MaBaiKiemTra = Convert.ToInt32(reader["MaBaiKiemTra"]),
                MaKhoaHoc = Convert.ToInt32(reader["MaKhoaHoc"]),
                TieuDe = reader["TieuDe"].ToString() ?? "",
                ThoiGianLamBai = Convert.ToInt32(reader["ThoiGianLamBai"]),
                DiemToiDa = Convert.ToDecimal(reader["DiemToiDa"]),
                DiemDat = Convert.ToDecimal(reader["DiemDat"]),
                SoLanLamToiDa = Convert.ToInt32(reader["SoLanLamToiDa"]),
                TronCauHoi = Convert.ToBoolean(reader["TronCauHoi"]),
                GioiHanChuyenTab = Convert.ToInt32(reader["GioiHanChuyenTab"]),
                YeuCauTienDoToiThieu = Convert.ToDecimal(reader["YeuCauTienDoToiThieu"]),
                TrangThai = Convert.ToBoolean(reader["TrangThai"]),
                NgayTao = Convert.ToDateTime(reader["NgayTao"]),
                DaXoa = Convert.ToBoolean(reader["DaXoa"]),
                NgayCapNhat = reader["NgayCapNhat"] != DBNull.Value ? Convert.ToDateTime(reader["NgayCapNhat"]) : null,
                TenKhoaHoc = reader["TenKhoaHoc"] != DBNull.Value ? reader["TenKhoaHoc"].ToString() : null
            };
        }
    }
}
