using System.Web.Mvc;

public class HomeController : Controller
{
    public ActionResult Dashboard()
    {
        if (Session["UserId"] == null)
            return RedirectToAction("Login", "Account");

        return View();
    }
}