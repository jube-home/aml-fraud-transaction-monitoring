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

using Jube.Service.CaseDispatch.Models;

namespace Jube.Service.CaseDispatch
{
    public sealed class DispatchPlan
    {
        private readonly CaseDispatcher dispatcher;

        internal DispatchPlan(CaseDispatcher dispatcher, DispatchSource source, DispatchCase target,
            IReadOnlyDictionary<string, string?>? values, LegacyRender? legacy, CaseRows rows,
            IReadOnlyDictionary<string, object?>? roots, bool refused)
        {
            this.dispatcher = dispatcher;
            Source = source;
            Target = target;
            Values = values;
            Legacy = legacy;
            Rows = rows;
            Roots = roots;
            Refused = refused;
        }

        internal DispatchSource Source { get; }
        internal DispatchCase Target { get; }
        internal IReadOnlyDictionary<string, string?>? Values { get; }
        internal LegacyRender? Legacy { get; }
        internal IReadOnlyDictionary<string, object?>? Roots { get; }
        internal bool Refused { get; }

        public CaseRows Rows { get; }

        public Task<string?> NotifyAsync(CancellationToken token = default)
        {
            return dispatcher.NotifyPlanAsync(this, token);
        }

        public Task<string?> CallAsync(CancellationToken token = default)
        {
            return dispatcher.CallPlanAsync(this, token);
        }
    }
}
