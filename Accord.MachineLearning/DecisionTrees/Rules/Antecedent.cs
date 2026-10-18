// Accord.NET Framework (originally)
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
// Relicensed to MIT per the author's own explicit grant made when the
// upstream project was archived (2020-11-19):
// https://github.com/accord-net/framework
// "I am also willing to change the license of any file where I am the
// single author... to MIT so people can reuse individual pieces of code
// more easily." -- Cesar De Souza
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;

namespace Accord.MachineLearning.DecisionTrees.Rules
{
    /// <summary>
    ///     Antecedent expression for <see cref="DecisionRule" />s.
    /// </summary>
    [Serializable]
    public struct Antecedent : IEquatable<Antecedent>
    {
        /// <summary>
        ///     Gets the index of the variable used as the
        ///     left hand side term of this expression.
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     Gets the comparison being made between the variable
        ///     value at <see cref="Index" /> and <see cref="Value" />.
        /// </summary>
        public ComparisonKind Comparison { get; }

        /// <summary>
        ///     Gets the right hand side of this expression.
        /// </summary>
        public double Value { get; }

        /// <summary>
        ///     Creates a new instance of the <see cref="Antecedent" /> class.
        /// </summary>
        /// <param name="index">The variable index.</param>
        /// <param name="comparison">
        ///     The comparison to be made using the value at
        ///     <paramref name="index" /> and <paramref name="value" />.
        /// </param>
        /// <param name="value">The value to be compared against.</param>
        public Antecedent(int index, ComparisonKind comparison, double value)
        {
            this.Index = index;
            this.Comparison = comparison;
            this.Value = value;
        }

        /// <summary>
        ///     Checks if this antecedent applies to a given input.
        /// </summary>
        /// <param name="input">An input vector.</param>
        /// <returns>
        ///     True if the input element at position <see cref="Index" />
        ///     compares to <see cref="Value" /> using <see cref="Comparison" />; false
        ///     otherwise.
        /// </returns>
        public bool Match(double[] input)
        {
            var x = input[Index];

            if (double.IsNaN(x))
            {
                return true;
            }

            switch (Comparison)
            {
                case ComparisonKind.Equal:
                    return x == Value;

                case ComparisonKind.GreaterThan:
                    return x > Value;

                case ComparisonKind.GreaterThanOrEqual:
                    return x >= Value;

                case ComparisonKind.LessThan:
                    return x < Value;

                case ComparisonKind.LessThanOrEqual:
                    return x <= Value;

                case ComparisonKind.NotEqual:
                    return x != Value;

                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        ///     Determines whether the specified <see cref="Antecedent" />
        ///     is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="Antecedent" /> to compare with this instance.</param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="Antecedent" />
        ///     is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(Antecedent other)
        {
            if (Comparison != other.Comparison)
            {
                return false;
            }

            if (Index != other.Index)
            {
                return false;
            }

            if (Value != other.Value)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Determines whether the specified <see cref="System.Object" />
        ///     is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="System.Object" />
        ///     is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Antecedent))
            {
                return false;
            }

            return Equals((Antecedent)obj);
        }

        /// <summary>
        ///     Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        ///     A hash code for this instance, suitable for use in
        ///     hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return unchecked(Comparison.GetHashCode() +
                             13 * Index.GetHashCode() +
                             13 * Value.GetHashCode());
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var op = ComparisonExtensions.ToString(Comparison);
            return string.Format("x[{0}] {1} {2}", Index, op, Value);
        }

        /// <summary>
        ///     Implements the operator ==.
        /// </summary>
        public static bool operator ==(Antecedent a, Antecedent b)
        {
            return a.Equals(b);
        }

        /// <summary>
        ///     Implements the operator !=.
        /// </summary>
        public static bool operator !=(Antecedent a, Antecedent b)
        {
            return !a.Equals(b);
        }
    }
}