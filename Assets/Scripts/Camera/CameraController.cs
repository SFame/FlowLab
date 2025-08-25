using System;
using System.Numerics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Camera m_Camera;
    [SerializeField] private float m_CameraMaxSize;
    [SerializeField] private float m_CameraMinSize;
    [SerializeField] private float m_ZoomScale;
    [SerializeField] private float m_MoveScale;

    private Bounds _worldCanvasBounds;
    private bool _setBounds = false;

    private Bounds WorldCanvasBounds
    {
        get
        {
            if (!_setBounds)
            {
                _worldCanvasBounds = WorldCanvasGetter.GetBounds();
                _setBounds = true;
            }

            return _worldCanvasBounds;
        }
    }

    private Vector3 SetCameraPositionInBounds()
    {
        if (m_Camera == null)
        {
            return Vector3.zero;
        }

        // 현재 카메라가 비추는 영역 계산

        Vector2 cameraSize = GetCameraSize();

        // 월드 캔버스 경계
        Bounds bounds = WorldCanvasBounds;

        // 카메라 위치 제한 계산
        float minX = bounds.min.x + cameraSize.x * 0.5f;
        float maxX = bounds.max.x - cameraSize.x * 0.5f;
        float minY = bounds.min.y + cameraSize.y * 0.5f;
        float maxY = bounds.max.y - cameraSize.y * 0.5f;

        // 카메라가 월드 캔버스보다 큰 경우 중앙으로 고정
        if (cameraSize.x >= bounds.size.x)
        {
            minX = maxX = bounds.center.x;
        }
        if (cameraSize.y >= bounds.size.y)
        {
            minY = maxY = bounds.center.y;
        }

        // 현재 카메라 위치를 경계 안으로 제한
        Vector3 currentPos = m_Camera.transform.position;
        float clampedX = Mathf.Clamp(currentPos.x, minX, maxX);
        float clampedY = Mathf.Clamp(currentPos.y, minY, maxY);

        Vector3 cameraPos = new Vector3(clampedX, clampedY, currentPos.z);
        m_Camera.transform.position = cameraPos;

        return cameraPos;
    }

    public event Action<Vector2> OnCameraMove;
    public event Action<Vector3> OnCameraSizeUpdate;

    public Camera Camera => m_Camera;

    public Vector3 GetCameraPosition()
    {
        return m_Camera.transform.position;
    }

    public Vector2 GetCameraSize()
    {
        float cameraHeight = m_Camera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * m_Camera.aspect;
        return new Vector2(cameraWidth, cameraHeight);
    }

    public void ZoomIn()
    {
        if (m_Camera == null)
        {
            return;
        }

        // m_CameraMinSize까지 작아질 수 있고, 한번에 작아지는 수치는 m_ZoomScale를 사용
        m_Camera.orthographicSize = Mathf.Max(m_Camera.orthographicSize - m_ZoomScale, m_CameraMinSize);
        OnCameraMove?.Invoke(SetCameraPositionInBounds());
        OnCameraSizeUpdate?.Invoke(GetCameraSize());
    }

    public void ZoomOut()
    {
        if (m_Camera == null)
        {
            return;
        }

        // m_CameraMaxSize까지 커질 수 있고, 한번에 커지는 수치는 m_ZoomScale를 사용
        m_Camera.orthographicSize = Mathf.Min(m_Camera.orthographicSize + m_ZoomScale, m_CameraMaxSize);
        OnCameraMove?.Invoke(SetCameraPositionInBounds());
        OnCameraSizeUpdate?.Invoke(GetCameraSize());
    }

    public void MovePosition(Vector2 direction, float weight = 1.0f)
    {
        if (m_Camera == null)
        {
            return;
        }

        direction = direction.normalized;
        Vector2 move = direction * m_MoveScale * weight;
        Vector3 currentPos = m_Camera.transform.position;

        m_Camera.transform.position = new Vector3
        (
            currentPos.x + move.x,
            currentPos.y + move.y,
            currentPos.z
        );

        OnCameraMove?.Invoke(SetCameraPositionInBounds());
    }

    public void MovePositionAbsolutely(Vector2 move)
    {
        if (m_Camera == null)
        {
            return;
        }

        Vector3 currentPos = m_Camera.transform.position;

        m_Camera.transform.position = new Vector3
        (
            currentPos.x + move.x,
            currentPos.y + move.y,
            currentPos.z
        );

        OnCameraMove?.Invoke(SetCameraPositionInBounds());
    }

    public void SetPosition(Vector2 position)
    {
        if (m_Camera == null)
        {
            return;
        }

        m_Camera.transform.position = new Vector3(position.x, position.y, m_Camera.transform.position.z);
        OnCameraMove?.Invoke(SetCameraPositionInBounds());
    }

    public void SetPositionAsRatio(Vector2 ratio)
    {
        if (m_Camera == null)
        {
            return;
        }

        Vector2 newPos = WorldCanvasGetter.RatioToWorldPosition(ratio);
        m_Camera.transform.position = new Vector3(newPos.x, newPos.y, m_Camera.transform.position.z);
        OnCameraMove?.Invoke(SetCameraPositionInBounds());
    }
}