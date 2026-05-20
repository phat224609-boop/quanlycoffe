using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using quanlycoffe.Models;

namespace quanlycoffe.Controllers
{
    public class QuanLyBanController : Controller
    {
        // Thống nhất sử dụng BMSModel
        private BMSModel db = new BMSModel();

        // 1. DANH SÁCH BÀN
        public ActionResult Index()
        {
            var plp_Ban = db.plp_Ban.Include(p => p.plp_KhuVuc_Ban);
            return View(plp_Ban.ToList());
        }

        // 2. THÊM MỚI (GET)
        public ActionResult Create()
        {
            ViewBag.MaKhuVuc = new SelectList(db.plp_KhuVuc_Ban, "MaKhuVuc", "TenKhuVuc");
            return View();
        }

        // 2. THÊM MỚI (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(plp_Ban plp_Ban)
        {
            if (ModelState.IsValid)
            {
                db.plp_Ban.Add(plp_Ban);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.MaKhuVuc = new SelectList(db.plp_KhuVuc_Ban, "MaKhuVuc", "TenKhuVuc", plp_Ban.MaKhuVuc);
            return View(plp_Ban);
        }

        // 3. SỬA THÔNG TIN (GET)
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            plp_Ban plp_Ban = db.plp_Ban.Find(id);
            if (plp_Ban == null) return HttpNotFound();

            ViewBag.MaKhuVuc = new SelectList(db.plp_KhuVuc_Ban, "MaKhuVuc", "TenKhuVuc", plp_Ban.MaKhuVuc);
            return View(plp_Ban);
        }

        // 3. SỬA THÔNG TIN (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(plp_Ban plp_Ban)
        {
            if (ModelState.IsValid)
            {
                db.Entry(plp_Ban).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.MaKhuVuc = new SelectList(db.plp_KhuVuc_Ban, "MaKhuVuc", "TenKhuVuc", plp_Ban.MaKhuVuc);
            return View(plp_Ban);
        }

        // 4. XÓA BÀN (GET - Xác nhận)
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            plp_Ban plp_Ban = db.plp_Ban.Find(id);
            if (plp_Ban == null) return HttpNotFound();
            return View(plp_Ban);
        }

        // 4. XÓA BÀN (POST - Thực hiện xóa)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            plp_Ban plp_Ban = db.plp_Ban.Find(id);
            db.plp_Ban.Remove(plp_Ban);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}