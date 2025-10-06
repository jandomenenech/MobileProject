using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovimientoPorCeldas : MonoBehaviour
{
    [Header("Grid")]
    public float cellSize = 1.0f;     // Tamaño de cada casilla
    public float moveSpeed = 5.0f;    // Velocidad de movimiento
    public Vector2 gridOrigin = new Vector2(-0.5f, 1.2f); // Posición inicial

    private Vector2 targetPosition;   // Posición objetivo
    private Animator animator;        // Animator del personaje
    private Vector2 movementDirection;// Dirección de movimiento
    private bool isMoving = false;    // ¿Está moviéndose?
    private Vector2 inputDirection;   // Entrada del jugador

    // >>> Mantén una dirección inicial por defecto
    private Vector2 lastInputDirection = Vector2.down; // Última dirección válida

    private Inventario inventario;

    [Header("Ataque / Gizmo")]
    [SerializeField] private Transform attackCheck;      // Asigna en el inspector
    [SerializeField] private SpriteRenderer spriteRenderer; // (Opcional) para flip del sprite

    // Offsets del gizmo por dirección
    [SerializeField] private Vector2 offsetRight = new Vector2(0.6f, 0f);
    [SerializeField] private Vector2 offsetLeft = new Vector2(-0.6f, 0f);
    [SerializeField] private Vector2 offsetUp = new Vector2(0f, 0.6f);
    [SerializeField] private Vector2 offsetDown = new Vector2(0f, -0.6f);

    void Start()
    {
        targetPosition = SnapToGrid(transform.position);
        transform.position = targetPosition;

        animator = GetComponent<Animator>();
        inventario = GetComponent<Inventario>();

        // Inicializa la orientación del gizmo/sprite
        ActualizarOrientacionYGizmo();
    }

    void Update()
    {
        if (inventario.isActive == false)
        {
            Vector2 currentPosition = transform.position;
            transform.position = Vector2.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.deltaTime);

            if ((Vector2)transform.position == targetPosition)
            {
                isMoving = false;
                DetectMovementInput();
            }

            movementDirection = targetPosition - currentPosition;

            UpdateAnimations();         // Mantiene animator en sync con lastInputDirection
            ActualizarOrientacionYGizmo(); // Mueve attackCheck hacia donde “mira”
        }
    }

    void DetectMovementInput()
    {
        inputDirection = Vector2.zero;

        // Nota: si pulsas varias teclas a la vez, la última línea evaluada puede sobrescribir.
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) inputDirection = Vector2.up;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) inputDirection = Vector2.down;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) inputDirection = Vector2.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) inputDirection = Vector2.right;

        if (inputDirection != Vector2.zero)
        {
            Vector2 newTargetPosition = targetPosition + inputDirection * cellSize;

            if (IsOrthogonalMove(newTargetPosition))
            {
                targetPosition = newTargetPosition;
                isMoving = true;
            }
        }
    }

    void UpdateAnimations()
    {
        bool isMovingAnim = isMoving || inputDirection != Vector2.zero;

        // Solo actualizamos la última dirección cuando hay entrada
        if (inputDirection != Vector2.zero)
        {
            lastInputDirection = inputDirection;
        }

        // Siempre usar la última dirección para las animaciones
        animator.SetFloat("Horizontal", lastInputDirection.x);
        animator.SetFloat("Vertical", lastInputDirection.y);
        animator.SetBool("IsMoving", isMovingAnim);
    }

    // --- NUEVO: Orientación del sprite y posición del gizmo ---
    void ActualizarOrientacionYGizmo()
    {
        if (attackCheck == null) return;

        // Flip horizontal opcional (solo si hay SpriteRenderer)
        if (spriteRenderer != null)
        {
            if (lastInputDirection.x > 0) spriteRenderer.flipX = true;
            else if (lastInputDirection.x < 0) spriteRenderer.flipX = false;
            // Si es vertical, dejamos el flip como esté
        }

        // Offset según la dirección dominante (horizontal vs vertical)
        Vector2 gizmoOffset;
        if (Mathf.Abs(lastInputDirection.x) > Mathf.Abs(lastInputDirection.y))
            gizmoOffset = (lastInputDirection.x > 0) ? offsetRight : offsetLeft;
        else
            gizmoOffset = (lastInputDirection.y > 0) ? offsetUp : offsetDown;

        attackCheck.position = (Vector2)transform.position + gizmoOffset;
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

    // Permite a otros scripts obtener la dirección actual
    public Vector2 GetLastInputDirection()
    {
        return lastInputDirection;
    }

    // (Opcional) Dibuja una ayuda visual en el editor
    void OnDrawGizmosSelected()
    {
        if (attackCheck == null) return;
        Gizmos.DrawWireSphere(attackCheck.position, 0.2f);
    }
}
