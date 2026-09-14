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
using System.Linq;
using FluentAssertions;
using Jube.Service.Observability;
using Jube.Service.Observability.OtlpDispatchCounters;
using Jube.Test.Infrastructure;
using OpenTelemetry;
using Xunit;

namespace Jube.Test.Service.Observability.DispatchTrackingExporter
{
    [Trait("Category", "Unit")]
    [Collection("OtlpDispatchCounters")]
    public sealed class DispatchTrackingExporterTests
    {
        [Fact]
        public void ExportReturnsSuccessAndRecordsItWhenInnerExporterSucceeds()
        {
            var signal = $"unit-test-signal-{Guid.NewGuid():N}";
            var inner = new FakeExporter<FakeItem>(() => ExportResult.Success);
            var exporter = new DispatchTrackingExporter<FakeItem>(inner, signal, TestLog.NoOp);
            var batch = new Batch<FakeItem>(new FakeItem());

            var result = exporter.Export(in batch);

            result.Should().Be(ExportResult.Success);
            inner.CallCount.Should().Be(1);

            var snapshot = OtlpDispatchCounters.TakeSnapshot().Single(s => s.Signal == signal);
            snapshot.Count.Should().Be(1);
            snapshot.SuccessCount.Should().Be(1);
            snapshot.FailureCount.Should().Be(0);
        }

        [Fact]
        public void ExportReturnsFailureAndRecordsItWhenInnerExporterReturnsFailure()
        {
            var signal = $"unit-test-signal-{Guid.NewGuid():N}";
            var inner = new FakeExporter<FakeItem>(() => ExportResult.Failure);
            var exporter = new DispatchTrackingExporter<FakeItem>(inner, signal, TestLog.NoOp);
            var batch = new Batch<FakeItem>(new FakeItem());

            var result = exporter.Export(in batch);

            result.Should().Be(ExportResult.Failure);

            var snapshot = OtlpDispatchCounters.TakeSnapshot().Single(s => s.Signal == signal);
            snapshot.Count.Should().Be(1);
            snapshot.SuccessCount.Should().Be(0);
            snapshot.FailureCount.Should().Be(1);
        }

        [Fact]
        public void ExportNeverThrowsAndIsCountedAsFailureWhenInnerExporterThrows()
        {
            var signal = $"unit-test-signal-{Guid.NewGuid():N}";
            var inner = new FakeExporter<FakeItem>(() => throw new InvalidOperationException("dead backend"));
            var exporter = new DispatchTrackingExporter<FakeItem>(inner, signal, TestLog.NoOp);
            var batch = new Batch<FakeItem>(new FakeItem());

            ExportResult result = default;
            var act = () => result = exporter.Export(in batch);

            act.Should().NotThrow();
            result.Should().Be(ExportResult.Failure);

            var snapshot = OtlpDispatchCounters.TakeSnapshot().Single(s => s.Signal == signal);
            snapshot.Count.Should().Be(1);
            snapshot.FailureCount.Should().Be(1);
        }

        [Fact]
        public void ToStringReturnsTheSignalNameSoSelfDiagnosticsCanAttributeDropsBackToIt()
        {
            var exporter = new DispatchTrackingExporter<FakeItem>(
                new FakeExporter<FakeItem>(() => ExportResult.Success), "traces", TestLog.NoOp);

            exporter.ToString().Should().Be("traces");
        }
    }
}