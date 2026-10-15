/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.ClientModel.Primitives;

namespace Jube.AskJooby.Observability
{
    public sealed class ModelAffinityPolicy : PipelinePolicy
    {
        public const string HeaderName = "X-Model-Affinity";

        public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            Stamp(message);
            ProcessNext(message, pipeline, currentIndex);
        }

        public override ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline,
            int currentIndex)
        {
            Stamp(message);

            return ProcessNextAsync(message, pipeline, currentIndex);
        }

        private static void Stamp(PipelineMessage message)
        {
            if (ModelAffinityScope.Current is { } affinity)
            {
                message.Request.Headers.Set(HeaderName, affinity);
            }
        }
    }
}