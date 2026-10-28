using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ConAuntenticacion.Models;

namespace ConAuntenticacion.Controllers
{
    public class HomeController : Controller
    {
        private ServicioAerolinea servicio = new ServicioAerolinea();
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // Pantalla 1: página de inicio (público)
        public ActionResult Index()
        {
            HomeViewModel modelo = new HomeViewModel();
            modelo.Origen = "UIO";            // valor inicial: Quito
            modelo.Pasajeros = 1;
            modelo.Aeropuertos = db.Aeropuerto
                .Where(a => a.Activo)
                .OrderBy(a => a.Localidad)
                .ToList()
                .Select(a => new SelectListItem { Value = a.CodigoIATA, Text = a.Localidad + " (" + a.CodigoIATA + ")" })
                .ToList();

            // Sección «Destinos» y destinos iniciales del selector (desde Quito)
            modelo.Destinos = servicio.DestinosDesde(modelo.Origen);
            modelo.DestinosOrigen = modelo.Destinos
                .Select(d => new SelectListItem { Value = d.Codigo, Text = d.Ciudad + (d.Escala != null ? " (escala en " + d.Escala + ")" : "") })
                .ToList();
            return View(modelo);
        }

        // GET: /Home/Destinos?origen=UIO  -> JSON para filtrar el destino con jQuery
        public JsonResult Destinos(string origen)
        {
            var lista = servicio.DestinosDesde(origen ?? "")
                .Select(d => new { codigo = d.Codigo, texto = d.Ciudad + (d.Escala != null ? " (escala en " + d.Escala + ")" : "") })
                .ToList();
            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
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