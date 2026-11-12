using System;
using DAL;
using Models;

namespace BLL
{
    public class GiangVienBLL
    {
        private readonly IGiangVienRepository _dal;
        private readonly NguoiDungBLL _nguoiDungBLL;

        public GiangVienBLL(IGiangVienRepository? dal = null, NguoiDungBLL? nguoiDungBLL = null)
        {
            _dal = dal ?? new GiangVienDAL();
            _nguoiDungBLL = nguoiDungBLL ?? new NguoiDungBLL();
        }

        public GiangVienModel? GetById(int id)
        {
            if (id <= 0) return null;
            return _dal.GetById(id);
        }

        public GiangVienModel? GetByMaNguoiDung(int maNguoiDung)
        {
            if (maNguoiDung <= 0) return null;
            return _dal.GetByMaNguoiDung(maNguoiDung);
        }

        public bool UpdateHoSo(int maNguoiDung, UpdateTeacherProfileRequest request)
        {
            var giangVien = GetByMaNguoiDung(maNguoiDung);
            if (giangVien == null)
                throw new InvalidOperationException("Không tìm thấy hồ sơ giảng viên.");

            _nguoiDungBLL.UpdateThongTinChung(maNguoiDung, request.HoTen, request.Email, request.SoDienThoai);

            giangVien.TrinhDo = request.TrinhDo?.Trim();
            giangVien.ChuyenMon = request.ChuyenMon?.Trim();
            giangVien.Khoa = request.Khoa?.Trim();
            return _dal.Update(giangVien);
        }
    }
}
