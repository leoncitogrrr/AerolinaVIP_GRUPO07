using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ConAuntenticacion.Models
{
    // =====================================================================
    // PANTALLA 5: BÚSQUEDA DE VUELOS (validaciones en el modelo)
    // =====================================================================
    public class BusquedaVuelosViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Seleccione el origen.")]
        [Display(Name = "Origen")]
        public string Origen { get; set; }

        [Required(ErrorMessage = "Seleccione el destino.")]
        [Display(Name = "Destino")]
        public string Destino { get; set; }

        [Required(ErrorMessage = "Seleccione la fecha de viaje.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Fecha de viaje")]
        public Nullable<DateTime> Fecha { get; set; }

        // RN-03: máximo 7 asientos por vuelo
        [Required(ErrorMessage = "Indique la cantidad de pasajeros.")]
        [Range(1, 7, ErrorMessage = "Entre 1 y 7 pasajeros (capacidad máxima del jet).")]
        [Display(Name = "Pasajeros")]
        public int Pasajeros { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(Origen) && Origen == Destino)
            {
                yield return new ValidationResult("El origen y el destino deben ser distintos.", new[] { "Destino" });
            }
            if (Fecha.HasValue && Fecha.Value.Date < DateTime.Today)
            {
                yield return new ValidationResult("La fecha de viaje no puede ser anterior a hoy.", new[] { "Fecha" });
            }

            // Servidor: debe existir una ruta activa (directa o con escala) entre origen y destino
            if (!string.IsNullOrEmpty(Origen) && !string.IsNullOrEmpty(Destino) && Origen != Destino)
            {
                bool existe;
                using (ServicioAerolinea servicio = new ServicioAerolinea())
                {
                    existe = servicio.DestinosDesde(Origen).Any(d => d.Codigo == Destino);
                }
                if (!existe)
                {
                    yield return new ValidationResult("No existe una ruta entre el origen y el destino seleccionados.", new[] { "Destino" });
                }
            }
        }
    }

    // =====================================================================
    // PANTALLA 1: PÁGINA DE INICIO (formulario «Encuentra tu vuelo» + destinos)
    // Hereda la búsqueda para que el formulario envíe Origen, Destino, Fecha y Pasajeros
    // con sus mismas validaciones a Vuelos/Resultados.
    // =====================================================================
    public class HomeViewModel : BusquedaVuelosViewModel
    {
        public List<System.Web.Mvc.SelectListItem> Aeropuertos { get; set; }
        public List<System.Web.Mvc.SelectListItem> DestinosOrigen { get; set; }
        public List<DestinoViewModel> Destinos { get; set; }
    }

    public class DestinoViewModel
    {
        private static readonly string[] nombres = { "", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado", "domingo" };

        public string Codigo { get; set; }
        public string Ciudad { get; set; }
        public string Escala { get; set; }
        public List<int> Dias { get; set; }

        // "Martes, jueves y sábado"
        public string DiasTexto
        {
            get
            {
                List<string> d = Dias.Select(x => nombres[x]).ToList();
                string texto = d.Count <= 1 ? string.Join("", d) : string.Join(", ", d.Take(d.Count - 1)) + " y " + d.Last();
                return texto.Length > 0 ? char.ToUpper(texto[0]) + texto.Substring(1) : texto;
            }
        }
    }

    // =====================================================================
    // PANTALLA 6: RESULTADOS
    // =====================================================================
    public class ResultadoVueloViewModel
    {
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public string Origen { get; set; }
        public string CiudadOrigen { get; set; }
        public string Destino { get; set; }
        public string CiudadDestino { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public decimal Tarifa { get; set; }
        public int Capacidad { get; set; }
        public int Disponibles { get; set; }
        public bool TieneAeronave { get; set; }
        public string Matricula { get; set; }
        public string Estado { get; set; }

        public DateTime Salida
        {
            get { return Fecha.Date.Add(HoraSalida); }
        }

        // Se puede vender si tiene aeronave (RNP-01), está programado/confirmado y aún no sale
        public bool Comercializable
        {
            get
            {
                return TieneAeronave
                    && (Estado == "PROGRAMADO" || Estado == "CONFIRMADO")
                    && Salida > DateTime.Now;
            }
        }
    }

    // Itinerario con escala (ej. Quito -> Guayaquil -> Galápagos)
    public class ConexionViewModel
    {
        public ResultadoVueloViewModel Tramo1 { get; set; }
        public ResultadoVueloViewModel Tramo2 { get; set; }
    }

    public class ResultadosBusquedaViewModel
    {
        public BusquedaVuelosViewModel Busqueda { get; set; }
        public List<ResultadoVueloViewModel> Directos { get; set; }
        public List<ConexionViewModel> Conexiones { get; set; }
    }

    // =====================================================================
    // PANTALLA 7: DISPONIBILIDAD DE ASIENTOS
    // =====================================================================
    public class AsientoViewModel
    {
        public int NumeroAsiento { get; set; }
        public string Codigo { get; set; }
        public int Fila { get; set; }
        public string Lado { get; set; }
        public string Ubicacion { get; set; }
        public string EstadoVisual { get; set; }   // DISPONIBLE | RESERVADO | OCUPADO
        public string ColorEstado { get; set; }    // Verde | Morado | Rojo (de vw_EstadoAsientoVuelo)
    }

    public class DisponibilidadViewModel
    {
        public ResultadoVueloViewModel Vuelo { get; set; }
        public int Pasajeros { get; set; }
        public List<AsientoViewModel> Asientos { get; set; }
    }

    // =====================================================================
    // DTO: filas de las VISTAS SQL (se leen con Database.SqlQuery)
    // =====================================================================
    public class VueloDetalleDto
    {
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public DateTime FechaOperacion { get; set; }
        public int RutaId { get; set; }
        public decimal TarifaReferencial { get; set; }
        public string Origen { get; set; }
        public string CiudadOrigen { get; set; }
        public string Destino { get; set; }
        public string CiudadDestino { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public Nullable<int> AeronaveId { get; set; }
        public string EstadoVueloCodigo { get; set; }
    }

    public class EstadoAsientoDto
    {
        public int VueloId { get; set; }
        public int NumeroAsiento { get; set; }
        public string CodigoAsiento { get; set; }
        public int Fila { get; set; }
        public string Lado { get; set; }
        public string Ubicacion { get; set; }
        public string EstadoVisual { get; set; }
        public string ColorEstado { get; set; }
    }

    public class DisponibilidadVueloDto
    {
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public string EstadoVueloCodigo { get; set; }
        public string Matricula { get; set; }
        public Nullable<int> Capacidad { get; set; }
        public Nullable<int> Ocupados { get; set; }
        public Nullable<int> Disponibles { get; set; }
    }

    public class ComprobanteDetalleDto
    {
        public int ComprobanteId { get; set; }
        public string NumeroComprobante { get; set; }
        public string TipoComprobanteCodigo { get; set; }
        public string TipoComprobante { get; set; }
        public DateTime FechaEmision { get; set; }
        public string EstadoComprobanteCodigo { get; set; }
        public string EmisorNombre { get; set; }
        public string EmisorIdentificacion { get; set; }
        public string EmisorDireccion { get; set; }
        public string LeyendaComprobante { get; set; }
        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public string UsuarioId { get; set; }
        public int CompraId { get; set; }
        public DateTime FechaCompra { get; set; }
        public decimal TotalOperacion { get; set; }
        public string MonedaCodigo { get; set; }
        public string EstadoCompraCodigo { get; set; }
        public Nullable<int> PagoId { get; set; }
        public string CodigoOperacion { get; set; }
        public Nullable<DateTime> FechaPago { get; set; }
        public string EstadoPagoCodigo { get; set; }
        public string MetodoPago { get; set; }
        public string MarcaTarjeta { get; set; }
        public string TarjetaEnmascarada { get; set; }
    }
}