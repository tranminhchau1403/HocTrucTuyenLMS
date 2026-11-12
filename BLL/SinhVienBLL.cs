using System;
using DAL;
using Models;

namespace BLL
{
    public class SinhVienBLL
    {
        private readonly ISinhVienRepository _dal;
        private readonly NguoiDungBLL _nguoiDungBLL;

        public SinhVienBLL(ISinhVienRepository? dal = null, NguoiDungBLL? nguoiDungBLL = null)
        {
            _dal = dal ?? new SinhVienDAL();
            _nguoiDungBLL = nguoiDungBLL ?? new NguoiDungBLL();
        }

        public SinhVienModel? GetById(int id)
        {
            if (id <= 0) return null;
            return _dal.GetById(id);
        }

        public SinhVienModel? GetByMaNguoiDung(int maNguoiDung)
        {
            if (maNguoiDung <= 0) return null;
            return _dal.GetByMaNguoiDung(maNguoiDung);
        }

        public bool UpdateHoSo(int maNguoiDung, UpdateStudentProfileRequest request)
        {
            var sinhVien = GetByMaNguoiDung(maNguoiDung);
            if (sinhVien == null)
                throw new InvalidOperationException("Không tìm thấy hồ sơ sinh viên.");

            _nguoiDungBLL.UpdateThongTinChung(maNguoiDung, request.HoTen, request.Email, request.SoDienThoai);

            sinhVien.NgaySinh = request.NgaySinh;
            sinhVien.Lop = request.Lop?.Trim();
            sinhVien.NganhHoc = request.NganhHoc?.Trim();
            sinhVien.DiaChi = request.DiaChi?.Trim();
            return _dal.Update(sinhVien);
        }
    }
}
