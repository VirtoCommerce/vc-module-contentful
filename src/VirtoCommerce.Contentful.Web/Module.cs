using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Contentful.Core;
using VirtoCommerce.Contentful.Core.Services;
using VirtoCommerce.Contentful.Data.Services;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.Contentful.Web;

public class Module : IModule
{
    public ManifestModuleInfo ModuleInfo { get; set; }

    public void Initialize(IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IContentfulRenderer, ContentfulRenderer>();
        serviceCollection.AddTransient<IContentfulReader, ContentfulReader>();
    }

    public void PostInitialize(IApplicationBuilder appBuilder)
    {
        var permissionsRegistrar = appBuilder.ApplicationServices.GetRequiredService<IPermissionsRegistrar>();
        permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, "Contentful", ContentfulConstants.Security.Permissions.AllPermissions);
    }

    public void Uninstall()
    {
    }
}
