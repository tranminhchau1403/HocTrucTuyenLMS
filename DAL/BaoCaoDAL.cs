using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using Models;

namespace DAL
{
    public interface IBaoCaoRepository
    {
        BaoCaoTongQuanModel GetTongQuan();
        ChungChiModel? GetChungChi(int maDangKy);
        ChungChiModel? GetChungChiByCode(string maChungChiCode);
    }

    public class BaoCaoDAL : IBaoCaoRepository
    {
        public BaoCaoTongQuanModel GetTongQuan()
        {
            using var conn = KetNoi.GetConnection();
            conn.Open();

            using var multi = conn.QueryMultiple("sp_BaoCao_ThongKeTongQuan", commandType: CommandType.StoredProcedure);
            var res = multi.ReadFirstOrDefault<BaoCaoTongQuanModel>() ?? new BaoCaoTongQuanModel();
            res.TopKhoaHoc = multi.Read<KhoaHocPhoBienModel>().ToList();
            return res;
        }

        public ChungChiModel? GetChungChi(int maDangKy)
        {
            using var conn = KetNoi.GetConnection();
            conn.Open();

            var cmd = new SqlCommand(@"
                SELECT d.MaDangKy, d.MaSinhVien, nd.HoTen AS HoTenSinhVien, sv.Lop,
                       d.MaKhoaHoc, kh.TenKhoaHoc, gv_nd.HoTen AS TenGiangVien,
                       d.TienDoPhanTram, ISNULL(cc.NgayCap, ISNULL(d.NgayCapNhat, d.NgayDangKy)) AS NgayHoanThanh,
                       cc.MaChungChi, cc.MaChungChiCode, cc.MaXacThucHash, cc.DiemTrungBinh, cc.XepLoai, cc.DuongDanPdf, cc.DaThuHoi
                FROM DangKyKhoaHoc d
                JOIN SinhVien sv ON d.MaSinhVien = sv.MaSinhVien
                JOIN NguoiDung nd ON sv.MaNguoiDung = nd.MaNguoiDung
                JOIN KhoaHoc kh ON d.MaKhoaHoc = kh.MaKhoaHoc
                JOIN GiangVien gv ON kh.MaGiangVien = gv.MaGiangVien
                JOIN NguoiDung gv_nd ON gv.MaNguoiDung = gv_nd.MaNguoiDung
                LEFT JOIN ChungChi cc ON cc.MaDangKy = d.MaDangKy AND cc.DaXoa = 0
                WHERE d.MaDangKy = @maDangKy AND d.DaXoa = 0", conn);
            cmd.Parameters.AddWithValue("@maDangKy", maDangKy);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                decimal tienDo = Convert.ToDecimal(reader["TienDoPhanTram"]);
                if (tienDo < 100) return null; // Chưa hoàn thành chưa có chứng chỉ

                int id = Convert.ToInt32(reader["MaDangKy"]);
                string code = reader["MaChungChiCode"] != DBNull.Value
                    ? reader["MaChungChiCode"].ToString()!
                    : $"LMS-CERT-{id:D6}";
                string hash = reader["MaXacThucHash"] != DBNull.Value
                    ? reader["MaXacThucHash"].ToString()!
                    : ComputeCertificateHash(id, code);

                return new ChungChiModel
                {
                    MaChungChi = reader["MaChungChi"] != DBNull.Value ? Convert.ToInt32(reader["MaChungChi"]) : id,
                    MaDangKy = id,
                    MaSinhVien = Convert.ToInt32(reader["MaSinhVien"]),
                    HoTenSinhVien = reader["HoTenSinhVien"].ToString() ?? "",
                    Lop = reader["Lop"] != DBNull.Value ? reader["Lop"].ToString()! : "N/A",
                    MaKhoaHoc = Convert.ToInt32(reader["MaKhoaHoc"]),
                    TenKhoaHoc = reader["TenKhoaHoc"].ToString() ?? "",
                    TenGiangVien = reader["TenGiangVien"].ToString() ?? "",
                    NgayHoanThanh = Convert.ToDateTime(reader["NgayHoanThanh"]),
                    TienDoPhanTram = tienDo,
                    DiemTrungBinh = reader["DiemTrungBinh"] != DBNull.Value ? Convert.ToDecimal(reader["DiemTrungBinh"]) : 9.0m,
                    SoChungChi = code,
                    MaXacThucHash = hash,
                    XepLoai = reader["XepLoai"] != DBNull.Value ? reader["XepLoai"].ToString()! : "Hoàn thành xuất sắc",
                    DuongDanPdf = reader["DuongDanPdf"] != DBNull.Value ? reader["DuongDanPdf"].ToString() : $"/certificates/{code}.pdf",
                    DaThuHoi = reader["DaThuHoi"] != DBNull.Value && Convert.ToBoolean(reader["DaThuHoi"])
                };
            }
            return null;
        }

        public ChungChiModel? GetChungChiByCode(string maChungChiCode)
        {
            using var conn = KetNoi.GetConnection();
            conn.Open();

            // Hỗ trợ tra cứu theo MaChungChiCode trong bảng ChungChi hoặc theo định dạng LMS-CERT-000001
            int? parsedDangKyId = null;
            if (maChungChiCode.StartsWith("LMS-CERT-", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(maChungChiCode.Substring("LMS-CERT-".Length), out int dkId))
            {
                parsedDangKyId = dkId;
            }

            var maDangKy = conn.QueryFirstOrDefault<int?>(@"
                SELECT TOP 1 d.MaDangKy
                FROM DangKyKhoaHoc d
                LEFT JOIN ChungChi cc ON cc.MaDangKy = d.MaDangKy AND cc.DaXoa = 0
                WHERE d.DaXoa = 0 AND d.TienDoPhanTram >= 100
                  AND (cc.MaChungChiCode = @code OR (@parsedDangKyId IS NOT NULL AND d.MaDangKy = @parsedDangKyId))",
                new { code = maChungChiCode.Trim(), parsedDangKyId });

            return maDangKy.HasValue ? GetChungChi(maDangKy.Value) : null;
        }

        public static string ComputeCertificateHash(int maDangKy, string code)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{maDangKy}:{code}:CT_LMS_SECRET_2026"));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
