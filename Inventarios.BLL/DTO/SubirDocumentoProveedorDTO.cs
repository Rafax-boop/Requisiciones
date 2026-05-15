using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.DTO
{
    public class SubirDocumentoProveedorDTO
    {
        public int IdRequisicion { get; set; }
        public string TipoDocumento { get; set; } = "";
        public IFormFile Archivo { get; set; } = null!;
    }

    public class RebotarDocumentosDTO
    {
        public int IdRequisicion { get; set; }
        public string Observaciones { get; set; } = "";
        public List<string> DocumentosObservados { get; set; } = new();
    }

    public class RebotarDocumentosConsolidadaDTO
    {
        public int IdConsolidada { get; set; }
        public string Observaciones { get; set; } = "";
        public List<string> DocumentosObservados { get; set; } = new();
    }
}
