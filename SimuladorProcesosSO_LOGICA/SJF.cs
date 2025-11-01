using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimuladorProcesosSO_LOGICA
{
    /// <summary>
    /// SJF (Shortest Job First) NO expulsivo.
    /// Siempre toma el proceso disponible con menor ráfaga.
    /// Si no hay procesos disponibles, adelanta el tiempo al próximo que llegue.
    /// </summary>
    public class SJF : PlanificadorBase
    {
        public SJF()
        {
            Nombre = "SJF (no expulsivo)";
        }

        public override List<Proceso> Ejecutar(List<Proceso> procesos, int? quantum = null)
        {
            if (procesos == null) throw new ArgumentNullException(nameof(procesos));
            Reset();
            InicializarProcesos(procesos);

            // Conjunto de procesos aún no ejecutados
            var pendientes = procesos
                .Select(p => p) // misma referencia para llenar métricas
                .ToList();

            int tiempoActual = 0;

            // Ordenar referencia por llegada para localizar próximo evento cuando CPU está ocioso
            var porLlegada = pendientes.OrderBy(p => p.TiempoLlegada).ToList();

            while (pendientes.Count > 0)
            {
                // Filtrar procesos que ya llegaron
                var disponibles = pendientes
                    .Where(p => p.TiempoLlegada <= tiempoActual)
                    .ToList();

                if (disponibles.Count == 0)
                {
                    // CPU ocioso: saltar al tiempo de llegada del próximo proceso
                    var proximo = pendientes.OrderBy(p => p.TiempoLlegada).First();
                    tiempoActual = Math.Max(tiempoActual, proximo.TiempoLlegada);
                    disponibles = pendientes.Where(p => p.TiempoLlegada <= tiempoActual).ToList();
                }

                // Elegir el de menor ráfaga (desempate por llegada y luego por ID para estabilidad)
                var elegido = disponibles
                    .OrderBy(p => p.Rafaga)
                    .ThenBy(p => p.TiempoLlegada)
                    .ThenBy(p => p.ID)
                    .First();

                if (elegido.Rafaga < 0)
                    throw new ArgumentException("La ráfaga no puede ser negativa.");

                int inicio = tiempoActual;
                int fin = inicio + elegido.Rafaga;

                RegistrarTramo(elegido.ID, inicio, fin);

                elegido.TiempoRestante = 0; // terminó
                tiempoActual = fin;

                // Quitar de pendientes
                pendientes.Remove(elegido);
            }

            CalcularMetricasFinales(procesos, Gantt);
            return procesos;
        }
    }
}

