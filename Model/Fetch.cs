using System.Collections.Generic;
using System.Xml.Serialization;

namespace FetchXMLToSqlConverter.Model
{
    [XmlRoot(ElementName = "attribute")]
    public class Attribute
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "alias")]
        public string Alias { get; set; }
    }

    [XmlRoot(ElementName = "condition")]
    public class Condition
    {

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        [XmlAttribute(AttributeName = "attribute")]
        public string Attribute { get; set; }

        [XmlAttribute(AttributeName = "operator")]
        public string Operator { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "filter")]
    public class Filter
    {

        [XmlElement(ElementName = "condition")]
        public List<Condition> Condition { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; } // And & Or

        [XmlElement(ElementName = "filter")]
        public Filter InnerFilter { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "link-entity")]
    public class LinkEntity
    {

        [XmlElement(ElementName = "attribute")]
        public List<Attribute> Attribute { get; set; }

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "from")]
        public string From { get; set; }

        [XmlAttribute(AttributeName = "to")]
        public string To { get; set; }

        [XmlAttribute(AttributeName = "link-type")]
        public string LinkType { get; set; }

        [XmlAttribute(AttributeName = "alias")]
        public string Alias { get; set; }

        [XmlElement(ElementName = "filter")]
        public List<Filter> Filter { get; set; }
    }

    [XmlRoot(ElementName = "order")]
    public class Order
    {

        [XmlAttribute(AttributeName = "attribute")]
        public string Attribute { get; set; }

        [XmlAttribute(AttributeName = "descending")]
        public string Descending { get; set; }
    }

    [XmlRoot(ElementName = "entity")]
    public class Entity
    {

        [XmlElement(ElementName = "attribute")]
        public List<Attribute> Attribute { get; set; }

        [XmlElement(ElementName = "all-attributes")]
        public object allAttributes { get; set; }

        [XmlElement(ElementName = "order")]
        public List<Order> Order { get; set; }

        [XmlElement(ElementName = "filter")]
        public Filter Filter { get; set; }

        [XmlElement(ElementName = "link-entity")]
        public List<LinkEntity> LinkEntity { get; set; }

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "alias")]
        public string Alias { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "fetch")]
    public class Fetch
    {

        [XmlElement(ElementName = "entity")]
        public Entity Entity { get; set; }

        [XmlAttribute(AttributeName = "version")]
        public double Version { get; set; }

        [XmlAttribute(AttributeName = "output-format")]
        public string OutputFormat { get; set; }

        [XmlAttribute(AttributeName = "mapping")]
        public string Mapping { get; set; }

        [XmlAttribute(AttributeName = "distinct")]
        public string Distinct { get; set; }

        [XmlAttribute(AttributeName = "page")]
        public string Page { get; set; }

        [XmlAttribute(AttributeName = "count")]
        public string Count { get; set; }

        [XmlText]
        public string Text { get; set; }
    }
}
