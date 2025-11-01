# Sistema_FIFO_SJF_RR_MLQ
Simulador de planificación de procesos (clientes) para el curso de **Arquitectura / Sistemas Operativos**.

El objetivo del proyecto es **comparar cómo distintos algoritmos de planificación atienden una misma cola de procesos** y ver el efecto que tiene cada uno en el **tiempo de espera**, **tiempo de retorno** y **orden de atención**. El programa permite cargar procesos, escoger un algoritmo y ver el resultado en forma de tabla y diagrama de tiempo (estilo Gantt).

## Objetivos
- Simular el comportamiento de varios **algoritmos clásicos de planificación de CPU**.
- Mostrar de forma visual el **orden en que se atienden los procesos**.
- Permitir la **comparación** entre algoritmos usando las mismas entradas.
- Dejar claro cuáles algoritmos son **apropiados para colas simples** y cuáles para **colas con prioridades/niveles**.

## Algoritmos implementados
1. **FIFO / FCFS (First Come, First Served)**  
   - Atiende en el orden de llegada.
   - Simple, pero puede generar espera larga.

2. **SJF (Shortest Job First) – no expropiativo**  
   - Atiende primero el proceso con menor ráfaga.
   - Bueno cuando los trabajos son cortos.
   - Puede dejar esperando a procesos largos.

3. **Round Robin (RR)**  
   - Usa **quantum**.
   - Cada proceso recibe un pedacito de CPU y vuelve al final de la cola.
   - Útil para **entornos interactivos**.

4. **MLQ (Multi-Level Queue)**  
   - Cola por **niveles/prioridades**.
   - Primero atiende las colas de más prioridad.
   - Se puede usar para casos tipo **clientes VIP / adultos mayores / regulares**.

## Qué hace el sistema
- Permite **cargar procesos/clientes** con sus datos básicos (ID, tiempo de llegada, ráfaga, prioridad si aplica).
- Permite **elegir el algoritmo** a usar.
- Muestra una **tabla de resultados** con:
  - Tiempo de inicio
  - Tiempo de finalización
  - Tiempo de espera
  - Tiempo de retorno
- Genera una **vista tipo Gantt** para entender el orden de ejecución.
- Genera un **reporte comparativo** cuando se ejecutan varios algoritmos con la misma entrada.

## Estructura de la solución
La solución está separada en proyectos:

- **SimuladorProcesosSO_LOGICA**  
  Contiene toda la **lógica de simulación**:
  - `Proceso.cs`
  - `PlanificadorBase.cs`
  - `FIFO.cs`
  - `SJF.cs`
  - `RoundRobin.cs`
  - `MLQ.cs`
  - `Simulador.cs`
  - `Reporte.cs`
  - (Opcional) `GestorArchivos.cs` para cargar/guardar listas de procesos.

- **SimuladorProcesosSO** (WinForms)  
  Interfaz gráfica para probar los algoritmos:
  - `FormPrincipal.cs` muestra la tabla y el Gantt.
  - Desde aquí se selecciona el algoritmo.

- **SimuladorProcesosSO_Consola**  
  Versión por consola para pruebas rápidas desde el curso.

Esta separación se hizo para cumplir con el enfoque de **arquitectura por capas**: la lógica no depende de la interfaz.

## Requisitos
- **Windows**  
- **.NET Framework 4.8** (o el que tenga el proyecto)
- **Visual Studio 2022** (o compatible)

## Cómo ejecutar
1. Clonar el repositorio:
   ```bash
   git clone https://github.com/andreyac1397/Sistema_FIFO_SJF_RR_MLQ.git
Abrir la solución SimuladorProcesosSO.sln en Visual Studio.

Establecer como proyecto de inicio: SimuladorProcesosSO (el WinForms).

Ejecutar con F5.

Cargar o digitar los procesos y escoger el algoritmo.


El tiempo se maneja en unidades (no en minutos reales).

Los procesos llegan en el tiempo indicado por el usuario.

En RR se debe indicar quantum.

En MLQ se asume que los procesos ya vienen con su prioridad / nivel de cola.

Trabajo académico
Este proyecto corresponde al proyecto 1 del curso de Arquitectura / Sistemas Operativos y su propósito principal es mostrar y comparar estrategias de planificación, no construir un planificador real del sistema operativo.

Autor
Andrey Calderón Vega-https://github.com/andreyac1397
Kendall Solano Solís-https://github.com/kendalls-s

CUC – Tecnologías de la Información
