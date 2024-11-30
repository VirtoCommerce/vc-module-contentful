using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VirtoCommerce.Contentful.Core;
using VirtoCommerce.Contentful.Core.Models;
using VirtoCommerce.Contentful.Core.Services;
using VirtoCommerce.Contentful.Data.Extensions;
using VirtoCommerce.Pages.Core.Events;
using VirtoCommerce.Pages.Core.Extensions;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.Contentful.Web.Controllers.Api;

[Authorize]
[Route("api/pages/contentful")]
public class PagesContentfulController(IContentfulReader contentfulReader,
    IContentfulRenderer contentfulRenderer,
    IEventPublisher eventPublisher) : Controller
{
    [HttpPost]
    public async Task<ActionResult> Post(
        [FromQuery] string storeId,
        [FromQuery] string cultureName,
        [FromHeader(Name = "X-Contentful-Topic")] string operation
        )
    {
        var pageOperation = operation.ToPageOperation();
        if (pageOperation == PageOperation.Unknown)
        {
            return Ok();
        }
        var model = await contentfulReader.ReadEntry(Request.Body);
        if (model.EntryType == EntryType.Page)
        {
            if ((pageOperation == PageOperation.Delete &&
                 !User.HasGlobalPermission(ContentfulConstants.Security.Permissions.Delete))
                || !User.HasGlobalPermission(ContentfulConstants.Security.Permissions.Update))
            {
                return Forbid();
            }

            model.CultureName = cultureName;

            var pageDocument = model.ToPageDocument();
            if (model.Fields.TryGetValue("content", out var value))
            {
                var json = value[cultureName];
                var html = await contentfulRenderer.RenderContent(json?.ToString());
                pageDocument.Content = JsonConvert.SerializeObject(new { json, html });
            }

            pageDocument.Status = pageOperation.GetPageDocumentStatus();
            pageDocument.StoreId = storeId;

            var pageChangedEvent = AbstractTypeFactory<PagesDomainEvent>.TryCreateInstance();
            pageChangedEvent.Operation = pageOperation;
            pageChangedEvent.Page = pageDocument;

            await eventPublisher.Publish(pageChangedEvent);
        }

        return Ok();
    }

}
