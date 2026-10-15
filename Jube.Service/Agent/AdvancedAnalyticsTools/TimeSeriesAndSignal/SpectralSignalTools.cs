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

using System.ComponentModel;
using System.Numerics;
using Accord.Math;
using Accord.Math.Transforms;
using Accord.Math.Wavelets;
using Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal
{
    public enum SpectralWindow
    {
        None,
        Hann,
        Hamming,
        Blackman
    }

    public enum SpectralDetrend
    {
        None,
        Mean,
        Linear
    }

    public enum ConvolutionMode
    {
        Full,
        Same,
        Valid
    }

    public enum FrequencyFilterKind
    {
        LowPass,
        HighPass,
        BandPass,
        BandStop
    }

    [AdvancedAnalyticsToolGroup("TimeSeriesAndSignal")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class SpectralSignalTools
    {
        [AdvancedAnalyticsTool("FourierTransformOfSeries")]
        [Description(
            "Discrete Fourier transform of an evenly spaced series, splitting it into sinusoids of different frequencies. Use it to see which cycles a series contains, to filter in the frequency domain or to inspect phase. Any length is accepted. " +
            "Real and Imaginary are the full complex transform (unnormalised: X[k] is the sum of x[t] times exp(-2 pi i k t / n)), so a series of n values returns n coefficients. The one-sided views cover bins 0 to n/2: Frequencies in cycles per unit of samplingIntervalSeconds (k / (n x interval)), Magnitudes = |X[k]|, Amplitudes = the amplitude of the equivalent cosine wave (2|X[k]|/n, or |X[k]|/n for bin 0 and the Nyquist bin), and Phases in radians. " +
            "Bin 0 is the mean level. Requires finite values.")]
        public FourierResult FourierTransformOfSeries(
            [Description("The series in time order, evenly spaced, finite, at least 2 values.")]
            double[] values,
            [Description(
                "Time between samples, above 0, in the unit you want frequencies expressed in (for example 1 for per-sample, 3600 for hourly data in seconds). Default 1.")]
            double samplingIntervalSeconds = 1)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 2);
            TimeSeriesGuard.Positive(samplingIntervalSeconds, nameof(samplingIntervalSeconds));
            var n = v.Length;
            var spectrum = Transform(v);
            var bins = n / 2 + 1;
            var magnitudes = new double[bins];
            var amplitudes = new double[bins];
            var phases = new double[bins];

            for (var k = 0; k < bins; k++)
            {
                magnitudes[k] = spectrum[k].Magnitude;
                var edge = k == 0 || (n % 2 == 0 && k == n / 2);
                amplitudes[k] = (edge ? 1.0 : 2.0) * magnitudes[k] / n;
                phases[k] = spectrum[k].Phase;
            }

            return new FourierResult(spectrum.Select(c => c.Real).ToArray(),
                spectrum.Select(c => c.Imaginary).ToArray(),
                Enumerable.Range(0, bins).Select(k => k / (n * samplingIntervalSeconds)).ToArray(),
                magnitudes, amplitudes, phases, samplingIntervalSeconds, n);
        }

        [AdvancedAnalyticsTool("InverseFourierTransform")]
        [Description(
            "Inverse discrete Fourier transform: rebuilds a series from its complex frequency coefficients, the counterpart of FourierTransformOfSeries. Use it after editing coefficients (for example zeroing high frequencies) to get a filtered series back. " +
            "Real and Imaginary are the coefficient parts, the same length, in the same layout as FourierTransformOfSeries returns (bin 0 first, negative frequencies in the upper half). The result gives the reconstructed values in Real; Imaginary is close to 0 whenever the coefficients had conjugate symmetry.")]
        public InverseFourierResult InverseFourierTransform(
            [Description("Real parts of the frequency coefficients, one per bin, finite.")]
            double[] real,
            [Description("Imaginary parts of the frequency coefficients, the same length as real.")]
            double[] imaginary)
        {
            var re = TimeSeriesGuard.Finite(real, nameof(real), 2);
            var im = TimeSeriesGuard.Finite(imaginary, nameof(imaginary), 2);
            TimeSeriesGuard.SameLength(re, im, nameof(real), nameof(imaginary));
            var data = re.Select((r, i) => new Complex(r, im[i])).ToArray();
            FourierTransform2.FFT(data, FourierTransform.Direction.Backward);

            return new InverseFourierResult(data.Select(c => c.Real).ToArray(),
                data.Select(c => c.Imaginary).ToArray());
        }

        [AdvancedAnalyticsTool("PowerSpectrumOfSeries")]
        [Description(
            "One-sided power spectrum (periodogram) of an evenly spaced series: how much of its variation lies at each frequency. Use it to find cycles, for example a daily rhythm in hourly counts. " +
            "The series is de-trended (Mean by default, or Linear) and optionally windowed with a periodic Hann, Hamming or Blackman window to reduce leakage, then Fourier transformed. Power[k] = 2|X[k]|^2 / (n x window power) for interior bins and |X[k]|^2 / n for bin 0 and the Nyquist bin, so a cosine of amplitude A exactly on a bin has power A^2 n / 2. " +
            "Frequencies are in cycles per unit of samplingIntervalSeconds, Periods are their reciprocals (infinite for bin 0), PeakFrequency, PeakPeriod and PeakPower describe the strongest non-zero frequency and TotalPowerExcludingDc sums the rest. FrequencyResolution is the bin spacing; cycles longer than the series length cannot be resolved. Requires finite values.")]
        public PowerSpectrumResult PowerSpectrumOfSeries(
            [Description("The series in time order, evenly spaced, finite, at least 4 values.")]
            double[] values,
            [Description("Time between samples, above 0. Default 1.")]
            double samplingIntervalSeconds = 1,
            [Description(
                "None, Hann, Hamming or Blackman. Default None; use Hann when cycles do not fit a whole number of times in the series.")]
            SpectralWindow window = SpectralWindow.None,
            [Description("None, Mean or Linear removal before the transform. Default Mean.")]
            SpectralDetrend detrend = SpectralDetrend.Mean)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 4);
            TimeSeriesGuard.Positive(samplingIntervalSeconds, nameof(samplingIntervalSeconds));
            var prepared = Prepare(v, detrend, window, out var windowPower);

            return BuildSpectrum(OneSidedPower(prepared, windowPower), v.Length, samplingIntervalSeconds, 1, window);
        }

        [AdvancedAnalyticsTool("WelchPowerSpectrum")]
        [Description(
            "Welch's averaged power spectrum: the series is cut into overlapping segments, each is de-trended, windowed and transformed, and their power spectra are averaged. This gives a smoother, more reliable spectrum than a single periodogram for noisy series, at the cost of coarser frequency resolution (bin spacing is 1 / (segmentLength x interval)). " +
            "Power has the same scaling as PowerSpectrumOfSeries but for one segment; Segments reports how many were averaged. Use it to find stable cycles in long, noisy series. Requires finite values, segmentLength of at least 8 and no more than the series length.")]
        public PowerSpectrumResult WelchPowerSpectrum(
            [Description("The series in time order, evenly spaced, finite.")]
            double[] values,
            [Description("Number of samples in each segment, at least 8 and at most the series length.")]
            int segmentLength,
            [Description("Share of each segment shared with the next, from 0 up to but not including 1. Default 0.5.")]
            double overlapFraction = 0.5,
            [Description("None, Hann, Hamming or Blackman. Default Hann.")]
            SpectralWindow window = SpectralWindow.Hann,
            [Description("Time between samples, above 0. Default 1.")]
            double samplingIntervalSeconds = 1,
            [Description("None, Mean or Linear removal within each segment. Default Mean.")]
            SpectralDetrend detrend = SpectralDetrend.Mean)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 8);
            TimeSeriesGuard.Positive(samplingIntervalSeconds, nameof(samplingIntervalSeconds));
            TimeSeriesGuard.Unit(overlapFraction, nameof(overlapFraction), true, false);

            if (segmentLength < 8 || segmentLength > v.Length)
            {
                throw new ArgumentException(
                    $"segmentLength must be between 8 and the series length {v.Length} but was {segmentLength}.",
                    nameof(segmentLength));
            }

            var hop = Math.Max(1, (int)Math.Round(segmentLength * (1 - overlapFraction)));
            var sum = new double[segmentLength / 2 + 1];
            var segments = 0;

            for (var start = 0; start + segmentLength <= v.Length; start += hop)
            {
                var segment = new double[segmentLength];
                Array.Copy(v, start, segment, 0, segmentLength);
                var power = OneSidedPower(Prepare(segment, detrend, window, out var windowPower), windowPower);

                for (var k = 0; k < sum.Length; k++)
                {
                    sum[k] += power[k];
                }

                segments++;
            }

            return BuildSpectrum(sum.Select(p => p / segments).ToArray(), segmentLength, samplingIntervalSeconds,
                segments,
                window);
        }

        [AdvancedAnalyticsTool("DominantPeriodsOfSeries")]
        [Description(
            "Finds the strongest repeating cycles in an evenly spaced series from its power spectrum, ranked by power. Use it to answer 'what are the main periodicities?', for example 24 hours and 168 hours in hourly transaction counts. " +
            "Only local peaks of the spectrum (excluding the zero-frequency mean) are reported, strongest first: Periods in units of samplingIntervalSeconds, PeriodsInSamples in samples, Frequencies, Powers and PowerShares (each peak's share of all non-zero-frequency power). Peaks are limited by resolution: periods are 1/(k x resolution), so long periods are coarsely spaced, and a cycle that does not fit a whole number of times leaks into neighbouring bins (use a window to reduce this). " +
            "The result may have fewer than topK entries. Requires finite values.")]
        public DominantPeriodsResult DominantPeriodsOfSeries(
            [Description("The series in time order, evenly spaced, finite, at least 4 values.")]
            double[] values,
            [Description("How many periods to return, at least 1. Default 3.")]
            int topK = 3,
            [Description("Time between samples, above 0, so periods come out in your time unit. Default 1 (samples).")]
            double samplingIntervalSeconds = 1,
            [Description(
                "None, Mean or Linear removal before the transform. Default Linear so a trend does not swamp the spectrum.")]
            SpectralDetrend detrend = SpectralDetrend.Linear,
            [Description("None, Hann, Hamming or Blackman. Default None.")]
            SpectralWindow window = SpectralWindow.None)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 4);
            TimeSeriesGuard.AtLeast(topK, 1, nameof(topK));
            TimeSeriesGuard.Positive(samplingIntervalSeconds, nameof(samplingIntervalSeconds));
            var n = v.Length;
            var power = OneSidedPower(Prepare(v, detrend, window, out var windowPower), windowPower);
            var total = power.Skip(1).Sum();
            var peaks = new List<int>();

            for (var k = 1; k < power.Length; k++)
            {
                var left = k == 1 ? double.NegativeInfinity : power[k - 1];
                var right = k == power.Length - 1 ? double.NegativeInfinity : power[k + 1];

                if (power[k] > left && power[k] >= right && power[k] > 0)
                {
                    peaks.Add(k);
                }
            }

            var top = peaks.OrderByDescending(k => power[k]).ThenBy(k => k).Take(topK).ToArray();

            return new DominantPeriodsResult(
                top.Select(k => n * samplingIntervalSeconds / k).ToArray(),
                top.Select(k => (double)n / k).ToArray(),
                top.Select(k => k / (n * samplingIntervalSeconds)).ToArray(),
                top.Select(k => power[k]).ToArray(),
                top.Select(k => total > 0 ? power[k] / total : double.NaN).ToArray(),
                1 / (n * samplingIntervalSeconds), n);
        }

        [AdvancedAnalyticsTool("SpectralSummary")]
        [Description(
            "Summarises the shape of a series' power spectrum in a few numbers that separate noise-like, cyclic and smooth signals. Use it to classify behaviour: a single strong cycle, broadband noise or slowly varying drift. " +
            "TotalPowerExcludingDc is the variance-like total; SpectralEntropyBits and NormalisedSpectralEntropy (0 to 1) measure how evenly power is spread (near 1 for white noise, near 0 for one pure cycle); SpectralFlatness is geometric over arithmetic mean power (1 for perfectly flat, near 0 for peaky); CentroidFrequency is the power-weighted mean frequency and RolloffFrequency the frequency below which 85 percent of power lies; DominantFrequency, DominantPeriod and DominantPowerShare describe the strongest bin. " +
            "The zero-frequency mean is excluded and the series is mean-removed first. A series with no variation returns NaN for the ratios.")]
        public SpectralSummaryResult SpectralSummary(
            [Description("The series in time order, evenly spaced, finite, at least 4 values.")]
            double[] values,
            [Description("Time between samples, above 0, so frequencies and periods use your unit. Default 1.")]
            double samplingIntervalSeconds = 1,
            [Description("None, Mean or Linear removal before the transform. Default Mean.")]
            SpectralDetrend detrend = SpectralDetrend.Mean)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 4);
            TimeSeriesGuard.Positive(samplingIntervalSeconds, nameof(samplingIntervalSeconds));
            var n = v.Length;

            var power = OneSidedPower(Prepare(v, detrend, SpectralWindow.None, out var windowPower), windowPower)
                .Skip(1)
                .ToArray();

            var total = power.Sum();

            var frequencies = Enumerable.Range(1, power.Length).Select(k => k / (n * samplingIntervalSeconds))
                .ToArray();

            if (total <= 1e-300)
            {
                return new SpectralSummaryResult(0, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN,
                    double.NaN,
                    double.NaN, double.NaN);
            }

            var shares = power.Select(p => p / total).ToArray();
            var entropy = -shares.Where(s => s > 0).Sum(s => s * Math.Log(s, 2));
            var arithmetic = total / power.Length;
            var flatness = power.Any(p => p <= 0) ? 0.0 : Math.Exp(power.Average(Math.Log)) / arithmetic;
            var centroid = frequencies.Select((f, i) => f * shares[i]).Sum();
            var cumulative = 0.0;
            var rolloff = frequencies[^1];

            for (var i = 0; i < shares.Length; i++)
            {
                cumulative += shares[i];

                if (cumulative >= 0.85)
                {
                    rolloff = frequencies[i];
                    break;
                }
            }

            var top = Array.IndexOf(power, power.Max());

            return new SpectralSummaryResult(total, entropy,
                power.Length > 1 ? entropy / Math.Log(power.Length, 2) : double.NaN,
                flatness, centroid, rolloff, frequencies[top], 1 / frequencies[top], shares[top]);
        }

        [AdvancedAnalyticsTool("FisherPeriodicityTest")]
        [Description(
            "Fisher's g test for hidden periodicity: tests the null hypothesis that a series is white noise against the alternative that it contains one dominant sinusoidal cycle. The statistic g is the largest periodogram ordinate divided by the sum of all ordinates (excluding the mean and any Nyquist bin). " +
            "PValue is the exact probability of a g at least as large under white noise (Fisher's formula); Significant is true when it is below alpha. PeakBin is the Fourier bin of the strongest cycle and PeakPeriodInSamples its length in samples (n / bin). TestedFrequencies is the number of ordinates used. " +
            "The test targets a single strong cycle; several equal cycles dilute g. The series is mean-removed first. Needs at least 8 finite values.")]
        public FisherPeriodicityResult FisherPeriodicityTest(
            [Description("The series in time order, evenly spaced, finite, at least 8 values.")]
            double[] values,
            [Description("Significance level for the Significant flag. Default 0.05.")]
            double alpha = 0.05,
            [Description("None, Mean or Linear removal before the transform. Default Mean.")]
            SpectralDetrend detrend = SpectralDetrend.Mean)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 8);
            TimeSeriesGuard.Unit(alpha, nameof(alpha), false, false);
            var n = v.Length;
            var power = OneSidedPower(Prepare(v, detrend, SpectralWindow.None, out var windowPower), windowPower);
            var m = (n - 1) / 2;
            var ordinates = power.Skip(1).Take(m).ToArray();
            var total = ordinates.Sum();

            if (total <= 1e-300)
            {
                return new FisherPeriodicityResult(double.NaN, double.NaN, m, 0, double.NaN, false);
            }

            var g = ordinates.Max() / total;
            var peak = Array.IndexOf(ordinates, ordinates.Max()) + 1;
            var p = 0.0;
            var terms = (int)Math.Floor(1 / g);

            for (var j = 1; j <= terms && j <= m; j++)
            {
                var term = Math.Exp(LogChoose(m, j) + (m - 1) * Math.Log(Math.Max(1 - j * g, 1e-300)));
                p += (j % 2 == 1 ? 1 : -1) * term;
            }

            p = Math.Min(1.0, Math.Max(0.0, p));

            return new FisherPeriodicityResult(g, p, m, peak, (double)n / peak, p < alpha);
        }

        [AdvancedAnalyticsTool("LinearConvolution")]
        [Description(
            "Linear convolution of a series with a kernel (a weighting pattern): each output is the sum of the series values multiplied by the reversed kernel. Use it to apply any custom smoothing or differencing filter, or to combine two distributions of delays. " +
            "Mode Full returns all n + m - 1 overlapping positions; Same returns the central n values aligned with the input (start offset (m - 1) / 2); Valid returns only the n - m + 1 positions where the kernel fully overlaps the series. For example [1, 2, 3] convolved with [0, 1, 0.5] gives [0, 1, 2.5, 4, 1.5] in Full mode. " +
            "Requires finite values and a kernel no longer than the series for Same and Valid; the work is limited to 200 million multiplications.")]
        public double[] LinearConvolution(
            [Description("The series to filter, finite, at least 1 value.")]
            double[] values,
            [Description("The kernel or filter weights, finite, at least 1 value.")]
            double[] kernel,
            [Description("Full, Same or Valid. Default Full.")]
            ConvolutionMode mode = ConvolutionMode.Full)
        {
            var x = TimeSeriesGuard.Finite(values, nameof(values), 1);
            var k = TimeSeriesGuard.Finite(kernel, nameof(kernel), 1);

            if ((double)x.Length * k.Length > 2e8)
            {
                throw new ArgumentException(
                    "values length times kernel length exceeds 200 million. Use a shorter kernel or series.",
                    nameof(kernel));
            }

            if (mode != ConvolutionMode.Full && k.Length > x.Length)
            {
                throw new ArgumentException(
                    $"kernel has {k.Length} values, more than values ({x.Length}); use Full mode or a shorter kernel.",
                    nameof(kernel));
            }

            var full = new double[x.Length + k.Length - 1];

            for (var i = 0; i < x.Length; i++)
            {
                for (var j = 0; j < k.Length; j++)
                {
                    full[i + j] += x[i] * k[j];
                }
            }

            switch (mode)
            {
                case ConvolutionMode.Same:
                    return full.Skip((k.Length - 1) / 2).Take(x.Length).ToArray();
                case ConvolutionMode.Valid:
                    return full.Skip(k.Length - 1).Take(x.Length - k.Length + 1).ToArray();
                default:
                    return full;
            }
        }

        [AdvancedAnalyticsTool("TemplateMatchNormalisedCrossCorrelation")]
        [Description(
            "Slides a template pattern along a series and scores how closely each stretch matches it, using normalised (Pearson) correlation so level and scale are ignored: 1 is a perfect match in shape, 0 unrelated and -1 an inverted match. Use it to find repeats of a known shape, for example a structuring pattern of deposits in a transaction history. " +
            "Scores[i] compares the template with values[i] to values[i + m - 1] (m is the template length); it is NaN where that stretch is constant. BestOffsets and BestScores list up to topK non-overlapping matches (offsets at least half a template length apart) scoring at least minScore, best first. " +
            "Requires finite values, a template of at least 3 values that is not constant, and a series at least as long as the template.")]
        public TemplateMatchResult TemplateMatchNormalisedCrossCorrelation(
            [Description("The series to search, finite.")]
            double[] values,
            [Description("The pattern to look for, at least 3 values and not constant.")]
            double[] template,
            [Description("How many matches to return, at least 1. Default 3.")]
            int topK = 3,
            [Description("Lowest correlation that counts as a match, between -1 and 1. Default 0.8.")]
            double minScore = 0.8)
        {
            var x = TimeSeriesGuard.Finite(values, nameof(values), 3);
            var t = TimeSeriesGuard.Finite(template, nameof(template), 3);
            TimeSeriesGuard.AtLeast(topK, 1, nameof(topK));

            if (double.IsNaN(minScore) || minScore < -1 || minScore > 1)
            {
                throw new ArgumentException($"minScore must be between -1 and 1 but was {minScore}.", nameof(minScore));
            }

            if (x.Length < t.Length)
            {
                throw new ArgumentException($"values has {x.Length} points, fewer than the template ({t.Length}).",
                    nameof(template));
            }

            if (TimeSeriesGuard.IsConstant(t))
            {
                throw new ArgumentException(
                    "template is constant, so its correlation is undefined. Supply a pattern with variation.",
                    nameof(template));
            }

            var m = t.Length;
            var tm = TimeSeriesGuard.Mean(t);
            var tNorm = Math.Sqrt(t.Sum(v => (v - tm) * (v - tm)));
            var count = x.Length - m + 1;
            var scores = new double[count];

            for (var i = 0; i < count; i++)
            {
                var window = new double[m];
                Array.Copy(x, i, window, 0, m);
                var wm = TimeSeriesGuard.Mean(window);
                var wNorm = Math.Sqrt(window.Sum(v => (v - wm) * (v - wm)));
                var dot = 0.0;

                for (var j = 0; j < m; j++)
                {
                    dot += (window[j] - wm) * (t[j] - tm);
                }

                scores[i] = wNorm <= 1e-12 * Math.Max(1, Math.Abs(wm))
                    ? double.NaN
                    : Math.Max(-1, Math.Min(1, dot / (wNorm * tNorm)));
            }

            var separation = Math.Max(1, m / 2);
            var chosen = new List<int>();

            foreach (var i in Enumerable.Range(0, count).Where(i => !double.IsNaN(scores[i]) && scores[i] >= minScore)
                         .OrderByDescending(i => scores[i]).ThenBy(i => i))
            {
                if (chosen.Count < topK && chosen.All(c => Math.Abs(c - i) >= separation))
                {
                    chosen.Add(i);
                }
            }

            return new TemplateMatchResult(scores, chosen.ToArray(), chosen.Select(i => scores[i]).ToArray(), m);
        }

        [AdvancedAnalyticsTool("HilbertEnvelopeAndPhase")]
        [Description(
            "Analytic signal of an oscillating series via the Hilbert transform: its instantaneous amplitude (envelope), phase and frequency at every point. Use it to track how the strength or the speed of a cycle changes over time, for example a daily rhythm that grows or drifts. " +
            "QuadratureComponent is the Hilbert transform (the series shifted a quarter cycle); Envelope = sqrt(series squared + quadrature squared); Phase is the unwrapped angle in radians; InstantaneousFrequency is the phase change per sample divided by 2 pi and by samplingIntervalSeconds (cycles per unit time), estimated with central differences. " +
            "The method treats the series as periodic, so results near both ends are distorted unless the series covers whole cycles, and it is only meaningful for narrow-band signals (remove the mean and trend first). Requires finite values.")]
        public HilbertResult HilbertEnvelopeAndPhase(
            [Description(
                "The series in time order, evenly spaced, finite, at least 4 values; de-trend it first for a clean result.")]
            double[] values,
            [Description("Time between samples, above 0, so frequencies come out in your unit. Default 1.")]
            double samplingIntervalSeconds = 1)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 4);
            TimeSeriesGuard.Positive(samplingIntervalSeconds, nameof(samplingIntervalSeconds));
            var n = v.Length;
            var quadrature = (double[])v.Clone();
            HilbertTransform2.FHT(quadrature, FourierTransform.Direction.Forward);
            var envelope = new double[n];
            var phase = new double[n];

            for (var i = 0; i < n; i++)
            {
                envelope[i] = Math.Sqrt(v[i] * v[i] + quadrature[i] * quadrature[i]);
                var angle = Math.Atan2(quadrature[i], v[i]);

                if (i > 0)
                {
                    while (angle - phase[i - 1] > Math.PI)
                    {
                        angle -= 2 * Math.PI;
                    }

                    while (angle - phase[i - 1] < -Math.PI)
                    {
                        angle += 2 * Math.PI;
                    }
                }

                phase[i] = angle;
            }

            var frequency = new double[n];

            for (var i = 0; i < n; i++)
            {
                var lo = Math.Max(0, i - 1);
                var hi = Math.Min(n - 1, i + 1);
                frequency[i] = (phase[hi] - phase[lo]) / ((hi - lo) * 2 * Math.PI * samplingIntervalSeconds);
            }

            return new HilbertResult(envelope, phase, frequency, quadrature);
        }

        [AdvancedAnalyticsTool("DiscreteCosineTransformSeries")]
        [Description(
            "Orthonormal type-II discrete cosine transform of a series, or its inverse. The cosine transform packs the shape of a smooth series into a few low-order coefficients, so it is used for compression, for smoothing by discarding high-order coefficients and as a compact feature vector. " +
            "With inverse false the output is the coefficient list: element 0 is the mean scaled by the square root of n, and later elements measure slower to faster cosine patterns. With inverse true the input is treated as coefficients and the original series is returned; a forward then an inverse call reproduces the input. " +
            "The transform is orthonormal, so the sum of squares is preserved. Requires finite values.")]
        public double[] DiscreteCosineTransformSeries(
            [Description("The series (or, for the inverse, the coefficients), finite, at least 2 values.")]
            double[] values,
            [Description("False (default) computes the forward transform; true computes the inverse.")]
            bool inverse = false)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 2);
            var work = (double[])v.Clone();

            if (inverse)
            {
                CosineTransform.IDCT(work);
            }
            else
            {
                CosineTransform.DCT(work);
            }

            return work;
        }

        [AdvancedAnalyticsTool("HaarWaveletDecomposition")]
        [Description(
            "Multi-level Haar wavelet decomposition: splits a series into a coarse approximation and detail coefficients at successively coarser time scales, separating slow structure from fast fluctuations. Use it to see at which time scale variation lives, to compress a series or to build multi-scale features. " +
            "The length must be divisible by 2 to the power levels. At each level neighbouring pairs are replaced by their average (kept for the next level) and half their difference (a detail). Approximation is the final coarse series (length n / 2^levels); Details[0] is the finest scale (length n / 2), Details[1] the next, and so on; DetailEnergyShares gives each level's share of the total squared detail (energy) so you can see which scale dominates. " +
            "For [4, 2, 5, 5] with one level the approximation is [3, 5] and the details are [1, 0]. Requires finite values.")]
        public HaarDecompositionResult HaarWaveletDecomposition(
            [Description("The series in time order, finite, with a length divisible by 2 to the power levels.")]
            double[] values,
            [Description("Number of decomposition levels, from 1 to 30. Default 3.")]
            int levels = 3)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 2);
            RequireHaarLength(v.Length, levels, nameof(values));
            var coefficients = HaarForward(v, levels);
            var details = new double[levels][];
            var length = v.Length;

            for (var level = 0; level < levels; level++)
            {
                var half = length / 2;
                details[level] = coefficients.Skip(half).Take(half).ToArray();
                length = half;
            }

            var energies = details.Select((d, i) => d.Sum(x => x * x) * Math.Pow(2, i + 1)).ToArray();
            var total = energies.Sum();

            return new HaarDecompositionResult(
                coefficients.Take(length).ToArray(),
                details,
                energies.Select(e => total > 0 ? e / total : double.NaN).ToArray(),
                levels);
        }

        [AdvancedAnalyticsTool("HaarWaveletDenoise")]
        [Description(
            "Denoises a series by wavelet shrinkage: it is decomposed with the Haar wavelet, small detail coefficients (assumed to be noise) are shrunk or removed, and the series is rebuilt. It preserves abrupt level changes better than a moving average. Use it to clean a noisy series before change point or trend work. " +
            "Threshold is applied to orthonormal-scale detail coefficients; leave it NaN to use the universal threshold sigma x sqrt(2 ln n), with the noise sigma estimated robustly from the finest details (median absolute value / 0.6745). Soft thresholding (default) shrinks surviving coefficients toward zero; hard keeps them unchanged. " +
            "Denoised is the reconstructed series, NoiseStandardDeviation the estimated sigma (reported even when you supply the threshold), and ZeroedCoefficients out of DetailCoefficients shows how much was removed. The length must be divisible by 2 to the power levels.")]
        public HaarDenoiseResult HaarWaveletDenoise(
            [Description("The series in time order, finite, with a length divisible by 2 to the power levels.")]
            double[] values,
            [Description("Number of decomposition levels, from 1 to 30. Default 3.")]
            int levels = 3,
            [Description(
                "Threshold for detail coefficients (orthonormal scale), 0 or more, or NaN (default) for the universal threshold.")]
            double threshold = double.NaN,
            [Description("True (default) applies soft thresholding; false applies hard thresholding.")]
            bool soft = true)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 2);
            RequireHaarLength(v.Length, levels, nameof(values));

            if (!double.IsNaN(threshold) && (threshold < 0 || double.IsInfinity(threshold)))
            {
                throw new ArgumentException($"threshold must be 0 or more or NaN but was {threshold}.",
                    nameof(threshold));
            }

            var coefficients = HaarForward(v, levels);
            var n = v.Length;
            var finest = coefficients.Skip(n / 2).Take(n / 2).Select(c => Math.Abs(c * Math.Sqrt(2))).ToArray();
            var sigma = TimeSeriesGuard.Median(finest) / 0.6745;
            var t = double.IsNaN(threshold) ? sigma * Math.Sqrt(2 * Math.Log(n)) : threshold;
            var zeroed = 0;
            var total = 0;
            var length = n;

            for (var level = 1; level <= levels; level++)
            {
                var half = length / 2;
                var scale = Math.Pow(2, level / 2.0);

                for (var i = half; i < length; i++)
                {
                    var ortho = coefficients[i] * scale;
                    var shrunk = Math.Abs(ortho) <= t ? 0 : soft ? Math.Sign(ortho) * (Math.Abs(ortho) - t) : ortho;
                    coefficients[i] = shrunk / scale;
                    total++;

                    if (shrunk == 0)
                    {
                        zeroed++;
                    }
                }

                length = half;
            }

            return new HaarDenoiseResult(HaarInverse(coefficients, levels), t, sigma, zeroed, total);
        }

        [AdvancedAnalyticsTool("FrequencyDomainFilterSeries")]
        [Description(
            "Filters a series in the frequency domain by removing chosen frequencies with an ideal (brick-wall) cut-off: LowPass keeps frequencies at or below lowCutoff, HighPass keeps those at or above lowCutoff, BandPass keeps those between lowCutoff and highCutoff and BandStop removes that band. Frequencies are in cycles per unit of samplingIntervalSeconds. " +
            "Use a low-pass to remove noise and keep slow cycles, a high-pass to remove a trend and slow drift, a band-pass to isolate one cycle (for example around 1/24 per hour) and a band-stop to remove one. Filtered is the result, Removed is the input minus Filtered, and KeptFrequencyBins of TotalFrequencyBins shows how much was kept. " +
            "Sharp cut-offs ring near the ends of the series and treat it as periodic; taper with a smoother filter (SavitzkyGolayFilter) if that matters. Requires finite values; highCutoff is needed only for BandPass and BandStop.")]
        public FrequencyFilterResult FrequencyDomainFilterSeries(
            [Description("The series in time order, evenly spaced, finite, at least 4 values.")]
            double[] values,
            [Description("LowPass, HighPass, BandPass or BandStop.")]
            FrequencyFilterKind kind,
            [Description(
                "The cut-off (LowPass, HighPass) or lower band edge (BandPass, BandStop) in cycles per time unit, 0 or more.")]
            double lowCutoff,
            [Description(
                "The upper band edge in cycles per time unit for BandPass and BandStop, above lowCutoff; NaN (default) otherwise.")]
            double highCutoff = double.NaN,
            [Description("Time between samples, above 0. Default 1.")]
            double samplingIntervalSeconds = 1)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 4);
            TimeSeriesGuard.Positive(samplingIntervalSeconds, nameof(samplingIntervalSeconds));

            if (double.IsNaN(lowCutoff) || lowCutoff < 0)
            {
                throw new ArgumentException($"lowCutoff must be 0 or more but was {lowCutoff}.", nameof(lowCutoff));
            }

            var band = kind == FrequencyFilterKind.BandPass || kind == FrequencyFilterKind.BandStop;

            if (band && (double.IsNaN(highCutoff) || highCutoff <= lowCutoff))
            {
                throw new ArgumentException(
                    "highCutoff must be a number greater than lowCutoff for BandPass and BandStop.",
                    nameof(highCutoff));
            }

            var n = v.Length;
            var spectrum = Transform(v);
            var kept = 0;

            for (var k = 0; k < n; k++)
            {
                var f = Math.Min(k, n - k) / (n * samplingIntervalSeconds);

                var keep = kind switch
                {
                    FrequencyFilterKind.LowPass => f <= lowCutoff,
                    FrequencyFilterKind.HighPass => f >= lowCutoff,
                    FrequencyFilterKind.BandPass => f >= lowCutoff && f <= highCutoff,
                    _ => f < lowCutoff || f > highCutoff
                };

                if (keep)
                {
                    kept++;
                }
                else
                {
                    spectrum[k] = Complex.Zero;
                }
            }

            FourierTransform2.FFT(spectrum, FourierTransform.Direction.Backward);
            var filtered = spectrum.Select(c => c.Real).ToArray();

            return new FrequencyFilterResult(filtered, v.Select((x, i) => x - filtered[i]).ToArray(), kept, n);
        }

        [AdvancedAnalyticsTool("ZeroCrossingAnalysis")]
        [Description(
            "Counts and locates the points where a series crosses a chosen level (zero by default), a simple measure of how quickly and how often it oscillates. Use it to estimate the cycle length of a de-meaned oscillating series or to count sign changes in a balance or residual series. " +
            "A crossing lies between two neighbouring samples where one is below the level and the other at or above it; Positions gives the fractional sample index of each crossing by linear interpolation, Count their number, RatePerSample the count divided by the number of gaps between samples and MeanCyclePeriodInSamples twice the mean spacing of the crossings (a full cycle has two crossings; NaN with fewer than 2 crossings). " +
            "Remove the mean or trend first if you want crossings of the average level. Requires finite values.")]
        public ZeroCrossingResult ZeroCrossingAnalysis(
            [Description("The series in time order, evenly spaced, finite, at least 2 values.")]
            double[] values,
            [Description("The level whose crossings are counted. Default 0.")]
            double level = 0)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 2);

            if (!double.IsFinite(level))
            {
                throw new ArgumentException("level must be a finite number.", nameof(level));
            }

            var positions = new List<double>();

            for (var i = 0; i < v.Length - 1; i++)
            {
                if (v[i] >= level != v[i + 1] >= level)
                {
                    positions.Add(i + (level - v[i]) / (v[i + 1] - v[i]));
                }
            }

            var period = positions.Count >= 2 ? 2 * (positions[^1] - positions[0]) / (positions.Count - 1) : double.NaN;

            return new ZeroCrossingResult(positions.Count, positions.ToArray(),
                positions.Count / (double)(v.Length - 1),
                period);
        }

        [AdvancedAnalyticsTool("PeakDetectionInSeries")]
        [Description(
            "Finds local peaks (or troughs) in a series and measures how much each stands out. A peak is a value higher than both neighbours (the middle of a flat top counts once); its prominence is how far it rises above the higher of the two lowest points reached before a taller peak or the series end on each side, so prominence separates real peaks from small wiggles. " +
            "Filter with minHeight (absolute level) and minProminence, and remove crowded peaks with minDistance (of two peaks closer than that, only the taller survives). Indices are ascending; Values, Prominences and the two base levels line up with them. With findTroughs true the series is negated so troughs are found and Values are reported in original units. " +
            "For [0, 2, 0, 3, 0, 1, 0] the peaks are at 1, 3 and 5 with prominences 2, 3 and 1. Requires finite values.")]
        public PeakDetectionResult PeakDetectionInSeries(
            [Description("The series in time order, finite, at least 3 values.")]
            double[] values,
            [Description(
                "Smallest value (or, for troughs, the largest) a peak may have. NaN (default) applies no height limit.")]
            double minHeight = double.NaN,
            [Description("Smallest prominence to keep, 0 or more. Default 0.")]
            double minProminence = 0,
            [Description("Smallest spacing in samples between kept peaks, at least 1. Default 1.")]
            int minDistance = 1,
            [Description("False (default) finds peaks; true finds troughs.")]
            bool findTroughs = false)
        {
            var v = TimeSeriesGuard.Finite(values, nameof(values), 3);
            TimeSeriesGuard.AtLeast(minDistance, 1, nameof(minDistance));

            if (double.IsNaN(minProminence) || minProminence < 0)
            {
                throw new ArgumentException($"minProminence must be 0 or more but was {minProminence}.",
                    nameof(minProminence));
            }

            var s = findTroughs ? v.Select(x => -x).ToArray() : v;
            var n = s.Length;
            var candidates = new List<(int Index, double Prominence, double Left, double Right)>();
            var i = 1;

            while (i < n - 1)
            {
                if (s[i] > s[i - 1])
                {
                    var j = i;

                    while (j + 1 < n && s[j + 1] == s[i])
                    {
                        j++;
                    }

                    if (j + 1 < n && s[j + 1] < s[i])
                    {
                        var index = (i + j) / 2;
                        var left = s[index];

                        for (var k = index - 1; k >= 0 && s[k] <= s[index]; k--)
                        {
                            left = Math.Min(left, s[k]);
                        }

                        var right = s[index];

                        for (var k = index + 1; k < n && s[k] <= s[index]; k++)
                        {
                            right = Math.Min(right, s[k]);
                        }

                        candidates.Add((index, s[index] - Math.Max(left, right), left, right));
                    }

                    i = j + 1;
                }
                else
                {
                    i++;
                }
            }

            var filtered = candidates.Where(c => c.Prominence >= minProminence
                                                 && (double.IsNaN(minHeight) || (findTroughs
                                                     ? v[c.Index] <= minHeight
                                                     : v[c.Index] >= minHeight))).ToList();

            var kept = new List<(int Index, double Prominence, double Left, double Right)>();

            foreach (var c in filtered.OrderByDescending(c => s[c.Index]).ThenBy(c => c.Index))
            {
                if (kept.All(k => Math.Abs(k.Index - c.Index) >= minDistance))
                {
                    kept.Add(c);
                }
            }

            kept = kept.OrderBy(c => c.Index).ToList();

            return new PeakDetectionResult(kept.Select(c => c.Index).ToArray(), kept.Select(c => v[c.Index]).ToArray(),
                kept.Select(c => c.Prominence).ToArray(),
                kept.Select(c => findTroughs ? -c.Left : c.Left).ToArray(),
                kept.Select(c => findTroughs ? -c.Right : c.Right).ToArray());
        }

        private static Complex[] Transform(double[] values)
        {
            var data = values.Select(x => new Complex(x, 0)).ToArray();
            FourierTransform2.FFT(data, FourierTransform.Direction.Forward);

            return data;
        }

        private static double[] Prepare(double[] values, SpectralDetrend detrend, SpectralWindow window,
            out double windowPower)
        {
            var n = values.Length;
            var work = (double[])values.Clone();

            if (detrend == SpectralDetrend.Mean)
            {
                var mean = TimeSeriesGuard.Mean(work);

                for (var i = 0; i < n; i++)
                {
                    work[i] -= mean;
                }
            }
            else if (detrend == SpectralDetrend.Linear)
            {
                TimeSeriesGuard.LinearFit(work, out var intercept, out var slope);

                for (var i = 0; i < n; i++)
                {
                    work[i] -= intercept + slope * i;
                }
            }

            windowPower = 1.0;

            if (window != SpectralWindow.None)
            {
                var sumSquares = 0.0;

                for (var i = 0; i < n; i++)
                {
                    var phase = 2 * Math.PI * i / n;

                    var w = window switch
                    {
                        SpectralWindow.Hann => 0.5 - 0.5 * Math.Cos(phase),
                        SpectralWindow.Hamming => 0.54 - 0.46 * Math.Cos(phase),
                        _ => 0.42 - 0.5 * Math.Cos(phase) + 0.08 * Math.Cos(2 * phase)
                    };

                    work[i] *= w;
                    sumSquares += w * w;
                }

                windowPower = sumSquares / n;
            }

            return work;
        }

        private static double[] OneSidedPower(double[] prepared, double windowPower)
        {
            var n = prepared.Length;
            var spectrum = Transform(prepared);
            var bins = n / 2 + 1;
            var power = new double[bins];

            for (var k = 0; k < bins; k++)
            {
                var magnitudeSquared =
                    spectrum[k].Real * spectrum[k].Real + spectrum[k].Imaginary * spectrum[k].Imaginary;

                var factor = k == 0 || (n % 2 == 0 && k == n / 2) ? 1.0 : 2.0;
                power[k] = factor * magnitudeSquared / (n * windowPower);
            }

            return power;
        }

        private static PowerSpectrumResult BuildSpectrum(double[] power, int n, double dt, int segments,
            SpectralWindow window)
        {
            var frequencies = Enumerable.Range(0, power.Length).Select(k => k / (n * dt)).ToArray();
            var periods = frequencies.Select(f => f > 0 ? 1 / f : double.PositiveInfinity).ToArray();
            var peak = -1;

            for (var k = 1; k < power.Length; k++)
            {
                if (peak < 0 || power[k] > power[peak])
                {
                    peak = k;
                }
            }

            var total = power.Skip(1).Sum();

            return new PowerSpectrumResult(frequencies, power, periods,
                peak < 0 ? double.NaN : frequencies[peak], peak < 0 ? double.NaN : periods[peak],
                peak < 0 ? double.NaN : power[peak], total, 1 / (n * dt), n, segments, window.ToString());
        }

        private static double LogChoose(int n, int k)
        {
            var sum = 0.0;

            for (var i = 1; i <= k; i++)
            {
                sum += Math.Log(n - k + i) - Math.Log(i);
            }

            return sum;
        }

        private static void RequireHaarLength(int length, int levels, string parameter)
        {
            if (levels < 1 || levels > 30)
            {
                throw new ArgumentException($"levels must be between 1 and 30 but was {levels}.", "levels");
            }

            if (length % (1L << levels) != 0)
            {
                throw new ArgumentException(
                    $"{parameter} has {length} values, which is not divisible by 2 to the power {levels} ({1L << levels}). Trim or pad the series to a multiple of {1L << levels}, or use fewer levels.",
                    parameter);
            }
        }

        private static double[] HaarForward(double[] v, int levels)
        {
            var work = (double[])v.Clone();
            var length = work.Length;

            for (var level = 0; level < levels; level++)
            {
                var sub = new double[length];
                Array.Copy(work, sub, length);
                Haar.FWT(sub);
                Array.Copy(sub, work, length);
                length /= 2;
            }

            return work;
        }

        private static double[] HaarInverse(double[] coefficients, int levels)
        {
            var work = (double[])coefficients.Clone();
            var length = work.Length >> (levels - 1);

            for (var level = 0; level < levels; level++)
            {
                var sub = new double[length];
                Array.Copy(work, sub, length);
                Haar.IWT(sub);
                Array.Copy(sub, work, length);
                length *= 2;
            }

            return work;
        }
    }
}