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

using System.Text;
using Jube.Charting.Exceptions;

namespace Jube.Charting.Svg
{
    internal sealed class SvgWriter
    {
        private readonly StringBuilder builder = new();
        private readonly Stack<string> open = new();
        public int Length => builder.Length;

        public SvgWriter Open(string name, params (string Name, string? Value)[] attributes)
        {
            WriteStart(name, attributes);
            builder.Append('>');
            open.Push(name);

            return this;
        }

        public SvgWriter Close()
        {
            var name = open.Pop();
            builder.Append("</").Append(name).Append('>');

            return this;
        }

        public SvgWriter Empty(string name, params (string Name, string? Value)[] attributes)
        {
            WriteStart(name, attributes);
            builder.Append("/>");

            return this;
        }

        public SvgWriter Element(string name, string? text, params (string Name, string? Value)[] attributes)
        {
            WriteStart(name, attributes);
            builder.Append('>').Append(XmlText.Escape(text)).Append("</").Append(name).Append('>');

            return this;
        }

        public SvgWriter Text(string? text)
        {
            builder.Append(XmlText.Escape(text));

            return this;
        }

        public SvgWriter Raw(string markup)
        {
            builder.Append(markup);

            return this;
        }

        public SvgWriter Style(string css)
        {
            if (css.Contains("</", StringComparison.Ordinal) || css.Contains("<!", StringComparison.Ordinal))
            {
                throw new ChartingException("Stylesheet content must not contain markup.");
            }

            builder.Append("<style>").Append(css).Append("</style>");

            return this;
        }

        public override string ToString()
        {
            if (open.Count != 0)
            {
                throw new ChartingException("Unclosed SVG element " + open.Peek());
            }

            return builder.ToString();
        }

        private void WriteStart(string name, (string Name, string? Value)[] attributes)
        {
            builder.Append('<').Append(name);

            foreach (var (attributeName, value) in attributes)
            {
                if (value is null)
                {
                    continue;
                }

                builder.Append(' ').Append(attributeName).Append("=\"")
                    .Append(XmlText.Escape(value,
                        attributeName is "d" or "style"
                            ? ChartLimits.MaxOutputCharacters
                            : ChartLimits.MaxTextCharacters))
                    .Append('"');
            }
        }
    }
}