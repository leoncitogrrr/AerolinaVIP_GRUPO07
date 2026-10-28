using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ConAuntenticacion.Models
{

    [MetadataType(typeof(AeropuertoMetadata))]
    public partial class Aeropuerto { }

    public class AeropuertoMetadata
    {
        [Required(ErrorMessage = "El código IATA es obligatorio.")]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "El código IATA son 3 letras MAYÚSCULAS (ej. UIO).")]
        [Display(Name = "Código IATA")]
        public string CodigoIATA { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150, ErrorMessage = "Máximo {1} caracteres.")]
        [Display(Name = "Nombre del aeropuerto")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La localidad es obligatoria.")]
        [StringLength(100, ErrorMessage = "Máximo {1} caracteres.")]
        [Display(Name = "Localidad")]
        public string Localidad { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; }
    }

    // ------------------------- RUTA -------------------------
    [MetadataType(typeof(RutaMetadata))]
    public partial class Ruta : IValidatableObject
    {
        // Regla: el origen no puede ser igual al destino
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (AeropuertoOrigenId == AeropuertoDestinoId)
            {
                yield return new ValidationResult("El origen y el destino deben ser distintos.",
                                                  new[] { "AeropuertoDestinoId" });
            }
        }
    }

    public class RutaMetadata
    {
        [Required(ErrorMessage = "Seleccione el aeropuerto de origen.")]
        [Display(Name = "Origen")]
        public int AeropuertoOrigenId { get; set; }

        [Required(ErrorMessage = "Seleccione el aeropuerto de destino.")]
        [Display(Name = "Destino")]
        public int AeropuertoDestinoId { get; set; }

        [Required(ErrorMessage = "La tarifa es obligatoria.")]
        [Range(0, 10000, ErrorMessage = "La tarifa debe estar entre {1} y {2} USD.")]
        [DataType(DataType.Currency)]
        [Display(Name = "Tarifa referencial (USD)")]
        public decimal TarifaReferencial { get; set; }

        [Display(Name = "Activa")]
        public bool Activa { get; set; }
    }

    // ------------------------- FRECUENCIA -------------------------
    [MetadataType(typeof(FrecuenciaMetadata))]
    public partial class Frecuencia : IValidatableObject
    {
        // Regla: la llegada debe ser posterior a la salida
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (HoraLlegada <= HoraSalida)
            {
                yield return new ValidationResult("La hora de llegada debe ser posterior a la hora de salida.",
                                                  new[] { "HoraLlegada" });
            }
        }
    }

    public class FrecuenciaMetadata
    {
        [Required(ErrorMessage = "Seleccione la ruta.")]
        [Display(Name = "Ruta")]
        public int RutaId { get; set; }

        [Required(ErrorMessage = "El día es obligatorio.")]
        [Range(1, 7, ErrorMessage = "El día va de 1 (lunes) a 7 (domingo).")]
        [Display(Name = "Día de la semana (1=Lun … 7=Dom)")]
        public byte DiaSemana { get; set; }

        [Required(ErrorMessage = "La hora de salida es obligatoria.")]
        [DataType(DataType.Time)]
        [Display(Name = "Hora de salida")]
        public TimeSpan HoraSalida { get; set; }

        [Required(ErrorMessage = "La hora de llegada es obligatoria.")]
        [DataType(DataType.Time)]
        [Display(Name = "Hora de llegada")]
        public TimeSpan HoraLlegada { get; set; }

        [Display(Name = "Activa")]
        public bool Activa { get; set; }
    }

    // ------------------------- MODELO DE AERONAVE -------------------------
    [MetadataType(typeof(ModeloAeronaveMetadata))]
    public partial class ModeloAeronave { }

    public class ModeloAeronaveMetadata
    {
        [Required(ErrorMessage = "El fabricante es obligatorio.")]
        [StringLength(60)]
        [Display(Name = "Fabricante")]
        public string Fabricante { get; set; }

        [Required(ErrorMessage = "El nombre del modelo es obligatorio.")]
        [StringLength(60)]
        [Display(Name = "Modelo")]
        public string Nombre { get; set; }

        // RN-03: máximo 7 asientos por vuelo
        [Required(ErrorMessage = "La capacidad es obligatoria.")]
        [Range(1, 7, ErrorMessage = "La capacidad máxima es de 7 asientos (RN-03).")]
        [Display(Name = "Capacidad de asientos")]
        public byte CapacidadAsientos { get; set; }
    }

    // ------------------------- AERONAVE -------------------------
    [MetadataType(typeof(AeronaveMetadata))]
    public partial class Aeronave { }

    public class AeronaveMetadata
    {
        [Required(ErrorMessage = "La matrícula es obligatoria.")]
        [StringLength(15, ErrorMessage = "Máximo {1} caracteres.")]
        [RegularExpression(@"^[A-Z0-9\-]+$", ErrorMessage = "Solo MAYÚSCULAS, números y guion (ej. AV-01).")]
        [Display(Name = "Matrícula")]
        public string Matricula { get; set; }

        [Required(ErrorMessage = "Seleccione el modelo.")]
        [Display(Name = "Modelo")]
        public int ModeloAeronaveId { get; set; }

        [Required(ErrorMessage = "Seleccione el estado.")]
        [Display(Name = "Estado")]
        public string EstadoAeronaveCodigo { get; set; }
    }

    // ------------------------- VUELO -------------------------
    [MetadataType(typeof(VueloMetadata))]
    public partial class Vuelo : IValidatableObject
    {
        // RN-04/05/06: la fecha debe caer en el día autorizado de la frecuencia
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            using (AerolineaVIPEntities db = new AerolineaVIPEntities())
            {
                Frecuencia f = db.Frecuencia.Find(FrecuenciaId);
                if (f != null)
                {
                    // DayOfWeek: domingo = 0 ... se convierte a 1 = lunes ... 7 = domingo
                    int dia = ((int)FechaOperacion.DayOfWeek + 6) % 7 + 1;
                    if (dia != f.DiaSemana)
                    {
                        yield return new ValidationResult(
                            "La fecha no corresponde al día de operación de la frecuencia seleccionada.",
                            new[] { "FechaOperacion" });
                    }
                }
            }
        }
    }

    public class VueloMetadata
    {
        [Required(ErrorMessage = "Seleccione la frecuencia.")]
        [Display(Name = "Frecuencia")]
        public int FrecuenciaId { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Fecha de operación")]
        public DateTime FechaOperacion { get; set; }

        [Display(Name = "Aeronave asignada")]
        public Nullable<int> AeronaveId { get; set; }

        [Required(ErrorMessage = "Seleccione el estado.")]
        [Display(Name = "Estado del vuelo")]
        public string EstadoVueloCodigo { get; set; }
    }

    // ------------------------- ITINERARIO -------------------------
    [MetadataType(typeof(ItinerarioMetadata))]
    public partial class Itinerario { }

    public class ItinerarioMetadata
    {
        [Required(ErrorMessage = "La referencia es obligatoria.")]
        [StringLength(30)]
        [Display(Name = "Referencia")]
        public string Referencia { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150)]
        [Display(Name = "Nombre del itinerario")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Seleccione el estado.")]
        [Display(Name = "Estado")]
        public string EstadoItinerarioCodigo { get; set; }
    }

    // ------------------------- TRAMO DE ITINERARIO -------------------------
    [MetadataType(typeof(TramoItinerarioMetadata))]
    public partial class TramoItinerario { }

    public class TramoItinerarioMetadata
    {
        [Required(ErrorMessage = "Seleccione el itinerario.")]
        [Display(Name = "Itinerario")]
        public int ItinerarioId { get; set; }

        [Required(ErrorMessage = "Seleccione el vuelo.")]
        [Display(Name = "Vuelo")]
        public int VueloId { get; set; }

        [Required(ErrorMessage = "El orden es obligatorio.")]
        [Range(1, 10, ErrorMessage = "El orden va de {1} a {2}.")]
        [Display(Name = "Orden del tramo")]
        public byte Orden { get; set; }
    }

    // ------------------------- PASAJERO -------------------------
    [MetadataType(typeof(PasajeroMetadata))]
    public partial class Pasajero : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Cédula ecuatoriana: algoritmo de módulo 10
            if (TipoDocumentoCodigo == "CEDULA" && !CedulaValida(NumeroDocumento))
            {
                yield return new ValidationResult("La cédula ecuatoriana no es válida.", new[] { "NumeroDocumento" });
            }

            if (FechaNacimiento.HasValue && FechaNacimiento.Value.Date > DateTime.Today)
            {
                yield return new ValidationResult("La fecha de nacimiento no puede ser futura.", new[] { "FechaNacimiento" });
            }
        }

        public static bool CedulaValida(string cedula)
        {
            if (string.IsNullOrEmpty(cedula) || cedula.Length != 10 || !cedula.All(char.IsDigit))
            {
                return false;
            }

            int provincia = int.Parse(cedula.Substring(0, 2));
            if (provincia < 1 || provincia > 24)
            {
                return false;
            }

            int suma = 0;
            for (int i = 0; i < 9; i++)
            {
                int digito = cedula[i] - '0';
                if (i % 2 == 0)
                {
                    digito = digito * 2;
                    if (digito > 9) digito = digito - 9;
                }
                suma = suma + digito;
            }

            int verificador = (10 - (suma % 10)) % 10;
            return verificador == (cedula[9] - '0');
        }
    }

    public class PasajeroMetadata
    {
        [Required(ErrorMessage = "Seleccione el tipo de documento.")]
        [Display(Name = "Tipo de documento")]
        public string TipoDocumentoCodigo { get; set; }

        [Required(ErrorMessage = "El número de documento es obligatorio.")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "Entre {2} y {1} caracteres.")]
        [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Solo letras y números.")]
        [Display(Name = "Número de documento")]
        public string NumeroDocumento { get; set; }

        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ ]+$", ErrorMessage = "Solo letras y espacios.")]
        [Display(Name = "Nombres")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ ]+$", ErrorMessage = "Solo letras y espacios.")]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Fecha de nacimiento (opcional)")]
        public Nullable<DateTime> FechaNacimiento { get; set; }

        [EmailAddress(ErrorMessage = "Formato de correo no válido.")]
        [StringLength(256)]
        [Display(Name = "Correo (opcional)")]
        public string Correo { get; set; }

        [RegularExpression(@"^[0-9+ ]{7,20}$", ErrorMessage = "Solo números, + y espacios (7 a 20).")]
        [Display(Name = "Teléfono (opcional)")]
        public string Telefono { get; set; }

        [Display(Name = "Fecha de registro")]
        public DateTime FechaRegistro { get; set; }
    }

    // ------------------------- CATÁLOGOS DE CONFIGURACIÓN -------------------------
    [MetadataType(typeof(MarcaTarjetaMetadata))]
    public partial class MarcaTarjeta { }

    public class MarcaTarjetaMetadata
    {
        [Required(ErrorMessage = "El código es obligatorio.")]
        [RegularExpression(@"^[A-Z_]{2,20}$", ErrorMessage = "MAYÚSCULAS y guion bajo (ej. VISA).")]
        [Display(Name = "Código")]
        public string Codigo { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(60)]
        [Display(Name = "Marca")]
        public string Nombre { get; set; }

        [Required]
        [Range(12, 19, ErrorMessage = "La longitud del número va de {1} a {2}.")]
        [Display(Name = "Longitud del número")]
        public byte LongitudNumero { get; set; }

        [Required]
        [Range(3, 4, ErrorMessage = "El código de seguridad tiene {1} o {2} dígitos.")]
        [Display(Name = "Longitud del código de seguridad")]
        public byte LongitudCodigoSeguridad { get; set; }

        [Required(ErrorMessage = "Los prefijos son obligatorios.")]
        [StringLength(40)]
        [Display(Name = "Prefijos de referencia")]
        public string PrefijosReferencia { get; set; }

        [Required(ErrorMessage = "La ruta del logo es obligatoria.")]
        [StringLength(150)]
        [Display(Name = "Ruta del logo")]
        public string RutaLogo { get; set; }

        [Display(Name = "Activa")]
        public bool Activa { get; set; }
    }

    [MetadataType(typeof(MetodoPagoMetadata))]
    public partial class MetodoPago { }

    public class MetodoPagoMetadata
    {
        [Required(ErrorMessage = "El código es obligatorio.")]
        [RegularExpression(@"^[A-Z_]{2,20}$", ErrorMessage = "MAYÚSCULAS y guion bajo.")]
        [Display(Name = "Código")]
        public string Codigo { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        [Display(Name = "Método de pago")]
        public string Nombre { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; }
    }

    [MetadataType(typeof(MonedaMetadata))]
    public partial class Moneda { }

    public class MonedaMetadata
    {
        [Required(ErrorMessage = "El código es obligatorio.")]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Código ISO 4217 de 3 letras (ej. USD).")]
        [Display(Name = "Código")]
        public string Codigo { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(100)]
        [Display(Name = "Moneda")]
        public string Descripcion { get; set; }
    }

    [MetadataType(typeof(TipoDocumentoIdentidadMetadata))]
    public partial class TipoDocumentoIdentidad { }

    public class TipoDocumentoIdentidadMetadata
    {
        [Required(ErrorMessage = "El código es obligatorio.")]
        [RegularExpression(@"^[A-Z_]{2,20}$", ErrorMessage = "MAYÚSCULAS y guion bajo.")]
        [Display(Name = "Código")]
        public string Codigo { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(100)]
        [Display(Name = "Tipo de documento")]
        public string Descripcion { get; set; }
    }

    [MetadataType(typeof(DatosEmisorMetadata))]
    public partial class DatosEmisor { }

    public class DatosEmisorMetadata
    {
        [Required]
        [Range(1, 1, ErrorMessage = "Solo existe un registro de emisor (Id = 1).")]
        [Display(Name = "Id")]
        public byte DatosEmisorId { get; set; }

        [Required(ErrorMessage = "El nombre comercial es obligatorio.")]
        [StringLength(150)]
        [Display(Name = "Nombre comercial")]
        public string NombreComercial { get; set; }

        [Required(ErrorMessage = "La identificación es obligatoria.")]
        [StringLength(60)]
        [Display(Name = "Identificación")]
        public string Identificacion { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        [StringLength(200)]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; }

        [RegularExpression(@"^[0-9+ ]{7,20}$", ErrorMessage = "Solo números, + y espacios.")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [EmailAddress(ErrorMessage = "Formato de correo no válido.")]
        [Display(Name = "Correo")]
        public string Correo { get; set; }

        [Required(ErrorMessage = "La leyenda es obligatoria.")]
        [StringLength(300)]
        [Display(Name = "Leyenda del comprobante")]
        public string LeyendaComprobante { get; set; }
    }
}