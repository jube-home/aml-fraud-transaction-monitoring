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

namespace Jube.Engine.EntityAnalysisModelInvoke.Simulation
{
    using System.Collections.Generic;
    using Dictionary;
    using HttpAdaptationProtocol;

    public sealed class RuleRunInputs
    {
        public DictionaryNoBoxing<string> Data { get; } = new();
        public Dictionary<string, List<string>> Lists { get; init; } = new();
        public PooledDictionary<string, double> Kvp { get; } = new();
        public PooledDictionary<string, double> TtlCounter { get; } = new();
        public PooledDictionary<string, double> Abstraction { get; } = new();
        public PooledDictionary<string, double> AbstractionCalculation { get; } = new();
        public PooledDictionary<string, double> Sanction { get; } = new();
        public PooledDictionary<string, double> ExhaustiveAdaptation { get; } = new();
        public PooledDictionary<string, Adaptation> HttpAdaptation { get; } = new();
        public List<string> Activation { get; } = [];
    }
}