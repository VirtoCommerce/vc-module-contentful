using Contentful.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using VirtoCommerce.Contentful.Model.Virto;
using VirtoCommerce.ContentModule.Data.Services;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Store.Services;
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
        private readonly IStoreService _storeService;
        private readonly IItemService _itemService;
        private readonly ICatalogService _catalogService;
        private readonly ICatalogSearchService _searchService;

        public ContentfulController(Func<string, IContentBlobStorageProvider> contentStorageProviderFactory, 
            IBlobUrlResolver urlResolver, ISecurityService securityService, 
            IPermissionScopeService permissionScopeService, IStoreService storeService,
            IItemService itemService, ICatalogService catalogService, ICatalogSearchService searchService)
        {
            _itemService = itemService;
            _storeService = storeService;
            _contentStorageProviderFactory = contentStorageProviderFactory;
            _urlResolver = urlResolver;
            _securityService = securityService;
            _permissionScopeService = permissionScopeService;
            _catalogService = catalogService;
            _searchService = searchService;
        }

        // GET: api/contentful/stores/{storeid}
        [HttpPost]
        [Route("{storeId}")]
        public async Task<IHttpActionResult> WebhookHandlerAsync(string storeId)
        {
            // TODO: add check if user has store permissions
            var json = await Request.Content.ReadAsStringAsync();
            var source = JsonConvert.DeserializeObject<JObject>(json);

            var entry = GetEntry<Entry<Dictionary<string, Dictionary<string, object>>>>(source);

            var type = GetEntryType(entry.SystemProperties.ContentType.SystemProperties.Id);

            if (type == EntryType.Unknown)
                return Ok("Only entities named \"page*\" are supported");

            // now check if store actually exists, this is more expensive than checking page type, so do it later
            var store = _storeService.GetById(storeId);
            if (store == null)
            {
                return NotFound();
            }

            // X-Contentful-Topic
            var headers = this.Request.Headers;

            var operations = headers.GetValues("X-Contentful-Topic");
            var op = operations.FirstOrDefault();
            var action = GetAction(op);

            // TODO: get language from the response, add support for multiple languages
            if (type == EntryType.Page) // create/update/delete CMS pages
            {
                var page = new LocalizedPageEntity(entry.SystemProperties.Id, "en-US", entry.Fields);
                RouteContentCall(action, storeId, page);
            }
            if (type == EntryType.Product) // create/update/delete products
            {
                var product = new ProductEntity(entry.SystemProperties.Id, entry.Fields);
                RouteProductCall(action, product);
            }

            return Ok("Updated successfully");
        }

        #region Product
        private void RouteProductCall(Operation op, ProductEntity entry)
        {
            const string ReviewType = "FullReview";
            if (op == Operation.Publish) // publish
            {
                var product = GetCatalogProduct(entry, out bool isNew);
                product.IsActive = true;

                if (entry.Content != null)
                {
                    var list = new List<EditorialReview>();
                    foreach(var lang in entry.Content.Keys)
                    {
                        var review = new EditorialReview()
                        {
                            Content = entry.Content[lang],
                            ReviewType = ReviewType
                        };

                        list.Add(review);
                    }

                    // add new reviews or update existing ones
                    if (product.Reviews == null)
                    {
                        product.Reviews = list.ToArray();
                    }
                    else
                    {
                        foreach(var review in list)
                        {
                            var existingReview = product.Reviews.Where(x => x.ReviewType == ReviewType && x.LanguageCode == review.LanguageCode).SingleOrDefault();
                            if(existingReview == null)
                            {
                                product.Reviews.Add(review);
                            }
                            else
                            {
                                existingReview.Content = review.Content;
                            }
                        }
                       
                    }
                }

                // now add all the properties
                if (entry.Properties != null)
                {
                    var propValuesList = new List<PropertyValue>();
                    foreach (var key in entry.Properties.Keys)
                    {
                        var prop = entry.Properties[key];

                        foreach (var lang in prop.Keys)
                        {
                            var proValue = new PropertyValue()
                            {
                                LanguageCode = lang,
                                PropertyName = key,
                                Value = prop[lang]
                            };

                            propValuesList.Add(proValue);
                        }
                    }

                    propValuesList.Add(new PropertyValue(){PropertyName = "contentfulid",Value = entry.Id});

                    // add new properties or update existing ones
                    if (product.PropertyValues == null)
                    {
                        product.PropertyValues = propValuesList.ToArray();
                    }
                    else
                    {
                        foreach (var propertyValue in propValuesList)
                        {
                            var existingPropertyValue = product.PropertyValues.Where(x => x.PropertyName == propertyValue.PropertyName && x.LanguageCode == propertyValue.LanguageCode).SingleOrDefault();
                            if (existingPropertyValue == null)
                            {
                                product.PropertyValues.Add(propertyValue);
                            }
                            else
                            {
                                existingPropertyValue.Value = propertyValue.Value;
                            }
                        }
                    }                    
                }

                if (isNew)
                {
                    _itemService.Create(product);
                }
                else
                {
                    _itemService.Update(new[] { product });
                }
            }
            else if (op == Operation.Unpublish || op == Operation.Delete) // unpublish
            {
                var product = GetCatalogProduct(entry, out bool isNew);
                product.IsActive = false;

                if (!isNew)
                {
                    _itemService.Update(new[] { product });
                }
            }
        }

        private CatalogProduct GetCatalogProduct(ProductEntity entry, out bool isNew)
        {
            // try finding catalog by name
            var catalog = _catalogService.GetCatalogsList().Where(x => x.Name.Equals(entry.Catalog, StringComparison.OrdinalIgnoreCase)).SingleOrDefault();
            if (catalog == null)
                throw new ApplicationException("Catalog not found");

            // try finding product by id

            var criteria = new SearchCriteria();
            //criteria.CatalogId = catalog.Id;
            criteria.Code = entry.Sku;
            criteria.ResponseGroup = SearchResponseGroup.WithProducts;
            criteria.WithHidden = true;
            var results = _searchService.Search(criteria);

            CatalogProduct product = null; //= _itemService.GetById(entry.Id, ItemResponseGroup.ItemLarge);

            if(results.ProductsTotalCount > 0)
            {
                product = results.Products.SingleOrDefault();
            }

            isNew = false;

            if (product == null)
            {
                isNew = true;
                product = new CatalogProduct()
                {
                    CatalogId = catalog.Id,
                    Id = entry.Sku,
                    Name = entry.Name["en-US"],
                    Code = entry.Sku
                };
            }
            else
            {
                // change title
                product.Name = entry.Name["en-US"];
            }

            return product;
        }
        #endregion

        #region CMS
        private void RouteContentCall(Operation op, string storeId, LocalizedPageEntity entry)
        {
            if (op == Operation.Undefined) // unpublish
            {
                UnpublishContentPage(storeId, entry);
            }
            else if (op == Operation.Publish) // publish
            {
                PublishContentPage(storeId, entry);
            }
        }

        [CheckPermission(Permission = ContentPredefinedPermissions.Delete)]
        private void UnpublishContentPage(string storeId, LocalizedPageEntity entry)
        {
            var storageProvider = _contentStorageProviderFactory($"Pages/{storeId}");
            storageProvider.Remove(new[] { String.Format("{0}.md", entry.Id) });
        }

        [CheckPermission(Permission = ContentPredefinedPermissions.Create)]
        private void PublishContentPage(string storeId, LocalizedPageEntity entry)
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
        #endregion

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

        private Operation GetAction(string topic)
        {
            if (topic.Equals("ContentManagement.Entry.unpublish")) // unpublish
            {
                return Operation.Unpublish;
            }
            else if (topic.Equals("ContentManagement.Entry.publish")) // publish
            {
                return Operation.Publish;
            }

            return Operation.Undefined;
        }

        private EntryType GetEntryType(string entityType)
        {
            if (entityType.StartsWith("page")) // we only support pages for now
                return EntryType.Page;
            if (entityType.StartsWith("product"))
                return EntryType.Product;

            return EntryType.Unknown;
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

    public enum Operation
    {
        Undefined,
        Publish,
        Unpublish,
        Update,
        Delete
    }

    public enum EntryType
    {
        Unknown,
        Page,
        BlogArticle,
        Product
    }
}
