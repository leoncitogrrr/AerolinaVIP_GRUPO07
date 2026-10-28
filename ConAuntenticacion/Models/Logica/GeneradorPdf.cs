using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ConAuntenticacion.Models
{
    // =====================================================================
    // GENERADOR DE PDF SENCILLO (sin librerías externas ni NuGet)
    // Escribe un PDF 1.4 con texto, líneas y rectángulos de colores.
    // Se usa para el comprobante académico (pantalla 16): logo, fecha,
    // aerolínea y valor.
    // =====================================================================
    public class GeneradorPdf
    {
        private const float Ancho = 595f;   // A4 en puntos
        private const float Alto = 842f;

        private readonly List<StringBuilder> paginas = new List<StringBuilder>();
        private StringBuilder actual;
        private static readonly CultureInfo inv = CultureInfo.InvariantCulture;

        public GeneradorPdf()
        {
            NuevaPagina();
        }

        public void NuevaPagina()
        {
            actual = new StringBuilder();
            paginas.Add(actual);
        }

        // y se mide desde ARRIBA (más cómodo); el PDF lo mide desde abajo
        public void Texto(float x, float y, string texto, float tamano = 10, bool negrita = false, string colorHex = "#000000")
        {
            float[] c = Color(colorHex);
            actual.AppendFormat(inv, "BT /{0} {1} Tf {2} {3} {4} rg {5} {6} Td ({7}) Tj ET\n",
                negrita ? "F2" : "F1", tamano, c[0], c[1], c[2], x, Alto - y, Escapar(texto ?? ""));
        }

        // Texto alineado a la derecha (aproximado con el ancho promedio de Helvetica)
        public void TextoDerecha(float xDerecha, float y, string texto, float tamano = 10, bool negrita = false, string colorHex = "#000000")
        {
            float ancho = (texto ?? "").Length * tamano * (negrita ? 0.56f : 0.52f);
            Texto(xDerecha - ancho, y, texto, tamano, negrita, colorHex);
        }

        public void Rectangulo(float x, float y, float ancho, float alto, string colorHex)
        {
            float[] c = Color(colorHex);
            actual.AppendFormat(inv, "{0} {1} {2} rg {3} {4} {5} {6} re f\n", c[0], c[1], c[2], x, Alto - y - alto, ancho, alto);
        }

        public void Linea(float x1, float y1, float x2, float y2, string colorHex = "#999999", float grosor = 0.6f)
        {
            float[] c = Color(colorHex);
            actual.AppendFormat(inv, "{0} {1} {2} RG {3} w {4} {5} m {6} {7} l S\n", c[0], c[1], c[2], grosor, x1, Alto - y1, x2, Alto - y2);
        }

        public void Circulo(float cx, float cy, float r, string colorHex)
        {
            // Círculo con 4 curvas de Bézier
            float[] c = Color(colorHex);
            float k = 0.5523f * r;
            float y = Alto - cy;
            actual.AppendFormat(inv, "{0} {1} {2} rg\n", c[0], c[1], c[2]);
            actual.AppendFormat(inv, "{0} {1} m\n", cx + r, y);
            actual.AppendFormat(inv, "{0} {1} {2} {3} {4} {5} c\n", cx + r, y + k, cx + k, y + r, cx, y + r);
            actual.AppendFormat(inv, "{0} {1} {2} {3} {4} {5} c\n", cx - k, y + r, cx - r, y + k, cx - r, y);
            actual.AppendFormat(inv, "{0} {1} {2} {3} {4} {5} c\n", cx - r, y - k, cx - k, y - r, cx, y - r);
            actual.AppendFormat(inv, "{0} {1} {2} {3} {4} {5} c f\n", cx + k, y - r, cx + r, y - k, cx + r, y);
        }

        public byte[] Generar()
        {
            Encoding win1252 = Encoding.GetEncoding(1252);
            MemoryStream ms = new MemoryStream();
            List<long> offsets = new List<long>();

            Action<string> escribir = s =>
            {
                byte[] b = win1252.GetBytes(s);
                ms.Write(b, 0, b.Length);
            };

            int n = paginas.Count;
            // Objetos: 1 catálogo, 2 páginas, 3 fuente normal, 4 fuente negrita, luego (página, contenido) por cada hoja
            int total = 4 + n * 2;

            escribir("%PDF-1.4\n%âãÏÓ\n");

            offsets.Add(ms.Position);
            escribir("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n");

            offsets.Add(ms.Position);
            StringBuilder kids = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                kids.Append((5 + i * 2) + " 0 R ");
            }
            escribir("2 0 obj << /Type /Pages /Kids [" + kids + "] /Count " + n + " >> endobj\n");

            offsets.Add(ms.Position);
            escribir("3 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >> endobj\n");

            offsets.Add(ms.Position);
            escribir("4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >> endobj\n");

            for (int i = 0; i < n; i++)
            {
                int objPagina = 5 + i * 2;
                int objContenido = objPagina + 1;
                byte[] contenido = win1252.GetBytes(paginas[i].ToString());

                offsets.Add(ms.Position);
                escribir(objPagina + " 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] " +
                         "/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents " + objContenido + " 0 R >> endobj\n");

                offsets.Add(ms.Position);
                escribir(objContenido + " 0 obj << /Length " + contenido.Length + " >>\nstream\n");
                ms.Write(contenido, 0, contenido.Length);
                escribir("\nendstream endobj\n");
            }

            long inicioXref = ms.Position;
            StringBuilder xref = new StringBuilder();
            xref.Append("xref\n0 " + (total + 1) + "\n");
            xref.Append("0000000000 65535 f \n");
            foreach (long o in offsets)
            {
                xref.Append(o.ToString("0000000000") + " 00000 n \n");
            }
            xref.Append("trailer << /Size " + (total + 1) + " /Root 1 0 R >>\n");
            xref.Append("startxref\n" + inicioXref + "\n%%EOF");
            escribir(xref.ToString());

            return ms.ToArray();
        }

        private static string Escapar(string texto)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in texto)
            {
                if (ch == '\\' || ch == '(' || ch == ')')
                {
                    sb.Append('\\');
                    sb.Append(ch);
                }
                else if (ch == '→')
                {
                    sb.Append("->");     // la flecha no existe en WinAnsi
                }
                else if (ch < 32)
                {
                    sb.Append(' ');
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        private static float[] Color(string hex)
        {
            hex = hex.TrimStart('#');
            return new float[]
            {
                (float)Math.Round(Convert.ToInt32(hex.Substring(0, 2), 16) / 255f, 3),
                (float)Math.Round(Convert.ToInt32(hex.Substring(2, 2), 16) / 255f, 3),
                (float)Math.Round(Convert.ToInt32(hex.Substring(4, 2), 16) / 255f, 3)
            };
        }

        // =================================================================
        // COMPROBANTE ACADÉMICO EN PDF
        // =================================================================
        public static byte[] Comprobante(ComprobanteViewModel m)
        {
            const string azul = "#0B1F3A";
            const string dorado = "#D4AF37";
            const string gris = "#555555";
            ComprobanteDetalleDto e = m.Encabezado;
            GeneradorPdf pdf = new GeneradorPdf();

            // ---------- Encabezado con logo ----------
            pdf.Rectangulo(0, 0, Ancho, 95, azul);
            pdf.Circulo(70, 47, 28, dorado);
            pdf.Texto(52, 54, "VIP", 18, true, azul);
            pdf.Texto(115, 40, e.EmisorNombre, 15, true, "#FFFFFF");
            pdf.Texto(115, 58, "Identificación: " + e.EmisorIdentificacion, 9, false, "#DDDDDD");
            pdf.Texto(115, 72, e.EmisorDireccion, 9, false, "#DDDDDD");
            pdf.TextoDerecha(565, 20, Cortar(e.TipoComprobante, 45), 8, true, dorado);
            pdf.TextoDerecha(565, 58, "N.º " + e.NumeroComprobante, 12, true, "#FFFFFF");
            pdf.TextoDerecha(565, 74, "Estado: " + e.EstadoComprobanteCodigo, 9, false, "#DDDDDD");

            // ---------- Datos de la operación ----------
            float y = 125;
            pdf.Texto(30, y, "Fecha de emisión:", 10, true, azul);
            pdf.Texto(140, y, e.FechaEmision.ToString("dd/MM/yyyy HH:mm"), 10);
            pdf.Texto(320, y, "Código de reserva:", 10, true, azul);
            pdf.Texto(430, y, e.CodigoReserva, 10);
            y += 16;
            pdf.Texto(30, y, "Cliente:", 10, true, azul);
            pdf.Texto(140, y, m.Cliente, 10);
            pdf.Texto(320, y, "Estado de la compra:", 10, true, azul);
            pdf.Texto(430, y, e.EstadoCompraCodigo, 10);
            y += 16;
            pdf.Texto(30, y, "Pago simulado:", 10, true, azul);
            pdf.Texto(140, y, (e.MarcaTarjeta ?? "-") + " " + (e.TarjetaEnmascarada ?? ""), 10);
            pdf.Texto(320, y, "Operación:", 10, true, azul);
            pdf.Texto(390, y, e.CodigoOperacion ?? "-", 8.5f);

            // ---------- Detalle ----------
            y += 30;
            pdf.Rectangulo(25, y - 13, 545, 20, "#E9EEF5");
            pdf.Texto(30, y, "Pasajero", 9, true, azul);
            pdf.Texto(165, y, "Vuelo", 9, true, azul);
            pdf.Texto(220, y, "Ruta", 9, true, azul);
            pdf.Texto(390, y, "Fecha / hora", 9, true, azul);
            pdf.Texto(480, y, "Asiento", 9, true, azul);
            pdf.TextoDerecha(565, y, "Valor", 9, true, azul);
            y += 18;

            foreach (DetalleAsignacionViewModel d in m.Lineas)
            {
                if (y > 740)
                {
                    pdf.NuevaPagina();
                    y = 50;
                }
                pdf.Texto(30, y, Cortar(d.Pasajero, 26), 8.5f);
                pdf.Texto(165, y, d.CodigoVuelo, 8.5f);
                pdf.Texto(220, y, Cortar(d.Ruta, 34), 8.5f);
                pdf.Texto(390, y, d.Fecha.ToString("dd/MM/yyyy") + " " + d.HoraSalida.ToString(@"hh\:mm"), 8.5f);
                pdf.Texto(480, y, d.Asiento + (d.EstadoAsignacion == "CONFIRMADA" ? "" : " (" + d.EstadoAsignacion.ToLower() + ")"), 8.5f);
                pdf.TextoDerecha(565, y, d.Importe.ToString("N2", inv), 8.5f);
                pdf.Linea(25, y + 5, 570, y + 5, "#DDDDDD");
                y += 16;
            }

            // ---------- Totales ----------
            y += 10;
            pdf.Texto(380, y, "Subtotal (tarifas):", 10, false, gris);
            pdf.TextoDerecha(565, y, m.Subtotal.ToString("N2", inv) + " " + e.MonedaCodigo, 10);
            y += 16;
            pdf.Texto(380, y, "IVA (" + (PoliticasAerolinea.PorcentajeIva * 100).ToString("0") + " %):", 10, false, gris);
            pdf.TextoDerecha(565, y, m.Iva.ToString("N2", inv) + " " + e.MonedaCodigo, 10);
            y += 8;
            pdf.Linea(380, y, 570, y, azul, 1);
            y += 18;
            pdf.Rectangulo(375, y - 14, 195, 22, azul);
            pdf.Texto(382, y, "VALOR TOTAL:", 11, true, "#FFFFFF");
            pdf.TextoDerecha(565, y, m.Total.ToString("N2", inv) + " " + e.MonedaCodigo, 11, true, dorado);
            if (m.ImporteReembolso > 0)
            {
                y += 22;
                pdf.Texto(380, y, "Reembolso simulado:", 10, true, "#B02A37");
                pdf.TextoDerecha(565, y, m.ImporteReembolso.ToString("N2", inv) + " " + e.MonedaCodigo, 10, true, "#B02A37");
            }

            // ---------- Leyenda académica ----------
            pdf.Linea(25, 780, 570, 780, "#CCCCCC");
            pdf.Texto(30, 795, e.LeyendaComprobante, 8, false, gris);
            pdf.Texto(30, 808, "Pago SIMULADO con fines académicos. No se almacenan números completos de tarjeta ni códigos de seguridad.", 8, false, gris);
            pdf.Texto(30, 821, "Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " · Aerolínea VIP · Proyecto académico ESPE", 8, false, gris);

            return pdf.Generar();
        }

        private static string Cortar(string texto, int maximo)
        {
            if (string.IsNullOrEmpty(texto) || texto.Length <= maximo)
            {
                return texto ?? "";
            }
            return texto.Substring(0, maximo - 1) + "…";
        }
    }
}
