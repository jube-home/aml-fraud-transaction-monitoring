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

namespace Jube.Dictionary.Extensions
{
    public enum FlowOutcome
    {
        Undecided,
        Matched,
        Rejected,
        Broken,
        Errored
    }

    public enum FlowKind
    {
        Match,
        Reject,
        Break,
        Require,
        Ensure
    }

    public readonly struct Flow<T>(T value, FlowOutcome outcome, double score, string? label)
    {
        public T Value { get; } = value;

        public FlowOutcome Outcome { get; } = outcome;

        public double Score { get; } = score;

        public string? Label { get; } = label;

        public bool IsDecided => Outcome != FlowOutcome.Undecided;

        public Flow<T> Apply(FlowKind kind, bool test)
        {
            if (IsDecided)
            {
                return this;
            }

            return kind switch
            {
                FlowKind.Match => test ? Decide(FlowOutcome.Matched) : this,
                FlowKind.Reject => test ? Decide(FlowOutcome.Rejected) : this,
                FlowKind.Break => test ? Decide(FlowOutcome.Broken) : this,
                FlowKind.Require => test ? this : Decide(FlowOutcome.Rejected),
                FlowKind.Ensure => test ? this : Decide(FlowOutcome.Broken),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        public Flow<T> Decide(FlowOutcome outcome, string? label = null)
        {
            return new Flow<T>(Value, outcome, Score, label ?? Label);
        }

        public Flow<T> WithScore(double score)
        {
            return new Flow<T>(Value, Outcome, score, Label);
        }

        public Flow<T> WithLabel(string? label)
        {
            return new Flow<T>(Value, Outcome, Score, label);
        }

        public Flow<TNext> Carry<TNext>(TNext value)
        {
            return new Flow<TNext>(value, Outcome, Score, Label);
        }

        public static implicit operator bool(Flow<T> flow)
        {
            return flow.Outcome == FlowOutcome.Matched;
        }

        public static bool operator |(Flow<T> left, Flow<T> right)
        {
            return left.Outcome == FlowOutcome.Matched || right.Outcome == FlowOutcome.Matched;
        }

        public static bool operator &(Flow<T> left, Flow<T> right)
        {
            return left.Outcome == FlowOutcome.Matched && right.Outcome == FlowOutcome.Matched;
        }

        public static bool operator |(Flow<T> left, bool right)
        {
            return left.Outcome == FlowOutcome.Matched || right;
        }

        public static bool operator &(Flow<T> left, bool right)
        {
            return left.Outcome == FlowOutcome.Matched && right;
        }

        public static bool operator !(Flow<T> flow)
        {
            return flow.Outcome != FlowOutcome.Matched;
        }
    }
}
