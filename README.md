# VirtoCommerce Contentful Module
VirtoCommerce.Contentful module provides integration with https://contentful.com CMS service.
Key features:
* develop and publish CMS pages in Contentful and automatically publish to Virto Commerce CMS
* un-publish existing pages
* create and modify products (name, properties and editorial reviews)

# Installation
Installing the module:
* Automatically: in VC Manager go to Configuration -> Modules -> Contentful module -> Install
* Manually: download module zip package from https://github.com/VirtoCommerce/vc-module-contentful/releases. In VC Manager go to Configuration -> Modules -> Advanced -> upload module package -> Install.

# Settings
* In Contentful configure a new custom application and under events enable event that posts to the following URL http://{URL}/admin/api/contentful/{STOREID}?api_key={VIRTO_API_KEY}, it should only apply for create, update and delete Entry events.

![Contentful CMS settings](https://github.com/user-attachments/assets/e32086ec-8b80-41cc-9e2c-e2c461c06cbe)


# Pages Module Integration

The module integrates with [Virto Pages](https://github.com/VirtoCommerce/vc-module-pages) as a content provider (`IPageContentProvider`), enabling:

* **Index Rebuild** — full reindex of all Contentful pages from the admin UI
* **Scheduled Sync** — periodic synchronization of modified pages using `sys.updatedAt` filter
* **Webhook Push** — real-time page updates via `POST /api/pages/contentful` (existing functionality)

The content provider uses the [Contentful Content Delivery API](https://www.contentful.com/developers/docs/references/content-delivery-api/) directly via HTTP requests. Configure the following store-level settings:

| Setting | Description | Default |
|---|---|---|
| **Contentful.SpaceId** | Contentful space ID | — |
| **Contentful.DeliveryApiKey** | Content Delivery API access token | — |
| **Contentful.ContentTypeId** | Content type ID to index as pages | `page` |
| **Contentful.PreviewApiKey** | Content Preview API token (optional) | — |

When `PreviewApiKey` is configured, the provider uses the [Content Preview API](https://www.contentful.com/developers/docs/references/content-preview-api/) (`preview.contentful.com`) instead of the Delivery API, which returns both published and draft entries. Draft entries are indexed with `Status = Draft`, published entries with `Status = Published`.

### Content Model Fields

The content type (default: `page`) should include the following fields:

| Contentful Field | Type | PageDocument Property | Required | Notes |
|---|---|---|---|---|
| `sys.id` | system | `Id`, `OuterId` | auto | Set by Contentful |
| `sys.createdAt` | system | `CreatedDate` | auto | Set by Contentful |
| `sys.updatedAt` | system | `ModifiedDate` | auto | Set by Contentful |
| `sys.publishedVersion` | system | `Status` | auto | Present = Published, absent = Draft |
| `title` | Short text | `Title` | yes | Page title |
| `permalink` | Short text | `Permalink` | yes | URL slug |
| `description` | Short text | `Description` | no | Meta description |
| `content` | Rich text | `Content` | no | Rendered to HTML via `IContentfulRenderer` |
| `storeId` | Short text | `StoreId` | recommended | Required for index rebuild. Fallback: webhook query param |
| `cultureName` | Short text | `CultureName` | recommended | Required for index rebuild. Fallback: detected from field locales |
| `isAuthenticated` | Boolean | `Visibility` | no | `false` = Public, `true` or absent = Private |
| `userGroups` | List (Short text) | `UserGroups` | no | Restrict access to specific user groups |
| `startDate` | Date & time | `StartDate` | no | Scheduled publishing start |
| `endDate` | Date & time | `EndDate` | no | Scheduled publishing end |

All fields are read using Contentful's `locale=*` mode. The locale is auto-detected from the first available locale key in the entry fields.

## References

* [Contentful Content Delivery API](https://www.contentful.com/developers/docs/references/content-delivery-api/)
* [Contentful Content Preview API](https://www.contentful.com/developers/docs/references/content-preview-api/)
* [Contentful .NET SDK](https://github.com/contentful/contentful.net)

# Documentation
* In Contentful create "page-virto" entity with "Title", "Content" and "Permalink" properties (you can add additional properties like layout etc). You can also create other entries as long as they start with "page" prefix, for instance "page.doc". Module supports multiple entries.

![setting up page model](https://user-images.githubusercontent.com/1566470/27984254-f057f266-6385-11e7-9a1a-fec1bfe67439.png)

* Now go to content and create new "page" entry.

![Create page in Contentful](https://user-images.githubusercontent.com/330693/211509494-82cbbd40-842f-46e3-b314-1362cfba9a2a.png)


* After publishing you can open page in Virto Commerce site and it should look something like this

![image](https://user-images.githubusercontent.com/1566470/27984281-a87f280a-6386-11e7-8543-74b0e0e20091.png)

# License
Copyright (c) Virto Solutions LTD.  All rights reserved. 

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

http://virtocommerce.com/opensourcelicense

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied. 
