using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BLL.BackgroundJobs;
using BLL.Security;
using DAL;
using Models;

namespace BLL
{
    public class NguoiDungBLL
    {
        private const string QuyenAdmin = "Admin";
        private const string QuyenGiangVien = "Giảng viên";
        private const string QuyenHocVien = "Học viên";

        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly INguoiDungRepository _dal;
        private readonly ISinhVienRepository _sinhVienDAL;
        private readonly IGiangVienRepository _giangVienDAL;
        private readonly IRefreshTokenRepository _refreshTokenDAL;
        private readonly IAuditLogRepository _auditLogDAL;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IBackgroundTaskQueue _backgroundQueue;

        public NguoiDungBLL(
            INguoiDungRepository? dal = null,
            ISinhVienRepository? sinhVienDAL = null,
            IGiangVienRepository? giangVienDAL = null,
            IRefreshTokenRepository? refreshTokenDAL = null,
            IAuditLogRepository? auditLogDAL = null,
            IJwtTokenService? jwtTokenService = null,
            IBackgroundTaskQueue? backgroundQueue = null)
        {
            _dal = dal ?? new NguoiDungDAL();
            _sinhVienDAL = sinhVienDAL ?? new SinhVienDAL();
            _giangVienDAL = giangVienDAL ?? new GiangVienDAL();
            _refreshTokenDAL = refreshTokenDAL ?? new RefreshTokenDAL();
            _auditLogDAL = auditLogDAL ?? new AuditLogDAL();
            _jwtTokenService = jwtTokenService ?? new JwtTokenService();
            _backgroundQueue = backgroundQueue ?? BackgroundTaskQueue.Shared;
        }

        public PagedResult<NguoiDungModel> GetAll(
            string? quyenHan,
            string? tuKhoa,
            int trang,
            int kichThuocTrang,
            string? sortBy = null,
            string? sortOrder = null)
        {
            if (!string.IsNullOrWhiteSpace(quyenHan) && !IsQuyenHopLe(quyenHan.Trim()))
                throw new ArgumentException("Quyền hạn không hợp lệ.");

            if (trang < 1) trang = 1;
            if (kichThuocTrang < 1) kichThuocTrang = 20;
            if (kichThuocTrang > 100) kichThuocTrang = 100;

            return _dal.GetAll(
                string.IsNullOrWhiteSpace(quyenHan) ? null : quyenHan.Trim(),
                tuKhoa,
                trang,
                kichThuocTrang,
                sortBy,
                sortOrder);
        }

        public NguoiDungModel? GetById(int id)
        {
            if (id <= 0) return null;
            return _dal.GetById(id);
        }

        public NguoiDungModel? GetByTenDangNhap(string tenDangNhap)
        {
            if (string.IsNullOrWhiteSpace(tenDangNhap)) return null;
            return _dal.GetByTenDangNhap(tenDangNhap.Trim());
        }

        public NguoiDungModel? Login(string tenDangNhap, string matKhau)
        {
            if (string.IsNullOrWhiteSpace(tenDangNhap) || string.IsNullOrWhiteSpace(matKhau))
                return null;

            var user = _dal.GetByTenDangNhap(tenDangNhap.Trim());
            if (user == null || !user.TrangThai || user.DaXoa)
                return null;

            if (!PasswordHasher.VerifyPassword(matKhau, user.MatKhau, out bool needsRehash))
                return null;

            // Tự động nâng cấp mật khẩu plaintext cũ lên BCrypt Hash ngay khi đăng nhập thành công
            if (needsRehash)
            {
                string upgradedHash = PasswordHasher.HashPassword(matKhau);
                _dal.ChangePassword(user.MaNguoiDung, upgradedHash);
                user.MatKhau = upgradedHash;
            }

            return user;
        }

        public AuthResponse IssueAuthResponse(NguoiDungModel user, string? remoteIp = null)
        {
            int? maSinhVien = null;
            int? maGiangVien = null;
            if (user.QuyenHan == QuyenHocVien)
            {
                maSinhVien = _sinhVienDAL.GetByMaNguoiDung(user.MaNguoiDung)?.MaSinhVien;
            }
            else if (user.QuyenHan == QuyenGiangVien)
            {
                maGiangVien = _giangVienDAL.GetByMaNguoiDung(user.MaNguoiDung)?.MaGiangVien;
            }

            var (accessToken, jwtId, expiry) = _jwtTokenService.GenerateAccessToken(user, maSinhVien, maGiangVien);
            var (rawRefresh, refreshHash, refreshExpiresUtc) = _jwtTokenService.GenerateRefreshToken();

            _refreshTokenDAL.Insert(new RefreshTokenModel
            {
                MaNguoiDung = user.MaNguoiDung,
                TokenHash = refreshHash,
                JwtId = jwtId,
                ExpiresAtUtc = refreshExpiresUtc,
                RemoteIpAddress = remoteIp
            });

            _auditLogDAL.Insert(new AuditLogModel
            {
                MaNguoiDung = user.MaNguoiDung,
                TenDangNhap = user.TenDangNhap,
                HanhDong = "LOGIN",
                TenBang = "NguoiDung",
                MaBanGhi = user.MaNguoiDung.ToString(),
                DiaChiIp = remoteIp
            });

            return new AuthResponse
            {
                AccessToken = accessToken,
                Expiration = expiry,
                RefreshToken = rawRefresh,
                RefreshTokenExpiration = refreshExpiresUtc.ToLocalTime(),
                MaNguoiDung = user.MaNguoiDung,
                TenDangNhap = user.TenDangNhap,
                HoTen = user.HoTen,
                Email = user.Email,
                QuyenHan = user.QuyenHan,
                MaSinhVien = maSinhVien,
                MaGiangVien = maGiangVien
            };
        }

        /// <summary>
        /// Xoay vòng Refresh Token (Refresh Token Rotation) chuẩn Bài 10 Giáo trình:
        /// Phát hiện token bị dùng lại (Reuse Detection) và tự động thu hồi toàn bộ phiên nếu có dấu hiệu đánh cắp
        /// </summary>
        public AuthResponse RefreshAccessToken(string rawRefreshToken, string? remoteIp = null)
        {
            if (string.IsNullOrWhiteSpace(rawRefreshToken))
                throw new UnauthorizedAppException("Refresh Token không được để trống.");

            string tokenHash = _jwtTokenService.ComputeSha256Hash(rawRefreshToken);
            var storedToken = _refreshTokenDAL.GetByTokenHash(tokenHash);
            if (storedToken == null)
                throw new UnauthorizedAppException("Refresh Token không hợp lệ.");

            if (storedToken.IsRevoked)
            {
                // Phát hiện Reuse Attack -> Thu hồi toàn bộ Refresh Token của người dùng này
                _refreshTokenDAL.RevokeAllUserTokens(storedToken.MaNguoiDung, "Phát hiện sử dụng lại Refresh Token đã bị thu hồi (Reuse Detection).");
                throw new UnauthorizedAppException("Phiên đăng nhập không an toàn đã bị thu hồi. Vui lòng đăng nhập lại.");
            }

            if (storedToken.IsExpired)
                throw new UnauthorizedAppException("Refresh Token đã hết hạn. Vui lòng đăng nhập lại.");

            var user = _dal.GetById(storedToken.MaNguoiDung);
            if (user == null || !user.TrangThai || user.DaXoa)
                throw new UnauthorizedAppException("Tài khoản đã bị khóa hoặc không tồn tại.");

            int? maSinhVien = user.QuyenHan == QuyenHocVien ? _sinhVienDAL.GetByMaNguoiDung(user.MaNguoiDung)?.MaSinhVien : null;
            int? maGiangVien = user.QuyenHan == QuyenGiangVien ? _giangVienDAL.GetByMaNguoiDung(user.MaNguoiDung)?.MaGiangVien : null;

            var (newAccessToken, newJwtId, newExpiry) = _jwtTokenService.GenerateAccessToken(user, maSinhVien, maGiangVien);
            var (newRawRefresh, newRefreshHash, newRefreshExpiresUtc) = _jwtTokenService.GenerateRefreshToken();

            // Đánh dấu token cũ đã được thay thế bởi token mới
            _refreshTokenDAL.RevokeToken(tokenHash, newRefreshHash, "Rotated");

            _refreshTokenDAL.Insert(new RefreshTokenModel
            {
                MaNguoiDung = user.MaNguoiDung,
                TokenHash = newRefreshHash,
                JwtId = newJwtId,
                ExpiresAtUtc = newRefreshExpiresUtc,
                RemoteIpAddress = remoteIp
            });

            return new AuthResponse
            {
                AccessToken = newAccessToken,
                Expiration = newExpiry,
                RefreshToken = newRawRefresh,
                RefreshTokenExpiration = newRefreshExpiresUtc.ToLocalTime(),
                MaNguoiDung = user.MaNguoiDung,
                TenDangNhap = user.TenDangNhap,
                HoTen = user.HoTen,
                Email = user.Email,
                QuyenHan = user.QuyenHan,
                MaSinhVien = maSinhVien,
                MaGiangVien = maGiangVien
            };
        }

        public bool RevokeRefreshToken(string rawRefreshToken, string? reason = null)
        {
            if (string.IsNullOrWhiteSpace(rawRefreshToken))
                return false;

            string tokenHash = _jwtTokenService.ComputeSha256Hash(rawRefreshToken);
            return _refreshTokenDAL.RevokeToken(tokenHash, null, reason ?? "User Logout");
        }

        public UserProfileDto? GetProfile(int maNguoiDung)
        {
            var user = GetById(maNguoiDung);
            if (user == null) return null;

            var profile = new UserProfileDto
            {
                MaNguoiDung = user.MaNguoiDung,
                TenDangNhap = user.TenDangNhap,
                HoTen = user.HoTen,
                Email = user.Email,
                SoDienThoai = user.SoDienThoai,
                QuyenHan = user.QuyenHan,
                NgayTao = user.NgayTao
            };

            if (user.QuyenHan == QuyenHocVien)
            {
                var sv = _sinhVienDAL.GetByMaNguoiDung(user.MaNguoiDung);
                if (sv != null)
                {
                    profile.MaSinhVien = sv.MaSinhVien;
                    profile.NgaySinh = sv.NgaySinh;
                    profile.Lop = sv.Lop;
                    profile.NganhHoc = sv.NganhHoc;
                    profile.DiaChi = sv.DiaChi;
                }
            }
            else if (user.QuyenHan == QuyenGiangVien)
            {
                var gv = _giangVienDAL.GetByMaNguoiDung(user.MaNguoiDung);
                if (gv != null)
                {
                    profile.MaGiangVien = gv.MaGiangVien;
                    profile.TrinhDo = gv.TrinhDo;
                    profile.ChuyenMon = gv.ChuyenMon;
                    profile.Khoa = gv.Khoa;
                }
            }

            return profile;
        }

        public (int MaNguoiDung, int MaSinhVien) DangKyHocVien(RegisterStudentRequest request)
        {
            ValidateTaiKhoan(request.TenDangNhap, request.MatKhau, request.HoTen, request.Email, request.SoDienThoai);
            KiemTraTrungLap(request.TenDangNhap, request.Email);

            var nguoiDung = new NguoiDungModel
            {
                TenDangNhap = request.TenDangNhap.Trim(),
                MatKhau = PasswordHasher.HashPassword(request.MatKhau),
                HoTen = request.HoTen.Trim(),
                Email = request.Email.Trim(),
                SoDienThoai = request.SoDienThoai?.Trim(),
                QuyenHan = QuyenHocVien,
                TrangThai = true
            };
            var sinhVien = new SinhVienModel
            {
                NgaySinh = request.NgaySinh,
                Lop = request.Lop?.Trim(),
                NganhHoc = request.NganhHoc?.Trim(),
                DiaChi = request.DiaChi?.Trim()
            };

            var res = _dal.InsertSinhVien(nguoiDung, sinhVien);

            _backgroundQueue.TryQueueEmailNotification(
                nguoiDung.Email,
                "Chào mừng bạn gia nhập hệ thống học trực tuyến CT LMS",
                $"<p>Xin chào <strong>{nguoiDung.HoTen}</strong>,</p><p>Tài khoản học viên <code>{nguoiDung.TenDangNhap}</code> của bạn đã được kích hoạt thành công.</p>");

            return res;
        }

        public (int MaNguoiDung, int MaGiangVien) DangKyGiangVien(RegisterTeacherRequest request)
        {
            ValidateTaiKhoan(request.TenDangNhap, request.MatKhau, request.HoTen, request.Email, request.SoDienThoai);
            KiemTraTrungLap(request.TenDangNhap, request.Email);

            var nguoiDung = new NguoiDungModel
            {
                TenDangNhap = request.TenDangNhap.Trim(),
                MatKhau = PasswordHasher.HashPassword(request.MatKhau),
                HoTen = request.HoTen.Trim(),
                Email = request.Email.Trim(),
                SoDienThoai = request.SoDienThoai?.Trim(),
                QuyenHan = QuyenGiangVien,
                TrangThai = true
            };
            var giangVien = new GiangVienModel
            {
                TrinhDo = request.TrinhDo?.Trim(),
                ChuyenMon = request.ChuyenMon?.Trim(),
                Khoa = request.Khoa?.Trim()
            };

            var res = _dal.InsertGiangVien(nguoiDung, giangVien);

            _backgroundQueue.TryQueueEmailNotification(
                nguoiDung.Email,
                "Tài khoản Giảng viên CT LMS đã được khởi tạo",
                $"<p>Xin chào Giảng viên <strong>{nguoiDung.HoTen}</strong>,</p><p>Tài khoản giảng dạy <code>{nguoiDung.TenDangNhap}</code> của bạn đã sẵn sàng.</p>");

            return res;
        }

        public bool ChangePassword(int id, string oldPassword, string newPassword)
        {
            if (id <= 0) return false;
            if (string.IsNullOrWhiteSpace(oldPassword))
                throw new ArgumentException("Mật khẩu cũ không được để trống.");
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException("Mật khẩu mới không được để trống.");
            if (newPassword.Trim().Length < 6)
                throw new ArgumentException("Mật khẩu mới phải có ít nhất 6 ký tự.");

            var user = _dal.GetById(id);
            if (user == null || !PasswordHasher.VerifyPassword(oldPassword, user.MatKhau, out _))
                throw new InvalidOperationException("Mật khẩu cũ không chính xác.");

            string newHash = PasswordHasher.HashPassword(newPassword);
            bool ok = _dal.ChangePassword(id, newHash);
            if (ok)
            {
                _refreshTokenDAL.RevokeAllUserTokens(id, "Đổi mật khẩu");
                _auditLogDAL.Insert(new AuditLogModel
                {
                    MaNguoiDung = id,
                    TenDangNhap = user.TenDangNhap,
                    HanhDong = "CHANGE_PASSWORD",
                    TenBang = "NguoiDung",
                    MaBanGhi = id.ToString()
                });
            }
            return ok;
        }

        public bool UpdateThongTinChung(int maNguoiDung, string hoTen, string email, string? soDienThoai)
        {
            if (maNguoiDung <= 0)
                throw new ArgumentException("Mã người dùng không hợp lệ.");
            ValidateThongTinChung(hoTen, email, soDienThoai);

            if (_dal.ExistsEmail(email.Trim(), maNguoiDung))
                throw new InvalidOperationException("Email đã được sử dụng.");

            return _dal.Update(new NguoiDungModel
            {
                MaNguoiDung = maNguoiDung,
                HoTen = hoTen.Trim(),
                Email = email.Trim(),
                SoDienThoai = soDienThoai?.Trim()
            });
        }

        public bool UpdateTrangThai(int id, bool trangThai, int maNguoiThucHien)
        {
            if (id <= 0) return false;
            if (id == maNguoiThucHien)
                throw new InvalidOperationException("Không thể tự khóa/mở khóa tài khoản của chính mình.");

            bool ok = _dal.UpdateTrangThai(id, trangThai);
            if (ok)
            {
                if (!trangThai)
                {
                    _refreshTokenDAL.RevokeAllUserTokens(id, "Tài khoản bị khóa bởi Admin");
                }
                _auditLogDAL.Insert(new AuditLogModel
                {
                    MaNguoiDung = maNguoiThucHien,
                    HanhDong = trangThai ? "UNLOCK_USER" : "LOCK_USER",
                    TenBang = "NguoiDung",
                    MaBanGhi = id.ToString(),
                    DuLieuMoi = $"TrangThai={trangThai}"
                });
            }
            return ok;
        }

        public bool Delete(int id, int maNguoiThucHien)
        {
            if (id <= 0) return false;
            if (id == maNguoiThucHien)
                throw new InvalidOperationException("Không thể tự xóa tài khoản của chính mình.");

            bool ok = _dal.Delete(id);
            if (ok)
            {
                _refreshTokenDAL.RevokeAllUserTokens(id, "Tài khoản bị xóa mềm");
                _auditLogDAL.Insert(new AuditLogModel
                {
                    MaNguoiDung = maNguoiThucHien,
                    HanhDong = "SOFT_DELETE_USER",
                    TenBang = "NguoiDung",
                    MaBanGhi = id.ToString()
                });
            }
            return ok;
        }

        private void KiemTraTrungLap(string tenDangNhap, string email)
        {
            if (_dal.GetByTenDangNhap(tenDangNhap.Trim()) != null)
                throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");
            if (_dal.ExistsEmail(email.Trim()))
                throw new InvalidOperationException("Email đã được sử dụng.");
        }

        private static void ValidateTaiKhoan(string tenDangNhap, string matKhau, string hoTen, string email, string? soDienThoai)
        {
            if (string.IsNullOrWhiteSpace(tenDangNhap))
                throw new ArgumentException("Tên đăng nhập không được để trống.");
            if (tenDangNhap.Trim().Length < 3 || tenDangNhap.Trim().Length > 50)
                throw new ArgumentException("Tên đăng nhập phải từ 3 đến 50 ký tự.");
            if (string.IsNullOrWhiteSpace(matKhau))
                throw new ArgumentException("Mật khẩu không được để trống.");
            if (matKhau.Trim().Length < 6)
                throw new ArgumentException("Mật khẩu phải có ít nhất 6 ký tự.");

            ValidateThongTinChung(hoTen, email, soDienThoai);
        }

        private static void ValidateThongTinChung(string hoTen, string email, string? soDienThoai)
        {
            if (string.IsNullOrWhiteSpace(hoTen))
                throw new ArgumentException("Họ tên không được để trống.");
            if (hoTen.Trim().Length > 100)
                throw new ArgumentException("Họ tên không được vượt quá 100 ký tự.");
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không được để trống.");
            if (email.Trim().Length > 100 || !EmailRegex.IsMatch(email.Trim()))
                throw new ArgumentException("Email không hợp lệ.");
            if (!string.IsNullOrWhiteSpace(soDienThoai) && !Regex.IsMatch(soDienThoai.Trim(), @"^[0-9+]{8,15}$"))
                throw new ArgumentException("Số điện thoại không hợp lệ.");
        }

        private static bool IsQuyenHopLe(string quyenHan)
        {
            return quyenHan == QuyenAdmin || quyenHan == QuyenGiangVien || quyenHan == QuyenHocVien;
        }
    }
}
