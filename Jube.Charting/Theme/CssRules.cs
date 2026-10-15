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

namespace Jube.Charting.Theme
{
    internal sealed class CssRules(string scope, char separator = ',')
    {
        private readonly StringBuilder css = new();

        public CssRules Rule(string selector, string body)
        {
            css.Append(string.Join(',', selector.Split(separator).Select(part => scope + ' ' + part))).Append('{')
                .Append(body).Append('}');

            return this;
        }

        public CssRules Has(string condition, string selector, string body)
        {
            css.Append(string.Join(',',
                    selector.Split(separator).Select(part => scope + ":has(" + condition + ") " + part)))
                .Append('{').Append(body).Append('}');

            return this;
        }

        public CssRules Raw(string cssText)
        {
            css.Append(cssText);

            return this;
        }

        public override string ToString()
        {
            return css.ToString();
        }
    }
}