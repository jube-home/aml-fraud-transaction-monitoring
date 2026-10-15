// AForge Math Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2007-2011
// contacts@aforgenet.com
//

using System;

namespace Accord.Math.Random
{
    /// <summary>
    ///     Standard random numbers generator.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The random number generator generates gaussian
    ///         random numbers with zero mean and standard deviation of one. The generator
    ///         implements polar form of the Box-Muller transformation.
    ///     </para>
    ///     <para>
    ///         The generator uses <see cref="UniformOneGenerator" /> generator as a base
    ///         to generate random numbers.
    ///     </para>
    ///     <para>Sample usage:</para>
    ///     <code>
    /// // create instance of random generator
    /// IRandomNumberGenerator generator = new StandardGenerator( );
    /// // generate random number
    /// float randomNumber = generator.Next( );
    /// </code>
    /// </remarks>
    [Obsolete("Please use Accord.Statistics.Distributions.NormalDistribution.Standard instead.")]
    public class StandardGenerator : IRandomNumberGenerator
    {
        private UniformOneGenerator rand;

        private float secondValue;
        private bool useSecond;

        /// <summary>
        ///     Initializes a new instance of the <see cref="StandardGenerator" /> class.
        /// </summary>
        public StandardGenerator()
        {
            rand = new UniformOneGenerator();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="StandardGenerator" /> class.
        /// </summary>
        /// <param name="seed">Seed value to initialize random numbers generator.</param>
        public StandardGenerator(int seed)
        {
            rand = new UniformOneGenerator(seed);
        }

        /// <summary>
        ///     Mean value of the generator.
        /// </summary>
        public float Mean => 0;

        /// <summary>
        ///     Variance value of the generator.
        /// </summary>
        public float Variance => 1;

        /// <summary>
        ///     Generate next random number.
        /// </summary>
        /// <returns>Returns next random number.</returns>
        public float Next()
        {
            // check if we can use second value
            if (useSecond)
            {
                // return the second number
                useSecond = false;
                return secondValue;
            }

            float x1, x2, w;

            // generate new numbers
            do
            {
                x1 = rand.Next() * 2.0f - 1.0f;
                x2 = rand.Next() * 2.0f - 1.0f;
                w = x1 * x1 + x2 * x2;
            } while (w >= 1.0f);

            w = (float)System.Math.Sqrt(-2.0f * System.Math.Log(w) / w);

            // get two standard random numbers
            var firstValue = x1 * w;
            secondValue = x2 * w;

            useSecond = true;

            // return the first number
            return firstValue;
        }

        /// <summary>
        ///     Set seed of the random numbers generator.
        /// </summary>
        /// <param name="seed">Seed value.</param>
        /// <remarks>
        ///     Resets random numbers generator initializing it with
        ///     specified seed value.
        /// </remarks>
        public void SetSeed(int seed)
        {
            rand = new UniformOneGenerator(seed);
            useSecond = false;
        }
    }
}