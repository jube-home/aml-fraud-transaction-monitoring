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

using FluentAssertions;
using Jube.Dictionary.Extensions;

namespace Jube.Test.Dictionary.Extensions
{
    public static class FlowAssert
    {
        public static void Kinds<T>(bool testHolds, Flow<T> match, Flow<T> reject, Flow<T> @break, Flow<T> require,
            Flow<T> ensure)
        {
            match.Outcome.Should().Be(testHolds ? FlowOutcome.Matched : FlowOutcome.Undecided);
            reject.Outcome.Should().Be(testHolds ? FlowOutcome.Rejected : FlowOutcome.Undecided);
            @break.Outcome.Should().Be(testHolds ? FlowOutcome.Broken : FlowOutcome.Undecided);
            require.Outcome.Should().Be(testHolds ? FlowOutcome.Undecided : FlowOutcome.Rejected);
            ensure.Outcome.Should().Be(testHolds ? FlowOutcome.Undecided : FlowOutcome.Broken);
        }
    }
}
