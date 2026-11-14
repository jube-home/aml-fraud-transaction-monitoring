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

using System.Xml;
using System.Xml.Schema;

namespace Jube.RegulatoryReport
{
    internal static class SchemaValidator
    {
        public static IReadOnlyList<ReportIssue> Validate(ReportProfile profile, string xml)
        {
            var issues = new List<ReportIssue>();

            if (profile.SchemaDocuments.Count == 0)
            {
                issues.Add(new ReportIssue(ReportIssueStage.Profile, "NoSchema",
                    "The profile has no schema to validate against; add the schema the FIU issued."));

                return issues;
            }

            var set = new XmlSchemaSet { XmlResolver = null };

            void OnSchemaProblem(object? sender, ValidationEventArgs e)
            {
                if (e.Severity == XmlSeverityType.Error)
                {
                    issues.Add(new ReportIssue(ReportIssueStage.Profile, "SchemaInvalid", e.Message, null,
                        e.Exception.LineNumber, e.Exception.LinePosition));
                }
            }

            try
            {
                foreach (var (name, text) in profile.SchemaDocuments)
                {
                    var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
                    using var reader = XmlReader.Create(new StringReader(text), settings, name);
                    set.Add(XmlSchema.Read(reader, OnSchemaProblem)!);
                }

                set.ValidationEventHandler += OnSchemaProblem;
                set.Compile();
            }
            catch (Exception ex) when (ex is XmlException or XmlSchemaException)
            {
                issues.Add(new ReportIssue(ReportIssueStage.Profile, "SchemaInvalid", ex.Message));

                return issues;
            }

            if (issues.Count > 0)
            {
                return issues;
            }

            var path = new Stack<string>();

            var readerSettings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                ValidationType = ValidationType.Schema,
                Schemas = set,
                ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings
            };

            readerSettings.ValidationEventHandler += (_, e) =>
            {
                if (e.Severity == XmlSeverityType.Error)
                {
                    issues.Add(new ReportIssue(ReportIssueStage.Schema, "SchemaViolation", e.Message,
                        "/" + string.Join("/", path.Reverse()), e.Exception.LineNumber, e.Exception.LinePosition));
                }
            };

            try
            {
                using var reader = XmlReader.Create(new StringReader(xml), readerSettings);

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        path.Push(reader.LocalName);

                        if (reader.IsEmptyElement)
                        {
                            path.Pop();
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && path.Count > 0)
                    {
                        path.Pop();
                    }
                }
            }
            catch (XmlException ex)
            {
                issues.Add(new ReportIssue(ReportIssueStage.Schema, "XmlInvalid", ex.Message, null, ex.LineNumber,
                    ex.LinePosition));
            }

            return issues;
        }
    }
}
