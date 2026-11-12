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
    public class CourseAndApprovalServiceTests
    {
        private readonly Mock<IKhoaHocRepository> _mockCourseRepo = new();
        private readonly Mock<ICacheService> _mockCache = new();

        private KhoaHocBLL CreateService()
        {
            return new KhoaHocBLL(_mockCourseRepo.Object, _mockCache.Object);
        }

        [Fact]
        public void InsertCourse_WithValidData_ReturnsIdAndInvalidatesCache()
        {
            var bll = CreateService();
            _mockCourseRepo.Setup(r => r.Insert(It.IsAny<KhoaHocModel>())).Returns(15);

            int id = bll.Insert(new KhoaHocModel
            {
                TenKhoaHoc = "Khóa học .NET 8 Cloud Native",
                MaDanhMuc = 1,
                MaGiangVien = 2,
                Gia = 299000
            });

            Assert.Equal(15, id);
            _mockCache.Verify(c => c.RemoveByPrefix("lms:courses:"), Times.Once);
        }

        [Fact]
        public void InsertCourse_WithEmptyName_ThrowsArgumentException()
        {
            var bll = CreateService();

            var ex = Assert.Throws<ArgumentException>(() => bll.Insert(new KhoaHocModel
            {
                TenKhoaHoc = "   ",
                MaDanhMuc = 1,
                MaGiangVien = 1
            }));

            Assert.Contains("không được để trống", ex.Message);
        }

        [Fact]
        public void UpdateCourse_ByDifferentTeacher_ThrowsForbiddenException()
        {
            var bll = CreateService();
            _mockCourseRepo.Setup(r => r.GetById(10)).Returns(new KhoaHocModel
            {
                MaKhoaHoc = 10,
                TenKhoaHoc = "C# Core",
                MaGiangVien = 1
            });

            var ex = Assert.Throws<InvalidOperationException>(() => bll.Update(new KhoaHocModel
            {
                MaKhoaHoc = 10,
                TenKhoaHoc = "C# Core Updated"
            }, maGiangVienYeuCau: 2, isAdmin: false));

            Assert.Contains("không có quyền", ex.Message);
        }

        [Fact]
        public void GuiDuyet_ByCourseOwner_UpdatesStatusToChoDuyet()
        {
            var bll = CreateService();
            _mockCourseRepo.Setup(r => r.GetById(20)).Returns(new KhoaHocModel
            {
                MaKhoaHoc = 20,
                MaGiangVien = 5,
                TenKhoaHoc = "Microservices Patterns"
            });
            _mockCourseRepo.Setup(r => r.UpdateTrangThaiDuyet(20, "Chờ duyệt", null, 5)).Returns(true);

            bool ok = bll.GuiDuyet(20, 5);

            Assert.True(ok);
            _mockCache.Verify(c => c.RemoveByPrefix("lms:courses:"), Times.Once);
        }

        [Fact]
        public void PheDuyet_ByAdmin_WithValidStatus_UpdatesSuccessfully()
        {
            var bll = CreateService();
            _mockCourseRepo.Setup(r => r.UpdateTrangThaiDuyet(20, "Đã duyệt", null, 1)).Returns(true);

            bool ok = bll.PheDuyet(20, "Đã duyệt", null, 1);

            Assert.True(ok);
            _mockCache.Verify(c => c.RemoveByPrefix("lms:courses:"), Times.Once);
        }

        [Fact]
        public void PheDuyet_WhenRejectedWithoutReason_ThrowsArgumentException()
        {
            var bll = CreateService();

            var ex = Assert.Throws<ArgumentException>(() => bll.PheDuyet(20, "Từ chối", "   ", 1));

            Assert.Contains("lý do", ex.Message);
        }
    }
}
