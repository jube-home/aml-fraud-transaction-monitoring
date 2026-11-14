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

using MessagePack;

namespace Jube.CaseRisk.Analytics
{
    [MessagePackObject]
    public sealed record HandoffBucket([property: Key(0)] string Label, [property: Key(1)] int Cases);

    [MessagePackObject]
    public sealed record HandoffCase([property: Key(0)] int CaseId, [property: Key(1)] int Handoffs, [property: Key(2)] int Reopens);

    [MessagePackObject]
    public sealed record HandoffReport(
        [property: Key(0)] List<HandoffBucket> Distribution,
        [property: Key(1)] int ReopenedCases,
        [property: Key(2)] double ReopenRate,
        [property: Key(3)] List<HandoffCase> PingPong);
}
