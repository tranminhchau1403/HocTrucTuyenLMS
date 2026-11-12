using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Models;

namespace DAL
{
    public interface ISinhVienRepository
    {
        SinhVienModel? GetById(int id);
        SinhVienModel? GetByMaNguoiDung(int maNguoiDung);
        bool Update(SinhVienModel model);
    }

    public class SinhVienDAL : ISinhVienRepository
    {
        private const string SelectBase = @"
            SELECT MaSinhVien, MaNguoiDung, NgaySinh, Lop, NganhHoc, DiaChi,
                   TrangThai, NgayTao, DaXoa, NgayCapNhat
            FROM SinhVien";

        public SinhVienModel? GetById(int id)
        {
            return QuerySingle(" WHERE MaSinhVien = @id AND DaXoa = 0", "@id", id);
        }

        public SinhVienModel? GetByMaNguoiDung(int maNguoiDung)
        {
            return QuerySingle(" WHERE MaNguoiDung = @id AND DaXoa = 0", "@id", maNguoiDung);
        }

        public bool Update(SinhVienModel model)
        {
            using (var conn = KetNoi.GetConnection())
            {
                var cmd = new SqlCommand(@"UPDATE SinhVien
                                           SET NgaySinh = @ngaysinh, Lop = @lop, NganhHoc = @nganh, DiaChi = @diachi
                                           WHERE MaSinhVien = @id AND DaXoa = 0", conn);
                cmd.Parameters.AddWithValue("@id", model.MaSinhVien);
                cmd.Parameters.Add("@ngaysinh", SqlDbType.Date).Value = model.NgaySinh.HasValue
                    ? model.NgaySinh.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
                cmd.Parameters.AddWithValue("@lop", (object?)model.Lop ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@nganh", (object?)model.NganhHoc ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@diachi", (object?)model.DiaChi ?? DBNull.Value);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        private static SinhVienModel? QuerySingle(string where, string paramName, int value)
        {
            using (var conn = KetNoi.GetConnection())
            {
                var cmd = new SqlCommand(SelectBase + where, conn);
                cmd.Parameters.AddWithValue(paramName, value);
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

        private static SinhVienModel MapFromReader(SqlDataReader reader)
        {
            return new SinhVienModel
            {
                MaSinhVien = Convert.ToInt32(reader["MaSinhVien"]),
                MaNguoiDung = Convert.ToInt32(reader["MaNguoiDung"]),
                NgaySinh = reader["NgaySinh"] != DBNull.Value ? DateOnly.FromDateTime(Convert.ToDateTime(reader["NgaySinh"])) : null,
                Lop = reader["Lop"] != DBNull.Value ? reader["Lop"].ToString() : null,
                NganhHoc = reader["NganhHoc"] != DBNull.Value ? reader["NganhHoc"].ToString() : null,
                DiaChi = reader["DiaChi"] != DBNull.Value ? reader["DiaChi"].ToString() : null,
                TrangThai = Convert.ToBoolean(reader["TrangThai"]),
                NgayTao = Convert.ToDateTime(reader["NgayTao"]),
                DaXoa = Convert.ToBoolean(reader["DaXoa"]),
                NgayCapNhat = reader["NgayCapNhat"] != DBNull.Value ? Convert.ToDateTime(reader["NgayCapNhat"]) : null
            };
        }
    }
}
