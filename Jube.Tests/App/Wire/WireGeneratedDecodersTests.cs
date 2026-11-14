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
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Jube.App.Wire;
using Jube.Dto.Query.WireSchema;
using MessagePack;
using Xunit;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireGeneratedDecodersTests
    {
        private const string Route = "Jube.Dto.Query.WireSchema.WireRouteDto";
        private const string Field = "Jube.Dto.Query.WireSchema.WireFieldDto";
        private const string Type = "Jube.Dto.Query.WireSchema.WireTypeDto";

        [MessagePackObject]
        public sealed class ExtraOutsideTheDtoAssembly
        {
            [Key(0)]
            public string Name { get; set; }
        }

        private static WireSchemaDto Synthetic()
        {
            return new WireSchemaDto
            {
                TypesHash = "0123456789abcdef",
                Types = new Dictionary<string, WireTypeDto>
                {
                    [Route] = new() { Fields = [] },
                    [Field] = new()
                    {
                        Fields =
                        [
                            new() { Index = 0, Name = "index", Type = "int" },
                            new() { Index = 1, Name = "my-name", Type = "string" },
                            new() { Index = 2, Name = "__proto__", Type = "string" }
                        ]
                    },
                    [Type] = new()
                    {
                        Fields =
                        [
                            new() { Index = 0, Name = "fields", Type = Field + "[]" },
                            new() { Index = 1, Name = "byName", Type = Field + "{}" },
                            new() { Index = 2, Name = "grid", Type = "any{}#" },
                            new() { Index = 3, Name = "matrix", Type = Field + "[][]" },
                            new() { Index = 4, Name = "plain", Type = "int[]" },
                            new() { Index = 5, Name = "first", Type = Field + "?" },
                            new() { Index = 7, Name = "later", Type = "date" }
                        ]
                    },
                    ["Some.Other.Assembly.Type"] = new() { Fields = [] }
                }
            };
        }

        private static string RepositoryGeneratedDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "Jube.App", "Vue", "src", "generated");

                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Jube.App/Vue/src/generated was not found above the test binaries.");
        }

        [Fact]
        public void EachDtoBecomesOneFunctionThatReadsItsMembersByPositionInSchemaOrder()
        {
            var text = WireTypeScriptGenerator.Files(Synthetic())[WireTypeScriptGenerator.DecodersFile];

            text.Should().Contain("export const typesHash = '0123456789abcdef'");
            text.Should().Contain("function d_Jube_Dto_Query_WireSchema_WireRouteDto(value: unknown): unknown {\n" +
                                  "  if (value == null) return value\n  return {}\n}");
            text.Should().Contain("    index: a[0] ?? null,\n    'my-name': a[1] ?? null,\n" +
                                  "    ['__proto__']: a[2] ?? null,\n");
        }

        [Fact]
        public void ListsMapsRowGridsAndNestedListsAreComposedAndPlainMembersAreReadDirectly()
        {
            var text = WireTypeScriptGenerator.Files(Synthetic())[WireTypeScriptGenerator.DecodersFile];
            var field = "d_Jube_Dto_Query_WireSchema_WireFieldDto";

            text.Should().Contain($"fields: ((v) => many(v, {field}))(a[0] ?? null),");
            text.Should().Contain($"byName: ((v) => dict(v, {field}))(a[1] ?? null),");
            text.Should().Contain("grid: ((v) => rows(v, null))(a[2] ?? null),");
            text.Should().Contain($"matrix: ((v) => many(v, (v) => many(v, {field})))(a[3] ?? null),");
            text.Should().Contain("plain: a[4] ?? null,");
            text.Should().Contain($"first: {field}(a[5] ?? null),");
            text.Should().Contain("later: a[7] ?? null,");
        }

        [Fact]
        public void OnlyTypesFromTheDtoAssemblyAreGeneratedAndTheMapNamesThemByTheirFullNames()
        {
            var text = WireTypeScriptGenerator.Files(Synthetic())[WireTypeScriptGenerator.DecodersFile];

            text.Should().NotContain("Some.Other.Assembly");
            text.Should().Contain($"  '{Route}': d_Jube_Dto_Query_WireSchema_WireRouteDto,");
            text.Should().EndWith("}\n");
        }

        [Fact]
        public void TheSameSchemaAlwaysGeneratesTheSameBytes()
        {
            var first = WireTypeScriptGenerator.Files(Synthetic());
            var second = WireTypeScriptGenerator.Files(Synthetic());

            first[WireTypeScriptGenerator.DecodersFile].Should().Be(second[WireTypeScriptGenerator.DecodersFile]);
            first[WireTypeScriptGenerator.TypesFile].Should().Be(second[WireTypeScriptGenerator.TypesFile]);
        }

        [Fact]
        public void ATypesFileCarriesTheHashAndTheTypesInTheShapeTheBrowserSchemaHas()
        {
            var json = WireTypeScriptGenerator.Files(Synthetic())[WireTypeScriptGenerator.TypesFile];

            json.Should().Contain("\"typesHash\":\"0123456789abcdef\"");
            json.Should().Contain("\"fields\":[{\"index\":0,\"name\":\"index\",\"type\":\"int\"}");
        }

        [Fact]
        public void AShapeThatNamesATypeOutsideTheDtoAssemblyStopsTheBuildInsteadOfSkippingAConversion()
        {
            var schema = Synthetic();
            schema.Types[Field].Fields.Add(new WireFieldDto { Index = 3, Name = "stray", Type = "Some.Other.Type" });

            var act = () => WireTypeScriptGenerator.Files(schema);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void WritingThenCheckingLeavesNothingStaleAndAnEditIsDetected()
        {
            var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                var schema = Synthetic();

                WireTypeScriptGenerator.Stale(directory, schema).Should().HaveCount(2);

                WireTypeScriptGenerator.Write(directory, schema);

                WireTypeScriptGenerator.Stale(directory, schema).Should().BeEmpty();

                File.AppendAllText(Path.Combine(directory, WireTypeScriptGenerator.DecodersFile), "// edited\n");

                WireTypeScriptGenerator.Stale(directory, schema).Should()
                    .Equal(WireTypeScriptGenerator.DecodersFile);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Fact]
        public void TheGeneratedDecodersCommittedWithTheUserInterfaceAreExactlyWhatTheServerWouldGenerateNow()
        {
            var registry = new WireTypeRegistry();
            registry.Attach([]);

            var stale = WireTypeScriptGenerator.Stale(RepositoryGeneratedDirectory(), registry.GetSchema(),
                registry.GeneratedTypeNames);

            stale.Should().BeEmpty("build Jube.App, or run it with --wire-generate, after changing a DTO");
        }

        [Fact]
        public void TheSchemaAllowsGeneratedDecodersByDefaultAndNamesTheSwitchWhenItIsOff()
        {
            var on = new WireTypeRegistry();
            on.Attach([]);

            var off = new WireTypeRegistry(new WireOptions(WireMode.MessagePack, generatedDecoders: false));
            off.Attach([]);

            on.GetSchema().GeneratedDecoders.Should().BeTrue();
            off.GetSchema().GeneratedDecoders.Should().BeFalse();
            off.GetSchema().Hash.Should().NotBe(on.GetSchema().Hash);
            off.GetSchema().TypesHash.Should().Be(on.GetSchema().TypesHash);
        }

        [Fact]
        public void TheTypesHashIgnoresTypesFromOutsideTheDtoAssemblyButTheFullHashDoesNot()
        {
            var baseline = new WireTypeRegistry();
            baseline.Attach([]);
            var before = baseline.GetSchema();

            var extended = new WireTypeRegistry();
            extended.Attach([]);
            extended.ShapeOf(typeof(ExtraOutsideTheDtoAssembly));
            var after = extended.GetSchema();

            after.TypesHash.Should().Be(before.TypesHash).And.HaveLength(16);
            after.Hash.Should().NotBe(before.Hash);
        }
    }
}
