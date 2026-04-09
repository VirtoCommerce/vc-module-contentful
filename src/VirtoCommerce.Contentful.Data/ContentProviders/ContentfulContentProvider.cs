using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Contentful.Core;
using Contentful.Core.Configuration;
using Contentful.Core.Search;
using VirtoCommerce.Contentful.Core;
using VirtoCommerce.Contentful.Core.Models;
using VirtoCommerce.Contentful.Core.Services;
using VirtoCommerce.Pages.Core.ContentProviders;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Model.Search;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.Contentful.Data.ContentProviders;

public class ContentfulContentProvider(
    IHttpClientFactory httpClientFactory,
    IStoreSearchService storeSearchService,
    ISettingsManager settingsManager,
    IContentfulRenderer contentfulRenderer)
    : IPageContentProvider
{
    private const int PageSize = 100;

    public string ProviderName => "Contentful";
    public bool SupportsReindexation => true;

    public async Task<long> GetTotalChangesCountAsync(DateTime? startDate, DateTime? endDate)
    {
        long totalCount = 0;
        var processedSpaces = new HashSet<string>();

        await ForEachStoreAsync(async (client, contentTypeId, _, _, spaceId) =>
        {
            if (!processedSpaces.Add($"{spaceId}:{contentTypeId}"))
            {
                return;
            }

            var queryBuilder = new QueryBuilder<ContentfulEntry>()
                .ContentTypeIs(contentTypeId)
                .LocaleIs("*")
                .Limit(0);

            AddDateFilters(queryBuilder, startDate, endDate);

            var result = await client.GetEntries(queryBuilder);
            totalCount += result.Total;
        });

        return totalCount;
    }

    public async Task<IList<IndexDocumentChange>> GetChangesAsync(DateTime? startDate, DateTime? endDate, long skip, long take)
    {
        var allChanges = new List<IndexDocumentChange>();
        var processedSpaces = new HashSet<string>();

        await ForEachStoreAsync(async (client, contentTypeId, _, _, spaceId) =>
        {
            if (!processedSpaces.Add($"{spaceId}:{contentTypeId}"))
            {
                return;
            }

            var offset = 0;
            while (true)
            {
                var queryBuilder = new QueryBuilder<ContentfulEntry>()
                    .ContentTypeIs(contentTypeId)
                    .LocaleIs("*")
                    .OrderBy("sys.updatedAt")
                    .Skip(offset)
                    .Limit(PageSize);

                AddDateFilters(queryBuilder, startDate, endDate);

                var result = await client.GetEntries(queryBuilder);

                allChanges.AddRange(result.Select(entry => new IndexDocumentChange
                {
                    DocumentId = entry.SystemProperties.Id,
                    ChangeDate = entry.SystemProperties.UpdatedAt ?? entry.SystemProperties.CreatedAt ?? DateTime.UtcNow,
                    ChangeType = IndexDocumentChangeType.Modified,
                }));

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
        var processedIds = new HashSet<string>();

        await ForEachStoreAsync(async (client, contentTypeId, storeId, defaultLocale, _) =>
        {
            foreach (var id in ids)
            {
                if (processedIds.Contains(id))
                {
                    continue;
                }

                var pageDocument = await TryGetPageDocumentAsync(client, id, contentTypeId, storeId, defaultLocale);
                if (pageDocument != null)
                {
                    processedIds.Add(id);
                    result.Add(pageDocument);
                }
            }
        });

        return result;
    }

    private async Task<PageDocument> TryGetPageDocumentAsync(
        ContentfulClient client, string id, string contentTypeId, string storeId, string defaultLocale)
    {
        try
        {
            var entry = await client.GetEntry<ContentfulEntry>(id, queryString: "locale=*");
            var entryContentTypeId = entry?.SystemProperties?.ContentType?.SystemProperties?.Id;

            if (entryContentTypeId == null || !entryContentTypeId.Equals(contentTypeId, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var cultureName = entry.Fields.Values
                .SelectMany(f => f.Keys)
                .Distinct()
                .FirstOrDefault() ?? defaultLocale;

            entry.CultureName = cultureName;
            var pageDocument = entry.ToPageDocument();

            if (entry.Fields.TryGetValue("content", out var contentField) &&
                contentField.TryGetValue(cultureName, out var contentJson))
            {
                pageDocument.Content = await contentfulRenderer.RenderContent(contentJson?.ToString());
            }

            if (pageDocument.StoreId.IsNullOrEmpty())
            {
                pageDocument.StoreId = storeId;
            }

            return pageDocument;
        }
        catch (global::Contentful.Core.Errors.ContentfulException ex) when (ex.StatusCode == 404)
        {
            // Entry not found in this space — will retry with next store
            return null;
        }
    }

    private static void AddDateFilters(QueryBuilder<ContentfulEntry> queryBuilder, DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue)
        {
            queryBuilder.FieldGreaterThan("sys.updatedAt", startDate.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssK"));
        }

        if (endDate.HasValue)
        {
            queryBuilder.FieldLessThan("sys.updatedAt", endDate.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssK"));
        }
    }

    private async Task ForEachStoreAsync(Func<ContentfulClient, string, string, string, string, Task> action)
    {
        const int storeBatchSize = 50;
        var criteria = AbstractTypeFactory<StoreSearchCriteria>.TryCreateInstance();
        criteria.Take = storeBatchSize;
        criteria.Skip = 0;

        int totalStores;
        do
        {
            var storesResult = await storeSearchService.SearchAsync(criteria);
            totalStores = storesResult.TotalCount;

            await ProcessStoresAsync(storesResult.Results, action);

            criteria.Skip += storeBatchSize;
        }
        while (criteria.Skip < totalStores);
    }

    private async Task ProcessStoresAsync(IList<Store> stores, Func<ContentfulClient, string, string, string, string, Task> action)
    {
        foreach (var store in stores)
        {
            var spaceIdSetting = await settingsManager.GetObjectSettingAsync(ContentfulConstants.Settings.General.SpaceId.Name, "Store", store.Id);
            var apiKeySetting = await settingsManager.GetObjectSettingAsync(ContentfulConstants.Settings.General.DeliveryApiKey.Name, "Store", store.Id);
            var contentTypeIdSetting = await settingsManager.GetObjectSettingAsync(ContentfulConstants.Settings.General.ContentTypeId.Name, "Store", store.Id);

            var spaceId = spaceIdSetting?.Value as string;
            var apiKey = apiKeySetting?.Value as string;
            var contentTypeId = contentTypeIdSetting?.Value as string;

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

            await action(client, string.IsNullOrEmpty(contentTypeId) ? ContentfulConstants.PageContentTypePrefix : contentTypeId, store.Id, defaultLocale, spaceId);
        }
    }
}
