/**
 * goi-api.js — Dịch vụ gọi API Gateway qua AngularJS $http cho Hệ Thống CT LMS
 */

(function () {
  'use strict';

  var app = angular.module('lmsApp');

  app.factory('ApiService', ['$http', function ($http) {
    return {
      // 1. Xác thực & Tài khoản
      dangNhap: function (data) {
        return $http.post(TIEN_TO_API.login + '/DangNhap', data);
      },
      lamMoiToken: function (data) {
        return $http.post(TIEN_TO_API.login + '/LamMoiToken', data);
      },
      thuHoiToken: function (data) {
        return $http.post(TIEN_TO_API.login + '/ThuHoiToken', data);
      },
      dangKyHocVien: function (data) {
        return $http.post(TIEN_TO_API.login + '/DangKyHocVien', data);
      },
      layThongTin: function () {
        return $http.get(TIEN_TO_API.login + '/LayThongTin');
      },
      doiMatKhau: function (data) {
        return $http.post(TIEN_TO_API.login + '/DoiMatKhau', data);
      },

      // 2. Thông báo
      layThongBaoCuaToi: function () {
        return $http.get(TIEN_TO_API.thongbao + '/CuaToi');
      },
      danhDauDaDoc: function (id) {
        return $http.put(TIEN_TO_API.thongbao + '/DaDoc/' + id);
      },

      // 3. Admin: Báo cáo, Phê duyệt, Quản lý người dùng, Danh mục
      layBaoCaoTongQuan: function () {
        return $http.get(TIEN_TO_API.admin + '/BaoCao/TongQuan');
      },
      xuatExcelBaoCaoTongQuan: function () {
        return TIEN_TO_API.admin + '/BaoCao/XuatExcel';
      },
      pheDuyetKhoaHoc: function (maKh, data) {
        return $http.put(TIEN_TO_API.admin + '/BaoCao/PheDuyetKhoaHoc/' + maKh, data);
      },
      layNhatKyKiemToan: function (params) {
        return $http.get(TIEN_TO_API.admin + '/BaoCao/NhatKyKiemToan', { params: params });
      },
      layDanhSachNguoiDung: function (params) {
        return $http.get(TIEN_TO_API.admin + '/NguoiDung/LayDanhSach', { params: params });
      },
      doiTrangThaiNguoiDung: function (id, data) {
        return $http.put(TIEN_TO_API.admin + '/NguoiDung/DoiTrangThai/' + id, data);
      },
      layDanhSachDanhMuc: function () {
        return $http.get(TIEN_TO_API.admin + '/DanhMuc/LayDanhSach');
      },
      themDanhMuc: function (data) {
        return $http.post(TIEN_TO_API.admin + '/DanhMuc/Them', data);
      },
      suaDanhMuc: function (id, data) {
        return $http.put(TIEN_TO_API.admin + '/DanhMuc/Sua/' + id, data);
      },
      xoaDanhMuc: function (id) {
        return $http.delete(TIEN_TO_API.admin + '/DanhMuc/Xoa/' + id);
      },

      // 4. Giảng viên: Quản lý Khóa học, Gửi duyệt, Bài giảng, Ngân hàng đề, Bảng điểm
      layKhoaHocCuaToiGV: function (params) {
        return $http.get(TIEN_TO_API.giangvien + '/KhoaHoc/LayDanhSachCuaToi', { params: params });
      },
      taoKhoaHoc: function (data) {
        return $http.post(TIEN_TO_API.giangvien + '/KhoaHoc/TaoKhoaHoc', data);
      },
      suaKhoaHoc: function (id, data) {
        return $http.put(TIEN_TO_API.giangvien + '/KhoaHoc/Sua/' + id, data);
      },
      guiDuyetKhoaHoc: function (id) {
        return $http.post(TIEN_TO_API.giangvien + '/KhoaHoc/GuiDuyet/' + id);
      },
      xoaKhoaHoc: function (id) {
        return $http.delete(TIEN_TO_API.giangvien + '/KhoaHoc/Xoa/' + id);
      },
      layBaiGiangTheoKhoaHoc: function (maKh, maDk) {
        var url = TIEN_TO_API.giangvien + '/BaiGiang/LayTheoKhoaHoc/' + maKh;
        if (maDk) url += '?maDangKy=' + maDk;
        return $http.get(url);
      },
      themBaiGiang: function (data) {
        return $http.post(TIEN_TO_API.giangvien + '/BaiGiang/Them', data);
      },
      suaBaiGiang: function (id, data) {
        return $http.put(TIEN_TO_API.giangvien + '/BaiGiang/Sua/' + id, data);
      },
      xoaBaiGiang: function (id) {
        return $http.delete(TIEN_TO_API.giangvien + '/BaiGiang/Xoa/' + id);
      },
      layBaiTestTheoKhoaHoc: function (maKh) {
        return $http.get(TIEN_TO_API.giangvien + '/BaiKiemTra/LayTheoKhoaHoc/' + maKh);
      },
      layCauHoiBaiTest: function (maBkt) {
        return $http.get(TIEN_TO_API.giangvien + '/BaiKiemTra/LayCauHoi/' + maBkt);
      },
      themCauHoiBaiTest: function (data) {
        return $http.post(TIEN_TO_API.giangvien + '/BaiKiemTra/ThemCauHoi', data);
      },
      taoBaiTest: function (data) {
        return $http.post(TIEN_TO_API.giangvien + '/BaiKiemTra/TaoBaiKiemTra', data);
      },
      layBangDiemKhoaHoc: function (maKh, params) {
        return $http.get(TIEN_TO_API.giangvien + '/BangDiem/LayKetQuaTheoKhoaHoc/' + maKh, { params: params });
      },
      xuatExcelBangDiemKhoaHoc: function (maKh) {
        return TIEN_TO_API.giangvien + '/BangDiem/XuatExcel/' + maKh;
      },

      // 5. Học viên: Khám phá, Ghi danh, Tuần tự, Chống gian lận, Chứng chỉ
      traCuuKhoaHoc: function (params) {
        return $http.get(TIEN_TO_API.sinhvien + '/KhoaHoc/TraCuu', { params: params });
      },
      layChiTietKhoaHoc: function (id) {
        return $http.get(TIEN_TO_API.sinhvien + '/KhoaHoc/ChiTiet/' + id);
      },
      ghiDanhKhoaHoc: function (maKh) {
        return $http.post(TIEN_TO_API.sinhvien + '/DangKyKhoaHoc/GhiDanh/' + maKh);
      },
      layKhoaHocCuaToiHV: function (params) {
        return $http.get(TIEN_TO_API.sinhvien + '/DangKyKhoaHoc/KhoaHocCuaToi', { params: params });
      },
      hoanThanhBaiGiangTuanTu: function (data) {
        return $http.post(TIEN_TO_API.sinhvien + '/DangKyKhoaHoc/HoanThanhBaiGiang', data);
      },
      capNhatTienDo: function (maDk, data) {
        return $http.put(TIEN_TO_API.sinhvien + '/DangKyKhoaHoc/CapNhatTienDo/' + maDk, data);
      },
      layBaiTestTheoKhoaHocHV: function (maKh) {
        return $http.get(TIEN_TO_API.sinhvien + '/BaiKiemTra/LayDanhSach/' + maKh);
      },
      batDauPhienLamBai: function (maBkt, maDk) {
        return $http.post(TIEN_TO_API.sinhvien + '/BaiKiemTra/BatDau/' + maBkt + '?maDangKy=' + maDk);
      },
      nopBaiTest: function (data) {
        return $http.post(TIEN_TO_API.sinhvien + '/KetQuaKiemTra/NopBai', data);
      },
      layLichSuLamBai: function (maDk) {
        return $http.get(TIEN_TO_API.sinhvien + '/KetQuaKiemTra/LichSuLamBai/' + maDk);
      },
      xuatChungChi: function (maDk) {
        return $http.get(TIEN_TO_API.sinhvien + '/ChungChi/XuatChungChi/' + maDk);
      },
      taiPdfChungChiUrl: function (maDk) {
        return TIEN_TO_API.sinhvien + '/ChungChi/TaiPdf/' + maDk;
      },
      xacThucChungChiCongKhai: function (code) {
        return $http.get(TIEN_TO_API.chungchi + '/XacThuc/' + encodeURIComponent(code));
      }
    };
  }]);

})();
