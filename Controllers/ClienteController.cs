using Microsoft.AspNetCore.Mvc;

namespace Sportia.Controllers
{
    public class ClienteController : Controller
    {
        public IActionResult Index()
        {
            // Verificar que haya iniciado sesión
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            int? idRol = HttpContext.Session.GetInt32("IdRol");

            // Si no ha iniciado sesión
            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Si no es cliente
            if (idRol != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
    }
}