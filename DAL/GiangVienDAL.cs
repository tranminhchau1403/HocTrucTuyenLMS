using System;
using Microsoft.Data.SqlClient;
using Models;

namespace DAL
{
    public interface IGiangVienRepository
    {
        GiangVienModel? GetById(int id);
        GiangVienModel? GetByMaNguoiDung(int maNguoiDung);
        bool Update(GiangVienModel model);
    }

    public class GiangVienDAL : IGiangVienRepository
    {
        private const string SelectBase = @"
            SELECT MaGiangVien, MaNguoiDung, TrinhDo, ChuyenMon, Khoa,
                   TrangThai, NgayTao, DaXoa, NgayCapNhat
            FROM GiangVien";

        public GiangVienModel? GetById(int id)
        {
            return QuerySingle(" WHERE MaGiangVien = @id AND DaXoa = 0", "@id", id);
        }

        public GiangVienModel? GetByMaNguoiDung(int maNguoiDung)
        {
            return QuerySingle(" WHERE MaNguoiDung = @id AND DaXoa = 0", "@id", maNguoiDung);
        }

        public bool Update(GiangVienModel model)
        {
            using (var conn = KetNoi.GetConnection())
            {
                var cmd = new SqlCommand(@"UPDATE GiangVien
                                           SET TrinhDo = @trinhdo, ChuyenMon = @chuyenmon, Khoa = @khoa
                                           WHERE MaGiangVien = @id AND DaXoa = 0", conn);
                cmd.Parameters.AddWithValue("@id", model.MaGiangVien);
                cmd.Parameters.AddWithValue("@trinhdo", (object?)model.TrinhDo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@chuyenmon", (object?)model.ChuyenMon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@khoa", (object?)model.Khoa ?? DBNull.Value);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        private static GiangVienModel? QuerySingle(string where, string paramName, int value)
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

        private static GiangVienModel MapFromReader(SqlDataReader reader)
        {
            return new GiangVienModel
            {
                MaGiangVien = Convert.ToInt32(reader["MaGiangVien"]),
                MaNguoiDung = Convert.ToInt32(reader["MaNguoiDung"]),
                TrinhDo = reader["TrinhDo"] != DBNull.Value ? reader["TrinhDo"].ToString() : null,
                ChuyenMon = reader["ChuyenMon"] != DBNull.Value ? reader["ChuyenMon"].ToString() : null,
                Khoa = reader["Khoa"] != DBNull.Value ? reader["Khoa"].ToString() : null,
                TrangThai = Convert.ToBoolean(reader["TrangThai"]),
                NgayTao = Convert.ToDateTime(reader["NgayTao"]),
                DaXoa = Convert.ToBoolean(reader["DaXoa"]),
                NgayCapNhat = reader["NgayCapNhat"] != DBNull.Value ? Convert.ToDateTime(reader["NgayCapNhat"]) : null
            };
        }
    }
}
