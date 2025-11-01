using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SimuladorProcesosSO_LOGICA
{
    /// <summary>
    /// Formato de archivo soportado (una fila por proceso):
    /// ID, Llegada, Rafaga[, Prioridad[, TipoCliente]]
    /// Ejemplos válidos (separador ; , \t | o espacios):
    ///   1,0,5,1,VIP
    ///   2;1;3;2;Regular
    ///   3 2 4 3 AdultoMayor
    ///   4|3|2               (prioridad por defecto = 3, tipo = "Regular")
    ///
    /// Se permiten:
    /// - Encabezado opcional (tieneEncabezado = true)
    /// - Comentarios: líneas que empiezan con # o //
    /// - Líneas en blanco
    /// </summary>
    public class GestorArchivos
    {
        /// <summary>
        /// Carga procesos desde un archivo .txt/.csv.
        /// </summary>
        /// <param name="ruta">Ruta completa del archivo</param>
        /// <param name="tieneEncabezado">Indica si la primera línea útil es encabezado</param>
        /// <param name="separadorForzado">Si lo indicas, se usa ese separador. Si es null, se infiere por línea.</param>
        public List<Proceso> CargarProcesos(string ruta, bool tieneEncabezado = true, char? separadorForzado = null)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                throw new ArgumentException("La ruta del archivo no puede ser vacía.", nameof(ruta));

            if (!File.Exists(ruta))
                throw new FileNotFoundException("No se encontró el archivo especificado.", ruta);

            var procesos = new List<Proceso>();
            var lineas = File.ReadAllLines(ruta, Encoding.UTF8);

            bool saltoEncabezado = tieneEncabezado;
            int numeroLineaArchivo = 0;

            foreach (var lineaRaw in lineas)
            {
                numeroLineaArchivo++;
                var linea = (lineaRaw ?? string.Empty).Trim();

                // Saltar comentarios o líneas vacías
                if (linea.Length == 0) continue;
                if (linea.StartsWith("#") || linea.StartsWith("//")) continue;

                // Saltar encabezado (solo la primera línea "útil")
                if (saltoEncabezado)
                {
                    saltoEncabezado = false;
                    continue;
                }

                char? usado;
                string[] partes = PartirLineaFlexible(linea, separadorForzado, out usado);

                // Se esperan 3 a 5 columnas: ID, Llegada, Rafaga, [Prioridad], [TipoCliente]
                if (partes.Length < 3 || partes.Length > 5)
                    throw new FormatException($"Línea {numeroLineaArchivo}: se esperaban 3 a 5 columnas, se leyeron {partes.Length}. Línea: '{linea}'");

                int id, llegada, rafaga;
                if (!int.TryParse(partes[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
                    throw new FormatException($"Línea {numeroLineaArchivo}: 'ID' no es un entero válido. Valor: '{partes[0]}'");

                if (!int.TryParse(partes[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out llegada))
                    throw new FormatException($"Línea {numeroLineaArchivo}: 'Llegada' no es un entero válido. Valor: '{partes[1]}'");

                if (!int.TryParse(partes[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out rafaga))
                    throw new FormatException($"Línea {numeroLineaArchivo}: 'Rafaga' no es un entero válido. Valor: '{partes[2]}'");

                int prioridad = 3; // por defecto (3 = baja)
                if (partes.Length >= 4 && !string.IsNullOrWhiteSpace(partes[3]))
                {
                    if (!int.TryParse(partes[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out prioridad))
                        throw new FormatException($"Línea {numeroLineaArchivo}: 'Prioridad' no es un entero válido. Valor: '{partes[3]}'");
                }

                string tipo = "Regular";
                if (partes.Length == 5 && !string.IsNullOrWhiteSpace(partes[4]))
                    tipo = partes[4].Trim();

                if (rafaga < 0)
                    throw new FormatException($"Línea {numeroLineaArchivo}: la ráfaga no puede ser negativa. Valor: {rafaga}");

                var p = new Proceso(id, llegada, rafaga, prioridad, tipo);
                procesos.Add(p);
            }

            // Validación básica: IDs únicos
            var repetidos = procesos
                .GroupBy(p => p.ID)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (repetidos.Count > 0)
                throw new FormatException("Existen IDs de proceso repetidos: " + string.Join(", ", repetidos));

            return procesos
                .OrderBy(p => p.TiempoLlegada)
                .ThenBy(p => p.ID)
                .ToList();
        }

        /// <summary>
        /// Guarda la tabla por proceso y métricas agregadas en formato CSV.
        /// </summary>
        public void GuardarResultadosCsv(
            string ruta,
            List<ResultadoProceso> tabla,
            Estadisticas estadisticas)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                throw new ArgumentException("La ruta no puede ser vacía.", nameof(ruta));
            if (tabla == null) throw new ArgumentNullException(nameof(tabla));
            if (estadisticas == null) throw new ArgumentNullException(nameof(estadisticas));

            var sb = new StringBuilder();
            sb.AppendLine("ID,Llegada,Rafaga,Comienzo,Finalizacion,Espera,Retorno,Prioridad,TipoCliente");
            foreach (var r in tabla)
            {
                sb.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                    r.ID, r.Llegada, r.Rafaga, r.Comienzo, r.Finalizacion,
                    r.Espera, r.Retorno, r.Prioridad,
                    (r.TipoCliente ?? "").Replace(",", " ")
                );
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("Métricas");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "PromedioEspera,{0:0.###}", estadisticas.PromedioEspera));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "PromedioRetorno,{0:0.###}", estadisticas.PromedioRetorno));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "TiempoTotalCPU,{0}", estadisticas.TiempoTotalCPU));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "TiempoTotalSimulacion,{0}", estadisticas.TiempoTotalSimulacion));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "UtilizacionCPU,{0:0.###}", estadisticas.UtilizacionCPU));

            File.WriteAllText(ruta, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Guarda el Gantt en texto plano (mismo formato que genera Reporte.GanttComoTexto).
        /// </summary>
        public void GuardarGanttTexto(string ruta, string ganttTexto)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                throw new ArgumentException("La ruta no puede ser vacía.", nameof(ruta));
            File.WriteAllText(ruta, ganttTexto ?? "(sin ejecución)", Encoding.UTF8);
        }

        // -------------------- Helpers --------------------

        private static string[] PartirLineaFlexible(string linea, char? separadorForzado, out char? usado)
        {
            usado = null;

            if (separadorForzado.HasValue)
            {
                usado = separadorForzado.Value;
                return linea.Split(new[] { separadorForzado.Value }, StringSplitOptions.None)
                            .Select(s => s.Trim())
                            .ToArray();
            }

            // Detectar separador por presencia
            if (linea.IndexOf(';') >= 0) { usado = ';'; return SplitTrim(linea, ';'); }
            if (linea.IndexOf('\t') >= 0) { usado = '\t'; return SplitTrim(linea, '\t'); }
            if (linea.IndexOf('|') >= 0) { usado = '|'; return SplitTrim(linea, '|'); }
            if (linea.IndexOf(',') >= 0) { usado = ','; return SplitTrim(linea, ','); }

            // Sin separadores “claros”: dividir por espacios en blanco múltiples
            // Nota: string.Split con separador null/empty => separa por espacios en blanco
            return linea.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToArray();
        }

        private static string[] SplitTrim(string linea, char sep)
        {
            return linea.Split(new[] { sep }, StringSplitOptions.None)
                        .Select(s => s.Trim())
                        .ToArray();
        }
    }
}

