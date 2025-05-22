using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;


public class MovimientoPorCeldas : MonoBehaviour
{
    public float cellSize = 1.0f; // Tamaño de cada casilla en Unity
    public float moveSpeed = 5.0f; // Velocidad de movimiento entre casillas
    public Vector2 gridOrigin = new Vector2(-0.5f, 1.2f); // Origen de la grilla (posición inicial del personaje)

    private Vector2 targetPosition; // Posición objetivo del personaje
    private Animator animator; // Referencia al Animator
    private Vector2 movementDirection; // Dirección del movimiento actual
    private bool isMoving = false; // Controla si el personaje está en movimiento
    private Vector2 inputDirection; // Dirección de entrada del jugador


    private Inventario inventario;

    void Start()                                                                                                                                              
    {
        // Configura la posición inicial como el centro de la primera casilla
        targetPosition = SnapToGrid(transform.position);
        transform.position = targetPosition; // Alinea al personaje al centro de la casilla inicial

        inventario = GetComponent<Inventario>();
        // Obtén el Animator
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (inventario.isActive == false)
        { 
            // Mueve al personaje hacia la posición objetivo
            Vector2 currentPosition = transform.position;
            transform.position = Vector2.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.deltaTime);

            // Si ha llegado a la celda objetivo, permitir nuevo movimiento
            if ((Vector2)transform.position == targetPosition)
            {
                isMoving = false;
                DetectMovementInput(); // Detecta la siguiente entrada de movimiento
            }

            // Actualiza la dirección del movimiento
            movementDirection = targetPosition - currentPosition;
            UpdateAnimations();
        }
        
    }

    void DetectMovementInput()
    {
        // Detecta la entrada de movimiento continuo
        inputDirection = Vector2.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) inputDirection = Vector2.up;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) inputDirection = Vector2.down;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) inputDirection = Vector2.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) inputDirection = Vector2.right;

        if (inputDirection != Vector2.zero)
        {
            Vector2 newTargetPosition = targetPosition + inputDirection * cellSize;

            if (IsOrthogonalMove(newTargetPosition)) // Solo mueve si es válido
            {
                targetPosition = newTargetPosition;
                isMoving = true;
            }
        }
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

    void UpdateAnimations()
    {
        // Si el personaje se está moviendo, actualizar animaciones
        bool isMovingAnim = isMoving || inputDirection != Vector2.zero;

        animator.SetFloat("Horizontal", inputDirection.x);
        animator.SetFloat("Vertical", inputDirection.y);
        animator.SetBool("IsMoving", isMovingAnim);
    }
}





