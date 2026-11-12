/**
 * angular-app.js — Khởi tạo ứng dụng AngularJS, Interceptor bảo mật và bộ lọc cho CT LMS
 */

(function () {
  'use strict';

  var app = angular.module('lmsApp', []);

  // Cấu hình HTTP Interceptor tự động chèn JWT Bearer Token
  app.factory('JwtInterceptor', ['$q', function ($q) {
    return {
      request: function (config) {
        var token = sessionStorage.getItem(KHOA_PHIEN.token);
        if (token && !config.headers.Authorization) {
          config.headers.Authorization = 'Bearer ' + token;
        }
        return config;
      },
      response: function (response) {
        var newToken = response.headers('X-New-Access-Token') || response.headers('x-new-access-token');
        if (newToken) {
          sessionStorage.setItem(KHOA_PHIEN.token, newToken);
          sessionStorage.setItem(KHOA_PHIEN.thoi_gian, Date.now().toString());
        }
        return response;
      },
      responseError: function (rejection) {
        if (rejection.status === 401) {
          console.warn('Phiên làm việc hết hạn hoặc token không hợp lệ (401). Đang chuyển về trang Đăng nhập.');
          sessionStorage.removeItem(KHOA_PHIEN.nguoi_dung);
          sessionStorage.removeItem(KHOA_PHIEN.token);
          if (!window.location.pathname.endsWith('dang-nhap.html')) {
            window.location.replace('dang-nhap.html');
          }
        }
        return $q.reject(rejection);
      }
    };
  }]);

  app.config(['$httpProvider', function ($httpProvider) {
    $httpProvider.interceptors.push('JwtInterceptor');
  }]);

  // Bộ lọc định dạng tiền tệ VND
  app.filter('tienVND', function () {
    return function (giaTri) {
      if (giaTri === null || giaTri === undefined || isNaN(giaTri) || Number(giaTri) === 0) return 'Miễn phí';
      return Number(giaTri).toLocaleString('vi-VN') + ' ₫';
    };
  });

  // Bộ lọc định dạng ngày tháng yyyy-MM-dd -> dd/MM/yyyy
  app.filter('dinhDangNgay', function () {
    return function (chuoiNgay) {
      if (!chuoiNgay) return '';
      try {
        var d = new Date(chuoiNgay);
        if (isNaN(d.getTime())) return chuoiNgay;
        var ngay = ('0' + d.getDate()).slice(-2);
        var thang = ('0' + (d.getMonth() + 1)).slice(-2);
        var nam = d.getFullYear();
        return ngay + '/' + thang + '/' + nam;
      } catch (e) {
        return chuoiNgay;
      }
    };
  });

  // Bộ lọc định dạng ngày giờ dd/MM/yyyy HH:mm
  app.filter('dinhDangNgayGio', function () {
    return function (chuoiNgay) {
      if (!chuoiNgay) return '';
      try {
        var d = new Date(chuoiNgay);
        if (isNaN(d.getTime())) return chuoiNgay;
        var ngay = ('0' + d.getDate()).slice(-2);
        var thang = ('0' + (d.getMonth() + 1)).slice(-2);
        var nam = d.getFullYear();
        var gio = ('0' + d.getHours()).slice(-2);
        var phut = ('0' + d.getMinutes()).slice(-2);
        return ngay + '/' + thang + '/' + nam + ' ' + gio + ':' + phut;
      } catch (e) {
        return chuoiNgay;
      }
    };
  });

  // Bộ lọc cắt ngắn chuỗi
  app.filter('catNgan', function () {
    return function (vanBan, doDai) {
      if (!vanBan) return '';
      var max = doDai || 60;
      if (vanBan.length <= max) return vanBan;
      return vanBan.substring(0, max) + '...';
    };
  });

  // Bộ lọc trạng thái kích hoạt / khóa
  app.filter('nhanTrangThai', function () {
    return function (trangThai) {
      return trangThai ? 'Hoạt động' : 'Đã khóa';
    };
  });

})();
