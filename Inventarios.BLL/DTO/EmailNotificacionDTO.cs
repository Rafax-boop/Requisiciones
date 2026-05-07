using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.DTO
{
    public class EmailNotificacionDTO
    {
        public string Para { get; set; } = "";
        public string Asunto { get; set; } = "";
        public string CuerpoHtml { get; set; } = "";
        public List<string>? CC { get; set; }
    }
}
