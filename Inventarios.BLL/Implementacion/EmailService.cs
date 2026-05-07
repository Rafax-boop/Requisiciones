using Inventario.BLL.DTO;
using Inventario.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Implementacion
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private string Host => _config["Email:Host"] ?? "";
        private int Port => int.Parse(_config["Email:Port"] ?? "587");
        private string Usuario => _config["Email:Usuario"] ?? "";
        private string Password => _config["Email:Password"] ?? "";
        private string Remitente => _config["Email:Remitente"] ?? Usuario;
        private string NombreRemitente => _config["Email:NombreRemitente"] ?? "Sistema de Requisiciones";

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task EnviarAsync(EmailNotificacionDTO dto)
        {
            using var client = new SmtpClient(Host, Port)
            {
                Credentials = new NetworkCredential(Usuario, Password),
                EnableSsl = true
            };

            using var mensaje = new MailMessage
            {
                From = new MailAddress(Remitente, NombreRemitente),
                Subject = dto.Asunto,
                Body = dto.CuerpoHtml,
                IsBodyHtml = true
            };

            mensaje.To.Add(dto.Para);
            dto.CC?.ForEach(cc => mensaje.CC.Add(cc));

            await client.SendMailAsync(mensaje);
        }

        // ─── Notificaciones semánticas ───────────────────────────────

        public async Task NotificarProductosListosEntregaAsync(
            string correoDestinatario,
            string numRequisicion,
            string departamento,
            string responsable,
            List<string> articulosEntrega,
            List<string>? articulosCompra = null)
        {
            bool hayCompras = articulosCompra != null && articulosCompra.Count > 0;

            var asunto = hayCompras
                ? $"Requisición {numRequisicion} — Procesada (entrega parcial + compra)"
                : $"Requisición {numRequisicion} — Todo listo para entrega";

            var itemsEntrega = string.Join("", articulosEntrega
                .Select(a => $"<li style='margin:4px 0'>{a}</li>"));

            var seccionCompras = hayCompras ? $"""
                    <h3 style="color:#e65100">Enviados a compra</h3>
                    <ul>
                      {string.Join("", articulosCompra!.Select(a => $"<li style='margin:4px 0'>{a}</li>"))}
                    </ul>
                    <p style="color:#666;font-size:13px">
                      Estos artículos se notificarán cuando lleguen del proveedor.
                    </p>
                """ : "";

            var dto = new EmailNotificacionDTO
            {
                Para = correoDestinatario,
                Asunto = asunto,
                CuerpoHtml = $"""
                    <div style="font-family:Arial,sans-serif;max-width:600px">
                      <h2 style="color:#2e7d32">
                        {(hayCompras ? "Requisición procesada" : "Materiales listos para entrega")}
                      </h2>
                      <p>Estimado/a <strong>{responsable}</strong>,</p>
                      <p>La requisición <strong>{numRequisicion}</strong> 
                         del departamento <strong>{departamento}</strong> ha sido procesada.</p>
                      <h3 style="color:#2e7d32">Listos para recoger en almacén</h3>
                      <ul>{itemsEntrega}</ul>
                      {seccionCompras}
                      <hr/>
                      <p style="font-size:12px;color:#666">Sistema de Requisiciones — notificación automática</p>
                    </div>
                    """
                    };

            await EnviarAsync(dto);
        }

        public async Task NotificarPedidoRecibidoParcialAsync(
            string correoDestinatario,
            string numRequisicion,
            List<string> recibidos,
            List<string> faltantes)
        {
            bool esCompleto = faltantes.Count == 0;

            var asunto = esCompleto
                ? $"Requisición {numRequisicion} — Ingreso completo de materiales"
                : $"Requisición {numRequisicion} — Ingreso parcial de pedido";

            var items = string.Join("", recibidos.Select(r => $"<li style='margin:4px 0'>{r}</li>"));
            var itemsFaltantes = string.Join("", faltantes.Select(f => $"<li style='color:#c62828;margin:4px 0'>{f}</li>"));

            var cuerpoFaltantes = esCompleto ? "" : $"""
                    <h3 style="color:#c62828">Pendientes del proveedor</h3>
                    <ul>{itemsFaltantes}</ul>
                """;

            var dto = new EmailNotificacionDTO
            {
                Para = correoDestinatario,
                Asunto = asunto,
                CuerpoHtml = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px">
              <h2 style="color:#1565c0">
                {(esCompleto ? "Ingreso completo de materiales" : "Ingreso parcial de materiales")}
              </h2>
              <p>Se registró el ingreso para la requisición <strong>{numRequisicion}</strong>.</p>
              <h3 style="color:#2e7d32">Recibidos</h3>
              <ul>{items}</ul>
              {cuerpoFaltantes}
              <hr/>
              <p style="font-size:12px;color:#666">Sistema de Requisiciones — notificación automática</p>
            </div>
            """
            };

            await EnviarAsync(dto);
        }
    }
}
