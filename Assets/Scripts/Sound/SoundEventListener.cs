using System.Collections.Generic;
using UnityEngine;

public class SoundEventListener : MonoBehaviour
{
    #region On Inspector
    [Space(5)]

    [SerializeField] private AudioSource m_AudioSource;

    [Space(10)]

    [SerializeField] private bool m_IsListen = true;
    [SerializeField] private List<AudioClip> m_AudioList;

    [Space(20)]

    [SerializeField] private bool m_FindSoundableThisObject = true;
    [SerializeField] private List<Component> m_SoundableComponent;
    #endregion

    private HashSet<ISoundable> _soundables = new();

    #region Protected
    protected List<AudioClip> AudioList => m_AudioList;
    protected HashSet<ISoundable> Soundables => _soundables;

    protected virtual void Awake()
    {
        Initialize();
    }

    protected virtual void OnDestroy()
    {
        Terminate();
    }

    protected virtual void OnSoundEvent(ISoundable sender, SoundEventArgs args)
    {
        if (!AudioSourceIsNull())
        {
            AudioClip targetClip = GetClipByIndex(args.AudioIndex);

            if (targetClip != null)
            {
                m_AudioSource.PlayOneShot(targetClip);
            }
        }
    }
    #endregion

    #region Privates
    private AudioClip GetClipByIndex(int index)
    {
        if (index < 0 || index >= m_AudioList.Count)
        {
            Debug.LogWarning($"Invalid audio index: {index}");
            return null;
        }

        return m_AudioList[index];
    }

    private void InternalOnSoundEvent(ISoundable sender, SoundEventArgs args)
    {
        if (m_IsListen)
        {
            OnSoundEvent(sender, args);
        }
    }

    private bool AudioSourceIsNull()
    {
        if (m_AudioSource == null)
        {
            Debug.LogWarning("AudioSource component is missing.");
            return true;
        }
        return false;
    }

    private void Initialize()
    {
        if (m_SoundableComponent != null)
        {
            foreach (Component component in m_SoundableComponent) // 인스펙터 등록 컴포넌트 => soundables 이동
            {
                if (component is ISoundable converted)
                {
                    _soundables.Add(converted);
                }
            }
        }

        if (m_FindSoundableThisObject)
        {
            ISoundable[] foundSoundables = GetComponents<ISoundable>();
            foreach (ISoundable foundSoundable in foundSoundables) // GetComponents 후 => soundables 이동
            {
                _soundables.Add(foundSoundable);
            }
        }

        foreach (ISoundable soundable in _soundables)
        {
            soundable.OnSounded += InternalOnSoundEvent;
        }
    }

    private void Terminate()
    {
        foreach (ISoundable soundable in _soundables)
        {
            if (soundable != null)
            {
                soundable.OnSounded -= InternalOnSoundEvent;
            }
        }
    }
    #endregion
}