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
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using Accord.IO;
using Accord.MachineLearning.DecisionTrees.Learning;
using Accord.MachineLearning.DecisionTrees.Rules;
using Accord.Math;
using Accord.Statistics.Filters;

namespace Accord.MachineLearning.DecisionTrees
{
    /// <summary>
    ///     Decision tree (for both discrete and continuous classification problems).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Represents a decision tree which can be compiled to code at run-time. For sample usage
    ///         and example of learning, please see the documentation pages for the
    ///         <see cref="ID3Learning">
    ///             ID3
    ///         </see>
    ///         and <see cref="C45Learning">C4.5 learning algorithms</see>.
    ///     </para>
    ///     <para>
    ///         It is also possible to create <see cref="RandomForest">random forests</see> using
    ///         the <see cref="RandomForestLearning">random forest learning algorithm</see>.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This example shows the simplest way to induce a decision tree with discrete variables.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\DecisionTrees\ID3LearningTest.cs" region="doc_learn_simplest" />
    ///     <para>
    ///         This example shows a common textbook example, and how to induce a decision tree using a
    ///         <see cref="Codification">codebook</see> to convert string (text) variables into discrete symbols.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\DecisionTrees\ID3LearningTest.cs" region="doc_learn_mitchell" />
    ///     <para>For more examples with discrete variables, please see <see cref="ID3Learning" /></para>
    ///     <para>
    ///         This example shows the simplest way to induce a decision tree with continuous variables.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\DecisionTrees\C45LearningTest.cs" region="doc_simplest" />
    ///     <para>For more examples with continuous variables, please see <see cref="C45Learning" /></para>
    ///     <para>
    ///         The next example shows how to estimate the true performance of a decision tree model using cross-validation:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\DecisionTrees\DecisionTreeTest.cs"
    ///         region="doc_cross_validation" />
    /// </example>
    /// <seealso cref="ID3Learning" />
    /// <seealso cref="C45Learning" />
    /// <seealso cref="RandomForestLearning" />
    [Serializable]
    [SerializationBinder(typeof(DecisionTreeBinder))]
    public class DecisionTree : MulticlassClassifierBase, IEnumerable<DecisionNode>
    {
        private DecisionNode root;

        /// <summary>
        ///     Creates a new <see cref="DecisionTree" /> to process
        ///     the given <paramref name="inputs" /> and the given
        ///     number of possible <paramref name="classes" />.
        /// </summary>
        /// <param name="inputs">An array specifying the attributes to be processed by this tree.</param>
        /// <param name="classes">The number of possible output classes for the given attributes.</param>
        public DecisionTree(IList<DecisionVariable> inputs, int classes)
        {
            if (classes <= 0)
            {
                throw new ArgumentOutOfRangeException("classes");
            }

            if (inputs == null)
            {
                throw new ArgumentNullException("inputs");
            }

            for (var i = 0; i < inputs.Count; i++)
            {
                if (inputs[i].Range.Length == 0)
                {
                    throw new ArgumentException("Attribute " + i + " is a constant.");
                }
            }


            Attributes = new DecisionVariableCollection(inputs);
            NumberOfInputs = inputs.Count;
            NumberOfOutputs = classes;
            NumberOfClasses = classes;
        }

        /// <summary>
        ///     Gets or sets the root node for this tree.
        /// </summary>
        public DecisionNode Root
        {
            get => root;
            set => root = value;
        }

        /// <summary>
        ///     Gets the collection of attributes processed by this tree.
        /// </summary>
        public DecisionVariableCollection Attributes { get; }

        /// <summary>
        ///     Returns an enumerator that iterates through the tree.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be
        ///     used to iterate through the collection.
        /// </returns>
        public IEnumerator<DecisionNode> GetEnumerator()
        {
            if (Root == null)
            {
                yield break;
            }

            var stack = new Stack<DecisionNode>(new[] { Root });

            while (stack.Count != 0)
            {
                var current = stack.Pop();

                yield return current;

                if (current.Branches != null)
                {
                    for (var i = current.Branches.Count - 1; i >= 0; i--)
                    {
                        stack.Push(current.Branches[i]);
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Computes the tree decision for a given input.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <returns>A predicted class for the given input.</returns>
        public override int Decide(double[] input)
        {
            return Decide(input, Root);
        }

        /// <summary>
        ///     Computes the tree decision for a given input.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <returns>A predicted class for the given input.</returns>
        public override int Decide(int[] input)
        {
            try
            {
                return decideIterative(input, Root);
            }
            catch
            {
                return decideRecursive(input.ToDouble(), Root, new int[NumberOfClasses]).ArgMax();
            }
        }

        /// <summary>
        ///     Computes the tree decision for a given input.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <returns>A predicted class for the given input.</returns>
        public int Decide(int?[] input)
        {
            return Decide(input, Root);
        }

        /// <summary>
        ///     Computes the tree decision for a given input.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <returns>A predicted class for the given input.</returns>
        public int[] Decide(int?[][] input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            return Decide(input, new int[input.Length]);
        }

        /// <summary>
        ///     Computes the tree decision for a given input.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="result">The location to where to store the class labels.</param>
        /// <returns>A predicted class for the given input.</returns>
        public int[] Decide(int?[][] input, int[] result)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            for (var i = 0; i < input.Length; i++)
            {
                result[i] = Decide(input[i], Root);
            }

            return result;
        }

        /// <summary>
        ///     Computes the tree decision for a given input.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="subtree">The node where the decision starts.</param>
        /// <returns>A predicted class for the given input.</returns>
        public int Decide(double[] input, DecisionNode subtree)
        {
            if (subtree == null)
            {
                throw new ArgumentNullException("subtree");
            }

            if (subtree.Owner != this)
            {
                throw new ArgumentException("The node does not belong to this tree.", "subtree");
            }

            try
            {
                // Check the instance contains missing values
                if (input.HasNaN())
                {
                    // Yes, the instance contains missing values. We will need to generate all possible 
                    // tree paths considering the diffent values that this value could have assumed, and
                    // take the most likely answer as the final decision for this sample.

                    return decideRecursive(input, subtree, new int[NumberOfClasses]).ArgMax();
                }

                // No missing values, proceed as normal:
                return decideIterative(input, subtree);
            }
            catch
            {
                return decideRecursive(input, subtree, new int[NumberOfClasses]).ArgMax();
            }
        }

        /// <summary>
        ///     Computes the tree decision for a given input.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="subtree">The node where the decision starts.</param>
        /// <returns>A predicted class for the given input.</returns>
        public int Decide(int?[] input, DecisionNode subtree)
        {
            return Decide(input.Apply(x => x == null ? double.NaN : (double)x), subtree);
        }

        private static int decideIterative(double[] input, DecisionNode subtree)
        {
            var current = subtree;

            // Start reasoning
            while (current != null)
            {
                // Check if this is a leaf
                if (current.IsLeaf)
                {
                    // This is a leaf node. The decision
                    // process thus should stop here.

                    return current.Output.HasValue ? current.Output.Value : -1;
                }

                // This node is not a leaf. Continue the
                // decision process following the children

                // Get the next attribute to guide reasoning
                var attribute = current.Branches.AttributeIndex;

                // Check which child is responsible for dealing
                // which the particular value of the attribute
                DecisionNode nextNode = null;

                foreach (var branch in current.Branches)
                {
                    var value = input[attribute];

                    if (branch.Compute(value))
                    {
                        // This is the child node responsible for dealing
                        // which this particular attribute value. Choose it
                        // to continue reasoning.

                        nextNode = branch;
                        break;
                    }
                }

                current = nextNode;
            }

            // Normal execution should not reach here.
            throw new InvalidOperationException("The tree is degenerated. This is often a sign that "
                                                + "the tree is expecting discrete inputs, but it was given only real values.");
        }

        private static int decideIterative(int[] input, DecisionNode subtree)
        {
            var current = subtree;

            // Start reasoning
            while (current != null)
            {
                // Check if this is a leaf
                if (current.IsLeaf)
                {
                    // This is a leaf node. The decision
                    // process thus should stop here.

                    return current.Output.HasValue ? current.Output.Value : -1;
                }

                // This node is not a leaf. Continue the
                // decision process following the children

                // Get the next attribute to guide reasoning
                double value = input[current.Branches.AttributeIndex];

                // Check which child is responsible for dealing
                // which the particular value of the attribute
                DecisionNode nextNode = null;

                foreach (var branch in current.Branches)
                {
                    if (branch.Compute(value))
                    {
                        // This is the child node responsible for dealing
                        // which this particular attribute value. Choose it
                        // to continue reasoning.

                        nextNode = branch;
                        break;
                    }
                }

                current = nextNode;
            }

            // Normal execution should not reach here.
            throw new InvalidOperationException("The tree is degenerated. This is often a sign that "
                                                + "the tree is expecting discrete inputs, but it was given only real values.");
        }

        private static int[] decideRecursive(double[] input, DecisionNode current, int[] answerCounts)
        {
            // Check if this is a leaf
            if (current.IsLeaf)
            {
                // This is a leaf node. The decision
                // process thus should stop here.
                if (current.Output.HasValue)
                {
                    answerCounts[current.Output.Value] += 1;
                }

                return answerCounts;
            }

            // This node is not a leaf. Continue the
            // decision process following the children

            // Get the next attribute to guide reasoning
            var value = input[current.Branches.AttributeIndex];

            if (double.IsNaN(value))
            {
                // This is a missing value. We will consider all possible branches
                foreach (var branch in current.Branches)
                {
                    decideRecursive(input, branch, answerCounts);
                }

                return answerCounts;
            }

            // This is not a missing value. We will find a branch that matches  
            foreach (var branch in current.Branches)
            {
                if (branch.Compute(value))
                {
                    // This is the child node responsible for dealing
                    // which this particular attribute value. Choose it
                    // to continue reasoning.

                    return decideRecursive(input, branch, answerCounts);
                }
            }

            // Normal execution should not reach here.
            return answerCounts;
        }

        /// <summary>
        ///     Traverse the tree using a
        ///     <see cref="DecisionTreeTraversal">
        ///         tree
        ///         traversal method
        ///     </see>
        ///     . Can be iterated with a foreach loop.
        /// </summary>
        /// <param name="method">
        ///     The tree traversal method. Common methods are
        ///     available in the <see cref="TreeTraversal" />static class.
        /// </param>
        /// <returns>
        ///     An <see cref="IEnumerable{T}" /> object which can be used to
        ///     traverse the tree using the chosen traversal method.
        /// </returns>
        public IEnumerable<DecisionNode> Traverse(DecisionTreeTraversalMethod method)
        {
            return new TreeTraversal(Root, method);
        }

        /// <summary>
        ///     Traverse a subtree using a
        ///     <see cref="DecisionTreeTraversal">
        ///         tree
        ///         traversal method
        ///     </see>
        ///     . Can be iterated with a foreach loop.
        /// </summary>
        /// <param name="method">
        ///     The tree traversal method. Common methods are
        ///     available in the <see cref="TreeTraversal" />static class.
        /// </param>
        /// <param name="subtree">The root of the subtree to be traversed.</param>
        /// <returns>
        ///     An <see cref="IEnumerable{T}" /> object which can be used to
        ///     traverse the tree using the chosen traversal method.
        /// </returns>
        public IEnumerable<DecisionNode> Traverse(DecisionTreeTraversalMethod method, DecisionNode subtree)
        {
            if (subtree.Owner != this)
            {
                throw new ArgumentException("The node does not belong to this tree.", "subtree");
            }

            return new TreeTraversal(subtree, method);
        }

        /// <summary>
        ///     Transforms the tree into a set of <see cref="DecisionSet">decision rules</see>.
        /// </summary>
        /// <returns>A <see cref="DecisionSet" /> created from this tree.</returns>
        public DecisionSet ToRules()
        {
            return DecisionSet.FromDecisionTree(this);
        }

#if !NET35 && !NETSTANDARD1_4
        /// <summary>
        ///     Creates an <see cref="Expression">Expression Tree</see> representation
        ///     of this decision tree, which can in turn be compiled into code.
        /// </summary>
        /// <returns>A tree in the form of an expression tree.</returns>
        public Expression<Func<double[], int>> ToExpression()
        {
            return new DecisionTreeExpressionCreator(this).Create();
        }
#endif

        /// <summary>
        ///     Generates a C# class implementing the decision tree.
        /// </summary>
        /// <param name="className">The name for the generated class.</param>
        /// <returns>A string containing the generated class.</returns>
        public string ToCode(string className = "MyTree")
        {
            using (var stream = new MemoryStream())
            {
                TextWriter writer = new StreamWriter(stream);
                var treeWriter = new DecisionTreeWriter(writer);
                treeWriter.Write(this, className);
                writer.Flush();

                stream.Seek(0, SeekOrigin.Begin);
                TextReader reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        ///     Generates a C# class implementing the decision tree.
        /// </summary>
        /// <param name="className">The name for the generated class.</param>
        /// <param name="writer">The <see cref="TextWriter" /> where the class should be written.</param>
        public void ToCode(TextWriter writer, string className = "MyTree")
        {
            var treeWriter = new DecisionTreeWriter(writer);
            treeWriter.Write(this, className);
        }

        /// <summary>
        ///     Computes the height of the tree, defined as the
        ///     greatest distance (in links) between the tree's
        ///     root node and its leaves.
        /// </summary>
        /// <returns>The tree's height.</returns>
        public int GetHeight()
        {
            var maxHeight = 0;
            foreach (var node in this)
            {
                if (!node.IsLeaf)
                {
                    continue;
                }

                var h = node.GetHeight();

                if (h > maxHeight)
                {
                    maxHeight = h;
                }
            }

            return maxHeight;
        }

        private class TreeTraversal : IEnumerable<DecisionNode>
        {
            private readonly DecisionTreeTraversalMethod method;
            private readonly DecisionNode tree;

            public TreeTraversal(DecisionNode tree, DecisionTreeTraversalMethod method)
            {
                this.tree = tree;
                this.method = method;
            }

            public IEnumerator<DecisionNode> GetEnumerator()
            {
                return method(tree);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return method(tree);
            }
        }

        #region Obsolete

#if !NETSTANDARD1_4
        /// <summary>
        ///     Obsolete. Please use <see cref="IO.Serializer.Save{T}(T, string)" /> (or use it as an extension method).
        /// </summary>
        [Obsolete("Please use Accord.IO.Serializer.Save(path) instead (or use it as an extension method).")]
        public void Save(string path)
        {
            Serializer.Save(this, path);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="IO.Serializer.Save{T}(T, Stream, IO.SerializerCompression)" /> (or use it as an
        ///     extension method).
        /// </summary>
        [Obsolete("Please use Accord.IO.Serializer.Save(stream) instead (or use it as an extension method).")]
        public void Save(Stream stream)
        {
            Serializer.Save(this, stream);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="IO.Serializer.Load{T}(Stream, IO.SerializerCompression)" />.
        /// </summary>
        [Obsolete("Please use Accord.IO.Serializer.Load<DecisionTree>(stream) instead.")]
        public static DecisionTree Load(Stream stream)
        {
            return Serializer.Load<DecisionTree>(stream);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="IO.Serializer.Load{T}(string)" />.
        /// </summary>
        [Obsolete("Please use Accord.IO.Serializer.Load<DecisionTree>(path) instead.")]
        public static DecisionTree Load(string path)
        {
            return Serializer.Load<DecisionTree>(path);
        }
#endif

        /// <summary>
        ///     Deprecated. Please use the NumberOfOutputs property instead.
        /// </summary>
        [Obsolete("Please use NumberOfOutputs instead.")]
        public int OutputClasses => NumberOfOutputs;

        /// <summary>
        ///     Deprecated. Please use the NumberOfInputs property.
        /// </summary>
        [Obsolete("Please use NumberOfInputs instead.")]
        public int InputCount => NumberOfInputs;

        /// <summary>
        ///     Deprecated. Please use the Decide() method instead.
        /// </summary>
        [Obsolete("Please use the Decide() method instead.")]
        public int Compute(int[] input)
        {
            return Decide(input);
        }

        /// <summary>
        ///     Deprecated. Please use the Decide() method instead.
        /// </summary>
        [Obsolete("Please use the Decide() method instead.")]
        public int Compute(double[] input)
        {
            return Decide(input);
        }

        /// <summary>
        ///     Deprecated. Please use the Decide() method instead.
        /// </summary>
        [Obsolete("Please use the Decide() method instead.")]
        public int[] Compute(int[][] input)
        {
            return Decide(input);
        }

        /// <summary>
        ///     Deprecated. Please use the Decide() method instead.
        /// </summary>
        [Obsolete("Please use the Decide() method instead.")]
        public int Compute(double[] input, DecisionNode subtree)
        {
            return Decide(input, subtree);
        }

        #endregion

        #region Serialization

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Root != null && Root.Owner == null)
            {
                Root.Owner = this;
            }
        }

        internal class DecisionTreeBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                var name = new AssemblyName(assemblyName);

                if (name.Version < new Version(3, 1, 0))
                {
                    if (typeName == "Accord.MachineLearning.DecisionTrees.DecisionTree")
                    {
                        return typeof(DecisionTree_2_13);
                    }

                    if (typeName == "AForge.DoubleRange")
                    {
                        return typeof(DoubleRange);
                    }
                }

                return null;
            }
        }

#pragma warning disable 0169
#pragma warning disable 0649

        [Serializable]
        private class DecisionTree_2_13
        {
            public DecisionNode Root { get; set; }

            public DecisionVariableCollection Attributes { get; private set; }

            public int OutputClasses { get; private set; }

            public int InputCount { get; private set; }

            public static implicit operator DecisionTree(DecisionTree_2_13 obj)
            {
                var tree = new DecisionTree(obj.Attributes, obj.OutputClasses);
                tree.Root = obj.Root;

                foreach (var node in tree)
                {
                    if (node.Owner == null)
                    {
                        node.Owner = tree;
                    }
                }

                return tree;
            }
        }

#pragma warning restore 0169
#pragma warning restore 0649

        #endregion
    }
}