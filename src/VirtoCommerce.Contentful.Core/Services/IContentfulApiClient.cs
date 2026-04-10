using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace VirtoCommerce.Contentful.Core.Services;

public interface IContentfulApiClient
{
    Task<ContentfulQueryResponse> GetEntriesAsync(ContentfulQueryRequest request);
    Task<ContentfulQueryResponse> GetEntriesByIdsAsync(ContentfulQueryRequest request, IList<string> ids);
}

public class ContentfulQueryRequest
{
    public string SpaceId { get; set; }
    public string AccessToken { get; set; }
    public string ContentTypeId { get; set; }
    public bool UsePreviewApi { get; set; }
    public int Limit { get; set; }
    public int Skip { get; set; }
    public DateTime? UpdatedAfter { get; set; }
    public DateTime? UpdatedBefore { get; set; }
}

public class ContentfulQueryResponse
{
    public IList<JObject> Items { get; set; } = [];
    public int Total { get; set; }
}
