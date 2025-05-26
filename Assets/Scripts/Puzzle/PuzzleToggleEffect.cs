using UnityEngine;
using UnityEngine.UI;

public class PuzzleToggleEffect : MonoBehaviour
{
    [SerializeField]
    private Image m_ToggleImage;
    private Color defaultColor;

    [SerializeField]
    private float m_blinkSpeed = 2f; // 깜빡이는 속도 조절

    [SerializeField]
    private float m_minAlpha = 0.3f; // 최소 알파값 (완전히 투명하지 않게)

    [SerializeField]
    private float m_maxAlpha = 1f; // 최대 알파값
    private void Start()
    {
        defaultColor = m_ToggleImage.color;
    }
    void Update()
    {
        // Time.time을 사용해 시간 기반으로 Sin 함수 계산
        float alpha = Mathf.Sin(Time.time * m_blinkSpeed);

        // Sin 값을 0~1 범위로 정규화 (Sin은 -1~1 값을 가지므로)
        alpha = (alpha + 1f) / 2f;

        // 최소값과 최대값 사이로 알파값 조절
        alpha = Mathf.Lerp(m_minAlpha, m_maxAlpha, alpha);

        // 새로운 색상 적용 (Color는 struct이므로 직접 수정 불가, 새로 할당 필요)
        Color newColor = defaultColor;
        newColor.a = alpha;
        m_ToggleImage.color = newColor;
    }

    
}
