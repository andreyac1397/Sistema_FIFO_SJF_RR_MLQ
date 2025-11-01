using System;
using System.Collections.Generic;
using System.Linq;


namespace SimuladorProcesosSO_LOGICA
{
    /// <summary>
    /// Planificador FIFO (First In, First Out) / FCFS.
    /// Atiende por orden de llegada, no es expulsivo.
    /// </summary>
    public class FIFO : PlanificadorBase
    {
        public FIFO()
        {
            Nombre = "FIFO";
        }

        public override List<Proceso> Ejecutar(List<Proceso> procesos, int? quantum = null)
        {
            if (procesos == null) throw new ArgumentNullException(nameof(procesos));
            Reset();
            InicializarProcesos(procesos);

            // Orden por llegada (y por ID para desempatar de forma estable)
            var cola = procesos
                .OrderBy(p => p.TiempoLlegada)
                .ThenBy(p => p.ID)
                .ToList();

            int tiempoActual = 0;

            foreach (var p in cola)
            {
                if (p.Rafaga < 0) throw new ArgumentException("La ráfaga no puede ser negativa.");

                // Si el CPU está ocioso antes de que llegue el proceso, salta al tiempo de llegada
                if (tiempoActual < p.TiempoLlegada)
                    tiempoActual = p.TiempoLlegada;

                int inicio = tiempoActual;
                int fin = inicio + p.Rafaga;

                RegistrarTramo(p.ID, inicio, fin);

                p.TiempoRestante = 0; // terminó
                tiempoActual = fin;
            }

            CalcularMetricasFinales(procesos, Gantt);
            return procesos;
        }
    }
}
