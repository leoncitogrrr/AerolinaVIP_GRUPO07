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
    // PERMISOS: catálogo de configuración -> solo el SuperUsuario
    [Authorize(Roles = "SuperUsuario")]
    public class TiposDocumentoController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: TiposDocumento
        public ActionResult Index()
        {
            return View(db.TipoDocumentoIdentidad.ToList());
        }

        // GET: TiposDocumento/Details/CEDULA
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TipoDocumentoIdentidad tipoDocumentoIdentidad = db.TipoDocumentoIdentidad.Find(id);
            if (tipoDocumentoIdentidad == null)
            {
                return HttpNotFound();
            }
            return View(tipoDocumentoIdentidad);
        }

        // GET: TiposDocumento/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TiposDocumento/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Codigo,Descripcion")] TipoDocumentoIdentidad tipoDocumentoIdentidad)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.TipoDocumentoIdentidad.Add(tipoDocumentoIdentidad);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Reglas de la base de datos (índices únicos, CHECK y triggers)
                    ModelState.AddModelError("", "No se pudo guardar: " + ServicioAerolinea.MensajeError(ex));
                }
            }

            return View(tipoDocumentoIdentidad);
        }

        // GET: TiposDocumento/Edit/CEDULA
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TipoDocumentoIdentidad tipoDocumentoIdentidad = db.TipoDocumentoIdentidad.Find(id);
            if (tipoDocumentoIdentidad == null)
            {
                return HttpNotFound();
            }
            return View(tipoDocumentoIdentidad);
        }

        // POST: TiposDocumento/Edit/CEDULA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Codigo,Descripcion")] TipoDocumentoIdentidad tipoDocumentoIdentidad)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(tipoDocumentoIdentidad).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Reglas de la base de datos (índices únicos, CHECK y triggers)
                    ModelState.AddModelError("", "No se pudo guardar: " + ServicioAerolinea.MensajeError(ex));
                }
            }
            return View(tipoDocumentoIdentidad);
        }

        // GET: TiposDocumento/Delete/CEDULA
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TipoDocumentoIdentidad tipoDocumentoIdentidad = db.TipoDocumentoIdentidad.Find(id);
            if (tipoDocumentoIdentidad == null)
            {
                return HttpNotFound();
            }
            return View(tipoDocumentoIdentidad);
        }

        // POST: TiposDocumento/Delete/CEDULA
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            TipoDocumentoIdentidad tipoDocumentoIdentidad = db.TipoDocumentoIdentidad.Find(id);
            try
            {
                db.TipoDocumentoIdentidad.Remove(tipoDocumentoIdentidad);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Integridad referencial: hay pasajeros registrados con este tipo de documento
                ModelState.AddModelError("", "No se puede eliminar el tipo de documento porque tiene pasajeros registrados. " + ex.Message);
                return View(tipoDocumentoIdentidad);
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