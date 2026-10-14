using Mapster;
using MapsterMapper;

namespace Gml.Web.Skin.Service.Core.Extensions.Application;

public static class MappingExtensions
{
    public static IServiceCollection AddMapping(this IServiceCollection services)
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(Gml.Web.Skin.Service.Core.Mapper.TextureMapper).Assembly);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        return services;
    }
}
