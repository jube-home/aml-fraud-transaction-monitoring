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

using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Repository.CaseWorkflowFormValidator;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseWorkflowFormValidator
{
    public sealed class CaseWorkflowFormValidatorDtoValidator : AbstractValidator<CaseWorkflowFormValidatorDto>
    {
        private const int MaxNameLength = 100;
        private const int MaxArgumentLength = 500;
        private const int MaxSqlLength = 8000;

        private static readonly Regex FieldNamePattern = new("^[A-Za-z][A-Za-z0-9_]{0,63}$",
            RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        private static readonly string[] Relations = ["eq", "ne", "lt", "le", "gt", "ge"];

        public CaseWorkflowFormValidatorDtoValidator(CaseWorkflowFormValidatorRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.CaseWorkflowFormGuid)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.CaseWorkflowFormGuidRequired])
                .WithErrorCode("CaseWorkflowFormGuidNotEmpty")
                .MustAsync(async (guid, cancellation) =>
                    await repository.ExistsCaseWorkflowFormAsync(guid, cancellation))
                .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.CaseWorkflowFormNotFound])
                .WithErrorCode("CaseWorkflowFormGuidNotFound");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.NameTooLong])
                .WithErrorCode("NameMaximumLength");

            RuleFor(p => p.FieldName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.FieldNameRequired])
                .WithErrorCode("FieldNameNotEmpty")
                .Must(name => FieldNamePattern.IsMatch(name!))
                .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.FieldNameInvalid])
                .WithErrorCode("FieldNameInvalid");

            RuleFor(p => p.KindId)
                .Must(kind => Enum.IsDefined(typeof(CaseWorkflowFormValidatorKind), kind))
                .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.KindInvalid])
                .WithErrorCode("KindIdInvalid");

            RuleFor(p => p.Message)
                .MaximumLength(MaxArgumentLength)
                .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.MessageTooLong])
                .WithErrorCode("MessageMaximumLength");

            When(p => Kind(p) == CaseWorkflowFormValidatorKind.RequiredWhen, () =>
            {
                RuleFor(p => p.Argument1).Cascade(CascadeMode.Stop)
                    .Must(a => !string.IsNullOrWhiteSpace(a))
                    .WithMessage(_ => string.Format(localiser[CaseWorkflowFormValidatorResources.ArgumentRequired],
                        nameof(CaseWorkflowFormValidatorDto.Argument1)))
                    .WithErrorCode("Argument1NotEmpty")
                    .Must(a => FieldNamePattern.IsMatch(a!))
                    .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.FieldNameInvalid])
                    .WithErrorCode("Argument1FieldName");

                RuleFor(p => p.Argument2)
                    .Must(a => !string.IsNullOrEmpty(a))
                    .WithMessage(_ => string.Format(localiser[CaseWorkflowFormValidatorResources.ArgumentRequired],
                        nameof(CaseWorkflowFormValidatorDto.Argument2)))
                    .WithErrorCode("Argument2NotEmpty");
            });

            When(p => Kind(p) == CaseWorkflowFormValidatorKind.Range, () =>
            {
                RuleFor(p => p)
                    .Cascade(CascadeMode.Stop)
                    .Must(p => !string.IsNullOrWhiteSpace(p.Argument1) || !string.IsNullOrWhiteSpace(p.Argument2))
                    .OverridePropertyName(nameof(CaseWorkflowFormValidatorDto.Argument1))
                    .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.ArgumentOneNeeded])
                    .WithErrorCode("RangeOneNeeded")
                    .Must(p => Bound(p.Argument1) && Bound(p.Argument2))
                    .OverridePropertyName(nameof(CaseWorkflowFormValidatorDto.Argument1))
                    .WithMessage(_ => string.Format(localiser[CaseWorkflowFormValidatorResources.ArgumentNumber],
                        "Argument1 and Argument2"))
                    .WithErrorCode("RangeBound")
                    .Must(p => !Number(p.Argument1, out var low) || !Number(p.Argument2, out var high) || low <= high)
                    .OverridePropertyName(nameof(CaseWorkflowFormValidatorDto.Argument1))
                    .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.ArgumentRange])
                    .WithErrorCode("RangeOrder");
            });

            When(p => Kind(p) == CaseWorkflowFormValidatorKind.Length, () =>
            {
                RuleFor(p => p)
                    .Cascade(CascadeMode.Stop)
                    .Must(p => !string.IsNullOrWhiteSpace(p.Argument1) || !string.IsNullOrWhiteSpace(p.Argument2))
                    .OverridePropertyName(nameof(CaseWorkflowFormValidatorDto.Argument1))
                    .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.ArgumentOneNeeded])
                    .WithErrorCode("LengthOneNeeded")
                    .Must(p => Whole(p.Argument1) && Whole(p.Argument2))
                    .OverridePropertyName(nameof(CaseWorkflowFormValidatorDto.Argument1))
                    .WithMessage(_ => string.Format(localiser[CaseWorkflowFormValidatorResources.ArgumentWholeNumber],
                        "Argument1 and Argument2"))
                    .WithErrorCode("LengthBound")
                    .Must(p => !Number(p.Argument1, out var low) || !Number(p.Argument2, out var high) || low <= high)
                    .OverridePropertyName(nameof(CaseWorkflowFormValidatorDto.Argument1))
                    .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.ArgumentRange])
                    .WithErrorCode("LengthOrder");
            });

            When(p => Kind(p) == CaseWorkflowFormValidatorKind.Pattern, () =>
            {
                RuleFor(p => p.Argument1).Cascade(CascadeMode.Stop)
                    .Must(a => !string.IsNullOrEmpty(a))
                    .WithMessage(_ => string.Format(localiser[CaseWorkflowFormValidatorResources.ArgumentRequired],
                        nameof(CaseWorkflowFormValidatorDto.Argument1)))
                    .WithErrorCode("Argument1NotEmpty")
                    .Must(a => a!.Length <= MaxArgumentLength)
                    .WithMessage(_ => string.Format(localiser[CaseWorkflowFormValidatorResources.ArgumentTooLong],
                        nameof(CaseWorkflowFormValidatorDto.Argument1), MaxArgumentLength))
                    .WithErrorCode("Argument1MaximumLength")
                    .Must(Compiles)
                    .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.PatternInvalid])
                    .WithErrorCode("PatternInvalid");
            });

            When(p => Kind(p) == CaseWorkflowFormValidatorKind.Compare, () =>
            {
                RuleFor(p => p.Argument1)
                    .Must(a => a != null && Relations.Contains(a))
                    .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.ArgumentRelation])
                    .WithErrorCode("Argument1Relation");

                RuleFor(p => p.Argument2).Cascade(CascadeMode.Stop)
                    .Must(a => !string.IsNullOrWhiteSpace(a))
                    .WithMessage(_ => string.Format(localiser[CaseWorkflowFormValidatorResources.ArgumentRequired],
                        nameof(CaseWorkflowFormValidatorDto.Argument2)))
                    .WithErrorCode("Argument2NotEmpty")
                    .Must(a => FieldNamePattern.IsMatch(a!))
                    .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.FieldNameInvalid])
                    .WithErrorCode("Argument2FieldName");
            });

            When(p => Kind(p) is CaseWorkflowFormValidatorKind.InList or CaseWorkflowFormValidatorKind.InDictionary,
                () =>
                {
                    RuleFor(p => p.Argument1).Cascade(CascadeMode.Stop)
                        .Must(a => !string.IsNullOrWhiteSpace(a))
                        .WithMessage(_ => string.Format(localiser[CaseWorkflowFormValidatorResources.ArgumentRequired],
                            nameof(CaseWorkflowFormValidatorDto.Argument1)))
                        .WithErrorCode("Argument1NotEmpty")
                        .Must(a => a!.Length <= MaxArgumentLength)
                        .WithMessage(_ => string.Format(localiser[CaseWorkflowFormValidatorResources.ArgumentTooLong],
                            nameof(CaseWorkflowFormValidatorDto.Argument1), MaxArgumentLength))
                        .WithErrorCode("Argument1MaximumLength");
                });

            When(p => Kind(p) == CaseWorkflowFormValidatorKind.Sql, () =>
            {
                RuleFor(p => p.Sql).Cascade(CascadeMode.Stop)
                    .Must(a => !string.IsNullOrWhiteSpace(a))
                    .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.SqlRequired])
                    .WithErrorCode("SqlNotEmpty")
                    .Must(a => a!.Length <= MaxSqlLength)
                    .WithMessage(_ => localiser[CaseWorkflowFormValidatorResources.SqlTooLong])
                    .WithErrorCode("SqlMaximumLength");
            });
        }

        private static CaseWorkflowFormValidatorKind? Kind(CaseWorkflowFormValidatorDto dto)
        {
            return Enum.IsDefined(typeof(CaseWorkflowFormValidatorKind), dto.KindId)
                ? (CaseWorkflowFormValidatorKind)dto.KindId
                : null;
        }

        private static bool Number(string? text, out decimal number)
        {
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out number);
        }

        private static bool Bound(string? text)
        {
            return string.IsNullOrWhiteSpace(text) || Number(text, out _) ||
                   DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        private static bool Whole(string? text)
        {
            return string.IsNullOrWhiteSpace(text) ||
                   int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out _);
        }

        private static bool Compiles(string? pattern)
        {
            try
            {
                _ = new Regex(pattern!, RegexOptions.NonBacktracking | RegexOptions.CultureInvariant,
                    TimeSpan.FromMilliseconds(250));

                return true;
            }
            catch (Exception e) when (e is ArgumentException or NotSupportedException)
            {
                return false;
            }
        }
    }
}
