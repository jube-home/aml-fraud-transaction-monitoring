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

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;

namespace Jube.RegulatoryReport
{
    public static class ReportBuilder
    {
        public static ReportResult Build(ReportProfile profile, JsonNode? data)
        {
            ArgumentNullException.ThrowIfNull(profile);

            var issues = new List<ReportIssue>();

            if (data is null)
            {
                issues.Add(new ReportIssue(ReportIssueStage.Mapping, "NoData", "There is no report data."));

                return new ReportResult(null, null, issues);
            }

            XDocument template;

            try
            {
                template = TemplateRenderer.Parse(profile.TemplateXml);
            }
            catch (Exception ex) when (ex is XmlException or InvalidDataException)
            {
                issues.Add(new ReportIssue(ReportIssueStage.Profile, "TemplateInvalid", ex.Message));

                return new ReportResult(null, null, issues);
            }

            XElement? rendered;

            try
            {
                rendered = new TemplateRenderer(profile, data, issues).Render(template.Root!);
            }
            catch (Exception ex) when (ex is XmlException or FormatException or ArgumentException
                                           or InvalidOperationException or InvalidDataException)
            {
                issues.Add(new ReportIssue(ReportIssueStage.Mapping, "TemplateFailed",
                    "The template could not be applied to this data."));

                return new ReportResult(null, null, issues);
            }

            if (rendered is null)
            {
                issues.Add(new ReportIssue(ReportIssueStage.Mapping, "NothingRendered",
                    "The template produced no report."));

                return new ReportResult(null, null, issues);
            }

            string xml;

            try
            {
                xml = Serialise(new XDocument(new XDeclaration("1.0", "utf-8", null), rendered));
            }
            catch (Exception ex) when (ex is XmlException or ArgumentException or InvalidOperationException)
            {
                issues.Add(new ReportIssue(ReportIssueStage.Mapping, "NotSerialisable",
                    "The data contains characters that cannot be written as XML."));

                return new ReportResult(null, null, issues);
            }

            if (issues.Any(i => i.Stage == ReportIssueStage.Mapping))
            {
                return new ReportResult(null, null, issues);
            }

            issues.AddRange(SchemaValidator.Validate(profile, xml));

            return new ReportResult(xml, Hash(xml), issues);
        }

        public static IReadOnlyList<ReportIssue> CheckProfile(ReportProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);

            var issues = new List<ReportIssue>();

            try
            {
                TemplateRenderer.Parse(profile.TemplateXml);
            }
            catch (Exception ex) when (ex is XmlException or InvalidDataException)
            {
                issues.Add(new ReportIssue(ReportIssueStage.Profile, "TemplateInvalid", ex.Message));
            }

            issues.AddRange(SchemaValidator.Validate(profile, "<probe/>").Where(i => i.Stage == ReportIssueStage.Profile));

            return issues;
        }

        public static ReportResult Validate(ReportProfile profile, string xml)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(xml);

            return new ReportResult(xml, Hash(xml), SchemaValidator.Validate(profile, xml));
        }

        private static string Serialise(XDocument document)
        {
            using var stream = new MemoryStream();

            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false), Indent = true, OmitXmlDeclaration = false
            };

            using (var writer = XmlWriter.Create(stream, settings))
            {
                document.Save(writer);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static string Hash(string xml)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(xml))).ToLowerInvariant();
        }
    }
}
