using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Models;

namespace DAL
{
    public interface INguoiDungRepository
    {
        PagedResult<NguoiDungModel> GetAll(string? quyenHan, string? tuKhoa, int trang, int kichThuocTrang, string? sortBy = null, string? sortOrder = null);
        NguoiDungModel? GetById(int id);
        NguoiDungModel? GetByTenDangNhap(string tenDangNhap);
        bool ExistsEmail(string email, int? excludeId = null);
        NguoiDungModel? Login(string tenDangNhap, string matKhau);
        (int MaNguoiDung, int MaSinhVien) InsertSinhVien(NguoiDungModel nguoiDung, SinhVienModel sinhVien);
        (int MaNguoiDung, int MaGiangVien) InsertGiangVien(NguoiDungModel nguoiDung, GiangVienModel giangVien);
        bool Update(NguoiDungModel model);
        bool ChangePassword(int id, string newPasswordHash);
        bool UpdateTrangThai(int id, bool trangThai);
        bool Delete(int id);
    }

    public class NguoiDungDAL : INguoiDungRepository
    {
        private static readonly Dictionary<string, string> AllowedSortColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["MaNguoiDung"] = "MaNguoiDung",
            ["TenDangNhap"] = "TenDangNhap",
            ["HoTen"] = "HoTen",
            ["Email"] = "Email",
            ["NgayTao"] = "NgayTao"
        };

        private const string SelectBase = @"
            SELECT MaNguoiDung, TenDangNhap, MatKhau, HoTen, Email, SoDienThoai,
                   QuyenHan, TrangThai, NgayTao, DaXoa, NgayCapNhat
            FROM NguoiDung";

        public PagedResult<NguoiDungModel> GetAll(
            string? quyenHan,
            string? tuKhoa,
            int trang,
            int kichThuocTrang,
            string? sortBy = null,
            string? sortOrder = null)
        {
            var result = new PagedResult<NguoiDungModel> { Trang = trang, KichThuocTrang = kichThuocTrang };
            string where = @" WHERE DaXoa = 0
                              AND (@quyen IS NULL OR QuyenHan = @quyen)
                              AND (@tukhoa IS NULL OR TenDangNhap LIKE @tukhoa OR HoTen LIKE @tukhoa OR Email LIKE @tukhoa)";

            string safeCol = !string.IsNullOrWhiteSpace(sortBy) && AllowedSortColumns.TryGetValue(sortBy.Trim(), out var col)
                ? col
                : "MaNguoiDung";
            string safeDir = string.Equals(sortOrder?.Trim(), "DESC", StringComparison.OrdinalIgnoreCase)
                ? "DESC"
                : "ASC";

            using (var conn = KetNoi.GetConnection())
            {
                conn.Open();

                var countCmd = new SqlCommand("SELECT COUNT(*) FROM NguoiDung" + where, conn);
                AddFilterParameters(countCmd, quyenHan, tuKhoa);
                result.TongSo = Convert.ToInt32(countCmd.ExecuteScalar());

                var cmd = new SqlCommand(SelectBase + where +
                    $" ORDER BY {safeCol} {safeDir} OFFSET @offset ROWS FETCH NEXT @size ROWS ONLY", conn);
                AddFilterParameters(cmd, quyenHan, tuKhoa);
                cmd.Parameters.AddWithValue("@offset", (trang - 1) * kichThuocTrang);
                cmd.Parameters.AddWithValue("@size", kichThuocTrang);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Items.Add(MapFromReader(reader));
                    }
                }
            }
            return result;
        }

        public NguoiDungModel? GetById(int id)
        {
            using (var conn = KetNoi.GetConnection())
            {
                var cmd = new SqlCommand(SelectBase + " WHERE MaNguoiDung = @id AND DaXoa = 0", conn);
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapFromReader(reader);
                    }
                }
            }
            return null;
        }

        public NguoiDungModel? GetByTenDangNhap(string tenDangNhap)
        {
            using (var conn = KetNoi.GetConnection())
            {
                var cmd = new SqlCommand(SelectBase + " WHERE TenDangNhap = @username AND DaXoa = 0", conn);
                cmd.Parameters.AddWithValue("@username", tenDangNhap);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapFromReader(reader);
                    }
                }
            }
            return null;
        }

        public bool ExistsEmail(string email, int? excludeId = null)
        {
            using (var conn = KetNoi.GetConnection())
            {
                var cmd = new SqlCommand(@"SELECT COUNT(*) FROM NguoiDung
                                           WHERE Email = @email AND DaXoa = 0
                                             AND (@excludeId IS NULL OR MaNguoiDung <> @excludeId)", conn);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@excludeId", (object?)excludeId ?? DBNull.Value);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public NguoiDungModel? Login(string tenDangNhap, string matKhau)
        {
            var user = GetByTenDangNhap(tenDangNhap);
            if (user == null || !user.TrangThai || user.DaXoa)
                return null;

            return user;
        }

        /// <summary>Tạo NguoiDung + SinhVien trong một transaction, trả về (MaNguoiDung, MaSinhVien).</summary>
        public (int MaNguoiDung, int MaSinhVien) InsertSinhVien(NguoiDungModel nguoiDung, SinhVienModel sinhVien)
        {
            using (var conn = KetNoi.GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        int maNguoiDung = InsertNguoiDung(conn, tran, nguoiDung);

                        var cmd = new SqlCommand(@"INSERT INTO SinhVien (MaNguoiDung, NgaySinh, Lop, NganhHoc, DiaChi)
                                                   VALUES (@manguoidung, @ngaysinh, @lop, @nganh, @diachi);
                                                   SELECT SCOPE_IDENTITY();", conn, tran);
                        cmd.Parameters.AddWithValue("@manguoidung", maNguoiDung);
                        cmd.Parameters.Add("@ngaysinh", SqlDbType.Date).Value = sinhVien.NgaySinh.HasValue
                            ? sinhVien.NgaySinh.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
                        cmd.Parameters.AddWithValue("@lop", (object?)sinhVien.Lop ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@nganh", (object?)sinhVien.NganhHoc ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@diachi", (object?)sinhVien.DiaChi ?? DBNull.Value);
                        int maSinhVien = Convert.ToInt32(cmd.ExecuteScalar());

                        tran.Commit();
                        return (maNguoiDung, maSinhVien);
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>Tạo NguoiDung + GiangVien trong một transaction, trả về (MaNguoiDung, MaGiangVien).</summary>
        public (int MaNguoiDung, int MaGiangVien) InsertGiangVien(NguoiDungModel nguoiDung, GiangVienModel giangVien)
        {
            using (var conn = KetNoi.GetConnection())
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        int maNguoiDung = InsertNguoiDung(conn, tran, nguoiDung);

                        var cmd = new SqlCommand(@"INSERT INTO GiangVien (MaNguoiDung, TrinhDo, ChuyenMon, Khoa)
                                                   VALUES (@manguoidung, @trinhdo, @chuyenmon, @khoa);
                                                   SELECT SCOPE_IDENTITY();", conn, tran);
                        cmd.Parameters.AddWithValue("@manguoidung", maNguoiDung);
                        cmd.Parameters.AddWithValue("@trinhdo", (object?)giangVien.TrinhDo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@chuyenmon", (object?)giangVien.ChuyenMon ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@khoa", (object?)giangVien.Khoa ?? DBNull.Value);
                        int maGiangVien = Convert.ToInt32(cmd.ExecuteScalar());

                        tran.Commit();
                        return (maNguoiDung, maGiangVien);
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool Update(NguoiDungModel model)
        {
            using (var conn = KetNoi.GetConnection())
            {
                var cmd = new SqlCommand(@"UPDATE NguoiDung
                                           SET HoTen = @hoten, Email = @email, SoDienThoai = @sdt
                                           WHERE MaNguoiDung = @id AND DaXoa = 0", conn);
                cmd.Parameters.AddWithValue("@id", model.MaNguoiDung);
                cmd.Parameters.AddWithValue("@hoten", model.HoTen);
                cmd.Parameters.AddWithValue("@email", model.Email);
                cmd.Parameters.AddWithValue("@sdt", (object?)model.SoDienThoai ?? DBNull.Value);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool ChangePassword(int id, string newPasswordHash)
        {
            using (var conn = KetNoi.GetConnection())
            {
                var cmd = new SqlCommand("UPDATE NguoiDung SET MatKhau = @password WHERE MaNguoiDung = @id AND DaXoa = 0", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@password", newPasswordHash);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool UpdateTrangThai(int id, bool trangThai)
        {
            using (var conn = KetNoi.GetConnection())
            {
                var cmd = new SqlCommand("UPDATE NguoiDung SET TrangThai = @trangthai WHERE MaNguoiDung = @id AND DaXoa = 0", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@trangthai", trangThai);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>Xóa mềm: đặt DaXoa = 1.</summary>
        public bool Delete(int id)
        {
            using (var conn = KetNoi.GetConnection())
            {
                var cmd = new SqlCommand("UPDATE NguoiDung SET DaXoa = 1 WHERE MaNguoiDung = @id AND DaXoa = 0", conn);
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        private static int InsertNguoiDung(SqlConnection conn, SqlTransaction tran, NguoiDungModel model)
        {
            var cmd = new SqlCommand(@"INSERT INTO NguoiDung (TenDangNhap, MatKhau, HoTen, Email, SoDienThoai, QuyenHan, TrangThai)
                                       VALUES (@username, @password, @hoten, @email, @sdt, @quyen, @trangthai);
                                       SELECT SCOPE_IDENTITY();", conn, tran);
            cmd.Parameters.AddWithValue("@username", model.TenDangNhap);
            cmd.Parameters.AddWithValue("@password", model.MatKhau);
            cmd.Parameters.AddWithValue("@hoten", model.HoTen);
            cmd.Parameters.AddWithValue("@email", model.Email);
            cmd.Parameters.AddWithValue("@sdt", (object?)model.SoDienThoai ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@quyen", model.QuyenHan);
            cmd.Parameters.AddWithValue("@trangthai", model.TrangThai);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static void AddFilterParameters(SqlCommand cmd, string? quyenHan, string? tuKhoa)
        {
            cmd.Parameters.AddWithValue("@quyen", (object?)quyenHan ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tukhoa", string.IsNullOrWhiteSpace(tuKhoa) ? DBNull.Value : "%" + tuKhoa.Trim() + "%");
        }

        private static NguoiDungModel MapFromReader(SqlDataReader reader)
        {
            return new NguoiDungModel
            {
                MaNguoiDung = Convert.ToInt32(reader["MaNguoiDung"]),
                TenDangNhap = reader["TenDangNhap"].ToString() ?? "",
                MatKhau = reader["MatKhau"].ToString() ?? "",
                HoTen = reader["HoTen"].ToString() ?? "",
                Email = reader["Email"].ToString() ?? "",
                SoDienThoai = reader["SoDienThoai"] != DBNull.Value ? reader["SoDienThoai"].ToString() : null,
                QuyenHan = reader["QuyenHan"].ToString() ?? "",
                TrangThai = Convert.ToBoolean(reader["TrangThai"]),
                NgayTao = Convert.ToDateTime(reader["NgayTao"]),
                DaXoa = Convert.ToBoolean(reader["DaXoa"]),
                NgayCapNhat = reader["NgayCapNhat"] != DBNull.Value ? Convert.ToDateTime(reader["NgayCapNhat"]) : null
            };
        }
    }
}
