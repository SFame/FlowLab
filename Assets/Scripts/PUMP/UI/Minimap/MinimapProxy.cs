using UnityEngine;

public class MinimapProxy : MonoBehaviour
{
    #region Singleton_Private
    private const string PREFAB_PATH = "PUMP/Minimap/MinimapProxy";
    private static MinimapProxy _instance;

    private static MinimapProxy Instance
    {
        get
        {
            InstanceCheck();
            return _instance;
        }
    }

    private static GameObject _prefab;

    private static GameObject Prefab
    {
        get
        {
            if (_prefab == null)
            {
                _prefab = Resources.Load<GameObject>(PREFAB_PATH);
            }

            return _prefab;
        }
    }

    private static void InstanceCheck()
    {
        if (_instance == null)
        {
            GameObject newObject = Instantiate(Prefab);
            _instance = newObject.GetComponent<MinimapProxy>();
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            return;
        }

        if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
    #endregion

    [SerializeField] private GameObject m_ClientMirrorTemplate;
    [SerializeField] private Transform m_ClientMirrorParent;
    [SerializeField] private Transform m_ClientMirrorPool;
    [SerializeField] private SpriteRenderer m_BackgroundRenderer;
    [SerializeField] private Sprite m_ClientMirrorDefaultSprite;

    private Pool<Transform> _clientMirrorPool;

    private Pool<Transform> ClientMirrorPool
    {
        get
        {
            return _clientMirrorPool ??= new Pool<Transform>
            (
                createFunc: () =>
                {
                    GameObject newGo = Instantiate(m_ClientMirrorTemplate, m_ClientMirrorPool, true);
                    newGo.SetActive(false);
                    return newGo.transform;
                },
                initSize: 50,
                maxSize: 2000,
                actionOnGet: t =>
                {
                    t.SetParent(m_ClientMirrorParent);
                    t.gameObject.SetActive(true);
                },
                actionOnRelease: t =>
                {
                    t.SetParent(m_ClientMirrorPool);
                    t.gameObject.SetActive(false);
                },
                actionOnDestroy: t =>
                {
                    Destroy(t.gameObject);
                }
            );
        }
    }

    private static void PlaceTransformAtRatio(Transform transform, Vector2 ratio)
    {
        Bounds backgroundBounds = Instance.m_BackgroundRenderer.bounds;

        Vector3 worldPosition = new Vector3
        (
            backgroundBounds.center.x + ratio.x * backgroundBounds.size.x,
            backgroundBounds.center.y + ratio.y * backgroundBounds.size.y,
            backgroundBounds.center.z
        );

        transform.position = worldPosition;
    }

    public static void Register(IMinimapProxyClient client)
    {
        Transform mirrorTransform = Instance.ClientMirrorPool.Get();

        SpriteRenderer spriteRenderer = mirrorTransform.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = client.Sprite ?? Instance.m_ClientMirrorDefaultSprite;
        spriteRenderer.color = client.SpriteColor;

        Vector2 clientRatio = WorldCanvasGetter.WorldPositionToRatio(client.CurrentWorldPosition, true);
        PlaceTransformAtRatio(mirrorTransform, clientRatio);

        client.OnClientMove += movePos =>
        {
            Vector2 ratio = WorldCanvasGetter.WorldPositionToRatio(movePos, true);
            PlaceTransformAtRatio(mirrorTransform, ratio);
        };

        client.OnActiveStateChanged += isActive =>
        {
            mirrorTransform.gameObject.SetActive(isActive);
        };

        client.OnClientDestroy += () =>
        {
            Instance.ClientMirrorPool.Release(mirrorTransform);
        };
    }
}