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
#pragma warning disable S1075 // Contentful API base URLs are fixed
    private const string DeliveryBaseUrl = "https://cdn.contentful.com";
    private const string PreviewBaseUrl = "https://preview.contentful.com";
#pragma warning restore S1075

    public async Task<ContentfulQueryResponse> GetEntriesAsync(ContentfulQueryRequest request)
    {
        var queryParams = new List<string>
        {
            $"content_type={Uri.EscapeDataString(request.ContentTypeId)}",
            "locale=*",
            $"limit={request.Limit}",
            $"skip={request.Skip}",
            "order=sys.updatedAt",
        };

        if (request.UpdatedAfter.HasValue)
        {
            queryParams.Add($"sys.updatedAt[gte]={request.UpdatedAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}");
        }

        if (request.UpdatedBefore.HasValue)
        {
            queryParams.Add($"sys.updatedAt[lte]={request.UpdatedBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}");
        }

        return await FetchAsync(request, queryParams);
    }

    public async Task<ContentfulQueryResponse> GetEntriesByIdsAsync(ContentfulQueryRequest request, IList<string> ids)
    {
        var idsValue = string.Join(",", ids);
        var queryParams = new List<string>
        {
            $"content_type={Uri.EscapeDataString(request.ContentTypeId)}",
            "locale=*",
            $"sys.id[in]={Uri.EscapeDataString(idsValue)}",
            $"limit={ids.Count}",
        };

        return await FetchAsync(request, queryParams);
    }

    private async Task<ContentfulQueryResponse> FetchAsync(ContentfulQueryRequest request, List<string> queryParams)
    {
        var baseUrl = request.UsePreviewApi ? PreviewBaseUrl : DeliveryBaseUrl;
        var url = $"{baseUrl}/spaces/{Uri.EscapeDataString(request.SpaceId)}/entries?{string.Join("&", queryParams)}";

        var client = httpClientFactory.CreateClient("Contentful");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);

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
