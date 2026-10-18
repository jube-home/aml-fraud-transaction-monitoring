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
using System.Linq.Expressions;

#if !NET35 && !NETSTANDARD1_4
namespace Accord.MachineLearning.DecisionTrees
{
    /// <summary>
    ///     Decision Tree (Linq) Expression Creator.
    /// </summary>
    internal class DecisionTreeExpressionCreator
    {
        private readonly DecisionTree tree;
        private ParameterExpression inputs;
        private LabelTarget label;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DecisionTreeExpressionCreator" /> class.
        /// </summary>
        /// <param name="tree">The decision tree.</param>
        internal DecisionTreeExpressionCreator(DecisionTree tree)
        {
            this.tree = tree;
        }

        /// <summary>
        ///     Creates an expression for the tree.
        /// </summary>
        /// <returns>
        ///     A strongly typed lambda expression in the form
        ///     of an <see cref="Expression{T}">expression</see> tree
        ///     representing the <see cref="DecisionTree" />.
        /// </returns>
        public Expression<Func<double[], int>> Create()
        {
            inputs = Expression.Parameter(typeof(double[]), "input");
            label = Expression.Label(typeof(int), "return");

            var rootExpression = create(tree.Root);

            var ex = typeof(ArgumentException).GetConstructor(
                new[] { typeof(string), typeof(string) });

            var block = Expression.Block(typeof(int),
                rootExpression,
                Expression.Throw(Expression.New(ex,
                    Expression.Constant("Input contains a value outside of expected ranges."),
                    Expression.Constant("input"))),
                Expression.Label(label, Expression.Constant(0))
            );

            return Expression.Lambda<Func<double[], int>>(block, inputs);
        }

        private Expression create(DecisionNode node)
        {
            if (!node.IsLeaf)
            {
                var attributeIndex = node.Branches.AttributeIndex;

                // Create all comparison expressions
                var comparisons = new BinaryExpression[node.Branches.Count];
                var childExpression = new Expression[node.Branches.Count];

                for (var i = 0; i < comparisons.Length; i++)
                {
                    var child = node.Branches[i];

                    var expr = Expression.ArrayIndex(inputs, Expression.Constant(attributeIndex));
                    var cons = Expression.Constant(child.Value);

                    switch (child.Comparison)
                    {
                        case ComparisonKind.Equal:
                            comparisons[i] = Expression.Equal(expr, cons); break;

                        case ComparisonKind.GreaterThan:
                            comparisons[i] = Expression.GreaterThan(expr, cons); break;

                        case ComparisonKind.GreaterThanOrEqual:
                            comparisons[i] = Expression.GreaterThanOrEqual(expr, cons); break;

                        case ComparisonKind.LessThan:
                            comparisons[i] = Expression.LessThan(expr, cons); break;

                        case ComparisonKind.LessThanOrEqual:
                            comparisons[i] = Expression.LessThanOrEqual(expr, cons); break;

                        case ComparisonKind.NotEqual:
                            comparisons[i] = Expression.NotEqual(expr, cons); break;

                        default:
                            throw new InvalidOperationException("Unexpected node comparison type.");
                    }


                    childExpression[i] = create(node.Branches[i]);
                }

                // Create expression for else expressions
                var ex = typeof(ArgumentException).GetConstructor(new[] { typeof(string), typeof(string) });

                var lastElse = Expression.IfThenElse(comparisons[comparisons.Length - 1],
                    childExpression[comparisons.Length - 1],
                    Expression.Throw(Expression.New(ex,
                        Expression.Constant("Input contains a value outside of expected ranges."),
                        Expression.Constant("input"))));

                ConditionalExpression currentIf = null;
                for (var i = comparisons.Length - 2; i >= 0; i--)
                {
                    currentIf = Expression.IfThenElse(comparisons[i],
                        childExpression[i], lastElse);
                    lastElse = currentIf;
                }

                return currentIf;
            }

            // node is a leaf
            if (node.Output.HasValue)
            {
                return Expression.Return(label, Expression.Constant(node.Output.Value), typeof(int));
            }

            return Expression.Return(label, Expression.Constant(-1), typeof(int));
        }
    }
}
#endif