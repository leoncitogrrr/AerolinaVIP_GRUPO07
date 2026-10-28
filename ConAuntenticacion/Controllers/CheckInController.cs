using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using ConAuntenticacion.Models;

namespace ConAuntenticacion.Controllers
{
    // Pantalla 15: check-in en línea (Cliente titular o Counter en nombre del cliente)
    [Authorize(Roles = RolesSistema.CompraPasajes)]
    public class CheckInController : Controller
    {
        private ServicioAerolinea servicio = new ServicioAerolinea();

        private bool EsPersonal()
        {
            return User.IsInRole(RolesSistema.Counter) || User.IsInRole(RolesSistema.Administrador) || User.IsInRole(RolesSistema.SuperUsuario);
        }

        // GET: /CheckIn/Index?CodigoReserva=RV00000006
        public ActionResult Index(CheckInIndexViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                modelo.Elegibles = new List<CheckInElegibleViewModel>();
                return View(modelo);
            }
            modelo.Elegibles = servicio.ListarCheckIn(User.Identity.GetUserId(), EsPersonal(), modelo.CodigoReserva);
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registrar(RegistrarCheckInViewModel modelo, string codigoReserva)
        {
            // Las reglas (ventana DP-12, asiento confirmado, titularidad) están en RegistrarCheckInViewModel
            if (ModelState.IsValid)
            {
                try
                {
                    int checkInId = servicio.RegistrarCheckIn(modelo.AsignacionId);
                    TempData["Mensaje"] = "Check-in realizado correctamente.";
                    return RedirectToAction("Pase", new { id = checkInId });
                }
                catch (ReglaNegocioException ex)
                {
                    TempData["Error"] = ex.Message;
                }
            }
            else
            {
                TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }
            return RedirectToAction("Index", new { CodigoReserva = codigoReserva });
        }

        // Pase de abordar académico
        public ActionResult Pase(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            string usuarioReserva = servicio.UsuarioDeCheckIn(id.Value);
            if (usuarioReserva == null)
            {
                return HttpNotFound();
            }
            if (!EsPersonal() && usuarioReserva != User.Identity.GetUserId())
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "El check-in no pertenece a su cuenta.");
            }
            CheckInElegibleViewModel pase = servicio.ObtenerPaseAbordar(id.Value);
            if (pase == null)
            {
                return HttpNotFound();
            }
            return View(pase);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                servicio.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
