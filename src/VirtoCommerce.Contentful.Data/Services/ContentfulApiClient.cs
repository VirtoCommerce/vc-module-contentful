using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using VirtoCommerce.Contentful.Core.Services;

namespace VirtoCommerce.Contentful.Data.Services;

public class ContentfulApiClient(IHttpClientFactory httpClientFactory) : IContentfulApiClient
{
    private const string BaseUrl = "https://cdn.contentful.com";

    public async Task<ContentfulQueryResponse> GetEntriesAsync(
        string spaceId,
        string accessToken,
        string contentTypeId,
        int limit,
        int skip,
        DateTime? updatedAfter = null,
        DateTime? updatedBefore = null)
    {
        var queryParams = new List<string>
        {
            $"content_type={Uri.EscapeDataString(contentTypeId)}",
            "locale=*",
            $"limit={limit}",
            $"skip={skip}",
            "order=sys.updatedAt",
        };

        if (updatedAfter.HasValue)
        {
            queryParams.Add($"sys.updatedAt[gte]={updatedAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}");
        }

        if (updatedBefore.HasValue)
        {
            queryParams.Add($"sys.updatedAt[lte]={updatedBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}");
        }

        return await FetchEntriesAsync(spaceId, accessToken, queryParams);
    }

    public async Task<ContentfulQueryResponse> GetEntriesByIdsAsync(
        string spaceId,
        string accessToken,
        string contentTypeId,
        IList<string> ids)
    {
        var idsValue = string.Join(",", ids);
        var queryParams = new List<string>
        {
            $"content_type={Uri.EscapeDataString(contentTypeId)}",
            "locale=*",
            $"sys.id[in]={Uri.EscapeDataString(idsValue)}",
            $"limit={ids.Count}",
        };

        return await FetchEntriesAsync(spaceId, accessToken, queryParams);
    }

    private async Task<ContentfulQueryResponse> FetchEntriesAsync(string spaceId, string accessToken, List<string> queryParams)
    {
        var url = $"{BaseUrl}/spaces/{Uri.EscapeDataString(spaceId)}/entries?{string.Join("&", queryParams)}";

        var client = httpClientFactory.CreateClient("Contentful");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = JObject.Parse(await response.Content.ReadAsStringAsync());
        var items = json["items"]?.ToObject<List<JObject>>() ?? [];
        var total = json["total"]?.Value<int>() ?? items.Count;

        return new ContentfulQueryResponse
        {
            Items = items,
            Total = total,
        };
    }
}
