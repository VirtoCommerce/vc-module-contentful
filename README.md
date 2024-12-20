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
