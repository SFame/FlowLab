using UnityEngine;

/// <summary>
/// 사운드 발생시키는 오브젝트 인터페이스
/// </summary>
public interface ISoundable
{
    event SoundEventHandler OnSounded;
}

public class SoundEventArgs
{
    public SoundEventArgs() { }

    public SoundEventArgs(int audioIndex)
    {
        AudioIndex = audioIndex;
    }

    public SoundEventArgs(int audioIndex, Vector3 occurredPosition)
    {
        AudioIndex = audioIndex;
        OccurredPosition = occurredPosition;
    }

    /// <summary>
    /// SoundEventListener의 오디오 인덱스
    /// </summary>
    public int AudioIndex { get; }

    /// <summary>
    /// 사운드 발생 위치 (null == 전역 발생)
    /// </summary>
    public Vector3? OccurredPosition { get; } = null;
}

public delegate void SoundEventHandler(ISoundable sender, SoundEventArgs args);