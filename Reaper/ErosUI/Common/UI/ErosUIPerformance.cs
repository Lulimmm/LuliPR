using System.Diagnostics;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;

namespace ErosUI;

internal enum ErosUIRenderSection { PixelPet, Settings, Qt, Hotkey, Splash, Count }

// Session-only, opt-in measurements. Inactive Draw calls take one branch and
// never read a clock, query allocations, create a scope object or write to disk.
internal static class ErosUIPerformance
{
    private const ulong AllowedContentId = 18014469510658913UL;
    private const int HistoryCapacity = 120;
    private static readonly WindowTotals[] Windows = new WindowTotals[(int)ErosUIRenderSection.Count];
    private static readonly float[] FrameHistory = new float[HistoryCapacity];
    private static bool enabled;
    private static int generation;
    private static int currentFrame = -1;
    private static long frameTicks, frameBytes;
    private static bool frameHasSamples;
    private static long totalTicks, totalBytes, peakFrameTicks, frames;
    private static int historyNext, historyCount;

    private struct WindowTotals
    {
        public long Calls, Ticks, Bytes, PeakTicks;
    }

    internal readonly record struct Snapshot(long Calls, double AverageMilliseconds, double PeakMilliseconds, double AverageBytes);

    internal static bool IsAuthorized(ulong contentId) => contentId == AllowedContentId;

    internal static bool CanView
    {
        get
        {
            var allowed = Svc.ClientState.IsLoggedIn && IsAuthorized(Svc.PlayerState.ContentId);
            if (!allowed) Stop();
            return allowed;
        }
    }

    internal static bool IsEnabled => enabled;
    internal static long FrameCount => frames;
    internal static double AverageFrameMilliseconds => frames == 0 ? 0d : ToMilliseconds(totalTicks) / frames;
    internal static double PeakFrameMilliseconds => ToMilliseconds(peakFrameTicks);
    internal static double AverageFrameBytes => frames == 0 ? 0d : (double)totalBytes / frames;
    internal static int HistoryCount => historyCount;
    internal static float HistoryAt(int index) => FrameHistory[(historyNext - historyCount + index + HistoryCapacity) % HistoryCapacity];

    internal static void Start()
    {
        if (!CanView) return;
        Clear();
        enabled = true;
    }

    internal static void Stop()
    {
        if (!enabled) return;
        CompleteFrame();
        enabled = false;
        generation++;
    }

    internal static void Clear()
    {
        generation++;
        Array.Clear(Windows);
        Array.Clear(FrameHistory);
        currentFrame = -1;
        frameTicks = frameBytes = totalTicks = totalBytes = peakFrameTicks = frames = 0;
        historyNext = historyCount = 0;
        frameHasSamples = false;
    }

    internal static Measurement Measure(ErosUIRenderSection section)
    {
        if (!enabled || !CanView) return default;
        var frame = ImGui.GetFrameCount();
        if (frame != currentFrame)
        {
            CompleteFrame();
            currentFrame = frame;
        }
        return new Measurement(section, generation, GC.GetAllocatedBytesForCurrentThread(), Stopwatch.GetTimestamp());
    }

    internal static Snapshot GetSnapshot(ErosUIRenderSection section)
    {
        var w = Windows[(int)section];
        return new Snapshot(w.Calls, w.Calls == 0 ? 0d : ToMilliseconds(w.Ticks) / w.Calls,
            ToMilliseconds(w.PeakTicks), w.Calls == 0 ? 0d : (double)w.Bytes / w.Calls);
    }

    private static double ToMilliseconds(long ticks) => ticks * (1000d / Stopwatch.Frequency);

    private static void CompleteFrame()
    {
        if (!frameHasSamples) return;
        frames++;
        totalTicks += frameTicks;
        totalBytes += frameBytes;
        peakFrameTicks = Math.Max(peakFrameTicks, frameTicks);
        FrameHistory[historyNext] = (float)ToMilliseconds(frameTicks);
        historyNext = (historyNext + 1) % HistoryCapacity;
        historyCount = Math.Min(historyCount + 1, HistoryCapacity);
        frameTicks = frameBytes = 0;
        frameHasSamples = false;
    }

    internal readonly struct Measurement : IDisposable
    {
        private readonly ErosUIRenderSection section;
        private readonly int capturedGeneration;
        private readonly long allocatedBytes, startedAt;
        private readonly bool active;

        internal Measurement(ErosUIRenderSection section, int generation, long allocatedBytes, long startedAt)
        {
            this.section = section;
            capturedGeneration = generation;
            this.allocatedBytes = allocatedBytes;
            this.startedAt = startedAt;
            active = true;
        }

        public void Dispose()
        {
            if (!active || !enabled || capturedGeneration != generation) return;
            var elapsed = Math.Max(0, Stopwatch.GetTimestamp() - startedAt);
            var bytes = Math.Max(0, GC.GetAllocatedBytesForCurrentThread() - allocatedBytes);
            ref var w = ref Windows[(int)section];
            w.Calls++;
            w.Ticks += elapsed;
            w.Bytes += bytes;
            w.PeakTicks = Math.Max(w.PeakTicks, elapsed);
            frameTicks += elapsed;
            frameBytes += bytes;
            frameHasSamples = true;
        }
    }
}
