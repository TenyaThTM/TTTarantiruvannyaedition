using Microsoft.AspNetCore.Mvc;

namespace TicTacToeGame.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}