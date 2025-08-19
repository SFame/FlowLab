using System;
using UnityEngine;

public static class ChannelClock
{
    public const int MaxChannels = 8;
    private static readonly int[] _phaseFrames = new int[MaxChannels];

    public static void AlignNow(int channel)
    {
        if (channel < 0 || channel >= MaxChannels) throw new ArgumentOutOfRangeException(nameof(channel));
        _phaseFrames[channel] = Time.frameCount;
    }

    public static int FramesUntilNextTick(int channel, int periodFrames, bool emitImmediatelyIfAligned = true)
    {
        if (channel < 0 || channel >= MaxChannels) throw new ArgumentOutOfRangeException(nameof(channel));
        if (periodFrames <= 0) periodFrames = 1;

        int now = Time.frameCount;
        int elapsed = (now - _phaseFrames[channel]) % periodFrames;
        if (elapsed < 0) elapsed += periodFrames;

        int wait = (periodFrames - elapsed) % periodFrames;

        if (wait == 0 && !emitImmediatelyIfAligned) return periodFrames; // 항상 다음 틱
        return wait; // 0이면 즉시
    }
}
