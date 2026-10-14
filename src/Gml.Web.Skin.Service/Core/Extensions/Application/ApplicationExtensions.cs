using Gml.Web.Skin.Service.Core.Requests;

namespace Gml.Web.Skin.Service.Core.Extensions.Application;

public static class ApplicationExtensions
{
    private static string _policyName = "SkinServicePolicy";

    public static WebApplicationBuilder CreateService(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddAntiforgery();
        builder.Services.AddMapping();

        builder.Services
            .AddCors(o => o.AddPolicy(_policyName, policyBuilder =>
            {
                policyBuilder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }));

        CheckFolders();

        return builder;
    }

    private static void CheckFolders()
    {
        if (!Directory.Exists(SkinHelper.SkinTextureDirectory))
            Directory.CreateDirectory(SkinHelper.SkinTextureDirectory);

        if (!Directory.Exists(SkinHelper.CloakTextureDirectory))
            Directory.CreateDirectory(SkinHelper.CloakTextureDirectory);
    }

    public static WebApplication Run(this WebApplicationBuilder builder)
    {
        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseCors(_policyName);
        // app.UseHttpsRedirection();
        app.AddRoutes();
        app.UseAntiforgery();

        app.Run();

        return app;
    }

    private static WebApplication AddRoutes(this WebApplication app)
    {
        app.MapGet("/{userName}", TextureRequests.GetUserTexture);

        app.MapPost("/skin/{userName}", TextureRequests.LoadSkin).DisableAntiforgery();
        app.MapDelete("/skin/{userName}", TextureRequests.DeleteSkin);
        app.MapGet("/skin/{userName}/{uuid?}", TextureRequests.GetSkin);
        app.MapGet("/skin/{userName}/head/{size}", TextureRequests.GetSkinHead);
        app.MapGet("/skin/{userName}/front/{size}", TextureRequests.GetSkinFront);
        app.MapGet("/skin/{userName}/back/{size}", TextureRequests.GetSkinBack);
        app.MapGet("/skin/{userName}/full-back/{size}", TextureRequests.GetSkinAndCloakBack);

        app.MapGet("/cloak/{userName}", TextureRequests.GetCloakTexture);
        app.MapPost("/cloak/{userName}", TextureRequests.LoadCloak).DisableAntiforgery();
        app.MapDelete("/cloak/{userName}", TextureRequests.DeleteCloak);
        app.MapGet("/cloak/{userName}/front/{size}", TextureRequests.GetCloak);

        app.MapGet("/refresh/{userName}", TextureRequests.RefreshCache);

        return app;
    }
}
