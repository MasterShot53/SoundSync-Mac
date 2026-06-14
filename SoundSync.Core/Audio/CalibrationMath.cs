namespace SoundSync.Audio;

/// <summary>
/// Platform-agnostic calibration math shared by Windows (WASAPI) and Mac (Core Audio) engines.
/// </summary>
public static class CalibrationMath
{
    /// <summary>
    /// Searches mic samples for the arrival time of a click at the given frequency.
    /// Returns the onset time in seconds, or null if not detected.
    /// </summary>
    public static float? FindArrivalTime(
        List<float> mic, int micRate, float freq, int clickMs, int searchStart, int searchEnd)
    {
        int windowSize = micRate * clickMs / 1000;
        if (windowSize < 1 || searchEnd - searchStart < windowSize * 2) return null;

        int noiseWindow = micRate * 20 / 1000;
        float noiseFloor = GoertzelEnergy(mic, 0, Math.Min(noiseWindow, searchStart), freq, micRate);
        float threshold  = Math.Max(noiseFloor * 8f, 1e-8f);

        // Coarse scan
        int coarseStep = Math.Max(1, windowSize / 3);
        float bestEnergy = 0f;
        int bestPos = -1;
        for (int pos = searchStart; pos <= searchEnd - windowSize; pos += coarseStep)
        {
            float e = GoertzelEnergy(mic, pos, windowSize, freq, micRate);
            if (e > bestEnergy) { bestEnergy = e; bestPos = pos; }
        }
        if (bestPos < 0 || bestEnergy < threshold) return null;

        // Fine scan
        int fineStep   = Math.Max(1, micRate / 1000);
        int fineRadius = windowSize * 2;
        int fineStart  = Math.Max(searchStart, bestPos - fineRadius);
        int fineEnd    = Math.Min(searchEnd - windowSize, bestPos + fineRadius);
        float fineBest = 0f;
        int finePos = bestPos;
        for (int pos = fineStart; pos <= fineEnd; pos += fineStep)
        {
            float e = GoertzelEnergy(mic, pos, windowSize, freq, micRate);
            if (e > fineBest) { fineBest = e; finePos = pos; }
        }

        // Onset walkback
        int onsetPos = finePos;
        for (int pos = finePos; pos >= Math.Max(searchStart, finePos - windowSize * 3); pos -= fineStep)
        {
            float e = GoertzelEnergy(mic, pos, windowSize, freq, micRate);
            if (e < threshold) { onsetPos = pos + fineStep; break; }
        }

        return (float)onsetPos / micRate;
    }

    public static float GoertzelEnergy(List<float> samples, int start, int count, float freq, int sampleRate)
    {
        if (count <= 0 || start + count > samples.Count) return 0f;
        double coeff = 2.0 * Math.Cos(2.0 * Math.PI * freq / sampleRate);
        double s1 = 0, s2 = 0;
        for (int i = start; i < start + count; i++)
        {
            double s = samples[i] + coeff * s1 - s2;
            s2 = s1; s1 = s;
        }
        return (float)(s1 * s1 + s2 * s2 - coeff * s1 * s2);
    }

    /// <summary>
    /// Given per-device arrival times (seconds), computes delay offsets (ms) so all
    /// speakers are aligned to the latest-arriving one.
    /// </summary>
    public static Dictionary<string, float> ComputeDelayOffsets(Dictionary<string, float> arrivals)
    {
        if (arrivals.Count < 1) return new();
        float maxArrival = arrivals.Values.Max();
        var offsets = arrivals.ToDictionary(
            kv => kv.Key,
            kv => (maxArrival - kv.Value) * 1000f);
        return offsets;
    }
}
