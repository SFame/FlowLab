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

        _clientMirrorPool?.Dispose();
    }
    #endregion

    [SerializeField] private GameObject m_ClientMirrorTemplate;
    [SerializeField] private Transform m_ClientMirrorParent;
    [SerializeField] private Transform m_ClientMirrorPool;
    [SerializeField] private SpriteRenderer m_BackgroundRenderer;
    [SerializeField] private Sprite m_ClientMirrorDefaultSprite;

    private Pool<TransformSpriteRendererPair> _clientMirrorPool;

    private Pool<TransformSpriteRendererPair> ClientMirrorPool
    {
        get
        {
            return _clientMirrorPool ??= new Pool<TransformSpriteRendererPair>
            (
                createFunc: () =>
                {
                    if (gameObject == null)
                    {
                        return null;
                    }

                    GameObject newGo = Instantiate(m_ClientMirrorTemplate, m_ClientMirrorPool, true);
                    newGo.SetActive(false);
                    return new TransformSpriteRendererPair(newGo);
                },
                initSize: 50,
                maxSize: 2000,
                actionOnGet: tsp =>
                {
                    tsp.Transform.SetParent(m_ClientMirrorParent);
                    tsp.Transform.gameObject.SetActive(true);
                },
                actionOnRelease: tsp =>
                {
                    if (tsp.IsDestroyed)
                    {
                        return;
                    }

                    tsp.Transform.SetParent(m_ClientMirrorPool);
                    tsp.Transform.gameObject.SetActive(false);
                },
                actionOnDestroy: tsp =>
                {
                    if (tsp.IsDestroyed)
                    {
                        return;
                    }

                    tsp.Destroy();
                }
            );
        }
    }

    private static void PlaceTransformAtRatio(TransformSpriteRendererPair tsp, Vector2 ratio)
    {
        if (tsp.IsDestroyed)
        {
            return;
        }

        Bounds backgroundBounds = Instance.m_BackgroundRenderer.bounds;

        Vector3 worldPosition = new Vector3
        (
            backgroundBounds.center.x + ratio.x * backgroundBounds.size.x,
            backgroundBounds.center.y + ratio.y * backgroundBounds.size.y,
            tsp.Transform.position.z
        );

        tsp.Transform.position = worldPosition;
    }

    private static void SetTransformSizeAtRatio(TransformSpriteRendererPair tsp, Vector2 ratio)
    {
        if (tsp.IsDestroyed || Instance.m_BackgroundRenderer == null || Instance.m_BackgroundRenderer.sprite == null)
        {
            return;
        }

        Vector3 backgroundOriginalSize = Instance.m_BackgroundRenderer.sprite.bounds.size;

        Vector2 targetSize = new Vector2
        (
            backgroundOriginalSize.x * ratio.x,
            backgroundOriginalSize.y * ratio.y
        );

        SpriteRenderer spriteRenderer = tsp.Renderer;
        if (spriteRenderer == null)
        {
            Debug.LogWarning("SpriteRenderer가 없어 크기를 조절할 수 없습니다.", tsp.Transform);
            return;
        }

        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = Instance.m_ClientMirrorDefaultSprite;
        }

        spriteRenderer.size = targetSize;

        tsp.Transform.localScale = Vector3.one;
    }

    public static void Register(IMinimapProxyClient client)
    {
        if (client == null)
        {
            Debug.LogError($"{Instance.GetType().Name}.Register: Null was assigned");
            return;
        }

        TransformSpriteRendererPair pair = Instance.ClientMirrorPool.Get();

        Transform mirrorTransform = pair.Transform;
        SpriteRenderer spriteRenderer = pair.Renderer;

        Vector3 mirrorTransformPosition = mirrorTransform.position;
        mirrorTransform.position = new Vector3(mirrorTransformPosition.x, mirrorTransformPosition.y, client.OrderZ);

        spriteRenderer.sprite = client.Sprite ?? Instance.m_ClientMirrorDefaultSprite;
        spriteRenderer.color = client.SpriteColor;

        Vector2 clientRatio = WorldCanvasGetter.WorldPositionToRatio(client.CurrentWorldPosition, true);
        PlaceTransformAtRatio(pair, clientRatio);

        Vector2 clientSizeRatio = WorldCanvasGetter.WorldSizeToRatio(client.Size, true);
        SetTransformSizeAtRatio(pair, clientSizeRatio);

        client.OnClientMove += movePos =>
        {
            if (pair.IsDestroyed)
            {
                return;
            }

            Vector2 ratio = WorldCanvasGetter.WorldPositionToRatio(movePos, true);
            PlaceTransformAtRatio(pair, ratio);
        };

        client.OnClientSizeUpdate += size =>
        {
            if (pair.IsDestroyed)
            {
                return;
            }

            Vector2 sizeRatio = WorldCanvasGetter.WorldSizeToRatio(size, true);
            SetTransformSizeAtRatio(pair, sizeRatio);
        };

        client.OnActiveStateChanged += isActive =>
        {
            if (pair.IsDestroyed)
            {
                return;
            }

            mirrorTransform.gameObject.SetActive(isActive);
        };

        client.OnClientDestroy += () =>
        {
            Instance.ClientMirrorPool.Release(pair);
        };
    }

    private class TransformSpriteRendererPair
    {
        public TransformSpriteRendererPair(GameObject gameObject)
        {
            Transform = gameObject.transform;
            Renderer = gameObject.GetComponent<SpriteRenderer>();
        }

        public Transform Transform { get; }
        public SpriteRenderer Renderer { get; }

        public bool IsDestroyed => Transform == null;

        public void Destroy()
        {
            Object.Destroy(Transform.gameObject);
        }
    }
}