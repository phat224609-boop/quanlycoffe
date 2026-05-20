using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using quanlycoffe.Models;

namespace quanlycoffe.Controllers
{
    public class HoaDonController : Controller
    {
        private BMSModel db = new BMSModel();

        // Mở trang Lịch sử
        public ActionResult LichSu()
        {
            var ds = db.HoaDonBH.OrderByDescending(x => x.NgayBan).ToList();
            return View(ds);
        }

        // Hàm xử lý khi bấm nút "Đã trả tiền" hoặc "Chờ lại"
        [HttpPost]
        public ActionResult CapNhatTrangThai(string maHD, decimal trangThaiMoi)
        {
            var hd = db.HoaDonBH.Find(maHD);
            if (hd != null)
            {
                // 1. Cập nhật trạng thái hóa đơn
                hd.DaThanhToan = trangThaiMoi;
                db.Entry(hd).State = EntityState.Modified; // Ép hệ thống lưu thay đổi hóa đơn

                // 2. BẬT LOGIC ĐỔI MÀU BÀN TẠI ĐÂY
                if (hd.MaBan != null)
                {
                    var ban = db.plp_Ban.Find(hd.MaBan);
                    if (ban != null)
                    {
                        // Nếu trangThaiMoi = 1 (Đã trả tiền) -> Giải phóng bàn (0) -> Xanh
                        if (trangThaiMoi == 1)
                        {
                            ban.TrangThai = 0;
                        }
                        // Nếu bấm "Chờ lại" (0) -> Kéo bàn lại trạng thái có khách (1) -> Đỏ
                        else
                        {
                            ban.TrangThai = 1;
                        }

                        // Ép cứng hệ thống lưu trạng thái của bàn xuống SQL
                        db.Entry(ban).State = EntityState.Modified;
                    }
                }

                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy hóa đơn" });
        }

        // ==========================================
        // LẤY CHI TIẾT HÓA ĐƠN BẰNG AJAX (ĐÃ THÊM LẤY TÊN NHÂN VIÊN)
        // ==========================================
        [HttpGet]
        public ActionResult GetChiTietHoaDon(string maHD)
        {
            try
            {
                // Kết nối bảng Chi Tiết Hóa Đơn và bảng Sản Phẩm để lấy Tên món
                var chiTiet = (from ct in db.CTHoaDonBH
                               join sp in db.SanPham on ct.MaSP equals sp.MaSP
                               where ct.MaHD == maHD
                               select new
                               {
                                   TenSP = sp.TenSP,
                                   SoLuong = ct.SoLuong,
                                   DonGia = ct.DonGia,
                                   ThanhTien = ct.ThanhTien
                               }).ToList();

                // --- TÌM TÊN NHÂN VIÊN TRONG DATABASE ---
                string tenNV = "Không xác định";
                var hd = db.HoaDonBH.Find(maHD);
                if (hd != null && hd.MANV != null)
                {
                    var nv = db.NhanVien.Find(hd.MANV);
                    if (nv != null) tenNV = nv.TenNV;
                }

                // Trả về kèm theo biến tenNhanVien
                return Json(new { success = true, data = chiTiet, tenNhanVien = tenNV }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { db.Dispose(); }
            base.Dispose(disposing);
        }
    }
}