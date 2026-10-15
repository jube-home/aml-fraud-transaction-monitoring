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

using System;
using FluentAssertions;
using Jube.Analytics.Bootstrap;
using Jube.Analytics.Bootstrap.Interfaces;
using Xunit;

namespace Jube.Test.Analytics
{
    public class BivariateCopulaTests
    {
        private static (double[] U, double[] V) SimulateFromCopula(IBivariateCopula copula, int n, int seed)
        {
            var random = new Random(seed);
            var u = new double[n];
            var v = new double[n];

            for (var i = 0; i < n; i++)
            {
                u[i] = random.NextDouble();
                var w = random.NextDouble();
                v[i] = copula.InverseConditionalCdf(u[i], w);
            }

            return (u, v);
        }

        [Theory]
        [InlineData(0.7)]
        [InlineData(-0.5)]
        [InlineData(0.2)]
        public void GaussianCopula_RecoversItsOwnParameterFromSimulatedData(double rho)
        {
            var truth = new GaussianCopula(rho);
            var (u, v) = SimulateFromCopula(truth, 4000, 11);
            var tau = KendallTau.Compute(u, v);

            var fitted = GaussianCopula.Fit(u, v, tau);

            fitted.Copula.Parameter.Should().BeApproximately(rho, 0.05);
        }

        [Theory]
        [InlineData(2.0)]
        [InlineData(5.0)]
        public void ClaytonCopula_RecoversItsOwnParameterFromSimulatedData(double theta)
        {
            var truth = new ClaytonCopula(theta);
            var (u, v) = SimulateFromCopula(truth, 4000, 23);
            var tau = KendallTau.Compute(u, v);

            var fitted = ClaytonCopula.Fit(u, v, tau);

            fitted.Should().NotBeNull();
            fitted!.Copula.Parameter.Should().BeApproximately(theta, theta * 0.25);
        }

        [Theory]
        [InlineData(2.0)]
        [InlineData(4.0)]
        public void GumbelCopula_RecoversItsOwnParameterFromSimulatedData(double theta)
        {
            var truth = new GumbelCopula(theta);
            var (u, v) = SimulateFromCopula(truth, 4000, 37);
            var tau = KendallTau.Compute(u, v);

            var fitted = GumbelCopula.Fit(u, v, tau);

            fitted.Should().NotBeNull();
            fitted!.Copula.Parameter.Should().BeApproximately(theta, theta * 0.25);
        }

        [Theory]
        [InlineData(4.0)]
        [InlineData(-4.0)]
        public void FrankCopula_RecoversItsOwnParameterFromSimulatedData(double theta)
        {
            var truth = new FrankCopula(theta);
            var (u, v) = SimulateFromCopula(truth, 4000, 41);
            var tau = KendallTau.Compute(u, v);

            var fitted = FrankCopula.Fit(u, v, tau);

            fitted.Copula.Parameter.Should().BeApproximately(theta, Math.Abs(theta) * 0.3);
        }

        [Fact]
        public void BivariateCopulaFitter_PicksGaussianForGaussianData()
        {
            var truth = new GaussianCopula(0.6);
            var (u, v) = SimulateFromCopula(truth, 3000, 53);

            var fitted = BivariateCopulaFitter.FitBest(u, v);

            fitted.Family.Should().Be("Gaussian");
        }

        [Fact]
        public void BivariateCopulaFitter_PicksClaytonForClaytonData()
        {
            var truth = new ClaytonCopula(4.0);
            var (u, v) = SimulateFromCopula(truth, 3000, 59);

            var fitted = BivariateCopulaFitter.FitBest(u, v);

            fitted.Family.Should().Be("Clayton");
        }

        [Fact]
        public void InverseConditionalCdf_IsAConsistentInverseOfConditionalCdf()
        {
            IBivariateCopula[] copulas =
            [
                new GaussianCopula(0.4), new ClaytonCopula(3.0), new GumbelCopula(2.5), new FrankCopula(5.0)
            ];

            var random = new Random(101);

            foreach (var copula in copulas)
            {
                for (var i = 0; i < 50; i++)
                {
                    var u = 0.01 + random.NextDouble() * 0.98;
                    var v = 0.01 + random.NextDouble() * 0.98;
                    var w = copula.ConditionalCdf(u, v);
                    var recovered = copula.InverseConditionalCdf(u, w);

                    recovered.Should().BeApproximately(v, 1e-4,
                        $"{copula.Family} h/h^-1 should round-trip for u={u}, v={v}");
                }
            }
        }
    }
}