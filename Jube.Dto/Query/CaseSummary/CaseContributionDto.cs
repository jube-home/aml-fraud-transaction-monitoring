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

using System.ComponentModel;
using Jube.Dto.Query.CaseSearch;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSummary
{
    [Description("One piece of evidence that moved a case likelihood, with how far.")]
    public class CaseContributionDto
    {
        [Description("Evidence family, such as Rule or KeyHistory.")]
        public string? Family { get; set; }

        [Description("What the evidence is.")]
        public string? Label { get; set; }

        [Description("Where the evidence comes from.")]
        public string? Source { get; set; }

        [Description("Closed cases showing this evidence that were confirmed true positives.")]
        public int Confirmed { get; set; }

        [Description("Closed cases showing this evidence that carry an outcome.")]
        public int Closed { get; set; }

        [Description("Precision of the evidence after shrinkage towards the base rate.")]
        public double Precision { get; set; }

        [Description("How much the evidence moves the log odds relative to the base rate, before the shrink factor.")]
        public double LogLikelihoodRatio { get; set; }

        [Description("Weight applied for age; one means fresh, one half means a half life old.")]
        public double Decay { get; set; }

        [Description("Weight applied because evidence sharing inputs counts less: one for the strongest in a " +
                     "group and the dependence factor for the rest.")]
        public double Weight { get; set; }

        [Description("The contribution to the log odds after weight, decay and the shrink factor. All contributions " +
                     "sum exactly to the distance of the likelihood from the base rate in log odds.")]
        public double LogOdds { get; set; }

        [Description("True when the evidence is discounted because it shares inputs with stronger evidence.")]
        public bool SharesInputs { get; set; }
    }
}
