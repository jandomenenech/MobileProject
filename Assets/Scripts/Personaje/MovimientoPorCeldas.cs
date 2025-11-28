using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovimientoPorCeldas : MonoBehaviour
{
    [Header("Grid")]
    private float cellSize = 1.0f;
    [SerializeField] private float moveSpeed = 5.0f;
    private Vector2 gridOrigin = new Vector2(-0.5f, 1.2f);

    private Vector2 targetPosition;
    private Animator animator;
    private Vector2 movementDirection;
    private bool isMoving = false;
    private Vector2 inputDirection;
    private Vector2 lastInputDirection = Vector2.down;

    private Inventario inventario;
    private Rigidbody2D rb;

    [Header("Colisiones")]
    [SerializeField] private LayerMask solidLayer;     // Layer de paredes / obstáculos
    [SerializeField] private float collisionRadius = 0.1f;

    [Header("Ataque / Gizmo")]
    [SerializeField] private Transform attackCheck;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private Vector2 offsetRight = new Vector2(0.6f, 0f);
    [SerializeField] private Vector2 offsetLeft = new Vector2(-0.6f, 0f);
    [SerializeField] private Vector2 offsetUp = new Vector2(0f, 0.6f);
    [SerializeField] private Vector2 offsetDown = new Vector2(0f, -0.6f);

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        targetPosition = SnapToGrid(rb.position);
        rb.position = targetPosition;

        animator = GetComponent<Animator>();
        inventario = GetComponent<Inventario>();

        ActualizarOrientacionYGizmo();
    }

    void Update()
    {
        if (inventario.isActive == false)
        {
            // Solo leer input / animaciones aquí
            if (!isMoving)
            {
                DetectMovementInput();
            }

            UpdateAnimations();
            ActualizarOrientacionYGizmo();
        }
    }

    void FixedUpdate()
    {
        if (inventario.isActive == false && isMoving)
        {
            Vector2 currentPosition = rb.position;
            Vector2 newPos = Vector2.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);

            if (Vector2.Distance(newPos, targetPosition) < 0.001f)
            {
                rb.position = targetPosition;
                isMoving = false;
            }
        }
    }

    void DetectMovementInput()
    {
        inputDirection = Vector2.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) inputDirection = Vector2.up;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) inputDirection = Vector2.down;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) inputDirection = Vector2.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) inputDirection = Vector2.right;

        if (inputDirection != Vector2.zero)
        {
            Vector2 newTargetPosition = targetPosition + inputDirection * cellSize;

            // 👇 Solo cambiamos de celda si NO hay muro
            if (IsCellWalkable(newTargetPosition))
            {
                targetPosition = newTargetPosition;
                isMoving = true;
            }
            else
            {
                // Celda bloqueada: no empezamos movimiento
                isMoving = false;
            }
        }
    }

    bool IsCellWalkable(Vector2 cellPosition)
    {
        // Si NO hay ningún collider de la capa sólida en esa posición, se puede caminar
        Collider2D hit = Physics2D.OverlapCircle(cellPosition, collisionRadius, solidLayer);
        return hit == null;
    }

    void UpdateAnimations()
    {
        bool isMovingAnim = isMoving || inputDirection != Vector2.zero;

        if (inputDirection != Vector2.zero)
        {
            lastInputDirection = inputDirection;
        }

        animator.SetFloat("Horizontal", lastInputDirection.x);
        animator.SetFloat("Vertical", lastInputDirection.y);
        animator.SetBool("IsMoving", isMovingAnim);
    }

    void ActualizarOrientacionYGizmo()
    {
        if (attackCheck == null) return;

        if (spriteRenderer != null)
        {
            if (lastInputDirection.x > 0) spriteRenderer.flipX = true;
            else if (lastInputDirection.x < 0) spriteRenderer.flipX = false;
        }

        Vector2 gizmoOffset;
        if (Mathf.Abs(lastInputDirection.x) > Mathf.Abs(lastInputDirection.y))
            gizmoOffset = (lastInputDirection.x > 0) ? offsetRight : offsetLeft;
        else
            gizmoOffset = (lastInputDirection.y > 0) ? offsetUp : offsetDown;

        attackCheck.position = (Vector2)rb.position + gizmoOffset;
    }

    Vector2 SnapToGrid(Vector2 position)
    {
        float snappedX = Mathf.Round((position.x - gridOrigin.x) / cellSize) * cellSize + gridOrigin.x;
        float snappedY = Mathf.Round((position.y - gridOrigin.y) / cellSize) * cellSize + gridOrigin.y;
        return new Vector2(snappedX, snappedY);
    }

    bool IsOrthogonalMove(Vector2 newPosition)
    {
        Vector2 difference = newPosition - (Vector2)transform.position;
        return (Mathf.Abs(difference.x) > 0 && Mathf.Abs(difference.y) == 0) ||
               (Mathf.Abs(difference.y) > 0 && Mathf.Abs(difference.x) == 0);
    }

    public Vector2 GetLastInputDirection()
    {
        return lastInputDirection;
    }

    void OnDrawGizmosSelected()
    {
        if (attackCheck == null) return;
        Gizmos.DrawWireSphere(attackCheck.position, 0.2f);
    }

    public void setVelocidad(float velocidad)
    {
        moveSpeed = velocidad;
    }
}
