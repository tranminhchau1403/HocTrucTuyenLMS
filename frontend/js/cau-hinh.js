/**
 * cau-hinh.js — Cấu hình toàn cục của Hệ thống quản lý học trực tuyến (LMS)
 */

// Base URL kết nối qua API Gateway (Ocelot)
const BASE_URL = 'http://localhost:5200';

// Tiền tố định tuyến Ocelot Gateway
const TIEN_TO_API = {
  login:     BASE_URL + '/gateway/XacThuc',
  admin:     BASE_URL + '/gateway',
  giangvien: BASE_URL + '/gateway/GiangVien',
  sinhvien:  BASE_URL + '/gateway/SinhVien',
  thongbao:  BASE_URL + '/gateway/ThongBao',
  chungchi:  BASE_URL + '/gateway/ChungChi'
};

// Khóa lưu phiên trong sessionStorage / localStorage
const KHOA_PHIEN = {
  nguoi_dung: 'current_user',
  token: 'access_token',
  refresh_token: 'refresh_token',
  thoi_gian: 'token_received_at'
};

// Ánh xạ Quyền
const QUYEN_ROLE = {
  Admin: 'Admin',
  'Giảng viên': 'GiangVien',
  'Học viên': 'SinhVien'
};

// Tên hiển thị tiếng Việt của các vai trò
const TEN_VAI_TRO = {
  Admin: 'Quản trị viên',
  'Giảng viên': 'Giảng viên',
  'Học viên': 'Học viên / Sinh viên'
};

// Màu sắc badge định danh vai trò
const MAU_BADGE_VAI_TRO = {
  Admin: 'badge-do',
  'Giảng viên': 'badge-lam',
  'Học viên': 'badge-xanh'
};

// Cấu hình menu top navbar thanh ngang chuẩn CT LMS
const CAU_HINH_MENU = {
  Admin: [
    {
      nhom: 'Quản trị hệ thống',
      muc: [
        { id: 'tong-quan', nhan: 'Bảng điều khiển', file: 'tong-quan.html', icon: 'bi-speedometer2', tooltip: 'Tổng quan hệ thống LMS' },
        { id: 'quan-ly-khoa-hoc', nhan: 'Quản lý khóa học', file: 'quan-ly-khoa-hoc.html', icon: 'bi-journal-bookmark', tooltip: 'Quản lý khóa học & Duyệt' },
        { id: 'quan-ly-danh-muc', nhan: 'Danh mục khóa học', file: 'quan-ly-danh-muc.html', icon: 'bi-folder', tooltip: 'Danh mục' },
        { id: 'quan-ly-tai-khoan', nhan: 'Quản lý tài khoản', file: 'quan-ly-tai-khoan.html', icon: 'bi-people', tooltip: 'Người dùng & phân quyền' },
        { id: 'tra-cuu-khoa-hoc', nhan: 'Khám phá khóa học', file: 'tra-cuu-khoa-hoc.html', icon: 'bi-search', tooltip: 'Xem thư viện' }
      ]
    }
  ],
  'Giảng viên': [
    {
      nhom: 'Đào tạo giảng dạy',
      muc: [
        { id: 'tong-quan', nhan: 'Bảng điều khiển', file: 'tong-quan.html', icon: 'bi-speedometer2', tooltip: 'Tổng quan giảng viên' },
        { id: 'quan-ly-khoa-hoc', nhan: 'Khóa học của tôi', file: 'quan-ly-khoa-hoc.html', icon: 'bi-journal-bookmark', tooltip: 'Khóa học phụ trách' },
        { id: 'tra-cuu-khoa-hoc', nhan: 'Khám phá khóa học', file: 'tra-cuu-khoa-hoc.html', icon: 'bi-search', tooltip: 'Thư viện khóa học' },
        { id: 'thong-bao', nhan: 'Thông báo', file: 'thong-bao.html', icon: 'bi-bell', tooltip: 'Thông báo' }
      ]
    }
  ],
  'Học viên': [
    {
      nhom: 'Góc học tập CT LMS',
      muc: [
        { id: 'tong-quan', nhan: 'Bảng điều khiển', file: 'tong-quan.html', icon: 'bi-speedometer2', tooltip: 'Tổng quan học viên' },
        { id: 'tra-cuu-khoa-hoc', nhan: 'Khám phá khóa học', file: 'tra-cuu-khoa-hoc.html', icon: 'bi-compass', tooltip: 'Tìm kiếm khóa học' },
        { id: 'khoa-hoc-cua-toi', nhan: 'Khóa học đã đăng ký', file: 'khoa-hoc-cua-toi.html', icon: 'bi-play-circle', tooltip: 'Đang theo học' },
        { id: 'lam-bai-test', nhan: 'Làm bài kiểm tra', file: 'lam-bai-test.html', icon: 'bi-pencil-square', tooltip: 'Kiểm tra chống gian lận' },
        { id: 'chung-chi', nhan: 'Chứng chỉ', file: 'chung-chi.html', icon: 'bi-award', tooltip: 'Chứng chỉ tốt nghiệp & Mã QR' }
      ]
    }
  ]
};
