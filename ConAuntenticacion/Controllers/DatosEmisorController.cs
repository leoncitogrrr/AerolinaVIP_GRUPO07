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
    // PERMISOS: datos de la aerolínea para el comprobante PDF -> solo el SuperUsuario
    // Regla de la BD: existe UNA sola fila (DatosEmisorId = 1)
    [Authorize(Roles = "SuperUsuario")]
    public class DatosEmisorController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: DatosEmisor
        public ActionResult Index()
        {
            return View(db.DatosEmisor.ToList());
        }

        // GET: DatosEmisor/Details/1
        public ActionResult Details(byte? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DatosEmisor datosEmisor = db.DatosEmisor.Find(id);
            if (datosEmisor == null)
            {
                return HttpNotFound();
            }
            return View(datosEmisor);
        }

        // GET: DatosEmisor/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DatosEmisor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "DatosEmisorId,NombreComercial,Identificacion,Direccion,Telefono,Correo,LeyendaComprobante")] DatosEmisor datosEmisor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.DatosEmisor.Add(datosEmisor);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // La BD solo admite una fila (DatosEmisorId = 1)
                    ModelState.AddModelError("", "Ya existen los datos del emisor; solo puede editarlos. " + ex.Message);
                }
            }

            return View(datosEmisor);
        }

        // GET: DatosEmisor/Edit/1
        public ActionResult Edit(byte? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DatosEmisor datosEmisor = db.DatosEmisor.Find(id);
            if (datosEmisor == null)
            {
                return HttpNotFound();
            }
            return View(datosEmisor);
        }

        // POST: DatosEmisor/Edit/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "DatosEmisorId,NombreComercial,Identificacion,Direccion,Telefono,Correo,LeyendaComprobante")] DatosEmisor datosEmisor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(datosEmisor).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Reglas de la base de datos (índices únicos, CHECK y triggers)
                    ModelState.AddModelError("", "No se pudo guardar: " + ServicioAerolinea.MensajeError(ex));
                }
            }
            return View(datosEmisor);
        }

        // GET: DatosEmisor/Delete/1
        public ActionResult Delete(byte? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DatosEmisor datosEmisor = db.DatosEmisor.Find(id);
            if (datosEmisor == null)
            {
                return HttpNotFound();
            }
            return View(datosEmisor);
        }

        // POST: DatosEmisor/Delete/1
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(byte id)
        {
            DatosEmisor datosEmisor = db.DatosEmisor.Find(id);
            try
            {
                db.DatosEmisor.Remove(datosEmisor);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "No se pudo eliminar los datos del emisor. " + ex.Message);
                return View(datosEmisor);
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