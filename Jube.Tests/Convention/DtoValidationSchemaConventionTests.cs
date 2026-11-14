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
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Npgsql;
using Xunit;

namespace Jube.Test.Convention
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class DtoValidationSchemaConventionTests(DatabaseFixture fx)
    {
        private static readonly HashSet<string> serverAssignedColumns = new(StringComparer.Ordinal)
        {
            "CreatedUser", "UpdatedUser", "DeletedUser"
        };

        private static readonly Dictionary<string, string> foreignKeysGuardedByTheService = new(StringComparer.Ordinal)
        {
            ["CaseNoteDto.CaseId"] = "The service loads the case visible to the caller before it writes.",
            ["CaseResearchDto.CaseId"] = "The service loads the case visible to the caller before it writes.",
            ["CaseWorkflowFormEntryDto.CaseId"] = "The service loads the case visible to the caller before it writes.",
            ["EntityAnalysisModelSynchronisationScheduleDto.EntityAnalysisModelId"] =
                "The service refuses a model outside the tenant before it writes.",
            ["EntityAnalysisModelSynchronisationScheduleDto.TenantRegistryId"] =
                "The tenant is taken from the caller and the client value is ignored.",
            ["ExhaustiveSearchInstanceDto.ExhaustiveInstanceCsvId"] =
                "The validator confirms the dataset through IsConfirmedAsync.",
            ["ExhaustiveSearchInstanceDto.ExhaustiveInstanceJsonId"] =
                "The validator confirms the dataset through IsConfirmedAsync."
        };

        private static readonly Regex validatorTarget = new(@"AbstractValidator<(\w+)>", RegexOptions.Compiled);

        private static readonly Regex constant = new(@"const int (\w+) = (\d+)", RegexOptions.Compiled);

        private static readonly Regex lengthRule = new(@"MaximumLength\((\w+)\)|Length\(\s*\d+\s*,\s*(\w+)\)",
            RegexOptions.Compiled);

        private static readonly Regex duplicateMarker = new("Duplicate|AlreadyExists|NameTaken|Taken|IsUnique|Unique",
            RegexOptions.Compiled);

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Jube.App", "Jube.App.csproj")))
            {
                directory = directory.Parent;
            }

            return directory.Required().FullName;
        }

        private static IEnumerable<string> SourceFiles(string project)
        {
            var separator = Path.DirectorySeparatorChar;

            return Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), project), "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{separator}obj{separator}") && !p.Contains($"{separator}bin{separator}"));
        }

        private static Dictionary<string, string> ValidatorSources()
        {
            var sources = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var path in SourceFiles("Jube.Validations").Where(p => p.EndsWith("Validator.cs", StringComparison.Ordinal)))
            {
                var text = File.ReadAllText(path);

                foreach (Match match in validatorTarget.Matches(text))
                {
                    sources[match.Groups[1].Value] = text;
                }
            }

            return sources;
        }

        private static Dictionary<string, List<(string Property, string Type)>> DtoProperties()
        {
            var dtos = new Dictionary<string, List<(string, string)>>(StringComparer.Ordinal);
            var property = new Regex(@"public (string|int|long|Guid|byte)\??\s+(\w+)\s*\{", RegexOptions.Compiled);
            var declaration = new Regex(@"class (\w+Dto)\b", RegexOptions.Compiled);

            foreach (var path in SourceFiles("Jube.Dto").Where(p => p.EndsWith("Dto.cs", StringComparison.Ordinal)))
            {
                var text = File.ReadAllText(path);

                foreach (Match match in declaration.Matches(text))
                {
                    dtos[match.Groups[1].Value] = property.Matches(text)
                        .Select(m => (m.Groups[2].Value, m.Groups[1].Value)).ToList();
                }
            }

            return dtos;
        }

        private static string RuleBlock(string validator, string property)
        {
            var blocks = Regex.Matches(validator, @"RuleFor\(\w+ => \w+\." + Regex.Escape(property) + @"\)(.*?)(?=RuleFor\(|\z)",
                RegexOptions.Singleline);

            return string.Concat(blocks.Select(b => b.Groups[1].Value));
        }

        private async Task<List<T>> QueryAsync<T>(string sql, Func<NpgsqlDataReader, T> read)
        {
            await using var connection = new NpgsqlConnection(fx.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<T>();

            while (await reader.ReadAsync())
            {
                rows.Add(read(reader));
            }

            return rows;
        }

        [Fact]
        public async Task EveryBoundedStringColumnHasAMatchingMaximumLengthRuleOnItsDtoValidatorAsync()
        {
            var bounded = (await QueryAsync(
                    "SELECT table_name, column_name, character_maximum_length FROM information_schema.columns " +
                    "WHERE table_schema = 'public' AND character_maximum_length IS NOT NULL",
                    r => (Table: r.GetString(0), Column: r.GetString(1), Length: r.GetInt32(2))))
                .ToDictionary(c => $"{c.Table}.{c.Column}", c => c.Length, StringComparer.Ordinal);

            var validators = ValidatorSources();
            var problems = new List<string>();
            var checkedColumns = 0;

            foreach (var (dto, properties) in DtoProperties())
            {
                var table = dto[..^3];

                if (!validators.TryGetValue(dto, out var validator))
                {
                    continue;
                }

                var constants = constant.Matches(validator).ToDictionary(m => m.Groups[1].Value,
                    m => int.Parse(m.Groups[2].Value), StringComparer.Ordinal);

                foreach (var (name, _) in properties.Where(p => p.Type == "string"
                                                                     && !serverAssignedColumns.Contains(p.Property)))
                {
                    if (!bounded.TryGetValue($"{table}.{name}", out var length))
                    {
                        continue;
                    }

                    checkedColumns++;

                    var limits = lengthRule.Matches(RuleBlock(validator, name))
                        .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
                        .Select(v => int.TryParse(v, out var literal) ? literal
                            : constants.GetValueOrDefault(v, int.MaxValue))
                        .ToList();

                    if (limits.Count == 0)
                    {
                        problems.Add($"{dto}.{name}: column allows {length}, validator has no length rule");
                    }
                    else if (limits.Min() > length)
                    {
                        problems.Add($"{dto}.{name}: column allows {length}, validator allows {limits.Min()}");
                    }
                }
            }

            checkedColumns.Should().BeGreaterThan(5, "the check must really compare DTO strings with columns");
            problems.Should().BeEmpty("a string longer than its column must be refused by our validator, not by the database");
        }

        [Fact]
        public async Task EveryUniqueNameIndexHasADuplicatePreCheckOnItsDtoValidatorAsync()
        {
            var uniqueNameTables = (await QueryAsync(
                    "SELECT t.relname, pg_get_indexdef(ix.indexrelid) FROM pg_index ix " +
                    "JOIN pg_class t ON t.oid = ix.indrelid JOIN pg_namespace n ON n.oid = t.relnamespace " +
                    "WHERE n.nspname = 'public' AND ix.indisunique AND NOT ix.indisprimary",
                    r => (Table: r.GetString(0), Definition: r.GetString(1))))
                .Where(i => i.Definition.Contains("\"Name\"", StringComparison.Ordinal))
                .Select(i => i.Table)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var validators = ValidatorSources();
            var dtos = DtoProperties();
            var problems = new List<string>();
            var checkedTables = 0;

            foreach (var table in uniqueNameTables)
            {
                var dto = $"{table}Dto";

                if (!validators.TryGetValue(dto, out var validator) || !dtos.ContainsKey(dto))
                {
                    continue;
                }

                checkedTables++;

                if (!duplicateMarker.IsMatch(validator))
                {
                    problems.Add($"{dto}: unique name index on {table} with no duplicate pre-check in the validator");
                }
            }

            checkedTables.Should().BeGreaterThan(5, "the check must really cover unique name indexes");
            problems.Should().BeEmpty("a duplicate name must be refused by our validator, not by the database");
        }

        [Fact]
        public async Task EveryForeignKeyPropertyOfAValidatedDtoIsCheckedOrNamedAsGuardedByTheServiceAsync()
        {
            var foreignKeys = (await QueryAsync(
                    "SELECT c.conrelid::regclass::text, a.attname FROM pg_constraint c " +
                    "JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = c.conkey[1] " +
                    "WHERE c.contype = 'f' AND array_length(c.conkey, 1) = 1",
                    r => (Table: r.GetString(0).Trim('"'), Column: r.GetString(1))))
                .Select(f => $"{f.Table}.{f.Column}")
                .ToHashSet(StringComparer.Ordinal);

            var validators = ValidatorSources();
            var problems = new List<string>();
            var checkedProperties = 0;

            foreach (var (dto, properties) in DtoProperties())
            {
                var table = dto[..^3];

                if (!validators.TryGetValue(dto, out var validator))
                {
                    continue;
                }

                foreach (var (name, _) in properties.Where(p => p.Type != "string"
                                                                     && foreignKeys.Contains($"{table}.{p.Property}")))
                {
                    checkedProperties++;

                    var block = RuleBlock(validator, name);

                    if (!block.Contains("Must", StringComparison.Ordinal)
                        && !foreignKeysGuardedByTheService.ContainsKey($"{dto}.{name}"))
                    {
                        problems.Add($"{dto}.{name}: foreign key with no existence rule and no named service guard");
                    }
                }
            }

            checkedProperties.Should().BeGreaterThan(20, "the check must really cover foreign key properties");
            problems.Should().BeEmpty("a missing or foreign target must be refused by our validator, not by the database");

            foreignKeysGuardedByTheService.Keys.Should().OnlyContain(k => k.Contains('.', StringComparison.Ordinal));
        }
    }
}
