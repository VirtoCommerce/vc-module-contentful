curl -XPOST http://localhost/admin/api/contentful/electronics?api_key=1ad56a55d84e40a6bc321708b2d4f29c --data @%1.json --verbose --trace trace -H "Content-Type: application/vnd.contentful.management.v1+json" -H "X-Contentful-Topic: ContentManagement.Entry.publish"