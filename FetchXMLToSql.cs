using FetchXMLToSqlConverter.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Attribute = FetchXMLToSqlConverter.Model.Attribute;

namespace FetchXMLSqlConverter
{
    public class FetchXMLToSql
    {
        public static string ConvertToSQL(string FetchXML)
        {
            try
            {
                if (!IsValidXml(FetchXML))
                {
                    throw new Exception("Invalid FetchXML!");
                }

                Fetch fetch = DeserializeXML(FetchXML);

                #region [Variables]
                bool distinct = string.IsNullOrEmpty(fetch.Distinct) ? false : fetch.Distinct == "true";
                string table = fetch.Entity.Name;
                List<string> columns = new List<string>();
                string columnSql = string.Empty;
                string top = string.Empty;
                string page = string.Empty;
                string orderBy = string.Empty;
                string where = string.Empty;
                string join = string.Empty;
                #endregion

                if (!string.IsNullOrEmpty(fetch.Count))
                {
                    top = $"TOP {fetch.Count}";
                }

                if (!string.IsNullOrEmpty(fetch.Page))
                {
                    int recordsPerPage = !string.IsNullOrEmpty(fetch.Count) ? int.Parse(fetch.Count) : 50;

                    top = string.Empty; // TOP cannot be combined with OFFSET and FETCH in the same query expression

                    page = $"OFFSET {recordsPerPage * int.Parse(fetch.Page)} ROWS FETCH NEXT {recordsPerPage} ROWS ONLY";
                }

                if (fetch.Entity.Attribute != null && fetch.Entity.Attribute.Any())
                {
                    foreach (Attribute attribute in fetch.Entity.Attribute)
                    {
                        columns.Add(attribute.Name);
                    }

                    columnSql = string.Join(',', columns);
                }
                else
                {
                    columnSql = "*";
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

                string sql = $"SELECT {top} {columnSql} FROM {table} {join} {where} {orderBy} {page}";

                return sql;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        #region [Privates]
        private static string ConvertToSqlOrderByClause(List<Order> Order)
        {
            string orderBy = string.Empty;

            if (Order == null || !Order.Any())
            {
                return orderBy;
            }

            orderBy = $"ORDER BY ";

            for (int i = 0; i < Order.Count(); i++)
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

        private static string ConvertToSqlWhereClause(Filter filter)
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
                    whereClause.Append($"{condition.Attribute} {GetSqlOperator(condition.Operator)} ");

                    if (!string.IsNullOrEmpty(condition.Value))
                    {
                        whereClause.Append($"'{condition.Value}'");
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

        private static string GetSqlOperator(string fetchXmlOperator)
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
                case "null":
                    return "IS NULL";
                case "not-null":
                    return "IS NOT NULL";
                default:
                    throw new NotSupportedException($"Operator '{fetchXmlOperator}' is not supported.");
            }
        }

        private static string ConvertToSqlJoin(List<LinkEntity> linkEntities, string primaryTableAlias = "t0")
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

        private static string GetTableAlias(string tableName, Dictionary<string, int> aliasCounters, string primaryTableAlias = null)
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

        private static bool IsValidXml(string xmlString)
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

        private static Fetch DeserializeXML(string xml)
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
