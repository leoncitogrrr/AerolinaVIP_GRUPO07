using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ConAuntenticacion.Models;

namespace ConAuntenticacion.Controllers
{
    // PERMISOS: el Administrador gestiona; solo el SuperUsuario elimina
    [Authorize(Roles = "SuperUsuario,Administrador")]
    public class TramosItinerarioController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: TramosItinerario
        public ActionResult Index()
        {
            var tramoItinerario = db.TramoItinerario.Include(t => t.Itinerario).Include(t => t.Vuelo);
            return View(tramoItinerario.ToList());
        }

        // GET: TramosItinerario/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TramoItinerario tramoItinerario = db.TramoItinerario.Find(id);
            if (tramoItinerario == null)
            {
                return HttpNotFound();
            }
            return View(tramoItinerario);
        }

        // GET: TramosItinerario/Create
        public ActionResult Create()
        {
            ViewBag.ItinerarioId = new SelectList(db.Itinerario, "ItinerarioId", "Referencia");
            ViewBag.VueloId = new SelectList(db.Vuelo, "VueloId", "EstadoVueloCodigo");
            return View();
        }

        // POST: TramosItinerario/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TramoItinerarioId,ItinerarioId,VueloId,Orden")] TramoItinerario tramoItinerario)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.TramoItinerario.Add(tramoItinerario);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Reglas de la base de datos (índices únicos, CHECK y triggers)
                    ModelState.AddModelError("", "No se pudo guardar: " + ServicioAerolinea.MensajeError(ex));
                }
            }

            ViewBag.ItinerarioId = new SelectList(db.Itinerario, "ItinerarioId", "Referencia", tramoItinerario.ItinerarioId);
            ViewBag.VueloId = new SelectList(db.Vuelo, "VueloId", "EstadoVueloCodigo", tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // GET: TramosItinerario/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TramoItinerario tramoItinerario = db.TramoItinerario.Find(id);
            if (tramoItinerario == null)
            {
                return HttpNotFound();
            }
            ViewBag.ItinerarioId = new SelectList(db.Itinerario, "ItinerarioId", "Referencia", tramoItinerario.ItinerarioId);
            ViewBag.VueloId = new SelectList(db.Vuelo, "VueloId", "EstadoVueloCodigo", tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // POST: TramosItinerario/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TramoItinerarioId,ItinerarioId,VueloId,Orden")] TramoItinerario tramoItinerario)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(tramoItinerario).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Reglas de la base de datos (índices únicos, CHECK y triggers)
                    ModelState.AddModelError("", "No se pudo guardar: " + ServicioAerolinea.MensajeError(ex));
                }
            }
            ViewBag.ItinerarioId = new SelectList(db.Itinerario, "ItinerarioId", "Referencia", tramoItinerario.ItinerarioId);
            ViewBag.VueloId = new SelectList(db.Vuelo, "VueloId", "EstadoVueloCodigo", tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // GET: TramosItinerario/Delete/5
        [Authorize(Roles = "SuperUsuario")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TramoItinerario tramoItinerario = db.TramoItinerario.Find(id);
            if (tramoItinerario == null)
            {
                return HttpNotFound();
            }
            return View(tramoItinerario);
        }

        // POST: TramosItinerario/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario")]
        public ActionResult DeleteConfirmed(int id)
        {
            TramoItinerario tramoItinerario = db.TramoItinerario.Find(id);
            try
            {
                db.TramoItinerario.Remove(tramoItinerario);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Integridad referencial: el registro está usado en otras tablas
                ModelState.AddModelError("", "No se puede eliminar porque está relacionado con otros registros. " + ServicioAerolinea.MensajeError(ex));
                return View(tramoItinerario);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
