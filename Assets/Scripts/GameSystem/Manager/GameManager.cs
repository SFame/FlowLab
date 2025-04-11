using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region singleton
    public static GameManager Instance { get; private set; }
    #endregion

    private GameObject player;
    public GameObject Player
    {
        get
        {
            player ??= GameObject.FindGameObjectWithTag("Player");
            return player;
        }
    }

    private void Awake()
    {
        // 싱글톤 처리
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        GlobalEventManager.OnGameStartEvent();

    }
}
