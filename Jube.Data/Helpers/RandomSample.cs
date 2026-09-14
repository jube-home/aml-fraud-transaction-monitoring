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
using System.Linq.Expressions;
using LinqToDB;

namespace Jube.Data.Helpers
{
    public static class RandomSample
    {
        [ExpressionMethod(nameof(PredicateExpression))]
        public static bool Predicate(double samplePercentage)
        {
            throw new InvalidOperationException(
                $"{nameof(Predicate)} is for use inside a LinqToDB queryable expression only, and is never " +
                "actually invoked -- LinqToDB substitutes PredicateExpression's body at the call site instead.");
        }

        private static Expression<Func<double, bool>> PredicateExpression()
        {
            return samplePercentage => Sql.Expr<bool>($"random() < {samplePercentage / 100.0}");
        }
    }
}