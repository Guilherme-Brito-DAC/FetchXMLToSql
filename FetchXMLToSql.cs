using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using static System.Net.WebRequestMethods;

namespace FetchXMLToSql
{
    public class FetchXMLToSql
    {
        #region [Class]
        [XmlRoot(ElementName = "attribute")]
        private class Attribute
        {
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }

            [XmlAttribute(AttributeName = "alias")]
            public string Alias { get; set; }
        }

        [XmlRoot(ElementName = "condition")]
        private class Condition
        {

            [XmlElement(ElementName = "value")]
            public List<string> Value { get; set; }

            [XmlAttribute(AttributeName = "attribute")]
            public string Attribute { get; set; }

            [XmlAttribute(AttributeName = "operator")]
            public string Operator { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot(ElementName = "filter")]
        private class Filter
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
        private class LinkEntity
        {

            [XmlElement(ElementName = "attribute")]
            public List<Attribute> Attribute { get; set; }

            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }

            [XmlAttribute(AttributeName = "from")]
            public string From { get; set; }

            [XmlAttribute(AttributeName = "to")]
            public string To { get; set; }

            [XmlAttribute(AttributeName = "visible")]
            public bool Visible { get; set; }

            [XmlAttribute(AttributeName = "link-type")]
            public string LinkType { get; set; }

            [XmlAttribute(AttributeName = "alias")]
            public string Alias { get; set; }

            [XmlElement(ElementName = "filter")]
            public List<Filter> Filter { get; set; }
        }

        [XmlRoot(ElementName = "order")]
        private class Order
        {

            [XmlAttribute(AttributeName = "attribute")]
            public string Attribute { get; set; }

            [XmlAttribute(AttributeName = "descending")]
            public string Descending { get; set; }
        }

        [XmlRoot(ElementName = "entity")]
        private class Entity
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
        private class Fetch
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
            public bool? Distinct { get; set; }

            [XmlAttribute(AttributeName = "page")]
            public int? Page { get; set; }

            [XmlAttribute(AttributeName = "count")]
            public int? Count { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        #endregion

        public string ConvertToSQL(string FetchXML)
        {
            try
            {
                if (!IsValidXml(FetchXML))
                {
                    throw new Exception("Invalid FetchXML!");
                }

                Fetch fetch = DeserializeXML(FetchXML);

                #region [Variables]
                bool distinct = fetch.Distinct.HasValue && fetch.Distinct.Value;
                string table = fetch.Entity.Name;
                string columns = string.Empty;
                string top = string.Empty;
                string page = string.Empty;
                string orderBy = string.Empty;
                string where = string.Empty;
                string join = string.Empty;
                #endregion

                if (fetch.Count.HasValue)
                {
                    top = $"TOP {fetch.Count.Value}";
                }

                if (fetch.Page.HasValue)
                {
                    int recordsPerPage = fetch.Count.HasValue ? fetch.Count.Value : 50;

                    top = string.Empty; // TOP cannot be combined with OFFSET and FETCH in the same query expression

                    page = $"OFFSET {recordsPerPage * fetch.Page.Value} ROWS FETCH NEXT {recordsPerPage} ROWS ONLY";
                }

                if (fetch.Entity.Attribute != null && fetch.Entity.Attribute.Any())
                {
                    foreach (Attribute attribute in fetch.Entity.Attribute)
                    {
                        columns += attribute.Name;
                    }
                }
                else
                {
                    columns = "*";
                }

                orderBy = ConvertToSqlOrderByClause(fetch.Entity.Order);

                where = ConvertToSqlWhereClause(fetch.Entity.Filter);

                if (fetch.Entity.LinkEntity != null && fetch.Entity.LinkEntity.Any())
                {
                    string alias = "t0";

                    if (!string.IsNullOrEmpty(fetch.Entity.Alias))
                        alias = fetch.Entity.Alias;

                    join = ConvertToSqlJoin(fetch.Entity.LinkEntity, alias);

                    table = ""; // the method above insert alias in the join statement
                }

                string sql = $"SELECT {top} {columns} FROM {table} {join} {where} {orderBy} {page}";

                return sql;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        #region [Privates]
        private string ConvertToSqlOrderByClause(List<Order> Order)
        {
            string orderBy = string.Empty;

            if (Order == null || !Order.Any())
            {
                return orderBy;
            }

            orderBy = $"ORDER BY ";

            for (int i = 0; i < Order.Count() - 1; i++)
            {
                string direction = string.Empty;

                if (!string.IsNullOrEmpty(Order[i].Descending))
                {
                    direction = Order[i].Descending == "false" ? "ASC" : "DESC";
                }

                orderBy += $"{Order[i].Attribute} {direction}";

                if (i + 1 < Order.Count())
                    orderBy += " ,";
            }

            return orderBy;
        }

        private string ConvertToSqlWhereClause(Filter filter)
        {
            if (filter == null)
            {
                return string.Empty;
            }

            StringBuilder whereClause = new StringBuilder();

            if (filter.Condition != null && filter.Condition.Count > 0)
            {
                foreach (var condition in filter.Condition)
                {
                    if (whereClause.Length > 0)
                    {
                        whereClause.Append($" {filter.Type} ");
                    }

                    whereClause.Append("(");
                    whereClause.Append($"{condition.Attribute} {GetSqlOperator(condition.Operator)}");

                    if (condition.Value != null && condition.Value.Count > 0)
                    {
                        whereClause.Append("(");

                        foreach (var value in condition.Value)
                        {
                            whereClause.Append($"'{value}'");
                            if (value != condition.Value.Last())
                            {
                                whereClause.Append(" OR ");
                            }
                        }

                        whereClause.Append(")");
                    }

                    whereClause.Append(")");
                }
            }

            if (filter.InnerFilter != null)
            {
                if (whereClause.Length > 0)
                {
                    whereClause.Insert(0, "(");
                    whereClause.Append(")");
                }

                string innerFilterClause = ConvertToSqlWhereClause(filter.InnerFilter);

                if (!string.IsNullOrEmpty(innerFilterClause))
                {
                    if (whereClause.Length > 0)
                    {
                        whereClause.Append($" {filter.Type} ");
                    }
                    whereClause.Append(innerFilterClause);
                }
            }

            return "WHERE " + whereClause.ToString();
        }

        private string GetSqlOperator(string fetchXmlOperator)
        {
            switch (fetchXmlOperator.ToLower())
            {
                case "eq":
                    return "=";
                case "neq":
                    return "<>";
                case "gt":
                    return ">";
                case "ge":
                    return ">=";
                case "lt":
                    return "<";
                case "le":
                    return "<=";
                case "like":
                    return "LIKE";
                default:
                    throw new NotSupportedException($"Operator '{fetchXmlOperator}' is not supported.");
            }
        }

        private string ConvertToSqlJoin(List<LinkEntity> linkEntities, string primaryTableAlias = "t0")
        {
            var joinStringBuilder = new StringBuilder();

            joinStringBuilder.Append($"FROM {linkEntities[0].Name} AS {primaryTableAlias}");

            var aliasCounters = new Dictionary<string, int> { { primaryTableAlias, 0 } };

            for (int i = 1; i < linkEntities.Count; i++)
            {
                var linkEntity = linkEntities[i];
                string parentAlias = GetTableAlias(linkEntity.From, aliasCounters, primaryTableAlias);
                string childAlias = GetTableAlias(linkEntity.Name, aliasCounters);

                switch (linkEntity.LinkType)
                {
                    case "outer":
                        joinStringBuilder.Append($" LEFT JOIN {linkEntity.Name} AS {childAlias} " +
                                                 $"ON {parentAlias}.{linkEntity.To} = {childAlias}.{linkEntity.From}");
                        break;
                    case "inner":
                    default:
                        joinStringBuilder.Append($" INNER JOIN {linkEntity.Name} AS {childAlias} " +
                                                 $"ON {parentAlias}.{linkEntity.To} = {childAlias}.{linkEntity.From}");
                        break;
                }
            }

            return joinStringBuilder.ToString();
        }

        private string GetTableAlias(string tableName, Dictionary<string, int> aliasCounters, string primaryTableAlias = null)
        {
            string aliasBase = tableName.ToLowerInvariant();
            if (aliasCounters.TryGetValue(aliasBase, out int aliasCounter))
            {
                aliasCounters[aliasBase]++;
                return $"{aliasBase}{aliasCounter}";
            }

            aliasCounters[aliasBase] = 1;
            return aliasBase;
        }

        private bool IsValidXml(string xmlString)
        {
            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();

                settings.ValidationType = ValidationType.None;

                using (XmlReader reader = XmlReader.Create(new System.IO.StringReader(xmlString), settings))
                {
                    while (reader.Read())
                    {
                    }
                }

                return true;
            }
            catch (XmlException)
            {
                return false;
            }
        }

        private Fetch DeserializeXML(string xml)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Fetch));

                using (StringReader reader = new StringReader(xml))
                {
                    return (Fetch)serializer.Deserialize(reader);
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }
        #endregion
    }
}
