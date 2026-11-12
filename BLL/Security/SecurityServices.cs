using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Models;

namespace BLL.Security
{
    /// <summary>
    /// Dịch vụ băm mật khẩu BCrypt (cost factor 11) hỗ trợ tự động nâng cấp mật khẩu plaintext cũ lên BCrypt
    /// </summary>
    public static class PasswordHasher
    {
        private const int WorkFactor = 11;

        public static string HashPassword(string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(plainPassword))
                throw new ArgumentException("Mật khẩu không được để trống.");

            return BCrypt.Net.BCrypt.HashPassword(plainPassword.Trim(), WorkFactor);
        }

        public static bool VerifyPassword(string plainPassword, string storedHashOrPlain, out bool needsRehash)
        {
            needsRehash = false;
            if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(storedHashOrPlain))
                return false;

            string trimmedInput = plainPassword.Trim();

            // Kiểm tra định dạng BCrypt ($2a$, $2b$, $2y$)
            if (storedHashOrPlain.StartsWith("$2a$") || storedHashOrPlain.StartsWith("$2b$") || storedHashOrPlain.StartsWith("$2y$"))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(trimmedInput, storedHashOrPlain);
                }
                catch
                {
                    return false;
                }
            }

            // Tương thích ngược với tài khoản plaintext cũ và đánh dấu cần nâng cấp lên BCrypt
            if (string.Equals(trimmedInput, storedHashOrPlain, StringComparison.Ordinal))
            {
                needsRehash = true;
                return true;
            }

            return false;
        }
    }

    public interface IJwtTokenService
    {
        (string AccessToken, string JwtId, DateTime ExpiresAtLocal) GenerateAccessToken(
            NguoiDungModel user,
            int? maSinhVien = null,
            int? maGiangVien = null);

        (string RawToken, string TokenHash, DateTime ExpiresAtUtc) GenerateRefreshToken();

        string ComputeSha256Hash(string rawToken);
    }

    /// <summary>
    /// Dịch vụ phát hành JWT Access Token và Refresh Token xoay vòng (Refresh Token Rotation) chuẩn Bài 10 Giáo trình
    /// </summary>
    public class JwtTokenService : IJwtTokenService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenMinutes;
        private readonly int _refreshTokenDays;

        public JwtTokenService(IConfiguration? configuration = null)
        {
            _secretKey = configuration?["Jwt:Key"]
                ?? configuration?["Jwt:SecretKey"]
                ?? Environment.GetEnvironmentVariable("Jwt__Key")
                ?? Environment.GetEnvironmentVariable("Jwt__SecretKey")
                ?? "QuanLyHocTrucTuyen_SecretKey_2026_Secure_JWT_Key_Antigravity!";
            _issuer = configuration?["Jwt:Issuer"] ?? "QuanLyHocTrucTuyen_API";
            _audience = configuration?["Jwt:Audience"] ?? "QuanLyHocTrucTuyen_Client";
            _accessTokenMinutes = int.TryParse(configuration?["Jwt:ExpireMinutes"], out int m) && m > 0 ? m : 180;
            _refreshTokenDays = int.TryParse(configuration?["Jwt:RefreshTokenDays"], out int d) && d > 0 ? d : 7;
        }

        public (string AccessToken, string JwtId, DateTime ExpiresAtLocal) GenerateAccessToken(
            NguoiDungModel user,
            int? maSinhVien = null,
            int? maGiangVien = null)
        {
            string jwtId = Guid.NewGuid().ToString("N");
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Jti, jwtId),
                new(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()),
                new(ClaimTypes.Name, user.TenDangNhap),
                new("FullName", user.HoTen ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Role, user.QuyenHan)
            };

            if (maSinhVien.HasValue)
                claims.Add(new Claim("MaSinhVien", maSinhVien.Value.ToString()));
            if (maGiangVien.HasValue)
                claims.Add(new Claim("MaGiangVien", maGiangVien.Value.ToString()));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresLocal = DateTime.Now.AddMinutes(_accessTokenMinutes);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: expiresLocal,
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(token), jwtId, expiresLocal);
        }

        public (string RawToken, string TokenHash, DateTime ExpiresAtUtc) GenerateRefreshToken()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
            string rawToken = Convert.ToBase64String(randomBytes);
            string tokenHash = ComputeSha256Hash(rawToken);
            DateTime expiresUtc = DateTime.UtcNow.AddDays(_refreshTokenDays);
            return (rawToken, tokenHash, expiresUtc);
        }

        public string ComputeSha256Hash(string rawToken)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken.Trim()));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
