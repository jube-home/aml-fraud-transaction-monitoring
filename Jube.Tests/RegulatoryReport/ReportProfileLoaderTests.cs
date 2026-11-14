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
using System.IO;
using FluentAssertions;
using Jube.RegulatoryReport;
using Xunit;

namespace Jube.Test.RegulatoryReport
{
    [Trait("Category", "Unit")]
    public sealed class ReportProfileLoaderTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "jube-profile-" + Guid.NewGuid().ToString("N"));

        public ReportProfileLoaderTests()
        {
            Directory.CreateDirectory(directory);
        }

        public void Dispose()
        {
            Directory.Delete(directory, true);
        }

        [Fact]
        public void AProfileIsLoadedFromItsFolder()
        {
            File.WriteAllText(Path.Combine(directory, "t.xml"), ExampleProfile.Template);
            File.WriteAllText(Path.Combine(directory, "s.xsd"), ExampleProfile.Schema);
            File.WriteAllText(Path.Combine(directory, "profile.json"), """
                {"name":"example","description":"d","template":"t.xml","schemas":["s.xsd"],
                 "codeLists":{"countries":{"Kuwait":"KW"}}}
                """);

            var profile = ReportProfileLoader.Load(directory);

            (profile.Name, profile.Description).Should().Be(("example", "d"));
            profile.SchemaDocuments.Should().ContainKey("s.xsd");
            profile.CodeLists["countries"]["Kuwait"].Should().Be("KW");
            profile.TemplateXml.Should().Contain("<report");
        }

        [Fact]
        public void AFileOutsideTheFolderOrAMissingOneIsRefused()
        {
            File.WriteAllText(Path.Combine(directory, "profile.json"),
                "{\"name\":\"x\",\"template\":\"../outside.xml\"}");
            ((Action)(() => ReportProfileLoader.Load(directory))).Should().Throw<InvalidDataException>();

            File.WriteAllText(Path.Combine(directory, "profile.json"), "{\"name\":\"x\",\"template\":\"gone.xml\"}");
            ((Action)(() => ReportProfileLoader.Load(directory))).Should().Throw<FileNotFoundException>();

            File.WriteAllText(Path.Combine(directory, "profile.json"), "{\"template\":\"gone.xml\"}");
            ((Action)(() => ReportProfileLoader.Load(directory))).Should().Throw<InvalidDataException>();
        }

        [Fact]
        public void ADirectoryWithoutAProfileIsRefused()
        {
            ((Action)(() => ReportProfileLoader.Load(directory))).Should().Throw<FileNotFoundException>();
        }
    }
}
