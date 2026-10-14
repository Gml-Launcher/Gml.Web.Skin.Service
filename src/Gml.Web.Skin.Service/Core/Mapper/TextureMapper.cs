using Mapster;
using Gml.Web.Skin.Service.Models;
using Gml.Web.Skin.Service.Models.Dto;

namespace Gml.Web.Skin.Service.Core.Mapper;

public class TextureMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UserTexture, UserTextureReadDto>();
    }
}
