using UnityEngine;

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

    private void OnEnable()
    {
        TextDisplay.OnDialogueStarted += HandleDialogueStarted;
        TextDisplay.OnDialogueEnded += HandleDialogueEnded;
    }
    private void OnDisable()
    {

        TextDisplay.OnDialogueStarted -= HandleDialogueStarted;
        TextDisplay.OnDialogueEnded -= HandleDialogueEnded;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        if (_isInteracting)
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
        if (_isInteracting)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        rb.linearVelocity = movementDirection * moveSpeed;
    }

    private void HandleDialogueStarted(string dialogueId)
    {
        _isInteracting = true;
    }

    private void HandleDialogueEnded(string dialogueId)
    {
        _isInteracting = false;
    }
}