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

using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class FlowBooleanTests
    {
        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void IsTrueAppliesTheOutcomeOfEveryStepKind(bool value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsTrue(),
                start.RejectIsTrue(),
                start.BreakIsTrue(),
                start.RequireIsTrue(),
                start.EnsureIsTrue());

            FlowAssert.Kinds(expected,
                value.MatchIsTrue(),
                value.RejectIsTrue(),
                value.BreakIsTrue(),
                value.RequireIsTrue(),
                value.EnsureIsTrue());
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void IsFalseAppliesTheOutcomeOfEveryStepKind(bool value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsFalse(),
                start.RejectIsFalse(),
                start.BreakIsFalse(),
                start.RequireIsFalse(),
                start.EnsureIsFalse());

            FlowAssert.Kinds(expected,
                value.MatchIsFalse(),
                value.RejectIsFalse(),
                value.BreakIsFalse(),
                value.RequireIsFalse(),
                value.EnsureIsFalse());
        }
    }
}
