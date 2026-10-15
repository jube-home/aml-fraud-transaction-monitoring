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

using System.ComponentModel;
using Jube.Dictionary.Extensions;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;

namespace Jube.Service.Extensions.StructuredData
{
    [FunctionToolGroup("StructuredData",
        "Reads values out of JSON and XML documents by path without loading them into the conversation, and checks that documents are well formed. DTDs and external entities are refused.")]
    public static class StructuredDataTools
    {
        [FunctionTool("IsValidJson")]
        [Description("Whether a text is well-formed JSON (up to 1,000,000 characters and 64 levels deep).")]
        public static bool IsValidJson([Description("The JSON text.")] string json)
        {
            return FunctionToolInput.Text(json, nameof(json), 1_000_000).IsValidJson();
        }

        [FunctionTool("JsonValueAtPath")]
        [Description(
            "The value at a path in a JSON document as text: strings unquoted, anything else as compact JSON. Paths use dots and brackets with an optional $, such as $.customer.name, customer.tags[0], items[-1] (last) or $['odd key']. Empty when the path is missing.")]
        public static string JsonValueAtPath(
            [Description("The JSON document.")] string json,
            [Description("The path, such as $.customer.limits[0].amount.")]
            string path)
        {
            return FunctionToolInput.Text(json, nameof(json), 1_000_000)
                .JsonTextAt(FunctionToolInput.Text(path, nameof(path), 1000));
        }

        [FunctionTool("JsonNumberAtPath")]
        [Description(
            "The number at a path in a JSON document; refused when the path is missing or not a number. Uses the same paths as JsonValueAtPath.")]
        public static double JsonNumberAtPath(
            [Description("The JSON document.")] string json,
            [Description("The path to a numeric value.")]
            string path)
        {
            return FunctionToolInput.Defined(
                FunctionToolInput.Text(json, nameof(json), 1_000_000)
                    .JsonNumberAt(FunctionToolInput.Text(path, nameof(path), 1000)),
                "the path is missing or does not hold a number.");
        }

        [FunctionTool("JsonArrayLengthAtPath")]
        [Description(
            "Number of elements in the array at a path in a JSON document; -1 when the path is missing or not an array.")]
        public static int JsonArrayLengthAtPath(
            [Description("The JSON document.")] string json,
            [Description("The path to an array, such as $.items.")]
            string path)
        {
            return FunctionToolInput.Text(json, nameof(json), 1_000_000)
                .JsonArrayLengthAt(FunctionToolInput.Text(path, nameof(path), 1000));
        }

        [FunctionTool("JsonHasPath")]
        [Description("Whether a path exists and is not null in a JSON document; the same paths as JsonValueAtPath.")]
        public static bool JsonHasPath(
            [Description("The JSON document.")] string json,
            [Description("The path to test.")] string path)
        {
            return FunctionToolInput.Text(json, nameof(json), 1_000_000)
                .JsonHasPath(FunctionToolInput.Text(path, nameof(path), 1000));
        }

        [FunctionTool("MinifyJson")]
        [Description("Removes insignificant whitespace from a JSON document; refused when the text is not valid JSON.")]
        public static string MinifyJson([Description("The JSON document.")] string json)
        {
            return FunctionToolInput.Found(FunctionToolInput.Text(json, nameof(json), 1_000_000).JsonMinified(),
                "the text is not valid JSON.");
        }

        [FunctionTool("IsValidXml")]
        [Description(
            "Whether a text is a well-formed XML document without a DTD (up to 1,000,000 characters). Documents declaring a DTD or entities are refused for safety.")]
        public static bool IsValidXml([Description("The XML text.")] string xml)
        {
            return FunctionToolInput.Text(xml, nameof(xml), 1_000_000).IsValidXml();
        }

        [FunctionTool("XmlValueAtXPath")]
        [Description(
            "Text content of the first node selected by an XPath 1.0 expression, such as /Document/GrpHdr/MsgId or //Nm, in an XML document such as an ISO 20022 payment message; empty when nothing is selected. Namespaced documents need local-name(), for example //*[local-name()='MsgId'].")]
        public static string XmlValueAtXPath(
            [Description("The XML document.")] string xml,
            [Description("The XPath 1.0 expression.")]
            string xpath)
        {
            return FunctionToolInput.Text(xml, nameof(xml), 1_000_000)
                .XmlTextAt(FunctionToolInput.Required(xpath, nameof(xpath), 1000));
        }
    }
}