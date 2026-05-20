using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using quanlycoffe.Models;

namespace quanlycoffe.Controllers
{
    public class HeThongController : Controller
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
        // 1. DANH SÁCH NHÂN VIÊN
        // ==========================================
        public ActionResult Index()
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            // Lấy danh sách nhân viên chuẩn
            var danhSachNV = db.NhanVien.Include(n => n.plp_NghiepVu).OrderBy(x => x.MaNV).ToList();

            // Lấy danh sách tài khoản để lấy Email thật từ SQL
            var danhSachTaiKhoan = db.plp_LoginUser.Where(x => x.MaNV != null).ToList();

            // Tạo một Dictionary để tra cứu nhanh: Khóa là MaNV, Giá trị là Email
            var danhSachEmail = new Dictionary<int, string>();
            foreach (var tk in danhSachTaiKhoan)
            {
                if (tk.MaNV.HasValue && !danhSachEmail.ContainsKey(tk.MaNV.Value))
                {
                    danhSachEmail.Add(tk.MaNV.Value, tk.Email ?? "Chưa cập nhật");
                }
            }

            // Truyền danh sách Email này ra ngoài giao diện thông qua ViewBag
            ViewBag.DanhSachEmail = danhSachEmail;

            return View(danhSachNV);
        }

        // ==========================================
        // 2. THÊM NHÂN VIÊN
        // ==========================================
        [HttpGet]
        public ActionResult Create()
        {
            if (!IsAdmin()) return Content("Bạn không có quyền truy cập chức năng này!");
            ViewBag.MaNghiepVu = new SelectList(db.plp_NghiepVu, "MaNghiepVu", "TenNghiepVu");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(NhanVien nv, string EmailInput)
        {
            // Xóa sạch toàn bộ lỗi Validation kiểm tra của các trường trên Form
            ModelState.Clear();

            var checkTrung = db.NhanVien.Find(nv.MaNV);
            if (checkTrung != null)
            {
                ModelState.AddModelError("", "Mã nhân viên này đã tồn tại! Vui lòng nhập mã khác.");
                ViewBag.MaNghiepVu = new SelectList(db.plp_NghiepVu, "MaNghiepVu", "TenNghiepVu", nv.MaNghiepVu);
                return View(nv);
            }

            if (ModelState.IsValid)
            {
                // 1. Thêm nhân viên
                db.NhanVien.Add(nv);

                // --- BƯỚC BỌC THÉP TỰ ĐỘNG TẠO ID MỚI TRÁNH TRÙNG LẶP ---
                // Lấy UserID lớn nhất hiện tại, nếu chưa có ai thì lấy số 0
                int maxUserID = db.plp_LoginUser.Any() ? db.plp_LoginUser.Max(t => t.UserID) : 0;

                // 2. Tạo tài khoản tự động (Bơm đầy đủ các trường NOT NULL)
                var taiKhoanMoi = new plp_LoginUser();
                taiKhoanMoi.UserID = maxUserID + 1; // TỰ ĐỘNG CỘNG 1 CHO ID MỚI
                taiKhoanMoi.Login_Name = "nv" + nv.MaNV;
                taiKhoanMoi.PW_matkhau = "123456";
                taiKhoanMoi.Quyen = "User";
                taiKhoanMoi.MaNV = nv.MaNV;
                taiKhoanMoi.Email = string.IsNullOrEmpty(EmailInput) ? "chua_co@gmail.com" : EmailInput;
                taiKhoanMoi.TrangThai = "Hoạt động";

                db.plp_LoginUser.Add(taiKhoanMoi);

                // 3. Lưu tổng thể xuống SQL
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.MaNghiepVu = new SelectList(db.plp_NghiepVu, "MaNghiepVu", "TenNghiepVu", nv.MaNghiepVu);
            return View(nv);
        }

        // ==========================================
        // 3. SỬA THÔNG TIN NHÂN VIÊN
        // ==========================================
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return Content("Bạn không có quyền!");
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var nv = db.NhanVien.Find(id);
            if (nv == null) return HttpNotFound();

            // Lấy Email từ bảng tài khoản liên quan lên để hiển thị vào ô sửa
            var taikhoan = db.plp_LoginUser.FirstOrDefault(x => x.MaNV == id);
            ViewBag.EmailThat = taikhoan != null ? taikhoan.Email : "";

            ViewBag.MaNghiepVu = new SelectList(db.plp_NghiepVu, "MaNghiepVu", "TenNghiepVu", nv.MaNghiepVu);
            return View(nv);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(NhanVien nv, string EmailInput)
        {
            ModelState.Clear();

            if (ModelState.IsValid)
            {
                var updateNV = db.NhanVien.Find(nv.MaNV);
                if (updateNV != null)
                {
                    // Cập nhật nhân viên
                    updateNV.TenNV = nv.TenNV;
                    updateNV.GioiTinh = nv.GioiTinh;
                    updateNV.Ngaysinh = nv.Ngaysinh;
                    updateNV.DienThoai = nv.DienThoai;
                    updateNV.DiaChi = nv.DiaChi;
                    updateNV.LuongNV = nv.LuongNV;
                    updateNV.TrangThai = nv.TrangThai;
                    updateNV.MaNghiepVu = nv.MaNghiepVu;

                    // Cập nhật hoặc tự tạo tài khoản liên kết
                    var taikhoan = db.plp_LoginUser.FirstOrDefault(x => x.MaNV == nv.MaNV);
                    if (taikhoan != null)
                    {
                        taikhoan.Email = EmailInput;
                    }
                    else
                    {
                        // --- ÁP DỤNG LUÔN CHIÊU NÀY KHI SỬA MÀ PHẢI TẠO TÀI KHOẢN MỚI ---
                        int maxUserID = db.plp_LoginUser.Any() ? db.plp_LoginUser.Max(t => t.UserID) : 0;

                        var taiKhoanMoi = new plp_LoginUser();
                        taiKhoanMoi.UserID = maxUserID + 1; // TỰ ĐỘNG CỘNG 1 CHO ID MỚI
                        taiKhoanMoi.Login_Name = "nv" + nv.MaNV;
                        taiKhoanMoi.PW_matkhau = "123456";
                        taiKhoanMoi.Quyen = "User";
                        taiKhoanMoi.MaNV = nv.MaNV;
                        taiKhoanMoi.Email = EmailInput;
                        taiKhoanMoi.TrangThai = "Hoạt động";
                        db.plp_LoginUser.Add(taiKhoanMoi);
                    }

                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            ViewBag.MaNghiepVu = new SelectList(db.plp_NghiepVu, "MaNghiepVu", "TenNghiepVu", nv.MaNghiepVu);
            return View(nv);
        }

        // ==========================================
        // 4. XÓA NHÂN VIÊN
        // ==========================================
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return Content("Bạn không có quyền!");

            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var nv = db.NhanVien.Find(id);
            if (nv == null) return HttpNotFound();

            db.NhanVien.Remove(nv);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // ==========================================
        // 5. QUẢN LÝ TÀI KHOẢN HỆ THỐNG
        // ==========================================
        public ActionResult DSTaiKhoan()
        {
            // Chỉ cho phép Admin xem danh sách tài khoản
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin")
            {
                return Content("Bạn không có quyền truy cập!");
            }

            // Lấy toàn bộ tài khoản và nạp kèm thông tin Nhân viên để biết tài khoản đó của ai
            var danhSachTK = db.plp_LoginUser.Include(t => t.NhanVien).OrderBy(x => x.Login_Name).ToList();
            return View(danhSachTK);
        }
    }
}