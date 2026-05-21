using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

public class AccountController : Controller
{
    SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["con"].ConnectionString);

    public ActionResult Login()
    {
        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }
        return View();
    }

    [HttpPost]
    public ActionResult Login(string username, string password)
    {
        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }
        con.Open();
        SqlCommand cmd = new SqlCommand("select * from Users where Username=@u and Password=@p", con);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@p", password);

        var dr = cmd.ExecuteReader();

        if (dr.Read())
        {
            Session["UserId"] = dr["Id"];
            return RedirectToAction("Dashboard", "Home");
        }
        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }
        ViewBag.Error = "Invalid Login";
        return View();
    }

    public ActionResult Logout()
    {
        if (con.State == ConnectionState.Open)
        {
            con.Close();
        }
        Session.Clear();
        return RedirectToAction("Login");
    }
}