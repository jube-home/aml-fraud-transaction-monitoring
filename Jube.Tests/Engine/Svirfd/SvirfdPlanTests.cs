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

using System.Collections.Generic;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Engine.Svirfd;
using Xunit;

namespace Jube.Test.Engine.Svirfd
{
    [Trait("Category", "Unit")]
    public sealed class SvirfdPlanTests
    {
        private static List<EntityAnalysisModelSvirfd> Rows()
        {
            return
            [
                new EntityAnalysisModelSvirfd { Id = 1, Name = "IP", RequestXPathName = "IP", Active = 1, Deleted = 0 }
            ];
        }

        [Fact]
        public void ThePlanAllowsRemoteLookupsByDefault()
        {
            SvirfdPlan.Build(Rows()).RemoteLookupsEnabled.Should().BeTrue();
            SvirfdPlan.Empty.RemoteLookupsEnabled.Should().BeTrue();
        }

        [Fact]
        public void ThePlanCarriesTheTenantsSwitchWithoutChangingTheEntries()
        {
            var off = SvirfdPlan.Build(Rows(), false);
            var on = SvirfdPlan.Build(Rows(), true);

            off.RemoteLookupsEnabled.Should().BeFalse();
            on.RemoteLookupsEnabled.Should().BeTrue();
            off.Names.Should().Equal(on.Names);
            off.Any.Should().BeTrue();
        }
    }
}
