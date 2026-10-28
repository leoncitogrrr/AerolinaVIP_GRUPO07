using System.Net;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using ConAuntenticacion.Models;

namespace ConAuntenticacion.Controllers
{
    // Pantalla 16: comprobantes académicos (vista HTML y descarga en PDF)
    [Authorize]
    public class ComprobantesController : Controller
    {
        private ServicioAerolinea servicio = new ServicioAerolinea();

        private bool EsPersonal()
        {
            return User.IsInRole(RolesSistema.Counter) || User.IsInRole(RolesSistema.Administrador) || User.IsInRole(RolesSistema.SuperUsuario);
        }

        // GET: /Comprobantes
        public ActionResult Index(string buscar)
        {
            ViewBag.Buscar = buscar;
            return View(servicio.ListarComprobantes(User.Identity.GetUserId(), EsPersonal(), buscar));
        }

        // GET: /Comprobantes/Ver/5
        public ActionResult Ver(int? id)
        {
            ActionResult error = Verificar(id);
            if (error != null)
            {
                return error;
            }
            return View(servicio.ObtenerComprobante(id.Value));
        }

        // GET: /Comprobantes/Pdf/5  -> descarga el comprobante en PDF
        public ActionResult Pdf(int? id)
        {
            ActionResult error = Verificar(id);
            if (error != null)
            {
                return error;
            }
            ComprobanteViewModel comprobante = servicio.ObtenerComprobante(id.Value);
            byte[] pdf = GeneradorPdf.Comprobante(comprobante);
            return File(pdf, "application/pdf", "Comprobante_" + comprobante.Encabezado.NumeroComprobante + ".pdf");
        }

        private ActionResult Verificar(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (!servicio.PuedeVerComprobante(id.Value, User.Identity.GetUserId(), EsPersonal()))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "El comprobante no pertenece a su cuenta.");
            }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                servicio.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
