using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimuladorProcesosSO_LOGICA
{
    public class Estadisticas
    {
        public double PromedioEspera { get; set; }
        public double PromedioRetorno { get; set; }
        public int TiempoTotalCPU { get; set; }
        public int TiempoTotalSimulacion { get; set; }
        public double UtilizacionCPU => TiempoTotalSimulacion > 0
            ? (double)TiempoTotalCPU / TiempoTotalSimulacion
            : 0.0;
    }

    public class ResultadoProceso
    {
        public int ID { get; set; }
        public int Llegada { get; set; }
        public int Rafaga { get; set; }
        public int Comienzo { get; set; }
        public int Finalizacion { get; set; }
        public int Espera { get; set; }
        public int Retorno { get; set; }
        public string TipoCliente { get; set; }
        public int Prioridad { get; set; }
    }

    public class Reporte
    {
        public Estadisticas CalcularEstadisticas(List<Proceso> procesos, List<PlanificadorBase.TramoGantt> gantt)
        {
            if (procesos == null) throw new ArgumentNullException(nameof(procesos));
            if (gantt == null) throw new ArgumentNullException(nameof(gantt));

            foreach (var p in procesos)
            {
                var tramos = gantt.Where(t => t.ProcesoID == p.ID).ToList();
                if (tramos.Count > 0)
                {
                    p.TiempoComienzo = tramos.Min(t => t.Inicio);
                    p.TiempoFinalizacion = tramos.Max(t => t.Fin);
                }
                p.TiempoRetorno = p.TiempoFinalizacion - p.TiempoLlegada;
                p.TiempoEspera = Math.Max(0, p.TiempoRetorno - p.Rafaga);
            }

            int n = procesos.Count == 0 ? 1 : procesos.Count;
            double promEspera = procesos.Sum(p => p.TiempoEspera) / (double)n;
            double promRetorno = procesos.Sum(p => p.TiempoRetorno) / (double)n;

            int cpu = gantt.Sum(t => t.Fin - t.Inicio);
            int sim = 0;
            if (gantt.Count > 0)
            {
                int start = gantt.Min(t => t.Inicio);
                int end = gantt.Max(t => t.Fin);
                sim = Math.Max(0, end - start);
            }

            return new Estadisticas
            {
                PromedioEspera = promEspera,
                PromedioRetorno = promRetorno,
                TiempoTotalCPU = cpu,
                TiempoTotalSimulacion = sim
            };
        }

        public List<ResultadoProceso> TablaResultadosPorProceso(List<Proceso> procesos)
        {
            return procesos
                .OrderBy(p => p.ID)
                .Select(p => new ResultadoProceso
                {
                    ID = p.ID,
                    Llegada = p.TiempoLlegada,
                    Rafaga = p.Rafaga,
                    Comienzo = p.TiempoComienzo,
                    Finalizacion = p.TiempoFinalizacion,
                    Espera = p.TiempoEspera,
                    Retorno = p.TiempoRetorno,
                    Prioridad = p.Prioridad,
                    TipoCliente = p.TipoCliente
                })
                .ToList();
        }

        public string GanttComoTexto(List<PlanificadorBase.TramoGantt> gantt)
        {
            if (gantt == null || gantt.Count == 0) return "(sin ejecución)";

            var ordenados = gantt.OrderBy(t => t.Inicio).ThenBy(t => t.ProcesoID).ToList();
            var fusion = new List<PlanificadorBase.TramoGantt>();

            // Fusionar tramos contiguos del mismo proceso
            foreach (var t in ordenados)
            {
                if (fusion.Count > 0)
                {
                    var last = fusion[fusion.Count - 1]; // <-- C# 7.3 compatible
                    if (last.ProcesoID == t.ProcesoID && last.Fin == t.Inicio)
                    {
                        last.Fin = t.Fin;
                        continue;
                    }
                }
                fusion.Add(new PlanificadorBase.TramoGantt
                {
                    ProcesoID = t.ProcesoID,
                    Inicio = t.Inicio,
                    Fin = t.Fin
                });
            }

            var sbTop = new StringBuilder();
            var sbTimes = new StringBuilder();

            sbTop.Append("|");
            sbTimes.Append(fusion.First().Inicio.ToString().PadLeft(1));

            foreach (var tr in fusion)
            {
                string etiqueta = $" P{tr.ProcesoID} ";
                sbTop.Append(etiqueta).Append("|");
                sbTimes.Append(tr.Fin.ToString().PadLeft(etiqueta.Length + 1));
            }

            return sbTop.ToString() + Environment.NewLine + sbTimes.ToString();
        }
    }
}
