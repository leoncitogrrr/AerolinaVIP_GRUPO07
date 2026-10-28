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
    public class MarcaTarjetasController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: MarcaTarjetas
        public ActionResult Index()
        {
            return View(db.MarcaTarjeta.ToList());
        }

        // GET: MarcaTarjetas/Details/VISA
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MarcaTarjeta marcaTarjeta = db.MarcaTarjeta.Find(id);
            if (marcaTarjeta == null)
            {
                return HttpNotFound();
            }
            return View(marcaTarjeta);
        }

        // GET: MarcaTarjetas/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: MarcaTarjetas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Codigo,Nombre,LongitudNumero,LongitudCodigoSeguridad,PrefijosReferencia,RutaLogo,Activa")] MarcaTarjeta marcaTarjeta)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.MarcaTarjeta.Add(marcaTarjeta);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Reglas de la base de datos (índices únicos, CHECK y triggers)
                    ModelState.AddModelError("", "No se pudo guardar: " + ServicioAerolinea.MensajeError(ex));
                }
            }

            return View(marcaTarjeta);
        }

        // GET: MarcaTarjetas/Edit/VISA
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MarcaTarjeta marcaTarjeta = db.MarcaTarjeta.Find(id);
            if (marcaTarjeta == null)
            {
                return HttpNotFound();
            }
            return View(marcaTarjeta);
        }

        // POST: MarcaTarjetas/Edit/VISA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Codigo,Nombre,LongitudNumero,LongitudCodigoSeguridad,PrefijosReferencia,RutaLogo,Activa")] MarcaTarjeta marcaTarjeta)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(marcaTarjeta).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Reglas de la base de datos (índices únicos, CHECK y triggers)
                    ModelState.AddModelError("", "No se pudo guardar: " + ServicioAerolinea.MensajeError(ex));
                }
            }
            return View(marcaTarjeta);
        }

        // GET: MarcaTarjetas/Delete/VISA
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MarcaTarjeta marcaTarjeta = db.MarcaTarjeta.Find(id);
            if (marcaTarjeta == null)
            {
                return HttpNotFound();
            }
            return View(marcaTarjeta);
        }

        // POST: MarcaTarjetas/Delete/VISA
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            MarcaTarjeta marcaTarjeta = db.MarcaTarjeta.Find(id);
            try
            {
                db.MarcaTarjeta.Remove(marcaTarjeta);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Integridad referencial: la marca ya se usó en pagos simulados
                ModelState.AddModelError("", "No se puede eliminar la marca porque tiene pagos registrados. Puede desactivarla (Activa = No). " + ex.Message);
                return View(marcaTarjeta);
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