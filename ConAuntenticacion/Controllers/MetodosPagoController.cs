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
    // PERMISOS: configuración del pago simulado -> solo el SuperUsuario
    [Authorize(Roles = "SuperUsuario")]
    public class MetodosPagoController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: MetodosPago
        public ActionResult Index()
        {
            return View(db.MetodoPago.ToList());
        }

        // GET: MetodosPago/Details/TARJETA_CREDITO
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MetodoPago metodoPago = db.MetodoPago.Find(id);
            if (metodoPago == null)
            {
                return HttpNotFound();
            }
            return View(metodoPago);
        }

        // GET: MetodosPago/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: MetodosPago/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Codigo,Nombre,Activo")] MetodoPago metodoPago)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.MetodoPago.Add(metodoPago);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Reglas de la base de datos (índices únicos, CHECK y triggers)
                    ModelState.AddModelError("", "No se pudo guardar: " + ServicioAerolinea.MensajeError(ex));
                }
            }

            return View(metodoPago);
        }

        // GET: MetodosPago/Edit/TARJETA_CREDITO
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MetodoPago metodoPago = db.MetodoPago.Find(id);
            if (metodoPago == null)
            {
                return HttpNotFound();
            }
            return View(metodoPago);
        }

        // POST: MetodosPago/Edit/TARJETA_CREDITO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Codigo,Nombre,Activo")] MetodoPago metodoPago)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(metodoPago).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Reglas de la base de datos (índices únicos, CHECK y triggers)
                    ModelState.AddModelError("", "No se pudo guardar: " + ServicioAerolinea.MensajeError(ex));
                }
            }
            return View(metodoPago);
        }

        // GET: MetodosPago/Delete/TARJETA_CREDITO
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MetodoPago metodoPago = db.MetodoPago.Find(id);
            if (metodoPago == null)
            {
                return HttpNotFound();
            }
            return View(metodoPago);
        }

        // POST: MetodosPago/Delete/TARJETA_CREDITO
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            MetodoPago metodoPago = db.MetodoPago.Find(id);
            try
            {
                db.MetodoPago.Remove(metodoPago);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Integridad referencial: el método ya se usó en pagos simulados
                ModelState.AddModelError("", "No se puede eliminar el método de pago porque tiene pagos registrados. Puede desactivarlo (Activo = No). " + ex.Message);
                return View(metodoPago);
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