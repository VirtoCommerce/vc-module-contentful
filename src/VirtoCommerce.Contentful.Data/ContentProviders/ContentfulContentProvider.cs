using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Contentful.Core;
using Contentful.Core.Configuration;
using Contentful.Core.Models;
using Contentful.Core.Search;
using VirtoCommerce.Contentful.Core;
using VirtoCommerce.Contentful.Core.Models;
using VirtoCommerce.Pages.Core.ContentProviders;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.Contentful.Data.ContentProviders;

public class ContentfulContentProvider(
    IHttpClientFactory httpClientFactory,
    IStoreService storeService,
    ISettingsManager settingsManager)
    : IPageContentProvider
{
    private const int PageSize = 100;

    public string ProviderName => "Contentful";
    public bool SupportsReindexation => true;

    public async Task<long> GetTotalChangesCountAsync(DateTime? startDate, DateTime? endDate)
    {
        long totalCount = 0;

        await ForEachStoreAsync(async (client, contentTypeId, _, _) =>
        {
            var queryBuilder = new QueryBuilder<ContentfulEntry>()
                .ContentTypeIs(contentTypeId)
                .Limit(0);

            if (startDate.HasValue)
            {
                queryBuilder.FieldGreaterThan("sys.updatedAt", startDate.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssK"));
            }

            var result = await client.GetEntries(queryBuilder);
            totalCount += result.Total;
        });

        return totalCount;
    }

    public async Task<IList<IndexDocumentChange>> GetChangesAsync(DateTime? startDate, DateTime? endDate, long skip, long take)
    {
        var allChanges = new List<IndexDocumentChange>();

        await ForEachStoreAsync(async (client, contentTypeId, _, _) =>
        {
            var offset = 0;
            while (true)
            {
                var queryBuilder = new QueryBuilder<ContentfulEntry>()
                    .ContentTypeIs(contentTypeId)
                    .OrderBy("sys.updatedAt")
                    .Skip(offset)
                    .Limit(PageSize);

                if (startDate.HasValue)
                {
                    queryBuilder.FieldGreaterThan("sys.updatedAt", startDate.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssK"));
                }

                var result = await client.GetEntries(queryBuilder);

                foreach (var entry in result)
                {
                    allChanges.Add(new IndexDocumentChange
                    {
                        DocumentId = entry.SystemProperties.Id,
                        ChangeDate = entry.SystemProperties.UpdatedAt ?? entry.SystemProperties.CreatedAt ?? DateTime.UtcNow,
                        ChangeType = IndexDocumentChangeType.Modified,
                    });
                }

                offset += PageSize;
                if (offset >= result.Total || !result.Any())
                {
                    break;
                }
            }
        });

        return allChanges
            .OrderByDescending(x => x.ChangeDate)
            .Skip(Convert.ToInt32(skip))
            .Take(Convert.ToInt32(take))
            .ToList();
    }

    public async Task<IList<PageDocument>> GetByIdsAsync(IList<string> ids)
    {
        var result = new List<PageDocument>();

        await ForEachStoreAsync(async (client, contentTypeId, storeId, defaultLocale) =>
        {
            foreach (var id in ids)
            {
                try
                {
                    var entry = await client.GetEntry<ContentfulEntry>(id);

                    if (entry?.SystemProperties?.ContentType?.SystemProperties?.Id?.StartsWith(ContentfulConstants.PageContentTypePrefix) != true)
                    {
                        continue;
                    }

                    // Process each locale
                    var locales = entry.Fields.Values
                        .SelectMany(f => f.Keys)
                        .Distinct()
                        .ToList();

                    var cultureName = locales.FirstOrDefault() ?? defaultLocale;

                    entry.CultureName = cultureName;
                    var pageDocument = entry.ToPageDocument();
                    pageDocument.StoreId = storeId;
                    result.Add(pageDocument);
                }
                catch
                {
                    // Entry not found or not accessible — skip
                }
            }
        });

        return result;
    }

    private async Task ForEachStoreAsync(Func<ContentfulClient, string, string, string, Task> action)
    {
        var stores = await storeService.GetAsync([], null, clone: false);
        if (stores == null)
        {
            return;
        }

        foreach (var store in stores)
        {
            var spaceId = await settingsManager.GetValueAsync<string>(ContentfulConstants.Settings.General.SpaceId);
            var apiKey = await settingsManager.GetValueAsync<string>(ContentfulConstants.Settings.General.DeliveryApiKey);
            var contentTypeId = await settingsManager.GetValueAsync<string>(ContentfulConstants.Settings.General.ContentTypeId);

            if (string.IsNullOrEmpty(spaceId) || string.IsNullOrEmpty(apiKey))
            {
                continue;
            }

            var httpClient = httpClientFactory.CreateClient("Contentful");
            var options = new ContentfulOptions
            {
                SpaceId = spaceId,
                DeliveryApiKey = apiKey,
            };
            var client = new ContentfulClient(httpClient, options);
            var defaultLocale = store.DefaultLanguage ?? "en-US";

            await action(client, contentTypeId ?? "page", store.Id, defaultLocale);
        }
    }
}
