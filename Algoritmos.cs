// Algoritmos.cs
// Implementación de BFS (Lectura 1, Lección 5) y "primero el mejor"
// (Lectura 2, Lección 3). Ambos usan el procedimiento general con listas
// abierta/cerrada; lo único que cambia entre métodos es la estructura
// de la lista abierta.

using System.Diagnostics;

namespace TP2_Prototipo_Unificado;

public sealed record ResultadoBusqueda(
    bool Encontro,
    IReadOnlyList<int> Camino,
    int Palpados,
    int NodosExpandidos,
    TimeSpan Duracion,
    IReadOnlyList<IteracionBusqueda>? Trace = null);

public sealed record IteracionBusqueda(
    int Numero,
    int Posicion,
    string? InfoExtra);

/// <summary>Interfaz común para los algoritmos de búsqueda.</summary>
public interface IAlgoritmoBusqueda
{
    string Nombre { get; }
    ResultadoBusqueda Buscar(Problema problema, bool verbose);
}

// =============================================================================
//  BFS — Primero en anchura (cola FIFO, método no informado)
// =============================================================================

public sealed class BusquedaExhaustiva : IAlgoritmoBusqueda
{
    public string Nombre => "BFS (exhaustivo, anchura)";

    public ResultadoBusqueda Buscar(Problema problema, bool verbose)
    {
        var abierta = new Queue<Estado>();
        var cerrada = new HashSet<int>();
        var padre = new Dictionary<int, int>();
        padre[problema.Inicial] = int.MinValue;

        abierta.Enqueue(new Estado(problema.Inicial));

        int palpados = 0;
        int expandidos = 0;
        var trace = verbose ? new List<IteracionBusqueda>() : null;
        var cronometro = Stopwatch.StartNew();

        while (abierta.Count > 0)
        {
            var actual = abierta.Dequeue();
            if (!cerrada.Add(actual.Posicion)) continue;

            palpados++;
            expandidos++;

            string? info = verbose
                ? $"abierta=[{string.Join(",", abierta.Select(e => e.Posicion))}]  cerrada=[{string.Join(",", cerrada.OrderBy(x => x))}]"
                : null;
            trace?.Add(new IteracionBusqueda(expandidos, actual.Posicion, info));

            if (actual.Posicion == problema.Meta)
            {
                cronometro.Stop();
                return new ResultadoBusqueda(true, Reconstruir(padre, actual.Posicion),
                                            palpados, expandidos, cronometro.Elapsed, trace);
            }

            foreach (var op in OperadorExtensions.EnOrdenCanónico())
            {
                int nuevaPos = op.Aplicar(actual.Posicion, problema.DeltaH);
                if (cerrada.Contains(nuevaPos)) continue;
                if (!problema.DentroDeCota(nuevaPos)) continue;

                padre[nuevaPos] = actual.Posicion;
                abierta.Enqueue(new Estado(nuevaPos));
            }
        }

        cronometro.Stop();
        return new ResultadoBusqueda(false, Array.Empty<int>(), palpados, expandidos,
                                   cronometro.Elapsed, trace);
    }

    private static IReadOnlyList<int> Reconstruir(Dictionary<int, int> padre, int destino)
    {
        var camino = new List<int>();
        int? nodo = destino;
        while (nodo is int n && n != int.MinValue)
        {
            camino.Add(n);
            nodo = padre.TryGetValue(n, out int p) && p != int.MinValue ? p : (int?)null;
        }
        camino.Reverse();
        return camino;
    }
}

// =============================================================================
//  "Primero el mejor" — Cola de prioridad por h(n)
// =============================================================================

/// <summary>
/// Heurística admisible y consistente: h(h) = (AlturaMax − altura) / Pendiente = |h − A|.
/// </summary>
public static class Heuristica
{
    public static int Calcular(Problema problema, int posicion)
    {
        double altura = problema.AlturaRelieveEn(posicion);
        double distanciaEstimada = (problema.AlturaMax - altura) / problema.Pendiente;
        return (int)Math.Round(distanciaEstimada);
    }

    public static Palpado Palpar(Problema problema, int posicion) =>
        new(posicion == problema.Meta, problema.AlturaRelieveEn(posicion));
}

public sealed class BusquedaHeuristica : IAlgoritmoBusqueda
{
    public string Nombre => "Primero el mejor (heurístico)";

    public ResultadoBusqueda Buscar(Problema problema, bool verbose)
    {
        // Cola de prioridad por h(n) creciente; empate por menor |h| (más cerca de B).
        var abierta = new SortedSet<(int h, int absH, int pos)>(
            Comparer<(int h, int absH, int pos)>.Create((a, b) =>
                a.h != b.h ? a.h.CompareTo(b.h)
                : a.absH != b.absH ? a.absH.CompareTo(b.absH)
                : a.pos.CompareTo(b.pos)));

        var padre = new Dictionary<int, int>();
        var cerrada = new HashSet<int>();
        var trace = verbose ? new List<IteracionBusqueda>() : null;

        var hInicial = Heuristica.Calcular(problema, problema.Inicial);
        abierta.Add((hInicial, Math.Abs(problema.Inicial), problema.Inicial));
        padre[problema.Inicial] = int.MinValue;

        int palpados = 0;
        int expandidos = 0;
        var cronometro = Stopwatch.StartNew();

        while (abierta.Count > 0)
        {
            var (h, _, posActual) = abierta.Min;
            abierta.Remove(abierta.Min);

            if (!cerrada.Add(posActual)) continue;

            var palpado = Heuristica.Palpar(problema, posActual);
            palpados++;
            expandidos++;

            string? info = verbose
                ? $"h={h}  altura_palpada={palpado.AlturaRelieve:F1}  abierta=[{string.Join(",", abierta.Select(t => t.pos))}]  cerrada=[{string.Join(",", cerrada.OrderBy(x => x))}]"
                : null;
            trace?.Add(new IteracionBusqueda(expandidos, posActual, info));

            if (palpado.EsMeta)
            {
                cronometro.Stop();
                return new ResultadoBusqueda(true, Reconstruir(padre, posActual),
                                            palpados, expandidos, cronometro.Elapsed, trace);
            }

            foreach (var op in OperadorExtensions.EnOrdenCanónico())
            {
                int nuevaPos = op.Aplicar(posActual, problema.DeltaH);
                if (cerrada.Contains(nuevaPos)) continue;
                if (!problema.DentroDeCota(nuevaPos)) continue;

                int hNuevo = Heuristica.Calcular(problema, nuevaPos);
                abierta.Add((hNuevo, Math.Abs(nuevaPos), nuevaPos));
                if (!padre.ContainsKey(nuevaPos))
                    padre[nuevaPos] = posActual;
            }
        }

        cronometro.Stop();
        return new ResultadoBusqueda(false, Array.Empty<int>(), palpados, expandidos,
                                   cronometro.Elapsed, trace);
    }

    private static IReadOnlyList<int> Reconstruir(Dictionary<int, int> padre, int destino)
    {
        var camino = new List<int>();
        int? nodo = destino;
        while (nodo is int n && n != int.MinValue)
        {
            camino.Add(n);
            nodo = padre.TryGetValue(n, out int p) && p != int.MinValue ? p : (int?)null;
        }
        camino.Reverse();
        return camino;
    }
}