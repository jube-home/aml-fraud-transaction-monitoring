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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Accord.MachineLearning.DecisionTrees.Learning;
using Accord.Math;
using Accord.Statistics.Filters;

namespace Accord.MachineLearning.DecisionTrees.Rules
{
    /// <summary>
    ///     Decision Rule.
    /// </summary>
    /// <example>
    ///     <para>
    ///         The simplest way to create a set of decision rules is by extracting them from an existing
    ///         <see cref="DecisionTree" />.
    ///         The example below shows how to create a simple decision tree and convert it to a
    ///         <see cref="DecisionSet">set of rules</see>
    ///         using its <see cref="DecisionTree.ToRules()" /> method.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\DecisionTrees\C45LearningTest.cs" region="doc_simplest" />
    /// </example>
    /// <seealso cref="DecisionSet" />
    /// <seealso cref="DecisionTree" />
    /// <seealso cref="C45Learning" />
    /// <seealso cref="ID3Learning" />
    [Serializable]
    public class DecisionRule : ICloneable, IEnumerable<Antecedent>,
        IEquatable<DecisionRule>, IComparable<DecisionRule>
    {
        private List<Antecedent> expressions;
        private double output;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DecisionRule" /> class.
        /// </summary>
        /// <param name="variables">The decision variables handled by this decision rule.</param>
        /// <param name="output">The output value, given after all antecedents are met.</param>
        /// <param name="antecedents">The antecedent conditions that lead to the <paramref name="output" />.</param>
        public DecisionRule(IList<DecisionVariable> variables,
            double output, params Antecedent[] antecedents)
            : this(variables, output, (IEnumerable<Antecedent>)antecedents)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DecisionRule" /> class.
        /// </summary>
        /// <param name="variables">The decision variables handled by this decision rule.</param>
        /// <param name="output">The output value, given after all antecedents are met.</param>
        /// <param name="antecedents">The antecedent conditions that lead to the <paramref name="output" />.</param>
        public DecisionRule(IList<DecisionVariable> variables,
            double output, IEnumerable<Antecedent> antecedents)
        {
            this.Variables = new DecisionVariableCollection(variables);
            expressions = new List<Antecedent>(antecedents);
            this.output = output;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DecisionRule" /> class.
        /// </summary>
        /// <param name="output">The output value, given after all antecedents are met.</param>
        /// <param name="antecedents">The antecedent conditions that lead to the <paramref name="output" />.</param>
        public DecisionRule(double output, params Antecedent[] antecedents)
        {
            expressions = new List<Antecedent>(antecedents);
            this.output = output;
        }

        /// <summary>
        ///     Gets the decision variables handled by this rule.
        /// </summary>
        public DecisionVariableCollection Variables { get; }

        /// <summary>
        ///     Gets the <see cref="Antecedent" /> expressions that
        ///     must be fulfilled in order for this rule to be applicable.
        /// </summary>
        public IList<Antecedent> Antecedents => expressions;

        /// <summary>
        ///     Gets or sets the output of this decision rule, given
        ///     when all <see cref="Antecedent" /> conditions are met.
        /// </summary>
        public double Output
        {
            get => output;
            set => output = value;
        }

        /// <summary>
        ///     Gets the number of antecedents contained
        ///     in this <see cref="DecisionRule" />.
        /// </summary>
        public int Count => Antecedents.Count;

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return new DecisionRule(Variables, output, Antecedents);
        }

        /// <summary>
        ///     Compares this instance to another <see cref="DecisionRule" />.
        /// </summary>
        public int CompareTo(DecisionRule other)
        {
            var order = Output.CompareTo(other.Output);

            if (order == 0)
            {
                return Antecedents.Count.CompareTo(other.Antecedents.Count);
            }

            return order;
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that
        ///     can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<Antecedent> GetEnumerator()
        {
            return expressions.GetEnumerator();
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that
        ///     can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return expressions.GetEnumerator();
        }

        /// <summary>
        ///     Determines whether the specified <see cref="DecisionRule" /> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="DecisionRule" /> to compare with this instance.</param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="DecisionRule" />
        ///     is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(DecisionRule other)
        {
            if ((object)other == null)
            {
                return false;
            }

            return Output == other.output
                   && Antecedents.SetEquals(other.Antecedents);
        }

        /// <summary>
        ///     Checks whether a the rule applies to a given input vector.
        /// </summary>
        /// <param name="input">An input vector.</param>
        /// <returns>
        ///     True, if the input matches the rule
        ///     <see cref="Antecedents" />; otherwise, false.
        /// </returns>
        public bool Match(double[] input)
        {
            foreach (var expr in Antecedents)
            {
                if (!expr.Match(input))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Creates a new <see cref="DecisionRule" /> from a <see cref="DecisionTree" />'s
        ///     <see cref="DecisionNode" />. This node must be a leaf, cannot be the root, and
        ///     should have one output value.
        /// </summary>
        /// <param name="node">A <see cref="DecisionNode" /> from a <see cref="DecisionTree" />.</param>
        /// <returns>A <see cref="DecisionRule" /> representing the given <paramref name="node" />.</returns>
        public static DecisionRule FromNode(DecisionNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (!node.IsLeaf || node.IsRoot || !node.Value.HasValue)
            {
                throw new InvalidOperationException(
                    "Only leaf nodes that have a parent can be converted to rules.");
            }

            var current = node;
            double output = current.Output.Value;

            var antecedents = new List<Antecedent>();

            while (current.Parent != null)
            {
                var index = current.Parent.Branches.AttributeIndex;
                var comparison = current.Comparison;
                var value = current.Value.Value;

                antecedents.Insert(0, new Antecedent(index, comparison, value));

                current = current.Parent;
            }

            return new DecisionRule(node.Owner.Attributes, output, antecedents);
        }

        /// <summary>
        ///     Gets whether this rule and another rule have
        ///     the same antecedents but different outputs.
        /// </summary>
        /// <param name="rule"></param>
        /// <returns>
        ///     True if the two rules are contradictory;
        ///     false otherwise.
        /// </returns>
        public bool IsInconsistentWith(DecisionRule rule)
        {
            return Antecedents.SetEquals(rule.Antecedents) && Output != rule.Output;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return toString(null, null, CultureInfo.CurrentUICulture);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public string ToString(Codification<string> codebook)
        {
            return toString(codebook, null, CultureInfo.CurrentUICulture);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public string ToString(CultureInfo cultureInfo)
        {
            return toString(null, null, cultureInfo);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public string ToString(Codification<string> codebook, CultureInfo cultureInfo)
        {
            return toString(codebook, null, cultureInfo);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public string ToString(Codification<string> codebook, string outputColumn, CultureInfo cultureInfo)
        {
            return toString(codebook, outputColumn, cultureInfo);
        }

        /// <summary>
        ///     Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        ///     A hash code for this instance, suitable for use in hashing
        ///     algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return Output.GetHashCode();
        }

        /// <summary>
        ///     Determines whether the specified <see cref="System
        ///   .Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="System.Object" />
        ///     is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as DecisionRule);
        }

        private string toString(Codification<string> codebook, string outputColumn, CultureInfo culture)
        {
            var sb = new StringBuilder();

            var expr = expressions.ToArray();

            for (var i = 0; i < expr.Length - 1; i++)
            {
                sb.AppendFormat("({0}) && ", toString(expr[i], codebook, culture));
            }

            sb.AppendFormat("({0})", toString(expr[expr.Length - 1], codebook, culture));

            if (string.IsNullOrEmpty(outputColumn))
            {
                return string.Format(culture, "{0} =: {1}", Output, sb);
            }

            var name = codebook.Revert(outputColumn, (int)Output);
            return string.Format(culture, "{0} =: {1}", name, sb);
        }

        private string toString(Antecedent antecedent, Codification<string> codebook, CultureInfo culture)
        {
            var index = antecedent.Index;
            var name = Variables[index].Name;

            if (string.IsNullOrEmpty(name))
            {
                name = "x[" + index + "]";
            }

            var op = ComparisonExtensions.ToString(antecedent.Comparison);

            string value;
            if (codebook != null && codebook.Columns.Contains(name))
            {
                value = codebook.Revert(name, (int)antecedent.Value);
            }
            else
            {
                value = antecedent.Value.ToString(culture);
            }

            return string.Format(culture, "{0} {1} {2}", name, op, value);
        }

        /// <summary>
        ///     Implements the operator &lt;.
        /// </summary>
        public static bool operator <(DecisionRule a, DecisionRule b)
        {
            if (a.Output == b.Output)
            {
                return a.Antecedents.Count < b.Antecedents.Count;
            }

            return a.Output < b.Output;
        }

        /// <summary>
        ///     Implements the operator &gt;.
        /// </summary>
        public static bool operator >(DecisionRule a, DecisionRule b)
        {
            if (a.Output == b.Output)
            {
                return a.Antecedents.Count > b.Antecedents.Count;
            }

            return a.Output > b.Output;
        }

        /// <summary>
        ///     Implements the operator ==.
        /// </summary>
        public static bool operator ==(DecisionRule a, DecisionRule b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if ((object)a == null || (object)b == null)
            {
                return false;
            }

            return a.Output == b.output
                   && a.Antecedents.SetEquals(b.Antecedents);
        }

        /// <summary>
        ///     Implements the operator !=.
        /// </summary>
        public static bool operator !=(DecisionRule a, DecisionRule b)
        {
            return !(a == b);
        }
    }
}