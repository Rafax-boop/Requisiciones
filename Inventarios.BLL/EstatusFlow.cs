namespace Inventario.BLL;

/// <summary>
/// Constantes compartidas del flujo de estatus de requisiciones.
/// Fuente única de verdad para ids terminales y clasificación de cierre.
/// </summary>
public static class EstatusFlow
{
    public static readonly HashSet<int> TerminalNegativos = new() { 5, 6 };
    public static readonly HashSet<int> TerminalPositivos = new() { 7, 12 };

    public static bool EsTerminal(int idEstatus)
        => TerminalNegativos.Contains(idEstatus) || TerminalPositivos.Contains(idEstatus);

    public static bool EsTerminalNegativo(int idEstatus)
        => TerminalNegativos.Contains(idEstatus);
}
