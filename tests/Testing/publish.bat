curl -XPOST http://localhost:10645/api/contentful/electronics?api_key=a348fa7508d342f6a32f8bf6c6681a2a --data @%1.json --verbose --trace trace -H "Content-Type: application/vnd.contentful.management.v1+json" -H "X-Contentful-Webhook-Name: Demo" -H "X-Contentful-Topic: ContentManagement.Entry.publish"