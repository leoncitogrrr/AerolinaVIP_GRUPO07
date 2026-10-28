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
    // PERMISOS: configuración comercial -> solo el SuperUsuario
    [Authorize(Roles = "SuperUsuario")]
    public class MonedasController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: Monedas
        public ActionResult Index()
        {
            return View(db.Moneda.ToList());
        }

        // GET: Monedas/Details/USD
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Moneda moneda = db.Moneda.Find(id);
            if (moneda == null)
            {
                return HttpNotFound();
            }
            return View(moneda);
        }

        // GET: Monedas/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Monedas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Codigo,Descripcion")] Moneda moneda)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Moneda.Add(moneda);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Reglas de la base de datos (índices únicos, CHECK y triggers)
                    ModelState.AddModelError("", "No se pudo guardar: " + ServicioAerolinea.MensajeError(ex));
                }
            }

            return View(moneda);
        }

        // GET: Monedas/Edit/USD
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Moneda moneda = db.Moneda.Find(id);
            if (moneda == null)
            {
                return HttpNotFound();
            }
            return View(moneda);
        }

        // POST: Monedas/Edit/USD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Codigo,Descripcion")] Moneda moneda)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(moneda).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Reglas de la base de datos (índices únicos, CHECK y triggers)
                    ModelState.AddModelError("", "No se pudo guardar: " + ServicioAerolinea.MensajeError(ex));
                }
            }
            return View(moneda);
        }

        // GET: Monedas/Delete/USD
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Moneda moneda = db.Moneda.Find(id);
            if (moneda == null)
            {
                return HttpNotFound();
            }
            return View(moneda);
        }

        // POST: Monedas/Delete/USD
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            Moneda moneda = db.Moneda.Find(id);
            try
            {
                db.Moneda.Remove(moneda);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Integridad referencial: la moneda (USD) está usada en las compras
                ModelState.AddModelError("", "No se puede eliminar la moneda porque tiene compras registradas. " + ex.Message);
                return View(moneda);
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