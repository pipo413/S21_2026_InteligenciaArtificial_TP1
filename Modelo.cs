// Modelo.cs
// Representación formal del problema de búsqueda: P = (I, O, M).
// Cada estado es la coordenada horizontal h del brazo del robot.

namespace TP2_Prototipo_Unificado;

/// <summary>Operadores disponibles: avanzar un incremento ΔH a izquierda o derecha.</summary>
public enum Operador
{
    Izquierda,
    Derecha
}

public static class OperadorExtensions
{
    /// <summary>Aplica el operador a una posición, devolviendo la nueva posición.</summary>
    public static int Aplicar(this Operador op, int posicion, int deltaH) =>
        op == Operador.Izquierda ? posicion - deltaH : posicion + deltaH;

    /// <summary>Devuelve el símbolo ASCII del operador (← o →).</summary>
    public static string Simbolo(this Operador op) =>
        op == Operador.Izquierda ? "←" : "→";

    /// <summary>Devuelve los dos operadores en el orden canónico (Izquierda, Derecha).</summary>
    public static Operador[] EnOrdenCanónico() => new[] { Operador.Izquierda, Operador.Derecha };
}

/// <summary>Estado del sistema: coordenada horizontal h del brazo del robot.</summary>
public sealed record Estado(int Posicion);

/// <summary>
/// Problema tal como se define en la Lectura 1, Lección 3:
/// P = (I, O, M), más el incremento ΔH, la cota L y los parámetros del relieve.
/// </summary>
public sealed record Problema(
    int Inicial,
    int Meta,
    int DeltaH = 1,
    int Limite = 6,
    double AlturaMax = 10,
    double Pendiente = 1
)
{
    /// <summary>
    /// Altura del relieve en h (joroba centrada en A):
    /// altura(h) = max(0, AlturaMax − Pendiente · |h − A|)
    /// </summary>
    public double AlturaRelieveEn(int posicion)
    {
        int distancia = Math.Abs(posicion - Meta);
        return Math.Max(0.0, AlturaMax - Pendiente * distancia);
    }

    /// <summary>Verifica que una posición esté dentro del dominio permitido (cota L respecto de B).</summary>
    public bool DentroDeCota(int posicion) =>
        Math.Abs(posicion - Inicial) <= Limite;
}

/// <summary>Resultado de un palpado: ¿estoy en A? + altura del relieve medida.</summary>
public sealed record Palpado(bool EsMeta, double AlturaRelieve);