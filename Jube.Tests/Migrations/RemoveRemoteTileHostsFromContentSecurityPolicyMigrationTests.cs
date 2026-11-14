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
using System.Text;
using FluentAssertions;
using Jube.Migrations.Branches.WatcherMap;
using Xunit;

namespace Jube.Test.Migrations
{
    [Trait("Category", "Unit")]
    public sealed class RemoveRemoteTileHostsFromContentSecurityPolicyMigrationTests
    {
        private static List<string> Literals(string sql)
        {
            var found = new List<string>();
            var current = new StringBuilder();
            var inside = false;

            for (var index = 0; index < sql.Length; index++)
            {
                var character = sql[index];

                if (!inside)
                {
                    inside = character == '\'';

                    continue;
                }

                if (character != '\'')
                {
                    current.Append(character);

                    continue;
                }

                if (index + 1 < sql.Length && sql[index + 1] == '\'')
                {
                    current.Append('\'');
                    index++;

                    continue;
                }

                found.Add(current.ToString());
                current.Clear();
                inside = false;
            }

            inside.Should().BeFalse("every string literal in the statement must be closed");

            return found;
        }

        [Fact]
        public void TheStatementIsValidSqlWhoseLiteralsAreTheHeaderAndBothPoliciesIntact()
        {
            Literals(RemoveRemoteTileHostsFromContentSecurityPolicy.BuildSql()).Should().Equal(
                RemoveRemoteTileHostsFromContentSecurityPolicy.ContentSecurityPolicy,
                "Content-Security-Policy",
                RemoveRemoteTileHostsFromContentSecurityPolicy.PreviousContentSecurityPolicy);
        }

        [Fact]
        public void ThePolicyKeepsItsQuotedKeywordsAndNamesNoRemoteHost()
        {
            var policy = RemoveRemoteTileHostsFromContentSecurityPolicy.ContentSecurityPolicy;

            policy.Should().Contain("default-src 'self'");
            policy.Should().Contain("connect-src 'self';");
            policy.Should().Contain("worker-src 'self' blob:");
            policy.Should().Contain("img-src 'self' data: blob:;");
            policy.Should().Contain("frame-ancestors 'none'");
        }

        [Fact]
        public void ThePolicyNamesNoRemoteHostSoTheMapStaysLocalUnlessAnOperatorOptsIn()
        {
            RemoveRemoteTileHostsFromContentSecurityPolicy.ContentSecurityPolicy.Should().NotContain("http://")
                .And.NotContain("https://");
        }

        [Fact]
        public void TheStatementOnlyReplacesTheStockPolicy()
        {
            RemoveRemoteTileHostsFromContentSecurityPolicy.BuildSql().Should()
                .Contain("AND \"Value\" = '" + RemoveRemoteTileHostsFromContentSecurityPolicy.PreviousContentSecurityPolicy.Replace("'", "''") + "'");
        }
    }
}
