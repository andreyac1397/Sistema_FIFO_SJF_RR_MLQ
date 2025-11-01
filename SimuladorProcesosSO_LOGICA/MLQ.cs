using System;
using System.Collections.Generic;
using System.Linq;

namespace SimuladorProcesosSO_LOGICA
{
    /// <summary>
    /// MLQ (Multilevel Queue) con prioridad estricta entre colas.
    /// 1 = mayor prioridad.
    /// Cada cola puede tener su propio planificador (FIFO, SJF, RR).
    /// </summary>
    public class MLQ : PlanificadorBase
    {
        // prioridad -> planificador que va a usar esa cola
        private readonly Dictionary<int, PlanificadorBase> _planificadorPorPrioridad =
            new Dictionary<int, PlanificadorBase>();

        public MLQ()
        {
            Nombre = "MLQ (prioridad estricta)";
        }

        public void ConfigurarCola(int prioridad, PlanificadorBase planificador)
        {
            if (prioridad <= 0)
                throw new ArgumentException("La prioridad debe ser >= 1 (1 = más alta).");
            if (planificador == null)
                throw new ArgumentNullException(nameof(planificador));

            _planificadorPorPrioridad[prioridad] = planificador;
        }

        public override List<Proceso> Ejecutar(List<Proceso> procesos, int? quantum = null)
        {
            if (procesos == null) throw new ArgumentNullException(nameof(procesos));

            // limpiamos el Gantt global de ESTE planificador
            Reset();

            // prioridades presentes en los procesos (1,2,3,...)
            var prioridades = procesos
                .Select(p => p.Prioridad <= 0 ? int.MaxValue : p.Prioridad)
                .Distinct()
                .OrderBy(pr => pr)
                .ToList();

            int tiempoActual = 0;

            foreach (var pr in prioridades)
            {
                // procesos que van en ESTA cola
                var subColaOriginal = procesos
                    .Where(p => (p.Prioridad <= 0 ? int.MaxValue : p.Prioridad) == pr)
                    .OrderBy(p => p.TiempoLlegada)
                    .ThenBy(p => p.ID)
                    .ToList();

                if (subColaOriginal.Count == 0)
                    continue;

                // planificador de la cola (si no hay, FIFO)
                PlanificadorBase planificadorCola =
                    _planificadorPorPrioridad.ContainsKey(pr)
                        ? _planificadorPorPrioridad[pr]
                        : new FIFO();

                // ⚠️ importante: limpiar el gantt del planificador de la cola
                planificadorCola.Reset();

                // clonar procesos de la cola y ajustar la llegada al tiempoActual
                var clones = subColaOriginal.Select(p => new Proceso
                {
                    ID = p.ID,
                    TiempoLlegada = Math.Max(0, p.TiempoLlegada - tiempoActual),
                    Rafaga = p.Rafaga,
                    Prioridad = p.Prioridad,
                    TiempoRestante = p.Rafaga,
                    TipoCliente = p.TipoCliente
                }).ToList();

                // ejecutar la cola (su tiempo empieza en 0)
                var _ = planificadorCola.Ejecutar(clones, quantum);

                // traer sus tramos y pasarlos al tiempo global
                int finCola = tiempoActual;
                foreach (var tramo in planificadorCola.Gantt)
                {
                    int inicioGlobal = tiempoActual + tramo.Inicio;
                    int finGlobal = tiempoActual + tramo.Fin;

                    RegistrarTramo(tramo.ProcesoID, inicioGlobal, finGlobal);

                    if (finGlobal > finCola)
                        finCola = finGlobal;
                }

                // avanzar el reloj global
                tiempoActual = finCola;
            }

            // calcular métricas en los procesos ORIGINALES usando el Gantt global
            CalcularMetricasFinales(procesos, Gantt);
            return procesos;
        }
    }
}
