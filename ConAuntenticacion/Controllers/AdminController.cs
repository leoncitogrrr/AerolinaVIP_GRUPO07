using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ConAuntenticacion.Models;

namespace ConAuntenticacion.Controllers
{
    // Pantallas 17 a 21 (área administrativa). El dashboard NO se desarrolla en esta entrega:
    // la pantalla 17 es solo una página de accesos a los módulos.
    [Authorize]
    public class AdminController : Controller
    {
        private ServicioAerolinea servicio = new ServicioAerolinea();

        // ================= PANTALLA 17: PANEL DE ACCESOS =================
        [Authorize(Roles = RolesSistema.Administracion)]
        public ActionResult Index()
        {
            return View();
        }

        // ================= PANTALLA 18: VUELOS E ITINERARIOS =================
        [Authorize(Roles = RolesSistema.Administracion)]
        public ActionResult Vuelos(AdminVuelosViewModel modelo)
        {
            if (!modelo.Desde.HasValue)
            {
                modelo.Desde = DateTime.Today;
            }
            if (!modelo.Hasta.HasValue || modelo.Hasta < modelo.Desde)
            {
                modelo.Hasta = modelo.Desde.Value.AddDays(14);
            }
            modelo.Vuelos = servicio.ListarVuelosAdmin(modelo.Desde.Value, modelo.Hasta.Value);
            modelo.Aeronaves = servicio.ListarAeronaves();
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.Administracion)]
        public ActionResult AsignarAeronave(AsignarAeronaveViewModel modelo, DateTime? desde, DateTime? hasta)
        {
            if (ModelState.IsValid)
            {
                string error = servicio.AsignarAeronave(modelo.VueloId, modelo.AeronaveId);
                if (error == null)
                {
                    TempData["Mensaje"] = "Aeronave actualizada en el vuelo.";
                }
                else
                {
                    TempData["Error"] = error;
                }
            }
            return RedirectToAction("Vuelos", new { Desde = Fecha(desde), Hasta = Fecha(hasta) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.Administracion)]
        public ActionResult CambiarEstadoVuelo(CambiarEstadoVueloViewModel modelo, DateTime? desde, DateTime? hasta)
        {
            if (ModelState.IsValid)
            {
                string error = servicio.CambiarEstadoVuelo(modelo.VueloId, modelo.EstadoVueloCodigo);
                if (error == null)
                {
                    TempData["Mensaje"] = "Estado del vuelo actualizado a " + modelo.EstadoVueloCodigo + ".";
                }
                else
                {
                    TempData["Error"] = error;
                }
            }
            else
            {
                TempData["Error"] = Errores();
            }
            return RedirectToAction("Vuelos", new { Desde = Fecha(desde), Hasta = Fecha(hasta) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.Administracion)]
        public ActionResult GenerarVuelos(GenerarVuelosViewModel modelo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    int nuevos = servicio.GenerarVuelos(modelo.FechaInicio.Value, modelo.FechaFin.Value);
                    TempData["Mensaje"] = "Se generaron " + nuevos + " vuelos a partir de las frecuencias activas. " +
                                          "Asígneles una aeronave para poder venderlos (RNP-01).";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ServicioAerolinea.MensajeError(ex);
                }
                return RedirectToAction("Vuelos", new { Desde = Fecha(modelo.FechaInicio), Hasta = Fecha(modelo.FechaFin) });
            }
            TempData["Error"] = Errores();
            return RedirectToAction("Vuelos");
        }

        // ================= PANTALLA 19: AERONAVES =================
        [Authorize(Roles = RolesSistema.Administracion)]
        public ActionResult Aeronaves()
        {
            List<AdminVueloItemViewModel> proximos = servicio.ListarVuelosAdmin(DateTime.Today, DateTime.Today.AddDays(30));
            List<AeronaveResumenViewModel> modelo = servicio.ListarAeronaves().Select(a => new AeronaveResumenViewModel
            {
                AeronaveId = a.AeronaveId,
                Matricula = a.Matricula,
                Modelo = a.ModeloAeronave.Fabricante + " " + a.ModeloAeronave.Nombre,
                Capacidad = a.ModeloAeronave.CapacidadAsientos,
                Estado = a.EstadoAeronaveCodigo,
                ProximosVuelos = proximos.Where(v => v.AeronaveId == a.AeronaveId && v.Estado != "CANCELADO").ToList()
            }).ToList();

            ViewBag.SinAeronave = proximos.Count(v => v.AeronaveId == null && v.Estado != "CANCELADO");
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.Administracion)]
        public ActionResult CambiarEstadoAeronave(CambiarEstadoAeronaveViewModel modelo)
        {
            if (ModelState.IsValid)
            {
                string error = servicio.CambiarEstadoAeronave(modelo.AeronaveId, modelo.EstadoAeronaveCodigo);
                if (error == null)
                {
                    TempData["Mensaje"] = "Estado de la aeronave actualizado.";
                }
                else
                {
                    TempData["Error"] = error;
                }
            }
            return RedirectToAction("Aeronaves");
        }

        // ================= PANTALLA 20: USUARIOS, PASAJEROS Y RESERVAS =================
        [Authorize(Roles = RolesSistema.Personal)]
        public ActionResult Gestion(GestionViewModel modelo)
        {
            servicio.ExpirarReservasVencidas();
            modelo.Reservas = servicio.BuscarReservas(modelo.Buscar, modelo.Estado);

            // El Counter consulta reservas; los usuarios los ve la administración
            modelo.Usuarios = User.IsInRole(RolesSistema.Administrador) || User.IsInRole(RolesSistema.SuperUsuario)
                ? ServicioUsuarios.ListarUsuarios(modelo.Buscar)
                : new List<UsuarioItemViewModel>();
            return View(modelo);
        }

        // Solo el SuperUsuario asigna roles (seguridad del registro)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.SuperUsuario)]
        public ActionResult AsignarRol(AsignarRolViewModel modelo)
        {
            if (ModelState.IsValid)
            {
                ServicioUsuarios.AsignarRol(modelo.UsuarioId, modelo.Rol);
                TempData["Mensaje"] = "Rol actualizado a " + modelo.Rol + ".";
            }
            else
            {
                TempData["Error"] = Errores();
            }
            return RedirectToAction("Gestion");
        }

        // ================= PANTALLA 21: CONSULTAS Y REPORTES =================
        [Authorize(Roles = RolesSistema.Administracion)]
        public ActionResult Reportes(ReportesViewModel modelo)
        {
            if (!modelo.Desde.HasValue && !modelo.Hasta.HasValue)
            {
                ModelState.Clear();
                modelo.Desde = DateTime.Today.AddDays(-30);
                modelo.Hasta = DateTime.Today.AddDays(30);
            }

            if (ModelState.IsValid)
            {
                servicio.CargarReportes(modelo);
            }
            else
            {
                modelo.Ocupacion = new List<DisponibilidadVueloDto>();
                modelo.VentasPorRuta = new List<VentaRutaViewModel>();
                modelo.ReservasPorEstado = new List<ConteoEstadoViewModel>();
                modelo.PagosPorEstado = new List<ConteoEstadoViewModel>();
            }
            return View(modelo);
        }

        // Fecha en formato ISO para la URL (independiente de la cultura del servidor)
        private static string Fecha(DateTime? fecha)
        {
            return fecha.HasValue ? fecha.Value.ToString("yyyy-MM-dd") : null;
        }

        private string Errores()
        {
            return string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
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
