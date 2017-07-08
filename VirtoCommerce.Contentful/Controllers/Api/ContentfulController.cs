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

            // TODO: add permission checking
            //var entry = GetEntry<Entry<Dictionary<string, Dictionary<string, object>>>>(source);
            var entry = GetEntry<Entry<Page>>(source);

            if (entry.SystemProperties.ContentType.SystemProperties.Id != "page") // we only support pages for now
                return NotFound();

            // X-Contentful-Topic
            var headers = this.Request.Headers;

            var operations = headers.GetValues("X-Contentful-Topic");
            var op = operations.FirstOrDefault();

            if (op.Equals("ContentManagement.Entry.unpublish")) // unpublish
            {
                UnpublishContent(storeId, entry);
            }
            else if (op.Equals("ContentManagement.Entry.publish")) // publish
            {
                PublishContent(storeId, entry);
            }
                
            return Ok(new { result = entry });
        }

        protected string[] GetObjectPermissionScopeStrings(object obj)
        {
            return _permissionScopeService.GetObjectPermissionScopeStrings(obj).ToArray();
        }

        protected void CheckCurrentUserHasPermissionForObjects(string permission, params object[] objects)
        {
            //Scope bound security check
            var scopes = objects.SelectMany(x => _permissionScopeService.GetObjectPermissionScopeStrings(x)).Distinct().ToArray();
            if (!_securityService.UserHasAnyPermission(User.Identity.Name, scopes, permission))
            {
                throw new HttpResponseException(HttpStatusCode.Unauthorized);
            }
        }

        [CheckPermission(Permission = ContentPredefinedPermissions.Delete)]
        private void UnpublishContent(string storeId, Entry<Page> entry)
        {
            var storageProvider = _contentStorageProviderFactory($"Pages/{storeId}");
            storageProvider.Remove(new[] { String.Format("{0}.md", entry.SystemProperties.Id) });
        }

        [CheckPermission(Permission = ContentPredefinedPermissions.Create)]
        private void PublishContent(string storeId, Entry<Page> entry)
        {
            var language = "en-US";
            var storageProvider = _contentStorageProviderFactory($"Pages/{storeId}");

            var pageYaml = new PageYaml() { Title = entry.Fields.Title[language], Permalink = entry.Fields.Filename[language] };
            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(pageYaml);

            var contents = new StringBuilder();
            contents.AppendLine("---");
            contents.AppendLine(yaml);
            contents.AppendLine("---");
            contents.AppendLine(entry.Fields.Content[language]);
            using (var stream = storageProvider.OpenWrite(String.Format("{0}.md", entry.SystemProperties.Id)))
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

    public class Page
    {
        public Dictionary<string, string> Content { get; set; }

        public Dictionary<string, string> Title { get; set; }

        public Dictionary<string, string> Filename { get; set; }
    }

    public class PageYaml
    {
        public string Title { get; set; }
        public string Permalink { get; set; }
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
