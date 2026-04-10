using Newtonsoft.Json.Linq;
using VirtoCommerce.Contentful.Core;
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
    IContentfulApiClient apiClient,
    IStoreSearchService storeSearchService,
    ISettingsManager settingsManager,
    IContentfulRenderer contentfulRenderer)
    : IPageContentProvider
{
    private const int PageSize = 100;
    private const string StoreObjectType = "Store";

    public string ProviderName => "Contentful";
    public bool SupportsReindexation => true;

    public async Task<long> GetTotalChangesCountAsync(DateTime? startDate, DateTime? endDate)
    {
        long totalCount = 0;
        var processedSpaces = new HashSet<string>();

        await ForEachStoreAsync(async (request, _, _) =>
        {
            if (!processedSpaces.Add($"{request.SpaceId}:{request.ContentTypeId}"))
            {
                return;
            }

            request.Limit = 0;
            request.UpdatedAfter = startDate;
            request.UpdatedBefore = endDate;

            var response = await apiClient.GetEntriesAsync(request);
            totalCount += response.Total;
        });

        return totalCount;
    }

    public async Task<IList<IndexDocumentChange>> GetChangesAsync(DateTime? startDate, DateTime? endDate, long skip, long take)
    {
        var allChanges = new List<IndexDocumentChange>();
        var processedSpaces = new HashSet<string>();

        await ForEachStoreAsync(async (request, _, _) =>
        {
            if (!processedSpaces.Add($"{request.SpaceId}:{request.ContentTypeId}"))
            {
                return;
            }

            var offset = 0;
            while (true)
            {
                request.Limit = PageSize;
                request.Skip = offset;
                request.UpdatedAfter = startDate;
                request.UpdatedBefore = endDate;

                var response = await apiClient.GetEntriesAsync(request);

                allChanges.AddRange(response.Items.Select(item => new IndexDocumentChange
                {
                    DocumentId = item.SelectToken("sys.id")?.ToString(),
                    ChangeDate = item.SelectToken("sys.updatedAt")?.ToObject<DateTime>() ?? DateTime.UtcNow,
                    ChangeType = IndexDocumentChangeType.Modified,
                }));

                offset += PageSize;
                if (offset >= response.Total || response.Items.Count == 0)
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

        await ForEachStoreAsync(async (request, storeId, defaultLocale) =>
        {
            var remainingIds = ids.Where(id => !processedIds.Contains(id)).ToList();
            if (remainingIds.Count == 0)
            {
                return;
            }

            var response = await apiClient.GetEntriesByIdsAsync(request, remainingIds);

            foreach (var item in response.Items)
            {
                var entryId = item.SelectToken("sys.id")?.ToString();
                if (entryId == null || !processedIds.Add(entryId))
                {
                    continue;
                }

                var pageDocument = await ConvertItemToPageDocumentAsync(item, storeId, defaultLocale);
                result.Add(pageDocument);
            }
        });

        return result;
    }

    private async Task<PageDocument> ConvertItemToPageDocumentAsync(JObject item, string storeId, string defaultLocale)
    {
        var fields = item["fields"] as JObject;
        var cultureName = DetectLocale(fields) ?? defaultLocale;

        var pageDocument = AbstractTypeFactory<PageDocument>.TryCreateInstance();
        pageDocument.Id = item.SelectToken("sys.id")?.ToString();
        pageDocument.OuterId = pageDocument.Id;
        pageDocument.CreatedDate = item.SelectToken("sys.createdAt")?.ToObject<DateTime>() ?? DateTime.UtcNow;
        pageDocument.ModifiedDate = item.SelectToken("sys.updatedAt")?.ToObject<DateTime>();
        pageDocument.Source = "contentful";
        pageDocument.MimeType = "text/html";

        // Preview API returns drafts; Delivery API returns only published
        var publishedVersion = item.SelectToken("sys.publishedVersion");
        pageDocument.Status = publishedVersion != null ? PageDocumentStatus.Published : PageDocumentStatus.Draft;

        pageDocument.Title = GetLocalizedField(fields, "title", cultureName);
        pageDocument.Description = GetLocalizedField(fields, "description", cultureName);
        pageDocument.Permalink = GetLocalizedField(fields, "permalink", cultureName);
        pageDocument.StoreId = GetLocalizedField(fields, "storeId", cultureName);
        pageDocument.CultureName = GetLocalizedField(fields, "cultureName", cultureName) ?? cultureName;

        var isPublic = GetLocalizedField(fields, "isAuthenticated", cultureName);
        var isPrivate = !string.Equals(isPublic, "false", StringComparison.OrdinalIgnoreCase);
        pageDocument.Visibility = isPrivate ? PageDocumentVisibility.Private : PageDocumentVisibility.Public;

        var userGroupsToken = GetLocalizedToken(fields, "userGroups", cultureName);
        pageDocument.UserGroups = userGroupsToken?.ToObject<string[]>();

        pageDocument.StartDate = GetLocalizedToken(fields, "startDate", cultureName)?.ToObject<DateTime?>() ?? DateTime.MinValue;
        pageDocument.EndDate = GetLocalizedToken(fields, "endDate", cultureName)?.ToObject<DateTime?>() ?? DateTime.MaxValue;

        // Render rich text content
        var contentToken = GetLocalizedToken(fields, "content", cultureName);
        if (contentToken != null)
        {
            pageDocument.Content = await contentfulRenderer.RenderContent(contentToken.ToString());
        }

        if (pageDocument.StoreId.IsNullOrEmpty())
        {
            pageDocument.StoreId = storeId;
        }

        return pageDocument;
    }

    private static string DetectLocale(JObject fields)
    {
        return fields?.Properties()
            .Select(p => (p.Value as JObject)?.Properties().FirstOrDefault()?.Name)
            .FirstOrDefault(name => name != null);
    }

    private static string GetLocalizedField(JObject fields, string fieldName, string locale)
    {
        return GetLocalizedToken(fields, fieldName, locale)?.ToString();
    }

    private static JToken GetLocalizedToken(JObject fields, string fieldName, string locale)
    {
        var field = fields?[fieldName] as JObject;
        return field?[locale];
    }

    private async Task ForEachStoreAsync(Func<ContentfulQueryRequest, string, string, Task> action)
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

    private async Task ProcessStoresAsync(IList<Store> stores, Func<ContentfulQueryRequest, string, string, Task> action)
    {
        foreach (var store in stores)
        {
            var spaceIdSetting = await settingsManager.GetObjectSettingAsync(ContentfulConstants.Settings.General.SpaceId.Name, StoreObjectType, store.Id);
            var deliveryKeySetting = await settingsManager.GetObjectSettingAsync(ContentfulConstants.Settings.General.DeliveryApiKey.Name, StoreObjectType, store.Id);
            var contentTypeIdSetting = await settingsManager.GetObjectSettingAsync(ContentfulConstants.Settings.General.ContentTypeId.Name, StoreObjectType, store.Id);
            var previewKeySetting = await settingsManager.GetObjectSettingAsync(ContentfulConstants.Settings.General.PreviewApiKey.Name, StoreObjectType, store.Id);

            var spaceId = spaceIdSetting?.Value as string;
            var deliveryKey = deliveryKeySetting?.Value as string;
            var previewKey = previewKeySetting?.Value as string;
            var contentTypeId = contentTypeIdSetting?.Value as string;

            if (string.IsNullOrEmpty(spaceId) || string.IsNullOrEmpty(deliveryKey))
            {
                continue;
            }

            // Use Preview API when token is configured (returns drafts + published)
            var usePreview = !string.IsNullOrEmpty(previewKey);

            var request = new ContentfulQueryRequest
            {
                SpaceId = spaceId,
                AccessToken = usePreview ? previewKey : deliveryKey,
                ContentTypeId = string.IsNullOrEmpty(contentTypeId) ? ContentfulConstants.PageContentTypePrefix : contentTypeId,
                UsePreviewApi = usePreview,
            };

            var defaultLocale = store.DefaultLanguage ?? "en-US";

            await action(request, store.Id, defaultLocale);
        }
    }
}
