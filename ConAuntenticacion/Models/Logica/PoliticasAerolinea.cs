using System;

namespace ConAuntenticacion.Models
{
    // =====================================================================
    // POLÍTICAS PROPUESTAS POR EL GRUPO (apartado 18.1, DP-01 a DP-15)
    // Son SUPUESTOS DE IMPLEMENTACIÓN pendientes de aprobación del docente.
    // Por eso se centralizan aquí como valores parametrizables: si el docente
    // aprueba otros valores, solo se cambia esta clase.
    // =====================================================================
    public static class PoliticasAerolinea
    {
        // DP-04: moneda única de comercialización
        public const string Moneda = "USD";

        // DP-05: IVA aplicado sobre la tarifa de cada tramo (comprobante académico)
        public const decimal PorcentajeIva = 0.15m;

        // DP-07: tiempo de retención de una reserva PENDIENTE (asientos morados)
        public const int HorasRetencionReserva = 24;

        // DP-08: cancelación y reembolso según la anticipación a la salida del primer vuelo
        public const int HorasReembolsoTotal = 48;           // >= 48 h  -> 100 %
        public const int HorasReembolsoParcial = 24;         // 24-48 h  -> 50 %
        public const decimal PorcentajeReembolsoParcial = 0.50m;   // < 24 h -> no se permite cancelar

        // DP-12: ventana de check-in en línea
        public const int HorasAperturaCheckIn = 168;   // se abre 7 días (168 h) antes de la salida
        public const int HorasCierreCheckIn = 2;       // se cierra 2 h antes de la salida

        // DP-06: proveedor de pagos -> simulación académica.
        // Una tarjeta cuyo número termina en 0000 se RECHAZA para poder demostrar el error comercial.
        public const string SufijoTarjetaRechazada = "0000";

        // RN-03: capacidad máxima por vuelo
        public const int MaximoPasajeros = 7;

        public static decimal CalcularIva(decimal subtotal)
        {
            return Math.Round(subtotal * PorcentajeIva, 2);
        }

        public static decimal CalcularTotal(decimal subtotal)
        {
            return subtotal + CalcularIva(subtotal);
        }

        // Devuelve el porcentaje de reembolso (1 = 100 %), o -1 si ya no se permite cancelar
        public static decimal PorcentajeReembolso(double horasAntesDeLaSalida)
        {
            if (horasAntesDeLaSalida >= HorasReembolsoTotal)
            {
                return 1m;
            }
            if (horasAntesDeLaSalida >= HorasReembolsoParcial)
            {
                return PorcentajeReembolsoParcial;
            }
            return -1m;
        }
    }

    // Nombres de los roles creados en Global.asax.cs
    public static class RolesSistema
    {
        public const string SuperUsuario = "SuperUsuario";
        public const string Administrador = "Administrador";
        public const string Counter = "Counter";
        public const string Cliente = "Cliente";

        // Para usar en [Authorize(Roles = ...)]
        public const string Personal = "Counter,Administrador,SuperUsuario";
        public const string Administracion = "Administrador,SuperUsuario";
        public const string CompraPasajes = "Cliente,Counter";
    }

    // Error de una regla de negocio: el mensaje se muestra al usuario
    public class ReglaNegocioException : Exception
    {
        public ReglaNegocioException(string mensaje) : base(mensaje)
        {
        }
    }
}
