using Contentful.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.ContentfulModule.Web.Model.Virto;
using VirtoCommerce.ContentModule.Core.Services;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.StoreModule.Core.Services;
using YamlDotNet.Serialization;
using static VirtoCommerce.ContentModule.Core.ContentConstants.Security;

namespace VirtoCommerce.ContentfulModule.Web.Controllers.Api
{
    [Route("api/contentful")]
    public class ContentfulController : Controller
    {
        private readonly IBlobContentStorageProviderFactory _contentStorageProviderFactory;
        private readonly IBlobUrlResolver _urlResolver;
        private readonly IStoreService _storeService;
        private readonly IItemService _itemService;
        private readonly ICatalogService _catalogService;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly IProductIndexedSearchService _productIndexSearchService;
        private readonly IProductSearchService _productSearchService;

        public ContentfulController(IBlobContentStorageProviderFactory contentStorageProviderFactory, 
            IBlobUrlResolver urlResolver, IStoreService storeService, 
            IItemService itemService, ICatalogService catalogService, 
            ICatalogSearchService searchService, IProductIndexedSearchService productIndexSearchService, IProductSearchService productSearchService)
        {
            _itemService = itemService;
            _storeService = storeService;
            _contentStorageProviderFactory = contentStorageProviderFactory;
            _urlResolver = urlResolver;
            _catalogService = catalogService;
            _catalogSearchService = searchService;
            _productIndexSearchService = productIndexSearchService;
            _productSearchService = productSearchService;
        }

        // GET: api/contentful/stores/{storeid}
        [HttpPost]
        [Route("{storeId}")]
        public async Task<ActionResult> WebhookHandlerAsync(string storeId)
        {
            // TODO: add check if user has store permissions
            var json = await new StreamReader(Request.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(json))
                throw new NullReferenceException("http post body contains no json");

            var source = JsonConvert.DeserializeObject<JObject>(json);

            var entry = GetEntry<Entry<Dictionary<string, Dictionary<string, object>>>>(source);

            var type = GetEntryType(entry.SystemProperties.ContentType.SystemProperties.Id);

            if (type == EntryType.Unknown)
                return Ok("Only entities named \"page*\" are supported");

            // now check if store actually exists, this is more expensive than checking page type, so do it later
            var store = await _storeService.GetByIdAsync(storeId);
            if (store == null)
            {
                return NotFound();
            }

            // X-Contentful-Topic
            var headers = Request.Headers;

            var operations = headers["X-Contentful-Topic"];
            var op = operations.FirstOrDefault();
            var action = GetAction(op);

            // TODO: get language from the response, add support for multiple languages
            if (type == EntryType.Page) // create/update/delete CMS pages
            {
                // go through all the languages
                foreach (var lang in entry.Fields["title"].Keys)
                {
                    var page = new LocalizedPageEntity(entry.SystemProperties.Id, lang, entry.Fields);
                    await RouteContentCall(action, storeId, page);
                }

                return Ok(string.Format("Page updated successfully \"{0}\"", entry.SystemProperties.Id));
            }
            if (type == EntryType.Product) // create/update/delete products
            {
                var product = new ProductEntity(entry.SystemProperties.Id, entry.Fields);
                await RouteProductCall(action, product);
                return Ok(string.Format("Product updated successfully \"{0}\"", entry.SystemProperties.Id));
            }

            return Ok(string.Format("No handler for type \"{0}\" found", entry.SystemProperties.ContentType.SystemProperties.Id));
        }

        #region Product
        private async Task RouteProductCall(Operation op, ProductEntity entry)
        {
            const string ReviewType = "FullReview";
            if (op == Operation.Publish) // publish
            {
                var product = await GetCatalogProduct(entry);
                product.IsActive = true;

                if (entry.Content != null)
                {
                    var list = new List<EditorialReview>();
                    foreach (var lang in entry.Content.Keys)
                    {
                        var review = new EditorialReview()
                        {
                            Content = entry.Content[lang],
                            ReviewType = ReviewType,
                            LanguageCode = lang
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
                        foreach (var review in list)
                        {
                            var existingReview = product.Reviews.Where(x => x.ReviewType == ReviewType && x.LanguageCode == review.LanguageCode).FirstOrDefault();
                            if (existingReview == null)
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

                    propValuesList.Add(new PropertyValue() { PropertyName = "contentfulid", Value = entry.Id });

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

                // save product
                await _itemService.SaveChangesAsync(new[] { product });
            }
            else if (op == Operation.Unpublish || op == Operation.Delete) // unpublish
            {
                var criteria = new ProductIndexedSearchCriteria
                {
                    Terms = new[] { string.Format("contentfulid:{0}", entry.Id) }
                };
                var result = await _productIndexSearchService.SearchAsync(criteria);

                if (result.TotalCount > 0)
                {
                    var product = await _itemService.GetByIdAsync(result.Items[0].Id, ItemResponseGroup.ItemLarge.ToString()); // reload complete product now
                    product.IsActive = false;
                    await _itemService.SaveChangesAsync(new[] { product });

                }
            }
        }

        private async Task<CatalogProduct> GetCatalogProduct(ProductEntity entry)
        {
            // try finding catalog by name
            var catalogResults = await _catalogSearchService.SearchCatalogsAsync(new CatalogSearchCriteria() { Take = 100 });
            var catalog = catalogResults.Results.Where(x => x.Name.Equals(entry.Catalog, StringComparison.OrdinalIgnoreCase)).SingleOrDefault();
            if (catalog == null)
                throw new ApplicationException("Catalog not found");

            // try finding product by id
            var criteria = new ProductSearchCriteria
            {
                CatalogId = catalog.Id,
                Skus = new string[] { entry.Sku }
            };
            //criteria.ResponseGroup = SearchResponseGroup.WithProducts;
            //criteria.WithHidden = true;

            var results = await _productSearchService.SearchProductsAsync(criteria);

            CatalogProduct product = null; //= _itemService.GetById(entry.Id, ItemResponseGroup.ItemLarge);

            if(results.Results.Count > 0)
            {
                product = results.Results.SingleOrDefault();
                product = await _itemService.GetByIdAsync(product.Id, ItemResponseGroup.ItemLarge.ToString()); // reload complete product now
            }


            if (product == null)
            {
                product = new CatalogProduct()
                {
                    CatalogId = catalog.Id,
                    Id = entry.Sku,
                    Name = entry.Name,
                    Code = entry.Sku
                };
            }
            else
            {
                // change title
                product.Name = entry.Name;
            }

            return product;
        }
        #endregion

        #region CMS
        private async Task RouteContentCall(Operation op, string storeId, LocalizedPageEntity entry)
        {
            if (op == Operation.Undefined) // unpublish
            {
                await UnpublishContentPage(storeId, entry);
            }
            else if (op == Operation.Publish) // publish
            {
                await PublishContentPage(storeId, entry);
            }
        }

        [Authorize(Permissions.Delete)]
        private async Task UnpublishContentPage(string storeId, LocalizedPageEntity entry)
        {
            var storageProvider = _contentStorageProviderFactory.CreateProvider($"Pages/{storeId}");
            await storageProvider.RemoveAsync(new[] { string.Format("{0}.md", entry.Id) });
        }

        [Authorize(Permissions.Create)]
        private async Task PublishContentPage(string storeId, LocalizedPageEntity entry)
        {
            var storageProvider = _contentStorageProviderFactory.CreateProvider($"Pages/{storeId}");

            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(entry.Properties);

            var contents = new StringBuilder();
            contents.AppendLine("---");
            contents.AppendLine(yaml);
            contents.AppendLine("---");
            contents.AppendLine(entry.Content);
            using (var stream = storageProvider.OpenWrite(string.Format("{0}.md", entry.Id)))
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
