using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovimientoPorCeldas : MonoBehaviour
{ 
    public float cellSize = 1.0f; // Tama�o de cada casilla en Unity
    public float moveSpeed = 5.0f; // Velocidad de movimiento entre casillas
    public Vector2 gridOrigin = new Vector2(-0.5f, 1.2f); // Origen de la grilla (posici�n inicial del personaje)

    private Vector2 targetPosition; // Posici�n objetivo del personaje
    private Animator animator; // Referencia al Animator
    private Vector2 movementDirection; // Direcci�n del movimiento actual

    void Start()
    {
        // Configura la posici�n inicial como el centro de la primera casilla
        targetPosition = SnapToGrid(transform.position);
        transform.position = targetPosition; // Alinea al personaje al centro de la casilla inicial

        // Obt�n el Animator
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Mueve al personaje hacia la posici�n objetivo
        Vector2 currentPosition = transform.position;
        transform.position = Vector2.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.deltaTime);

        // Actualiza la direcci�n del movimiento
        movementDirection = targetPosition - currentPosition;

        // Control de animaciones
        UpdateAnimations();

        // Detecta clics del mouse
        if (Input.GetMouseButtonDown(0)) // Clic izquierdo
        {
            // Obt�n la posici�n del mouse en el mundo
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Calcula la celda m�s cercana al clic
            Vector2 clickedGridPosition = SnapToGrid(mouseWorldPosition);

            // Solo permite movimientos ortogonales (no diagonales)
            if (IsOrthogonalMove(clickedGridPosition))
            {
                targetPosition = clickedGridPosition; // Mueve al personaje al centro de la casilla
            }
        }
    }

    Vector2 SnapToGrid(Vector2 position)
    {
        // Calcula el centro de la casilla m�s cercana basada en el origen de la grilla
        float snappedX = Mathf.Round((position.x - gridOrigin.x) / cellSize) * cellSize + gridOrigin.x;
        float snappedY = Mathf.Round((position.y - gridOrigin.y) / cellSize) * cellSize + gridOrigin.y;

        return new Vector2(snappedX, snappedY);
    }

    bool IsOrthogonalMove(Vector2 newPosition)
    {
        // Calcula la diferencia entre la posici�n actual y la nueva posici�n
        Vector2 difference = newPosition - (Vector2)transform.position;

        // Solo permite movimientos horizontales o verticales
        return (Mathf.Abs(difference.x) > 0 && Mathf.Abs(difference.y) == 0) ||
               (Mathf.Abs(difference.y) > 0 && Mathf.Abs(difference.x) == 0);
    }

    void UpdateAnimations()
    {
        // Comprueba si el personaje est� en movimiento
        bool isMoving = movementDirection.magnitude > 0.01f;

        // Env�a los par�metros al Animator
        animator.SetFloat("Horizontal", movementDirection.x);
        animator.SetFloat("Vertical", movementDirection.y);
        animator.SetBool("IsMoving", isMoving);
    }
}




