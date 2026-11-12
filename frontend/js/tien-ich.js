/**
 * tien-ich.js — Bộ công cụ tiện ích nâng cao:
 * 1. Nhập / Xuất Excel & CSV chuẩn UTF-8 tiếng Việt
 * 2. Xuất bản in & Lưu PDF (Hóa đơn, Phiếu phát hành, Báo cáo)
 * 3. In-App Toast Notifications (Thông báo nổi thời gian thực)
 */

const TienIch = {
  /**
   * Xuất danh sách đối tượng ra file CSV (có UTF-8 BOM hiển thị chuẩn trong Excel)
   * @param {Array} data Danh sách đối tượng
   * @param {Object} mapping Ánh xạ { tieuDeCot: 'tenThuocTinh' }
   * @param {String} filename Tên file xuất
   */
  xuatCSV: function (data, mapping, filename) {
    if (!data || !data.length) {
      TienIch.thongBao('Không có dữ liệu để xuất file!', 'canh-bao');
      return;
    }

    const headers = Object.keys(mapping);
    const keys = Object.values(mapping);

    let csvContent = '\uFEFF'; // UTF-8 BOM
    csvContent += headers.map(h => `"${h.replace(/"/g, '""')}"`).join(',') + '\r\n';

    data.forEach(item => {
      const row = keys.map(key => {
        let val = '';
        if (typeof key === 'function') {
          val = key(item);
        } else {
          val = item[key] !== undefined && item[key] !== null ? item[key] : '';
        }
        return `"${String(val).replace(/"/g, '""')}"`;
      });
      csvContent += row.join(',') + '\r\n';
    });

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = (filename || 'xuat-du-lieu') + '.csv';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);

    TienIch.thongBao(`Đã xuất ${data.length} dòng dữ liệu ra file CSV!`, 'thanh-cong');
  },

  /**
   * Xuất danh sách ra định dạng bảng Excel (.xls)
   */
  xuatExcel: function (data, mapping, filename) {
    if (!data || !data.length) {
      TienIch.thongBao('Không có dữ liệu để xuất file!', 'canh-bao');
      return;
    }

    const headers = Object.keys(mapping);
    const keys = Object.values(mapping);

    let html = `
      <html xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel" xmlns="http://www.w3.org/TR/REC-html40">
      <head>
        <meta http-equiv="content-type" content="application/vnd.ms-excel; charset=UTF-8">
        <style>
          table { border-collapse: collapse; width: 100%; font-family: Arial, sans-serif; }
          th { background-color: #4f46e5; color: #ffffff; border: 1px solid #cbd5e1; padding: 10px; font-size: 13px; }
          td { border: 1px solid #e2e8f0; padding: 8px; font-size: 12px; }
          .so { mso-number-format:"\\#\\,\\#\\#0"; text-align: right; }
          .ngay { mso-number-format:"yyyy\\-mm\\-dd"; text-align: center; }
        </style>
      </head>
      <body>
        <h3>${filename || 'DANH SÁCH DỮ LIỆU'}</h3>
        <p>Thời gian xuất: ${new Date().toLocaleString('vi-VN')}</p>
        <table>
          <thead>
            <tr>${headers.map(h => `<th>${h}</th>`).join('')}</tr>
          </thead>
          <tbody>
    `;

    data.forEach(item => {
      html += '<tr>';
      keys.forEach(key => {
        let val = '';
        if (typeof key === 'function') {
          val = key(item);
        } else {
          val = item[key] !== undefined && item[key] !== null ? item[key] : '';
        }
        html += `<td>${val}</td>`;
      });
      html += '</tr>';
    });

    html += `
          </tbody>
        </table>
      </body>
      </html>
    `;

    const blob = new Blob(['\uFEFF' + html], { type: 'application/vnd.ms-excel;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = (filename || 'xuat-du-lieu') + '.xls';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);

    TienIch.thongBao(`Đã xuất ${data.length} bản ghi ra file Excel (.xls)!`, 'thanh-cong');
  },

  /**
   * Đọc và phân tích file CSV tải lên từ người dùng
   */
  docFileCSV: function (file, callback) {
    if (!file) return;
    const reader = new FileReader();
    reader.onload = function (e) {
      try {
        const text = e.target.result;
        const lines = text.split(/\r\n|\n/).filter(line => line.trim().length > 0);
        if (lines.length < 2) {
          TienIch.thongBao('File CSV rỗng hoặc chỉ có dòng tiêu đề!', 'canh-bao');
          return;
        }

        // Tách tiêu đề
        const headers = lines[0].split(',').map(h => h.trim().replace(/^"|"$/g, ''));
        const result = [];

        for (let i = 1; i < lines.length; i++) {
          const cells = lines[i].split(',').map(c => c.trim().replace(/^"|"$/g, ''));
          const rowObj = {};
          headers.forEach((h, idx) => {
            rowObj[h] = cells[idx] || '';
          });
          result.push(rowObj);
        }

        callback(result);
        TienIch.thongBao(`Đã nhập thành công ${result.length} dòng từ file!`, 'thanh-cong');
      } catch (err) {
        console.error(err);
        TienIch.thongBao('Lỗi đọc cấu trúc file CSV!', 'nguy-hiem');
      }
    };
    reader.readAsText(file, 'UTF-8');
  },

  /**
   * In ấn và Xuất Hóa Đơn PDF chuẩn quốc gia
   */
  inHoaDonPDF: function (hd) {
    if (!hd) return;
    const printWindow = window.open('', '_blank', 'width=850,height=950');
    if (!printWindow) {
      alert('Vui lòng cho phép mở cửa sổ popup để in hóa đơn PDF!');
      return;
    }

    const tienVND = (num) => Number(num || 0).toLocaleString('vi-VN') + ' ₫';

    printWindow.document.write(`
      <!DOCTYPE html>
      <html lang="vi">
      <head>
        <meta charset="UTF-8">
        <title>Hóa Đơn Thu Tiền Báo - #HD-${hd.maHoaDon}</title>
        <style>
          body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 30px; color: #1e293b; }
          .header { display: flex; justify-content: space-between; border-bottom: 2px solid #4f46e5; padding-bottom: 20px; }
          .logo { font-size: 22px; font-weight: 800; color: #4f46e5; }
          .title { text-align: center; margin: 25px 0 15px 0; font-size: 24px; font-weight: 800; text-transform: uppercase; color: #0f172a; }
          .sub { text-align: center; color: #64748b; font-size: 14px; margin-bottom: 25px; }
          .info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin-bottom: 25px; background: #f8fafc; padding: 15px; border-radius: 8px; border: 1px solid #e2e8f0; }
          table { width: 100%; border-collapse: collapse; margin-bottom: 30px; }
          th { background: #4f46e5; color: white; padding: 12px; font-size: 14px; border: 1px solid #4338ca; text-align: left; }
          td { padding: 12px; border: 1px solid #e2e8f0; font-size: 14px; }
          .total-row { font-weight: 700; font-size: 16px; background: #f1f5f9; }
          .signatures { display: flex; justify-content: space-between; margin-top: 40px; text-align: center; }
          .btn-in { position: fixed; top: 15px; right: 15px; background: #4f46e5; color: #fff; padding: 10px 20px; border-radius: 6px; border: none; cursor: pointer; font-weight: 600; font-size: 14px; }
          @media print {
            .btn-in { display: none; }
            body { margin: 0; }
          }
        </style>
      </head>
      <body>
        <button class="btn-in" onclick="window.print()">🖨️ In / Lưu file PDF</button>

        <div class="header">
          <div>
            <div class="logo">TÒA SOẠN & CÔNG TY PHÁT HÀNH BÁO CHÍ</div>
            <div style="font-size: 13px; color: #64748b; margin-top: 4px;">Địa chỉ: Thôn Phương Mạn Đường, xã Châu Ninh, tỉnh Hưng Yên</div>
            <div style="font-size: 13px; color: #64748b;">Hotline: (024) 3825 6789 - Email: toasoan@baochi.vn</div>
          </div>
          <div style="text-align: right;">
            <div style="font-weight: 700; font-size: 16px;">MÃ HÓA ĐƠN: #HD-${hd.maHoaDon}</div>
            <div style="font-size: 13px; color: #64748b; margin-top: 4px;">Ngày lập: ${hd.ngayLap || new Date().toISOString().split('T')[0]}</div>
            <div style="font-size: 13px; color: #059669; font-weight: 700; margin-top: 4px;">Trạng thái: ${hd.trangThai}</div>
          </div>
        </div>

        <div class="title">HÓA ĐƠN GIÁ TRỊ GIA TĂNG (ĐẶT BÁO ĐỊNH KỲ)</div>
        <div class="sub">Hợp đồng thuê bao phát hành báo & tạp chí</div>

        <div class="info-grid">
          <div>
            <strong>Độc giả thanh toán:</strong> ${hd.tenDocGia || 'Khách hàng độc giả'}<br>
            <strong>Mã đăng ký thuê bao:</strong> #ĐK-${hd.maDangKy}<br>
          </div>
          <div>
            <strong>Phương thức:</strong> ${hd.phuongThuc || 'Chuyển khoản trực tuyến / QR'}<br>
            <strong>Hình thức nhận:</strong> Phát hành và giao báo tận nơi
          </div>
        </div>

        <table>
          <thead>
            <tr>
              <th>STT</th>
              <th>Tên Ấn Phẩm Báo Chí</th>
              <th>Kỳ Hạn Thuê Bao</th>
              <th>Đơn Giá / Kỳ</th>
              <th>Tổng Thành Tiền</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td style="text-align: center;">1</td>
              <td><strong>${hd.tenAnPham || 'Ấn phẩm Báo chí & Tạp chí định kỳ'}</strong></td>
              <td>Theo hợp đồng đăng ký</td>
              <td style="text-align: right;">${tienVND(hd.tongTien)}</td>
              <td style="text-align: right; font-weight: 700; color: #4f46e5;">${tienVND(hd.thanhTien || hd.tongTien)}</td>
            </tr>
            <tr class="total-row">
              <td colspan="4" style="text-align: right;">TỔNG TIỀN THANH TOÁN (ĐÃ GỒM VAT):</td>
              <td style="text-align: right; color: #059669;">${tienVND(hd.thanhTien || hd.tongTien)}</td>
            </tr>
          </tbody>
        </table>

        <div class="signatures">
          <div>
            <strong>NGƯỜI ĐẶT BÁO</strong><br>
            <span style="font-size: 12px; color: #64748b;">(Ký, ghi rõ họ tên)</span><br><br><br><br>
            <strong>${hd.tenDocGia || ''}</strong>
          </div>
          <div>
            <strong>THỦ QUỸ / ĐẠI DIỆN TÒA SOẠN</strong><br>
            <span style="font-size: 12px; color: #64748b;">(Ký tên và đóng dấu)</span><br><br><br><br>
            <strong>PHÒNG PHÁT HÀNH BÁO CHÍ</strong>
          </div>
        </div>
      </body>
      </html>
    `);
    printWindow.document.close();
  },

  /**
   * Xuất Phiếu phát hành báo PDF
   */
  inPhieuPhatHanhPDF: function (ph) {
    if (!ph) return;
    const printWindow = window.open('', '_blank', 'width=850,height=900');
    if (!printWindow) {
      alert('Vui lòng cho phép popup để in phiếu!');
      return;
    }

    printWindow.document.write(`
      <!DOCTYPE html>
      <html lang="vi">
      <head>
        <meta charset="UTF-8">
        <title>Phiếu Phát Hành Báo - #PH-${ph.maPhatHanh}</title>
        <style>
          body { font-family: Arial, sans-serif; margin: 30px; color: #1e293b; }
          .header { border-bottom: 2px solid #059669; padding-bottom: 15px; margin-bottom: 20px; }
          .title { text-align: center; font-size: 22px; font-weight: 800; color: #059669; margin: 20px 0; }
          .box { border: 1px solid #cbd5e1; padding: 20px; border-radius: 8px; margin-bottom: 25px; background: #f8fafc; }
          .row { display: flex; justify-content: space-between; margin-bottom: 10px; font-size: 15px; }
          .btn-in { position: fixed; top: 15px; right: 15px; background: #059669; color: #fff; padding: 10px 20px; border-radius: 6px; border: none; cursor: pointer; font-weight: 600; }
          @media print { .btn-in { display: none; } body { margin: 0; } }
        </style>
      </head>
      <body>
        <button class="btn-in" onclick="window.print()">🖨️ In / Lưu PDF</button>
        <div class="header">
          <h2>BAN BIÊN TẬP & PHÒNG PHÁT HÀNH BÁO CHÍ</h2>
          <p>Hệ thống Quản lý Vận chuyển & Giao nhận Báo tận nơi</p>
        </div>
        <div class="title">PHIẾU ĐIỀU PHỐI PHÁT HÀNH BÁO</div>
        <div class="box">
          <div class="row"><span>Mã Lệnh Phát Hành:</span><strong>#PH-${ph.maPhatHanh}</strong></div>
          <div class="row"><span>Mã Thuê Bao Đăng Ký:</span><strong>#ĐK-${ph.maDangKy}</strong></div>
          <div class="row"><span>Người Nhận Báo:</span><strong>${ph.tenDocGia || 'Khách hàng độc giả'}</strong></div>
          <div class="row"><span>Ấn Phẩm & Số Kỳ:</span><strong style="color: #4f46e5;">${ph.tenAnPham || 'Báo ngày'} - Số #${ph.soThuTuBao || ph.maSoBao}</strong></div>
          <div class="row"><span>Ngày Phát Hành:</span><strong>${ph.ngayPhatHanh}</strong></div>
          <div class="row"><span>Số Lượng Giao:</span><strong>${ph.soLuong || 1} bản</strong></div>
          <div class="row"><span>Hình Thức Giao Nhận:</span><strong>${ph.hinhThuc || 'Giao báo tận nơi'}</strong></div>
          <div class="row"><span>Địa Chỉ Nhận:</span><strong style="color: #dc2626;">${ph.diaChiGiao || 'Địa chỉ đăng ký'}</strong></div>
          <div class="row"><span>Trạng Thái Hiện Tại:</span><strong>${ph.trangThai}</strong></div>
        </div>
        <div style="display: flex; justify-content: space-between; margin-top: 50px; text-align: center;">
          <div><strong>NHÂN VIÊN GIAO BÁO</strong><br><br><br><br>..................................................</div>
          <div><strong>NGƯỜI KÝ NHẬN BÁO</strong><br><br><br><br>..................................................</div>
        </div>
      </body>
      </html>
    `);
    printWindow.document.close();
  },

  /**
   * In-App Toast Notification thời gian thực
   */
  thongBao: function (thongDiep, loai = 'thong-tin') {
    let container = document.getElementById('toast-container');
    if (!container) {
      container = document.createElement('div');
      container.id = 'toast-container';
      container.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 9999;
        display: flex;
        flex-direction: column;
        gap: 10px;
        pointer-events: none;
      `;
      document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.style.cssText = `
      min-width: 280px;
      max-width: 420px;
      padding: 12px 18px;
      border-radius: 10px;
      font-size: 14px;
      font-weight: 500;
      color: #ffffff;
      box-shadow: 0 10px 25px -5px rgba(0,0,0,0.25);
      display: flex;
      align-items: center;
      gap: 12px;
      pointer-events: auto;
      transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
      transform: translateX(100%);
      opacity: 0;
    `;

    let icon = 'bi-info-circle-fill';
    let bgColor = '#4f46e5';

    if (loai === 'thanh-cong') {
      bgColor = '#10b981';
      icon = 'bi-check-circle-fill';
    } else if (loai === 'canh-bao') {
      bgColor = '#f59e0b';
      icon = 'bi-exclamation-triangle-fill';
    } else if (loai === 'nguy-hiem') {
      bgColor = '#ef4444';
      icon = 'bi-x-circle-fill';
    }

    toast.style.backgroundColor = bgColor;
    toast.innerHTML = `<i class="bi ${icon}" style="font-size: 18px;"></i> <span>${thongDiep}</span>`;
    container.appendChild(toast);

    requestAnimationFrame(() => {
      toast.style.transform = 'translateX(0)';
      toast.style.opacity = '1';
    });

    setTimeout(() => {
      toast.style.transform = 'translateX(100%)';
      toast.style.opacity = '0';
      setTimeout(() => {
        if (toast.parentNode) {
          toast.parentNode.removeChild(toast);
        }
      }, 300);
    }, 3500);
  }
};
