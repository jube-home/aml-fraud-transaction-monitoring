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


namespace Jube.Data.Repository
{
    public enum InvokeStage
    {
        Parse = 1,
        CheckIntegrityAndUpsert = 2,
        InlineFunctions = 3,
        InlineScripts = 4,
        Gateway = 5,
        CacheDbStorage = 6,
        Sanctions = 7,
        TtlCounters = 8,
        AbstractionRulesWithSearchKeys = 9,
        JoinReadTasks = 10,
        AbstractionRulesWithoutSearchKeys = 11,
        AbstractionCalculations = 12,
        ExhaustiveAdaptation = 13,
        HttpAdaptation = 14,
        Activation = 15,
        JoinWriteTasks = 16,
        WriteResponse = 17,
        BuildArchivePayload = 18
    }
}