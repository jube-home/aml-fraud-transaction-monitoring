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

using System.Collections.Concurrent;
using System.ComponentModel;
using FluentValidation;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Jube.Dto.Forms;
using Jube.Data.Repository.Models;
using Jube.Dto.Query.FormModel;
using Microsoft.Extensions.Localization;
using FormEditorAttribute = Jube.Dto.Forms.EditorAttribute;

namespace Jube.Service.Query.FormModel
{
    public sealed class FormModelRegistry
    {
        private readonly Dictionary<string, FormModelDto> models;
        private readonly ConcurrentDictionary<(string Area, string Culture), FormModelDto> localised = new();

        private FormModelRegistry(Dictionary<string, FormModelDto> models, List<string> failures)
        {
            this.models = models;
            Failures = failures;
        }

        public IReadOnlyCollection<string> Areas => models.Keys;

        public IReadOnlyList<string> Failures { get; }

        public static FormModelRegistry Build(Assembly assembly, Assembly validators)
        {
            var built = new Dictionary<string, FormModelDto>(StringComparer.OrdinalIgnoreCase);
            var failures = new List<string>();
            var rules = RulesByDto(validators, failures);
            var types = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            foreach (var type in assembly.GetTypes())
            {
                var endpoint = type.GetCustomAttribute<FormEndpointAttribute>();

                if (endpoint == null)
                {
                    continue;
                }

                built[endpoint.Area] = Describe(type, endpoint, rules.GetValueOrDefault(type), failures);
                types[endpoint.Area] = type;
            }

            Relate(built, types, failures);

            return new FormModelRegistry(built, failures);
        }

        public bool TryGetLocalised(string area, string culture, IStringLocalizer strings, out FormModelDto? model)
        {
            if (!models.TryGetValue(area, out var english))
            {
                model = null;

                return false;
            }

            model = localised.GetOrAdd((english.Area, culture), _ => FormModelLocaliser.Localise(english, strings));

            return true;
        }

        public bool TryGet(string area, out FormModelDto? model)
        {
            return models.TryGetValue(area, out model);
        }

        private static void Relate(Dictionary<string, FormModelDto> models, Dictionary<string, Type> types,
            List<string> failures)
        {
            foreach (var (area, type) in types)
            {
                var model = models[area];

                var approval = type.GetCustomAttribute<FormApprovalAttribute>();

                if (approval != null)
                {
                    var kindName = approval.Kind ?? area;

                    if (Enum.TryParse<EntityApprovalKind>(kindName, out var kind) && Enum.IsDefined(kind))
                    {
                        model.Approval = new FormApprovalDto
                        {
                            Kind = (int)kind,
                            HistoryRoute = $"/api/{kindName}VersionHistory"
                        };
                    }
                    else
                    {
                        failures.Add($"{area}: no EntityApprovalKind is named {kindName}");
                    }
                }

                if (type.GetCustomAttribute<FormRolesAttribute>() != null)
                {
                    model.Roles = new FormRolesDto
                    {
                        Endpoint = $"/api/{area}Role",
                        ParentKey = $"{CamelCase(area)}Guid"
                    };
                }

                model.Children = Children(area, type, models, failures);

                var values = type.GetCustomAttribute<FormValuesAttribute>();

                if (values != null)
                {
                    model.Values = Values(area, model, values, models, failures);
                }
            }
        }

        private static List<FormChildDto> Children(string area, Type type, Dictionary<string, FormModelDto> models,
            List<string> failures)
        {
            var children = new List<FormChildDto>();

            foreach (var child in type.GetCustomAttributes<FormChildAttribute>())
            {
                var childArea = child.Child.GetCustomAttribute<FormEndpointAttribute>()?.Area;

                if (childArea == null || !models.TryGetValue(childArea, out var childModel))
                {
                    failures.Add($"{area}: {child.Child.Name} is not a form endpoint");

                    continue;
                }

                var parentKey = childModel.Keys.Parent;

                if (string.IsNullOrEmpty(parentKey))
                {
                    failures.Add($"{area}: {childArea} names no parent key, so it cannot sit under {area}");

                    continue;
                }

                var expected = parentKey.EndsWith("Guid", StringComparison.Ordinal) ? parentKey[..^4] : parentKey[..^2];

                if (!string.Equals(expected, CamelCase(area), StringComparison.Ordinal) &&
                    !string.Equals(expected, CamelCase(area), StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add($"{area}: {childArea} is keyed to {parentKey}, which is not {area}");

                    continue;
                }

                children.Add(new FormChildDto
                {
                    Area = childArea,
                    Label = child.Label.Length > 0 ? child.Label : Humanise(childArea),
                    Hub = child.Hub,
                    Order = child.Order,
                    ParentKey = parentKey,
                    ParentField = parentKey.EndsWith("Guid", StringComparison.Ordinal) ? "guid" : "id",
                    ListRoute = $"{childModel.Endpoint}/By{char.ToUpperInvariant(parentKey[0])}{parentKey[1..]}"
                });
            }

            return [.. children.OrderBy(c => c.Order).ThenBy(c => c.Area, StringComparer.Ordinal)];
        }

        private static FormValuesDto? Values(string area, FormModelDto parent, FormValuesAttribute values,
            Dictionary<string, FormModelDto> models, List<string> failures)
        {
            var valueArea = values.ValueDto.GetCustomAttribute<FormEndpointAttribute>()?.Area;

            if (valueArea == null || !models.TryGetValue(valueArea, out var valueModel))
            {
                failures.Add($"{area}: {values.ValueDto.Name} is not a form endpoint");

                return null;
            }

            if (valueModel.Keys.Parent == null)
            {
                failures.Add($"{area}: {valueArea} has no parent key");

                return null;
            }

            var fields = new List<FormValueFieldDto>();

            foreach (var name in values.Fields)
            {
                var field = valueModel.Fields.SingleOrDefault(candidate => candidate.Name == CamelCase(name));

                if (field == null)
                {
                    failures.Add($"{area}: {valueArea} has no field {name}");

                    continue;
                }

                fields.Add(new FormValueFieldDto
                {
                    Field = field.Name,
                    Title = field.Label,
                    Type = field.Type is "integer" or "number" ? "number" : "string"
                });
            }

            if (parent.Approval == null)
            {
                failures.Add($"{area}: values need the owning record to be approved, so it needs FormApproval");

                return null;
            }

            return new FormValuesDto
            {
                Area = valueArea,
                Endpoint = valueModel.Endpoint,
                ParentField = valueModel.Keys.Parent,
                ParentKind = parent.Approval.Kind,
                Fields = fields
            };
        }

        private static Dictionary<Type, Dictionary<string, MemberRules>> RulesByDto(Assembly validators,
            List<string> failures)
        {
            var byDto = new Dictionary<Type, Dictionary<string, MemberRules>>();

            foreach (var type in validators.GetTypes()
                         .Where(type => type is { IsAbstract: false, IsClass: true, IsGenericTypeDefinition: false }))
            {
                var dto = ValidatedType(type);

                if (dto == null)
                {
                    continue;
                }

                try
                {
                    var validator = ValidatorBuilder.Build(type);
                    var members = byDto.TryGetValue(dto, out var existing)
                        ? existing
                        : byDto[dto] = new Dictionary<string, MemberRules>(StringComparer.Ordinal);

                    foreach (var (member, rules) in ValidatorRules.Analyse(validator))
                    {
                        members[member] = members.TryGetValue(member, out var previous)
                            ? ValidatorRules.Merge(previous, rules)
                            : rules;
                    }
                }
                catch (Exception exception)
                {
                    var cause = exception.InnerException ?? exception;

                    failures.Add($"{type.Name}: {cause.GetType().Name} {cause.Message} {string.Join(" <- ", (cause.StackTrace ?? string.Empty).Split('\n').Take(4).Select(line => line.Trim()))}");
                }
            }

            return byDto;
        }

        private static Type? ValidatedType(Type validator)
        {
            for (var current = validator.BaseType; current != null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
                {
                    return current.GetGenericArguments()[0];
                }
            }

            return null;
        }

        private static FormModelDto Describe(Type type, FormEndpointAttribute endpoint,
            Dictionary<string, MemberRules>? rules, List<string> failures)
        {
            var keys = type.GetCustomAttribute<FormKeysAttribute>();
            var nullability = new NullabilityInfoContext();
            var fields = new List<FormFieldDto>();
            var columns = new List<FormListColumnDto>();

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var field = property.GetCustomAttribute<FormFieldAttribute>();
                var column = property.GetCustomAttribute<ListColumnAttribute>();

                if (field != null)
                {
                    fields.Add(DescribeField(property, field, nullability, rules?.GetValueOrDefault(property.Name), failures));
                }

                if (column != null)
                {
                    columns.Add(new FormListColumnDto
                    {
                        Field = CamelCase(property.Name),
                        Title = column.Title ?? FieldLabel(property.Name),
                        Order = column.Order,
                        Sortable = column.Sortable,
                        Filterable = column.Filterable,
                        Hidden = column.Hidden
                    });
                }
            }

            var editors = Editors(type, fields, failures);
            var groups = type.GetCustomAttributes<FormGroupAttribute>()
                .ToDictionary(g => g.Path, g => g.Order, StringComparer.Ordinal);

            int GroupOrder(string path)
            {
                return groups.GetValueOrDefault(path, int.MaxValue);
            }

            return new FormModelDto
            {
                Area = endpoint.Area,
                Endpoint = $"/api/{endpoint.Area}",
                Operations = OperationNames(endpoint.Operations),
                Keys = new FormKeysDto
                {
                    Id = CamelCase(keys?.Id ?? "Id"),
                    Parent = keys?.Parent == null ? null : CamelCase(keys.Parent),
                    NaturalKey = keys?.NaturalKey == null ? null : CamelCase(keys.NaturalKey)
                },
                LockFields = [.. type.GetCustomAttributes<LockFieldAttribute>().Select(a => CamelCase(a.Property))],
                Editors = editors,
                Groups =
                [
                    .. type.GetCustomAttributes<FormGroupAttribute>()
                        .OrderBy(g => g.Order)
                        .ThenBy(g => g.Path, StringComparer.Ordinal)
                        .Select(g => new FormGroupDto
                        {
                            Path = g.Path,
                            Label = g.Label ?? Humanise(g.Path.Split('/')[^1]),
                            Order = g.Order,
                            Collapsed = g.Collapsed,
                            Widget = g.Widget
                        })
                ],
                Fields =
                [
                    .. fields.OrderBy(f => GroupOrder(f.Group))
                        .ThenBy(f => f.Group, StringComparer.Ordinal)
                        .ThenBy(f => f.Order)
                        .ThenBy(f => f.Name, StringComparer.Ordinal)
                ],
                ListColumns =
                [
                    .. columns.OrderBy(c => c.Hidden)
                        .ThenBy(c => c.Order)
                        .ThenBy(c => c.Field, StringComparer.Ordinal)
                ]
            };
        }

        private const int MaxRadioOptions = 3;

        private static FormFieldDto DescribeField(PropertyInfo property, FormFieldAttribute field,
            NullabilityInfoContext nullability, MemberRules? rules, List<string> failures)
        {
            var lookup = property.GetCustomAttribute<LookupAttribute>();
            var type = ClientType(property.PropertyType);
            var integer = type == "integer";
            var options = Choices(property, failures);

            return new FormFieldDto
            {
                Required = rules?.Required ?? false,
                MinLength = rules?.MinLength,
                MaxLength = rules?.MaxLength,
                Min = integer && rules is { Min: not null, ExclusiveMin: true } ? Math.Floor(rules.Min.Value) + 1 : rules?.Min,
                Max = integer && rules is { Max: not null, ExclusiveMax: true } ? Math.Ceiling(rules.Max.Value) - 1 : rules?.Max,
                ExclusiveMin = !integer && (rules?.ExclusiveMin ?? false),
                ExclusiveMax = !integer && (rules?.ExclusiveMax ?? false),
                Patterns = [.. rules?.Patterns ?? []],
                Options = options,
                Name = CamelCase(property.Name),
                Label = field.Label ?? FieldLabel(property.Name),
                Description = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty,
                Type = type,
                Nullable = IsNullable(property, nullability),
                Group = field.Group,
                Order = field.Order,
                Widget = field.Widget == "radio" && options.Count > MaxRadioOptions ? "select" : field.Widget,
                Span = Math.Clamp(field.Span, 1, 2),
                ReadOnly = field.ReadOnly,
                Placeholder = field.Placeholder,
                Help = field.Help,
                Editor = property.GetCustomAttribute<FormEditorAttribute>()?.Component,
                Lookup = lookup == null
                    ? null
                    : new FormLookupDto
                    {
                        Source = lookup.Source,
                        TextField = lookup.TextField,
                        ValueField = lookup.ValueField,
                        ParentField = lookup.ParentField == null ? null : CamelCase(lookup.ParentField),
                        AllowEmpty = lookup.AllowEmpty,
                        UpperField = lookup.UpperField
                    },
                NewDefault = property.GetCustomAttribute<NewDefaultAttribute>()?.Value,
                VisibleWhen =
                [
                    .. property.GetCustomAttributes<VisibleWhenAttribute>()
                        .Select(a => Condition(a.Property, a.Op, a.Values, a.MatchMode))
                ],
                RequiredWhen =
                [
                    .. property.GetCustomAttributes<RequiredWhenAttribute>()
                        .Select(a => Condition(a.Property, a.Op, a.Values, a.MatchMode))
                ],
                EnabledWhen =
                [
                    .. property.GetCustomAttributes<EnabledWhenAttribute>()
                        .Select(a => Condition(a.Property, a.Op, a.Values, a.MatchMode))
                ]
            };
        }

        internal static List<FormOptionDto> Choices(PropertyInfo property, List<string> failures)
        {
            var options = property.GetCustomAttribute<OptionsAttribute>();

            if (options == null)
            {
                return [];
            }

            var underlying = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var textual = underlying == typeof(string) || underlying == typeof(char);
            var allowed = options.Only.Length == 0 ? null : options.Only.Select(only => only.ToString() ?? string.Empty).ToHashSet();
            var choices = new List<FormOptionDto>();

            foreach (var member in options.EnumType.GetFields(BindingFlags.Public | BindingFlags.Static)
                         .OrderBy(candidate => Convert.ToInt64(candidate.GetRawConstantValue())))
            {
                if (allowed != null && !allowed.Contains(member.Name))
                {
                    continue;
                }

                object value;

                if (textual)
                {
                    var stored = member.GetCustomAttribute<EnumMemberAttribute>()?.Value;

                    if (stored == null)
                    {
                        failures.Add(
                            $"{property.DeclaringType?.Name}.{property.Name}: {options.EnumType.Name}.{member.Name} has no EnumMember value for a text property");

                        continue;
                    }

                    value = stored;
                }
                else
                {
                    value = Convert.ToInt64(member.GetRawConstantValue());
                }

                choices.Add(new FormOptionDto
                {
                    Key = $"{options.EnumType.Name}.{member.Name}",
                    Value = value,
                    Label = member.GetCustomAttribute<DescriptionAttribute>()?.Description ?? Humanise(member.Name)
                });
            }

            var unknown = allowed?.Where(name => options.EnumType.GetField(name) == null).ToList() ?? [];

            foreach (var name in unknown)
            {
                failures.Add($"{property.DeclaringType?.Name}.{property.Name}: {options.EnumType.Name} has no member {name}");
            }

            return choices;
        }

        private static List<FormEditorDto> Editors(Type type, List<FormFieldDto> fields, List<string> failures)
        {
            var editors = new List<FormEditorDto>();
            var rule = type.GetCustomAttribute<RuleEditorAttribute>();

            if (rule != null)
            {
                var roles = new Dictionary<string, string?>
                {
                    ["scriptType"] = rule.ScriptType,
                    ["builder"] = rule.Builder,
                    ["builderJson"] = rule.BuilderJson,
                    ["coder"] = rule.Coder
                };

                editors.Add(Claim("ruleBuilder", (int)rule.ParseType, roles, type, fields, failures));
            }

            var caseFilter = type.GetCustomAttribute<CaseFilterEditorAttribute>();

            if (caseFilter != null)
            {
                var roles = new Dictionary<string, string?>
                {
                    ["select"] = caseFilter.Select,
                    ["filter"] = caseFilter.Filter
                };

                editors.Add(Claim("caseFilter", null, roles, type, fields, failures));
            }

            var dataset = type.GetCustomAttribute<ExhaustiveDatasetEditorAttribute>();

            if (dataset != null)
            {
                var roles = new Dictionary<string, string?>
                {
                    ["csv"] = dataset.Csv,
                    ["json"] = dataset.Json
                };
                var reads = new Dictionary<string, string?>
                {
                    ["source"] = dataset.Source,
                    ["status"] = dataset.Status
                };

                editors.Add(Claim("exhaustiveDataset", null, roles, type, fields, failures, reads));
            }

            var exhaustiveFilter = type.GetCustomAttribute<ExhaustiveFilterEditorAttribute>();

            if (exhaustiveFilter != null)
            {
                var roles = new Dictionary<string, string?>
                {
                    ["filterJson"] = exhaustiveFilter.FilterJson,
                    ["filterSql"] = exhaustiveFilter.FilterSql,
                    ["filterTokens"] = exhaustiveFilter.FilterTokens
                };
                var reads = new Dictionary<string, string?>
                {
                    ["source"] = exhaustiveFilter.Source,
                    ["csv"] = exhaustiveFilter.Csv,
                    ["json"] = exhaustiveFilter.Json
                };

                editors.Add(Claim("exhaustiveFilter", null, roles, type, fields, failures, reads));
            }

            var datasource = type.GetCustomAttribute<DatasourceEditorAttribute>();

            if (datasource != null)
            {
                var roles = new Dictionary<string, string?>
                {
                    ["command"] = datasource.Command,
                    ["displayKind"] = datasource.DisplayKind,
                    ["grid"] = datasource.Grid,
                    ["template"] = datasource.Template,
                    ["mapping"] = datasource.Mapping
                };

                editors.Add(Claim("datasource", null, roles, type, fields, failures));
            }

            var caseSql = type.GetCustomAttribute<CaseSqlEditorAttribute>();

            if (caseSql != null)
            {
                editors.Add(Claim("caseSql", null, new Dictionary<string, string?> { ["sql"] = caseSql.Sql }, type,
                    fields, failures));
            }

            return editors;
        }

        private static FormEditorDto Claim(string kind, int? parseType, Dictionary<string, string?> roles, Type type,
            List<FormFieldDto> fields, List<string> failures, Dictionary<string, string?>? reads = null)
        {
            var editor = new FormEditorDto { Kind = kind, ParseType = parseType };

            foreach (var (role, property) in roles.Where(pair => pair.Value != null))
            {
                var field = fields.SingleOrDefault(candidate => candidate.Name == CamelCase(property!));

                if (field == null)
                {
                    failures.Add($"{type.Name}: the {kind} editor names {property} as its {role}, which is not a form field");

                    continue;
                }

                field.Editor = kind;
                editor.Roles[role] = field.Name;
            }

            foreach (var (role, property) in (reads ?? []).Where(pair => pair.Value != null))
            {
                var field = fields.SingleOrDefault(candidate => candidate.Name == CamelCase(property!));

                if (field == null)
                {
                    failures.Add($"{type.Name}: the {kind} editor reads {property} as its {role}, which is not a form field");

                    continue;
                }

                editor.Reads[role] = field.Name;
            }

            return editor;
        }

        private static List<string> OperationNames(FormOperations operations)
        {
            return
            [
                .. Enum.GetValues<FormOperations>()
                    .Where(operation => operation != FormOperations.None && operation != FormOperations.All &&
                                        operations.HasFlag(operation))
                    .Select(operation => CamelCase(operation.ToString()))
            ];
        }

        private static FormConditionDto Condition(string property, ConditionOp op, object[] values, string matchMode)
        {
            return new FormConditionDto
            {
                Property = CamelCase(property),
                Op = CamelCase(op.ToString()),
                Values = [.. values],
                MatchMode = matchMode
            };
        }

        private static bool IsNullable(PropertyInfo property, NullabilityInfoContext nullability)
        {
            if (Nullable.GetUnderlyingType(property.PropertyType) != null)
            {
                return true;
            }

            return !property.PropertyType.IsValueType &&
                   nullability.Create(property).ReadState == NullabilityState.Nullable;
        }

        internal static string ClientType(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (underlying == typeof(bool))
            {
                return "boolean";
            }

            if (underlying == typeof(Guid))
            {
                return "guid";
            }

            if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset))
            {
                return "datetime";
            }

            if (underlying == typeof(double) || underlying == typeof(float) || underlying == typeof(decimal))
            {
                return "number";
            }

            if (underlying != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(underlying))
            {
                return "json";
            }

            if (underlying == typeof(int) || underlying == typeof(long) || underlying == typeof(short) ||
                underlying == typeof(byte) || underlying.IsEnum)
            {
                return "integer";
            }

            return "string";
        }

        internal static string CamelCase(string name)
        {
            return name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
        }

        internal static string FieldLabel(string name)
        {
            var words = Humanise(name).Split(' ').ToList();

            words.RemoveAll(word => string.Equals(word, "guid", StringComparison.OrdinalIgnoreCase));

            if (words.Count > 1 && string.Equals(words[^1], "id", StringComparison.OrdinalIgnoreCase))
                words.RemoveAt(words.Count - 1);

            if (words.Count == 0) return Humanise(name);

            for (var index = 0; index < words.Count; index++)
            {
                words[index] = words[index].ToLowerInvariant() switch
                {
                    "ttl" => "TTL",
                    "json" => "JSON",
                    "sql" => "SQL",
                    "url" => "URL",
                    _ => words[index]
                };
            }

            words[0] = char.ToUpperInvariant(words[0][0]) + words[0][1..];

            return string.Join(' ', words);
        }

        private static string Humanise(string name)
        {
            var builder = new StringBuilder();

            for (var index = 0; index < name.Length; index++)
            {
                var character = name[index];

                if (index > 0 && char.IsUpper(character) &&
                    (!char.IsUpper(name[index - 1]) || (index + 1 < name.Length && char.IsLower(name[index + 1]))))
                {
                    builder.Append(' ');
                    builder.Append(char.ToLowerInvariant(character));
                }
                else
                {
                    builder.Append(index == 0 ? char.ToUpperInvariant(character) : character);
                }
            }

            return builder.ToString();
        }
    }
}
