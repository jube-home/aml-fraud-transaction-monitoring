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

using Jube.Dto.Options;
using Jube.Dto.Query.CaseBulk;
using Jube.Dto.Query.CaseSupervision;
using Jube.Service.Query.CaseSupervision;
using Jube.Service.Repository.Case;
using log4net;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;
using SupervisionExceptions = Jube.Service.Exceptions.Query.CaseSupervision;

namespace Jube.Service.Query.CaseBulk
{
    public sealed class CaseBulkService(CaseService caseService, CaseSupervisionService supervisionService, ILog log)
    {
        public const int MaximumCases = 500;
        private const int MaximumNote = 1000;

        private static List<int> Distinct(List<int>? ids)
        {
            if (ids == null || ids.Count == 0)
            {
                throw new ArgumentException("At least one case is required.");
            }

            var distinct = ids.Where(i => i > 0).Distinct().ToList();

            if (distinct.Count == 0 || distinct.Count > MaximumCases)
            {
                throw new ArgumentException($"Between 1 and {MaximumCases} cases are required.");
            }

            return distinct;
        }

        private static CaseBulkResultDto Result(List<CaseBulkItemDto> items)
        {
            return new CaseBulkResultDto
            {
                Items = items, Done = items.Count(i => i.Done), Skipped = items.Count(i => !i.Done)
            };
        }

        private static CaseBulkItemDto Done(int caseId)
        {
            return new CaseBulkItemDto { CaseId = caseId, Done = true };
        }

        private static CaseBulkItemDto Skip(int caseId, CaseBulkSkipReason reason)
        {
            return new CaseBulkItemDto { CaseId = caseId, SkipReasonId = (byte)reason };
        }

        public async Task<CaseBulkResultDto> CloseAsync(CaseBulkCloseDto? model, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            var ids = Distinct(model.CaseIds);

            if (model.ClosedReasonId > 8)
            {
                throw new ArgumentException("The close reason is not recognised.");
            }

            if (model.ClosedReasonNote is { Length: > MaximumNote })
            {
                throw new ArgumentException($"The note must be at most {MaximumNote} characters.");
            }

            var items = new List<CaseBulkItemDto>(ids.Count);

            foreach (var id in ids)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    var current = await caseService.GetByIdAsync(id, token).ConfigureAwait(false);

                    if (current == null)
                    {
                        items.Add(Skip(id, CaseBulkSkipReason.NotFound));

                        continue;
                    }

                    if (current.ClosedStatusId == 3)
                    {
                        items.Add(Skip(id, CaseBulkSkipReason.AlreadyClosed));

                        continue;
                    }

                    current.ClosedStatusId = 3;
                    current.ClosedReasonId = model.ClosedReasonId;
                    current.ClosedReasonNote = model.ClosedReasonNote;
                    current.Payload = null;

                    await caseService.UpdateAsync(current, token).ConfigureAwait(false);
                    items.Add(Done(id));
                }
                catch (CaseExceptions.ChecklistIncompleteException)
                {
                    items.Add(Skip(id, CaseBulkSkipReason.ChecklistIncomplete));
                }
                catch (CaseExceptions.ConflictException)
                {
                    items.Add(Skip(id, CaseBulkSkipReason.Conflict));
                }
                catch (CaseExceptions.NotFoundException)
                {
                    items.Add(Skip(id, CaseBulkSkipReason.NotFound));
                }
                catch (CaseExceptions.DtoValidationException)
                {
                    items.Add(Skip(id, CaseBulkSkipReason.Invalid));
                }
            }

            if (log.IsInfoEnabled)
            {
                log.Info($"CaseBulk.Close: requested={ids.Count} done={items.Count(i => i.Done)}");
            }

            return Result(items);
        }

        public async Task<CaseBulkResultDto> ReassignAsync(CaseBulkReassignDto? model,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            var ids = Distinct(model.CaseIds);

            if (string.IsNullOrWhiteSpace(model.ToUser))
            {
                throw new ArgumentException("The analyst is required.");
            }

            var items = new List<CaseBulkItemDto>(ids.Count);

            foreach (var id in ids)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    await supervisionService.ReassignAsync(new CaseReassignDto { CaseId = id, ToUser = model.ToUser },
                        token).ConfigureAwait(false);
                    items.Add(Done(id));
                }
                catch (SupervisionExceptions.ConflictException)
                {
                    items.Add(Skip(id, CaseBulkSkipReason.Conflict));
                }
                catch (SupervisionExceptions.NotFoundException)
                {
                    items.Add(Skip(id, CaseBulkSkipReason.NotFound));
                }
                catch (SupervisionExceptions.DtoValidationException)
                {
                    items.Add(Skip(id, CaseBulkSkipReason.Invalid));
                }
            }

            if (log.IsInfoEnabled)
            {
                log.Info($"CaseBulk.Reassign: requested={ids.Count} done={items.Count(i => i.Done)}");
            }

            return Result(items);
        }
    }
}
