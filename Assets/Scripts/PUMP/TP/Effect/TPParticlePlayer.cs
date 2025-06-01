using UnityEngine;

public class TPParticlePlayer : MonoBehaviour
{
    [SerializeField] private ParticleSystem m_Particle;
    [SerializeField] private Component m_Soundable;
    [SerializeField] private int m_TargetAudioIndex = 0;

    private void Awake()
    {
        if (m_Soundable?.TryGetComponent(out ISoundable soundable) ?? false)
        {
            soundable.OnSounded += (sender, args) =>
            {
                if (args.AudioIndex == m_TargetAudioIndex)
                {
                    m_Particle.Play();
                }
            };
            return;
        }

        Debug.LogWarning("TP ParticleSystem disabled");
    }
}