using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;

namespace ConAuntenticacion.Models
{
    // =====================================================================
    // PANTALLA 11: PAGO SIMULADO (no se guarda el número completo ni el CVV)
    // =====================================================================
    public class PagoViewModel : IValidatableObject
    {
        [Required]
        public int ReservaId { get; set; }

        [Required(ErrorMessage = "Seleccione el método de pago.")]
        [Display(Name = "Método de pago")]
        public string MetodoPagoCodigo { get; set; }

        [Required(ErrorMessage = "Seleccione la marca de la tarjeta.")]
        [Display(Name = "Marca de la tarjeta")]
        public string MarcaTarjetaCodigo { get; set; }

        [Required(ErrorMessage = "El número de tarjeta es obligatorio.")]
        [RegularExpression(@"^[0-9]{12,19}$", ErrorMessage = "Solo números, entre 12 y 19 dígitos, sin espacios.")]
        [Display(Name = "Número de tarjeta (ficticio)")]
        public string NumeroTarjeta { get; set; }

        [Required(ErrorMessage = "El nombre del titular es obligatorio.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Entre {2} y {1} caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ ]+$", ErrorMessage = "Solo letras y espacios.")]
        [Display(Name = "Nombre del titular")]
        public string NombreTitular { get; set; }

        [Required(ErrorMessage = "Seleccione el mes.")]
        [Range(1, 12, ErrorMessage = "Mes no válido.")]
        [Display(Name = "Mes de vencimiento")]
        public Nullable<int> MesExpiracion { get; set; }

        [Required(ErrorMessage = "Seleccione el año.")]
        [Range(2020, 2050, ErrorMessage = "Año no válido.")]
        [Display(Name = "Año de vencimiento")]
        public Nullable<int> AnioExpiracion { get; set; }

        [Required(ErrorMessage = "El código de seguridad es obligatorio.")]
        [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "3 o 4 dígitos.")]
        [DataType(DataType.Password)]
        [Display(Name = "Código de seguridad")]
        public string CodigoSeguridad { get; set; }

        // Solo para mostrar
        public ReservaResumenViewModel Reserva { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Formato según la marca (tabla MarcaTarjeta: longitudes y prefijos)
            MarcaTarjeta marca = null;
            using (AerolineaVIPEntities db = new AerolineaVIPEntities())
            {
                if (!string.IsNullOrEmpty(MarcaTarjetaCodigo))
                {
                    marca = db.MarcaTarjeta.FirstOrDefault(m => m.Codigo == MarcaTarjetaCodigo && m.Activa);
                }
            }

            if (marca == null)
            {
                yield return new ValidationResult("La marca seleccionada no está disponible.", new[] { "MarcaTarjetaCodigo" });
                yield break;
            }

            if (!string.IsNullOrEmpty(NumeroTarjeta))
            {
                if (NumeroTarjeta.Length != marca.LongitudNumero)
                {
                    yield return new ValidationResult("Una tarjeta " + marca.Nombre + " tiene " + marca.LongitudNumero + " dígitos.", new[] { "NumeroTarjeta" });
                }
                else if (!ValidacionTarjeta.PrefijoValido(NumeroTarjeta, marca.PrefijosReferencia))
                {
                    yield return new ValidationResult("El número no corresponde a la marca " + marca.Nombre + " (prefijos: " + marca.PrefijosReferencia + ").", new[] { "NumeroTarjeta" });
                }
                else if (!ValidacionTarjeta.Luhn(NumeroTarjeta))
                {
                    yield return new ValidationResult("El número de tarjeta no es válido (dígito de control).", new[] { "NumeroTarjeta" });
                }
            }

            if (!string.IsNullOrEmpty(CodigoSeguridad) && CodigoSeguridad.Length != marca.LongitudCodigoSeguridad)
            {
                yield return new ValidationResult("El código de seguridad de " + marca.Nombre + " tiene " + marca.LongitudCodigoSeguridad + " dígitos.", new[] { "CodigoSeguridad" });
            }

            if (MesExpiracion.HasValue && AnioExpiracion.HasValue)
            {
                DateTime finMes = new DateTime(AnioExpiracion.Value, MesExpiracion.Value, 1).AddMonths(1);
                if (finMes <= DateTime.Today)
                {
                    yield return new ValidationResult("La tarjeta está vencida.", new[] { "MesExpiracion" });
                }
            }
        }
    }

    // Reglas de formato de tarjeta (solo validación de formato; no se verifica ninguna tarjeta real)
    public static class ValidacionTarjeta
    {
        // Algoritmo de Luhn (módulo 10)
        public static bool Luhn(string numero)
        {
            int suma = 0;
            bool doble = false;
            for (int i = numero.Length - 1; i >= 0; i--)
            {
                int d = numero[i] - '0';
                if (doble)
                {
                    d = d * 2;
                    if (d > 9) d = d - 9;
                }
                suma = suma + d;
                doble = !doble;
            }
            return suma % 10 == 0;
        }

        // Prefijos con formato "4" | "34, 37" | "51-55, 2221-2720"
        public static bool PrefijoValido(string numero, string prefijos)
        {
            if (string.IsNullOrEmpty(prefijos))
            {
                return true;
            }
            foreach (string parte in prefijos.Split(','))
            {
                string p = parte.Trim();
                if (p.Contains("-"))
                {
                    string[] r = p.Split('-');
                    int largo = r[0].Trim().Length;
                    if (numero.Length < largo) continue;
                    int desde = int.Parse(r[0].Trim());
                    int hasta = int.Parse(r[1].Trim());
                    int inicio = int.Parse(numero.Substring(0, largo));
                    if (inicio >= desde && inicio <= hasta) return true;
                }
                else if (p.Length > 0 && numero.StartsWith(p))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class ResultadoPagoViewModel
    {
        public bool Aprobado { get; set; }
        public string Mensaje { get; set; }
    }

    // =====================================================================
    // PANTALLA 15: CHECK-IN
    // =====================================================================
    public class CheckInElegibleViewModel
    {
        public int AsignacionId { get; set; }
        public string CodigoReserva { get; set; }
        public string Pasajero { get; set; }
        public string Documento { get; set; }
        public string CodigoVuelo { get; set; }
        public string Ruta { get; set; }
        public DateTime Salida { get; set; }
        public string Asiento { get; set; }
        public string Estado { get; set; }          // Disponible | Aún no abre | Cerrado | Realizado
        public bool PuedeHacerCheckIn { get; set; }
        public Nullable<int> CheckInId { get; set; }
    }

    public class CheckInIndexViewModel
    {
        [RegularExpression(@"^[Rr][Vv][0-9]{8}$", ErrorMessage = "El código de reserva tiene el formato RV00000000.")]
        [Display(Name = "Código de reserva")]
        public string CodigoReserva { get; set; }

        public List<CheckInElegibleViewModel> Elegibles { get; set; }
    }

    public class RegistrarCheckInViewModel : IValidatableObject
    {
        [Required]
        public int AsignacionId { get; set; }

        [Display(Name = "Confirmo que el pasajero porta su documento de identidad vigente")]
        public bool ConfirmaDocumentos { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!ConfirmaDocumentos)
            {
                yield return new ValidationResult("Debe confirmar que el pasajero porta su documento de identidad.");
                yield break;
            }

            // DP-12: ventana de check-in, asiento confirmado y pertenencia de la reserva
            var usuario = HttpContext.Current.User;
            bool esPersonal = usuario.IsInRole(RolesSistema.Counter) || usuario.IsInRole(RolesSistema.Administrador) || usuario.IsInRole(RolesSistema.SuperUsuario);
            string error;
            using (ServicioAerolinea servicio = new ServicioAerolinea())
            {
                error = servicio.EvaluarCheckIn(AsignacionId, usuario.Identity.GetUserId(), esPersonal);
            }
            if (error != null)
            {
                yield return new ValidationResult(error);
            }
        }
    }

    // =====================================================================
    // PANTALLA 16: COMPROBANTE ACADÉMICO
    // =====================================================================
    public class ComprobanteViewModel
    {
        public ComprobanteDetalleDto Encabezado { get; set; }
        public string Cliente { get; set; }
        public List<DetalleAsignacionViewModel> Lineas { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
        public decimal ImporteReembolso { get; set; }
    }

    // =====================================================================
    // PANTALLA 18: ADMINISTRACIÓN DE VUELOS E ITINERARIOS
    // =====================================================================
    public class AdminVueloItemViewModel
    {
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public DateTime Fecha { get; set; }
        public string Ruta { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public Nullable<int> AeronaveId { get; set; }
        public string Matricula { get; set; }
        public string Estado { get; set; }
        public int Ocupados { get; set; }
        public int Capacidad { get; set; }
        public string Itinerario { get; set; }
    }

    public class AdminVuelosViewModel
    {
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Desde")]
        public Nullable<DateTime> Desde { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Hasta")]
        public Nullable<DateTime> Hasta { get; set; }

        public List<AdminVueloItemViewModel> Vuelos { get; set; }
        public List<Aeronave> Aeronaves { get; set; }
    }

    public class GenerarVuelosViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "La fecha inicial es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha inicial")]
        public Nullable<DateTime> FechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha final es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha final")]
        public Nullable<DateTime> FechaFin { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FechaInicio.HasValue && FechaFin.HasValue)
            {
                if (FechaFin.Value < FechaInicio.Value)
                {
                    yield return new ValidationResult("La fecha final debe ser posterior a la inicial.", new[] { "FechaFin" });
                }
                if ((FechaFin.Value - FechaInicio.Value).TotalDays > 365)
                {
                    yield return new ValidationResult("El rango no puede superar 366 días.", new[] { "FechaFin" });
                }
                if (FechaInicio.Value.Date < DateTime.Today)
                {
                    yield return new ValidationResult("No se generan vuelos en fechas pasadas.", new[] { "FechaInicio" });
                }
            }
        }
    }

    public class AsignarAeronaveViewModel
    {
        [Required]
        public int VueloId { get; set; }

        [Display(Name = "Aeronave")]
        public Nullable<int> AeronaveId { get; set; }
    }

    public class CambiarEstadoVueloViewModel : IValidatableObject
    {
        [Required]
        public int VueloId { get; set; }

        [Required(ErrorMessage = "Seleccione el estado.")]
        [RegularExpression("^(PROGRAMADO|CONFIRMADO|CANCELADO|COMPLETADO)$", ErrorMessage = "Estado no válido.")]
        [Display(Name = "Estado")]
        public string EstadoVueloCodigo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // RNP-07: no se cancela un vuelo con asientos reservados u ocupados sin reubicar a los pasajeros
            if (EstadoVueloCodigo == "CANCELADO")
            {
                int activos;
                using (AerolineaVIPEntities db = new AerolineaVIPEntities())
                {
                    activos = db.ReservaPasajeroVuelo.Count(r => r.VueloId == VueloId
                        && (r.EstadoAsignacionCodigo == "PENDIENTE" || r.EstadoAsignacionCodigo == "CONFIRMADA"));
                }
                if (activos > 0)
                {
                    yield return new ValidationResult("El vuelo tiene " + activos + " asiento(s) reservados u ocupados. Primero deben cancelarse esas reservas.");
                }
            }
        }
    }

    // =====================================================================
    // PANTALLA 19: AERONAVES
    // =====================================================================
    public class AeronaveResumenViewModel
    {
        public int AeronaveId { get; set; }
        public string Matricula { get; set; }
        public string Modelo { get; set; }
        public int Capacidad { get; set; }
        public string Estado { get; set; }
        public List<AdminVueloItemViewModel> ProximosVuelos { get; set; }
    }

    public class CambiarEstadoAeronaveViewModel
    {
        [Required]
        public int AeronaveId { get; set; }

        [Required(ErrorMessage = "Seleccione el estado.")]
        [RegularExpression("^(DISPONIBLE|NO_DISPONIBLE)$", ErrorMessage = "Estado no válido.")]
        public string EstadoAeronaveCodigo { get; set; }
    }

    // =====================================================================
    // PANTALLA 20: USUARIOS, PASAJEROS Y RESERVAS
    // =====================================================================
    public class UsuarioItemViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string NombreCompleto { get; set; }
        public string Roles { get; set; }
    }

    public class GestionViewModel
    {
        [StringLength(60)]
        [Display(Name = "Buscar (código de reserva o usuario)")]
        public string Buscar { get; set; }

        [Display(Name = "Estado de la reserva")]
        public string Estado { get; set; }

        public List<UsuarioItemViewModel> Usuarios { get; set; }
        public List<ReservaResumenViewModel> Reservas { get; set; }
    }

    public class AsignarRolViewModel : IValidatableObject
    {
        [Required]
        public string UsuarioId { get; set; }

        [Required(ErrorMessage = "Seleccione el rol.")]
        [RegularExpression("^(Cliente|Counter|Administrador|SuperUsuario)$", ErrorMessage = "Rol no válido.")]
        [Display(Name = "Rol")]
        public string Rol { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // El SuperUsuario no puede quitarse a sí mismo el rol (evita quedarse sin administración)
            if (UsuarioId == HttpContext.Current.User.Identity.GetUserId())
            {
                yield return new ValidationResult("No puede cambiar su propio rol.");
            }
        }
    }

    // =====================================================================
    // PANTALLA 21: CONSULTAS Y REPORTES
    // =====================================================================
    public class VentaRutaViewModel
    {
        public string Ruta { get; set; }
        public int Compras { get; set; }
        public int Pasajes { get; set; }
        public decimal Total { get; set; }
    }

    public class ConteoEstadoViewModel
    {
        public string Estado { get; set; }
        public int Cantidad { get; set; }
    }

    public class ReportesViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Indique la fecha inicial.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Desde")]
        public Nullable<DateTime> Desde { get; set; }

        [Required(ErrorMessage = "Indique la fecha final.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Hasta")]
        public Nullable<DateTime> Hasta { get; set; }

        public List<DisponibilidadVueloDto> Ocupacion { get; set; }
        public List<VentaRutaViewModel> VentasPorRuta { get; set; }
        public List<ConteoEstadoViewModel> ReservasPorEstado { get; set; }
        public List<ConteoEstadoViewModel> PagosPorEstado { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Desde.HasValue && Hasta.HasValue && Hasta.Value < Desde.Value)
            {
                yield return new ValidationResult("La fecha final debe ser posterior a la inicial.", new[] { "Hasta" });
            }
        }
    }
}
