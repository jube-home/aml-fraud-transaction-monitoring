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
using System.Threading;
using System.Threading.Tasks;
using Jube.Analytics.Datasets;
using Jube.Data.Repository;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Training.Exceptions;
using Jube.Engine.AdvancedAnalytics.Training.Models;

namespace Jube.Engine.AdvancedAnalytics.Training
{
    public sealed class AdvancedAnalyticsTrainingInputResolver
    {
        private readonly Dictionary<AdvancedAnalyticsTrainingInputKind, IAdvancedAnalyticsTrainingInputSource> sources;

        public AdvancedAnalyticsTrainingInputResolver(
            IEnumerable<IAdvancedAnalyticsTrainingInputSource> additional = null)
        {
            sources = new Dictionary<AdvancedAnalyticsTrainingInputKind, IAdvancedAnalyticsTrainingInputSource>
            {
                [AdvancedAnalyticsTrainingInputKind.Inline] = new InlineSource(),
                [AdvancedAnalyticsTrainingInputKind.CachePointer] = new CachePointerSource()
            };

            foreach (var source in additional ?? [])
            {
                sources[source.Kind] = source;
            }
        }

        public bool Supports(AdvancedAnalyticsTrainingInputKind kind)
        {
            return sources.ContainsKey(kind);
        }

        public Task<AdvancedAnalyticsResolvedTrainingData> ResolveAsync(AdvancedAnalyticsTrainingInputContext context,
            CancellationToken token)
        {
            if (!sources.TryGetValue(context.Input.Kind, out var source))
            {
                throw new NotSupportedException(
                    $"Training input of kind {context.Input.Kind} is not supported by this engine.");
            }

            return source.ResolveAsync(context, token);
        }

        private sealed class InlineSource : IAdvancedAnalyticsTrainingInputSource
        {
            public AdvancedAnalyticsTrainingInputKind Kind => AdvancedAnalyticsTrainingInputKind.Inline;

            public Task<AdvancedAnalyticsResolvedTrainingData> ResolveAsync(
                AdvancedAnalyticsTrainingInputContext context, CancellationToken token)
            {
                if (context.Input.Inputs == null || context.Input.Labels == null)
                {
                    throw new AdvancedAnalyticsTrainingInputException(
                        "The run carries no inline rows to train on.");
                }

                return Task.FromResult(new AdvancedAnalyticsResolvedTrainingData
                {
                    Inputs = context.Input.Inputs,
                    Labels = context.Input.Labels
                });
            }
        }

        private sealed class CachePointerSource : IAdvancedAnalyticsTrainingInputSource
        {
            public AdvancedAnalyticsTrainingInputKind Kind => AdvancedAnalyticsTrainingInputKind.CachePointer;

            public async Task<AdvancedAnalyticsResolvedTrainingData> ResolveAsync(
                AdvancedAnalyticsTrainingInputContext context, CancellationToken token)
            {
                if (context.Input.Pointer is not { } pointer || pointer == Guid.Empty)
                {
                    throw new AdvancedAnalyticsTrainingInputException("The run names no cached input.");
                }

                var repository = new AdvancedAnalyticsCacheRepository(context.DbContext, context.TenantRegistryId,
                    context.UserName, new AdvancedAnalyticsCacheLimits());

                var entry = await repository.GetAsync(pointer, token).ConfigureAwait(false);

                if (entry == null)
                {
                    throw new AdvancedAnalyticsTrainingInputException(
                        "The cached input has expired or does not exist. Put the rows in the cache again and " +
                        "submit a new run.");
                }

                var values = AdvancedAnalyticsPacking.Unpack(entry.Data, entry.PackingVersion);

                try
                {
                    var (inputs, labels) = CachedTableRows.Split(values, entry.Columns, entry.Rows,
                        context.Input.LabelColumn, context.VariableCount, token);

                    return new AdvancedAnalyticsResolvedTrainingData { Inputs = inputs, Labels = labels };
                }
                catch (InvalidOperationException ex)
                {
                    throw new AdvancedAnalyticsTrainingInputException(ex.Message);
                }
            }
        }
    }
}