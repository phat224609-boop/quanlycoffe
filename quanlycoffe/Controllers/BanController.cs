using System.Linq;
using System.Web.Mvc;
using quanlycoffe.Models;

namespace quanlycoffe.Controllers
{
    public class BanController : Controller
    {
        private BMSModel db = new BMSModel();

        public ActionResult SoDoBan()
        {
            // MẸO SẮP XẾP: Sắp xếp theo độ dài tên trước (để độ dài 1 ký tự lên trước),
            // rồi mới sắp xếp theo tên (để ra chuẩn thứ tự 1, 2, 3, 4, 5..., 10, 11)
            var danhSachBan = db.plp_Ban
                                .OrderBy(b => b.TenBan.Length)
                                .ThenBy(b => b.TenBan)
                                .ToList();

            return View(danhSachBan);
        }
    }
}