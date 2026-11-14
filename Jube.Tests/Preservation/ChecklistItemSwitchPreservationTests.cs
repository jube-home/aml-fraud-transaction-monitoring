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

using System.Linq;
using System.Reflection;
using FluentAssertions;
using Jube.Data.Poco;
using MessagePack;
using Xunit;

namespace Jube.Test.Preservation
{
    [Trait("Category", "Unit")]
    public sealed class ChecklistItemSwitchPreservationTests
    {
        [Fact]
        public void TheShowAndValidationSwitchesSurviveAnExportAndImportRoundTrip()
        {
            var item = new CaseWorkflowChecklistItem
            {
                Name = "Round trip", ShowSql = "select true", ValidateSql = "select false", EnableShow = 0,
                EnableValidation = 1
            };

            var bytes = MessagePackSerializer.Serialize(item, PreservationHarness.Lz4);
            var restored = MessagePackSerializer.Deserialize<CaseWorkflowChecklistItem>(bytes, PreservationHarness.Lz4);

            restored.EnableShow.Should().Be(0);
            restored.EnableValidation.Should().Be(1);
            restored.ShowSql.Should().Be("select true");
            restored.ValidateSql.Should().Be("select false");
        }

        [Fact]
        public void TheSwitchesTakeKeysAboveEveryExistingMemberSoOlderExportsStayReadable()
        {
            var keys = typeof(CaseWorkflowChecklistItem).GetProperties()
                .Select(s => (s.Name, Key: s.GetCustomAttribute<KeyAttribute>()?.IntKey))
                .Where(w => w.Key != null).ToDictionary(k => k.Name, k => k.Key!.Value);

            keys["EnableShow"].Should().BeGreaterThan(keys["CaseWorkflowChecklist"]);
            keys["EnableValidation"].Should().BeGreaterThan(keys["EnableShow"]);

            var versionKeys = typeof(CaseWorkflowChecklistItemVersion).GetProperties()
                .Select(s => (s.Name, Key: s.GetCustomAttribute<KeyAttribute>()?.IntKey))
                .Where(w => w.Key != null).ToDictionary(k => k.Name, k => k.Key!.Value);

            versionKeys["EnableShow"].Should().BeGreaterThan(versionKeys["ImportId"]);
            versionKeys["EnableValidation"].Should().BeGreaterThan(versionKeys["EnableShow"]);
        }
    }
}
