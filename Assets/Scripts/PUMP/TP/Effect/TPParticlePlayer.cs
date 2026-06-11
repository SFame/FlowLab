using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class TPParticlePlayer : MonoBehaviour
{
    [SerializeField] private Graphic m_Graphic;
    [SerializeField] private float m_Duration = 0.5f;
    [SerializeField] private Component m_Soundable;
    [SerializeField] private int m_TargetAudioIndex = 0;

    private ISoundable _soundable;
    private int _fadeVersion;
    private float _alpha;

    private void Awake()
    {
        if (m_Graphic == null)
        {
            Debug.LogWarning("TP ParticleSystem disabled", this);
            return;
        }

        if (m_Soundable != null && m_Soundable.TryGetComponent(out _soundable))
        {
            _soundable.OnSounded += OnSounded;
            return;
        }

        Debug.LogWarning("TP ParticleSystem disabled", this);
    }

    private void OnEnable()
    {
        if (m_Graphic != null)
        {
            Apply(_alpha);
        }
    }

    private void OnDestroy()
    {
        if (_soundable != null)
        {
            _soundable.OnSounded -= OnSounded;
        }
    }

    private void OnSounded(object sender, SoundEventArgs args)
    {
        if (args.AudioIndex != m_TargetAudioIndex)
            return;

        _fadeVersion++;
        FadeAsync(_fadeVersion, destroyCancellationToken).Forget();
    }

    private async UniTaskVoid FadeAsync(int version, CancellationToken ct)
    {
        Apply(1f);

        float elapsed = 0f;
        while (elapsed < m_Duration)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, ct);

            if (version != _fadeVersion)
            {
                return;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / m_Duration);

            Apply((1f - t) * (1f - t));
        }
    }

    private void Apply(float a)
    {
        _alpha = a;
        m_Graphic.canvasRenderer.SetAlpha(a);
    }
}