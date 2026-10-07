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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Data.Repository;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;

    public static class SyncOverridesExtensions
    {
        public static async Task<Context> SyncOverridesAsync(this Context context)
        {
            try
            {
                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose of adding overrides.");
                    }

                    var shadow = new Dictionary<string, Dictionary<string, EntityAnalysisModelOverride>>();

                    await AddModelOverridesAsync(context, key, shadow).ConfigureAwait(false);
                    await AddActivationRuleOverrideAsync(context, key, value.Instance.Guid, shadow)
                        .ConfigureAwait(false);

                    context.Snapshots.Builder(key, value).EntityAnalysisModelOverrides = shadow;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Model {key} has {shadow.Count} override keys added to collection.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncOverridesAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(
                        EntityAnalysisModelSynchronisationErrorRepository
                            .EntityAnalysisModelSynchronisationErrorStepEnum.Override, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }

        private static async Task AddModelOverridesAsync(Context context, int key,
            Dictionary<string, Dictionary<string, EntityAnalysisModelOverride>> shadow)
        {
            var repository = new EntityAnalysisModelOverrideRepository(context.Services.DbContext);

            var records = await repository
                .GetByEntityAnalysisModelIdAsync(key, context.Services.CancellationToken).ConfigureAwait(false);

            foreach (var record in records)
            {
                context.Services.CancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (record.OverrideKey == null || record.OverrideKeyValue == null)
                    {
                        continue;
                    }

                    Promote(shadow, record.OverrideKey, record.OverrideKeyValue, null,
                        ToOverrideKind(record.OverrideKind));

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Model {key} and Override ID {record.Id} added an all activation rules override for {record.OverrideKey}.");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Services.Log.Error(
                        $"Entity Start: Override ID {record.Id} returned for model {key} is in error with {ex}.");
                }
            }
        }

        private static async Task AddActivationRuleOverrideAsync(Context context, int key,
            Guid entityAnalysisModelGuid,
            Dictionary<string, Dictionary<string, EntityAnalysisModelOverride>> shadow)
        {
            var repository =
                new EntityAnalysisModelActivationRuleOverrideRepository(context.Services.DbContext);

            var records = await repository
                .GetByEntityAnalysisModelGuidOrderByIdAsync(entityAnalysisModelGuid,
                    context.Services.CancellationToken).ConfigureAwait(false);

            foreach (var record in records)
            {
                context.Services.CancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (record.OverrideKey == null || record.OverrideKeyValue == null
                                                   || record.EntityAnalysisModelActivationRuleName == null)
                    {
                        continue;
                    }

                    Promote(shadow, record.OverrideKey, record.OverrideKeyValue,
                        record.EntityAnalysisModelActivationRuleName, ToOverrideKind(record.OverrideKind));

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Model {key} and Activation Rule Override ID {record.Id} added an override for {record.OverrideKey} against {record.EntityAnalysisModelActivationRuleName}.");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Services.Log.Error(
                        $"Entity Start: Activation Rule Override ID {record.Id} returned for model {key} is in error with {ex}.");
                }
            }
        }

        private static void Promote(
            Dictionary<string, Dictionary<string, EntityAnalysisModelOverride>> shadow,
            string overrideKey, string overrideKeyValue, string activationRuleName,
            EntityAnalysisModelOverrideKind kind)
        {
            if (!shadow.TryGetValue(overrideKey, out var values))
            {
                values = new Dictionary<string, EntityAnalysisModelOverride>();
                shadow.Add(overrideKey, values);
            }

            if (!values.TryGetValue(overrideKeyValue, out var overrideBinding))
            {
                overrideBinding = new EntityAnalysisModelOverride();
                values.Add(overrideKeyValue, overrideBinding);
            }

            overrideBinding.Promote(activationRuleName, kind);
        }

        private static EntityAnalysisModelOverrideKind ToOverrideKind(byte? overrideKind)
        {
            return overrideKind == (byte)EntityAnalysisModelOverrideKind.Force
                ? EntityAnalysisModelOverrideKind.Force
                : EntityAnalysisModelOverrideKind.Suppress;
        }
    }
}