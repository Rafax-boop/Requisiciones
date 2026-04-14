using System.Threading;
using System.Threading.Tasks;

namespace Inventario.BLL.Interfaces
{
    public interface ICuadroComparativoPdfService
    {
        Task<(byte[] PdfBytes, string FileName)?> GenerarAsync(
            int idRequisicion,
            string webRootPath,
            CancellationToken cancellationToken = default);
    }
}
