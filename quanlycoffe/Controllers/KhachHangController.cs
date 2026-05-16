using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using quanlycoffe.Models; // Đảm bảo trùng khớp với Namespace Models của bạn

namespace quanlycoffe.Controllers
{
    public class KhachHangController : Controller
    {
        private BMSModel db = new BMSModel();

        // =========================================================
        // 1. DANH SÁCH KHÁCH HÀNG (GET)
        // =========================================================
        public ActionResult Index()
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            var danhSach = db.KhachHang.ToList();
            return View(danhSach);
        }

        // =========================================================
        // 2. MỞ FORM THÊM MỚI KHÁCH HÀNG (GET)
        // =========================================================
        public ActionResult Create()
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            ViewBag.MaKV = new SelectList(db.plp_KhuVuc, "MaKV", "TenKV");
            ViewBag.MaNhomKH = new SelectList(db.plp_NhomKH, "MaNhomKH", "TenNhomKH");
            ViewBag.MaHang = new SelectList(db.plp_HangThanhVien, "MaHang", "TenHang");

            return View();
        }

        // =========================================================
        // 3. XỬ LÝ LƯU KHÁCH HÀNG VÀO DATABASE (POST)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KhachHang kh)
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            try
            {
                if (ModelState.IsValid)
                {
                    kh.DiemTichLuy = 0;
                    kh.NgaySinh = DateTime.Now;

                    db.KhachHang.Add(kh);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }

                ViewBag.MaKV = new SelectList(db.plp_KhuVuc, "MaKV", "TenKV", kh.MaKV);
                ViewBag.MaNhomKH = new SelectList(db.plp_NhomKH, "MaNhomKH", "TenNhomKH", kh.MaNhomKH);
                ViewBag.MaHang = new SelectList(db.plp_HangThanhVien, "MaHang", "TenHang", kh.MaHang);
                return View(kh);
            }
            catch (Exception ex)
            {
                Exception rootCause = ex;
                while (rootCause.InnerException != null) { rootCause = rootCause.InnerException; }
                ViewBag.Error = "Lỗi SQL Server: " + rootCause.Message;

                ViewBag.MaKV = new SelectList(db.plp_KhuVuc, "MaKV", "TenKV", kh.MaKV);
                ViewBag.MaNhomKH = new SelectList(db.plp_NhomKH, "MaNhomKH", "TenNhomKH", kh.MaNhomKH);
                ViewBag.MaHang = new SelectList(db.plp_HangThanhVien, "MaHang", "TenHang", kh.MaHang);
                return View(kh);
            }
        }

        // =========================================================
        // 4. MỞ FORM CHỈNH SỬA KHÁCH HÀNG (GET)
        // =========================================================
        public ActionResult Edit(string id)
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            var kh = db.KhachHang.SingleOrDefault(x => x.MaKH == id);
            if (kh == null) return HttpNotFound();

            ViewBag.MaKV = new SelectList(db.plp_KhuVuc, "MaKV", "TenKV", kh.MaKV);
            ViewBag.MaNhomKH = new SelectList(db.plp_NhomKH, "MaNhomKH", "TenNhomKH", kh.MaNhomKH);
            ViewBag.MaHang = new SelectList(db.plp_HangThanhVien, "MaHang", "TenHang", kh.MaHang);

            return View(kh);
        }

        // =========================================================
        // 5. LƯU THÔNG TIN CHỈNH SỬA VÀO DATABASE (POST)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(KhachHang kh)
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            try
            {
                if (ModelState.IsValid)
                {
                    var editKh = db.KhachHang.SingleOrDefault(x => x.MaKH == kh.MaKH);
                    if (editKh != null)
                    {
                        editKh.TenKH = kh.TenKH;
                        editKh.DienThoai = kh.DienThoai;
                        editKh.DiaChi = kh.DiaChi;

                        // Cập nhật điểm tích lũy và các khóa ngoại
                        editKh.DiemTichLuy = kh.DiemTichLuy;
                        editKh.MaKV = kh.MaKV;
                        editKh.MaNhomKH = kh.MaNhomKH;

                        db.SaveChanges(); // Lưu thông tin cơ bản trước

                        // Gọi hàm tự động cập nhật Hạng và Nhóm sau khi lưu điểm mới
                        TuDongCapNhatTrangThai(editKh.MaKH);

                        return RedirectToAction("Index");
                    }
                }

                ViewBag.MaKV = new SelectList(db.plp_KhuVuc, "MaKV", "TenKV", kh.MaKV);
                ViewBag.MaNhomKH = new SelectList(db.plp_NhomKH, "MaNhomKH", "TenNhomKH", kh.MaNhomKH);
                ViewBag.MaHang = new SelectList(db.plp_HangThanhVien, "MaHang", "TenHang", kh.MaHang);
                return View(kh);
            }
            catch (Exception ex)
            {
                Exception rootCause = ex;
                while (rootCause.InnerException != null) { rootCause = rootCause.InnerException; }
                ViewBag.Error = "Lỗi cập nhật SQL Server: " + rootCause.Message;

                ViewBag.MaKV = new SelectList(db.plp_KhuVuc, "MaKV", "TenKV", kh.MaKV);
                ViewBag.MaNhomKH = new SelectList(db.plp_NhomKH, "MaNhomKH", "TenNhomKH", kh.MaNhomKH);
                ViewBag.MaHang = new SelectList(db.plp_HangThanhVien, "MaHang", "TenHang", kh.MaHang);
                return View(kh);
            }
        }

        // =========================================================
        // 6. XỬ LÝ XÓA KHÁCH HÀNG 
        // =========================================================
        public ActionResult Delete(string id)
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            try
            {
                var kh = db.KhachHang.SingleOrDefault(x => x.MaKH == id);
                if (kh != null)
                {
                    db.KhachHang.Remove(kh);
                    db.SaveChanges();
                }
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["Error"] = "Không thể xóa khách hàng này vì họ đã phát sinh giao dịch trong hệ thống!";
                return RedirectToAction("Index");
            }
        }

        // =========================================================
        // 7. HÀM NGHIỆP VỤ: TỰ ĐỘNG LÊN HẠNG & CHUYỂN NHÓM KHÁCH
        // =========================================================
        private void TuDongCapNhatTrangThai(string maKH)
        {
            var kh = db.KhachHang.SingleOrDefault(x => x.MaKH == maKH);
            if (kh != null)
            {
                // 1. Tự động xét lên Hạng Thẻ (Dựa vào điểm tối thiểu)
                var hangXungDang = db.plp_HangThanhVien
                                     .Where(h => h.DiemToiThieu <= kh.DiemTichLuy)
                                     .OrderByDescending(h => h.DiemToiThieu)
                                     .FirstOrDefault();

                if (hangXungDang != null && kh.MaHang != hangXungDang.MaHang)
                {
                    kh.MaHang = hangXungDang.MaHang;
                }

                // 2. Tự động xét chuyển Nhóm Khách Hàng (Dựa vào mốc điểm)
                string tenNhomMoi = "";
                if (kh.DiemTichLuy >= 500)
                {
                    tenNhomMoi = "Khách VIP";
                }
                else if (kh.DiemTichLuy >= 50)
                {
                    tenNhomMoi = "Khách quen";
                }

                if (!string.IsNullOrEmpty(tenNhomMoi))
                {
                    var nhomMoi = db.plp_NhomKH.FirstOrDefault(n => n.TenNhomKH.Contains(tenNhomMoi));
                    if (nhomMoi != null && kh.MaNhomKH != nhomMoi.MaNhomKH)
                    {
                        kh.MaNhomKH = nhomMoi.MaNhomKH;
                    }
                }

                db.SaveChanges(); // Lưu các cập nhật hệ thống tự tính xuống DB
            }
        }
    }
}