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

using Accord.Statistics.Links;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    internal sealed class GlmLinkFunction
    {
        private readonly Func<double, double> derivative;
        private readonly Func<double, double> inverse;
        private readonly Func<double, double> link;

        private GlmLinkFunction(Func<double, double> link, Func<double, double> inverse,
            Func<double, double> derivative)
        {
            this.link = link;
            this.inverse = inverse;
            this.derivative = derivative;
        }

        public double Link(double mean)
        {
            return link(mean);
        }

        public double Inverse(double eta)
        {
            return inverse(eta);
        }

        public double Derivative(double eta)
        {
            return derivative(eta);
        }

        public static GlmLinkFunction For(GlmLink kind)
        {
            ILinkFunction? library = kind switch
            {
                GlmLink.Identity => new IdentityLinkFunction(),
                GlmLink.Log => new LogLinkFunction(),
                GlmLink.Logit => new LogitLinkFunction(),
                GlmLink.Probit => new ProbitLinkFunction(),
                GlmLink.Cauchit => new CauchitLinkFunction(),
                GlmLink.Inverse => new InverseLinkFunction(),
                _ => null
            };

            if (library != null)
            {
                return new GlmLinkFunction(library.Function, library.Inverse, library.Derivative);
            }

            if (kind == GlmLink.ComplementaryLogLog)
            {
                return new GlmLinkFunction(m => Math.Log(-Math.Log(1 - m)), e => 1 - Math.Exp(-Math.Exp(e)),
                    e => Math.Exp(e - Math.Exp(e)));
            }

            throw new ArgumentException(
                "Unknown link. Use Identity, Log, Logit, Probit, ComplementaryLogLog, Cauchit or Inverse.", "link");
        }

        public static GlmLink DefaultFor(GlmFamily family)
        {
            return family switch
            {
                GlmFamily.Gaussian => GlmLink.Identity,
                GlmFamily.Binomial => GlmLink.Logit,
                _ => GlmLink.Log
            };
        }
    }
}