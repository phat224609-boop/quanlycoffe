using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using quanlycoffe.Models; // Đảm bảo đúng namespace dự án của Phát

namespace quanlycoffe.Controllers
{
    public class NhaCungCapController : Controller
    {
        private BMSModel db = new BMSModel();

        // ==========================================
        // 1. DANH SÁCH ĐỐI TÁC (INDEX)
        // ==========================================
        public ActionResult Index()
        {
            var list = db.NhaCungCap.ToList();
            return View(list);
        }

        // ==========================================
        // 2. THÊM ĐỐI TÁC (GET & POST)
        // ==========================================
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(NhaCungCap ncc)
        {
            ModelState.Clear();

            // Tự động tìm mã số lớn nhất rồi cộng 1 để người dùng không cần nhập MANCC thủ công
            int maxMa = db.NhaCungCap.Any() ? db.NhaCungCap.Max(n => n.MANCC) : 0;
            ncc.MANCC = maxMa + 1;

            if (ModelState.IsValid)
            {
                try
                {
                    db.NhaCungCap.Add(ncc);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi lưu SQL: " + ex.Message);
                }
            }
            return View(ncc);
        }

        // ==========================================
        // 3. SỬA ĐỐI TÁC (GET & POST)
        // ==========================================
        [HttpGet]
        public ActionResult Edit(int id) // id kiểu int khớp với MANCC
        {
            NhaCungCap ncc = db.NhaCungCap.Find(id);
            if (ncc == null)
            {
                return HttpNotFound();
            }
            return View(ncc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(NhaCungCap ncc)
        {
            ModelState.Clear();
            if (ModelState.IsValid)
            {
                var updateNCC = db.NhaCungCap.Find(ncc.MANCC);
                if (updateNCC != null)
                {
                    updateNCC.TenCongTy = ncc.TenCongTy;
                    updateNCC.NguoiLienHe = ncc.NguoiLienHe;
                    updateNCC.DiaChi = ncc.DiaChi;
                    updateNCC.DienThoai = ncc.DienThoai;
                    updateNCC.Fax = ncc.Fax;
                    updateNCC.Email = ncc.Email;

                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            return View(ncc);
        }

        // ==========================================
        // 4. XÓA ĐỐI TÁC
        // ==========================================
        public ActionResult Delete(int id)
        {
            NhaCungCap ncc = db.NhaCungCap.Find(id);
            if (ncc != null)
            {
                try
                {
                    db.NhaCungCap.Remove(ncc);
                    db.SaveChanges();
                }
                catch (Exception)
                {
                    // Nếu nhà cung cấp đã dính vào sản phẩm trong kho, chặn lỗi sập app bằng thông báo ẩn
                    TempData["Error"] = "Không thể xóa đối tác này vì đang có sản phẩm thuộc nhà cung cấp này!";
                }
            }
            return RedirectToAction("Index");
        }
    }
}