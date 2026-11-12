using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Models;

namespace DAL
{
    public interface IKetQuaKiemTraRepository
    {
        List<KetQuaKiemTraModel> GetByDangKy(int maDangKy);
        PagedResult<KetQuaKiemTraModel> GetByKhoaHoc(int maKhoaHoc, int trang, int kichThuocTrang);
        int GetLanThuTiepTheo(int maDangKy, int maBaiKiemTra);
        int Insert(KetQuaKiemTraModel model);
        KetQuaKiemTraModel? GetById(int id);
    }

    public class KetQuaKiemTraDAL : IKetQuaKiemTraRepository
    {
        private const string SelectBase = @"
            SELECT kq.MaKetQua, kq.MaDangKy, kq.MaBaiKiemTra, kq.LanThu, kq.NgayNop, kq.DiemSo,
                   kq.DiemDat, kq.DanhGia, kq.SoCauDung, kq.TongSoCau, kq.SoLanChuyenTab,
                   kq.TrangThai, kq.NgayTao, kq.DaXoa, kq.NgayCapNhat,
                   bkt.TieuDe AS TieuDeBaiKiemTra, kh.TenKhoaHoc, nd.HoTen AS HoTenSinhVien
            FROM KetQuaKiemTra kq
            JOIN BaiKiemTra bkt ON kq.MaBaiKiemTra = bkt.MaBaiKiemTra
            JOIN DangKyKhoaHoc dk ON kq.MaDangKy = dk.MaDangKy
            JOIN KhoaHoc kh ON dk.MaKhoaHoc = kh.MaKhoaHoc
            JOIN SinhVien sv ON dk.MaSinhVien = sv.MaSinhVien
            JOIN NguoiDung nd ON sv.MaNguoiDung = nd.MaNguoiDung";

        public List<KetQuaKiemTraModel> GetByDangKy(int maDangKy)
        {
            var list = new List<KetQuaKiemTraModel>();
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(SelectBase + " WHERE kq.MaDangKy = @maDangKy AND kq.DaXoa = 0 ORDER BY kq.MaKetQua DESC", conn);
            cmd.Parameters.AddWithValue("@maDangKy", maDangKy);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapFromReader(reader));
            }
            return list;
        }

        public PagedResult<KetQuaKiemTraModel> GetByKhoaHoc(int maKhoaHoc, int trang, int kichThuocTrang)
        {
            var result = new PagedResult<KetQuaKiemTraModel> { Trang = trang, KichThuocTrang = kichThuocTrang };
            string where = " WHERE dk.MaKhoaHoc = @maKhoaHoc AND kq.DaXoa = 0";

            using var conn = KetNoi.GetConnection();
            conn.Open();

            var countCmd = new SqlCommand("SELECT COUNT(*) FROM KetQuaKiemTra kq JOIN DangKyKhoaHoc dk ON kq.MaDangKy = dk.MaDangKy" + where, conn);
            countCmd.Parameters.AddWithValue("@maKhoaHoc", maKhoaHoc);
            result.TongSo = Convert.ToInt32(countCmd.ExecuteScalar());

            var cmd = new SqlCommand(SelectBase + where + " ORDER BY kq.MaKetQua DESC OFFSET @offset ROWS FETCH NEXT @size ROWS ONLY", conn);
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

        public int GetLanThuTiepTheo(int maDangKy, int maBaiKiemTra)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(@"
                SELECT ISNULL(MAX(LanThu), 0) + 1
                FROM KetQuaKiemTra
                WHERE MaDangKy = @maDangKy AND MaBaiKiemTra = @maBaiKiemTra AND DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@maDangKy", maDangKy);
            cmd.Parameters.AddWithValue("@maBaiKiemTra", maBaiKiemTra);
            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public int Insert(KetQuaKiemTraModel model)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(@"
                INSERT INTO KetQuaKiemTra (MaDangKy, MaBaiKiemTra, LanThu, NgayNop, DiemSo, SoCauDung, TongSoCau, SoLanChuyenTab, TrangThai)
                VALUES (@maDangKy, @maBaiKiemTra, @lanThu, GETDATE(), @diemSo, @soCauDung, @tongSoCau, @soLanChuyenTab, @trangThai);
                SELECT SCOPE_IDENTITY();", conn);
            cmd.Parameters.AddWithValue("@maDangKy", model.MaDangKy);
            cmd.Parameters.AddWithValue("@maBaiKiemTra", model.MaBaiKiemTra);
            cmd.Parameters.AddWithValue("@lanThu", model.LanThu);
            cmd.Parameters.AddWithValue("@diemSo", model.DiemSo);
            cmd.Parameters.AddWithValue("@soCauDung", (object?)model.SoCauDung ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tongSoCau", (object?)model.TongSoCau ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@soLanChuyenTab", model.SoLanChuyenTab);
            cmd.Parameters.AddWithValue("@trangThai", model.TrangThai);
            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public KetQuaKiemTraModel? GetById(int id)
        {
            using var conn = KetNoi.GetConnection();
            var cmd = new SqlCommand(SelectBase + " WHERE kq.MaKetQua = @id AND kq.DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@id", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapFromReader(reader) : null;
        }

        private static KetQuaKiemTraModel MapFromReader(SqlDataReader reader)
        {
            return new KetQuaKiemTraModel
            {
                MaKetQua = Convert.ToInt32(reader["MaKetQua"]),
                MaDangKy = Convert.ToInt32(reader["MaDangKy"]),
                MaBaiKiemTra = Convert.ToInt32(reader["MaBaiKiemTra"]),
                LanThu = Convert.ToInt32(reader["LanThu"]),
                NgayNop = Convert.ToDateTime(reader["NgayNop"]),
                DiemSo = Convert.ToDecimal(reader["DiemSo"]),
                DiemDat = reader["DiemDat"] != DBNull.Value ? Convert.ToDecimal(reader["DiemDat"]) : null,
                DanhGia = reader["DanhGia"] != DBNull.Value ? reader["DanhGia"].ToString() : null,
                SoCauDung = reader["SoCauDung"] != DBNull.Value ? Convert.ToInt32(reader["SoCauDung"]) : null,
                TongSoCau = reader["TongSoCau"] != DBNull.Value ? Convert.ToInt32(reader["TongSoCau"]) : null,
                SoLanChuyenTab = reader["SoLanChuyenTab"] != DBNull.Value ? Convert.ToInt32(reader["SoLanChuyenTab"]) : 0,
                TrangThai = reader["TrangThai"].ToString() ?? "Đã chấm",
                NgayTao = Convert.ToDateTime(reader["NgayTao"]),
                DaXoa = Convert.ToBoolean(reader["DaXoa"]),
                NgayCapNhat = reader["NgayCapNhat"] != DBNull.Value ? Convert.ToDateTime(reader["NgayCapNhat"]) : null,
                TieuDeBaiKiemTra = reader["TieuDeBaiKiemTra"] != DBNull.Value ? reader["TieuDeBaiKiemTra"].ToString() : null,
                TenKhoaHoc = reader["TenKhoaHoc"] != DBNull.Value ? reader["TenKhoaHoc"].ToString() : null,
                HoTenSinhVien = reader["HoTenSinhVien"] != DBNull.Value ? reader["HoTenSinhVien"].ToString() : null
            };
        }
    }
}
