/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Jube.RegulatoryReport;

namespace Jube.Test.RegulatoryReport
{
    internal static class ExampleProfile
    {
        public const string Schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="urn:example:report"
                       xmlns="urn:example:report" elementFormDefault="qualified">
              <xs:element name="report">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element name="reference" type="xs:string"/>
                    <xs:element name="submitted" type="xs:dateTime"/>
                    <xs:element name="reason" type="xs:string" minOccurs="0"/>
                    <xs:element name="transaction" maxOccurs="unbounded">
                      <xs:complexType>
                        <xs:sequence>
                          <xs:element name="amount" type="xs:decimal"/>
                          <xs:element name="country" type="xs:string"/>
                        </xs:sequence>
                        <xs:attribute name="currency" type="xs:string" use="required"/>
                      </xs:complexType>
                    </xs:element>
                  </xs:sequence>
                </xs:complexType>
              </xs:element>
            </xs:schema>
            """;

        public const string Template = """
            <report xmlns="urn:example:report" xmlns:jr="urn:jube:regulatory-report:template:1">
              <reference jr:value="reference" jr:required="true"/>
              <submitted jr:value="submitted|date:yyyy-MM-ddTHH:mm:ss" jr:required="true"/>
              <reason jr:value="reason"/>
              <transaction jr:each="transactions" jr:required="true" jr:attributes="currency=currency">
                <amount jr:value="amount|number:0.00" jr:required="true"/>
                <country jr:value="country|map:countries" jr:required="true"/>
              </transaction>
            </report>
            """;

        public static ReportProfile Create(string? template = null, string? schema = null)
        {
            return new ReportProfile("example", template ?? Template,
                new System.Collections.Generic.Dictionary<string, string> { ["example.xsd"] = schema ?? Schema },
                new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IReadOnlyDictionary<string, string>>
                {
                    ["countries"] = new System.Collections.Generic.Dictionary<string, string> { ["United Kingdom"] = "GB", ["Kuwait"] = "KW" }
                });
        }
    }
}
