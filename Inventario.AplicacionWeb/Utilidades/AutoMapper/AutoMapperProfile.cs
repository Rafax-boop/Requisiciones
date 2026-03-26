using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;


namespace Inventario.AplicacionWeb.Utilidades.AutoMapper
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<VMRequiForm, FormularioRequisicionDTO>().ReverseMap();

            CreateMap<ItemRequiVM, FormularioRequisicionDetalleDTO>();

            CreateMap<RequisicionMaestraDTO, VMRequisicionMaestra>();

            CreateMap<InventarioItemDTO, VMInventarioItem>();

            CreateMap<AtenderRequiDTO, VMAtenderRequisicion>();
        }
    }
}
