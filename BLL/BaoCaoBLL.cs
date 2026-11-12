using System;
using BLL.Caching;
using DAL;
using Models;

namespace BLL
{
    public class BaoCaoBLL
    {
        private const string ReportOverviewCacheKey = "lms:reports:overview";
        private readonly IBaoCaoRepository _dal;
        private readonly ICacheService _cache;

        public BaoCaoBLL(IBaoCaoRepository? dal = null, ICacheService? cache = null)
        {
            _dal = dal ?? new BaoCaoDAL();
            _cache = cache ?? HybridCacheService.Shared;
        }

        public BaoCaoTongQuanModel GetTongQuan()
        {
            return _cache.GetOrCreate(
                ReportOverviewCacheKey,
                () => _dal.GetTongQuan(),
                TimeSpan.FromMinutes(2),
                TimeSpan.FromMinutes(1)) ?? new BaoCaoTongQuanModel();
        }

        public ChungChiModel? GetChungChi(int maDangKy, int maSinhVien, bool isAdmin = false)
        {
            if (maDangKy <= 0) return null;
            var cc = _dal.GetChungChi(maDangKy);
            if (cc == null) return null;
            if (!isAdmin && cc.MaSinhVien != maSinhVien)
                throw new InvalidOperationException("Bạn không có quyền truy xuất chứng chỉ của học viên khác.");
            return cc;
        }

        /// <summary>Luồng 3: Tra cứu và xác thực công khai chứng chỉ điện tử qua mã chứng chỉ</summary>
        public ChungChiModel? XacThucChungChiCongKhai(string maChungChiCode)
        {
            if (string.IsNullOrWhiteSpace(maChungChiCode)) return null;
            return _dal.GetChungChiByCode(maChungChiCode.Trim());
        }
    }
}
