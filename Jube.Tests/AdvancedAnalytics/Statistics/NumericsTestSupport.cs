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

using System;
using FluentAssertions;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public static class NumericsTestSupport
    {
        public static void MatrixShouldMatch(double[][] actual, double[][] expected, double tolerance)
        {
            actual.Length.Should().Be(expected.Length);

            for (var i = 0; i < expected.Length; i++)
            {
                actual[i].Length.Should().Be(expected[i].Length);

                for (var j = 0; j < expected[i].Length; j++)
                {
                    actual[i][j].Should().BeApproximately(expected[i][j], tolerance, $"element ({i},{j})");
                }
            }
        }

        public static void VectorShouldMatch(double[] actual, double[] expected, double tolerance)
        {
            actual.Length.Should().Be(expected.Length);

            for (var i = 0; i < expected.Length; i++)
            {
                actual[i].Should().BeApproximately(expected[i], tolerance, $"element {i}");
            }
        }

        public static void ShouldRejectWith<T>(Func<T> call, string parameterName)
        {
            var thrown = Assert.Throws<ArgumentException>(() => call());
            thrown.ParamName.Should().Be(parameterName);
        }
    }
}