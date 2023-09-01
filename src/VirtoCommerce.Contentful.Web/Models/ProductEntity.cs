using System.Collections.Generic;
using System.Linq;

namespace VirtoCommerce.Contentful.Web.Models;

public class ProductEntity
{
    public ProductEntity()
    {

    }

    public ProductEntity(string id, Dictionary<string, Dictionary<string, object>> properties)
    {
        Id = id;
        Properties = new Dictionary<string, Dictionary<string, object>>();
        if (properties != null)
        {
            foreach (var key in properties.Keys)
            {
                if (key == "catalog")
                {
                    Catalog = properties[key].First().Value.ToString();
                }
                else if (key == "title")
                {
                    Name = properties[key].First().Value.ToString();
                }
                else if (key == "sku")
                {
                    Sku = properties[key].First().Value.ToString();
                }
                else if (key == "content")
                {
                    Content = properties[key].ToDictionary(k => k.Key, v => v.Value.ToString());
                }
                else
                {
                    Properties.Add(key, properties[key]);
                }
            }
        }
    }

    public string Id { get; set; }

    public string Catalog { get; set; }

    public string Sku { get; set; }

    public string Name { get; set; }

    public Dictionary<string, string> Content { get; set; }

    public Dictionary<string, Dictionary<string, object>> Properties
    {
        get; private set;
    }
}