using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;

namespace ConAuntenticacion.Models
{
    // =====================================================================
    // LÓGICA DE NEGOCIO DE LA AEROLÍNEA (BACKEND)
    // Toda regla de negocio se programa aquí, no en las vistas ni en los
    // controladores. Todo es SÍNCRONO y sin bloqueos: cada operación se
    // guarda con un solo SaveChanges (atómico) y el servidor revalida la
    // disponibilidad antes de guardar (RNP-04).
    // =====================================================================
    public class ServicioAerolinea : IDisposable
    {
        private AerolineaVIPEntities db = new AerolineaVIPEntities();

        private const string SqlVueloDetalle =
            "SELECT VueloId, CodigoVuelo, FechaOperacion, RutaId, TarifaReferencial, Origen, CiudadOrigen, " +
            "Destino, CiudadDestino, HoraSalida, HoraLlegada, AeronaveId, EstadoVueloCodigo FROM dbo.vw_VueloDetalle ";

        public void Dispose()
        {
            db.Dispose();
        }

        // =================================================================
        // VUELOS (vistas vw_VueloDetalle, vw_EstadoAsientoVuelo)
        // =================================================================
        public List<VueloDetalleDto> VuelosPorFecha(DateTime desde, DateTime hasta)
        {
            return db.Database.SqlQuery<VueloDetalleDto>(
                SqlVueloDetalle + "WHERE FechaOperacion BETWEEN @p0 AND @p1 ORDER BY FechaOperacion, HoraSalida",
                desde.Date, hasta.Date).ToList();
        }

        public List<VueloDetalleDto> VuelosPorId(IEnumerable<int> ids)
        {
            List<int> lista = ids.Distinct().ToList();
            if (lista.Count == 0)
            {
                return new List<VueloDetalleDto>();
            }
            // Los Id son enteros, por eso se pueden concatenar sin riesgo de inyección
            return db.Database.SqlQuery<VueloDetalleDto>(
                SqlVueloDetalle + "WHERE VueloId IN (" + string.Join(",", lista) + ")").ToList();
        }

        // Agrega capacidad, aeronave y asientos disponibles a cada vuelo
        public List<ResultadoVueloViewModel> ConvertirResultados(List<VueloDetalleDto> vuelos)
        {
            List<int> ids = vuelos.Select(v => v.VueloId).ToList();

            // RNP-03: ocupan inventario las asignaciones PENDIENTE (morado) y CONFIRMADA (rojo)
            Dictionary<int, int> ocupados = db.ReservaPasajeroVuelo
                .Where(r => ids.Contains(r.VueloId)
                         && (r.EstadoAsignacionCodigo == "PENDIENTE" || r.EstadoAsignacionCodigo == "CONFIRMADA"))
                .GroupBy(r => r.VueloId)
                .Select(g => new { VueloId = g.Key, Total = g.Count() })
                .ToList()
                .ToDictionary(x => x.VueloId, x => x.Total);

            var aeronaves = db.Aeronave
                .Select(a => new { a.AeronaveId, a.Matricula, Capacidad = (int)a.ModeloAeronave.CapacidadAsientos })
                .ToList()
                .ToDictionary(a => a.AeronaveId);

            List<ResultadoVueloViewModel> lista = new List<ResultadoVueloViewModel>();
            foreach (VueloDetalleDto v in vuelos)
            {
                ResultadoVueloViewModel r = new ResultadoVueloViewModel();
                r.VueloId = v.VueloId;
                r.CodigoVuelo = v.CodigoVuelo;
                r.Origen = v.Origen;
                r.CiudadOrigen = v.CiudadOrigen;
                r.Destino = v.Destino;
                r.CiudadDestino = v.CiudadDestino;
                r.Fecha = v.FechaOperacion;
                r.HoraSalida = v.HoraSalida;
                r.HoraLlegada = v.HoraLlegada;
                r.Tarifa = v.TarifaReferencial;
                r.Estado = v.EstadoVueloCodigo;

                // RNP-01: sin aeronave asignada el vuelo no se comercializa
                r.TieneAeronave = v.AeronaveId.HasValue && aeronaves.ContainsKey(v.AeronaveId.Value);
                if (r.TieneAeronave)
                {
                    r.Matricula = aeronaves[v.AeronaveId.Value].Matricula;
                    r.Capacidad = aeronaves[v.AeronaveId.Value].Capacidad;
                }
                int ocup = ocupados.ContainsKey(v.VueloId) ? ocupados[v.VueloId] : 0;
                r.Disponibles = Math.Max(r.Capacidad - ocup, 0);
                lista.Add(r);
            }
            return lista;
        }

        public ResultadoVueloViewModel ObtenerVuelo(int vueloId)
        {
            return ConvertirResultados(VuelosPorId(new[] { vueloId })).FirstOrDefault();
        }

        // Pantalla 6: vuelos directos y con escala (RNP-05)
        public ResultadosBusquedaViewModel Buscar(BusquedaVuelosViewModel busqueda)
        {
            DateTime fecha = busqueda.Fecha.Value.Date;

            List<ResultadoVueloViewModel> vuelosDia = ConvertirResultados(VuelosPorFecha(fecha, fecha))
                .Where(v => v.Estado == "PROGRAMADO" || v.Estado == "CONFIRMADO")
                .ToList();

            ResultadosBusquedaViewModel resultado = new ResultadosBusquedaViewModel();
            resultado.Busqueda = busqueda;
            resultado.Directos = vuelosDia
                .Where(v => v.Origen == busqueda.Origen && v.Destino == busqueda.Destino)
                .OrderBy(v => v.HoraSalida)
                .ToList();

            // Conexión: el segundo tramo sale desde donde llega el primero y después de su llegada
            resultado.Conexiones = new List<ConexionViewModel>();
            foreach (ResultadoVueloViewModel t1 in vuelosDia.Where(v => v.Origen == busqueda.Origen && v.Destino != busqueda.Destino))
            {
                foreach (ResultadoVueloViewModel t2 in vuelosDia.Where(v => v.Origen == t1.Destino && v.Destino == busqueda.Destino && v.HoraSalida >= t1.HoraLlegada))
                {
                    resultado.Conexiones.Add(new ConexionViewModel { Tramo1 = t1, Tramo2 = t2 });
                }
            }
            return resultado;
        }

        // Pantallas 7 y 8: los 7 asientos con su color, leídos de vw_EstadoAsientoVuelo
        public List<AsientoViewModel> ObtenerAsientos(int vueloId)
        {
            return db.Database.SqlQuery<EstadoAsientoDto>(
                    "SELECT VueloId, CAST(NumeroAsiento AS INT) AS NumeroAsiento, CodigoAsiento, CAST(Fila AS INT) AS Fila, " +
                    "Lado, Ubicacion, EstadoVisual, ColorEstado FROM dbo.vw_EstadoAsientoVuelo WHERE VueloId = @p0 ORDER BY NumeroAsiento",
                    vueloId)
                .ToList()
                .Select(a => new AsientoViewModel
                {
                    NumeroAsiento = a.NumeroAsiento,
                    Codigo = a.CodigoAsiento,
                    Fila = a.Fila,
                    Lado = a.Lado,
                    Ubicacion = a.Ubicacion,
                    EstadoVisual = a.EstadoVisual,
                    ColorEstado = a.ColorEstado
                })
                .ToList();
        }

        // Pantalla 1: destinos con ruta activa desde un origen (directos o con una escala) y sus días de operación
        public List<DestinoViewModel> DestinosDesde(string origen)
        {
            Dictionary<int, Aeropuerto> aeropuertos = db.Aeropuerto.Where(a => a.Activo).ToList().ToDictionary(a => a.AeropuertoId);
            var rutas = db.Ruta
                .Where(r => r.Activa)
                .Select(r => new
                {
                    r.AeropuertoOrigenId,
                    r.AeropuertoDestinoId,
                    Dias = r.Frecuencia.Where(f => f.Activa).Select(f => f.DiaSemana)
                })
                .ToList()
                .Where(r => aeropuertos.ContainsKey(r.AeropuertoOrigenId) && aeropuertos.ContainsKey(r.AeropuertoDestinoId))
                .Select(r => new
                {
                    Origen = aeropuertos[r.AeropuertoOrigenId],
                    Destino = aeropuertos[r.AeropuertoDestinoId],
                    Dias = r.Dias.Select(d => (int)d).Distinct().ToList()
                })
                .ToList();

            List<DestinoViewModel> destinos = new List<DestinoViewModel>();

            // Rutas directas
            foreach (var r in rutas.Where(x => x.Origen.CodigoIATA == origen && x.Dias.Any()))
            {
                destinos.Add(new DestinoViewModel { Codigo = r.Destino.CodigoIATA, Ciudad = r.Destino.Localidad, Dias = r.Dias.OrderBy(d => d).ToList() });
            }

            // Con una escala, el mismo día (RN-06: Galápagos por Guayaquil)
            foreach (var r1 in rutas.Where(x => x.Origen.CodigoIATA == origen))
            {
                foreach (var r2 in rutas.Where(x => x.Origen.AeropuertoId == r1.Destino.AeropuertoId && x.Destino.CodigoIATA != origen))
                {
                    if (destinos.Any(d => d.Codigo == r2.Destino.CodigoIATA))
                    {
                        continue;
                    }
                    List<int> dias = r1.Dias.Intersect(r2.Dias).OrderBy(d => d).ToList();
                    if (dias.Any())
                    {
                        destinos.Add(new DestinoViewModel { Codigo = r2.Destino.CodigoIATA, Ciudad = r2.Destino.Localidad, Escala = r1.Destino.Localidad, Dias = dias });
                    }
                }
            }
            return destinos.OrderBy(d => d.Escala != null).ThenBy(d => d.Ciudad).ToList();
        }

        // =================================================================
        // RESERVAS (pantallas 8, 9, 10, 13, 14)
        // =================================================================

        // DP-07: las reservas PENDIENTES vencidas liberan sus asientos (morado -> verde)
        public void ExpirarReservasVencidas()
        {
            DateTime ahora = DateTime.Now;
            List<Reserva> vencidas = db.Reserva
                .Include(r => r.ReservaPasajeroVuelo)
                .Include(r => r.Compra)
                .Where(r => r.EstadoReservaCodigo == "PENDIENTE" && r.FechaExpiracion != null && r.FechaExpiracion < ahora)
                .ToList();

            if (vencidas.Count == 0)
            {
                return;
            }

            foreach (Reserva r in vencidas)
            {
                r.EstadoReservaCodigo = "EXPIRADA";
                foreach (ReservaPasajeroVuelo a in r.ReservaPasajeroVuelo.Where(x => x.EstadoAsignacionCodigo == "PENDIENTE"))
                {
                    a.EstadoAsignacionCodigo = "LIBERADA";
                }
                foreach (Compra c in r.Compra.Where(x => x.EstadoCompraCodigo == "PENDIENTE"))
                {
                    c.EstadoCompraCodigo = "ANULADA";
                }
            }
            db.SaveChanges();
        }

        // RNP-01 / RNP-04: el vuelo debe venderse y los asientos seguir disponibles
        public string ValidarAsientos(int vueloId, List<int> asientos)
        {
            ResultadoVueloViewModel vuelo = ObtenerVuelo(vueloId);
            if (vuelo == null)
            {
                return "El vuelo seleccionado no existe.";
            }
            if (!vuelo.TieneAeronave)
            {
                return "El vuelo no tiene una aeronave asignada; sus asientos no pueden reservarse (RNP-01).";
            }
            if (vuelo.Estado != "PROGRAMADO" && vuelo.Estado != "CONFIRMADO")
            {
                return "El vuelo no se encuentra en un estado que permita reservas.";
            }
            if (vuelo.Salida <= DateTime.Now)
            {
                return "El vuelo ya salió; no admite reservas.";
            }

            List<AsientoViewModel> mapa = ObtenerAsientos(vueloId);
            foreach (int numero in asientos)
            {
                AsientoViewModel asiento = mapa.FirstOrDefault(a => a.NumeroAsiento == numero);
                if (asiento == null)
                {
                    return "El asiento seleccionado no existe en la distribución de la aeronave.";
                }
                if (asiento.EstadoVisual != "DISPONIBLE")
                {
                    return "El asiento " + asiento.Codigo + " ya no está disponible. Actualice el mapa y elija otro.";
                }
            }
            return null;
        }

        // Un pasajero no puede ocupar dos asientos vigentes en el mismo vuelo
        public string ValidarPasajerosEnVuelo(int vueloId, List<PasajeroReservaViewModel> pasajeros)
        {
            foreach (PasajeroReservaViewModel p in pasajeros)
            {
                if (string.IsNullOrEmpty(p.TipoDocumentoCodigo) || string.IsNullOrEmpty(p.NumeroDocumento))
                {
                    continue;
                }
                string tipo = p.TipoDocumentoCodigo;
                string numero = p.NumeroDocumento.Trim().ToUpper();
                bool yaViaja = db.ReservaPasajeroVuelo.Any(r => r.VueloId == vueloId
                    && (r.EstadoAsignacionCodigo == "PENDIENTE" || r.EstadoAsignacionCodigo == "CONFIRMADA")
                    && r.Pasajero.TipoDocumentoCodigo == tipo
                    && r.Pasajero.NumeroDocumento == numero);
                if (yaViaja)
                {
                    return "El pasajero con documento " + numero + " ya tiene un asiento en este vuelo.";
                }
            }
            return null;
        }

        // Pantalla 10: registra la reserva PENDIENTE, sus asientos (morados) y la compra PENDIENTE
        public int CrearReserva(ReservaEnCurso enCurso, string usuarioId)
        {
            // RNP-04: se revalida justo antes de guardar
            string error = ValidarAsientos(enCurso.VueloId, enCurso.Asientos);
            if (error == null)
            {
                error = ValidarPasajerosEnVuelo(enCurso.VueloId, enCurso.Pasajeros);
            }
            if (error != null)
            {
                throw new ReglaNegocioException(error);
            }

            ResultadoVueloViewModel vuelo = ObtenerVuelo(enCurso.VueloId);
            DateTime ahora = DateTime.Now;

            Reserva reserva = new Reserva();
            reserva.UsuarioId = usuarioId;
            reserva.FechaCreacion = ahora;
            reserva.EstadoReservaCodigo = "PENDIENTE";
            // DP-07: retención de 24 h, sin superar la hora de salida del vuelo
            reserva.FechaExpiracion = ahora.AddHours(PoliticasAerolinea.HorasRetencionReserva);
            if (reserva.FechaExpiracion > vuelo.Salida)
            {
                reserva.FechaExpiracion = vuelo.Salida;
            }

            decimal subtotal = 0;
            foreach (PasajeroReservaViewModel p in enCurso.Pasajeros)
            {
                string tipo = p.TipoDocumentoCodigo;
                string numero = p.NumeroDocumento.Trim().ToUpper();

                // Si el pasajero ya existe (mismo documento) se reutiliza; si no, se registra
                Pasajero pasajero = db.Pasajero.Local.FirstOrDefault(x => x.TipoDocumentoCodigo == tipo && x.NumeroDocumento == numero)
                                 ?? db.Pasajero.FirstOrDefault(x => x.TipoDocumentoCodigo == tipo && x.NumeroDocumento == numero);
                if (pasajero == null)
                {
                    pasajero = new Pasajero();
                    pasajero.TipoDocumentoCodigo = tipo;
                    pasajero.NumeroDocumento = numero;
                    pasajero.Nombres = p.Nombres.Trim();
                    pasajero.Apellidos = p.Apellidos.Trim();
                    pasajero.FechaNacimiento = p.FechaNacimiento;
                    pasajero.Correo = string.IsNullOrWhiteSpace(p.Correo) ? null : p.Correo.Trim();
                    pasajero.Telefono = string.IsNullOrWhiteSpace(p.Telefono) ? null : p.Telefono.Trim();
                    pasajero.FechaRegistro = ahora;
                    db.Pasajero.Add(pasajero);
                }

                ReservaPasajeroVuelo asignacion = new ReservaPasajeroVuelo();
                asignacion.Pasajero = pasajero;
                asignacion.VueloId = enCurso.VueloId;
                asignacion.NumeroAsiento = (byte)p.NumeroAsiento;
                asignacion.ImporteTramo = vuelo.Tarifa;          // DP-04: tarifa vigente al reservar
                asignacion.EstadoAsignacionCodigo = "PENDIENTE";  // morado
                asignacion.FechaAsignacion = ahora;
                reserva.ReservaPasajeroVuelo.Add(asignacion);

                subtotal = subtotal + vuelo.Tarifa;
            }

            // RNP-06: la compra se registra aparte, PENDIENTE de pago; ClaveIdempotencia evita duplicados (RNP-10)
            Compra compra = new Compra();
            compra.FechaCompra = ahora;
            compra.Importe = PoliticasAerolinea.CalcularTotal(subtotal);
            compra.MonedaCodigo = PoliticasAerolinea.Moneda;
            compra.EstadoCompraCodigo = "PENDIENTE";
            compra.ClaveIdempotencia = Guid.NewGuid();
            reserva.Compra.Add(compra);

            db.Reserva.Add(reserva);
            Guardar("No se pudo registrar la reserva");
            return reserva.ReservaId;
        }

        public bool PuedeAcceder(int reservaId, string usuarioId, bool esPersonal)
        {
            if (esPersonal)
            {
                return db.Reserva.Any(r => r.ReservaId == reservaId);
            }
            return db.Reserva.Any(r => r.ReservaId == reservaId && r.UsuarioId == usuarioId);
        }

        private IQueryable<Reserva> ConsultaReservas()
        {
            return db.Reserva
                .Include(r => r.ReservaPasajeroVuelo.Select(a => a.Pasajero))
                .Include(r => r.ReservaPasajeroVuelo.Select(a => a.CheckIn))
                .Include(r => r.Compra.Select(c => c.Pago))
                .Include(r => r.Compra.Select(c => c.Comprobante.Select(x => x.TipoComprobante)));
        }

        public ReservaResumenViewModel ObtenerResumen(int reservaId)
        {
            Reserva reserva = ConsultaReservas().FirstOrDefault(r => r.ReservaId == reservaId);
            if (reserva == null)
            {
                return null;
            }
            return CrearResumenes(new List<Reserva> { reserva }, ServicioUsuarios.NombresUsuarios()).First();
        }

        public List<ReservaResumenViewModel> ReservasDeUsuario(string usuarioId)
        {
            List<Reserva> reservas = ConsultaReservas()
                .Where(r => r.UsuarioId == usuarioId)
                .OrderByDescending(r => r.FechaCreacion)
                .ToList();
            return CrearResumenes(reservas, ServicioUsuarios.NombresUsuarios());
        }

        // Pantalla 20: búsqueda de reservas por código o por usuario
        public List<ReservaResumenViewModel> BuscarReservas(string texto, string estado)
        {
            Dictionary<string, string> usuarios = ServicioUsuarios.NombresUsuarios();
            IQueryable<Reserva> consulta = ConsultaReservas();

            if (!string.IsNullOrWhiteSpace(texto))
            {
                string t = texto.Trim().ToUpper();
                List<string> idsUsuarios = usuarios.Where(u => u.Value.ToUpper().Contains(t)).Select(u => u.Key).ToList();
                consulta = consulta.Where(r => r.CodigoReserva.Contains(t) || idsUsuarios.Contains(r.UsuarioId));
            }
            if (!string.IsNullOrEmpty(estado))
            {
                consulta = consulta.Where(r => r.EstadoReservaCodigo == estado);
            }

            List<Reserva> reservas = consulta.OrderByDescending(r => r.FechaCreacion).Take(100).ToList();
            return CrearResumenes(reservas, usuarios);
        }

        // Arma el resumen de varias reservas con una sola consulta de vuelos
        private List<ReservaResumenViewModel> CrearResumenes(List<Reserva> reservas, Dictionary<string, string> usuarios)
        {
            List<int> idsVuelos = reservas.SelectMany(r => r.ReservaPasajeroVuelo).Select(a => a.VueloId).Distinct().ToList();
            Dictionary<int, VueloDetalleDto> vuelos = VuelosPorId(idsVuelos).ToDictionary(v => v.VueloId);

            // Modelo de aeronave de cada vuelo (para el código del asiento: 1D, 2I, ...)
            Dictionary<int, Nullable<int>> modelos = db.Vuelo
                .Where(v => idsVuelos.Contains(v.VueloId))
                .Select(v => new { v.VueloId, Modelo = (Nullable<int>)v.Aeronave.ModeloAeronaveId })
                .ToList()
                .ToDictionary(v => v.VueloId, v => v.Modelo);
            List<Asiento> asientos = db.Asiento.ToList();

            List<ReservaResumenViewModel> lista = new List<ReservaResumenViewModel>();
            foreach (Reserva r in reservas)
            {
                ReservaResumenViewModel m = new ReservaResumenViewModel();
                m.ReservaId = r.ReservaId;
                m.CodigoReserva = r.CodigoReserva;
                m.UsuarioId = r.UsuarioId;
                m.Usuario = usuarios != null && usuarios.ContainsKey(r.UsuarioId) ? usuarios[r.UsuarioId] : "Usuario de demostración";
                m.FechaCreacion = r.FechaCreacion;
                m.FechaExpiracion = r.FechaExpiracion;
                m.EstadoReserva = r.EstadoReservaCodigo;

                m.Detalle = new List<DetalleAsignacionViewModel>();
                foreach (ReservaPasajeroVuelo a in r.ReservaPasajeroVuelo.OrderBy(x => x.VueloId).ThenBy(x => x.NumeroAsiento))
                {
                    DetalleAsignacionViewModel d = new DetalleAsignacionViewModel();
                    d.AsignacionId = a.AsignacionId;
                    d.Pasajero = a.Pasajero.Nombres + " " + a.Pasajero.Apellidos;
                    d.Documento = a.Pasajero.TipoDocumentoCodigo + " " + a.Pasajero.NumeroDocumento;
                    d.VueloId = a.VueloId;
                    d.Importe = a.ImporteTramo;
                    d.EstadoAsignacion = a.EstadoAsignacionCodigo;
                    d.CheckInRealizado = a.CheckIn.Any(c => c.EstadoCheckInCodigo == "COMPLETADO");

                    if (vuelos.ContainsKey(a.VueloId))
                    {
                        VueloDetalleDto v = vuelos[a.VueloId];
                        d.CodigoVuelo = v.CodigoVuelo;
                        d.Ruta = v.CiudadOrigen + " (" + v.Origen + ") → " + v.CiudadDestino + " (" + v.Destino + ")";
                        d.Fecha = v.FechaOperacion;
                        d.HoraSalida = v.HoraSalida;
                        d.HoraLlegada = v.HoraLlegada;
                    }

                    Nullable<int> modelo = modelos.ContainsKey(a.VueloId) ? modelos[a.VueloId] : null;
                    Asiento s = asientos.FirstOrDefault(x => x.ModeloAeronaveId == modelo && x.NumeroAsiento == a.NumeroAsiento)
                             ?? asientos.FirstOrDefault(x => x.NumeroAsiento == a.NumeroAsiento);
                    d.Asiento = s != null ? s.Codigo : a.NumeroAsiento.ToString();
                    d.Ubicacion = s != null ? s.Ubicacion : "";
                    m.Detalle.Add(d);
                }

                m.Subtotal = m.Detalle.Sum(d => d.Importe);

                Compra compra = r.Compra.OrderByDescending(c => c.CompraId).FirstOrDefault();
                if (compra != null)
                {
                    m.CompraId = compra.CompraId;
                    m.EstadoCompra = compra.EstadoCompraCodigo;
                    m.Total = compra.Importe;

                    Pago aprobado = compra.Pago.Where(p => p.EstadoPagoCodigo == "APROBADO").OrderByDescending(p => p.PagoId).FirstOrDefault();
                    Pago ultimo = compra.Pago.OrderByDescending(p => p.PagoId).FirstOrDefault();
                    Pago mostrar = (compra.EstadoCompraCodigo == "ANULADA" ? ultimo : (aprobado ?? ultimo));
                    if (mostrar != null)
                    {
                        m.EstadoPago = mostrar.EstadoPagoCodigo;
                        m.MensajePago = mostrar.MensajeResultado;
                        m.TarjetaPago = mostrar.MarcaTarjetaCodigo + " **** " + mostrar.UltimosCuatroDigitos;
                    }

                    m.Comprobantes = compra.Comprobante.OrderBy(c => c.FechaEmision).Select(c => new ComprobanteItemViewModel
                    {
                        ComprobanteId = c.ComprobanteId,
                        Numero = c.NumeroComprobante,
                        TipoCodigo = c.TipoComprobanteCodigo,
                        Tipo = c.TipoComprobante != null ? c.TipoComprobante.Descripcion : c.TipoComprobanteCodigo,
                        FechaEmision = c.FechaEmision,
                        Estado = c.EstadoComprobanteCodigo,
                        CodigoReserva = r.CodigoReserva,
                        Total = compra.Importe
                    }).ToList();
                }
                else
                {
                    m.Total = PoliticasAerolinea.CalcularTotal(m.Subtotal);
                    m.Comprobantes = new List<ComprobanteItemViewModel>();
                }
                m.Iva = m.Total - m.Subtotal;

                // DP-08: condiciones de cancelación
                m.MotivoNoCancelar = EvaluarCancelacion(r, m.PrimeraSalida);
                m.PuedeCancelar = m.MotivoNoCancelar == null;
                if (m.PuedeCancelar && m.EstadoCompra == "PAGADA" && m.PrimeraSalida.HasValue)
                {
                    m.PorcentajeReembolso = PoliticasAerolinea.PorcentajeReembolso((m.PrimeraSalida.Value - DateTime.Now).TotalHours);
                    m.ImporteReembolso = Math.Round(m.Total * m.PorcentajeReembolso, 2);
                }
                lista.Add(m);
            }
            return lista;
        }

        // =================================================================
        // CANCELACIÓN (pantalla 14) - DP-08 y RNP-07
        // =================================================================
        public string EvaluarCancelacion(int reservaId)
        {
            Reserva r = ConsultaReservas().FirstOrDefault(x => x.ReservaId == reservaId);
            if (r == null)
            {
                return "La reserva no existe.";
            }
            List<VueloDetalleDto> vuelos = VuelosPorId(r.ReservaPasajeroVuelo.Select(a => a.VueloId));
            Nullable<DateTime> primeraSalida = vuelos.Any()
                ? vuelos.Min(v => v.FechaOperacion.Date.Add(v.HoraSalida))
                : (Nullable<DateTime>)null;
            return EvaluarCancelacion(r, primeraSalida);
        }

        private string EvaluarCancelacion(Reserva r, Nullable<DateTime> primeraSalida)
        {
            if (r.EstadoReservaCodigo == "CANCELADA" || r.EstadoReservaCodigo == "EXPIRADA")
            {
                return "La reserva ya está " + r.EstadoReservaCodigo.ToLower() + ".";
            }
            if (r.ReservaPasajeroVuelo.Any(a => a.CheckIn.Any(c => c.EstadoCheckInCodigo == "COMPLETADO")))
            {
                return "Ya se realizó el check-in de un pasajero: la reserva no puede cancelarse en línea.";
            }
            if (primeraSalida.HasValue && primeraSalida.Value <= DateTime.Now)
            {
                return "El vuelo ya salió: la reserva no puede cancelarse.";
            }
            if (r.EstadoReservaCodigo == "CONFIRMADA" && primeraSalida.HasValue)
            {
                double horas = (primeraSalida.Value - DateTime.Now).TotalHours;
                if (PoliticasAerolinea.PorcentajeReembolso(horas) < 0)
                {
                    return "Faltan menos de " + PoliticasAerolinea.HorasReembolsoParcial +
                           " horas para la salida: según la política de cancelación ya no se permite cancelar.";
                }
            }
            return null;
        }

        public string CancelarReserva(int reservaId)
        {
            string error = EvaluarCancelacion(reservaId);
            if (error != null)
            {
                throw new ReglaNegocioException(error);
            }

            ReservaResumenViewModel resumen = ObtenerResumen(reservaId);
            Reserva r = ConsultaReservas().First(x => x.ReservaId == reservaId);
            DateTime ahora = DateTime.Now;
            string mensaje = "La reserva " + r.CodigoReserva + " fue cancelada y sus asientos quedaron disponibles.";

            Compra compra = r.Compra.Where(c => c.EstadoCompraCodigo == "PENDIENTE" || c.EstadoCompraCodigo == "PAGADA")
                                    .OrderByDescending(c => c.CompraId).FirstOrDefault();
            bool estabaPagada = compra != null && compra.EstadoCompraCodigo == "PAGADA";

            // RNP-07: la cancelación libera el inventario (asientos vuelven a verde)
            r.EstadoReservaCodigo = "CANCELADA";
            foreach (ReservaPasajeroVuelo a in r.ReservaPasajeroVuelo.Where(x => x.EstadoAsignacionCodigo == "PENDIENTE" || x.EstadoAsignacionCodigo == "CONFIRMADA"))
            {
                a.EstadoAsignacionCodigo = "CANCELADA";
            }

            if (compra != null)
            {
                compra.EstadoCompraCodigo = "ANULADA";

                foreach (Comprobante c in compra.Comprobante.Where(x => x.TipoComprobanteCodigo == "CONFIRMACION"))
                {
                    c.EstadoComprobanteCodigo = "ANULADO";
                }

                if (estabaPagada && resumen.ImporteReembolso > 0)
                {
                    // DP-08: reembolso simulado (100 % o 50 %) registrado como pago REVERSADO
                    Pago original = compra.Pago.Where(p => p.EstadoPagoCodigo == "APROBADO").OrderByDescending(p => p.PagoId).FirstOrDefault();
                    if (original != null)
                    {
                        Pago reembolso = new Pago();
                        reembolso.FechaPago = ahora;
                        reembolso.Importe = resumen.ImporteReembolso;
                        reembolso.EstadoPagoCodigo = "REVERSADO";
                        reembolso.MetodoPagoCodigo = original.MetodoPagoCodigo;
                        reembolso.MarcaTarjetaCodigo = original.MarcaTarjetaCodigo;
                        reembolso.UltimosCuatroDigitos = original.UltimosCuatroDigitos;
                        reembolso.NombreTitular = original.NombreTitular;
                        reembolso.CodigoOperacion = "REV-" + r.CodigoReserva + "-" + ahora.ToString("yyyyMMddHHmmssfff");
                        reembolso.EsSimulado = true;
                        reembolso.MensajeResultado = "Reembolso simulado del " + (resumen.PorcentajeReembolso * 100).ToString("0") + " % por cancelación (política DP-08).";
                        compra.Pago.Add(reembolso);
                    }

                    Comprobante nota = new Comprobante();
                    nota.TipoComprobanteCodigo = "NOTA_CREDITO";
                    nota.NumeroComprobante = SiguienteNumero("NOTA_CREDITO", "NC-ACAD-");
                    nota.FechaEmision = ahora;
                    nota.EstadoComprobanteCodigo = "EMITIDO";
                    compra.Comprobante.Add(nota);

                    mensaje = mensaje + " Reembolso simulado: " + resumen.ImporteReembolso.ToString("N2") + " " +
                              PoliticasAerolinea.Moneda + " (" + (resumen.PorcentajeReembolso * 100).ToString("0") + " %). Se emitió una nota de crédito académica.";
                }
            }

            Guardar("No se pudo cancelar la reserva");
            return mensaje;
        }

        // =================================================================
        // PAGO SIMULADO (pantallas 11 y 12) - DP-06, RNP-06, RNP-10
        // =================================================================
        public ResultadoPagoViewModel ProcesarPago(PagoViewModel datos)
        {
            ExpirarReservasVencidas();

            Reserva r = ConsultaReservas().FirstOrDefault(x => x.ReservaId == datos.ReservaId);
            if (r == null)
            {
                throw new ReglaNegocioException("La reserva no existe.");
            }

            Compra compra = r.Compra.Where(c => c.EstadoCompraCodigo == "PENDIENTE" || c.EstadoCompraCodigo == "PAGADA")
                                    .OrderByDescending(c => c.CompraId).FirstOrDefault();

            // RNP-10: si la solicitud se repite y ya está pagada, no se cobra de nuevo
            if (compra != null && compra.EstadoCompraCodigo == "PAGADA")
            {
                return new ResultadoPagoViewModel { Aprobado = true, Mensaje = "La compra ya estaba pagada; no se registró un nuevo cobro." };
            }
            if (compra == null || r.EstadoReservaCodigo != "PENDIENTE")
            {
                throw new ReglaNegocioException("La reserva no está pendiente de pago (puede haber expirado o haber sido cancelada).");
            }

            DateTime ahora = DateTime.Now;
            string numero = datos.NumeroTarjeta.Trim();
            bool aprobado = !numero.EndsWith(PoliticasAerolinea.SufijoTarjetaRechazada);

            // Solo se guardan la marca, los últimos 4 dígitos y el titular (nunca el número completo ni el CVV)
            Pago pago = new Pago();
            pago.FechaPago = ahora;
            pago.Importe = compra.Importe;
            pago.EstadoPagoCodigo = aprobado ? "APROBADO" : "RECHAZADO";
            pago.MetodoPagoCodigo = datos.MetodoPagoCodigo;
            pago.MarcaTarjetaCodigo = datos.MarcaTarjetaCodigo;
            pago.UltimosCuatroDigitos = numero.Substring(numero.Length - 4);
            pago.NombreTitular = datos.NombreTitular.Trim().ToUpper();
            pago.CodigoOperacion = "SIM-" + r.CodigoReserva + "-" + ahora.ToString("yyyyMMddHHmmssfff");
            pago.EsSimulado = true;
            pago.MensajeResultado = aprobado
                ? "Pago simulado aprobado."
                : "Pago simulado rechazado por el emisor (tarjeta de prueba terminada en " + PoliticasAerolinea.SufijoTarjetaRechazada + ").";
            compra.Pago.Add(pago);

            if (aprobado)
            {
                // Compra PAGADA -> reserva CONFIRMADA -> asientos OCUPADOS (rojo)
                compra.EstadoCompraCodigo = "PAGADA";
                r.EstadoReservaCodigo = "CONFIRMADA";
                foreach (ReservaPasajeroVuelo a in r.ReservaPasajeroVuelo.Where(x => x.EstadoAsignacionCodigo == "PENDIENTE"))
                {
                    a.EstadoAsignacionCodigo = "CONFIRMADA";
                }

                // Comprobantes académicos (no tributarios): confirmación y comprobante de venta
                Comprobante confirmacion = new Comprobante();
                confirmacion.TipoComprobanteCodigo = "CONFIRMACION";
                confirmacion.NumeroComprobante = SiguienteNumero("CONFIRMACION", "CNF-");
                confirmacion.FechaEmision = ahora;
                confirmacion.EstadoComprobanteCodigo = "EMITIDO";
                compra.Comprobante.Add(confirmacion);

                Comprobante venta = new Comprobante();
                venta.TipoComprobanteCodigo = "FACTURA";
                venta.NumeroComprobante = SiguienteNumero("FACTURA", "ACAD-");
                venta.FechaEmision = ahora;
                venta.EstadoComprobanteCodigo = "EMITIDO";
                compra.Comprobante.Add(venta);
            }

            Guardar("No se pudo registrar el pago simulado");
            return new ResultadoPagoViewModel { Aprobado = aprobado, Mensaje = pago.MensajeResultado };
        }

        private string SiguienteNumero(string tipo, string prefijo)
        {
            int n = db.Comprobante.Count(c => c.TipoComprobanteCodigo == tipo) + 1;
            string numero = prefijo + n.ToString("000000");
            while (db.Comprobante.Any(c => c.TipoComprobanteCodigo == tipo && c.NumeroComprobante == numero))
            {
                n++;
                numero = prefijo + n.ToString("000000");
            }
            return numero;
        }

        // =================================================================
        // CHECK-IN (pantalla 15) - DP-12
        // =================================================================
        public string EvaluarCheckIn(int asignacionId, string usuarioId, bool esPersonal)
        {
            ReservaPasajeroVuelo a = db.ReservaPasajeroVuelo
                .Include(x => x.Reserva)
                .Include(x => x.CheckIn)
                .FirstOrDefault(x => x.AsignacionId == asignacionId);

            if (a == null)
            {
                return "El asiento indicado no existe.";
            }
            if (!esPersonal && a.Reserva.UsuarioId != usuarioId)
            {
                return "La reserva no pertenece a su cuenta.";
            }
            if (a.EstadoAsignacionCodigo != "CONFIRMADA")
            {
                return "Solo se realiza check-in de asientos confirmados (compra pagada).";
            }
            if (a.CheckIn.Any(c => c.EstadoCheckInCodigo == "COMPLETADO"))
            {
                return "El check-in de este pasajero ya fue realizado.";
            }

            ResultadoVueloViewModel vuelo = ObtenerVuelo(a.VueloId);
            if (vuelo == null || vuelo.Estado == "CANCELADO")
            {
                return "El vuelo fue cancelado.";
            }
            double horas = (vuelo.Salida - DateTime.Now).TotalHours;
            if (horas > PoliticasAerolinea.HorasAperturaCheckIn)
            {
                return "El check-in se abre " + PoliticasAerolinea.HorasAperturaCheckIn + " horas antes de la salida: desde el " +
                       vuelo.Salida.AddHours(-PoliticasAerolinea.HorasAperturaCheckIn).ToString("dd/MM/yyyy HH:mm") + ".";
            }
            if (horas < PoliticasAerolinea.HorasCierreCheckIn)
            {
                return "El check-in en línea se cerró " + PoliticasAerolinea.HorasCierreCheckIn + " horas antes de la salida.";
            }
            return null;
        }

        public int RegistrarCheckIn(int asignacionId)
        {
            CheckIn checkIn = new CheckIn();
            checkIn.AsignacionId = asignacionId;
            checkIn.FechaRegistro = DateTime.Now;
            checkIn.EstadoCheckInCodigo = "COMPLETADO";
            db.CheckIn.Add(checkIn);
            Guardar("No se pudo registrar el check-in");
            return checkIn.CheckInId;
        }

        public List<CheckInElegibleViewModel> ListarCheckIn(string usuarioId, bool esPersonal, string codigoReserva)
        {
            IQueryable<ReservaPasajeroVuelo> consulta = db.ReservaPasajeroVuelo
                .Include(a => a.Pasajero)
                .Include(a => a.Reserva)
                .Include(a => a.CheckIn)
                .Where(a => a.EstadoAsignacionCodigo == "CONFIRMADA");

            if (!string.IsNullOrWhiteSpace(codigoReserva))
            {
                string codigo = codigoReserva.Trim().ToUpper();
                consulta = consulta.Where(a => a.Reserva.CodigoReserva == codigo);
                if (!esPersonal)
                {
                    consulta = consulta.Where(a => a.Reserva.UsuarioId == usuarioId);
                }
            }
            else if (!esPersonal)
            {
                consulta = consulta.Where(a => a.Reserva.UsuarioId == usuarioId);
            }
            else
            {
                // El personal busca por código de reserva
                return new List<CheckInElegibleViewModel>();
            }

            List<ReservaPasajeroVuelo> asignaciones = consulta.ToList();
            Dictionary<int, ResultadoVueloViewModel> vuelos = ConvertirResultados(VuelosPorId(asignaciones.Select(a => a.VueloId)))
                .ToDictionary(v => v.VueloId);
            List<Asiento> asientos = db.Asiento.ToList();
            DateTime ahora = DateTime.Now;

            List<CheckInElegibleViewModel> lista = new List<CheckInElegibleViewModel>();
            foreach (ReservaPasajeroVuelo a in asignaciones)
            {
                if (!vuelos.ContainsKey(a.VueloId))
                {
                    continue;
                }
                ResultadoVueloViewModel v = vuelos[a.VueloId];
                if (v.Salida < ahora.AddDays(-1))
                {
                    continue;   // solo vuelos próximos o del día
                }

                CheckIn hecho = a.CheckIn.FirstOrDefault(c => c.EstadoCheckInCodigo == "COMPLETADO");
                double horas = (v.Salida - ahora).TotalHours;
                Asiento s = asientos.FirstOrDefault(x => x.NumeroAsiento == a.NumeroAsiento);

                CheckInElegibleViewModel e = new CheckInElegibleViewModel();
                e.AsignacionId = a.AsignacionId;
                e.CodigoReserva = a.Reserva.CodigoReserva;
                e.Pasajero = a.Pasajero.Nombres + " " + a.Pasajero.Apellidos;
                e.Documento = a.Pasajero.TipoDocumentoCodigo + " " + a.Pasajero.NumeroDocumento;
                e.CodigoVuelo = v.CodigoVuelo;
                e.Ruta = v.CiudadOrigen + " → " + v.CiudadDestino;
                e.Salida = v.Salida;
                e.Asiento = s != null ? s.Codigo : a.NumeroAsiento.ToString();

                if (hecho != null)
                {
                    e.Estado = "Realizado";
                    e.CheckInId = hecho.CheckInId;
                }
                else if (v.Estado == "CANCELADO")
                {
                    e.Estado = "Vuelo cancelado";
                }
                else if (horas > PoliticasAerolinea.HorasAperturaCheckIn)
                {
                    e.Estado = "Aún no abre (desde " + v.Salida.AddHours(-PoliticasAerolinea.HorasAperturaCheckIn).ToString("dd/MM HH:mm") + ")";
                }
                else if (horas < PoliticasAerolinea.HorasCierreCheckIn)
                {
                    e.Estado = "Cerrado";
                }
                else
                {
                    e.Estado = "Disponible";
                    e.PuedeHacerCheckIn = true;
                }
                lista.Add(e);
            }
            return lista.OrderBy(x => x.Salida).ThenBy(x => x.Asiento).ToList();
        }

        public CheckInElegibleViewModel ObtenerPaseAbordar(int checkInId)
        {
            CheckIn c = db.CheckIn.Include(x => x.ReservaPasajeroVuelo.Reserva).FirstOrDefault(x => x.CheckInId == checkInId);
            if (c == null)
            {
                return null;
            }
            ReservaPasajeroVuelo a = c.ReservaPasajeroVuelo;
            return ListarCheckIn(a.Reserva.UsuarioId, false, a.Reserva.CodigoReserva)
                .FirstOrDefault(x => x.AsignacionId == a.AsignacionId);
        }

        public string UsuarioDeCheckIn(int checkInId)
        {
            return db.CheckIn.Where(c => c.CheckInId == checkInId).Select(c => c.ReservaPasajeroVuelo.Reserva.UsuarioId).FirstOrDefault();
        }

        // =================================================================
        // COMPROBANTES (pantalla 16)
        // =================================================================
        public List<ComprobanteItemViewModel> ListarComprobantes(string usuarioId, bool esPersonal, string buscar)
        {
            IQueryable<Comprobante> consulta = db.Comprobante
                .Include(c => c.Compra.Reserva)
                .Include(c => c.TipoComprobante);

            if (!esPersonal)
            {
                consulta = consulta.Where(c => c.Compra.Reserva.UsuarioId == usuarioId);
            }
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                string b = buscar.Trim().ToUpper();
                consulta = consulta.Where(c => c.Compra.Reserva.CodigoReserva.Contains(b) || c.NumeroComprobante.Contains(b));
            }

            return consulta.OrderByDescending(c => c.FechaEmision).Take(200).ToList().Select(c => new ComprobanteItemViewModel
            {
                ComprobanteId = c.ComprobanteId,
                Numero = c.NumeroComprobante,
                TipoCodigo = c.TipoComprobanteCodigo,
                Tipo = c.TipoComprobante.Descripcion,
                FechaEmision = c.FechaEmision,
                Estado = c.EstadoComprobanteCodigo,
                CodigoReserva = c.Compra.Reserva.CodigoReserva,
                Total = c.Compra.Importe
            }).ToList();
        }

        public bool PuedeVerComprobante(int comprobanteId, string usuarioId, bool esPersonal)
        {
            if (esPersonal)
            {
                return db.Comprobante.Any(c => c.ComprobanteId == comprobanteId);
            }
            return db.Comprobante.Any(c => c.ComprobanteId == comprobanteId && c.Compra.Reserva.UsuarioId == usuarioId);
        }

        public ComprobanteViewModel ObtenerComprobante(int comprobanteId)
        {
            ComprobanteDetalleDto encabezado = db.Database.SqlQuery<ComprobanteDetalleDto>(
                "SELECT ComprobanteId, NumeroComprobante, TipoComprobanteCodigo, TipoComprobante, FechaEmision, EstadoComprobanteCodigo, " +
                "EmisorNombre, EmisorIdentificacion, EmisorDireccion, LeyendaComprobante, ReservaId, CodigoReserva, UsuarioId, " +
                "CompraId, FechaCompra, TotalOperacion, MonedaCodigo, EstadoCompraCodigo, PagoId, CodigoOperacion, FechaPago, " +
                "EstadoPagoCodigo, MetodoPago, MarcaTarjeta, TarjetaEnmascarada FROM dbo.vw_ComprobanteDetalle WHERE ComprobanteId = @p0",
                comprobanteId).FirstOrDefault();

            if (encabezado == null)
            {
                return null;
            }

            ReservaResumenViewModel reserva = ObtenerResumen(encabezado.ReservaId);

            ComprobanteViewModel m = new ComprobanteViewModel();
            m.Encabezado = encabezado;
            m.Cliente = reserva.Usuario;
            m.Lineas = reserva.Detalle;
            m.Subtotal = reserva.Subtotal;
            m.Total = encabezado.TotalOperacion;
            m.Iva = m.Total - m.Subtotal;
            m.ImporteReembolso = db.Pago
                .Where(p => p.CompraId == encabezado.CompraId && p.EstadoPagoCodigo == "REVERSADO")
                .Select(p => (decimal?)p.Importe).Sum() ?? 0;
            return m;
        }

        // =================================================================
        // ADMINISTRACIÓN (pantallas 18, 19 y 21)
        // =================================================================
        public List<AdminVueloItemViewModel> ListarVuelosAdmin(DateTime desde, DateTime hasta)
        {
            List<ResultadoVueloViewModel> vuelos = ConvertirResultados(VuelosPorFecha(desde, hasta));
            List<int> ids = vuelos.Select(v => v.VueloId).ToList();

            Dictionary<int, Nullable<int>> aeronaves = db.Vuelo.Where(v => ids.Contains(v.VueloId))
                .Select(v => new { v.VueloId, v.AeronaveId }).ToList()
                .ToDictionary(v => v.VueloId, v => v.AeronaveId);

            Dictionary<int, string> itinerarios = db.TramoItinerario.Where(t => ids.Contains(t.VueloId))
                .Select(t => new { t.VueloId, t.Itinerario.Referencia, t.Orden }).ToList()
                .GroupBy(t => t.VueloId)
                .ToDictionary(g => g.Key, g => g.First().Referencia + " (tramo " + g.First().Orden + ")");

            return vuelos.Select(v => new AdminVueloItemViewModel
            {
                VueloId = v.VueloId,
                CodigoVuelo = v.CodigoVuelo,
                Fecha = v.Fecha,
                Ruta = v.Origen + " → " + v.Destino,
                HoraSalida = v.HoraSalida,
                HoraLlegada = v.HoraLlegada,
                AeronaveId = aeronaves.ContainsKey(v.VueloId) ? aeronaves[v.VueloId] : null,
                Matricula = v.Matricula,
                Estado = v.Estado,
                Capacidad = v.Capacidad,
                Ocupados = v.Capacidad - v.Disponibles,
                Itinerario = itinerarios.ContainsKey(v.VueloId) ? itinerarios[v.VueloId] : ""
            }).ToList();
        }

        public List<Aeronave> ListarAeronaves()
        {
            return db.Aeronave.Include(a => a.ModeloAeronave).Include(a => a.EstadoAeronave).OrderBy(a => a.Matricula).ToList();
        }

        // RNP-02: el trigger trg_Vuelo_Validar rechaza conflictos de horario de la aeronave
        public string AsignarAeronave(int vueloId, Nullable<int> aeronaveId)
        {
            Vuelo vuelo = db.Vuelo.Find(vueloId);
            if (vuelo == null)
            {
                return "El vuelo no existe.";
            }
            if (!aeronaveId.HasValue)
            {
                bool tieneAsientos = db.ReservaPasajeroVuelo.Any(r => r.VueloId == vueloId
                    && (r.EstadoAsignacionCodigo == "PENDIENTE" || r.EstadoAsignacionCodigo == "CONFIRMADA"));
                if (tieneAsientos)
                {
                    return "No se puede retirar la aeronave: el vuelo tiene asientos reservados u ocupados.";
                }
            }
            vuelo.AeronaveId = aeronaveId;
            try
            {
                db.SaveChanges();
                return null;
            }
            catch (Exception ex)
            {
                return "Vuelo con conflicto operativo: " + MensajeError(ex);
            }
        }

        public string CambiarEstadoVuelo(int vueloId, string estado)
        {
            Vuelo vuelo = db.Vuelo.Find(vueloId);
            if (vuelo == null)
            {
                return "El vuelo no existe.";
            }
            vuelo.EstadoVueloCodigo = estado;
            try
            {
                db.SaveChanges();
                return null;
            }
            catch (Exception ex)
            {
                return MensajeError(ex);
            }
        }

        // Usa el procedimiento almacenado usp_GenerarVuelos (los vuelos nuevos quedan sin aeronave)
        public int GenerarVuelos(DateTime inicio, DateTime fin)
        {
            int antes = db.Vuelo.Count();
            db.Database.ExecuteSqlCommand("EXEC dbo.usp_GenerarVuelos @FechaInicio = @p0, @FechaFin = @p1", inicio.Date, fin.Date);
            return db.Vuelo.Count() - antes;
        }

        public string CambiarEstadoAeronave(int aeronaveId, string estado)
        {
            Aeronave aeronave = db.Aeronave.Find(aeronaveId);
            if (aeronave == null)
            {
                return "La aeronave no existe.";
            }
            aeronave.EstadoAeronaveCodigo = estado;
            try
            {
                db.SaveChanges();
                return null;
            }
            catch (Exception ex)
            {
                return MensajeError(ex);
            }
        }

        public void CargarReportes(ReportesViewModel m)
        {
            DateTime desde = m.Desde.Value.Date;
            DateTime hasta = m.Hasta.Value.Date;
            DateTime hastaFin = hasta.AddDays(1);

            // Ocupación por vuelo (vista vw_DisponibilidadVuelo)
            m.Ocupacion = db.Database.SqlQuery<DisponibilidadVueloDto>(
                "SELECT VueloId, CodigoVuelo, FechaOperacion, Origen, Destino, HoraSalida, HoraLlegada, EstadoVueloCodigo, " +
                "Matricula, Capacidad, Ocupados, Disponibles FROM dbo.vw_DisponibilidadVuelo " +
                "WHERE FechaOperacion BETWEEN @p0 AND @p1 ORDER BY FechaOperacion, HoraSalida",
                desde, hasta).ToList();

            // Ventas por ruta: pasajes confirmados de compras pagadas en el rango
            var vendidos = db.ReservaPasajeroVuelo
                .Where(a => a.EstadoAsignacionCodigo == "CONFIRMADA"
                         && a.Reserva.Compra.Any(c => c.EstadoCompraCodigo == "PAGADA" && c.FechaCompra >= desde && c.FechaCompra < hastaFin))
                .Select(a => new { a.ReservaId, a.VueloId, a.ImporteTramo })
                .ToList();
            Dictionary<int, VueloDetalleDto> vuelos = VuelosPorId(vendidos.Select(v => v.VueloId)).ToDictionary(v => v.VueloId);

            m.VentasPorRuta = vendidos
                .Where(v => vuelos.ContainsKey(v.VueloId))
                .GroupBy(v => vuelos[v.VueloId].CiudadOrigen + " → " + vuelos[v.VueloId].CiudadDestino)
                .Select(g => new VentaRutaViewModel
                {
                    Ruta = g.Key,
                    Compras = g.Select(x => x.ReservaId).Distinct().Count(),
                    Pasajes = g.Count(),
                    Total = g.Sum(x => x.ImporteTramo)
                })
                .OrderBy(x => x.Ruta)
                .ToList();

            m.ReservasPorEstado = db.Reserva
                .Where(r => r.FechaCreacion >= desde && r.FechaCreacion < hastaFin)
                .GroupBy(r => r.EstadoReservaCodigo)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToList()
                .Select(x => new ConteoEstadoViewModel { Estado = x.Estado, Cantidad = x.Cantidad })
                .OrderBy(x => x.Estado)
                .ToList();

            m.PagosPorEstado = db.Pago
                .Where(p => p.FechaPago >= desde && p.FechaPago < hastaFin)
                .GroupBy(p => p.EstadoPagoCodigo)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToList()
                .Select(x => new ConteoEstadoViewModel { Estado = x.Estado, Cantidad = x.Cantidad })
                .OrderBy(x => x.Estado)
                .ToList();
        }

        // =================================================================
        // APOYO
        // =================================================================
        private void Guardar(string operacion)
        {
            try
            {
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new ReglaNegocioException(operacion + ": " + MensajeError(ex));
            }
        }

        // Devuelve el mensaje más útil de un error de EF o de un trigger de SQL Server
        public static string MensajeError(Exception ex)
        {
            DbEntityValidationException validacion = ex as DbEntityValidationException;
            if (validacion != null)
            {
                return string.Join(" ", validacion.EntityValidationErrors
                    .SelectMany(e => e.ValidationErrors)
                    .Select(e => e.ErrorMessage));
            }
            Exception interna = ex;
            while (interna.InnerException != null)
            {
                interna = interna.InnerException;
            }
            return interna.Message;
        }
    }
}