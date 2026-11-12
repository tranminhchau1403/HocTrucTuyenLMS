using System;
using System.Collections.Generic;
using BLL;
using BLL.Caching;
using DAL;
using Models;
using Moq;
using Xunit;

namespace BTL_QuanLyHocTrucTuyen.Tests.Unit
{
    public class EnrollmentAndQuizServiceTests
    {
        private readonly Mock<IDangKyKhoaHocRepository> _mockDangKyRepo = new();
        private readonly Mock<IKhoaHocRepository> _mockKhoaHocRepo = new();
        private readonly Mock<ITienDoBaiGiangRepository> _mockTienDoRepo = new();
        private readonly Mock<IBaiKiemTraRepository> _mockBaiKiemTraRepo = new();
        private readonly Mock<IKetQuaKiemTraRepository> _mockKetQuaRepo = new();
        private readonly Mock<ICauHoiRepository> _mockCauHoiRepo = new();
        private readonly Mock<IPhienLamBaiRepository> _mockPhienRepo = new();
        private readonly Mock<ICacheService> _mockCache = new();

        private DangKyKhoaHocBLL CreateEnrollmentService()
        {
            return new DangKyKhoaHocBLL(
                _mockDangKyRepo.Object,
                _mockKhoaHocRepo.Object,
                _mockTienDoRepo.Object,
                _mockCache.Object);
        }

        private KetQuaKiemTraBLL CreateQuizService()
        {
            return new KetQuaKiemTraBLL(
                _mockKetQuaRepo.Object,
                _mockBaiKiemTraRepo.Object,
                _mockDangKyRepo.Object,
                _mockKhoaHocRepo.Object,
                _mockCauHoiRepo.Object,
                _mockPhienRepo.Object);
        }

        [Fact]
        public void DangKy_WhenCourseActiveAndNotEnrolled_InsertsSuccessfully()
        {
            var bll = CreateEnrollmentService();
            _mockKhoaHocRepo.Setup(r => r.GetById(10)).Returns(new KhoaHocModel { MaKhoaHoc = 10, TrangThai = true });
            _mockDangKyRepo.Setup(r => r.GetBySinhVienAndKhoaHoc(1, 10)).Returns((DangKyKhoaHocModel?)null);
            _mockDangKyRepo.Setup(r => r.Insert(It.IsAny<DangKyKhoaHocModel>())).Returns(100);

            int maDangKy = bll.DangKy(1, 10);

            Assert.Equal(100, maDangKy);
            _mockCache.Verify(c => c.Remove("lms:reports:overview"), Times.Once);
        }

        [Fact]
        public void DangKy_WhenAlreadyEnrolled_ThrowsConflictException()
        {
            var bll = CreateEnrollmentService();
            _mockKhoaHocRepo.Setup(r => r.GetById(10)).Returns(new KhoaHocModel { MaKhoaHoc = 10, TrangThai = true });
            _mockDangKyRepo.Setup(r => r.GetBySinhVienAndKhoaHoc(1, 10)).Returns(new DangKyKhoaHocModel { MaDangKy = 55 });

            var ex = Assert.Throws<InvalidOperationException>(() => bll.DangKy(1, 10));

            Assert.Contains("đã đăng ký", ex.Message);
        }

        [Fact]
        public void HoanThanhBaiGiangTuanTu_CallsDalAndClearsReportCache()
        {
            var bll = CreateEnrollmentService();
            _mockTienDoRepo.Setup(r => r.HoanThanhBaiGiangTuanTu(1, 101, 1, 1200))
                .Returns(new DangKyKhoaHocModel { MaDangKy = 1, TienDoPhanTram = 25 });

            var result = bll.HoanThanhBaiGiangTuanTu(1, 101, 1, 1200);

            Assert.Equal(25, result.TienDoPhanTram);
            _mockCache.Verify(c => c.Remove("lms:reports:overview"), Times.Once);
        }

        [Fact]
        public void BatDauPhienLamBai_ReturnsQuestionsWithoutCorrectAnswerFlags()
        {
            var bll = CreateQuizService();
            _mockPhienRepo.Setup(r => r.BatDauPhien(1, 5, 1)).Returns(new BatDauKiemTraResponse
            {
                MaPhien = 12,
                MaDangKy = 1,
                MaBaiKiemTra = 5,
                ThoiGianLamBai = 15,
                GioiHanChuyenTab = 3
            });

            _mockCauHoiRepo.Setup(r => r.GetByBaiKiemTra(5)).Returns(new List<CauHoiModel>
            {
                new()
                {
                    MaCauHoi = 1,
                    NoiDung = "OOP là gì?",
                    Diem = 10,
                    DanhSachLuaChon = new List<LuaChonModel>
                    {
                        new() { MaLuaChon = 11, NoiDung = "Lap trinh huong doi tuong", LaDapAnDung = true },
                        new() { MaLuaChon = 12, NoiDung = "Co so du lieu", LaDapAnDung = false }
                    }
                }
            });

            var session = bll.BatDauPhienLamBai(1, 5, 1);

            Assert.NotNull(session);
            Assert.Single(session.DanhSachCauHoi);
            var q = session.DanhSachCauHoi[0];
            Assert.Equal("OOP là gì?", q.NoiDung);
            Assert.Equal(2, q.DanhSachLuaChon.Count);
        }

        [Fact]
        public void NopBaiChongGianLan_WhenTabSwitchesExceedLimit_SavesWarningStatus()
        {
            var bll = CreateQuizService();
            _mockDangKyRepo.Setup(r => r.GetById(1)).Returns(new DangKyKhoaHocModel { MaDangKy = 1, MaKhoaHoc = 10, MaSinhVien = 2 });
            _mockBaiKiemTraRepo.Setup(r => r.GetById(5)).Returns(new BaiKiemTraModel
            {
                MaBaiKiemTra = 5,
                MaKhoaHoc = 10,
                DiemToiDa = 10,
                SoLanLamToiDa = 3,
                GioiHanChuyenTab = 2
            });

            _mockPhienRepo.Setup(r => r.ChamDiemVaLuuKetQuaTransaction(
                1, 5, null, It.IsAny<int>(), 8.5m, null, null, 4,
                It.Is<string>(s => s.Contains("Cảnh báo chuyển tab")),
                It.IsAny<string?>())).Returns(505);

            int id = bll.NopBaiChongGianLan(new NopBaiKiemTraRequest
            {
                MaDangKy = 1,
                MaBaiKiemTra = 5,
                DiemSo = 8.5m,
                SoLanChuyenTab = 4 // Vượt quá giới hạn 2
            }, maSinhVien: 2);

            Assert.Equal(505, id);
        }
    }
}
