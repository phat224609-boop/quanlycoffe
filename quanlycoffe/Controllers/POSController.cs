using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using quanlycoffe.Models;
using System.Data.Entity;

namespace quanlycoffe.Controllers
{
    public class KhachHangViewModel
    {
        public string MaKH { get; set; }
        public string TenKH { get; set; }
        public string TenHang { get; set; }
        public decimal GiamGia { get; set; }
    }

    public class CartItem
    {
        public string MaSP { get; set; }
        public string TenSP { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
    }

    public class POSController : Controller
    {
        private BMSModel db = new BMSModel();

        public ActionResult Index(int? maBan)
        {
            if (maBan == null) return RedirectToAction("SoDoBan", "Ban");

            if (maBan == 0)
            {
                ViewBag.TenBan = "Khách Mua Mang Đi";
                ViewBag.MaBan = 0;
            }
            else
            {
                var ban = db.plp_Ban.Find(maBan);
                if (ban == null) return HttpNotFound();
                ViewBag.TenBan = ban.TenBan;
                ViewBag.MaBan = ban.MaBan;
            }

            ViewBag.DSMonAn = db.SanPham.ToList();

            ViewBag.DSKhachHang = db.KhachHang
                .Include(k => k.plp_HangThanhVien)
                .Select(k => new KhachHangViewModel
                {
                    MaKH = k.MaKH,
                    TenKH = k.TenKH,
                    GiamGia = k.plp_HangThanhVien != null ? (decimal)k.plp_HangThanhVien.TyLeGiamGia : 0,
                    TenHang = k.plp_HangThanhVien != null ? k.plp_HangThanhVien.TenHang : "Chưa có hạng"
                }).ToList();

            // ==========================================
            // BƯỚC 1: LẤY DANH SÁCH NHÂN VIÊN PHỤC VỤ
            // ==========================================
            try
            {
                var dsPhucVu = db.NhanVien.Where(x => x.plp_NghiepVu != null && x.plp_NghiepVu.TenNghiepVu.Contains("Phục vụ")).ToList();
                ViewBag.DSNhanVien = dsPhucVu;
            }
            catch (Exception)
            {
                // Phòng trường hợp lỗi gián đoạn web
                ViewBag.DSNhanVien = null;
            }
            // ==========================================

            return View();
        }

        [HttpPost]
        // ĐÃ SỬA: Thêm 'int maNV' vào để hứng dữ liệu
        public ActionResult ThanhToanBill(string maKH, int maNV, List<CartItem> gioHang, decimal tongThu, int maBan)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    if (gioHang == null || gioHang.Count == 0)
                        return Json(new { success = false, message = "Giỏ hàng không có món nào!" });

                    // Lấy ID của người đang thu tiền (đăng nhập vào phần mềm) để lưu vết
                    int currentUserId = Session["UserID"] != null ? (int)Session["UserID"] : 1;

                    string maHD = "HD" + DateTime.Now.ToString("ddHHmmss");

                    // A. Khởi tạo Hóa Đơn
                    HoaDonBH hd = new HoaDonBH
                    {
                        MaHD = maHD,
                        NgayBan = DateTime.Now,
                        MaKH = string.IsNullOrEmpty(maKH) ? "KVL" : maKH,
                        MaBan = maBan == 0 ? (int?)null : maBan,
                        TongThu = tongThu,
                        DaThanhToan = 0,

                        // LƯU CHÍNH XÁC MÃ NHÂN VIÊN ĐƯỢC CHỌN TỪ GIAO DIỆN
                        MANV = maNV,
                        UserID = currentUserId, // Lưu người bấm máy thu ngân

                        TangThem = 0,
                        CongNo = 0,
                        LoiNhuanHD = 0,
                        TrangThaiPhaChe = 0,
                        GhiChu = ""
                    };
                    db.HoaDonBH.Add(hd);

                    // B. Lưu chi tiết hóa đơn
                    foreach (var item in gioHang)
                    {
                        db.CTHoaDonBH.Add(new CTHoaDonBH
                        {
                            MaHD = maHD,
                            MaSP = item.MaSP,
                            SoLuong = item.SoLuong,
                            DonGia = item.DonGia,
                            ThanhTien = item.SoLuong * item.DonGia,
                            DVT = "Ly"
                        });
                    }

                    // C. Cập nhật trạng thái bàn thành ĐANG PHỤC VỤ (Màu đỏ)
                    if (maBan > 0)
                    {
                        var ban = db.plp_Ban.Find(maBan);
                        if (ban != null) ban.TrangThai = 1; // 1 = Có khách / Đang phục vụ
                    }

                    // D. TÍCH ĐIỂM CHO KHÁCH HÀNG VÀ THĂNG HẠNG TỰ ĐỘNG
                    if (!string.IsNullOrEmpty(maKH) && maKH != "KVL")
                    {
                        var kh = db.KhachHang.Find(maKH);
                        if (kh != null)
                        {
                            if (kh.DiemTichLuy == null)
                            {
                                kh.DiemTichLuy = 0;
                            }

                            int diemCongThem = (int)(tongThu / 10000);
                            kh.DiemTichLuy += diemCongThem;

                            TuDongCapNhatTrangThai(kh.MaKH);
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "Đã lưu hóa đơn thành công!" });
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    transaction.Rollback();
                    string errorMsg = "";
                    foreach (var eve in ex.EntityValidationErrors)
                    {
                        foreach (var ve in eve.ValidationErrors)
                        {
                            errorMsg += ve.PropertyName + ": " + ve.ErrorMessage + " | ";
                        }
                    }
                    return Json(new { success = false, message = "Lỗi dữ liệu: " + errorMsg });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    string msg = ex.InnerException != null ? (ex.InnerException.InnerException != null ? ex.InnerException.InnerException.Message : ex.InnerException.Message) : ex.Message;
                    return Json(new { success = false, message = "Lỗi hệ thống: " + msg });
                }
            }
        }

        private void TuDongCapNhatTrangThai(string maKH)
        {
            var kh = db.KhachHang.Find(maKH);
            if (kh == null) return;

            var hangPhuHop = db.plp_HangThanhVien
                               .Where(h => kh.DiemTichLuy >= h.DiemToiThieu)
                               .OrderByDescending(h => h.DiemToiThieu)
                               .FirstOrDefault();

            if (hangPhuHop != null && kh.MaHang != hangPhuHop.MaHang)
            {
                kh.MaHang = hangPhuHop.MaHang;
            }
        }

        public ActionResult LichSu()
        {
            var ds = db.HoaDonBH.OrderByDescending(x => x.NgayBan).ToList();
            return View(ds);
        }

        [HttpPost]
        public ActionResult CapNhatTrangThai(string maHD, decimal trangThaiMoi)
        {
            var hd = db.HoaDonBH.Find(maHD);
            if (hd != null)
            {
                hd.DaThanhToan = trangThaiMoi;

                // LOGIC ĐỔI MÀU BÀN KHI THANH TOÁN
                if (hd.MaBan != null)
                {
                    var ban = db.plp_Ban.Find(hd.MaBan);
                    if (ban != null)
                    {
                        if (trangThaiMoi == 1) ban.TrangThai = 0;
                        else ban.TrangThai = 1;
                    }
                }

                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy hóa đơn" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { db.Dispose(); }
            base.Dispose(disposing);
        }
    }
}