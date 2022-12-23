using System.Collections.Generic;

namespace VirtoCommerce.Contentful.Models
{
    public class LocalizedPageEntity
    {
        public LocalizedPageEntity()
        {

        }

        public LocalizedPageEntity(string id, string language, Dictionary<string, Dictionary<string, object>> properties)
        {
            Id = id;
            Language = language;
            Properties = new Dictionary<string, string>();
            if (properties != null)
            {
                foreach (var key in properties.Keys)
                {
                    if (properties[key].ContainsKey(language))
                    {
                        if (properties[key][language] != null)
                        {
                            if (key == "content")
                            {
                                Content = properties[key][language].ToString();
                            }
                            else
                            {
                                Properties.Add(key, properties[key][language].ToString());
                            }
                        }
                    }
                }
            }
        }

        public string Id { get; set; }

        public string Language { get; set; }

        public string Content { get; set; }

        public Dictionary<string, string> Properties
        {
            get; private set;
        }
    }
}