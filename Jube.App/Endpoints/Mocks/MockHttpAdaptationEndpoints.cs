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
using Jube.HttpAdaptationProtocol;
using Jube.HttpAdaptationProtocol.Calibration;
using Jube.HttpAdaptationProtocol.Contribution;
using Jube.HttpAdaptationProtocol.Journey;
using Jube.HttpAdaptationProtocol.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Jube.App.Endpoints.Mocks
{
    // A series of unauthenticated mock HTTP Adaptation endpoints, standing in for the R Plumber
    // reference endpoints, so that an EntityAnalysisModelHttpAdaptation can be pointed at each
    // route below to comprehensively exercise every shape the Jube HTTP Adaptation Protocol v1.1
    // permits - from the bare-double floor of the contract through to a fully calibrated,
    // attributed, and validated Adaptation object - without an R sandbox. Unauthenticated for the
    // same reason as the mock RSA MFA endpoint: it is a stand-in for an unauthenticated third-party
    // model recall endpoint, not a Jube API surface. Routes, verbs, status codes, content types and
    // bodies are frozen from the legacy MockHttpAdaptationController.
    //
    // Every response is written with the default (PascalCase) contract resolver rather than
    // Jube.App's own camelCase API convention, because these bodies must match the wire protocol
    // exactly - they emulate what an external Plumber endpoint sends, not what Jube's API returns.
    public static class MockHttpAdaptationEndpoints
    {
        private static readonly JsonSerializerSettings wireSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        // The index listing was serialised by MVC's Newtonsoft formatter with Jube's camelCase resolver.
        private static readonly JsonSerializerSettings apiSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
        };

        public static void MapMockHttpAdaptationEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/MockHttpAdaptation").WithTags("MockHttpAdaptation").AllowAnonymous();

            group.MapGet("", Index)
                .Produces<IEnumerable<string>>()
                .WithName("MockHttpAdaptationIndexGet");

            group.MapPost("BareNumber", BareNumber)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationBareNumberPost");

            group.MapPost("BareNumberArrayWrapped", BareNumberArrayWrapped)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationBareNumberArrayWrappedPost");

            group.MapPost("Minimal", Minimal)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationMinimalPost");

            group.MapPost("Glm", Glm)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationGlmPost");

            group.MapPost("RandomForestCalibrated", RandomForestCalibrated)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationRandomForestCalibratedPost");

            group.MapPost("RandomForestUncalibrated", RandomForestUncalibrated)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationRandomForestUncalibratedPost");

            group.MapPost("C5", C5)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationC5Post");

            group.MapPost("XgBoost", XgBoost)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationXgBoostPost");

            group.MapPost("SvmLinear", SvmLinear)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationSvmLinearPost");

            group.MapPost("SvmRadial", SvmRadial)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationSvmRadialPost");

            group.MapPost("SvmDecisionValueMode", SvmDecisionValueMode)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationSvmDecisionValueModePost");

            group.MapPost("BayesianNetworkBootstrapStrength", BayesianNetworkBootstrapStrength)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationBayesianNetworkBootstrapStrengthPost");

            group.MapPost("BayesianNetworkArcStrength", BayesianNetworkArcStrength)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationBayesianNetworkArcStrengthPost");

            group.MapPost("NeuralNetwork", NeuralNetwork)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationNeuralNetworkPost");

            group.MapPost("ExpertRule", ExpertRule)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationExpertRulePost");

            group.MapPost("StaleCalibration", StaleCalibration)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationStaleCalibrationPost");

            group.MapPost("Suppressed", Suppressed)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationSuppressedPost");

            group.MapPost("LegacyError", LegacyError)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationLegacyErrorPost");

            group.MapPost("MalformedJson", MalformedJson)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationMalformedJsonPost");

            group.MapPost("EmptyBody", EmptyBody)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationEmptyBodyPost");

            group.MapPost("ServerError", ServerError)
                .Produces<string>(contentType: "application/json")
                .WithName("MockHttpAdaptationServerErrorPost");
        }

        private static IResult Json(object value)
        {
            return Results.Content(JsonConvert.SerializeObject(value, apiSettings), "application/json; charset=utf-8");
        }

        private static IResult Index()
        {
            return Json(new[]
            {
                "BareNumber - rule 1: a bare JSON number is a legal whole-body response, forever.",
                "BareNumberArrayWrapped - legacy Plumber [x] array wrapping, still legal.",
                "Minimal - {\"Value\":...} and nothing else: the floor of the contract.",
                "Glm - Native-calibrated probability, exact Coefficient/Relative contribution, ExpectedPositiveRate.",
                "RandomForestCalibrated - Isotonic-calibrated probability with a validation artifact.",
                "RandomForestUncalibrated - no artifact: raw VoteFraction, Calibrated false, not suppressed.",
                "C5 - Platt-calibrated probability with a fired-path Journey (Tree conditions).",
                "XgBoost - Native binary:logistic; Value in Probability, weights in LogOdds.",
                "SvmLinear - Platt-scaled probability with exact DecisionFunction contributions.",
                "SvmRadial - Platt-scaled probability, no contribution, Calibrated false (no artifact shipped).",
                "SvmDecisionValueMode - opt-in decision-value mode: Value itself in DecisionFunction space.",
                "BayesianNetworkBootstrapStrength - primary method: bootstrap arc strength and direction.",
                "BayesianNetworkArcStrength - opt-in secondary method: BIC score delta on arc removal.",
                "NeuralNetwork - sigmoid output, ConnectionWeight contribution, honestly uncalibrated.",
                "ExpertRule - a validated rule reporting a calibrated probability, Tree-shaped Journey.",
                "StaleCalibration - Method/Space would justify Calibrated, but Model.Validation.Stale forces false.",
                "Suppressed - Error non-null, Value explicitly null: must not be fed downstream.",
                "LegacyError - v1.0 shape {Value:0.0,Error:...}: parseable, but now treated as suppressed.",
                "MalformedJson - not parseable as a bare number or an Adaptation object.",
                "EmptyBody - a 200 with no content at all.", "ServerError - a 500 with a non-protocol plain-text body."
            });
        }

        // Rule 1: a bare JSON number remains a legal whole-body response, forever.
        private static IResult BareNumber()
        {
            return Results.Content("0.91", "application/json");
        }

        // Legacy Plumber array-wrapped scalar, e.g. jsonlite's default vector serialisation.
        // Still a legal bare number once the wrapping brackets are stripped.
        private static IResult BareNumberArrayWrapped()
        {
            return Results.Content("[0.07]", "application/json");
        }

        // Adaptation.Value remains the only required member of an adaptation.
        private static IResult Minimal()
        {
            return Results.Content("{\"Value\":0.07}", "application/json");
        }

        private static IResult Glm()
        {
            var adaptation = new Adaptation
            {
                Value = 0.0091,
                Narrative = "Logistic regression score of 0.0091 against a threshold of 0.05.",
                HumanLabel = "GLM score",
                ProtocolVersion = ProtocolConstants.CurrentProtocolVersion,
                Model = new ModelDescriptor
                {
                    Name = "consumer-fraud-glm",
                    Family = ProtocolConstants.Family.Glm,
                    Version = "3.4.0",
                    ArtifactHash = "sha256:1f3d9c6a",
                    TrainedDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                    FeatureCount = 11,
                    Validation = InDateValidation()
                },
                Result = new ResultDescriptor
                {
                    Threshold = 0.05,
                    Activated = false,
                    ExpectedPositiveRate = 0.031
                },
                Calibration = new CalibrationDescriptor
                {
                    Space = ProtocolConstants.ValueSpace.Probability,
                    Calibrated = true,
                    Method = ProtocolConstants.CalibrationMethod.Native,
                    ValidatedDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                    Sample = 184320,
                    Brier = 0.0412,
                    Intercept = -0.0113,
                    Slope = 0.9847,
                    Band = new List<CalibrationBand>
                    {
                        new CalibrationBand
                        {
                            Lower = 0.00,
                            Upper = 0.05,
                            Expected = 0.021,
                            Observed = 0.019,
                            Count = 152340
                        },
                        new CalibrationBand
                        {
                            Lower = 0.05,
                            Upper = 1.00,
                            Expected = 0.291,
                            Observed = 0.286,
                            Count = 4192
                        }
                    }
                },
                // BaseValue + sum(Weight) reconstructs Value exactly: Exact is verified, not asserted.
                Contribution = new ContributionSet
                {
                    Space = ProtocolConstants.ContributionSpace.Relative,
                    Method = ProtocolConstants.ContributionMethod.Coefficient,
                    Exact = true,
                    BaseValue = -0.0409,
                    Items = new List<ContributionItem>
                    {
                        new ContributionItem
                        {
                            Name = "Payload.SettlementAmount",
                            Weight = 0.0312,
                            Significance = 0.0021,
                            Source = ProtocolConstants.Source.Payload,
                            HumanLabel = "Settlement amount"
                        },
                        new ContributionItem
                        {
                            Name = "Abstraction.ResponseCodeEqual0Volume",
                            Weight = 0.0188,
                            Significance = 0.0140,
                            Source = ProtocolConstants.Source.Abstraction,
                            HumanLabel = "Approved response volume"
                        }
                    }
                }
            };

            return Results.Content(JsonConvert.SerializeObject(adaptation, wireSettings), "application/json");
        }

        private static IResult RandomForestCalibrated()
        {
            var adaptation = new Adaptation
            {
                Value = 0.62,
                ProtocolVersion = ProtocolConstants.CurrentProtocolVersion,
                Model = new ModelDescriptor
                {
                    Name = "consumer-fraud-rf",
                    Family = ProtocolConstants.Family.RandomForest,
                    ArtifactHash = "sha256:7ac41bd0",
                    TrainedDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    Validation = InDateValidation()
                },
                Result = new ResultDescriptor
                {
                    Threshold = 0.5,
                    Activated = true,
                    ExpectedPositiveRate = 0.612
                },
                Calibration = new CalibrationDescriptor
                {
                    Space = ProtocolConstants.ValueSpace.Probability,
                    Calibrated = true,
                    Method = ProtocolConstants.CalibrationMethod.Isotonic,
                    ValidatedDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                    Sample = 184320,
                    Brier = 0.0487,
                    Intercept = -0.0201,
                    Slope = 0.9612,
                    Band = new List<CalibrationBand>
                    {
                        new CalibrationBand
                        {
                            Lower = 0.40,
                            Upper = 1.00,
                            Expected = 0.612,
                            Observed = 0.598,
                            Count = 1103
                        }
                    }
                }
            };

            return Results.Content(JsonConvert.SerializeObject(adaptation, wireSettings), "application/json");
        }

        // No calibration artifact is shipped: the raw vote fraction is emitted honestly, declared
        // as such via Calibration.Space, rather than silently passed off as a probability. Error is
        // deliberately left null here - a non-null Error would suppress Value under rule C3, which
        // would contradict "emit the raw vote fraction" for what is a normal, ranking-only response.
        private static IResult RandomForestUncalibrated()
        {
            var adaptation = new Adaptation
            {
                Value = 0.71,
                ProtocolVersion = ProtocolConstants.CurrentProtocolVersion,
                Model = new ModelDescriptor
                {
                    Name = "consumer-fraud-rf-unvalidated",
                    Family = ProtocolConstants.Family.RandomForest,
                    ArtifactHash = "sha256:9b02e3f1",
                    TrainedDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                Result = new ResultDescriptor
                {
                    Threshold = 0.5,
                    Activated = true
                },
                Calibration = new CalibrationDescriptor
                {
                    Space = ProtocolConstants.ValueSpace.VoteFraction,
                    Calibrated = false,
                    Method = ProtocolConstants.CalibrationMethod.None
                }
            };

            return Results.Content(JsonConvert.SerializeObject(adaptation, wireSettings), "application/json");
        }

        // A single fired path through a decision tree: sufficient to transcribe into a Jube coder
        // rule without ever touching HTTP serialisation again.
        private static IResult C5()
        {
            var adaptation = new Adaptation
            {
                Value = 0.44,
                ProtocolVersion = ProtocolConstants.CurrentProtocolVersion,
                Model = new ModelDescriptor
                {
                    Name = "consumer-fraud-c5",
                    Family = ProtocolConstants.Family.C5,
                    ArtifactHash = "sha256:22cba6de",
                    TrainedDate = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
                    Validation = InDateValidation()
                },
                Calibration = new CalibrationDescriptor
                {
                    Space = ProtocolConstants.ValueSpace.Probability,
                    Calibrated = true,
                    Method = ProtocolConstants.CalibrationMethod.Platt,
                    ValidatedDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                    Sample = 92160,
                    Brier = 0.0533,
                    Intercept = 0.0087,
                    Slope = 1.0421
                },
                Journey = new JourneyDescriptor
                {
                    Path = new List<JourneyNode>
                    {
                        new JourneyNode
                        {
                            Feature = "Payload.SettlementAmount",
                            Operator = ">",
                            Threshold = 5000,
                            Source = ProtocolConstants.Source.Payload,
                            HumanLabel = "Settlement amount above 5000"
                        },
                        new JourneyNode
                        {
                            Feature = "Abstraction.CountryRiskEqualHigh",
                            Operator = "==",
                            ThresholdCategory = "High",
                            Source = ProtocolConstants.Source.Abstraction,
                            HumanLabel = "Country risk is High"
                        }
                    }
                }
            };

            return Results.Content(JsonConvert.SerializeObject(adaptation, wireSettings), "application/json");
        }

        // Value is a sigmoid of summed margins in Probability space; the margins themselves - the
        // Contribution weights - remain in LogOdds. Calibration.Space and Contribution.Space are
        // deliberately different members: this is the case the README calls out as the most likely
        // misreading of the protocol.
        private static IResult XgBoost()
        {
            var adaptation = new Adaptation
            {
                Value = 0.084,
                ProtocolVersion = ProtocolConstants.CurrentProtocolVersion,
                Model = new ModelDescriptor
                {
                    Name = "consumer-fraud-xgb",
                    Family = ProtocolConstants.Family.XgBoost,
                    ArtifactHash = "sha256:4dd018aa",
                    TrainedDate = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
                    Validation = InDateValidation()
                },
                Result = new ResultDescriptor
                {
                    Threshold = 0.1,
                    Activated = false,
                    ExpectedPositiveRate = 0.077
                },
                Calibration = new CalibrationDescriptor
                {
                    Space = ProtocolConstants.ValueSpace.Probability,
                    Calibrated = true,
                    Method = ProtocolConstants.CalibrationMethod.Native,
                    ValidatedDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                    Sample = 184320,
                    Brier = 0.0398,
                    Intercept = 0.0021,
                    Slope = 1.0110
                },
                Contribution = new ContributionSet
                {
                    Space = ProtocolConstants.ValueSpace.LogOdds,
                    Method = ProtocolConstants.ContributionMethod.Coefficient,
                    Exact = true,
                    BaseValue = -2.1102,
                    Items = new List<ContributionItem>
                    {
                        new ContributionItem
                        {
                            Name = "TtlCounter.TransactionCount1Hour",
                            Weight = 0.611,
                            Source = ProtocolConstants.Source.TtlCounter,
                            HumanLabel = "Transactions in the last hour"
                        }
                    }
                }
            };

            return Results.Content(JsonConvert.SerializeObject(adaptation, wireSettings), "application/json");
        }

        // e1071 fits the sigmoid at train time - that is a Platt scaling and is declared as one,
        // not as Native. Contribution is exact in DecisionFunction space (its own base + weights),
        // which is not the same reconstruction as Value, since Value has since been Platt-scaled
        // into Probability space.
        private static IResult SvmLinear()
        {
            var adaptation = new Adaptation
            {
                Value = 0.53,
                ProtocolVersion = ProtocolConstants.CurrentProtocolVersion,
                Model = new ModelDescriptor
                {
                    Name = "consumer-fraud-svm-linear",
                    Family = ProtocolConstants.Family.Svm,
                    ArtifactHash = "sha256:c001beef",
                    TrainedDate = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc),
                    Validation = InDateValidation()
                },
                Calibration = new CalibrationDescriptor
                {
                    Space = ProtocolConstants.ValueSpace.Probability,
                    Calibrated = true,
                    Method = ProtocolConstants.CalibrationMethod.Platt,
                    ValidatedDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                    Sample = 61230,
                    Brier = 0.0455,
                    Intercept = -0.0056,
                    Slope = 0.9911
                },
                Contribution = new ContributionSet
                {
                    Space = ProtocolConstants.ValueSpace.DecisionFunction,
                    Method = ProtocolConstants.ContributionMethod.Coefficient,
                    Exact = true,
                    BaseValue = 0.02,
                    Items = new List<ContributionItem>
                    {
                        new ContributionItem
                        {
                            Name = "Payload.SettlementAmount",
                            Weight = 0.11,
                            Source = ProtocolConstants.Source.Payload
                        },
                        new ContributionItem
                        {
                            Name = "Dictionary.MerchantCategoryHighRisk",
                            Weight = -0.08,
                            Source = ProtocolConstants.Source.Dictionary
                        }
                    }
                }
            };

            return Results.Content(JsonConvert.SerializeObject(adaptation, wireSettings), "application/json");
        }

        // The bundle's worked example of honest minimum under calibration as well as attribution:
        // no contribution shipped, and no validation artifact shipped for this kernel either.
        private static IResult SvmRadial()
        {
            var adaptation = new Adaptation
            {
                Value = 0.37,
                ProtocolVersion = ProtocolConstants.CurrentProtocolVersion,
                Model = new ModelDescriptor
                {
                    Name = "consumer-fraud-svm-radial",
                    Family = ProtocolConstants.Family.Svm,
                    ArtifactHash = "sha256:d4a92210",
                    TrainedDate = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc)
                },
                Calibration = new CalibrationDescriptor
                {
                    Space = ProtocolConstants.ValueSpace.Probability,
                    Calibrated = false,
                    Method = ProtocolConstants.CalibrationMethod.Platt
                }
            };

            return Results.Content(JsonConvert.SerializeObject(adaptation, wireSettings), "application/json");
        }

        // Opt-in decision-value mode (JUBE_SVM_VALUE_MODE=decision): Value itself is a raw decision
        // value, legitimate only for a rank-based Activation Rule threshold, never for probability
        // narration - Calibrated is always false in this mode.
        private static IResult SvmDecisionValueMode()
        {
            var adaptation = new Adaptation
            {
                Value = 1.842,
                ProtocolVersion = ProtocolConstants.CurrentProtocolVersion,
                Model = new ModelDescriptor
                {
                    Name = "consumer-fraud-svm-linear",
                    Family = ProtocolConstants.Family.Svm,
                    ArtifactHash = "sha256:c001beef",
                    TrainedDate = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc)
                },
                Result = new ResultDescriptor
                {
                    Threshold = 1.5,
                    Activated = true
                },
                Calibration = new CalibrationDescriptor
                {
                    Space = ProtocolConstants.ValueSpace.DecisionFunction,
                    Calibrated = false,
                    Method = ProtocolConstants.CalibrationMethod.None
                }
            };

            return Results.Content(JsonConvert.SerializeObject(adaptation, wireSettings), "application/json");
        }

        // The governance sentence, generated: "X -> Y present in NN% of bootstrap samples."
        // Direction carries the proportion of resamples in which the arc ran in the emitted
        // direction - the part of a learned DAG a validator will challenge first.
        private static IResult BayesianNetworkBootstrapStrength()
        {
            var adaptation = new Adaptation
            {
                Value = 0.19,
                ProtocolVersion = ProtocolConstants.CurrentProtocolVersion,
                Model = new ModelDescriptor
                {
                    Name = "consumer-fraud-bn",
                    Family = ProtocolConstants.Family.BayesianNetwork,
                    ArtifactHash = "sha256:6f10ab22",
                    TrainedDate = new DateTime(2026, 1, 25, 0, 0, 0, DateTimeKind.Utc),
                    Validation = InDateValidation(),
                    BootstrapReplicates = 500,
                    StructureLearning = ProtocolConstants.StructureLearning.HillClimbing,
                    WhitelistedArcs = 2,
                    BlacklistedArcs = 1
                },
                Calibration = new CalibrationDescriptor
                {
                    Space = ProtocolConstants.ValueSpace.Probability,
                    Calibrated = true,
                    Method = ProtocolConstants.CalibrationMethod.Native,
                    ValidatedDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                    Sample = 71440
                },
                Contribution = new ContributionSet
                {
                    Space = ProtocolConstants.ContributionSpace.Relative,
                    Method = ProtocolConstants.ContributionMethod.BootstrapStrength,
                    Exact = false,
                    Items = new List<ContributionItem>
                    {
                        new ContributionItem
                        {
                            Name = "Abstraction.MerchantCategoryChanged -> Abstraction.FraudLikely",
                            Weight = 0.94,
                            Direction = 0.88,
                            Source = ProtocolConstants.Source.Abstraction,
                            HumanLabel =
                                "Abstraction.MerchantCategoryChanged -> Abstraction.FraudLikely present in 94% of bootstrap samples"
                        }
                    }
                },
                // Bayesian networks have no fired path in the Tree sense: null is correct, not a degradation.
                Journey = null
            };

            return Results.Content(JsonConvert.SerializeObject(adaptation, wireSettings), "application/json");
        }

        // The retained, opt-in secondary method (JUBE_BN_STRENGTH_METHOD=bic): a score delta on arc
        // removal, defensible but not the bootstrap-resampling sentence governance actually wants.
        private static IResult BayesianNetworkArcStrength()
        {
            var adaptation = new Adaptation
            {
                Value = 0.19,
                ProtocolVersion = ProtocolConstants.CurrentProtocolVersion,
                Model = new ModelDescriptor
                {
                    Name = "consumer-fraud-bn",
                    Family = ProtocolConstants.Family.BayesianNetwork,
                    ArtifactHash = "sha256:6f10ab22",
                    TrainedDate = new DateTime(2026, 1, 25, 0, 0, 0, DateTimeKind.Utc),
                    StructureLearning = ProtocolConstants.StructureLearning.HillClimbing
                },
                Contribution = new ContributionSet
                {
                    Space = ProtocolConstants.ContributionSpace.Relative,
                    Method = ProtocolConstants.ContributionMethod.ArcStrength,
                    Exact = false,
                    Items = new List<ContributionItem>
                    {
                        new ContributionItem
                        {
                            Name = "Abstraction.MerchantCategoryChanged -> Abstraction.FraudLikely",
                            Weight = 6.21,
                            Source = ProtocolConstants.Source.Abstraction
                        }
                    }
                }
            };

            return Results.Content(JsonConvert.SerializeObject(adaptation, wireSettings), "application/json");
        }

        // The protocol permits NeuralNetwork and does not recommend it: no Journey, Contribution is
        // rank-only (ConnectionWeight, never Exact), and here left honestly uncalibrated.
        private static IResult NeuralNetwork()
        {
            var adaptation = new Adaptation
            {
                Value = 0.66,
                ProtocolVersion = ProtocolConstants.CurrentProtocolVersion,
                Model = new ModelDescriptor
                {
                    Name = "consumer-fraud-nn",
                    Family = ProtocolConstants.Family.NeuralNetwork,
                    ArtifactHash = "sha256:aa4402f1",
                    TrainedDate = new DateTime(2026, 3, 30, 0, 0, 0, DateTimeKind.Utc),
                    HiddenLayers = 2,
                    ProcessingElements = 24
                },
                Calibration = new CalibrationDescriptor
                {
                    Space = ProtocolConstants.ValueSpace.Probability,
                    Calibrated = false,
                    Method = ProtocolConstants.CalibrationMethod.Native
                },
                Contribution = new ContributionSet
                {
                    Space = ProtocolConstants.ContributionSpace.Relative,
                    Method = ProtocolConstants.ContributionMethod.ConnectionWeight,
                    Exact = false,
                    Items = new List<ContributionItem>
                    {
                        new ContributionItem
                        {
                            Name = "Payload.SettlementAmount",
                            Weight = 0.41,
                            Source = ProtocolConstants.Source.Payload
                        }
                    }
                },
                Journey = null
            };

            return Results.Content(JsonConvert.SerializeObject(adaptation, wireSettings), "application/json");
        }

        // A rule is a model: the protocol is agnostic as to whether it was fitted or authored by a
        // subject-matter expert, and asks the same of both - declare the space, declare the
        // calibration, and do not narrate probabilistically what has not been validated.
        private static IResult ExpertRule()
        {
            var adaptation = new Adaptation
            {
                Value = 0.80,
                ProtocolVersion = ProtocolConstants.CurrentProtocolVersion,
                Model = new ModelDescriptor
                {
                    Name = "high-value-first-transaction-rule",
                    Family = ProtocolConstants.Family.ExpertRule,
                    Version = "1.2",
                    Validation = InDateValidation()
                },
                Calibration = new CalibrationDescriptor
                {
                    Space = ProtocolConstants.ValueSpace.Probability,
                    Calibrated = true,
                    Method = ProtocolConstants.CalibrationMethod.Native,
                    ValidatedDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                    Sample = 4210
                },
                Contribution = new ContributionSet
                {
                    Space = ProtocolConstants.ContributionSpace.Relative,
                    Method = ProtocolConstants.ContributionMethod.Coefficient,
                    Exact = true,
                    BaseValue = 0.2,
                    Items = new List<ContributionItem>
                    {
                        new ContributionItem
                        {
                            Name = "Payload.AccountAgeDays",
                            Weight = 0.6,
                            Source = ProtocolConstants.Source.Payload,
                            HumanLabel = "Account age under 24 hours"
                        }
                    }
                },
                Journey = new JourneyDescriptor
                {
                    Path = new List<JourneyNode>
                    {
                        new JourneyNode
                        {
                            Feature = "Payload.AccountAgeDays",
                            Operator = "<",
                            Threshold = 1,
                            Source = ProtocolConstants.Source.Payload,
                            HumanLabel = "Account age under 24 hours"
                        },
                        new JourneyNode
                        {
                            Feature = "Payload.SettlementAmount",
                            Operator = ">",
                            Threshold = 1000,
                            Source = ProtocolConstants.Source.Payload,
                            HumanLabel = "Settlement amount above 1000"
                        }
                    }
                }
            };

            return Results.Content(JsonConvert.SerializeObject(adaptation, wireSettings), "application/json");
        }

        // Model.Validation is gated on NextReviewDate, not on the discrimination/calibration
        // statistics themselves: Method and Space here would otherwise justify Calibrated true, but
        // Stale forces it false. A stale model may not claim calibration.
        private static IResult StaleCalibration()
        {
            var adaptation = new Adaptation
            {
                Value = 0.58,
                ProtocolVersion = ProtocolConstants.CurrentProtocolVersion,
                Model = new ModelDescriptor
                {
                    Name = "consumer-fraud-glm",
                    Family = ProtocolConstants.Family.Glm,
                    ArtifactHash = "sha256:1f3d9c6a",
                    TrainedDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                    Validation = new ValidationDescriptor
                    {
                        Date = new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                        Sample = 184320,
                        Auc = 0.9114,
                        Gini = 0.8228,
                        Ks = 0.7043,
                        NextReviewDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                        Stale = true
                    }
                },
                Calibration = new CalibrationDescriptor
                {
                    Space = ProtocolConstants.ValueSpace.Probability,
                    Calibrated = false,
                    Method = ProtocolConstants.CalibrationMethod.Isotonic,
                    ValidatedDate = new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                    Sample = 184320
                }
            };

            return Results.Content(JsonConvert.SerializeObject(adaptation, wireSettings), "application/json");
        }

        // Error non-null => Value MUST be null. This is the shape a well-behaved v1.1 endpoint
        // produces on failure: it must not be fed into Abstraction Calculations or Activation Rules.
        private static IResult Suppressed()
        {
            return Results.Content(
                "{\"Value\":null,\"Error\":\"Feature store timed out building Abstraction.CountryRiskEqualHigh.\"}",
                "application/json");
        }

        // v1.0 legacy shape: parseable, but a non-null Error alongside a non-null Value is now
        // defined as suppressed - an error must never masquerade as a score.
        private static IResult LegacyError()
        {
            return Results.Content(
                "{\"Value\":0.0,\"Error\":\"Model artifact failed to load.\"}",
                "application/json");
        }

        // Neither a bare number nor a parseable Adaptation object: exercises the parser's
        // defensive fallback, which archives and suppresses rather than throwing.
        private static IResult MalformedJson()
        {
            return Results.Content("{ this is not valid json", "application/json");
        }

        private static IResult EmptyBody()
        {
            return Results.Content(String.Empty, "application/json", null, StatusCodes.Status200OK);
        }

        // PostAsync does not inspect the status code before reading the body: a 5xx with a
        // non-protocol body must be handled exactly like a malformed 200.
        private static IResult ServerError()
        {
            return Results.Content("Internal Server Error", "text/plain", null,
                StatusCodes.Status500InternalServerError);
        }

        private static ValidationDescriptor InDateValidation()
        {
            return new ValidationDescriptor
            {
                Date = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                Sample = 184320,
                Auc = 0.9114,
                Gini = 0.8228,
                Ks = 0.7043,
                Brier = 0.0412,
                PopulationStabilityIndex = 0.0731,
                NextReviewDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                Stale = false
            };
        }
    }
}