using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimuladorProcesosSO_LOGICA
{
    /// <summary>
    /// Round Robin expulsivo con quantum configurable.
    /// Atiende por turnos y reencola procesos con tiempo restante.
    /// </summary>
    public class RoundRobin : PlanificadorBase
    {
        public RoundRobin()
        {
            Nombre = "Round Robin";
        }

        public override List<Proceso> Ejecutar(List<Proceso> procesos, int? quantum = null)
        {
            if (procesos == null) throw new ArgumentNullException(nameof(procesos));
            if (quantum == null || quantum <= 0) throw new ArgumentException("Quantum debe ser > 0.", nameof(quantum));

            Reset();
            InicializarProcesos(procesos);

            // Orden de llegada estable (desempate por ID)
            var porLlegada = procesos
                .OrderBy(p => p.TiempoLlegada)
                .ThenBy(p => p.ID)
                .ToList();

            var ready = new Queue<Proceso>();
            int tiempoActual = 0;
            int i = 0; // índice de próximos por llegar

            // Bucle principal: mientras queden por llegar o en cola
            while (i < porLlegada.Count || ready.Count > 0)
            {
                // Encolar todos los que ya llegaron a 'tiempoActual'
                while (i < porLlegada.Count && porLlegada[i].TiempoLlegada <= tiempoActual)
                {
                    var px = porLlegada[i++];
                    if (px.Rafaga < 0)
                        throw new ArgumentException("La ráfaga no puede ser negativa.");
                    if (px.Rafaga == 0)
                    {
                        // Proceso de ráfaga 0: no consume CPU; métricas se ajustan luego.
                        px.TiempoRestante = 0;
                    }
                    else
                    {
                        ready.Enqueue(px);
                    }
                }

                // Si no hay listos, saltar al próximo evento de llegada
                if (ready.Count == 0)
                {
                    if (i < porLlegada.Count)
                    {
                        tiempoActual = Math.Max(tiempoActual, porLlegada[i].TiempoLlegada);
                        continue; // reintentar encolar y procesar
                    }
                    else
                    {
                        break; // sin listos ni por llegar
                    }
                }

                // Tomar el siguiente listo
                var p = ready.Dequeue();

                int cuanto = Math.Min(quantum.Value, p.TiempoRestante);
                int inicio = tiempoActual;
                int fin = inicio + cuanto;

                // Ejecutar su tramo
                RegistrarTramo(p.ID, inicio, fin);

                p.TiempoRestante -= cuanto;
                tiempoActual = fin;

                // Encolar los que llegaron durante este tramo
                while (i < porLlegada.Count && porLlegada[i].TiempoLlegada <= tiempoActual)
                {
                    var px = porLlegada[i++];
                    if (px.Rafaga < 0)
                        throw new ArgumentException("La ráfaga no puede ser negativa.");
                    if (px.Rafaga == 0)
                    {
                        px.TiempoRestante = 0;
                    }
                    else
                    {
                        ready.Enqueue(px);
                    }
                }

                // Si no terminó, reencolar al final
                if (p.TiempoRestante > 0)
                {
                    ready.Enqueue(p);
                }
            }

            CalcularMetricasFinales(procesos, Gantt);
            return procesos;
        }
    }
}

