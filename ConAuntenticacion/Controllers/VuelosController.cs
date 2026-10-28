using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using ConAuntenticacion.Models;

namespace ConAuntenticacion.Controllers
{
    // Pantallas 5, 6 y 7: acceso PÚBLICO (visitante, cliente y counter)
    public class VuelosController : Controller
    {
        private ServicioAerolinea servicio = new ServicioAerolinea();
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: /Vuelos/Buscar  (Pantalla 5)
        public ActionResult Buscar()
        {
            CargarAeropuertos();
            BusquedaVuelosViewModel busqueda = new BusquedaVuelosViewModel();
            busqueda.Pasajeros = 1;
            return View(busqueda);
        }

        // GET: /Vuelos/Resultados  (Pantalla 6)
        public ActionResult Resultados(BusquedaVuelosViewModel busqueda)
        {
            // Las validaciones están en BusquedaVuelosViewModel
            if (!ModelState.IsValid)
            {
                CargarAeropuertos();
                return View("Buscar", busqueda);
            }

            servicio.ExpirarReservasVencidas();
            return View(servicio.Buscar(busqueda));
        }

        // GET: /Vuelos/Disponibilidad/5  (Pantalla 7)
        public ActionResult Disponibilidad(int? id, int pasajeros = 1)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            servicio.ExpirarReservasVencidas();
            ResultadoVueloViewModel vuelo = servicio.ObtenerVuelo(id.Value);
            if (vuelo == null)
            {
                return HttpNotFound();
            }

            DisponibilidadViewModel modelo = new DisponibilidadViewModel();
            modelo.Vuelo = vuelo;
            modelo.Pasajeros = pasajeros;
            modelo.Asientos = servicio.ObtenerAsientos(id.Value);
            return View(modelo);
        }

        private void CargarAeropuertos()
        {
            ViewBag.Aeropuertos = db.Aeropuerto
                .Where(a => a.Activo)
                .OrderBy(a => a.Localidad)
                .ToList()
                .Select(a => new SelectListItem { Value = a.CodigoIATA, Text = a.Localidad + " (" + a.CodigoIATA + ")" })
                .ToList();
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
