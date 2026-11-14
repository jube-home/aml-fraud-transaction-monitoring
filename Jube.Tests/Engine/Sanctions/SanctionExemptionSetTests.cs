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
using Jube.Engine.Sanctions;
using Xunit;

namespace Jube.Test.Engine.Sanctions
{
    [Trait("Category", "Unit")]
    [Collection("Database")]
    public sealed class SanctionExemptionSetTests : IDisposable
    {
        private static readonly DateTime now = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

        public SanctionExemptionSetTests()
        {
            SanctionExemptionSet.Shared.Clear();
        }

        public void Dispose()
        {
            SanctionExemptionSet.Shared.Clear();
        }

        private static ApprovedSanctionExemption Exemption(int id = 1, int tenant = 7, string name = "Ivan Petrov",
            int source = 3, string reference = "R1", DateTime? expiry = null)
        {
            return new ApprovedSanctionExemption(id, tenant, SanctionExemptionSet.NameKey(name), source, reference,
                expiry);
        }

        [Theory]
        [InlineData("Ivan Petrov", "ivan petrov")]
        [InlineData("Ivan Petrov", "PETROV, Ivan")]
        [InlineData("Ivan Petrov", "Petrov-Ivan")]
        [InlineData("O'Brien Pat", "pat obrien")]
        public void SpellingVariantsOfTheSameNameShareAKey(string a, string b)
        {
            SanctionExemptionSet.NameKey(a).Should().Be(SanctionExemptionSet.NameKey(b));
        }

        [Fact]
        public void DifferentNamesHaveDifferentKeys()
        {
            SanctionExemptionSet.NameKey("Ivan Petrov").Should().NotBe(SanctionExemptionSet.NameKey("Ivan Petrova"));
        }

        [Fact]
        public void AnApprovedExemptionAppliesToThatNameEntryAndTenantOnly()
        {
            SanctionExemptionSet.Shared.Replace([Exemption()]);
            var set = SanctionExemptionSet.Shared;

            set.IsExempt(7, "PETROV, Ivan", 3, "R1", now).Should().BeTrue();
            set.IsExempt(8, "Ivan Petrov", 3, "R1", now).Should().BeFalse("another tenant");
            set.IsExempt(7, "Ivan Petrov", 4, "R1", now).Should().BeFalse("another list");
            set.IsExempt(7, "Ivan Petrov", 3, "R2", now).Should().BeFalse("another entry");
            set.IsExempt(7, "Maria Gomez", 3, "R1", now).Should().BeFalse("another name");
        }

        [Fact]
        public void AnExemptionStopsApplyingOnceItHasExpired()
        {
            SanctionExemptionSet.Shared.Replace([Exemption(expiry: now.AddMinutes(1))]);

            SanctionExemptionSet.Shared.IsExempt(7, "Ivan Petrov", 3, "R1", now).Should().BeTrue();
            SanctionExemptionSet.Shared.IsExempt(7, "Ivan Petrov", 3, "R1", now.AddMinutes(2)).Should().BeFalse();
        }

        [Fact]
        public void ReplacingTheSetForgetsWhatIsNoLongerApproved()
        {
            SanctionExemptionSet.Shared.Replace([Exemption()]);
            SanctionExemptionSet.Shared.Replace([]);

            SanctionExemptionSet.Shared.IsExempt(7, "Ivan Petrov", 3, "R1", now).Should().BeFalse();
        }

        [Fact]
        public void TheFingerprintIsZeroWithNoExemptionsAndChangesWhenTheTenantsExemptionsChange()
        {
            var set = SanctionExemptionSet.Shared;

            set.Fingerprint(7).Should().Be("0");

            set.Replace([Exemption(1)]);
            var one = set.Fingerprint(7);

            set.Replace([Exemption(1), Exemption(2, reference: "R2")]);
            var two = set.Fingerprint(7);

            one.Should().NotBe("0");
            two.Should().NotBe(one);
            set.Fingerprint(8).Should().Be("0", "another tenant is unaffected");
        }

        [Fact]
        public void TheFingerprintDoesNotDependOnTheOrderTheExemptionsArriveIn()
        {
            var set = SanctionExemptionSet.Shared;

            set.Replace([Exemption(1), Exemption(2, reference: "R2")]);
            var forward = set.Fingerprint(7);
            set.Replace([Exemption(2, reference: "R2"), Exemption(1)]);

            set.Fingerprint(7).Should().Be(forward);
        }
    }
}
