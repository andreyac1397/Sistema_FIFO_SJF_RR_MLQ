using System;
using System.Collections.Generic;
using System.Globalization;
using SimuladorProcesosSO_LOGICA;

namespace SimuladorProcesosSO_Consola
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Opcional: formato de decimales predecible
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

                // 1) Cargar procesos desde archivo si se pasa ruta por argumentos
                List<Proceso> procesos = null;
                if (args != null && args.Length > 0 && System.IO.File.Exists(args[0]))
                {
                    var gestor = new GestorArchivos();
                    procesos = gestor.CargarProcesos(args[0], true, null);
                    Console.WriteLine("Procesos cargados desde archivo: {0}", procesos.Count);
                }
                else
                {
                    // 2) Datos de prueba (demo)
                    procesos = DatosDemo();
                    Console.WriteLine("Usando datos de prueba (demo).");
                }

                var sim = new Simulador();

                // FIFO
                var r1 = sim.EjecutarFIFO(procesos);
                Imprimir(r1);

                // SJF (no expulsivo)
                var r2 = sim.EjecutarSJF(procesos);
                Imprimir(r2);

                // Round Robin (quantum = 2)
                int quantum = 2;
                var r3 = sim.EjecutarRoundRobin(procesos, quantum);
                Imprimir(r3);

                // MLQ: prioridad 1 -> SJF, prioridad 2 -> RR, prioridad 3 -> FIFO
                var cfg = new Dictionary<int, string>();
                cfg[1] = "SJF";
                cfg[2] = "RR";
                cfg[3] = "FIFO";
                var r4 = sim.EjecutarMLQ(procesos, cfg, quantum);
                Imprimir(r4);

                Console.WriteLine("\nFin de pruebas. Presione cualquier tecla para salir...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }
        }

        // ---------- Helpers ----------

        static List<Proceso> DatosDemo()
        {
            return new List<Proceso>
            {
                new Proceso(1, 0, 5, 1, "VIP"),
                new Proceso(2, 1, 3, 2, "Regular"),
                new Proceso(3, 2, 2, 1, "AdultoMayor"),
                new Proceso(4, 3, 1, 3, "Regular"),
                new Proceso(5, 5, 4, 2, "Foraneo"),
                new Proceso(6, 6, 2, 3, "Embarazada")
            };
        }

        static void Imprimir(ResultadoSimulacion res)
        {
            Console.WriteLine("\n==============================");
            Console.WriteLine("PLANIFICADOR: {0}", res.NombrePlanificador);
            Console.WriteLine("------------------------------");
            Console.WriteLine(res.GanttTexto);
            Console.WriteLine("------------------------------");
            Console.WriteLine("Promedio espera : {0:0.###}", res.Estadisticas.PromedioEspera);
            Console.WriteLine("Promedio retorno: {0:0.###}", res.Estadisticas.PromedioRetorno);
            Console.WriteLine("CPU total       : {0}", res.Estadisticas.TiempoTotalCPU);
            Console.WriteLine("Simulación total: {0}", res.Estadisticas.TiempoTotalSimulacion);
            Console.WriteLine("Utilización CPU : {0:P1}", res.Estadisticas.UtilizacionCPU);

            Console.WriteLine("\nID\tLleg\tRaf\tIni\tFin\tEsp\tRet\tPrio\tTipo");
            foreach (var p in res.ResultadosPorProceso)
            {
                Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                    p.ID, p.Llegada, p.Rafaga, p.Comienzo, p.Finalizacion,
                    p.Espera, p.Retorno, p.Prioridad, p.TipoCliente);
            }
        }
    }
}
