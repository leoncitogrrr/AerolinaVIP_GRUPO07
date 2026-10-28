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
    // PERMISOS: el Administrador y el SuperUsuario administran las rutas.
    // Solo el SuperUsuario puede eliminar.
    [Authorize(Roles = "SuperUsuario,Administrador")]
    public class RutasController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Rutas
        public ActionResult Index()
        {
            var ruta = db.Ruta.Include(r => r.Aeropuerto).Include(r => r.Aeropuerto1);
            return View(ruta.ToList());
        }

        // GET: Rutas/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta ruta = db.Ruta.Find(id);
            if (ruta == null)
            {
                return HttpNotFound();
            }
            return View(ruta);
        }

        // GET: Rutas/Create
        public ActionResult Create()
        {
            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuerto, "AeropuertoId", "CodigoIATA");
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuerto, "AeropuertoId", "CodigoIATA");
            return View();
        }

        // POST: Rutas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RutaId,AeropuertoOrigenId,AeropuertoDestinoId,TarifaReferencial,Activa")] Ruta ruta)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Ruta.Add(ruta);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Reglas de la base de datos (índices únicos, CHECK y triggers)
                    ModelState.AddModelError("", "No se pudo guardar: " + ServicioAerolinea.MensajeError(ex));
                }
            }

            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuerto, "AeropuertoId", "CodigoIATA", ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuerto, "AeropuertoId", "CodigoIATA", ruta.AeropuertoOrigenId);
            return View(ruta);
        }

        // GET: Rutas/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta ruta = db.Ruta.Find(id);
            if (ruta == null)
            {
                return HttpNotFound();
            }
            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuerto, "AeropuertoId", "CodigoIATA", ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuerto, "AeropuertoId", "CodigoIATA", ruta.AeropuertoOrigenId);
            return View(ruta);
        }

        // POST: Rutas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RutaId,AeropuertoOrigenId,AeropuertoDestinoId,TarifaReferencial,Activa")] Ruta ruta)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(ruta).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Reglas de la base de datos (índices únicos, CHECK y triggers)
                    ModelState.AddModelError("", "No se pudo guardar: " + ServicioAerolinea.MensajeError(ex));
                }
            }
            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuerto, "AeropuertoId", "CodigoIATA", ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuerto, "AeropuertoId", "CodigoIATA", ruta.AeropuertoOrigenId);
            return View(ruta);
        }

        // GET: Rutas/Delete/5
        [Authorize(Roles = "SuperUsuario")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta ruta = db.Ruta.Find(id);
            if (ruta == null)
            {
                return HttpNotFound();
            }
            return View(ruta);
        }

        // POST: Rutas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario")]
        public ActionResult DeleteConfirmed(int id)
        {
            Ruta ruta = db.Ruta.Find(id);
            try
            {
                db.Ruta.Remove(ruta);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Integridad referencial: la ruta tiene frecuencias registradas
                ModelState.AddModelError("", "No se puede eliminar la ruta porque tiene frecuencias asociadas. " + ex.Message);
                return View(ruta);
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