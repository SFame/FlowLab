using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    // Input variables
    private float horizontalInput;
    private float verticalInput;
    private Vector2 movementDirection;


    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer; // flip

    private bool _isInteracting = false;
    private bool _isPuzzleActive = false;

    void OnEnable()
    {
        TextDisplay.OnDialogueStarted += HandleDialogueStarted;
        TextDisplay.OnDialogueEnded += HandleDialogueEnded;

    }
    void OnDisable()
    {

        TextDisplay.OnDialogueStarted -= HandleDialogueStarted;
        TextDisplay.OnDialogueEnded -= HandleDialogueEnded;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    void Update()
    {
        if (_isInteracting || _isPuzzleActive)
        {
            return;
        }
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        movementDirection = new Vector2(horizontalInput, verticalInput).normalized;

        bool isMoving = movementDirection.magnitude > 0.1f;
        //animator.SetBool( ,isMoving)

        if (isMoving)
        {

            if (movementDirection.x != 0)
            {
                spriteRenderer.flipX = movementDirection.x < 0;
            }
        }
    }

    void FixedUpdate()
    {
        if (_isInteracting || _isPuzzleActive)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        rb.linearVelocity = movementDirection * moveSpeed;
    }

    // 퍼즐 활성화 상태를 설정하는 public 메서드
    public void SetPuzzleActive(bool isActive)
    {
        _isPuzzleActive = isActive;
    }

    private void HandleDialogueStarted(string dialogueId)
    {
        _isInteracting = true;
    }

    private void HandleDialogueEnded(string dialogueId)
    {
        _isInteracting = false;
    }
 


    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.CompareTag("Key") && Input.GetKey(KeyCode.Return))
        {
            GameManager.Instance.stageName = collision.GetComponent<Stage>().StageID;
            SceneManager.LoadSceneAsync("3.StageScene");
        }

    }
}