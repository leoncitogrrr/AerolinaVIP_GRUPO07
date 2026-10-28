using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using ConAuntenticacion.Models;

namespace ConAuntenticacion.Controllers
{
    // Pantallas 11 (pago simulado con modal) y 12 (confirmación)
    [Authorize]
    public class ComprasController : Controller
    {
        private ServicioAerolinea servicio = new ServicioAerolinea();
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        private bool EsPersonal()
        {
            return User.IsInRole(RolesSistema.Counter) || User.IsInRole(RolesSistema.Administrador) || User.IsInRole(RolesSistema.SuperUsuario);
        }

        // ================= PANTALLA 11: PAGO SIMULADO =================
        [Authorize(Roles = RolesSistema.CompraPasajes)]
        public ActionResult Pagar(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (!servicio.PuedeAcceder(id.Value, User.Identity.GetUserId(), EsPersonal()))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "La reserva no pertenece a su cuenta.");
            }

            servicio.ExpirarReservasVencidas();
            ReservaResumenViewModel reserva = servicio.ObtenerResumen(id.Value);
            if (!reserva.PuedePagar)
            {
                TempData["Error"] = "La reserva " + reserva.CodigoReserva + " no está pendiente de pago (estado: " + reserva.EstadoReserva + ").";
                return RedirectToAction("Confirmacion", new { id = id.Value });
            }

            PagoViewModel modelo = new PagoViewModel();
            modelo.ReservaId = id.Value;
            modelo.MetodoPagoCodigo = "TARJETA_CREDITO";
            modelo.Reserva = reserva;
            CargarCombos();
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.CompraPasajes)]
        public ActionResult Pagar(PagoViewModel modelo)
        {
            if (!servicio.PuedeAcceder(modelo.ReservaId, User.Identity.GetUserId(), EsPersonal()))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "La reserva no pertenece a su cuenta.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    ResultadoPagoViewModel resultado = servicio.ProcesarPago(modelo);
                    if (resultado.Aprobado)
                    {
                        TempData["Mensaje"] = resultado.Mensaje;
                    }
                    else
                    {
                        TempData["Error"] = resultado.Mensaje;
                    }
                    return RedirectToAction("Confirmacion", new { id = modelo.ReservaId });
                }
                catch (ReglaNegocioException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            // El número y el CVV no se devuelven a la vista
            modelo.NumeroTarjeta = null;
            modelo.CodigoSeguridad = null;
            modelo.Reserva = servicio.ObtenerResumen(modelo.ReservaId);
            CargarCombos();
            return View(modelo);
        }

        // ================= PANTALLA 12: CONFIRMACIÓN =================
        public ActionResult Confirmacion(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (!servicio.PuedeAcceder(id.Value, User.Identity.GetUserId(), EsPersonal()))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "La reserva no pertenece a su cuenta.");
            }
            return View(servicio.ObtenerResumen(id.Value));
        }

        private void CargarCombos()
        {
            ViewBag.MetodosPago = db.MetodoPago.Where(m => m.Activo).OrderBy(m => m.Nombre).ToList()
                .Select(m => new SelectListItem { Value = m.Codigo, Text = m.Nombre }).ToList();
            ViewBag.Marcas = db.MarcaTarjeta.Where(m => m.Activa).OrderBy(m => m.Nombre).ToList();
            ViewBag.Anios = Enumerable.Range(DateTime.Today.Year, 12)
                .Select(a => new SelectListItem { Value = a.ToString(), Text = a.ToString() }).ToList();
            ViewBag.Meses = Enumerable.Range(1, 12)
                .Select(m => new SelectListItem { Value = m.ToString(), Text = m.ToString("00") }).ToList();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                servicio.Dispose();
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
