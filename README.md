# CT LMS - HỆ THỐNG QUẢN LÝ HỌC TRỰC TUYẾN PHÂN TÁN (SOA / MICROSERVICES .NET 8)

---

## 🚀 KIẾN TRÚC HỆ THỐNG
- **Platform:** .NET 8 C# (LTS), ASP.NET Core Web API.
- **API Gateway:** Ocelot Gateway (Port 5200) - Reverse Proxy, Routing tập trung, Rate Limiting, Response Compression, Private Network Access.
- **Microservices:**
  - `API_Login` (Port 5201): Xác thực JWT, Refresh Token Rotation, Background Worker Queue (`Channel<T>`).
  - `API_Admin` (Port 5202): Quản trị người dùng, danh mục khóa học, báo cáo tổng quan, phê duyệt khóa học và nhật ký kiểm toán (Audit Logs).
  - `API_GiangVien` (Port 5203): Quản lý khóa học, bài giảng, upload file an toàn, ngân hàng đề kiểm tra, xuất bảng điểm Excel.
  - `API_SinhVien` (Port 5204): Ghi danh, học tuần tự (Drip Content), thi trắc nghiệm chống gian lận (Anti-Cheat), cấp và xác thực chứng chỉ PDF có mã QR.
- **Database & Cache:**
  - SQL Server 2022: 17 bảng quan hệ chuẩn 3NF, Trigger kiểm toán, Stored Procedures tối ưu, Seed > 2.000 bản ghi thực tế.
  - Redis 7: Hybrid Distributed Cache (Cache-Aside pattern) và tự động thu hồi Cache (Invalidation Key).

---

## 🛠 HƯỚNG DẪN KHỞI CHẠY HỆ THỐNG

### Cách 1: Khởi chạy trọn gói bằng Docker Compose (Khuyên dùng)
Yêu cầu: Đã cài đặt Docker & Docker Compose.
```bash
# 1. Khởi động toàn bộ 8 services (SQL Server, db-init, Redis, 5 APIs)
docker-compose up -d --build

# 2. Xem logs khởi động của Gateway
docker-compose logs -f api-gateway
```
- **API Gateway Swagger:** `http://localhost:5200/swagger`
- **Giao diện Web:** `http://localhost:5200/index.html` hoặc mở trực tiếp thư mục `frontend/` qua Live Server.

---

### Cách 2: Khởi chạy thủ công trên máy cục bộ (.NET SDK & Local SQL Server)
1. **Khởi tạo CSDL SQL Server:**
   Chạy lần lượt các script trong thư mục `DB/` bằng SQL Server Management Studio (SSMS) hoặc `sqlcmd`:
   - `01_Schema.sql`
   - `02_Triggers.sql`
   - `06_StoredProcedures.sql`
   - `03_SeedMau.sql`
   - `04_SeedLon.sql`
2. **Biên dịch và chạy các dự án:**
   ```powershell
   dotnet build BTL_QuanLyHocTrucTuyen/BTL_QuanLyHocTrucTuyen.sln
   ```
   Chạy đồng thời 5 services trên 5 cửa sổ terminal:
   - `dotnet run --project BTL_QuanLyHocTrucTuyen/API_Login` (Port 5201)
   - `dotnet run --project BTL_QuanLyHocTrucTuyen/API_Admin` (Port 5202)
   - `dotnet run --project BTL_QuanLyHocTrucTuyen/API_GiangVien` (Port 5203)
   - `dotnet run --project BTL_QuanLyHocTrucTuyen/API_SinhVien` (Port 5204)
   - `dotnet run --project BTL_QuanLyHocTrucTuyen/API_GateWay` (Port 5200)

---

## 🧪 CHẠY BỘ KIỂM THỬ TỰ ĐỘNG (TEST SUITE)
Toàn bộ 24 test cases Unit Tests (`xUnit`, `Moq`) và Integration Tests (`WebApplicationFactory` kiểm thử pipeline HTTP) đạt 100% tỷ lệ đỗ:
```powershell
dotnet test BTL_QuanLyHocTrucTuyen/Tests/BTL_QuanLyHocTrucTuyen.Tests.csproj
```

---

## 📮 BỘ POSTMAN COLLECTION & TÀI LIỆU
- **Postman Collection:** `postman/CT_LMS.postman_collection.json`
- **Postman Environment:** `postman/CT_LMS.postman_environment.json`
- **Tài liệu Báo cáo Kiến trúc & SRS:** `docs/SRS_Architecture_Report.md`
- **Kịch bản Demo thuyết minh 5 - 10 phút:** `docs/Demo_Script_5_10_Phut.md`
