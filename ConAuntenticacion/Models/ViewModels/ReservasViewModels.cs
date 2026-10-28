using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ConAuntenticacion.Models
{
    // =====================================================================
    // DATOS TEMPORALES DE LA COMPRA (se guardan en Session hasta confirmar)
    // La selección es TEMPORAL: no bloquea asientos (sin concurrencia avanzada).
    // =====================================================================
    [Serializable]
    public class ReservaEnCurso
    {
        public int VueloId { get; set; }
        public int CantidadPasajeros { get; set; }
        public List<int> Asientos { get; set; }
        public List<PasajeroReservaViewModel> Pasajeros { get; set; }
    }

    // =====================================================================
    // PANTALLA 8: SELECCIÓN DE ASIENTO
    // =====================================================================
    public class SeleccionAsientoViewModel : IValidatableObject
    {
        [Required]
        public int VueloId { get; set; }

        [Required]
        [Range(1, 7, ErrorMessage = "Entre 1 y 7 pasajeros.")]
        [Display(Name = "Pasajeros")]
        public int CantidadPasajeros { get; set; }

        // Números de asiento separados por coma (ej. "1,3"), los llena el mapa con jQuery
        [Required(ErrorMessage = "Seleccione al menos un asiento en el mapa.")]
        [RegularExpression(@"^[1-7](,[1-7]){0,6}$", ErrorMessage = "Selección de asientos no válida.")]
        [Display(Name = "Asientos seleccionados")]
        public string AsientosSeleccionados { get; set; }

        // Solo para mostrar
        public ResultadoVueloViewModel Vuelo { get; set; }
        public List<AsientoViewModel> Asientos { get; set; }

        public List<int> ListaAsientos()
        {
            if (string.IsNullOrEmpty(AsientosSeleccionados))
            {
                return new List<int>();
            }
            return AsientosSeleccionados.Split(',').Select(a => int.Parse(a)).ToList();
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<int> lista = ListaAsientos();

            if (lista.Count != CantidadPasajeros)
            {
                yield return new ValidationResult("Debe seleccionar exactamente " + CantidadPasajeros + " asiento(s), uno por pasajero.",
                                                  new[] { "AsientosSeleccionados" });
                yield break;
            }
            if (lista.Distinct().Count() != lista.Count)
            {
                yield return new ValidationResult("No puede repetir un asiento.", new[] { "AsientosSeleccionados" });
                yield break;
            }

            // RNP-04: el servidor revalida que el vuelo se venda y que los asientos sigan disponibles
            string error;
            using (ServicioAerolinea servicio = new ServicioAerolinea())
            {
                error = servicio.ValidarAsientos(VueloId, lista);
            }
            if (error != null)
            {
                yield return new ValidationResult(error, new[] { "AsientosSeleccionados" });
            }
        }
    }

    // =====================================================================
    // PANTALLA 9: DATOS DEL PASAJERO (uno por asiento)
    // =====================================================================
    [Serializable]
    public class PasajeroReservaViewModel : IValidatableObject
    {
        public int NumeroAsiento { get; set; }
        public string CodigoAsiento { get; set; }

        [Required(ErrorMessage = "Seleccione el tipo de documento.")]
        [Display(Name = "Tipo de documento")]
        public string TipoDocumentoCodigo { get; set; }

        [Required(ErrorMessage = "El número de documento es obligatorio.")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "Entre {2} y {1} caracteres.")]
        [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Solo letras y números, sin espacios.")]
        [Display(Name = "Número de documento")]
        public string NumeroDocumento { get; set; }

        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ ]+$", ErrorMessage = "Solo letras y espacios.")]
        [Display(Name = "Nombres")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ ]+$", ErrorMessage = "Solo letras y espacios.")]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Fecha de nacimiento")]
        public Nullable<DateTime> FechaNacimiento { get; set; }

        [EmailAddress(ErrorMessage = "Formato de correo no válido.")]
        [StringLength(256)]
        [Display(Name = "Correo (opcional)")]
        public string Correo { get; set; }

        [RegularExpression(@"^[0-9+ ]{7,20}$", ErrorMessage = "Solo números, + y espacios (7 a 20).")]
        [Display(Name = "Teléfono (opcional)")]
        public string Telefono { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (TipoDocumentoCodigo == "CEDULA" && !Pasajero.CedulaValida(NumeroDocumento))
            {
                yield return new ValidationResult("La cédula ecuatoriana no es válida.", new[] { "NumeroDocumento" });
            }
            if (FechaNacimiento.HasValue && FechaNacimiento.Value.Date > DateTime.Today)
            {
                yield return new ValidationResult("La fecha de nacimiento no puede ser futura.", new[] { "FechaNacimiento" });
            }
            if (FechaNacimiento.HasValue && FechaNacimiento.Value.Year < 1900)
            {
                yield return new ValidationResult("La fecha de nacimiento no es válida.", new[] { "FechaNacimiento" });
            }
        }
    }

    public class DatosPasajerosViewModel : IValidatableObject
    {
        public int VueloId { get; set; }
        public List<PasajeroReservaViewModel> Pasajeros { get; set; }

        // Solo para mostrar
        public ResultadoVueloViewModel Vuelo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Pasajeros == null || Pasajeros.Count == 0)
            {
                yield return new ValidationResult("Debe registrar los datos de los pasajeros.");
                yield break;
            }

            // Un mismo documento no puede repetirse en la misma reserva
            var repetidos = Pasajeros
                .Where(p => !string.IsNullOrEmpty(p.NumeroDocumento))
                .GroupBy(p => (p.TipoDocumentoCodigo ?? "") + "|" + p.NumeroDocumento.ToUpper())
                .Where(g => g.Count() > 1)
                .ToList();
            if (repetidos.Any())
            {
                yield return new ValidationResult("Hay pasajeros con el mismo documento. Cada asiento corresponde a una persona distinta.");
            }

            // Un pasajero no puede tener dos asientos vigentes en el mismo vuelo (UX_RPV_PasajeroActivo)
            string error;
            using (ServicioAerolinea servicio = new ServicioAerolinea())
            {
                error = servicio.ValidarPasajerosEnVuelo(VueloId, Pasajeros);
            }
            if (error != null)
            {
                yield return new ValidationResult(error);
            }
        }
    }

    // =====================================================================
    // PANTALLA 10: CONFIRMACIÓN DE LA RESERVA
    // =====================================================================
    public class ConfirmarReservaViewModel : IValidatableObject
    {
        [Display(Name = "Acepto las condiciones de reserva, pago y cancelación")]
        public bool AceptaCondiciones { get; set; }

        // Solo para el Counter: usuario del cliente a nombre de quien se reserva (DP-13)
        [StringLength(256)]
        [Display(Name = "Usuario del cliente")]
        public string UsuarioCliente { get; set; }

        // Solo para mostrar
        public ResultadoVueloViewModel Vuelo { get; set; }
        public List<PasajeroReservaViewModel> Pasajeros { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!AceptaCondiciones)
            {
                yield return new ValidationResult("Debe aceptar las condiciones para registrar la reserva.", new[] { "AceptaCondiciones" });
            }

            var usuario = HttpContext.Current.User;
            if (usuario != null && usuario.IsInRole(RolesSistema.Counter))
            {
                if (string.IsNullOrWhiteSpace(UsuarioCliente))
                {
                    yield return new ValidationResult("Indique el usuario del cliente a nombre de quien se registra la reserva.", new[] { "UsuarioCliente" });
                }
                else if (ServicioUsuarios.BuscarIdCliente(UsuarioCliente) == null)
                {
                    yield return new ValidationResult("No existe un cliente registrado con ese nombre de usuario.", new[] { "UsuarioCliente" });
                }
            }
        }
    }

    // =====================================================================
    // PANTALLAS 12, 13 y 14: RESUMEN DE UNA RESERVA
    // =====================================================================
    public class DetalleAsignacionViewModel
    {
        public int AsignacionId { get; set; }
        public string Pasajero { get; set; }
        public string Documento { get; set; }
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public string Ruta { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public string Asiento { get; set; }
        public string Ubicacion { get; set; }
        public decimal Importe { get; set; }
        public string EstadoAsignacion { get; set; }
        public bool CheckInRealizado { get; set; }

        public DateTime Salida
        {
            get { return Fecha.Date.Add(HoraSalida); }
        }
    }

    public class ComprobanteItemViewModel
    {
        public int ComprobanteId { get; set; }
        public string Numero { get; set; }
        public string TipoCodigo { get; set; }
        public string Tipo { get; set; }
        public DateTime FechaEmision { get; set; }
        public string Estado { get; set; }
        public string CodigoReserva { get; set; }
        public decimal Total { get; set; }
    }

    public class ReservaResumenViewModel
    {
        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public string UsuarioId { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaCreacion { get; set; }
        public Nullable<DateTime> FechaExpiracion { get; set; }
        public string EstadoReserva { get; set; }

        public Nullable<int> CompraId { get; set; }
        public string EstadoCompra { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }

        public string EstadoPago { get; set; }
        public string MensajePago { get; set; }
        public string TarjetaPago { get; set; }

        public List<DetalleAsignacionViewModel> Detalle { get; set; }
        public List<ComprobanteItemViewModel> Comprobantes { get; set; }

        // Reglas de cancelación (DP-08)
        public bool PuedeCancelar { get; set; }
        public string MotivoNoCancelar { get; set; }
        public decimal PorcentajeReembolso { get; set; }
        public decimal ImporteReembolso { get; set; }

        public Nullable<DateTime> PrimeraSalida
        {
            get
            {
                if (Detalle == null || !Detalle.Any())
                {
                    return null;
                }
                return Detalle.Min(d => d.Salida);
            }
        }

        public bool PuedePagar
        {
            get { return EstadoReserva == "PENDIENTE" && EstadoCompra == "PENDIENTE"; }
        }
    }

    // =====================================================================
    // PANTALLA 14: CANCELACIÓN
    // =====================================================================
    public class CancelarReservaViewModel : IValidatableObject
    {
        [Required]
        public int ReservaId { get; set; }

        [Display(Name = "Confirmo que deseo cancelar la reserva")]
        public bool ConfirmaCancelacion { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!ConfirmaCancelacion)
            {
                yield return new ValidationResult("Debe confirmar la cancelación.", new[] { "ConfirmaCancelacion" });
                yield break;
            }

            // DP-08: se revisa si la política permite cancelar
            string error;
            using (ServicioAerolinea servicio = new ServicioAerolinea())
            {
                error = servicio.EvaluarCancelacion(ReservaId);
            }
            if (error != null)
            {
                yield return new ValidationResult(error);
            }
        }
    }
}
