/**
 * xac-thuc.js — Tiện ích quản lý phiên đăng nhập và bảo vệ trang
 * Hệ Thống Quản Lý Học Trực Tuyến LMS (CT LMS)
 */

const XacThuc = {
  // Lấy thông tin người dùng từ sessionStorage
  layNguoiDung: function() {
    try {
      const data = sessionStorage.getItem(KHOA_PHIEN.nguoi_dung);
      return data ? JSON.parse(data) : null;
    } catch (e) {
      return null;
    }
  },

  // Lấy JWT Bearer token
  layToken: function() {
    return sessionStorage.getItem(KHOA_PHIEN.token) || '';
  },

  // Lấy Refresh Token
  layRefreshToken: function() {
    return sessionStorage.getItem(KHOA_PHIEN.refresh_token) || localStorage.getItem(KHOA_PHIEN.refresh_token) || '';
  },

  // Lưu phiên đăng nhập (Hỗ trợ Refresh Token xoay vòng)
  luuPhien: function(user, token, refreshToken) {
    if (user) {
      let rawRole = (user.quyenHan || user.vaiTro || user.role || 'Học viên').toString().trim();
      let roleCode = 'SinhVien';
      let roleDisplay = 'Học viên';

      if (rawRole.toLowerCase() === 'admin') {
        roleCode = 'Admin';
        roleDisplay = 'Admin';
      } else if (rawRole.toLowerCase().includes('giangvien') || rawRole.toLowerCase().includes('giảng viên')) {
        roleCode = 'GiangVien';
        roleDisplay = 'Giảng viên';
      } else {
        roleCode = 'SinhVien';
        roleDisplay = 'Học viên';
      }

      user.vaiTro = roleCode;
      user.quyenHan = roleDisplay;

      sessionStorage.setItem(KHOA_PHIEN.nguoi_dung, JSON.stringify(user));
    }
    if (token) {
      sessionStorage.setItem(KHOA_PHIEN.token, token);
    }
    if (refreshToken) {
      sessionStorage.setItem(KHOA_PHIEN.refresh_token, refreshToken);
      localStorage.setItem(KHOA_PHIEN.refresh_token, refreshToken);
    }
    sessionStorage.setItem(KHOA_PHIEN.thoi_gian, Date.now().toString());
  },

  // Tự động làm mới Access Token khi token hết hạn
  lamMoiToken: function() {
    const rawRefresh = this.layRefreshToken();
    if (!rawRefresh) return Promise.reject('Không có Refresh Token.');

    return fetch(TIEN_TO_API.login + '/LamMoiToken', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: rawRefresh })
    })
    .then(res => res.json())
    .then(data => {
      if (data && data.success && data.data) {
        const auth = data.data;
        this.luuPhien(auth, auth.accessToken, auth.refreshToken);
        return auth.accessToken;
      }
      throw new Error(data.message || 'Làm mới phiên thất bại.');
    });
  },

  // Kiểm tra quyền truy cập của trang hiện tại
  kiemTraQuyen: function(cacVaiTroChoPhep) {
    const user = this.layNguoiDung();
    if (!user) {
      window.location.replace('dang-nhap.html');
      return null;
    }

    let userRole = user.quyenHan || user.vaiTro || 'Học viên';

    if (cacVaiTroChoPhep && cacVaiTroChoPhep.length > 0) {
      const hopLe = cacVaiTroChoPhep.some(function(r) {
        return r.toLowerCase() === userRole.toLowerCase() ||
               (r === 'GiangVien' && userRole === 'Giảng viên') ||
               (r === 'SinhVien' && userRole === 'Học viên');
      });

      if (!hopLe) {
        alert('Bạn không có quyền truy cập vào chức năng này!');
        window.location.replace('tong-quan.html');
        return null;
      }
    }
    return user;
  },

  // Kiểm tra xem đã đăng nhập chưa, nếu chưa thì chuyển về trang đăng nhập
  kiemTraDangNhap: function() {
    const user = this.layNguoiDung();
    if (!user) {
      window.location.replace('dang-nhap.html');
      return false;
    }
    return true;
  },

  // Đăng xuất khỏi hệ thống
  dangXuat: function() {
    const rf = this.layRefreshToken();
    if (rf) {
      try {
        fetch(TIEN_TO_API.login + '/ThuHoiToken', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ refreshToken: rf })
        });
      } catch (e) {}
    }

    sessionStorage.removeItem(KHOA_PHIEN.nguoi_dung);
    sessionStorage.removeItem(KHOA_PHIEN.token);
    sessionStorage.removeItem(KHOA_PHIEN.refresh_token);
    sessionStorage.removeItem(KHOA_PHIEN.thoi_gian);
    localStorage.removeItem(KHOA_PHIEN.refresh_token);
    window.location.replace('dang-nhap.html');
  }
};
