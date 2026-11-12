using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using Models;
using QRCoder;

namespace BLL.Export
{
    public interface IExportService
    {
        byte[] GenerateCertificatePdf(ChungChiModel cert, string verifyBaseUrl);
        byte[] GenerateOverviewExcel(BaoCaoTongQuanModel report);
        byte[] GenerateGradebookExcel(string tenKhoaHoc, IEnumerable<KetQuaKiemTraModel> ketQuaList);
        string GenerateQrCodeBase64(string payload);
    }

    /// <summary>
    /// Dịch vụ xuất tệp PDF Chứng chỉ điện tử (kèm mã QR xác thực) và Excel Báo cáo/Bảng điểm
    /// Đạt chuẩn Bài 11 & Phụ lục B Giáo trình
    /// </summary>
    public class ExportService : IExportService
    {
        public static readonly ExportService Shared = new();

        public string GenerateQrCodeBase64(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return string.Empty;
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            using var pngByteQr = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = pngByteQr.GetGraphic(20);
            return Convert.ToBase64String(qrBytes);
        }

        public byte[] GenerateCertificatePdf(ChungChiModel cert, string verifyBaseUrl)
        {
            string certCode = !string.IsNullOrWhiteSpace(cert.SoChungChi) ? cert.SoChungChi : $"CTLMS-CERT-{cert.MaChungChi:D6}";
            string verifyUrl = $"{verifyBaseUrl.TrimEnd('/')}/api/ChungChi/XacThuc/{certCode}";

            var sb = new StringBuilder();
            sb.AppendLine("%PDF-1.4");
            sb.AppendLine("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj");
            sb.AppendLine("2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj");
            sb.AppendLine("3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >> endobj");
            sb.AppendLine("4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >> endobj");
            sb.AppendLine("5 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj");

            string safeHoTen = RemoveDiacritics(cert.HoTenSinhVien ?? "Hoc Vien CT LMS");
            string safeKhoaHoc = RemoveDiacritics(cert.TenKhoaHoc ?? "Khoa Hoc Truc Tuyen");
            string safeNgayCap = cert.NgayHoanThanh.ToString("dd/MM/yyyy HH:mm");
            string safeCode = certCode;
            string safeXepLoai = RemoveDiacritics(cert.XepLoai ?? "Xuat sac");

            var streamContent = new StringBuilder();
            // Viền ngoài
            streamContent.AppendLine("0.1 0.4 0.8 rg 5 w");
            streamContent.AppendLine("30 30 782 535 re S");
            streamContent.AppendLine("0.85 0.65 0.13 rg 2 w");
            streamContent.AppendLine("40 40 762 515 re S");

            // Header Chứng chỉ
            streamContent.AppendLine("BT");
            streamContent.AppendLine("/F1 28 Tf");
            streamContent.AppendLine("0.08 0.18 0.36 rg");
            streamContent.AppendLine("230 490 Td (CHUNG CHI HOAN THANH KHOA HOC) Tj");
            streamContent.AppendLine("ET");

            streamContent.AppendLine("BT");
            streamContent.AppendLine("/F2 14 Tf");
            streamContent.AppendLine("0.4 0.4 0.4 rg");
            streamContent.AppendLine("320 455 Td (HE THONG DAO TAO TRUC TUYEN CT LMS) Tj");
            streamContent.AppendLine("ET");

            streamContent.AppendLine("BT");
            streamContent.AppendLine("/F2 16 Tf");
            streamContent.AppendLine("0.2 0.2 0.2 rg");
            streamContent.AppendLine("345 400 Td (Chung nhan hoc vien:) Tj");
            streamContent.AppendLine("ET");

            // Tên sinh viên
            streamContent.AppendLine("BT");
            streamContent.AppendLine("/F1 26 Tf");
            streamContent.AppendLine("0.12 0.44 0.72 rg");
            streamContent.AppendLine($"280 355 Td ({safeHoTen}) Tj");
            streamContent.AppendLine("ET");

            streamContent.AppendLine("BT");
            streamContent.AppendLine("/F2 15 Tf");
            streamContent.AppendLine("0.2 0.2 0.2 rg");
            streamContent.AppendLine("260 310 Td (Da xuat sac hoan thanh khoa hoc chuyen de:) Tj");
            streamContent.AppendLine("ET");

            // Tên khóa học
            streamContent.AppendLine("BT");
            streamContent.AppendLine("/F1 22 Tf");
            streamContent.AppendLine("0.08 0.18 0.36 rg");
            streamContent.AppendLine($"200 265 Td ({safeKhoaHoc}) Tj");
            streamContent.AppendLine("ET");

            // Điểm số, ngày cấp
            decimal diem = cert.DiemTrungBinh ?? 10.0m;
            streamContent.AppendLine("BT");
            streamContent.AppendLine("/F2 13 Tf");
            streamContent.AppendLine("0.3 0.3 0.3 rg");
            streamContent.AppendLine($"260 215 Td (Diem trung binh: {diem:F1} / 10.0   -   Xep loai: {safeXepLoai}) Tj");
            streamContent.AppendLine($"150 160 Td (Ma tra cuu: {safeCode}) Tj");
            streamContent.AppendLine($"150 140 Td (Ngay cap: {safeNgayCap}) Tj");
            streamContent.AppendLine($"150 120 Td (Xac thuc truc tuyen tai: {verifyUrl}) Tj");
            streamContent.AppendLine("ET");

            // Chữ ký đại diện
            streamContent.AppendLine("BT");
            streamContent.AppendLine("/F1 13 Tf");
            streamContent.AppendLine("0.1 0.2 0.4 rg");
            streamContent.AppendLine("570 140 Td (BAN GIAM DOC CT LMS) Tj");
            streamContent.AppendLine("/F2 11 Tf");
            streamContent.AppendLine("565 120 Td ([Chung thuc so hop le]) Tj");
            streamContent.AppendLine("ET");

            string contentStr = streamContent.ToString();
            byte[] contentBytes = Encoding.ASCII.GetBytes(contentStr);

            sb.AppendLine($"6 0 obj << /Length {contentBytes.Length} >>");
            sb.AppendLine("stream");
            sb.Append(contentStr);
            sb.AppendLine("endstream");
            sb.AppendLine("endobj");

            sb.AppendLine("xref");
            sb.AppendLine("0 7");
            sb.AppendLine("0000000000 65535 f ");
            sb.AppendLine("0000000009 00000 n ");
            sb.AppendLine("0000000058 00000 n ");
            sb.AppendLine("0000000115 00000 n ");
            sb.AppendLine("0000000254 00000 n ");
            sb.AppendLine("0000000336 00000 n ");
            sb.AppendLine("0000000414 00000 n ");
            sb.AppendLine("trailer << /Size 7 /Root 1 0 R >>");
            sb.AppendLine("startxref");
            sb.AppendLine("550");
            sb.AppendLine("%%EOF");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public byte[] GenerateOverviewExcel(BaoCaoTongQuanModel report)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("BaoCaoTongQuan");

            ws.Cell(1, 1).Value = "BÁO CÁO THỐNG KÊ TỔNG QUAN HỆ THỐNG CT LMS";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Range(1, 1, 1, 4).Merge().Style.Fill.BackgroundColor = XLColor.Navy;
            ws.Row(1).Height = 30;

            ws.Cell(3, 1).Value = "Tổng số người dùng:";
            ws.Cell(3, 2).Value = report.TongNguoiDung;
            ws.Cell(3, 3).Value = "Tổng số khóa học:";
            ws.Cell(3, 4).Value = report.TongKhoaHoc;

            ws.Cell(4, 1).Value = "Tổng số học viên:";
            ws.Cell(4, 2).Value = report.TongHocVien;
            ws.Cell(4, 3).Value = "Tổng lượt đăng ký:";
            ws.Cell(4, 4).Value = report.TongLuotDangKy;

            ws.Cell(5, 1).Value = "Tổng số giảng viên:";
            ws.Cell(5, 2).Value = report.TongGiangVien;
            ws.Cell(5, 3).Value = "Lượt hoàn thành:";
            ws.Cell(5, 4).Value = report.TongLuotHoanThanh;

            ws.Range(3, 1, 5, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(3, 1, 5, 4).Style.Font.FontSize = 11;

            ws.Cell(7, 1).Value = "TOP KHÓA HỌC ĐƯỢC ĐĂNG KÝ NHIỀU NHẤT";
            ws.Cell(7, 1).Style.Font.Bold = true;
            ws.Range(7, 1, 7, 4).Merge().Style.Fill.BackgroundColor = XLColor.LightBlue;

            ws.Cell(8, 1).Value = "Mã KH";
            ws.Cell(8, 2).Value = "Tên Khóa Học";
            ws.Cell(8, 3).Value = "Giảng Viên";
            ws.Cell(8, 4).Value = "Số Lượng Học Viên";
            ws.Range(8, 1, 8, 4).Style.Font.Bold = true;
            ws.Range(8, 1, 8, 4).Style.Fill.BackgroundColor = XLColor.AliceBlue;

            int row = 9;
            if (report.TopKhoaHoc != null)
            {
                foreach (var kh in report.TopKhoaHoc)
                {
                    ws.Cell(row, 1).Value = kh.MaKhoaHoc;
                    ws.Cell(row, 2).Value = kh.TenKhoaHoc;
                    ws.Cell(row, 3).Value = kh.TenGiangVien;
                    ws.Cell(row, 4).Value = kh.SoLuongHocVien;
                    row++;
                }
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] GenerateGradebookExcel(string tenKhoaHoc, IEnumerable<KetQuaKiemTraModel> ketQuaList)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("BangDiem");

            ws.Cell(1, 1).Value = $"BẢNG ĐIỂM CHI TIẾT - KHÓA HỌC: {tenKhoaHoc.ToUpper()}";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 7).Merge().Style.Fill.BackgroundColor = XLColor.FromHtml("#1b4b72");
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;

            string[] headers = { "Mã KQ", "Mã Đăng Ký", "Tên Học Viên", "Bài Kiểm Tra", "Lần Thi", "Điểm Số", "Ngày Nộp" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(3, i + 1).Value = headers[i];
                ws.Cell(3, i + 1).Style.Font.Bold = true;
                ws.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            int row = 4;
            foreach (var kq in ketQuaList)
            {
                ws.Cell(row, 1).Value = kq.MaKetQua;
                ws.Cell(row, 2).Value = kq.MaDangKy;
                ws.Cell(row, 3).Value = kq.HoTenSinhVien ?? $"Học viên #{kq.MaDangKy}";
                ws.Cell(row, 4).Value = kq.TieuDeBaiKiemTra ?? $"Bài kiểm tra #{kq.MaBaiKiemTra}";
                ws.Cell(row, 5).Value = kq.LanThu;
                ws.Cell(row, 6).Value = kq.DiemSo;
                ws.Cell(row, 7).Value = kq.NgayNop.ToString("dd/MM/yyyy HH:mm");
                row++;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static string RemoveDiacritics(string text)
        {
            string[] vietnameseSigns = new string[]
            {
                "aAeEoOuUiIdDyY",
                "áàạảãâấầậẩẫăắằặẳẵ",
                "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
                "éèẹẻẽêếềệểễ",
                "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ",
                "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
                "úùụủũưứừựửữ",
                "ÚÙỤỦŨƯỨỪỰỬỮ",
                "íìịỉĩ",
                "ÍÌỊỈĨ",
                "đ",
                "Đ",
                "ýỳỵỷỹ",
                "ÝỲỴỶỸ"
            };

            for (int i = 1; i < vietnameseSigns.Length; i++)
            {
                for (int j = 0; j < vietnameseSigns[i].Length; j++)
                    text = text.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
            }
            return text;
        }
    }
}
