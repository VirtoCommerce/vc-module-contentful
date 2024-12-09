using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.CatalogModule.Core.Services;
using VirtoCommerce.Contentful.Core;
using VirtoCommerce.Contentful.Core.Models;
using VirtoCommerce.Contentful.Core.Services;
using VirtoCommerce.Contentful.Data.Extensions;
using VirtoCommerce.ContentModule.Core.Services;
using VirtoCommerce.Pages.Core.Events;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Services;
using YamlDotNet.Serialization;

namespace VirtoCommerce.Contentful.Web.Controllers.Api;

[Authorize]
[Route("api/contentful")]
public class ContentfulController : Controller
{
    private readonly IBlobContentStorageProviderFactory _blobContentStorageProviderFactory;
    private readonly IStoreService _storeService;
    private readonly IItemService _itemService;
    private readonly ICatalogService _catalogService;
    private readonly IProductSearchService _productSearchService;
    private readonly IContentPathResolver _pathResolver;
    private readonly IContentfulRenderer _contentfulRenderer;
    private readonly IContentfulReader _contentfulReader;

    public ContentfulController(
        IBlobContentStorageProviderFactory blobContentStorageProviderFactory,
        IStoreService storeService,
        IItemService itemService,
        ICatalogService catalogService,
        IProductSearchService productSearchService,
        IContentPathResolver pathResolver,
        IContentfulReader contentfulReader,
        IContentfulRenderer contentfulRenderer
    )
    {
        _itemService = itemService;
        _storeService = storeService;
        _blobContentStorageProviderFactory = blobContentStorageProviderFactory;
        _catalogService = catalogService;
        _productSearchService = productSearchService;
        _pathResolver = pathResolver;
        _contentfulRenderer = contentfulRenderer;
        _contentfulReader = contentfulReader;
    }

    // GET: api/contentful/{storeid}
    [HttpPost]
    [Route("{storeId}")]
    public async Task<IActionResult> WebhookHandlerAsync(string storeId)
    {
        // TODO: add check if user has store permissions
        var entry = await _contentfulReader.ReadEntry(Request.Body);

        if (entry.EntryType == EntryType.Unknown)
        {
            return Ok("Only entities named \"page*\" are supported");
        }

        // now check if store actually exists, this is more expensive than checking page type, so do it later
        var store = await _storeService.GetByIdAsync(storeId);
        if (store == null)
        {
            return NotFound();
        }

        // X-Contentful-Topic
        var headers = Request.Headers;

        if (headers.TryGetValue("X-Contentful-Topic", out var operations))
        {
            var op = operations.FirstOrDefault();
            var action = op.ToPageOperation();

            // TODO: get language from the response, add support for multiple languages
            if (entry.EntryType == EntryType.Page) // create/update/delete CMS pages
            {
                // go through all the languages
                if (!entry.Fields.TryGetValue("pageName", out var fields) && !entry.Fields.TryGetValue("title", out fields))
                {
                    return Ok($"Not found field with the name pageName or title");
                }
                else
                {
                    foreach (var lang in fields.Keys)
                    {
                        var page = new LocalizedPageEntity(entry.SystemProperties.Id, lang, entry.Fields);
                        await RouteContentCall(action, storeId, page);
                    }
                }

                return Ok(string.Format("Page updated successfully \"{0}\"", entry.SystemProperties.Id));
            }
            if (entry.EntryType == EntryType.Product) // create/update/delete products
            {
                var product = new ProductEntity(entry.SystemProperties.Id, entry.Fields);
                await RouteProductCall(action, product);
                return Ok(string.Format("Product updated successfully \"{0}\"", entry.SystemProperties.Id));
            }

        }
        return Ok($"No handler for type \"{entry.SystemProperties.ContentType.SystemProperties.Id}\" found");
    }

    #region Product
    private async Task RouteProductCall(PageOperation op, ProductEntity entry)
    {
        const string ReviewType = "FullReview";
        if (op == PageOperation.Publish) // publish
        {
            var (product, isNew) = await GetCatalogProductAsync(entry);
            product.IsActive = true;

            if (entry.Content != null)
            {
                var list = new List<EditorialReview>();
                foreach (var lang in entry.Content.Keys)
                {
                    var review = new EditorialReview
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
                        var existingReview = product.Reviews.SingleOrDefault(x => x.ReviewType == ReviewType && x.LanguageCode == review.LanguageCode);
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
                var propList = new List<Property>();
                foreach (var key in entry.Properties.Keys)
                {
                    var prop = entry.Properties[key];

                    var newProperty = new Property
                    {
                        Name = key,
                        Values = new List<PropertyValue>()
                    };

                    foreach (var lang in prop.Keys)
                    {
                        newProperty.Values.Add(new PropertyValue
                        {
                            LanguageCode = lang,
                            PropertyName = key,
                            Value = prop[lang]
                        });
                    }
                    propList.Add(newProperty);
                }

                propList.Add(new Property()
                {
                    Name = "contentfulid",
                    Values = new List<PropertyValue>
                    {
                        new PropertyValue
                        {
                            PropertyName = "contentfulid",
                            Value = entry.Id
                        }
                    }
                });

                // add new properties or update existing ones
                if (product.Properties == null)
                {
                    product.Properties = propList.ToArray();
                }
                else
                {
                    foreach (var property in propList)
                    {
                        var existingProperty = product.Properties.FirstOrDefault(x => x.Name == property.Name);
                        if (existingProperty == null)
                        {
                            product.Properties.Add(property);
                        }
                        else
                        {
                            foreach (var value in property.Values)
                            {
                                var existingPropertyValue = existingProperty.Values.SingleOrDefault(x => x.PropertyName == value.PropertyName && x.LanguageCode == value.LanguageCode);
                                if (existingPropertyValue == null)
                                {
                                    existingProperty.Values.Add(value);
                                }
                                else
                                {
                                    existingPropertyValue.Value = value.Value;
                                }
                            }
                        }
                    }
                }
            }

            await _itemService.SaveChangesAsync(new[] { product });
        }
        else if (op == PageOperation.Unpublish || op == PageOperation.Delete) // unpublish
        {
            var criteria = new ProductSearchCriteria
            {
                SearchPhrase = $"contentfulid:{entry.Id}"
            };
            var result = await _productSearchService.SearchAsync(criteria);

            if (result.TotalCount > 0)
            {
                await _itemService.DeleteAsync(new[] { result.Results[0].Id });

            }
        }
    }

    private async Task<(CatalogProduct, bool)> GetCatalogProductAsync(ProductEntity entry)
    {
        // try finding catalog by name
        var catalog = (await _catalogService.GetAsync(Array.Empty<string>(), null))
        .Where(x => x.Name.Equals(entry.Catalog, StringComparison.OrdinalIgnoreCase))
        .SingleOrDefault();
        if (catalog == null)
        {
            throw new Exception($"Catalog `{entry.Catalog}` not found");
        }

        // try finding product by id

        var criteria = new ProductSearchCriteria
        {
            Skus = new List<string>() { entry.Sku },
            // CatalogId = catalog.Id ??
            ResponseGroup = ItemResponseGroup.ItemLarge.ToString(),
            SearchInChildren = true
        };

        var result = await _productSearchService.SearchAsync(criteria);

        if (result.TotalCount > 0)
        {
            var item = result.Results[0];
            item.Name = entry.Name;
            return (item, false);
        }
        var product = new CatalogProduct
        {
            CatalogId = catalog.Id,
            Id = entry.Sku,
            Name = entry.Name,
            Code = entry.Sku
        };
        return (product, true);
    }
    #endregion

    #region CMS
    private async Task RouteContentCall(PageOperation op, string storeId, LocalizedPageEntity entry)
    {
        if (op == PageOperation.Unknown) // unpublish
        {
            await UnpublishContentPage(storeId, entry);
        }
        else if (op == PageOperation.Publish) // publish
        {
            await PublishContentPage(storeId, entry);
        }
    }

    [Authorize(ContentfulConstants.Security.Permissions.Delete)]
    private Task UnpublishContentPage(string storeId, LocalizedPageEntity entry)
    {
        var path = _pathResolver.GetContentBasePath("pages", storeId);
        var storageProvider = _blobContentStorageProviderFactory.CreateProvider(path);
        return storageProvider.RemoveAsync(new[] { $"{entry.Id}.md" });
    }

    [Authorize(ContentfulConstants.Security.Permissions.Create)]
    private async Task PublishContentPage(string storeId, LocalizedPageEntity entry)
    {
        var path = _pathResolver.GetContentBasePath("pages", storeId);
        var storageProvider = _blobContentStorageProviderFactory.CreateProvider(path);

        var serializer = new SerializerBuilder().Build();
        var yaml = serializer.Serialize(entry.Properties);

        var contents = new StringBuilder();
        contents.AppendLine("---");
        contents.AppendLine(yaml);
        contents.AppendLine("---");
        var content = await _contentfulRenderer.RenderContent(entry.Content);
        contents.AppendLine(content);
        await using var stream = await storageProvider.OpenWriteAsync($"{entry.Id}.md");
        using var memStream = new MemoryStream(Encoding.UTF8.GetB‌​ytes(contents.ToString()));
        await memStream.CopyToAsync(stream);
    }

    #endregion
}
