using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

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

        [XmlRoot(ElementName = "order")]
        private class Order
        {

            [XmlAttribute(AttributeName = "attribute")]
            public string Attribute { get; set; }

            [XmlAttribute(AttributeName = "descending")]
            public string Descending { get; set; }
        }

        [XmlRoot(ElementName = "filter")]
        private class Filter
        {

            [XmlElement(ElementName = "condition")]
            public List<Condition> Condition { get; set; }

            [XmlAttribute(AttributeName = "type")]
            public string Type { get; set; }

            // And & Or

            [XmlText]
            public string Text { get; set; }

            [XmlElement(ElementName = "filter")]
            public Filter InnerFilter { get; set; }
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

            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }

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

        #region [Enums]
        private enum Operators
        {
            [Description("lt")]
            LessThan,
            [Description("gt")]
            GreaterThan,
            [Description("le")]
            LessThanorEqualTo,
            [Description("ge")]
            GreaterThanorEqualTo,
            [Description("eq")]
            Equals,
            [Description("ne")]
            NotEquals,
            [Description("neq")]
            NotEqualTo,
            [Description("null")]
            DoesNotContainData,
            [Description("not-null")]
            ContainsData,
            [Description("in")]
            IsIn,
            [Description("not-in")]
            IsNotIn,
            [Description("between")]
            Between,
            [Description("not-between")]
            IsNotBetween,
            [Description("like")]
            Like,
            [Description("not-like")]
            NotLike,
            [Description("yesterday")]
            Yesterday,
            [Description("today")]
            Today,
            [Description("tomorrow")]
            Tomorrow,
            [Description("next-seven-days")]
            NextSevenDays,
            [Description("last-seven-days")]
            LastSevenDays,
            [Description("next-week")]
            NextWeek,
            [Description("last-week")]
            LastWeek,
            [Description("this-month")]
            ThisMonth,
            [Description("last-month")]
            LastMonth,
            [Description("next-month")]
            NextMonth,
            [Description("on")]
            On,
            [Description("on-or-before")]
            OnorBefore,
            [Description("on-or-after")]
            OnonAfter,
            [Description("this-year")]
            ThisYear,
            [Description("last-year")]
            LastYear,
            [Description("next-year")]
            NextYear,
            [Description("eq-userid")]
            EqualsCurrentUser,
            [Description("ne-userid")]
            DoesNotEqualCurrentUser,
            [Description("eq-businessid")]
            EqualsCurrentBusinessUnit,
            [Description("ne-businessid")]
            DoesNotEqualCurrentBusinessUnit,
            [Description("this-week")]
            ThisWeek,
            [Description("last-x-months")]
            LastXMonths,
            [Description("eq-userlanguage")]
            EqualsUserLanguage,
            [Description("eq-userteams")]
            EqualsCurrentUsersTeams,
            [Description("in-fiscal-year")]
            InFiscalYear,
            [Description("in-fiscal-period")]
            InFiscalPeriod,
            [Description("in-fiscal-period-and-year")]
            InFiscalPeriodandYear,
            [Description("in-or-after-fiscal-period-and-year")]
            InorAfterFiscalPeriodandYear,
            [Description("in-or-before-fiscal-period-and-year")]
            InorBeforeFiscalPeriodandYear,
            [Description("last-fiscal-year")]
            LastFiscalYear,
            [Description("this-fiscal-year")]
            ThisFiscalYear,
            [Description("next-fiscal-year")]
            NextFiscalYear,
            [Description("last-x-fiscal-years")]
            LastXFiscalYears,
            [Description("next-x-fiscal-years")]
            NextXFiscalYears,
            [Description("last-fiscal-period")]
            LastFiscalPeriod,
            [Description("this-fiscal-period")]
            ThisFiscalPeriod,
            [Description("next-fiscal-period")]
            NextFiscalPeriod,
            [Description("last-x-fiscal-periods")]
            LastXFiscalPeriods,
            [Description("next-x-fiscal-periods")]
            NextXFiscalPeriods
        }
        #endregion

        #endregion

        public string ConvertToSQL(string FetchXML)
        {
            try
            {
                // TO DO - Validate FetchXML

                if (false)
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

                if (fetch.Entity.Order != null && fetch.Entity.Order.Any())
                {
                    orderBy = $"ORDER BY ";

                    for (int i = 0; i < fetch.Entity.Order.Count() - 1; i++)
                    {
                        string direction = string.Empty;

                        if (!string.IsNullOrEmpty(fetch.Entity.Order[i].Descending))
                        {
                            direction = fetch.Entity.Order[i].Descending == "false" ? "ASC" : "DESC";
                        }

                        orderBy += $"{fetch.Entity.Order[i].Attribute} {direction}";

                        if (i + 1 < fetch.Entity.Order.Count())
                            orderBy += " ,";
                    }
                }

                // Order By

                // Filter

                // Link Entities

                string sql = $"SELECT {top} {columns} FROM {table} {where} {orderBy} {page}";

                return sql;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        #region [Privates]
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
