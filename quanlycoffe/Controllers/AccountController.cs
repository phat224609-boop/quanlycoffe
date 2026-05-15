using quanlycoffe.Models;
using System.Web.Mvc;
using System.Linq;
public class AccountController : Controller
{
    BMSModel db = new BMSModel();

 
    [HttpGet]
    public ActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public ActionResult Login(string user, string pass)
    {
        var account = db.plp_LoginUser.FirstOrDefault(x => x.Login_Name == user && x.PW_matkhau == pass);
        if (account != null)
        {
            Session["UserID"] = account.UserID;
            Session["LoginName"] = account.Login_Name; // Lưu tên để hiển thị lên Header
            Session["Quyen"] = account.Quyen;
            return RedirectToAction("Index", "Home");
        }

        ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng!";
        return View();
    }

    // Hàm Đăng xuất
    public ActionResult Logout()
    {
        Session.Clear(); // Xóa sạch Session
        return RedirectToAction("Index", "Home");
    }
}