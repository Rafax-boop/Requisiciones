using Inventario.BLL;

namespace Inventario.AplicacionWeb.Utilidades;

/// <summary>
/// Mapa IdEstatus → fase compacta (1-5) para la barra de timeline en fila.
/// Fase 1: Capturada | Fase 2: Cotización/Presupuesto | Fase 3: Trámite operativo
/// Fase 4: Autorización | Fase 5: Cierre (entregado/finalizado)
/// </summary>
public static class TimelineHelper
{
    private static readonly Dictionary<int, int> FaseMap = new()
    {
        [1]  = 1,
        [2]  = 2,
        [13] = 2,
        [14] = 2,
        [3]  = 3,
        [9]  = 3,
        [11] = 3,
        [18] = 3,
        [4]  = 4,
        [10] = 4,
        [15] = 4,
        [16] = 4,
        [12] = 5,
        [7]  = 5,
        [17] = 5,
    };

    public static (int fase, bool esTerminal, bool esMod, int progress) Calcular(int idEstatus)
    {
        bool esTerminal = EstatusFlow.EsTerminal(idEstatus);
        bool esMod = idEstatus == 3 || idEstatus == 18;
        int fase = esTerminal ? 1 : FaseMap.GetValueOrDefault(idEstatus, 1);

        int progress = fase switch
        {
            5 => 100,
            4 => 75,
            3 => 50,
            2 => 25,
            _ => 0
        };

        return (fase, esTerminal, esMod, progress);
    }
}
