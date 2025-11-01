using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimuladorProcesosSO_LOGICA
{
    /// <summary>
    /// Representa un proceso (o cliente) dentro del simulador del sistema operativo.
    /// Contiene toda la información necesaria para calcular tiempos y orden de ejecución.
    /// </summary>
    public class Proceso
    {
        // 🔹 Identificador único del proceso (ej. 1, 2, 3...)
        public int ID { get; set; }

        // 🔹 Tiempo en el que el proceso llega al sistema o al banco.
        public int TiempoLlegada { get; set; }

        // 🔹 Duración total que necesita para ejecutarse (ráfaga de CPU).
        public int Rafaga { get; set; }

        // 🔹 Nivel de prioridad (1 = alta, 2 = media, 3 = baja, etc.)
        public int Prioridad { get; set; }

        // 🔹 Tiempo que le falta para terminar (usado en Round Robin).
        public int TiempoRestante { get; set; }

        // 🔹 Momento en que empieza su ejecución.
        public int TiempoComienzo { get; set; }

        // 🔹 Momento en que termina su ejecución.
        public int TiempoFinalizacion { get; set; }

        // 🔹 Tiempo total desde que llegó hasta que terminó (turnaround).
        public int TiempoRetorno { get; set; }

        // 🔹 Tiempo total que esperó antes de ejecutarse.
        public int TiempoEspera { get; set; }

        // 🔹 (Opcional) Tipo de cliente (VIP, Regular, Adulto Mayor, etc.)
        public string TipoCliente { get; set; }

        /// <summary>
        /// Constructor base que inicializa los valores principales.
        /// </summary>
        public Proceso(int id, int llegada, int rafaga, int prioridad, string tipo = "Regular")
        {
            ID = id;
            TiempoLlegada = llegada;
            Rafaga = rafaga;
            Prioridad = prioridad;
            TipoCliente = tipo;
            TiempoRestante = rafaga; // al inicio le queda toda la ráfaga
        }

        /// <summary>
        /// Constructor vacío (por si necesitas crear el proceso y luego asignar valores).
        /// </summary>
        public Proceso() { }
    }
}

