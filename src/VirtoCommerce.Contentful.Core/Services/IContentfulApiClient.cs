using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace VirtoCommerce.Contentful.Core.Services;

public interface IContentfulApiClient
{
    Task<ContentfulQueryResponse> GetEntriesAsync(string spaceId, string accessToken, string contentTypeId, int limit, int skip, DateTime? updatedAfter = null, DateTime? updatedBefore = null);
    Task<ContentfulQueryResponse> GetEntriesByIdsAsync(string spaceId, string accessToken, string contentTypeId, IList<string> ids);
}

public class ContentfulQueryResponse
{
    public IList<JObject> Items { get; set; } = [];
    public int Total { get; set; }
}
