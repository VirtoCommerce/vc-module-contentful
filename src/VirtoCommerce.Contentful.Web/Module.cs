using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Contentful.Core;
using VirtoCommerce.Contentful.Core.Services;
using VirtoCommerce.Contentful.Data.ContentProviders;
using VirtoCommerce.Contentful.Data.Services;
using VirtoCommerce.Pages.Core.ContentProviders;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.Contentful.Web;

public class Module : IModule
{
    public ManifestModuleInfo ModuleInfo { get; set; }

    public void Initialize(IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient("Contentful");
        serviceCollection.AddTransient<IContentfulRenderer, ContentfulRenderer>();
        serviceCollection.AddTransient<IContentfulReader, ContentfulReader>();
        serviceCollection.AddTransient<IContentfulApiClient, ContentfulApiClient>();
        serviceCollection.AddTransient<IPageContentProvider, ContentfulContentProvider>();
    }

    public void PostInitialize(IApplicationBuilder appBuilder)
    {
        var serviceProvider = appBuilder.ApplicationServices;

        // Register settings
        var settingsRegistrar = serviceProvider.GetRequiredService<ISettingsRegistrar>();
        settingsRegistrar.RegisterSettings(ContentfulConstants.Settings.AllSettings, ModuleInfo.Id);
        settingsRegistrar.RegisterSettingsForType(ContentfulConstants.Settings.StoreLevelSettings, nameof(Store));

        // Register permissions
        var permissionsRegistrar = serviceProvider.GetRequiredService<IPermissionsRegistrar>();
        permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, "Contentful", ContentfulConstants.Security.Permissions.AllPermissions);

    }

    public void Uninstall()
    {
    }
}
