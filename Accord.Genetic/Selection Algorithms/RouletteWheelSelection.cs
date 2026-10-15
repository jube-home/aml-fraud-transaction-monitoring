// AForge Genetic Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2006-2011
// contacts@aforgenet.com
//

using System.Collections.Generic;
using Accord.Math.Random;

namespace Accord.Genetic
{
    /// <summary>
    ///     Roulette wheel selection method.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The algorithm selects chromosomes to the new generation according to
    ///         their fitness values - the more fitness value chromosome has, the more chances
    ///         it has to become member of new generation. Each chromosome can be selected
    ///         several times to the new generation.
    ///     </para>
    ///     <para>
    ///         The "roulette's wheel" is divided into sectors, which size is proportional to
    ///         the fitness values of chromosomes - the  size of the wheel is the sum of all fitness
    ///         values, size of each sector equals to fitness value of chromosome.
    ///     </para>
    /// </remarks>
    public class RouletteWheelSelection : ISelectionMethod
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RouletteWheelSelection" /> class.
        /// </summary>
        public RouletteWheelSelection()
        {
        }

        /// <summary>
        ///     Apply selection to the specified population.
        /// </summary>
        /// <param name="chromosomes">Population, which should be filtered.</param>
        /// <param name="size">The amount of chromosomes to keep.</param>
        /// <remarks>
        ///     Filters specified population keeping only those chromosomes, which
        ///     won "roulette" game.
        /// </remarks>
        public void ApplySelection(List<IChromosome> chromosomes, int size)
        {
            var rand = Generator.Random;

            // new population, initially empty
            var newPopulation = new List<IChromosome>();

            // size of current population
            var currentSize = chromosomes.Count;

            // calculate summary fitness of current population
            double fitnessSum = 0;
            foreach (var c in chromosomes)
            {
                fitnessSum += c.Fitness;
            }

            // create wheel ranges
            var rangeMax = new double[currentSize];
            double s = 0;
            var k = 0;

            foreach (var c in chromosomes)
            {
                // cumulative normalized fitness
                s += fitnessSum == 0 ? 1.0 / currentSize : c.Fitness / fitnessSum;
                rangeMax[k++] = s;
            }

            if (currentSize > 0)
            {
                rangeMax[currentSize - 1] = 1.0;
            }

            // select chromosomes from old population to the new population
            for (var j = 0; j < size; j++)
            {
                // get wheel value
                var wheelValue = rand.NextDouble();
                // find the chromosome for the wheel value
                for (var i = 0; i < currentSize; i++)
                {
                    if (wheelValue <= rangeMax[i])
                    {
                        // add the chromosome to the new population
                        newPopulation.Add(chromosomes[i].Clone());
                        break;
                    }
                }
            }

            // empty current population
            chromosomes.Clear();

            // move elements from new to current population
            chromosomes.AddRange(newPopulation);
        }
    }
}