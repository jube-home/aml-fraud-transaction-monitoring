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

using System.Runtime.CompilerServices;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    [InterpolatedStringHandler]
    public ref struct TraceLogInterpolatedStringHandler
    {
        private DefaultInterpolatedStringHandler inner;

        public TraceLogInterpolatedStringHandler(int literalLength, int formattedCount, Context context,
            out bool handlerIsValid)
        {
            IsEnabled = TraceLogExtensions.WantsTraceLog(context);
            handlerIsValid = IsEnabled;
            inner = IsEnabled ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
        }

        internal bool IsEnabled { get; }

        public void AppendLiteral(string value)
        {
            inner.AppendLiteral(value);
        }

        public void AppendFormatted<T>(T value)
        {
            inner.AppendFormatted(value);
        }

        internal string GetFormattedTextAndClear()
        {
            return inner.ToStringAndClear();
        }
    }
}