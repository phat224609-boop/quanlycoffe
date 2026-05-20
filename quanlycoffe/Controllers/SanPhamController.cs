using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using quanlycoffe.Models;

namespace quanlycoffe.Controllers
{
    public class SanPhamController : Controller
    {
        private BMSModel db = new BMSModel();

        // ==========================================
        // HÀM KIỂM TRA QUYỀN ADMIN
        // ==========================================
        private bool IsAdmin()
        {
            return Session["Quyen"] != null && Session["Quyen"].ToString() == "Admin";
        }

        // ==========================================
        // 1. DANH SÁCH SẢN PHẨM (Yêu cầu đăng nhập)
        // ==========================================
        public ActionResult Index()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Gọi thêm DanhMuc để hiển thị TenDanhMuc ra bảng
            var sanPhams = db.SanPham.Include(s => s.DanhMuc);
            return View(sanPhams.ToList());
        }

        // ==========================================
        // 2. THÊM SẢN PHẨM
        // ==========================================
        [HttpGet]
        public ActionResult Create()
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin")
            {
                return Content("Bạn không có quyền truy cập!");
            }

            ViewBag.MaDM = new SelectList(db.DanhMuc, "MaDM", "TenDanhMuc");

            // Đảm bảo truyền MANCC là kiểu số, lấy khóa chính MANCC và hiển thị TenCongTy
            ViewBag.MANCC = new SelectList(db.NhaCungCap, "MANCC", "TenCongTy");

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SanPham sp)
        {
            ModelState.Clear();

            var checkTrung = db.SanPham.Find(sp.MaSP);
            if (checkTrung != null)
            {
                ModelState.AddModelError("", "Mã sản phẩm này đã tồn tại trong hệ thống!");
                ViewBag.MaDM = new SelectList(db.DanhMuc, "MaDM", "TenDanhMuc", sp.MaDM);
                // Sửa tham số cuối cùng thành sp.MaNCC
                ViewBag.MANCC = new SelectList(db.NhaCungCap, "MANCC", "TenCongTy", sp.MaNCC);
                return View(sp);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    sp.HetHang = false;
                    sp.ChietKhau = 0;
                    sp.QuyCach = 1;
                    sp.DVT_QC = sp.DVT;
                    sp.MaVach = "00000000";
                    sp.MaSoRiengTA = "N/A";
                    sp.TenBietDuoc = sp.TenSP;
                    sp.GhiChu = "Tạo tự động";

                    db.SanPham.Add(sp);
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi lưu Database: " + ex.Message);
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError("", "Chi tiết lỗi: " + ex.InnerException.Message);
                    }
                }
            }

            ViewBag.MaDM = new SelectList(db.DanhMuc, "MaDM", "TenDanhMuc", sp.MaDM);
            // Sửa tham số cuối cùng thành sp.MaNCC
            ViewBag.MANCC = new SelectList(db.NhaCungCap, "MANCC", "TenCongTy", sp.MaNCC);
            return View(sp);
        }

        // ==========================================
        // 3. SỬA SẢN PHẨM (GET) - Hiển thị dữ liệu cũ lên Form
        // ==========================================
        [HttpGet]
        public ActionResult Edit(string id) // Khóa chính MaSP của bạn là chuỗi nvarchar nên dùng string id
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin")
            {
                return Content("Bạn không có quyền truy cập chức năng này!");
            }

            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            SanPham sp = db.SanPham.Find(id);
            if (sp == null)
            {
                return HttpNotFound();
            }

            // Đổ danh mục và NCC vào dropdownlist, chọn sẵn giá trị hiện tại của sản phẩm
            ViewBag.MaDM = new SelectList(db.DanhMuc, "MaDM", "TenDanhMuc", sp.MaDM);
            ViewBag.MaNCC = new SelectList(db.NhaCungCap, "MANCC", "TenCongTy", sp.MaNCC);
            return View(sp);
        }

        // ==========================================
        // 3. SỬA SẢN PHẨM (POST) - Lưu dữ liệu mới xuống SQL
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(SanPham sp)
        {
            ModelState.Clear(); // Bỏ qua validate tự động để tránh lỗi entities ẩn

            if (ModelState.IsValid)
            {
                var updateSP = db.SanPham.Find(sp.MaSP);
                if (updateSP != null)
                {
                    // Chỉ cập nhật những cột xuất hiện trên form sửa
                    updateSP.TenSP = sp.TenSP;
                    updateSP.MaDM = sp.MaDM;
                    updateSP.MaNCC = sp.MaNCC;
                    updateSP.GiaMua = sp.GiaMua;
                    updateSP.GiaBan = sp.GiaBan;
                    updateSP.SoLuongTrongKho = sp.SoLuongTrongKho;
                    updateSP.DVT = sp.DVT;
                    updateSP.HetHang = sp.HetHang;
                    updateSP.GhiChu = sp.GhiChu;

                    // Giữ nguyên các cột kỹ thuật ẩn để tránh sập Validation
                    updateSP.TenBietDuoc = sp.TenSP;
                    updateSP.DVT_QC = sp.DVT;

                    db.SaveChanges(); // Lưu mượt mà
                    return RedirectToAction("Index");
                }
            }

            ViewBag.MaDM = new SelectList(db.DanhMuc, "MaDM", "TenDanhMuc", sp.MaDM);
            ViewBag.MaNCC = new SelectList(db.NhaCungCap, "MaNCC", "TenNCC", sp.MaNCC);
            return View(sp);
        }

        // ==========================================
        // 4. XÓA SẢN PHẨM (Chạy trực tiếp khi click xác nhận)
        // ==========================================
        public ActionResult Delete(string id)
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin")
            {
                return Content("Bạn không có quyền thực hiện!");
            }

            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            SanPham sp = db.SanPham.Find(id);
            if (sp == null)
            {
                return HttpNotFound();
            }

            try
            {
                db.SanPham.Remove(sp);
                db.SaveChanges(); // Xóa thật khỏi SQL Server
            }
            catch (Exception)
            {
                // Phòng trường hợp món này đã có trong hóa đơn cũ (dính khóa ngoại không cho xóa),
                // thì mình tự động chuyển trạng thái thành "Hết hàng" để ẩn đi, tránh sập app.
                sp.HetHang = true;
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }
        // ==========================================
        // THÊM NHANH DANH MỤC (POST) - Chạy bằng Ajax hoặc gọi trực tiếp
        // ==========================================
        [HttpPost]
        public ActionResult ThemNhanhDanhMuc(int MaDM, string TenDanhMuc) // Đổi string thành int ở đây
        {
            // Ép về kiểu số nguyên để tìm kiếm đúng với kiểu dữ liệu trong SQL
            var check = db.DanhMuc.Find(MaDM);

            if (check == null && !string.IsNullOrEmpty(TenDanhMuc))
            {
                DanhMuc dm = new DanhMuc();
                dm.MaDM = MaDM; // Bây giờ MaDM đã là số nguyên, gán vào cột số nguyên sẽ không bị lỗi nữa
                dm.TenDanhMuc = TenDanhMuc;

                db.DanhMuc.Add(dm);
                db.SaveChanges();

                return Content("Thành công");
            }
            return Content("Thất bại hoặc trùng mã");
        }
    }
}