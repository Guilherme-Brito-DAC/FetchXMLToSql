# FetchXMLToSql

Microsoft FetchXML to SQL (SQL SERVER)

```
string FetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='contact'>
                                    <attribute name='fullname' />
                                    <attribute name='emailaddress1' />
                                    <attribute name='telephone1' />
                                    <order attribute='fullname' descending='false' />
                                    <filter type='and'>
                                      <condition attribute='address1_city' operator='eq' value='Seattle' />
                                    </filter>
                                  </entity>
                                </fetch>";

string SqlResult = FetchXMLToSql.ConvertToSQL(FetchXml);

// SELECT fullname,emailaddress1,telephone1 FROM contact WHERE (address1_city = 'Seattle') ORDER BY fullname ASC

```

<i>Some operators are missing, they will be added in the next release</i>

Feel free to to contribute :) 

# Installation

Nuget URL : https://www.nuget.org/packages/FetchXMLToSql/1.0.0
`dotnet add package FetchXMLToSql --version 1.0.0` 
