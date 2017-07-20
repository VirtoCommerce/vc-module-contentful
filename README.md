# VirtoCommerce.Contentful (Preview!)
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
* In Contentful configure webhook to point to http://{URL}/admin/api/contentful/{STOREID}?api_key={VIRTO_API_KEY}, it should only apply for create, update and delete events.

![image](https://user-images.githubusercontent.com/1566470/27984261-4d6bc8d8-6386-11e7-9f7d-346045311d15.png)

# Documentation
* In Contentful create "page" entity with "Title", "Content" and "Permalink" properties (you can add additional properties like layout etc). You can also create other entries as long as they start with "page" prefix, for instance "page.doc". Module supports multiple entries.

![image](https://user-images.githubusercontent.com/1566470/27984254-f057f266-6385-11e7-9a1a-fec1bfe67439.png)

* Now go to content and create new "page" entry.

![image](https://user-images.githubusercontent.com/1566470/27984274-7f482928-6386-11e7-8d23-37c461dedb4c.png)

* After publishing you can open page in Virto Commerce site and it should look something like this

![image](https://user-images.githubusercontent.com/1566470/27984281-a87f280a-6386-11e7-8543-74b0e0e20091.png)

# License
Copyright (c) Virtosoftware Ltd.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

http://virtocommerce.com/opensourcelicense

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied. 
