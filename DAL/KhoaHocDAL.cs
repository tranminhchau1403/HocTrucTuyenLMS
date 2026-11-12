using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Microsoft.Data.SqlClient;
using Models;

namespace DAL
{
    public interface IKhoaHocRepository
    {
        PagedResult<KhoaHocModel> GetAll(int? maDanhMuc, int? maGiangVien, string? tuKhoa, int trang, int kichThuocTrang, string? trangThaiDuyet = null, string? sortBy = null, string? sortOrder = null);
        KhoaHocModel? GetById(int id);
        int Insert(KhoaHocModel model);
        bool Update(KhoaHocModel model);
        bool Delete(int id);
        bool UpdateTrangThaiDuyet(int maKhoaHoc, string trangThaiDuyet, string? lyDoTuChoi, int maNguoiCapNhat);
    }

    public class KhoaHocDAL : IKhoaHocRepository
    {
        private static readonly HashSet<string> AllowedSortColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "MaKhoaHoc", "TenKhoaHoc", "Gia", "NgayTao"
        };

        private const string SelectBase = @"
            SELECT k.MaKhoaHoc, k.MaDanhMuc, k.MaGiangVien, k.TenKhoaHoc, k.MoTa, k.Gia,
                   k.TrangThai, k.TrangThaiDuyet, k.LyDoTuChoi, k.YeuCauHoanThanhBaiGiang,
                   k.NgayTao, k.DaXoa, k.NgayCapNhat,
                   d.TenDanhMuc, n.HoTen AS TenGiangVien
            FROM KhoaHoc k
            JOIN DanhMuc d ON k.MaDanhMuc = d.MaDanhMuc
            JOIN GiangVien g ON k.MaGiangVien = g.MaGiangVien
            JOIN NguoiDung n ON g.MaNguoiDung = n.MaNguoiDung";

        public PagedResult<KhoaHocModel> GetAll(
            int? maDanhMuc,
            int? maGiangVien,
            string? tuKhoa,
            int trang,
            int kichThuocTrang,
            string? trangThaiDuyet = null,
            string? sortBy = null,
            string? sortOrder = null)
        {
            var result = new PagedResult<KhoaHocModel> { Trang = trang, KichThuocTrang = kichThuocTrang };

            string safeSortBy = !string.IsNullOrWhiteSpace(sortBy) && AllowedSortColumns.Contains(sortBy.Trim())
                ? sortBy.Trim()
                : "MaKhoaHoc";
            string safeSortOrder = string.Equals(sortOrder?.Trim(), "ASC", StringComparison.OrdinalIgnoreCase)
                ? "ASC"
                : "DESC";

            using var conn = KetNoi.GetConnection();
            conn.Open();

            using var multi = conn.QueryMultiple(
                "sp_KhoaHoc_TimKiemPhanTrang",
                new
                {
                    MaDanhMuc = maDanhMuc,
                    MaGiangVien = maGiangVien,
                    TuKhoa = tuKhoa,
                    TrangThaiDuyet = string.IsNullOrWhiteSpace(trangThaiDuyet) ? null : trangThaiDuyet.Trim(),
                    GiaTu = (decimal?)null,
                    GiaDen = (decimal?)null,
                    SortBy = safeSortBy,
                    SortOrder = safeSortOrder,
                    Trang = trang,
                    KichThuocTrang = kichThuocTrang
                },
                commandType: CommandType.StoredProcedure);

            result.TongSo = multi.ReadSingleOrDefault<int>();
            result.Items = multi.Read<KhoaHocModel>().ToList();
            return result;
        }

        public KhoaHocModel? GetById(int id)
        {
            using var conn = KetNoi.GetConnection();
            return conn.QueryFirstOrDefault<KhoaHocModel>(
                SelectBase + " WHERE k.MaKhoaHoc = @id AND k.DaXoa = 0",
                new { id });
        }

        public int Insert(KhoaHocModel model)
        {
            using var conn = KetNoi.GetConnection();
            string trangThaiDuyet = string.IsNullOrWhiteSpace(model.TrangThaiDuyet) ? "Đã duyệt" : model.TrangThaiDuyet;
            return conn.ExecuteScalar<int>(@"
                INSERT INTO KhoaHoc (MaDanhMuc, MaGiangVien, TenKhoaHoc, MoTa, Gia, TrangThai, TrangThaiDuyet, YeuCauHoanThanhBaiGiang)
                VALUES (@MaDanhMuc, @MaGiangVien, @TenKhoaHoc, @MoTa, @Gia, @TrangThai, @TrangThaiDuyet, @YeuCauHoanThanhBaiGiang);
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new
                {
                    model.MaDanhMuc,
                    model.MaGiangVien,
                    model.TenKhoaHoc,
                    model.MoTa,
                    model.Gia,
                    model.TrangThai,
                    TrangThaiDuyet = trangThaiDuyet,
                    model.YeuCauHoanThanhBaiGiang
                });
        }

        public bool Update(KhoaHocModel model)
        {
            using var conn = KetNoi.GetConnection();
            int rows = conn.Execute(@"
                UPDATE KhoaHoc
                SET MaDanhMuc = @MaDanhMuc,
                    TenKhoaHoc = @TenKhoaHoc,
                    MoTa = @MoTa,
                    Gia = @Gia,
                    TrangThai = @TrangThai,
                    YeuCauHoanThanhBaiGiang = @YeuCauHoanThanhBaiGiang
                WHERE MaKhoaHoc = @MaKhoaHoc AND DaXoa = 0",
                model);
            return rows > 0;
        }

        public bool Delete(int id)
        {
            using var conn = KetNoi.GetConnection();
            return conn.Execute("UPDATE KhoaHoc SET DaXoa = 1 WHERE MaKhoaHoc = @id AND DaXoa = 0", new { id }) > 0;
        }

        public bool UpdateTrangThaiDuyet(int maKhoaHoc, string trangThaiDuyet, string? lyDoTuChoi, int maNguoiCapNhat)
        {
            using var conn = KetNoi.GetConnection();
            conn.Open();

            if (trangThaiDuyet == "Đã duyệt" || trangThaiDuyet == "Từ chối")
            {
                conn.Execute(
                    "sp_KhoaHoc_PheDuyet",
                    new
                    {
                        MaKhoaHoc = maKhoaHoc,
                        TrangThaiDuyetMoi = trangThaiDuyet,
                        LyDoTuChoi = lyDoTuChoi,
                        MaAdmin = maNguoiCapNhat
                    },
                    commandType: CommandType.StoredProcedure);
                return true;
            }

            int rows = conn.Execute(@"
                UPDATE KhoaHoc
                SET TrangThaiDuyet = @trangThaiDuyet,
                    LyDoTuChoi = @lyDoTuChoi,
                    NguoiCapNhat = @maNguoiCapNhat
                WHERE MaKhoaHoc = @maKhoaHoc AND DaXoa = 0",
                new { maKhoaHoc, trangThaiDuyet, lyDoTuChoi, maNguoiCapNhat });
            return rows > 0;
        }
    }
}
