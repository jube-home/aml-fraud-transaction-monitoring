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

using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;

namespace Jube.Service.Query.FormModel
{
    public static class ValidatorRules
    {
        public static Dictionary<string, MemberRules> Analyse(IValidator validator)
        {
            var found = new Dictionary<string, MemberRules>(StringComparer.Ordinal);

            foreach (var member in validator.CreateDescriptor().GetMembersWithValidators())
            {
                if (string.IsNullOrEmpty(member.Key) || member.Key.Contains('.') || member.Key.Contains('['))
                {
                    continue;
                }

                found[member.Key] = Summarise(member);
            }

            return found;
        }

        public static MemberRules Merge(MemberRules left, MemberRules right)
        {
            var min = Tightest(left.Min, left.ExclusiveMin, right.Min, right.ExclusiveMin, true);
            var max = Tightest(left.Max, left.ExclusiveMax, right.Max, right.ExclusiveMax, false);

            return new MemberRules(
                left.Required || right.Required,
                left.RequiredWhenConditional || right.RequiredWhenConditional,
                Largest(left.MinLength, right.MinLength),
                Smallest(left.MaxLength, right.MaxLength),
                min.Value, max.Value, min.Exclusive, max.Exclusive,
                [.. left.Patterns.Concat(right.Patterns).Distinct(StringComparer.Ordinal)]);
        }

        private static MemberRules Summarise(IEnumerable<(IPropertyValidator Validator, IRuleComponent Options)> validators)
        {
            var required = false;
            var conditional = false;
            int? minLength = null;
            int? maxLength = null;
            (double? Value, bool Exclusive) min = (null, false);
            (double? Value, bool Exclusive) max = (null, false);
            var patterns = new List<string>();

            foreach (var (validator, options) in validators)
            {
                var hasCondition = options.HasCondition || options.HasAsyncCondition;

                switch (validator)
                {
                    case { Name: "NotEmptyValidator" or "NotNullValidator" }:
                        if (hasCondition)
                        {
                            conditional = true;
                        }
                        else
                        {
                            required = true;
                        }

                        break;
                    case IRegularExpressionValidator regular:
                        var whole = WholeValuePattern.Translate(regular.Expression);

                        if (whole != null)
                        {
                            patterns.Add(whole);
                        }

                        break;
                    case ILengthValidator length:
                        minLength = Largest(minLength, length.Min > 0 ? length.Min : null);
                        maxLength = Smallest(maxLength, length.Max > 0 ? length.Max : null);
                        break;
                    case IBetweenValidator between:
                        min = Tightest(min.Value, min.Exclusive, Number(between.From), validator.Name == "ExclusiveBetweenValidator", true);
                        max = Tightest(max.Value, max.Exclusive, Number(between.To), validator.Name == "ExclusiveBetweenValidator", false);
                        break;
                    case IComparisonValidator { MemberToCompare: null } comparison:
                        var value = Number(comparison.ValueToCompare);

                        switch (comparison.Comparison)
                        {
                            case Comparison.GreaterThan:
                                min = Tightest(min.Value, min.Exclusive, value, true, true);
                                break;
                            case Comparison.GreaterThanOrEqual:
                                min = Tightest(min.Value, min.Exclusive, value, false, true);
                                break;
                            case Comparison.LessThan:
                                max = Tightest(max.Value, max.Exclusive, value, true, false);
                                break;
                            case Comparison.LessThanOrEqual:
                                max = Tightest(max.Value, max.Exclusive, value, false, false);
                                break;
                        }

                        break;
                }
            }

            return new MemberRules(required, conditional, minLength, maxLength, min.Value, max.Value, min.Exclusive,
                max.Exclusive, patterns);
        }

        private static double? Number(object? value)
        {
            return value switch
            {
                sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal =>
                    Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture),
                _ => null
            };
        }

        private static (double? Value, bool Exclusive) Tightest(double? left, bool leftExclusive, double? right,
            bool rightExclusive, bool lower)
        {
            if (left == null)
            {
                return (right, right != null && rightExclusive);
            }

            if (right == null)
            {
                return (left, leftExclusive);
            }

            if (left == right)
            {
                return (left, leftExclusive || rightExclusive);
            }

            var rightWins = lower ? right > left : right < left;

            return rightWins ? (right, rightExclusive) : (left, leftExclusive);
        }

        private static int? Largest(int? left, int? right)
        {
            return left == null ? right : right == null ? left : Math.Max(left.Value, right.Value);
        }

        private static int? Smallest(int? left, int? right)
        {
            return left == null ? right : right == null ? left : Math.Min(left.Value, right.Value);
        }
    }
}
