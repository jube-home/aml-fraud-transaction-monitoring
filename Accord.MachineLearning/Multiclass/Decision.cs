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

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Pair of class labels.
    /// </summary>
    /// <remarks>
    ///     The <see cref="Decision" /> structure is the equivalent of a <see cref="Tuple{T1,T2}" />
    ///     where the tuple elements are called <see cref="Class1" /> and <see cref="Class2" /> instead of
    ///     <see cref="Tuple{T1, T2}.Item1" /> and <see cref="Tuple{T1, T2}.Item2" />. It is
    ///     mainly used to index or provide access to individual binary models within a <see cref="OneVsOne{TBinary}" />
    ///     (i.e. through <see cref="OneVsOne{TBinary, TInput}.Indices" /> and
    ///     <see cref="OneVsOne{TBinary, TInput}.GetEnumerator()" />) and in the definition of the
    ///     <see cref="Decision" /> structure.
    /// </remarks>
    /// <seealso cref="Decision" />
    /// <seealso cref="OneVsOneLearning{TBinary, TModel}" />
    /// <seealso cref="OneVsRestLearning{TBinary, TModel}" />
    [Serializable]
    public struct ClassPair : IEquatable<ClassPair>
    {
        /// <summary>
        ///     Gets the first class in the pair.
        /// </summary>
        public int Class1 { get; }

        /// <summary>
        ///     Gets the second class in the pair.
        /// </summary>
        public int Class2 { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ClassPair" /> struct.
        /// </summary>
        /// <param name="i">The first class index in the pair.</param>
        /// <param name="j">The second class index in the pair.</param>
        public ClassPair(int i, int j)
        {
            Class1 = i;
            Class2 = j;
        }

        /// <summary>
        ///     Converts to a tuple (class_a, class_b).
        /// </summary>
        public Tuple<int, int> ToTuple()
        {
            return Tuple.Create(Class1, Class2);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "{0},{1}".Format(Class1, Class2);
        }

        /// <summary>
        ///     Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(ClassPair other)
        {
            return Class1 == other.Class1 && Class2 == other.Class2;
        }

        /// <summary>
        ///     Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals((ClassPair)obj);
        }

        /// <summary>
        ///     Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return Class1 * 47 + Class2;
        }
    }

    /// <summary>
    ///     Decision between two class labels. Indicates the class index of the first
    ///     class, the class index of the adversary, and the class index of the winner.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The <see cref="Decision" /> structure is used to represent the outcome of a binary classifier for the
    ///         problem of deciding between two classes. For example, let's say we would like to represent that, given
    ///         the problem of deciding between class #4 and class #2, a binary classsifier has opted for deciding that
    ///         class #2 was more likely than class #4. This could be represented by a <see cref="Decision" /> structure
    ///         by instantiating it using <c>Decision(i: 4, j: 2, winner: 2)</c>.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The <see cref="Decision" /> structure is more likely to be used or found when dealing with strategies
    ///         for creating multi-class and/or multi-label classifiers using a set of binary classifiers, such as when using
    ///         <see cref="OneVsOne{TBinary, TInput}" /> and <see cref="OneVsRest{TModel, TInput}" />. In the example below, we
    ///         will extract the sequence of binary classification problems and their respective decisions when evaluating a
    ///         multi-class SVM using the one-vs-one decision strategy for multi-class problems:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn" />
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn_decision_path" />
    /// </example>
    /// <seealso cref="ClassPair" />
    /// <seealso cref="OneVsOne{TBinary, TInput}" />
    /// <seealso cref="OneVsRest{TBinary, TInput}" />
    [Serializable]
    public struct Decision
    {
        /// <summary>
        ///     Gets the adversarial classes.
        /// </summary>
        public ClassPair Pair { get; }

        /// <summary>
        ///     Gets the class label of the winner.
        /// </summary>
        public int Winner { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Decision" /> struct.
        /// </summary>
        /// <param name="i">The first class index.</param>
        /// <param name="j">The second class index.</param>
        /// <param name="winner">The class index that won.</param>
        public Decision(int i, int j, int winner)
        {
            if (winner != i && winner != j)
            {
                throw new ArgumentOutOfRangeException("winner");
            }

            Pair = new ClassPair(i, j);
            this.Winner = winner;
        }

        /// <summary>
        ///     Converts to a triplet (class_a, class_b, winner).
        /// </summary>
        public Tuple<int, int, int> ToTuple()
        {
            return Tuple.Create(Pair.Class1, Pair.Class2, Winner);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "{0},{1}>{2}".Format(Pair.Class1, Pair.Class2, Winner);
        }
    }
}