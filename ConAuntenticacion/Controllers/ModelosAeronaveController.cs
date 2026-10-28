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
    public class ModelosAeronaveController : Controller
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // GET: ModelosAeronave
        public ActionResult Index()
        {
            return View(db.ModeloAeronave.ToList());
        }

        // GET: ModelosAeronave/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ModeloAeronave modeloAeronave = db.ModeloAeronave.Find(id);
            if (modeloAeronave == null)
            {
                return HttpNotFound();
            }
            return View(modeloAeronave);
        }

        // GET: ModelosAeronave/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ModelosAeronave/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ModeloAeronaveId,Fabricante,Nombre,CapacidadAsientos")] ModeloAeronave modeloAeronave)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.ModeloAeronave.Add(modeloAeronave);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Reglas de la base de datos (índices únicos, CHECK y triggers)
                    ModelState.AddModelError("", "No se pudo guardar: " + ServicioAerolinea.MensajeError(ex));
                }
            }

            return View(modeloAeronave);
        }

        // GET: ModelosAeronave/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ModeloAeronave modeloAeronave = db.ModeloAeronave.Find(id);
            if (modeloAeronave == null)
            {
                return HttpNotFound();
            }
            return View(modeloAeronave);
        }

        // POST: ModelosAeronave/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ModeloAeronaveId,Fabricante,Nombre,CapacidadAsientos")] ModeloAeronave modeloAeronave)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(modeloAeronave).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Reglas de la base de datos (índices únicos, CHECK y triggers)
                    ModelState.AddModelError("", "No se pudo guardar: " + ServicioAerolinea.MensajeError(ex));
                }
            }
            return View(modeloAeronave);
        }

        // GET: ModelosAeronave/Delete/5
        [Authorize(Roles = "SuperUsuario")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ModeloAeronave modeloAeronave = db.ModeloAeronave.Find(id);
            if (modeloAeronave == null)
            {
                return HttpNotFound();
            }
            return View(modeloAeronave);
        }

        // POST: ModelosAeronave/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario")]
        public ActionResult DeleteConfirmed(int id)
        {
            ModeloAeronave modeloAeronave = db.ModeloAeronave.Find(id);
            try
            {
                db.ModeloAeronave.Remove(modeloAeronave);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Integridad referencial: el registro está usado en otras tablas
                ModelState.AddModelError("", "No se puede eliminar porque está relacionado con otros registros. " + ServicioAerolinea.MensajeError(ex));
                return View(modeloAeronave);
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
