using System;

namespace Inventario.AplicacionWeb.Utilidades
{
    public static class EstatusBadgeHelper
    {
        public static string ObtenerBadge(int idEstatus, string? nombreEstatus)
        {
            string colorClass;
            string iconClass;
            string nombreSafe = nombreEstatus ?? "Desconocido";

            switch (idEstatus)
            {
                // Cierre / Éxito (Gama Verde)
                case 7:  // FINALIZADO - EN TRAMITE DE PAGO
                case 12: // ENTREGADO
                case 17: // ENVIADA A PAGO
                    colorClass = "badge-estado-success";
                    iconClass = "fa-solid fa-check-double";
                    break;

                // Verificaciones / Autorizaciones (Gama Púrpura/Índigo)
                case 4:  // REQUISICION AUTORIZADA
                case 10: // REQUISICION AUTORIZADA PARCIAL
                case 15: // AUTORIZADA - FINANCIEROS
                case 16: // VERIFICADA - DAF
                    colorClass = "badge-estado-purple";
                    iconClass = "fa-solid fa-user-check";
                    break;

                // Urgencia / Rechazo (Gama Roja/Rosa fuerte)
                case 5:  // REQUISICION RECHAZADA
                case 6:  // REQUISICION CANCELADA
                    colorClass = "badge-estado-danger";
                    iconClass = "fa-solid fa-ban";
                    break;

                // Atención / Corrección (Gama Naranja/Ambar)
                case 3:  // REQUISICION MODIFICACION
                case 18: // DOCUMENTOS OBSERVADOS
                    colorClass = "badge-estado-warning";
                    iconClass = "fa-solid fa-triangle-exclamation";
                    break;

                // En Proceso / Capturada / Cotización (Gama Azul / Info)
                case 1:  // REQUISICION CAPTURADA - ANALISIS
                case 2:  // REQUISICION EN PROCESO - COTIZACION
                case 9:  // REQUISICION EN PROCESO - ALMACEN
                case 11: // EN PROCESO PARA COMPRA
                case 13: // EN PROCESO - PRESUPUESTO
                case 14: // ANALISIS DE PRESUPUESTO
                default:
                    colorClass = "badge-estado-info";
                    iconClass = idEstatus == 1 ? "fa-solid fa-file-signature" : "fa-solid fa-spinner fa-spin-pulse";
                    break;
            }

            return $"<div class=\"badge-estado-premium {colorClass}\" title=\"{nombreEstatus}\"><i class=\"{iconClass}\"></i><span class=\"badge-text\">{nombreEstatus}</span></div>";
        }
    }
}
