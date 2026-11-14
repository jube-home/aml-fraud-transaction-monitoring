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

using FluentAssertions;
using Jube.App.Wire;
using Jube.Dto.EntityAnalysisModel;
using Jube.Dto.EntityAnalysisModelActivationRule;
using Jube.Dto.EntityAnalysisModelSanction;
using MessagePack;
using Xunit;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireCharMemberTests
    {
        [Fact]
        public void ACharMemberOfADtoTravelsAsAOneCharacterStringBothWays()
        {
            var dto = new EntityAnalysisModelDto { Name = "x", CacheTtlInterval = 'h', MaxResponseElevationInterval = 'd' };

            var bytes = MessagePackSerializer.Serialize(dto, WireSerializer.Options);
            var back = MessagePackSerializer.Deserialize<EntityAnalysisModelDto>(bytes, WireSerializer.Options);

            back.CacheTtlInterval.Should().Be('h');
            back.MaxResponseElevationInterval.Should().Be('d');
            MessagePackSerializer.ConvertToJson(bytes).Should().Contain("\"h\"").And.Contain("\"d\"");
        }

        [Fact]
        public void ACharMemberSentAsAStringByTheSpaIsAccepted()
        {
            var sent = MessagePackSerializer.Serialize(
                new EntityAnalysisModelDto { Name = "x", CacheTtlInterval = 'h' }, WireSerializer.Options);

            var act = () => MessagePackSerializer.Deserialize<EntityAnalysisModelDto>(sent, WireSerializer.Options);

            act.Should().NotThrow();
        }

        [Fact]
        public void EveryDtoCharMemberUsesTheStringFormatter()
        {
            var rule = new EntityAnalysisModelActivationRuleDto { BypassSuspendInterval = 'm' };
            var sanction = new EntityAnalysisModelSanctionDto { CacheInterval = 's' };

            MessagePackSerializer.Deserialize<EntityAnalysisModelActivationRuleDto>(
                MessagePackSerializer.Serialize(rule, WireSerializer.Options), WireSerializer.Options)
                .BypassSuspendInterval.Should().Be('m');
            MessagePackSerializer.Deserialize<EntityAnalysisModelSanctionDto>(
                MessagePackSerializer.Serialize(sanction, WireSerializer.Options), WireSerializer.Options)
                .CacheInterval.Should().Be('s');
            MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(rule, WireSerializer.Options))
                .Should().Contain("\"m\"");
        }
    }
}
