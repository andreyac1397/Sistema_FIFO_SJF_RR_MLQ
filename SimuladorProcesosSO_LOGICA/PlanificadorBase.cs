using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimuladorProcesosSO_LOGICA
{
    /// <summary>
    /// Clase base abstracta para los planificadores de CPU.
    /// Provee:
    ///  - Un contrato (Ejecutar) que cada algoritmo debe implementar.
    ///  - Un contenedor Gantt (tramos de ejecución) común.
    ///  - Utilidades para inicializar y calcular métricas finales.
    /// </summary>
    public abstract class PlanificadorBase
    {
        /// <summary>
        /// Nombre legible del algoritmo (FIFO, SJF, Round Robin, MLQ, etc.)
        /// </summary>
        public string Nombre { get; protected set; } = "Planificador";

        /// <summary>
        /// Tramos ejecutados para construir el diagrama de Gantt.
        /// Cada tramo indica: ProcesoID, Inicio y Fin en tiempo.
        /// </summary>
        public List<TramoGantt> Gantt { get; } = new List<TramoGantt>();

        /// <summary>
        /// Tipo interno para representar un bloque de CPU en el Gantt.
        /// (Anidado para no crear más archivos/clases sueltas.)
        /// </summary>
        public class TramoGantt
        {
            public int ProcesoID { get; set; }
            public int Inicio { get; set; }
            public int Fin { get; set; }
        }

        /// <summary>
        /// Punto de entrada de cada algoritmo.
        /// Debe:
        ///  - Limpiar/Inicializar estados.
        ///  - Llenar Gantt con los tramos ejecutados.
        ///  - Actualizar métricas de los procesos (finalización, retorno, espera).
        /// Retorna la lista de procesos (con métricas calculadas).
        /// 
        /// El parámetro quantum se usa solo por Round Robin (otros lo ignoran).
        /// </summary>
        public abstract List<Proceso> Ejecutar(List<Proceso> procesos, int? quantum = null);

        /// <summary>
        /// Limpia el Gantt antes de una nueva ejecución.
        /// </summary>
        public virtual void Reset()
        {
            Gantt.Clear();
        }

        /// <summary>
        /// Agrega un bloque al Gantt (utilidad para las clases hijas).
        /// </summary>
        protected void RegistrarTramo(int procesoId, int inicio, int fin)
        {
            if (fin < inicio)
                throw new ArgumentException("El tiempo de fin no puede ser menor que el de inicio.");
            Gantt.Add(new TramoGantt { ProcesoID = procesoId, Inicio = inicio, Fin = fin });
        }

        /// <summary>
        /// Inicializa campos de métricas en los procesos antes de simular.
        /// </summary>
        protected static void InicializarProcesos(List<Proceso> procesos)
        {
            foreach (var p in procesos)
            {
                p.TiempoRestante = p.Rafaga;
                p.TiempoComienzo = 0;
                p.TiempoFinalizacion = 0;
                p.TiempoRetorno = 0;
                p.TiempoEspera = 0;
            }
        }

        /// <summary>
        /// Calcula métricas finales genéricas a partir del Gantt:
        ///  - TiempoComienzo: primer inicio del proceso.
        ///  - TiempoFinalizacion: último fin del proceso.
        ///  - TiempoRetorno = Finalizacion - Llegada.
        ///  - TiempoEspera  = Retorno - Ráfaga. (no negativo)
        /// </summary>
        protected static void CalcularMetricasFinales(List<Proceso> procesos, List<TramoGantt> gantt)
        {
            foreach (var p in procesos)
            {
                var tramos = gantt.Where(t => t.ProcesoID == p.ID).ToList();

                if (tramos.Count > 0)
                {
                    p.TiempoComienzo = tramos.Min(t => t.Inicio);
                    p.TiempoFinalizacion = tramos.Max(t => t.Fin);
                }
                else
                {
                    // No ejecutó: se deja comienzo= llegada, fin = llegada (o 0)
                    p.TiempoComienzo = p.TiempoLlegada;
                    p.TiempoFinalizacion = p.TiempoLlegada;
                }

                p.TiempoRetorno = p.TiempoFinalizacion - p.TiempoLlegada;
                p.TiempoEspera = Math.Max(0, p.TiempoRetorno - p.Rafaga);
            }
        }
    }
}

