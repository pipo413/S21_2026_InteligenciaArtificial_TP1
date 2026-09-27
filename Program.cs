// Program.cs — Prototipo UNIFICADO del TP2 de Inteligencia Artificial.
//
// Menú interactivo con dos algoritmos de búsqueda:
//   - BFS (Lectura 1, Lección 5): exhaustivo, primero en anchura.
//   - Primero el mejor (Lectura 2, Lección 3): heurístico, orden por h(n).
//
// Modo CLI:  TP2_Prototipo_Unificado --demo --b 0 --a 4 --l 6

using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace TP2_Prototipo_Unificado;

public static class Program
{
    private const int RANGO_MIN = -10;
    private const int RANGO_MAX = 10;
    private const int POSITIVO_MIN = 1;

    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length > 0 && args[0] == "--demo")
        {
            var problemaDemo = ParsearArgumentos(args, ProblemaPorDefecto());
            EjecutarDemo(problemaDemo);
            return;
        }

        var problema = ProblemaPorDefecto();

        while (true)
        {
            LimpiarConsola();
            ImprimirEncabezado();
            ImprimirEstadoActual(problema);
            ImprimirMenu();
            Console.Write("Opción: ");

            string? op = Console.ReadLine();
            Console.WriteLine();

            switch (op)
            {
                case "1":
                    problema = ConfigurarProblema(problema);
                    break;
                case "2":
                    EjecutarAlgoritmo(problema, new BusquedaExhaustiva(), verbose: true);
                    Pausa();
                    break;
                case "3":
                    EjecutarAlgoritmo(problema, new BusquedaHeuristica(), verbose: true);
                    Pausa();
                    break;
                case "4":
                    EjecutarComparacion(problema);
                    Pausa();
                    break;
                case "5":
                    MostrarRelieve(problema);
                    Pausa();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Opción no válida. Presione ENTER para continuar...");
                    Console.ReadLine();
                    break;
            }
        }
    }

    private static Problema ProblemaPorDefecto() => new(
        Inicial: 0, Meta: 4, DeltaH: 1, Limite: 6,
        AlturaMax: 10, Pendiente: 1);

    private static void ImprimirEncabezado()
    {
        Console.WriteLine("=========================================================");
        Console.WriteLine(" TP2 - Búsqueda en el espacio de estados");
         Console.WriteLine("=========================================================");
    }

    private static void ImprimirEstadoActual(Problema p)
    {
        Console.WriteLine();
        Console.WriteLine("Parámetros del problema:");
        Console.WriteLine($"   AlturaMax    = {p.AlturaMax,6}    (altura del relieve en A)");
        Console.WriteLine($"   Pendiente    = {p.Pendiente,6}    (decaimiento del relieve por unidad de distancia)");
        Console.WriteLine($"   A (meta)     = {p.Meta,6}    (centro del anillo)");
        Console.WriteLine($"   B (inicial)  = {p.Inicial,6}    (posición teórica del brazo)");
        Console.WriteLine($"   ΔH           = {p.DeltaH,6}    (incremento elemental)");
        Console.WriteLine($"   L (cota)     = {p.Limite,6}    (exploración máxima a cada lado de B)");
        int dist = Math.Abs(p.Meta - p.Inicial);
        string dir = p.Meta > p.Inicial ? "a la derecha"
                  : p.Meta < p.Inicial ? "a la izquierda"
                  : "sobre B";
        Console.WriteLine($"\n   La meta está a {dist} ΔH de B ({dir}).");
    }

    private static void ImprimirMenu()
    {
        Console.WriteLine();
        Console.WriteLine("Menú:");
        Console.WriteLine("   1. Configurar parámetros del problema");
        Console.WriteLine("   2. Ejecutar SOLO BFS (paso a paso)");
        Console.WriteLine("   3. Ejecutar SOLO \"primero el mejor\" (paso a paso)");
        Console.WriteLine("   4. Ejecutar AMBOS en paralelo y comparar");
        Console.WriteLine("   5. Ver el relieve y cómo se deriva la heurística");
        Console.WriteLine("   0. Salir");
    }

    private static Problema ConfigurarProblema(Problema actual)
    {
        Console.WriteLine("Configurar parámetros del problema");
        Console.WriteLine("(Pulse ENTER para mantener el valor actual.)\n");

        int b     = LeerEnteroEnRango("B (inicial)", actual.Inicial, RANGO_MIN, RANGO_MAX);
        int a     = LeerEnteroEnRango("A (meta)", actual.Meta, RANGO_MIN, RANGO_MAX);
        int delta = LeerEnteroEnRango("ΔH", actual.DeltaH, POSITIVO_MIN, 5);
        int l     = LeerEnteroEnRango("L (cota)", actual.Limite, POSITIVO_MIN, RANGO_MAX);
        double am = LeerDoublePositivo("AlturaMax", actual.AlturaMax);
        double pe = LeerDoublePositivo("Pendiente", actual.Pendiente);

        var nuevo = new Problema(b, a, delta, l, am, pe);
        Console.WriteLine($"\nNuevo problema configurado:");
        ImprimirEstadoActual(nuevo);
        Console.WriteLine("\nPresione ENTER para volver al menú...");
        Console.ReadLine();
        return nuevo;
    }

    private static int LeerEnteroEnRango(string etiqueta, int valorActual, int min, int max)
    {
        while (true)
        {
            Console.Write($"  {etiqueta} [{valorActual}] (entero en [{min}, {max}]): ");
            string? s = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(s)) return valorActual;
            if (int.TryParse(s, out int n) && n >= min && n <= max) return n;
            Console.WriteLine($"    Valor inválido. Ingresá un entero entre {min} y {max}, o ENTER para mantener.");
        }
    }

    private static double LeerDoublePositivo(string etiqueta, double valorActual)
    {
        while (true)
        {
            Console.Write($"  {etiqueta} [{valorActual}] (real positivo): ");
            string? s = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(s)) return valorActual;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double n) && n > 0)
                return n;
            Console.WriteLine("    Valor inválido. Ingresá un real positivo, o ENTER para mantener.");
        }
    }

    private static void EjecutarAlgoritmo(Problema problema, IAlgoritmoBusqueda algoritmo, bool verbose)
    {
        Console.WriteLine($"=== {algoritmo.Nombre} ===");
        var resultado = algoritmo.Buscar(problema, verbose);

        if (verbose && resultado.Trace != null)
        {
            foreach (var it in resultado.Trace)
            {
                string extra = it.InfoExtra ?? "";
                Console.WriteLine($"  Iter {it.Numero,2}: pos={it.Posicion,3}  {extra}");
            }
        }

        ImprimirResultado(resultado);
    }

    private static void ImprimirResultado(ResultadoBusqueda r)
    {
        if (r.Encontro)
        {
            Console.WriteLine($"   META ENCONTRADA");
            Console.WriteLine($"  Trayecto: {string.Join(" → ", r.Camino)}");
        }
        else
        {
            Console.WriteLine("  FALLO: meta NO encontrada dentro de la cota L.");
        }
        Console.WriteLine($"  Palpados realizados:  {r.Palpados}");
        Console.WriteLine($"  Nodos expandidos:     {r.NodosExpandidos}");
        Console.WriteLine($"  Tiempo de cómputo:    {r.Duracion.TotalMilliseconds:F2} ms");
    }

    private static void EjecutarComparacion(Problema problema)
    {
        Console.WriteLine("=== Comparación: BFS vs. Primero el mejor ===");
        ImprimirEstadoActual(problema);
        Console.WriteLine();
        Console.WriteLine("Ejecutando ambos algoritmos en paralelo con los mismos parámetros...");
        Console.WriteLine("(BFS y \"primero el mejor\" corren en hilos separados, sobre los mismos B, A, ΔH, L.)");

        var bfs = new BusquedaExhaustiva();
        var pm  = new BusquedaHeuristica();

        // Parallel.Invoke lanza ambos en hilos del ThreadPool.
        ResultadoBusqueda rBfs = null!;
        ResultadoBusqueda rPm  = null!;
        var cronometroParalelo = Stopwatch.StartNew();
        try
        {
            Parallel.Invoke(
                () => rBfs = bfs.Buscar(problema, verbose: false),
                () => rPm  = pm.Buscar(problema, verbose: false));
        }
        finally
        {
            cronometroParalelo.Stop();
        }
        var tiempoParalelo = cronometroParalelo.Elapsed;
        var tiempoSecuencialEstimado = rBfs.Duracion + rPm.Duracion;

        Console.WriteLine($"  {"Métrica",-32} {"BFS (exhaustivo)",18} {"Primero el mejor",18}");
        Console.WriteLine($"  {new string('-', 68)}");
        Console.WriteLine($"  {"¿Encontró la meta?",-32} {(rBfs.Encontro ? "Sí" : "No"),18} {(rPm.Encontro ? "Sí" : "No"),18}");
        Console.WriteLine($"  {"Palpados realizados",-32} {rBfs.Palpados,18} {rPm.Palpados,18}");
        Console.WriteLine($"  {"Nodos expandidos",-32} {rBfs.NodosExpandidos,18} {rPm.NodosExpandidos,18}");
        Console.WriteLine($"  {"Tiempo individual (ms)",-32} {rBfs.Duracion.TotalMilliseconds,18:F2} {rPm.Duracion.TotalMilliseconds,18:F2}");
        Console.WriteLine($"  {"Longitud del trayecto",-32} {(rBfs.Encontro ? rBfs.Camino.Count.ToString() : "-"),18} {(rPm.Encontro ? rPm.Camino.Count.ToString() : "-"),18}");

        Console.WriteLine();
        Console.WriteLine($"  Tiempo paralelo (wall clock): {tiempoParalelo.TotalMilliseconds:F2} ms");
        Console.WriteLine($"  Tiempo secuencial estimado (suma): {tiempoSecuencialEstimado.TotalMilliseconds:F2} ms");
        double ahorroMs = tiempoSecuencialEstimado.TotalMilliseconds - tiempoParalelo.TotalMilliseconds;
        if (ahorroMs > 0.5)
            Console.WriteLine($"  Speedup del paralelismo: {tiempoSecuencialEstimado.TotalMilliseconds / Math.Max(0.01, tiempoParalelo.TotalMilliseconds):F2}x");

        Console.WriteLine();
        if (rBfs.Encontro)
            Console.WriteLine($"  Trayecto BFS:              {string.Join(" → ", rBfs.Camino)}");
        if (rPm.Encontro)
            Console.WriteLine($"  Trayecto Primero el mejor: {string.Join(" → ", rPm.Camino)}");

        Console.WriteLine();
        Console.WriteLine("  Veredicto (por calidad de la búsqueda):");
        if (!rBfs.Encontro && !rPm.Encontro)
            Console.WriteLine("    Ninguno encontró la meta (revisar parámetros / cota L).");
        else if (rBfs.Palpados == rPm.Palpados && rPm.Encontro)
            Console.WriteLine($"    Empate en palpados ({rBfs.Palpados}).");
        else if (rPm.Encontro && (!rBfs.Encontro || rPm.Palpados < rBfs.Palpados))
        {
            int diff = rBfs.Encontro ? rBfs.Palpados - rPm.Palpados : rPm.Palpados;
            double pct = rBfs.Encontro ? 100.0 * diff / rBfs.Palpados : 100.0;
            Console.WriteLine($"    Primero el mejor gana: {diff} palpado(s) menos " +
                              (rBfs.Encontro ? $"(reducción del {pct:F0} %)" : "(BFS no encontró)"));
        }
        else if (rBfs.Encontro)
            Console.WriteLine("    BFS gana (raro: heurístico debería haber sido mejor o igual).");

        Console.WriteLine();
        Console.WriteLine("  Veredicto (por tiempo individual de cómputo):");
        if (rBfs.Duracion < rPm.Duracion)
            Console.WriteLine($"    BFS fue más rápido en cómputo individual ({rBfs.Duracion.TotalMilliseconds:F2} ms vs {rPm.Duracion.TotalMilliseconds:F2} ms).");
        else if (rPm.Duracion < rBfs.Duracion)
            Console.WriteLine($"    Primero el mejor fue más rápido en cómputo individual ({rPm.Duracion.TotalMilliseconds:F2} ms vs {rBfs.Duracion.TotalMilliseconds:F2} ms).");
    }

    private static void MostrarRelieve(Problema problema)
    {
        Console.WriteLine("=== Relieve del bloque y derivación de h(h) ===");
        Console.WriteLine();
        Console.WriteLine($"A = {problema.Meta}, AlturaMax = {problema.AlturaMax}, Pendiente = {problema.Pendiente}");
        Console.WriteLine();
        Console.WriteLine("   h       altura(h)      altura_max − altura(h)      (altura_max − altura) / pendiente = h(h)");
        Console.WriteLine("  ─────   ─────────────   ────────────────────────   ─────────────────────────────────────────");

        int desde = Math.Max(RANGO_MIN, problema.Inicial - problema.Limite);
        int hasta = Math.Min(RANGO_MAX, problema.Inicial + problema.Limite);
        desde = Math.Min(desde, problema.Meta - 3);
        hasta = Math.Max(hasta, problema.Meta + 3);

        for (int h = desde; h <= hasta; h++)
        {
            if (!problema.DentroDeCota(h)) continue;
            double altura = problema.AlturaRelieveEn(h);
            double diferencia = problema.AlturaMax - altura;
            double hHeur = diferencia / problema.Pendiente;
            string marca = (h == problema.Inicial) ? "← B" :
                           (h == problema.Meta)    ? "★ A" : "";
            Console.WriteLine($"   {h,3}       {altura,6:F1}              {diferencia,6:F1}                    {hHeur,6:F1}    {marca}");
        }

        Console.WriteLine();
        Console.WriteLine("Para el modelo lineal del relieve (joroba triangular), h(h) = |h − A|.");
        Console.WriteLine("Como coincide con la distancia real, es ADMISIBLE y CONSISTENTE.");
    }

    private static void LimpiarConsola()
    {
        try { Console.Clear(); } catch { /* puede fallar en entornos redirigidos */ }
    }

    private static void Pausa()
    {
        Console.WriteLine();
        Console.WriteLine("Presione ENTER para volver al menú...");
        Console.ReadLine();
    }

    private static string? ObtenerFlag(string[] args, string flag)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == flag) return args[i + 1];
        return null;
    }

    private static Problema ParsearArgumentos(string[] args, Problema actual)
    {
        int b = ObtenerFlag(args, "--b") is string sb && int.TryParse(sb, out var nb) ? nb : actual.Inicial;
        int a = ObtenerFlag(args, "--a") is string sa && int.TryParse(sa, out var na) ? na : actual.Meta;
        int d = ObtenerFlag(args, "--delta") is string sd && int.TryParse(sd, out var nd) ? nd : actual.DeltaH;
        int l = ObtenerFlag(args, "--l") is string sl && int.TryParse(sl, out var nl) ? nl : actual.Limite;
        double am = ObtenerFlag(args, "--altura") is string sam && double.TryParse(sam,
            NumberStyles.Float, CultureInfo.InvariantCulture, out var nam) ? nam : actual.AlturaMax;
        double pe = ObtenerFlag(args, "--pend") is string spe && double.TryParse(spe,
            NumberStyles.Float, CultureInfo.InvariantCulture, out var npe) ? npe : actual.Pendiente;
        return new Problema(b, a, d, l, am, pe);
    }

    private static void EjecutarDemo(Problema problema)
    {
        Console.WriteLine("=========================================================");
        Console.WriteLine(" TP2 - Búsqueda en el espacio de estados");
        Console.WriteLine(" Prototipo unificado - Modo demo");
        Console.WriteLine("=========================================================");
        Console.WriteLine($"Problema: B={problema.Inicial}, A={problema.Meta}, " +
                          $"ΔH={problema.DeltaH}, L={problema.Limite}");
        Console.WriteLine($"Relieve: AlturaMax={problema.AlturaMax}, Pendiente={problema.Pendiente}");
        Console.WriteLine();
        EjecutarComparacion(problema);
    }
}