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


namespace Jube.Service.Charts.Datasource.Template
{
    public sealed class TemplateScope(TemplateScope? parent, string name, object? value)
    {
        public static TemplateScope Empty { get; } = new(null, string.Empty, null);

        public TemplateScope With(string newName, object? newValue)
        {
            return new TemplateScope(this, newName, newValue);
        }

        public object? Lookup(string wanted)
        {
            for (var scope = this; scope != null; scope = scope.Parent)
            {
                if (string.Equals(scope.Name, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    return scope.Value;
                }
            }

            return null;
        }

        private TemplateScope? Parent => parent;
        private string Name => name;
        private object? Value => value;
    }
}
