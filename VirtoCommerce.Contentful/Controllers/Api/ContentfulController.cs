using Contentful.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using VirtoCommerce.ContentModule.Data.Services;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Web.Security;
using YamlDotNet.Serialization;

namespace VirtoCommerce.Contentful.Controllers.Api
{
    [RoutePrefix("api/contentful")]
    public class ContentfulController : ApiController
    {
        private readonly Func<string, IContentBlobStorageProvider> _contentStorageProviderFactory;
        private readonly IBlobUrlResolver _urlResolver;
        private readonly ISecurityService _securityService;
        private readonly IPermissionScopeService _permissionScopeService;

        public ContentfulController(Func<string, IContentBlobStorageProvider> contentStorageProviderFactory, IBlobUrlResolver urlResolver, ISecurityService securityService, IPermissionScopeService permissionScopeService)
        {
            _contentStorageProviderFactory = contentStorageProviderFactory;
            _urlResolver = urlResolver;
            _securityService = securityService;
            _permissionScopeService = permissionScopeService;
        }

        // GET: api/contentful
        [HttpPost]
        [Route("{storeId}")]
        public async Task<IHttpActionResult> WebhookHandlerAsync(string storeId)
        {
            var json = await Request.Content.ReadAsStringAsync();
            var source = JsonConvert.DeserializeObject<JObject>(json);

            var entry = GetEntry<Entry<Dictionary<string, Dictionary<string, object>>>>(source);

            if (entry.SystemProperties.ContentType.SystemProperties.Id.StartsWith("page")) // we only support pages for now
                return StatusCode(HttpStatusCode.NotImplemented);

            var page = new LocalizedPage(entry.SystemProperties.Id, "en-US", entry.Fields);

            // cvonvert custom o
            // X-Contentful-Topic
            var headers = this.Request.Headers;

            var operations = headers.GetValues("X-Contentful-Topic");
            var op = operations.FirstOrDefault();

            if (op.Equals("ContentManagement.Entry.unpublish")) // unpublish
            {
                UnpublishContent(storeId, page);
            }
            else if (op.Equals("ContentManagement.Entry.publish")) // publish
            {
                PublishContent(storeId, page);
            }
                
            return Ok(new { result = entry });
        }

        [CheckPermission(Permission = ContentPredefinedPermissions.Delete)]
        private void UnpublishContent(string storeId, LocalizedPage entry)
        {
            var storageProvider = _contentStorageProviderFactory($"Pages/{storeId}");
            storageProvider.Remove(new[] { String.Format("{0}.md", entry.Id) });
        }

        [CheckPermission(Permission = ContentPredefinedPermissions.Create)]
        private void PublishContent(string storeId, LocalizedPage entry)
        {
            var storageProvider = _contentStorageProviderFactory($"Pages/{storeId}");

            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(entry.Properties);

            var contents = new StringBuilder();
            contents.AppendLine("---");
            contents.AppendLine(yaml);
            contents.AppendLine("---");
            contents.AppendLine(entry.Content);
            using (var stream = storageProvider.OpenWrite(String.Format("{0}.md", entry.Id)))
            {
                using (var memStream = new MemoryStream(Encoding.UTF8.GetB‌​ytes(contents.ToString())))
                {
                    memStream.CopyTo(stream);
                }
            }
        }

        private T GetEntry<T>(JObject source)
        {
            var ob = default(T);

            if (typeof(IContentfulResource).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()))
            {
                ob = source.ToObject<T>();
            }
            else
            {
                var json = source;

                //move the sys object beneath the fields to make serialization more logical for the end user.
                var sys = json.SelectToken("$.sys");
                var fields = json.SelectToken("$.fields");
                fields["sys"] = sys;
                ob = fields.ToObject<T>();
            }
            return ob;
        }
    }

    public class LocalizedPage
    {
        public LocalizedPage()
        {

        }

        public LocalizedPage(string id, string language, Dictionary<string, Dictionary<string, object>> properties)
        {
            this.Id = id;
            this.Language = language;
            this.Properties = new Dictionary<string, string>();
            if(properties != null)
            foreach(var key in properties.Keys)
            {
                if (properties[key].ContainsKey(language))
                {
                    if (properties[key][language] != null)
                    {
                        if (key == "content")
                        {
                            this.Content = properties[key][language].ToString();
                        }
                        else
                        {
                            this.Properties.Add(key, properties[key][language].ToString());
                        }
                    }
                }
            }
        }

        public string Id { get; set; }

        public string Language { get; set; }

        public string Content { get; set; }

        public Dictionary<string, string> Properties
        {
            get; private set;
        }
    }

    public static class ContentPredefinedPermissions
    {
        public const string Read = "content:read",
            Create = "content:create",
            Access = "content:access",
            Update = "content:update",
            Delete = "content:delete";
    }
}
