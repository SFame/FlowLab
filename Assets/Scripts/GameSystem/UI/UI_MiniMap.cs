using TMPro;
using UnityEngine;

public class UI_MiniMap : MonoBehaviour, ISoundable
{
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private float zoomMin = 1;
    [SerializeField] private float zoomMax = 20;
    [SerializeField] private float zoomStep = 1;

    [SerializeField] TextMeshProUGUI stageName;

    public event SoundEventHandler OnSounded;

    public void SetStageName(string name) // 미니맵 호출할떄마다 플레이어 위치 참조 혹은 어딘가에서 가져와야겟는데
    {
        stageName.text = name;
    }

    public void ZoomIn()
    {
        minimapCamera.orthographicSize = Mathf.Max(minimapCamera.orthographicSize - zoomStep, zoomMin);
        //OnSounded?.Invoke(this, new (0, gameObject.transform.position)); // ZoomInSound
    }

    public void ZoomOut()
    {
        minimapCamera.orthographicSize = Mathf.Min(minimapCamera.orthographicSize + zoomStep, zoomMax);
        //OnSounded?.Invoke(this, new(1, gameObject.transform.position)); // ZoomOutSound
    }
}
