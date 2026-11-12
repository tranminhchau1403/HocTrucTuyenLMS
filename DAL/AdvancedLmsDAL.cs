using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using Models;

namespace DAL
{
    public interface IRefreshTokenRepository
    {
        int Insert(RefreshTokenModel token);
        RefreshTokenModel? GetByTokenHash(string tokenHash);
        bool RevokeToken(string tokenHash, string? replacedByTokenHash, string? reason);
        int RevokeAllUserTokens(int maNguoiDung, string reason);
        int DeleteExpiredTokens();
    }

    public class RefreshTokenDAL : IRefreshTokenRepository
    {
        public int Insert(RefreshTokenModel token)
        {
            using var conn = KetNoi.GetConnection();
            return conn.ExecuteScalar<int>(@"
                INSERT INTO RefreshTokens (MaNguoiDung, TokenHash, JwtId, ExpiresAtUtc, CreatedAtUtc, RemoteIpAddress)
                VALUES (@MaNguoiDung, @TokenHash, @JwtId, @ExpiresAtUtc, SYSUTCDATETIME(), @RemoteIpAddress);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", token);
        }

        public RefreshTokenModel? GetByTokenHash(string tokenHash)
        {
            using var conn = KetNoi.GetConnection();
            return conn.QueryFirstOrDefault<RefreshTokenModel>(@"
                SELECT MaRefreshToken, MaNguoiDung, TokenHash, JwtId, ExpiresAtUtc, CreatedAtUtc,
                       RevokedAtUtc, ReplacedByTokenHash, ReasonRevoked, RemoteIpAddress, DaXoa
                FROM RefreshTokens
                WHERE TokenHash = @tokenHash AND DaXoa = 0", new { tokenHash });
        }

        public bool RevokeToken(string tokenHash, string? replacedByTokenHash, string? reason)
        {
            using var conn = KetNoi.GetConnection();
            int rows = conn.Execute(@"
                UPDATE RefreshTokens
                SET RevokedAtUtc = SYSUTCDATETIME(),
                    ReplacedByTokenHash = @replacedByTokenHash,
                    ReasonRevoked = @reason
                WHERE TokenHash = @tokenHash AND RevokedAtUtc IS NULL AND DaXoa = 0",
                new { tokenHash, replacedByTokenHash, reason });
            return rows > 0;
        }

        public int RevokeAllUserTokens(int maNguoiDung, string reason)
        {
            using var conn = KetNoi.GetConnection();
            return conn.Execute(@"
                UPDATE RefreshTokens
                SET RevokedAtUtc = SYSUTCDATETIME(),
                    ReasonRevoked = @reason
                WHERE MaNguoiDung = @maNguoiDung AND RevokedAtUtc IS NULL AND DaXoa = 0",
                new { maNguoiDung, reason });
        }

        public int DeleteExpiredTokens()
        {
            using var conn = KetNoi.GetConnection();
            return conn.Execute(@"
                UPDATE RefreshTokens
                SET DaXoa = 1
                WHERE DaXoa = 0 AND ExpiresAtUtc < DATEADD(DAY, -7, SYSUTCDATETIME())");
        }
    }

    public interface IAuditLogRepository
    {
        void Insert(AuditLogModel log);
        PagedResult<AuditLogModel> GetPaged(int trang, int kichThuocTrang, string? hanhDong = null);
    }

    public class AuditLogDAL : IAuditLogRepository
    {
        public void Insert(AuditLogModel log)
        {
            try
            {
                using var conn = KetNoi.GetConnection();
                conn.Execute(@"
                    INSERT INTO AuditLogs (MaNguoiDung, TenDangNhap, HanhDong, TenBang, MaBanGhi, DuLieuCu, DuLieuMoi, DiaChiIp, CorrelationId)
                    VALUES (@MaNguoiDung, @TenDangNhap, @HanhDong, @TenBang, @MaBanGhi, @DuLieuCu, @DuLieuMoi, @DiaChiIp, @CorrelationId)", log);
            }
            catch
            {
                // Không để lỗi ghi AuditLog làm gián đoạn luồng chính nếu DB chưa khởi tạo bảng
            }
        }

        public PagedResult<AuditLogModel> GetPaged(int trang, int kichThuocTrang, string? hanhDong = null)
        {
            if (trang < 1) trang = 1;
            if (kichThuocTrang < 1) kichThuocTrang = 20;
            using var conn = KetNoi.GetConnection();
            string where = " WHERE (@hanhDong IS NULL OR HanhDong = @hanhDong)";
            int total = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM AuditLogs" + where, new { hanhDong });
            var items = conn.Query<AuditLogModel>(@"
                SELECT MaAuditLog, MaNguoiDung, TenDangNhap, HanhDong, TenBang, MaBanGhi,
                       DuLieuCu, DuLieuMoi, DiaChiIp, CorrelationId, ThoiGianUtc
                FROM AuditLogs" + where + @"
                ORDER BY ThoiGianUtc DESC
                OFFSET @offset ROWS FETCH NEXT @size ROWS ONLY",
                new { hanhDong, offset = (trang - 1) * kichThuocTrang, size = kichThuocTrang }).ToList();

            return new PagedResult<AuditLogModel>
            {
                Items = items,
                TongSo = total,
                Trang = trang,
                KichThuocTrang = kichThuocTrang
            };
        }
    }

    public interface ICauHoiRepository
    {
        List<CauHoiModel> GetByBaiKiemTra(int maBaiKiemTra);
        int InsertWithOptions(CauHoiModel model);
        bool Delete(int maCauHoi);
    }

    public class CauHoiDAL : ICauHoiRepository
    {
        public List<CauHoiModel> GetByBaiKiemTra(int maBaiKiemTra)
        {
            using var conn = KetNoi.GetConnection();
            var questions = conn.Query<CauHoiModel>(@"
                SELECT MaCauHoi, MaBaiKiemTra, NoiDung, Diem, GiaiThich, SoThuTu, NgayTao, DaXoa
                FROM CauHoi
                WHERE MaBaiKiemTra = @maBaiKiemTra AND DaXoa = 0
                ORDER BY SoThuTu ASC, MaCauHoi ASC", new { maBaiKiemTra }).ToList();

            if (questions.Count == 0) return questions;

            var qIds = questions.Select(q => q.MaCauHoi).ToArray();
            var options = conn.Query<LuaChonModel>(@"
                SELECT MaLuaChon, MaCauHoi, NoiDung, LaDapAnDung, SoThuTu
                FROM LuaChon
                WHERE MaCauHoi IN @qIds AND DaXoa = 0
                ORDER BY SoThuTu ASC, MaLuaChon ASC", new { qIds }).ToList();

            var lookup = options.ToLookup(o => o.MaCauHoi);
            foreach (var q in questions)
            {
                q.DanhSachLuaChon = lookup[q.MaCauHoi].ToList();
            }

            return questions;
        }

        public int InsertWithOptions(CauHoiModel model)
        {
            using var conn = KetNoi.GetConnection();
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                int maCauHoi = conn.ExecuteScalar<int>(@"
                    INSERT INTO CauHoi (MaBaiKiemTra, NoiDung, Diem, GiaiThich, SoThuTu)
                    VALUES (@MaBaiKiemTra, @NoiDung, @Diem, @GiaiThich, @SoThuTu);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", model, tran);

                int stt = 1;
                foreach (var opt in model.DanhSachLuaChon)
                {
                    conn.Execute(@"
                        INSERT INTO LuaChon (MaCauHoi, NoiDung, LaDapAnDung, SoThuTu)
                        VALUES (@MaCauHoi, @NoiDung, @LaDapAnDung, @SoThuTu)",
                        new { MaCauHoi = maCauHoi, opt.NoiDung, opt.LaDapAnDung, SoThuTu = opt.SoThuTu > 0 ? opt.SoThuTu : stt++ },
                        tran);
                }

                tran.Commit();
                return maCauHoi;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public bool Delete(int maCauHoi)
        {
            using var conn = KetNoi.GetConnection();
            return conn.Execute("UPDATE CauHoi SET DaXoa = 1 WHERE MaCauHoi = @maCauHoi AND DaXoa = 0", new { maCauHoi }) > 0;
        }
    }

    public interface ITienDoBaiGiangRepository
    {
        HashSet<int> GetCompletedLessonIds(int maDangKy);
        DangKyKhoaHocModel HoanThanhBaiGiangTuanTu(int maDangKy, int maBaiGiang, int maSinhVien, int thoiLuongDaXem);
    }

    public class TienDoBaiGiangDAL : ITienDoBaiGiangRepository
    {
        public HashSet<int> GetCompletedLessonIds(int maDangKy)
        {
            using var conn = KetNoi.GetConnection();
            var ids = conn.Query<int>(@"
                SELECT MaBaiGiang
                FROM TienDoBaiGiang
                WHERE MaDangKy = @maDangKy AND DaHoanThanh = 1 AND DaXoa = 0", new { maDangKy });
            return new HashSet<int>(ids);
        }

        public DangKyKhoaHocModel HoanThanhBaiGiangTuanTu(int maDangKy, int maBaiGiang, int maSinhVien, int thoiLuongDaXem)
        {
            using var conn = KetNoi.GetConnection();
            conn.Open();
            return conn.QuerySingle<DangKyKhoaHocModel>(
                "sp_BaiGiang_HoanThanhTuanTu",
                new
                {
                    MaDangKy = maDangKy,
                    MaBaiGiang = maBaiGiang,
                    MaSinhVien = maSinhVien,
                    ThoiLuongDaXem = thoiLuongDaXem
                },
                commandType: CommandType.StoredProcedure);
        }
    }

    public interface IPhienLamBaiRepository
    {
        BatDauKiemTraResponse BatDauPhien(int maDangKy, int maBaiKiemTra, int maSinhVien);
        PhienLamBaiModel? GetById(int maPhien);
        int ChamDiemVaLuuKetQuaTransaction(
            int maDangKy,
            int maBaiKiemTra,
            int? maPhien,
            int lanThu,
            decimal diemSo,
            int? soCauDung,
            int? tongSoCau,
            int soLanChuyenTab,
            string trangThaiKetQua,
            string? duLieuBaiLamJson);
    }

    public class PhienLamBaiDAL : IPhienLamBaiRepository
    {
        public BatDauKiemTraResponse BatDauPhien(int maDangKy, int maBaiKiemTra, int maSinhVien)
        {
            using var conn = KetNoi.GetConnection();
            conn.Open();
            return conn.QuerySingle<BatDauKiemTraResponse>(
                "sp_KiemTra_BatDauPhienLamBai",
                new { MaDangKy = maDangKy, MaBaiKiemTra = maBaiKiemTra, MaSinhVien = maSinhVien },
                commandType: CommandType.StoredProcedure);
        }

        public PhienLamBaiModel? GetById(int maPhien)
        {
            using var conn = KetNoi.GetConnection();
            return conn.QueryFirstOrDefault<PhienLamBaiModel>(@"
                SELECT MaPhien, MaDangKy, MaBaiKiemTra, ThoiGianBatDau, ThoiGianHetHan,
                       ThoiGianNop, SoLanChuyenTab, TrangThai, DuLieuBaiLamJson, MaKetQua
                FROM PhienLamBai
                WHERE MaPhien = @maPhien AND DaXoa = 0", new { maPhien });
        }

        public int ChamDiemVaLuuKetQuaTransaction(
            int maDangKy,
            int maBaiKiemTra,
            int? maPhien,
            int lanThu,
            decimal diemSo,
            int? soCauDung,
            int? tongSoCau,
            int soLanChuyenTab,
            string trangThaiKetQua,
            string? duLieuBaiLamJson)
        {
            using var conn = KetNoi.GetConnection();
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                int maKetQua = conn.ExecuteScalar<int>(@"
                    INSERT INTO KetQuaKiemTra (MaDangKy, MaBaiKiemTra, LanThu, NgayNop, DiemSo, SoCauDung, TongSoCau, SoLanChuyenTab, TrangThai)
                    VALUES (@maDangKy, @maBaiKiemTra, @lanThu, GETDATE(), @diemSo, @soCauDung, @tongSoCau, @soLanChuyenTab, @trangThaiKetQua);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new { maDangKy, maBaiKiemTra, lanThu, diemSo, soCauDung, tongSoCau, soLanChuyenTab, trangThaiKetQua },
                    tran);

                if (maPhien.HasValue && maPhien.Value > 0)
                {
                    string trangThaiPhien = trangThaiKetQua.Contains("Vi phạm", StringComparison.OrdinalIgnoreCase)
                        ? "Vi phạm quy chế"
                        : "Đã nộp";
                    conn.Execute(@"
                        UPDATE PhienLamBai
                        SET ThoiGianNop = GETDATE(),
                            SoLanChuyenTab = @soLanChuyenTab,
                            TrangThai = @trangThaiPhien,
                            DuLieuBaiLamJson = @duLieuBaiLamJson,
                            MaKetQua = @maKetQua
                        WHERE MaPhien = @maPhien",
                        new { maPhien = maPhien.Value, soLanChuyenTab, trangThaiPhien, duLieuBaiLamJson, maKetQua },
                        tran);
                }

                // Kiểm tra nếu bài thi Đạt -> Tự động đẩy tiến độ lên 100% và phát hành Chứng chỉ nếu chưa có
                var kqInfo = conn.QueryFirstOrDefault<(string DanhGia, int MaNguoiDungSV, string TenKhoaHoc)>(@"
                    SELECT kq.DanhGia, s.MaNguoiDung AS MaNguoiDungSV, kh.TenKhoaHoc
                    FROM KetQuaKiemTra kq
                    JOIN DangKyKhoaHoc dk ON dk.MaDangKy = kq.MaDangKy
                    JOIN SinhVien s ON s.MaSinhVien = dk.MaSinhVien
                    JOIN KhoaHoc kh ON kh.MaKhoaHoc = dk.MaKhoaHoc
                    WHERE kq.MaKetQua = @maKetQua", new { maKetQua }, tran);

                if (kqInfo.DanhGia == "Đạt")
                {
                    conn.Execute(@"
                        UPDATE DangKyKhoaHoc
                        SET TienDoPhanTram = 100, TrangThai = N'Hoàn thành'
                        WHERE MaDangKy = @maDangKy AND TienDoPhanTram < 100",
                        new { maDangKy }, tran);

                    string certCode = $"LMS-CERT-{maDangKy:D6}";
                    string certHash = BaoCaoDAL.ComputeCertificateHash(maDangKy, certCode);

                    conn.Execute(@"
                        IF NOT EXISTS (SELECT 1 FROM ChungChi WHERE MaDangKy = @maDangKy AND DaXoa = 0)
                        BEGIN
                            INSERT INTO ChungChi (MaDangKy, MaChungChiCode, MaXacThucHash, NgayCap, DiemTrungBinh, XepLoai, DuongDanPdf)
                            VALUES (@maDangKy, @certCode, @certHash, GETDATE(), @diemSo,
                                    CASE WHEN @diemSo >= 8.5 THEN N'Hoàn thành xuất sắc' ELSE N'Hoàn thành giỏi' END,
                                    @pdfPath);
                        END",
                        new { maDangKy, certCode, certHash, diemSo, pdfPath = $"/certificates/{certCode}.pdf" },
                        tran);
                }

                if (kqInfo.MaNguoiDungSV > 0)
                {
                    conn.Execute(@"
                        INSERT INTO ThongBao (MaNguoiDung, TieuDe, NoiDung, LoaiThongBao)
                        VALUES (@uid, @tieuDe, @noiDung, N'Khảo thí')",
                        new
                        {
                            uid = kqInfo.MaNguoiDungSV,
                            tieuDe = $"Kết quả kiểm tra khóa {kqInfo.TenKhoaHoc}",
                            noiDung = $"Bạn đạt {diemSo:0.##} điểm ({kqInfo.DanhGia}) ở lần thi thứ {lanThu}."
                        },
                        tran);
                }

                tran.Commit();
                return maKetQua;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
    }
}
