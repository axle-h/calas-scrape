using System;

namespace Calas.Scrape.Attributes
{
    public class DataFieldAttribute : Attribute
    {
        public DataFieldAttribute(string name, DataFieldType type = DataFieldType.Table)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }

        public DataFieldType Type { get; }
    }
}