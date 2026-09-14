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

using System.Diagnostics;
using System.Reflection;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript.
    EntityAnalysisModelInlineScriptPropertyAttribute;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class ActivationRuleActivationWatcherGeolocationTests
    {
        private static Context NewContext()
        {
            return new Context
            {
                EntityAnalysisModel = new EntityAnalysisModel(),
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>()
                },
                Log = TestLog.NoOp,
                Stopwatch = Stopwatch.StartNew(),
                LogSampled = true
            };
        }

        private static double InvokePrivateCoordinateMethod(Context context, string methodName)
        {
            var method = typeof(ActivationRuleActivationWatcherExtensions).GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Static);
            method.Should()
                .NotBeNull($"{methodName} must still exist with this exact name for this test to mean anything");
            return (double)method!.Invoke(null, [context])!;
        }

        [Fact]
        public void GetLatitudeReadsFromTheXPathFlaggedWithDataTypeSix()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.Add(
                new EntityAnalysisModelRequestXPath { Name = "Lat", DataTypeId = 6 });
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Lat", 51.5);

            InvokePrivateCoordinateMethod(context, "GetLatitude").Should().Be(51.5);
        }

        [Fact]
        public void GetLongitudeReadsFromTheXPathFlaggedWithDataTypeSeven()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.Add(
                new EntityAnalysisModelRequestXPath { Name = "Lon", DataTypeId = 7 });
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Lon", -0.12);

            InvokePrivateCoordinateMethod(context, "GetLongitude").Should().Be(-0.12);
        }

        [Fact]
        public void GetLongitudeFallsBackToTheInlineScriptPropertyFlaggedLongitudeNotLatitude()
        {
            var context = NewContext();
            var script = new EntityAnalysisModelInlineScript
            {
                Id = 1,
                InlineScriptCode = "Script1",
                EntityAnalysisModelInlineScriptPropertyAttributes =
                {
                    ["LongitudeField"] = new EntityAnalysisModelInlineScriptPropertyAttribute
                    {
                        Name = "LongitudeField",
                        Longitude = true
                    }
                }
            };
            context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineScripts.Add(script);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("LongitudeField", 12.34);

            InvokePrivateCoordinateMethod(context, "GetLongitude").Should().Be(12.34);
        }

        [Fact]
        public void GetLongitudeDoesNotMatchAPropertyFlaggedLatitudeOnly()
        {
            var context = NewContext();
            var script = new EntityAnalysisModelInlineScript
            {
                Id = 1,
                InlineScriptCode = "Script1",
                EntityAnalysisModelInlineScriptPropertyAttributes =
                {
                    ["LatitudeField"] = new EntityAnalysisModelInlineScriptPropertyAttribute
                    {
                        Name = "LatitudeField",
                        Latitude = true
                    }
                }
            };
            context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineScripts.Add(script);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("LatitudeField", 99.0);

            InvokePrivateCoordinateMethod(context, "GetLongitude").Should().Be(0);
        }

        [Fact]
        public void GetLatitudeAndGetLongitudeDefaultToZeroWhenNothingIsConfigured()
        {
            var context = NewContext();

            InvokePrivateCoordinateMethod(context, "GetLatitude").Should().Be(0);
            InvokePrivateCoordinateMethod(context, "GetLongitude").Should().Be(0);
        }
    }
}