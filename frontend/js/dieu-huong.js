/**
 * dieu-huong.js — Hệ thống điều hướng Fluid Responsive cho CT LMS
 * - Desktop (>= 1024px): Top Horizontal Navbar + Expandable Search Input
 * - Mobile/Tablet (< 1024px): Slide-out Drawer Menu (Hamburger) + Floating Search Bar
 * - User Chip co giãn thông minh (ẩn tên trên màn hình nhỏ < 640px)
 */

var DieuHuong = (function () {
  'use strict';

  function khoiTao() {
    // 1. Xóa bỏ hoàn toàn sidebar dọc cũ nếu còn sót
    var oldSidebar = document.getElementById('sidebar');
    if (oldSidebar) oldSidebar.remove();
    var oldOverlay = document.getElementById('overlay-sidebar');
    if (oldOverlay) oldOverlay.remove();

    var currentFile = window.location.pathname.split('/').pop() || 'tong-quan.html';
    if (currentFile === 'dang-nhap.html') {
      khoiTaoDrawerDangNhap();
      return;
    }

    var nguoiDung = (typeof XacThuc !== 'undefined') ? XacThuc.layNguoiDung() : null;
    if (!nguoiDung) {
      if (typeof XacThuc !== 'undefined') {
        XacThuc.kiemTraDangNhap();
      }
      return;
    }

    var vaiTro = nguoiDung.quyenHan || 'Học viên';
    renderTopNav(vaiTro, currentFile);
    renderUserActions(nguoiDung, vaiTro);
    renderMobileDrawerAndSearch(nguoiDung, vaiTro, currentFile);
  }

  function layDanhSachMenu(vaiTro) {
    var menuConfig = (typeof CAU_HINH_MENU !== 'undefined')
      ? (CAU_HINH_MENU[vaiTro] || CAU_HINH_MENU['Học viên'])
      : [];
    var items = [];
    menuConfig.forEach(function (nhom) {
      nhom.muc.forEach(function (item) {
        items.push(item);
      });
    });
    return items;
  }

  function renderTopNav(vaiTro, currentFile) {
    var container = document.getElementById('header-navbar-links');
    if (!container) return;

    var items = layDanhSachMenu(vaiTro);
    var html = '';
    items.forEach(function (item) {
      var isActive = (currentFile === item.file) ? ' active' : '';
      html += '<a href="' + item.file + '" class="nav-link' + isActive + '" title="' + (item.tooltip || item.nhan) + '">';
      html += '<i class="bi ' + item.icon + '"></i> ';
      html += '<span>' + item.nhan + '</span>';
      html += '</a>';
    });

    container.innerHTML = html;

    // Đảm bảo có nút Hamburger bên cạnh Logo cho màn hình < 1024px
    var headerContainer = container.closest('.header-container');
    if (headerContainer && !document.getElementById('hamburger-trigger')) {
      var brand = headerContainer.querySelector('.header-brand');
      if (brand) {
        var btn = document.createElement('button');
        btn.type = 'button';
        btn.id = 'hamburger-trigger';
        btn.className = 'hamburger-btn';
        btn.setAttribute('aria-label', 'Mở menu điều hướng');
        btn.innerHTML = '<i class="bi bi-list"></i>';
        btn.onclick = toggleMobileDrawer;
        brand.parentNode.insertBefore(btn, brand);
      }
    }
  }

  function renderUserActions(nguoiDung, vaiTro) {
    var actionsContainer = document.getElementById('header-user-actions');
    if (!actionsContainer) {
      var header = document.querySelector('.header-container');
      if (header) {
        actionsContainer = header.querySelector('.header-actions');
      }
    }
    if (!actionsContainer) return;

    var hoTen = nguoiDung.hoTen || nguoiDung.tenDangNhap || 'Học viên';
    var chuDau = hoTen.charAt(0).toUpperCase();

    var html = '';

    // 1. Chỉ hiển thị icon kính lúp trên Header (click vào bật popup từ gáy trên của trang xuống)
    html += '<button type="button" class="header-search-trigger" onclick="DieuHuong.openTopSearch()" title="Tìm kiếm (Click hoặc nhấn ESC để đóng)">';
    html += '  <i class="bi bi-search" style="font-size: 1rem;"></i>';
    html += '</button>';

    // 2. Chuông thông báo
    html += '<a href="thong-bao.html" class="nav-link" style="padding: 0.4rem 0.55rem; position: relative;" title="Thông báo">';
    html += '  <i class="bi bi-bell" style="font-size: 1.15rem;"></i>';
    html += '  <span style="position: absolute; top: 4px; right: 4px; width: 8px; height: 8px; background: #ef4444; border-radius: 50%; border: 1.5px solid #fff;"></span>';
    html += '</a>';

    // 3. User Chip (Avatar + Tên tự ẩn trên mobile < 640px + Role Badge)
    html += '<a href="thong-tin-ca-nhan.html" class="user-chip" title="Hồ sơ: ' + hoTen + ' (' + vaiTro + ')">';
    html += '  <div class="user-avatar">' + chuDau + '</div>';
    html += '  <span class="user-name-text">' + hoTen + '</span>';
    html += '  <span class="role-tag">' + vaiTro + '</span>';
    html += '</a>';

    // 4. Nút Đăng xuất
    html += '<a href="dang-nhap.html" onclick="if(typeof XacThuc!==\'undefined\'){XacThuc.dangXuat();}" class="btn btn-secondary" style="padding: 0.42rem 0.65rem;" title="Đăng xuất">';
    html += '  <i class="bi bi-box-arrow-right"></i>';
    html += '</a>';

    actionsContainer.innerHTML = html;
  }

  function renderMobileDrawerAndSearch(nguoiDung, vaiTro, currentFile) {
    // Popup Thanh Tìm Kiếm Trượt Từ Gáy Trên Xuống (Search or hit ESC..)
    if (!document.getElementById('search-top-dropdown')) {
      var backdrop = document.createElement('div');
      backdrop.id = 'search-top-backdrop';
      backdrop.className = 'search-top-backdrop';
      backdrop.onclick = closeTopSearch;

      var topDropdown = document.createElement('div');
      topDropdown.id = 'search-top-dropdown';
      topDropdown.className = 'search-top-dropdown';
      topDropdown.innerHTML =
        '<form onsubmit="return DieuHuong.xuLyTimKiemHeader(event)" class="search-top-bar">' +
        '  <button type="button" class="search-top-close-btn" onclick="DieuHuong.closeTopSearch()" title="Đóng tìm kiếm (ESC)">' +
        '    <i class="bi bi-x-lg"></i>' +
        '  </button>' +
        '  <input type="text" id="top-search-input" class="search-top-input" placeholder="Search or hit ESC.." oninput="DieuHuong.dongBoTimKiem(this.value)" autocomplete="off" />' +
        '  <button type="submit" class="search-top-submit-btn" title="Tìm kiếm">' +
        '    <i class="bi bi-search"></i>' +
        '  </button>' +
        '</form>';

      document.body.appendChild(backdrop);
      document.body.appendChild(topDropdown);

      // Phím tắt ESC để đóng popup tìm kiếm
      document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' || e.keyCode === 27) {
          closeTopSearch();
        }
      });
    }

    // Drawer điều hướng Mobile
    if (document.getElementById('mobile-drawer')) return;

    var hoTen = nguoiDung.hoTen || nguoiDung.tenDangNhap || 'Học viên';
    var chuDau = hoTen.charAt(0).toUpperCase();
    var items = layDanhSachMenu(vaiTro);

    var backdrop = document.createElement('div');
    backdrop.id = 'drawer-backdrop';
    backdrop.className = 'drawer-backdrop';
    backdrop.onclick = closeMobileDrawer;

    var drawer = document.createElement('aside');
    drawer.id = 'mobile-drawer';
    drawer.className = 'mobile-drawer';

    var linksHtml = '';
    items.forEach(function (item) {
      var isActive = (currentFile === item.file);
      linksHtml +=
        '<a href="' + item.file + '" style="display:flex; align-items:center; gap:0.75rem; padding:0.75rem 1rem; border-radius:10px; text-decoration:none; font-weight:' + (isActive ? '700' : '600') + '; font-size:14px; color:' + (isActive ? '#0284c7' : '#334155') + '; background:' + (isActive ? '#e0f2fe' : 'transparent') + ';">' +
        '  <i class="bi ' + item.icon + '" style="font-size:1.1rem;"></i>' +
        '  <span>' + item.nhan + '</span>' +
        '</a>';
    });

    drawer.innerHTML =
      '<div style="padding:1rem 1.25rem; border-bottom:1px solid #e2e8f0; display:flex; align-items:center; justify-content:space-between;">' +
      '  <img src="../img/ct-lms-logo.svg" alt="CT LMS Logo" style="height:32px; width:auto; object-fit:contain;" />' +
      '  <button type="button" onclick="DieuHuong.closeMobileDrawer()" style="width:32px; height:32px; border-radius:8px; border:1px solid #e2e8f0; background:#f8fafc; display:flex; align-items:center; justify-content:center; cursor:pointer;">' +
      '    <i class="bi bi-x-lg"></i>' +
      '  </button>' +
      '</div>' +
      '<div style="padding:1rem 1.25rem; background:#f8fafc; border-bottom:1px solid #e2e8f0; display:flex; align-items:center; gap:0.75rem;">' +
      '  <div style="width:40px; height:40px; border-radius:50%; background:linear-gradient(135deg,#0284c7,#0369a1); color:#fff; display:flex; align-items:center; justify-content:center; font-weight:700; font-size:15px;">' + chuDau + '</div>' +
      '  <div style="min-width:0; flex:1;">' +
      '    <div style="font-weight:700; font-size:14px; color:#0f172a; white-space:nowrap; overflow:hidden; text-overflow:ellipsis;">' + hoTen + '</div>' +
      '    <span class="role-tag" style="display:inline-block; margin-top:2px;">' + vaiTro + '</span>' +
      '  </div>' +
      '</div>' +
      '<nav style="flex:1; overflow-y:auto; padding:0.75rem; display:flex; flex-direction:column; gap:0.25rem;">' +
      linksHtml +
      '  <hr style="border:none; border-top:1px solid #e2e8f0; margin:0.5rem 0;" />' +
      '  <a href="thong-tin-ca-nhan.html" style="display:flex; align-items:center; gap:0.75rem; padding:0.75rem 1rem; border-radius:10px; text-decoration:none; font-weight:600; font-size:14px; color:#334155;">' +
      '    <i class="bi bi-person-circle" style="font-size:1.1rem;"></i> <span>Hồ sơ cá nhân</span>' +
      '  </a>' +
      '  <a href="thong-bao.html" style="display:flex; align-items:center; gap:0.75rem; padding:0.75rem 1rem; border-radius:10px; text-decoration:none; font-weight:600; font-size:14px; color:#334155;">' +
      '    <i class="bi bi-bell" style="font-size:1.1rem;"></i> <span>Hộp thư thông báo</span>' +
      '  </a>' +
      '</nav>' +
      '<div style="padding:1rem 1.25rem; border-top:1px solid #e2e8f0;">' +
      '  <a href="dang-nhap.html" onclick="if(typeof XacThuc!==\'undefined\'){XacThuc.dangXuat();}" class="btn btn-secondary" style="width:100%; justify-content:center; padding:0.65rem; color:#dc2626;">' +
      '    <i class="bi bi-box-arrow-right"></i> Đăng xuất hệ thống' +
      '  </a>' +
      '</div>';

    document.body.appendChild(backdrop);
    document.body.appendChild(drawer);
  }

  function khoiTaoDrawerDangNhap() {
    // Khởi tạo từ khóa tìm kiếm trên URL nếu có
  }

  function toggleMobileDrawer() {
    var drawer = document.getElementById('mobile-drawer');
    var backdrop = document.getElementById('drawer-backdrop');
    if (drawer && backdrop) {
      drawer.classList.toggle('open');
      backdrop.classList.toggle('open');
    }
  }

  function closeMobileDrawer() {
    var drawer = document.getElementById('mobile-drawer');
    var backdrop = document.getElementById('drawer-backdrop');
    if (drawer && backdrop) {
      drawer.classList.remove('open');
      backdrop.classList.remove('open');
    }
  }

  function openTopSearch() {
    var dropdown = document.getElementById('search-top-dropdown');
    var backdrop = document.getElementById('search-top-backdrop');
    if (dropdown && backdrop) {
      dropdown.classList.add('open');
      backdrop.classList.add('open');
      var inp = document.getElementById('top-search-input');
      if (inp) {
        setTimeout(function () {
          inp.focus();
          inp.select();
        }, 120);
      }
    }
  }

  function closeTopSearch() {
    var dropdown = document.getElementById('search-top-dropdown');
    var backdrop = document.getElementById('search-top-backdrop');
    if (dropdown && backdrop) {
      dropdown.classList.remove('open');
      backdrop.classList.remove('open');
    }
  }

  function toggleTopSearch() {
    var dropdown = document.getElementById('search-top-dropdown');
    if (dropdown && dropdown.classList.contains('open')) {
      closeTopSearch();
    } else {
      openTopSearch();
    }
  }

  function dongBoTimKiem(val) {
    // Nếu đang ở trang tra-cuu-khoa-hoc.html, lọc realtime qua Angular scope
    var catalogSearchInput = document.querySelector('.search-bar input[ng-model="vm.boLoc.tuKhoa"]');
    if (catalogSearchInput && typeof angular !== 'undefined') {
      var scope = angular.element(catalogSearchInput).scope();
      if (scope && scope.vm) {
        scope.$applyAsync(function () {
          scope.vm.boLoc.tuKhoa = val;
          if (typeof scope.vm.timKiem === 'function') {
            scope.vm.timKiem();
          }
        });
      }
    }
  }

  function xuLyTimKiemHeader(e) {
    if (e) e.preventDefault();
    var topInp = document.getElementById('top-search-input');
    var q = (topInp && topInp.value) ? topInp.value : '';
    closeTopSearch();
    var currentFile = window.location.pathname.split('/').pop() || '';
    if (currentFile !== 'tra-cuu-khoa-hoc.html') {
      sessionStorage.setItem('app_search_query', q);
      window.location.href = 'tra-cuu-khoa-hoc.html';
    } else {
      dongBoTimKiem(q);
    }
    return false;
  }

  return {
    khoiTao: khoiTao,
    toggleMobileDrawer: toggleMobileDrawer,
    closeMobileDrawer: closeMobileDrawer,
    openTopSearch: openTopSearch,
    closeTopSearch: closeTopSearch,
    toggleTopSearch: toggleTopSearch,
    dongBoTimKiem: dongBoTimKiem,
    xuLyTimKiemHeader: xuLyTimKiemHeader
  };
})();

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', DieuHuong.khoiTao);
} else {
  DieuHuong.khoiTao();
}
