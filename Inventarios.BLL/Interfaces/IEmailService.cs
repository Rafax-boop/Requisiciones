using Inventario.BLL.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Interfaces
{
    public interface IEmailService
    {
        Task EnviarAsync(EmailNotificacionDTO notificacion);

        Task NotificarProductosListosEntregaAsync(
            string correoDestinatario,
            string numRequisicion,
            string departamento,
            string responsable,
            List<string> articulosEntrega,
            List<string>? articulosCompra = null);

        Task NotificarPedidoRecibidoParcialAsync(
            string correoDestinatario,
            string numRequisicion,
            List<string> recibidos,
            List<string> faltantes);
    }
}
