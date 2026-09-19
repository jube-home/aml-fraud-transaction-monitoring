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
using Jube.Dto.Forms;
using Jube.Dto.Interfaces;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Repository.ExhaustiveSearchInstancePromotedTrialInstance
{
    [FormEndpoint("ExhaustiveSearchInstancePromotedTrialInstance")]
    [FormKeys(Id = nameof(Id))]
    [FormGroup("Promotion", Order = 10)]
    public class ExhaustiveSearchInstancePromotedTrialInstanceDto : IActivatable
    {
        [Description("Server-assigned identifier of the promoted trial instance to toggle. Required.")]
        public int Id { get; set; }

        [Description("When true, this trial instance is promoted and considered for production use -- its " +
                     "performance statistics are surfaced in the Promoted Model Performance and Promoted Model " +
                     "Testing tabs and it is eligible to be the next-best model recalled on invocation. When " +
                     "false, the trial instance is rejected/deactivated and the next highest-performing " +
                     "promoted trial instance takes its place.")]
        [FormField(Group = "Promotion", Order = 10, Widget = "switch")]
        public bool Active { get; set; }
    }
}