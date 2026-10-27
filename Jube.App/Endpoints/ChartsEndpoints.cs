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
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jube.App.Endpoints.AskJoobyAnalytics;
using Jube.Data.Context;
using Jube.Dto.Query.AdvancedAnalyticsNarrative;
using Jube.Dto.Query.ExhaustiveNarrative;
using Jube.Dto.Payload;
using Jube.Service.Charts;
using Jube.Service.Charts.Models;
using Jube.Service.Exceptions;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints
{
    public static class ChartsEndpoints
    {
        private const string Base = "/api/Chart";
        private const string ExhaustiveBase = Base + "/Exhaustive";
        private const string StatisticsBase = Base + "/ExhaustiveStatistics";
        private const string AnalyticsBase = Base + "/AnalyticsModelVersion";
        private const string PayloadStatisticsBase = Base + "/PayloadStatistics";
        private const string WatcherBase = Base + "/Watcher";
        private const string BacktestBase = Base + "/Backtest";
        private const string SvgContentType = "image/svg+xml; charset=utf-8";
        private const string ThresholdSetting = "ExhaustiveValidationTestingActivationThreshold";

        public static void MapChartsEndpoints(this IEndpointRouteBuilder endpoints)
        {
            MapExhaustive(endpoints);
            MapExhaustivePerformance(endpoints);
            MapExhaustiveStatistics(endpoints);
            MapAnalytics(endpoints);
            MapPayloadStatistics(endpoints);
        }

        private static void MapExhaustive(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(ExhaustiveBase)
                .RequireAuthorization()
                .WithTags("ExhaustiveChart");

            group.MapGet("LearningCurve/{id:int}", LearningCurveAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveChartLearningCurveGet");
            group.MapGet("Roc/{id:int}", RocAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveChartRocGet");
            group.MapGet("PredictedActual/{id:int}", PredictedActualAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveChartPredictedActualGet");
            group.MapGet("CalibrationComparison/{id:int}", CalibrationComparisonAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveChartCalibrationComparisonGet");
            group.MapGet("ErrorHistogram/{id:int}", ErrorHistogramAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveChartErrorHistogramGet");
            group.MapGet("Confusion/{id:int}", ConfusionAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveChartConfusionGet");
            group.MapGet("Sensitivity/{id:int}", SensitivityAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveChartSensitivityGet");
            group.MapGet("Topology/{id:int}", TopologyAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveChartTopologyGet");
            group.MapPost("Topology/{id:int}", TopologyWithLiveRelevanceAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveChartTopologyPost");
            group.MapGet("TrialDiagram/{trialInstanceId:int}", TrialDiagramAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveChartTrialDiagramGet");
            group.MapGet("VariableHistogram/{id:int}/{variableId:int}", VariableHistogramAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveChartVariableHistogramGet");
            group.MapGet("PrescriptionHistogram/{id:int}/{promotedTrialInstanceVariableId:int}",
                    PrescriptionHistogramAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveChartPrescriptionHistogramGet");
            group.MapGet("PrescriptionSummary/{id:int}/{promotedTrialInstanceVariableId:int}",
                    PrescriptionSummaryAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveChartPrescriptionSummaryGet");

            group.MapGet("Narrative/{id:int}", NarrativeAsync)
                .Produces<ExhaustiveNarrativeDto>()
                .WithName("ExhaustiveChartNarrativeGet");
        }

        private static Task<IResult> NarrativeAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RunAsync(ExhaustiveBase, "Narrative/" + id, httpContext, log, dynamicEnvironment,
                async dbContext =>
                {
                    var narrative = await (await ExhaustiveNarrativeService.CreateAsync(dbContext,
                            httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                        .GetAsync(id, token);

                    Harden(httpContext);

                    return TypedResults.Json(narrative);
                });
        }

        private static void MapExhaustivePerformance(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(ExhaustiveBase)
                .RequireAuthorization()
                .WithTags("ExhaustiveChart");

            MapPerformance(group, "ScoreDistribution", ExhaustivePerformanceChart.ScoreDistribution);
            MapPerformance(group, "PerformanceRoc", ExhaustivePerformanceChart.Roc);
            MapPerformance(group, "PrecisionRecall", ExhaustivePerformanceChart.PrecisionRecall);
            MapPerformance(group, "Calibration", ExhaustivePerformanceChart.Calibration);
            MapPerformance(group, "Gains", ExhaustivePerformanceChart.Gains);
            MapPerformance(group, "ValueCaptured", ExhaustivePerformanceChart.ValueCaptured);
            MapPerformance(group, "Lift", ExhaustivePerformanceChart.Lift);
            MapPerformance(group, "Ks", ExhaustivePerformanceChart.Ks);
            MapPerformance(group, "ThresholdSweep", ExhaustivePerformanceChart.ThresholdSweep);
            MapPerformance(group, "PerformanceConfusion", ExhaustivePerformanceChart.Confusion);
            MapPerformance(group, "Measures", ExhaustivePerformanceChart.Measures);

            group.MapGet("PromotionHistory/{id:int}", PromotionHistoryAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveChartPromotionHistoryGet");
        }

        private static void MapExhaustiveStatistics(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(StatisticsBase)
                .RequireAuthorization()
                .WithTags("ExhaustiveStatisticsChart");

            group.MapGet("VariableClasses/{id:int}/{variableId:int}", VariableClassesAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveStatisticsChartVariableClassesGet");
            group.MapGet("MonteCarlo/{id:int}/{variableId:int}", MonteCarloAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveStatisticsChartMonteCarloGet");
            group.MapGet("MonteCarloSummary/{id:int}/{variableId:int}", MonteCarloSummaryAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveStatisticsChartMonteCarloSummaryGet");

            group.MapGet("Grid/{id:int}", GridAsync)
                .Produces<ExhaustiveStatisticsChartGridItem[]>()
                .WithName("ExhaustiveStatisticsChartGridGet");
        }

        private static void MapPayloadStatistics(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(PayloadStatisticsBase)
                .RequireAuthorization()
                .WithTags("PayloadStatisticsChart");

            group.MapPost("Histogram", PayloadStatisticsHistogram)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("PayloadStatisticsChartHistogramPost");

            var backtest = endpoints.MapGroup(BacktestBase)
                .RequireAuthorization()
                .WithTags("BacktestChart");

            backtest.MapPost("Funnel", (BacktestChartRequest request, HttpContext httpContext, ILog log) =>
                    BacktestSvg(request, httpContext, log, BacktestChartRenderer.Funnel, "Funnel"))
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("BacktestChartFunnelPost");
            backtest.MapPost("Confusion", (BacktestChartRequest request, HttpContext httpContext, ILog log) =>
                    BacktestSvg(request, httpContext, log, BacktestChartRenderer.Confusion, "Confusion"))
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("BacktestChartConfusionPost");
            backtest.MapPost("Rates", (BacktestChartRequest request, HttpContext httpContext, ILog log) =>
                    BacktestSvg(request, httpContext, log, BacktestChartRenderer.Rates, "Rates"))
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("BacktestChartRatesPost");
            backtest.MapPost("Tags", (BacktestChartRequest request, HttpContext httpContext, ILog log) =>
                    BacktestSvg(request, httpContext, log, BacktestChartRenderer.Tags, "Tags"))
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("BacktestChartTagsPost");

            endpoints.MapGroup(WatcherBase)
                .RequireAuthorization()
                .WithTags("WatcherChart")
                .MapPost("ActivationSummary", WatcherActivationSummary)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("WatcherChartActivationSummaryPost");
        }

        private static IResult BacktestSvg(BacktestChartRequest request, HttpContext httpContext, ILog log,
            Func<BacktestChartRequest, string> render, string kind)
        {
            try
            {
                return Svg(httpContext, render(request));
            }
            catch (ArgumentException)
            {
                return TypedResults.BadRequest();
            }
            catch (Exception e)
            {
                log.Error($"POST {BacktestBase}/{kind}: 500 user={httpContext.User.Identity?.Name}", e);

                return TypedResults.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private static IResult WatcherActivationSummary(WatcherSummaryChartRequest request,
            HttpContext httpContext, ILog log)
        {
            try
            {
                return Svg(httpContext, WatcherSummaryChartRenderer.Render(request));
            }
            catch (ArgumentException)
            {
                return TypedResults.BadRequest();
            }
            catch (Exception e)
            {
                log.Error($"POST {WatcherBase}/ActivationSummary: 500 user={httpContext.User.Identity?.Name}", e);

                return TypedResults.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private static IResult PayloadStatisticsHistogram(StatisticsHistogramChartRequest request,
            HttpContext httpContext, ILog log)
        {
            try
            {
                return Svg(httpContext, StatisticsHistogramChartRenderer.Render(request));
            }
            catch (ArgumentException)
            {
                return TypedResults.BadRequest();
            }
            catch (Exception e)
            {
                log.Error($"POST {PayloadStatisticsBase}/Histogram: 500 user={httpContext.User.Identity?.Name}", e);

                return TypedResults.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private static void MapAnalytics(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(AnalyticsBase)
                .RequireAuthorization()
                .WithTags("AdvancedAnalyticsChart");

            group.MapGet("ModelDiagram/{versionId:int}", AnalyticsModelDiagramAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("AdvancedAnalyticsChartModelDiagramGet");
            group.MapGet("Calibration/{versionId:int}", AnalyticsCalibrationAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("AdvancedAnalyticsChartCalibrationGet");
            group.MapGet("Roc/{versionId:int}", AnalyticsRocAsync)
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("AdvancedAnalyticsChartRocGet");

            group.MapGet("Narrative/{versionId:int}", AnalyticsNarrativeAsync)
                .Produces<AdvancedAnalyticsNarrativeDto>()
                .WithName("AdvancedAnalyticsChartNarrativeGet");
        }

        private static Task<IResult> AnalyticsNarrativeAsync(int versionId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return AskJoobyAnalyticsEndpointRunner.RunAsync("GET", $"{AnalyticsBase}/Narrative/{versionId}",
                httpContext, log, dynamicEnvironment, async dbContext =>
                {
                    var narrative = await (await AdvancedAnalyticsNarrativeService.CreateAsync(dbContext,
                            httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                        .GetAsync(versionId, token);

                    Harden(httpContext);

                    return TypedResults.Json(narrative);
                });
        }

        private static Task<IResult> LearningCurveAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RenderExhaustiveAsync("LearningCurve/" + id, httpContext, log, dynamicEnvironment, async dbContext =>
                await (await ExhaustiveLearningCurveChartService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                    .GetSvgAsync(id, token));
        }

        private static Task<IResult> RocAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RenderExhaustiveAsync("Roc/" + id, httpContext, log, dynamicEnvironment, async dbContext =>
                await (await ExhaustiveRocChartService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                    .GetSvgAsync(id, token));
        }

        private static Task<IResult> PredictedActualAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RenderExhaustiveAsync("PredictedActual/" + id, httpContext, log, dynamicEnvironment,
                async dbContext =>
                    await (await ExhaustivePredictedActualChartService.CreateAsync(dbContext,
                            httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                        .GetSvgAsync(id, token));
        }

        private static Task<IResult> CalibrationComparisonAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RenderExhaustiveAsync("CalibrationComparison/" + id, httpContext, log, dynamicEnvironment,
                async dbContext =>
                    await (await ExhaustiveCalibrationChartService.CreateAsync(dbContext,
                            httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                        .GetSvgAsync(id, token));
        }

        private static Task<IResult> ErrorHistogramAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RenderExhaustiveAsync("ErrorHistogram/" + id, httpContext, log, dynamicEnvironment,
                async dbContext =>
                    await (await ExhaustiveErrorHistogramChartService.CreateAsync(dbContext,
                            httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                        .GetSvgAsync(id, token));
        }

        private static Task<IResult> ConfusionAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RenderExhaustiveAsync("Confusion/" + id, httpContext, log, dynamicEnvironment, async dbContext =>
                await (await ExhaustiveConfusionChartService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                    .GetSvgAsync(id, token));
        }

        private static Task<IResult> SensitivityAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RenderExhaustiveAsync("Sensitivity/" + id, httpContext, log, dynamicEnvironment, async dbContext =>
                await (await ExhaustiveSensitivityChartService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                    .GetSvgAsync(id, token));
        }

        private static Task<IResult> TopologyAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RenderExhaustiveAsync("Topology/" + id, httpContext, log, dynamicEnvironment, async dbContext =>
                await (await ExhaustiveTopologyChartService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                    .GetSvgAsync(id, token));
        }

        private static Task<IResult> TopologyWithLiveRelevanceAsync(int id, TopologyLiveRelevanceRequest request,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RenderExhaustiveAsync("Topology/" + id, httpContext, log, dynamicEnvironment, async dbContext =>
                await (await ExhaustiveTopologyChartService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                    .GetSvgAsync(id, request.NodeRelevance, token));
        }

        private static Task<IResult> TrialDiagramAsync(int trialInstanceId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RenderExhaustiveAsync("TrialDiagram/" + trialInstanceId, httpContext, log, dynamicEnvironment,
                async dbContext =>
                    await (await ExhaustiveTopologyChartService.CreateAsync(dbContext,
                            httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                        .GetTrialSvgAsync(trialInstanceId, token));
        }

        private static Task<IResult> VariableHistogramAsync(int id, int variableId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RenderExhaustiveAsync("VariableHistogram/" + id + "/" + variableId, httpContext, log,
                dynamicEnvironment,
                async dbContext =>
                    await (await ExhaustiveVariableHistogramChartService.CreateAsync(dbContext,
                            httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                        .GetSvgAsync(id, variableId, token));
        }

        private static Task<IResult> PrescriptionHistogramAsync(int id, int promotedTrialInstanceVariableId,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RenderExhaustiveAsync(
                "PrescriptionHistogram/" + id + "/" + promotedTrialInstanceVariableId, httpContext, log,
                dynamicEnvironment,
                async dbContext =>
                    await (await ExhaustivePrescriptionHistogramChartService.CreateAsync(dbContext,
                            httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                        .GetSvgAsync(id, promotedTrialInstanceVariableId, token));
        }

        private static Task<IResult> PrescriptionSummaryAsync(int id, int promotedTrialInstanceVariableId,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RenderExhaustiveAsync(
                "PrescriptionSummary/" + id + "/" + promotedTrialInstanceVariableId, httpContext, log,
                dynamicEnvironment,
                async dbContext =>
                    await (await ExhaustivePrescriptionHistogramChartService.CreateAsync(dbContext,
                            httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                        .GetSummarySvgAsync(id, promotedTrialInstanceVariableId, token));
        }

        private static void MapPerformance(RouteGroupBuilder group, string name, ExhaustivePerformanceChart chart)
        {
            group.MapGet(name + "/{id:int}",
                    (int id, double? threshold, double? falseNegativeCost, double? falsePositiveCost,
                            int? variableId, string variableName,
                            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
                            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
                            CancellationToken token) =>
                        PerformanceAsync(chart, name, id, threshold, falseNegativeCost, falsePositiveCost,
                            variableId, variableName,
                            httpContext, log, dynamicEnvironment, stringLocalizerFactory, serviceChangeBus, token))
                .Produces<string>(StatusCodes.Status200OK, "image/svg+xml")
                .WithName("ExhaustiveChart" + name + "Get");
        }

        private static Task<IResult> PerformanceAsync(ExhaustivePerformanceChart chart, string name, int id,
            double? threshold, double? falseNegativeCost, double? falsePositiveCost, int? variableId,
            string variableName, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var effective = threshold ?? ConfiguredThreshold(dynamicEnvironment);

            return RenderExhaustiveAsync(name + "/" + id, httpContext, log, dynamicEnvironment, async dbContext =>
                await (await ExhaustivePerformanceChartService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                    .GetSvgAsync(id, chart, effective, falseNegativeCost, falsePositiveCost, variableId, variableName,
                        token));
        }

        private static Task<IResult> PromotionHistoryAsync(int id, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RenderExhaustiveAsync("PromotionHistory/" + id, httpContext, log, dynamicEnvironment,
                async dbContext =>
                    await (await ExhaustivePromotionHistoryChartService.CreateAsync(dbContext,
                            httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                        .GetSvgAsync(id, token));
        }

        private static double? ConfiguredThreshold(DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            return double.TryParse(dynamicEnvironment.AppSettings(ThresholdSetting), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }

        private static Task<IResult> VariableClassesAsync(int id, int variableId, bool? counts, bool? large,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RunStatisticsAsync("VariableClasses/" + id + "/" + variableId, httpContext, log,
                dynamicEnvironment,
                async dbContext => Svg(httpContext, await (await ExhaustiveStatisticsChartService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                    .GetVariableClassesSvgAsync(id, variableId, counts == true, large == true, token)));
        }

        private static Task<IResult> MonteCarloAsync(int id, int variableId, bool? counts, bool? large,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RunStatisticsAsync("MonteCarlo/" + id + "/" + variableId, httpContext, log, dynamicEnvironment,
                async dbContext => Svg(httpContext, await (await ExhaustiveStatisticsChartService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                    .GetMonteCarloSvgAsync(id, variableId, counts == true, large == true, token)));
        }

        private static Task<IResult> MonteCarloSummaryAsync(int id, int variableId, bool? large,
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RunStatisticsAsync("MonteCarloSummary/" + id + "/" + variableId, httpContext, log,
                dynamicEnvironment,
                async dbContext => Svg(httpContext, await (await ExhaustiveStatisticsChartService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                    .GetMonteCarloSummarySvgAsync(id, variableId, large == true, token)));
        }

        private static Task<IResult> GridAsync(int id, string view, bool? counts, HttpContext httpContext,
            ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            return RunStatisticsAsync("Grid/" + id, httpContext, log, dynamicEnvironment, async dbContext =>
            {
                var items = await (await ExhaustiveStatisticsChartService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token))
                    .GetGridAsync(id, view ?? ExhaustiveStatisticsChartService.ClassesView, counts == true, token);

                Harden(httpContext);

                return TypedResults.Json(items);
            });
        }

        private static Task<IResult> AnalyticsModelDiagramAsync(int versionId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RenderAnalyticsAsync("ModelDiagram/" + versionId, httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus, token,
                (service, t) => service.GetModelDiagramSvgAsync(versionId, t));
        }

        private static Task<IResult> AnalyticsCalibrationAsync(int versionId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RenderAnalyticsAsync("Calibration/" + versionId, httpContext, log, dynamicEnvironment,
                stringLocalizerFactory, serviceChangeBus, token,
                (service, t) => service.GetCalibrationSvgAsync(versionId, t));
        }

        private static Task<IResult> AnalyticsRocAsync(int versionId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            return RenderAnalyticsAsync("Roc/" + versionId, httpContext, log, dynamicEnvironment,
                stringLocalizerFactory,
                serviceChangeBus, token, (service, t) => service.GetRocSvgAsync(versionId, t));
        }

        private static Task<IResult> RenderExhaustiveAsync(string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, Func<DbContext, Task<string>> render)
        {
            return RunAsync(ExhaustiveBase, route, httpContext, log, dynamicEnvironment,
                async dbContext => Svg(httpContext, await render(dbContext)));
        }

        private static Task<IResult> RunStatisticsAsync(string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, Func<DbContext, Task<IResult>> render)
        {
            return RunAsync(StatisticsBase, route, httpContext, log, dynamicEnvironment, render);
        }

        private static Task<IResult> RenderAnalyticsAsync(string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token,
            Func<AdvancedAnalyticsChartService, CancellationToken, Task<string>> render)
        {
            return AskJoobyAnalyticsEndpointRunner.RunAsync("GET", $"{AnalyticsBase}/{route}", httpContext, log,
                dynamicEnvironment, async dbContext =>
                {
                    var service = await AdvancedAnalyticsChartService.CreateAsync(dbContext,
                        httpContext.User.Identity?.Name, log, stringLocalizerFactory, serviceChangeBus, token);

                    var svg = await render(service, token);

                    return Svg(httpContext, svg);
                });
        }

        internal static IResult Svg(HttpContext httpContext, string svg)
        {
            Harden(httpContext);

            return TypedResults.Content(svg, SvgContentType, Encoding.UTF8);
        }

        private static void Harden(HttpContext httpContext)
        {
            httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
            httpContext.Response.Headers["Content-Security-Policy"] = "default-src 'none'";
            httpContext.Response.Headers["Cache-Control"] = "no-store";
        }

        private static async Task<IResult> RunAsync(string prefix, string route, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, Func<DbContext, Task<IResult>> render)
        {
            var user = httpContext.User.Identity?.Name;
            var method = httpContext.Request.Method;

            if (log.IsDebugEnabled)
            {
                log.Debug($"{method} {prefix}/{route}: entry user={user}");
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                return await render(dbContext);
            }
            catch (ServiceException e) when (e.Code is "NotAuthenticated" or "PermissionDenied")
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{method} {prefix}/{route}: 403 ({e.Code}) user={user}");
                }

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"{method} {prefix}/{route}: client cancelled user={user}");
                }

                throw;
            }
            catch (Exception e)
            {
                log.Error($"{method} {prefix}/{route}: 500 user={user}", e);

                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        public sealed record TopologyLiveRelevanceRequest(List<List<double>> NodeRelevance);
    }
}