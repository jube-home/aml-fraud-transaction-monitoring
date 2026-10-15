// AForge Genetic Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2006-2011
// contacts@aforgenet.com
//

using System;
using Accord.Math.Random;

namespace Accord.Genetic
{
    /// <summary>
    ///     Binary chromosome, which supports length from 2 till 64.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The binary chromosome is the simplest type of chromosomes,
    ///         which is represented by a set of bits. Maximum number of bits comprising
    ///         the chromosome is 64.
    ///     </para>
    /// </remarks>
    public class BinaryChromosome : ChromosomeBase
    {
        /// <summary>
        ///     Chromosome's maximum length.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Maxim chromosome's length in bits, which is supported
        ///         by the class
        ///     </para>
        /// </remarks>
        public const int MaxLength = 64;

        /// <summary>
        ///     Chromosome's length in bits.
        /// </summary>
        protected int length;

        /// <summary>
        ///     Numerical chromosome's value.
        /// </summary>
        protected ulong val;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BinaryChromosome" /> class.
        /// </summary>
        /// <param name="length">Chromosome's length in bits, [2, <see cref="MaxLength" />].</param>
        public BinaryChromosome(int length)
        {
            this.length = System.Math.Max(2, System.Math.Min(MaxLength, length));
            // randomize the chromosome
            Generate();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BinaryChromosome" /> class.
        /// </summary>
        /// <param name="source">Source chromosome to copy.</param>
        /// <remarks>
        ///     <para>
        ///         This is a copy constructor, which creates the exact copy
        ///         of specified chromosome.
        ///     </para>
        /// </remarks>
        protected BinaryChromosome(BinaryChromosome source)
        {
            length = source.length;
            val = source.val;
            fitness = source.fitness;
        }

        /// <summary>
        ///     Chromosome's length.
        /// </summary>
        /// <remarks>
        ///     <para>Length of the chromosome in bits.</para>
        /// </remarks>
        public int Length => length;

        /// <summary>
        ///     Chromosome's value.
        /// </summary>
        /// <remarks>
        ///     <para>Current numerical value of the chromosome.</para>
        /// </remarks>
        public ulong Value => val & (0xFFFFFFFFFFFFFFFF >> (64 - length));

        /// <summary>
        ///     Max possible chromosome's value.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Maximum possible numerical value, which may be represented
        ///         by the chromosome of current length.
        ///     </para>
        /// </remarks>
        public ulong MaxValue => 0xFFFFFFFFFFFFFFFF >> (64 - length);

        /// <summary>
        ///     Get string representation of the chromosome.
        /// </summary>
        /// <returns>Returns string representation of the chromosome.</returns>
        public override string ToString()
        {
            var tval = val;
            var chars = new char[length];

            for (var i = length - 1; i >= 0; i--)
            {
                chars[i] = (char)((tval & 1) + '0');
                tval >>= 1;
            }

            // return the result string
            return new string(chars);
        }

        /// <summary>
        ///     Generate random chromosome value.
        /// </summary>
        /// <remarks>
        ///     <para>Regenerates chromosome's value using random number generator.</para>
        /// </remarks>
        public override void Generate()
        {
            var bytes = new byte[8];

            // generate value
            Generator.Random.NextBytes(bytes);
            val = BitConverter.ToUInt64(bytes, 0);
        }

        /// <summary>
        ///     Create new random chromosome with same parameters (factory method).
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The method creates new chromosome of the same type, but randomly
        ///         initialized. The method is useful as factory method for those classes, which work
        ///         with chromosome's interface, but not with particular chromosome type.
        ///     </para>
        /// </remarks>
        public override IChromosome CreateNew()
        {
            return new BinaryChromosome(length);
        }

        /// <summary>
        ///     Clone the chromosome.
        /// </summary>
        /// <returns>Return's clone of the chromosome.</returns>
        /// <remarks>
        ///     <para>The method clones the chromosome returning the exact copy of it.</para>
        /// </remarks>
        public override IChromosome Clone()
        {
            return new BinaryChromosome(this);
        }

        /// <summary>
        ///     Mutation operator.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The method performs chromosome's mutation, changing randomly
        ///         one of its bits.
        ///     </para>
        /// </remarks>
        public override void Mutate()
        {
            val ^= (ulong)1 << Generator.Random.Next(length);
        }

        /// <summary>
        ///     Crossover operator.
        /// </summary>
        /// <param name="pair">Pair chromosome to crossover with.</param>
        /// <remarks>
        ///     <para>
        ///         The method performs crossover between two chromosomes – interchanging
        ///         range of bits between these chromosomes.
        ///     </para>
        /// </remarks>
        public override void Crossover(IChromosome pair)
        {
            var p = (BinaryChromosome)pair;

            // check for correct pair
            if (p != null && p.length == length)
            {
                var crossOverPoint = 63 - Generator.Random.Next(length - 1);
                var mask1 = 0xFFFFFFFFFFFFFFFF >> crossOverPoint;
                var mask2 = ~mask1;

                var v1 = val;
                var v2 = p.val;

                // calculate new values
                val = (v1 & mask1) | (v2 & mask2);
                p.val = (v2 & mask1) | (v1 & mask2);
            }
        }
    }
}