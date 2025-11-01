using System;
using System.Collections.Generic;
using System.Linq;

namespace SimuladorProcesosSO_LOGICA
{
    // Los 4 modos que puede escoger el usuario en la UI
    public enum TipoPlanificador
    {
        FIFO,
        SJF,
        RoundRobin,
        MLQ
    }

    // Lo que la lógica le devuelve a la UI
    public class ResultadoSimulacion
    {
        public string NombrePlanificador { get; set; }
        public List<PlanificadorBase.TramoGantt> Gantt { get; set; }
        public List<ResultadoProceso> ResultadosPorProceso { get; set; }
        public Estadisticas Estadisticas { get; set; }
        public string GanttTexto { get; set; }
    }

    /// <summary>
    /// Orquesta la ejecución de los planificadores y arma el reporte para la UI.
    /// Aquí es donde decidimos:
    /// - qué algoritmo correr
    /// - con qué quantum
    /// - y, si es MLQ, qué algoritmo va en cada cola.
    /// </summary>
    public class Simulador
    {
        private readonly Reporte _reporte = new Reporte();

        /// <summary>
        /// Ejecuta el planificador indicado.
        /// - tipo: FIFO, SJF, RR o MLQ
        /// - procesosEntrada: vienen de la UI o del TXT
        /// - quantum: se usa para Round Robin y también como “quantum por defecto” en MLQ
        /// - configMLQ: prioridad -> algoritmo ("FIFO", "SJF", "RR", "RR:4")
        ///     Ejemplo:
        ///         { 1, "SJF" }
        ///         { 2, "RR:3" }
        ///         { 3, "FIFO" }
        /// </summary>
        public ResultadoSimulacion Ejecutar(
            TipoPlanificador tipo,
            List<Proceso> procesosEntrada,
            int? quantum = null,
            Dictionary<int, string> configMLQ = null)
        {
            if (procesosEntrada == null)
                throw new ArgumentNullException(nameof(procesosEntrada));

            // clonamos para no tocar lo que mostró la UI
            var procesos = ClonarProcesos(procesosEntrada);

            PlanificadorBase planificador;

            switch (tipo)
            {
                case TipoPlanificador.FIFO:
                    planificador = new FIFO();
                    break;

                case TipoPlanificador.SJF:
                    planificador = new SJF();
                    break;

                case TipoPlanificador.RoundRobin:
                    planificador = new RoundRobin();
                    break;

                case TipoPlanificador.MLQ:
                    // aquí sí metemos la config de las colas
                    planificador = ConfigurarMLQ(configMLQ, quantum);
                    break;

                default:
                    throw new NotSupportedException("Tipo de planificador no soportado.");
            }

            // correr el algoritmo
            planificador.Ejecutar(procesos, quantum);

            // armar lo que la UI necesita
            var stats = _reporte.CalcularEstadisticas(procesos, planificador.Gantt);
            var tabla = _reporte.TablaResultadosPorProceso(procesos);
            var ganttTx = _reporte.GanttComoTexto(planificador.Gantt);

            return new ResultadoSimulacion
            {
                NombrePlanificador = planificador.Nombre,
                Gantt = planificador.Gantt.ToList(),
                ResultadosPorProceso = tabla,
                Estadisticas = stats,
                GanttTexto = ganttTx
            };
        }

        // atajos por si la UI quiere usar métodos directos
        public ResultadoSimulacion EjecutarFIFO(List<Proceso> procesos)
            => Ejecutar(TipoPlanificador.FIFO, procesos);

        public ResultadoSimulacion EjecutarSJF(List<Proceso> procesos)
            => Ejecutar(TipoPlanificador.SJF, procesos);

        public ResultadoSimulacion EjecutarRoundRobin(List<Proceso> procesos, int quantum)
            => Ejecutar(TipoPlanificador.RoundRobin, procesos, quantum);

        public ResultadoSimulacion EjecutarMLQ(
            List<Proceso> procesos,
            Dictionary<int, string> configMLQ,
            int? quantumRRporDefecto = null)
            => Ejecutar(TipoPlanificador.MLQ, procesos, quantumRRporDefecto, configMLQ);

        // ------------------------------------------------
        // Helpers
        // ------------------------------------------------

        /// <summary>
        /// Crea un MLQ y le pone a cada cola el algoritmo que dijo el usuario.
        /// Soporta:
        ///   "FIFO"
        ///   "SJF"
        ///   "RR"
        ///   "RR:4"  (RR con quantum = 4)
        /// Si la cola dice "RR" sin número y arriba la UI mandó un quantum, usamos ese.
        /// Si no mandó nada, usamos 2 como valor seguro.
        /// </summary>
        private static MLQ ConfigurarMLQ(Dictionary<int, string> config, int? quantumPorDefecto)
        {
            var mlq = new MLQ();

            // si no mandaron config, MLQ ya usa FIFO por cada prioridad
            if (config == null || config.Count == 0)
                return mlq;

            foreach (var kv in config)
            {
                int prioridad = kv.Key;
                string valor = (kv.Value ?? "").Trim();

                // normalizamos
                var upper = valor.ToUpperInvariant();

                PlanificadorBase planificadorCola;

                if (upper.StartsWith("RR"))
                {
                    // puede venir "RR" o "RR:4"
                    int quantumLocal = quantumPorDefecto ?? 2; // si no mandaron nada, 2
                    if (valor.Contains(":"))
                    {
                        var partes = valor.Split(':');
                        if (partes.Length == 2 && int.TryParse(partes[1], out int qParsed) && qParsed > 0)
                            quantumLocal = qParsed;
                    }

                    planificadorCola = new RoundRobinConQuantumFijo(quantumLocal);
                }
                else if (upper == "SJF")
                {
                    planificadorCola = new SJF();
                }
                else // FIFO o cualquier cosa rara -> FIFO
                {
                    planificadorCola = new FIFO();
                }

                mlq.ConfigurarCola(prioridad, planificadorCola);
            }

            return mlq;
        }

        /// <summary>
        /// Clona la lista de procesos para no tocar lo que vino de la UI.
        /// </summary>
        private static List<Proceso> ClonarProcesos(List<Proceso> src)
        {
            var list = new List<Proceso>(src.Count);
            foreach (var p in src)
            {
                list.Add(new Proceso
                {
                    ID = p.ID,
                    TiempoLlegada = p.TiempoLlegada,
                    Rafaga = p.Rafaga,
                    Prioridad = p.Prioridad,
                    TiempoRestante = p.Rafaga,
                    TipoCliente = p.TipoCliente
                });
            }
            return list;
        }
    }

    /// <summary>
    /// Pequeño adaptador: es RoundRobin pero con un quantum fijo que ya viene dado.
    /// Así MLQ no tiene que estar pasando quantum todo el tiempo.
    /// </summary>
    internal class RoundRobinConQuantumFijo : RoundRobin
    {
        private readonly int _quantum;

        public RoundRobinConQuantumFijo(int quantum)
        {
            _quantum = quantum > 0 ? quantum : 2;
        }

        public override List<Proceso> Ejecutar(List<Proceso> procesos, int? quantum = null)
        {
            // ignoramos el quantum que venga de afuera y usamos el nuestro
            return base.Ejecutar(procesos, _quantum);
        }
    }
}
