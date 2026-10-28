using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using ConAuntenticacion.Models;

namespace ConAuntenticacion.Controllers
{
    // Pantallas 8, 9, 10 (flujo de reserva), 13 (mis reservas) y 14 (gestión y cancelación)
    [Authorize]
    public class ReservasController : Controller
    {
        private ServicioAerolinea servicio = new ServicioAerolinea();
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        // La selección se guarda en Session hasta confirmar: es TEMPORAL y no bloquea asientos
        private const string ClaveSesion = "ReservaEnCurso";

        private ReservaEnCurso EnCurso
        {
            get { return Session[ClaveSesion] as ReservaEnCurso; }
            set { Session[ClaveSesion] = value; }
        }

        private bool EsPersonal()
        {
            return User.IsInRole(RolesSistema.Counter) || User.IsInRole(RolesSistema.Administrador) || User.IsInRole(RolesSistema.SuperUsuario);
        }

        // ================= PANTALLA 8: SELECCIÓN DE ASIENTO =================
        [Authorize(Roles = RolesSistema.CompraPasajes)]
        public ActionResult SeleccionAsiento(int? id, int pasajeros = 1)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            servicio.ExpirarReservasVencidas();
            ResultadoVueloViewModel vuelo = servicio.ObtenerVuelo(id.Value);
            if (vuelo == null)
            {
                return HttpNotFound();
            }

            SeleccionAsientoViewModel modelo = new SeleccionAsientoViewModel();
            modelo.VueloId = id.Value;
            modelo.CantidadPasajeros = pasajeros < 1 ? 1 : (pasajeros > PoliticasAerolinea.MaximoPasajeros ? PoliticasAerolinea.MaximoPasajeros : pasajeros);
            modelo.Vuelo = vuelo;
            modelo.Asientos = servicio.ObtenerAsientos(id.Value);
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.CompraPasajes)]
        public ActionResult SeleccionAsiento(SeleccionAsientoViewModel modelo)
        {
            if (ModelState.IsValid)
            {
                ReservaEnCurso enCurso = new ReservaEnCurso();
                enCurso.VueloId = modelo.VueloId;
                enCurso.CantidadPasajeros = modelo.CantidadPasajeros;
                enCurso.Asientos = modelo.ListaAsientos();
                EnCurso = enCurso;
                return RedirectToAction("Pasajeros");
            }

            modelo.Vuelo = servicio.ObtenerVuelo(modelo.VueloId);
            modelo.Asientos = servicio.ObtenerAsientos(modelo.VueloId);
            return View(modelo);
        }

        // ================= PANTALLA 9: DATOS DE LOS PASAJEROS =================
        [Authorize(Roles = RolesSistema.CompraPasajes)]
        public ActionResult Pasajeros()
        {
            ReservaEnCurso enCurso = EnCurso;
            if (enCurso == null)
            {
                TempData["Error"] = "Primero busque un vuelo y seleccione sus asientos.";
                return RedirectToAction("Buscar", "Vuelos");
            }

            List<AsientoViewModel> asientos = servicio.ObtenerAsientos(enCurso.VueloId);
            DatosPasajerosViewModel modelo = new DatosPasajerosViewModel();
            modelo.VueloId = enCurso.VueloId;
            modelo.Vuelo = servicio.ObtenerVuelo(enCurso.VueloId);
            modelo.Pasajeros = enCurso.Pasajeros ?? enCurso.Asientos.Select(numero => new PasajeroReservaViewModel
            {
                NumeroAsiento = numero,
                CodigoAsiento = CodigoAsiento(asientos, numero),
                TipoDocumentoCodigo = "CEDULA"
            }).ToList();

            CargarTiposDocumento();
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.CompraPasajes)]
        public ActionResult Pasajeros(DatosPasajerosViewModel modelo)
        {
            ReservaEnCurso enCurso = EnCurso;
            if (enCurso == null || modelo.Pasajeros == null || modelo.Pasajeros.Count != enCurso.Asientos.Count)
            {
                TempData["Error"] = "La selección de asientos ya no es válida. Vuelva a seleccionarlos.";
                return RedirectToAction("Buscar", "Vuelos");
            }

            // Los asientos se toman de la sesión (no del formulario)
            List<AsientoViewModel> asientos = servicio.ObtenerAsientos(enCurso.VueloId);
            for (int i = 0; i < modelo.Pasajeros.Count; i++)
            {
                modelo.Pasajeros[i].NumeroAsiento = enCurso.Asientos[i];
                modelo.Pasajeros[i].CodigoAsiento = CodigoAsiento(asientos, enCurso.Asientos[i]);
            }

            if (ModelState.IsValid)
            {
                enCurso.Pasajeros = modelo.Pasajeros;
                EnCurso = enCurso;
                return RedirectToAction("Confirmar");
            }

            modelo.VueloId = enCurso.VueloId;
            modelo.Vuelo = servicio.ObtenerVuelo(enCurso.VueloId);
            CargarTiposDocumento();
            return View(modelo);
        }

        // ================= PANTALLA 10: CONFIRMAR LA RESERVA =================
        [Authorize(Roles = RolesSistema.CompraPasajes)]
        public ActionResult Confirmar()
        {
            ReservaEnCurso enCurso = EnCurso;
            if (enCurso == null || enCurso.Pasajeros == null)
            {
                TempData["Error"] = "No hay una reserva en curso.";
                return RedirectToAction("Buscar", "Vuelos");
            }
            return View(ArmarConfirmacion(new ConfirmarReservaViewModel(), enCurso));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.CompraPasajes)]
        public ActionResult Confirmar(ConfirmarReservaViewModel modelo)
        {
            ReservaEnCurso enCurso = EnCurso;
            if (enCurso == null || enCurso.Pasajeros == null)
            {
                // RNP-10: si se envía dos veces, la segunda ya no encuentra la reserva en curso
                TempData["Error"] = "La reserva ya fue registrada o la sesión expiró. Revise «Mis reservas».";
                return RedirectToAction("MisReservas");
            }

            if (ModelState.IsValid)
            {
                // El Counter registra la reserva a nombre del cliente (DP-13)
                string usuarioId = User.IsInRole(RolesSistema.Counter)
                    ? ServicioUsuarios.BuscarIdCliente(modelo.UsuarioCliente)
                    : User.Identity.GetUserId();
                try
                {
                    int reservaId = servicio.CrearReserva(enCurso, usuarioId);
                    Session.Remove(ClaveSesion);
                    TempData["Mensaje"] = "Reserva registrada. Sus asientos quedan RESERVADOS (morado) por " +
                                          PoliticasAerolinea.HorasRetencionReserva + " horas hasta completar el pago.";
                    return RedirectToAction("Pagar", "Compras", new { id = reservaId });
                }
                catch (ReglaNegocioException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(ArmarConfirmacion(modelo, enCurso));
        }

        // ================= PANTALLA 13: MIS RESERVAS =================
        public ActionResult MisReservas(string estado)
        {
            servicio.ExpirarReservasVencidas();
            List<ReservaResumenViewModel> reservas = servicio.ReservasDeUsuario(User.Identity.GetUserId());
            if (!string.IsNullOrEmpty(estado))
            {
                reservas = reservas.Where(r => r.EstadoReserva == estado).ToList();
            }
            ViewBag.Estado = estado;
            return View(reservas);
        }

        // ================= PANTALLA 14: GESTIÓN Y CANCELACIÓN =================
        public ActionResult Gestionar(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (!servicio.PuedeAcceder(id.Value, User.Identity.GetUserId(), EsPersonal()))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "La reserva no pertenece a su cuenta.");
            }
            servicio.ExpirarReservasVencidas();
            return View(servicio.ObtenerResumen(id.Value));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancelar(CancelarReservaViewModel modelo)
        {
            if (!servicio.PuedeAcceder(modelo.ReservaId, User.Identity.GetUserId(), EsPersonal()))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "La reserva no pertenece a su cuenta.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    TempData["Mensaje"] = servicio.CancelarReserva(modelo.ReservaId);
                    return RedirectToAction("Gestionar", new { id = modelo.ReservaId });
                }
                catch (ReglaNegocioException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View("Gestionar", servicio.ObtenerResumen(modelo.ReservaId));
        }

        // ---------------- APOYO ----------------
        private ConfirmarReservaViewModel ArmarConfirmacion(ConfirmarReservaViewModel modelo, ReservaEnCurso enCurso)
        {
            modelo.Vuelo = servicio.ObtenerVuelo(enCurso.VueloId);
            modelo.Pasajeros = enCurso.Pasajeros;
            modelo.Subtotal = modelo.Vuelo.Tarifa * enCurso.Pasajeros.Count;
            modelo.Iva = PoliticasAerolinea.CalcularIva(modelo.Subtotal);
            modelo.Total = modelo.Subtotal + modelo.Iva;
            return modelo;
        }

        private static string CodigoAsiento(List<AsientoViewModel> asientos, int numero)
        {
            AsientoViewModel a = asientos.FirstOrDefault(x => x.NumeroAsiento == numero);
            return a != null ? a.Codigo : numero.ToString();
        }

        private void CargarTiposDocumento()
        {
            ViewBag.TiposDocumento = db.TipoDocumentoIdentidad.OrderBy(t => t.Descripcion).ToList()
                .Select(t => new SelectListItem { Value = t.Codigo, Text = t.Descripcion }).ToList();
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
