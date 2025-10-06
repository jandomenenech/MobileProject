using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovimientoPersonaje : MonoBehaviour
{
    public float speed = 0.5f;
    private Vector2 targetPosition;
    private bool isMoving = false;

    // Tamaño de cada casilla en la cuadrícula
    public float gridSize = 0.13f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isMoving)
        {
            // Obtenemos la posición del mouse y la ajustamos a la cuadrícula
            Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            targetPosition = new Vector2(
                Mathf.Round(mouseWorldPosition.x / gridSize) * gridSize,
                Mathf.Round(mouseWorldPosition.y / gridSize) * gridSize
            );
            isMoving = true;
        }

        if (isMoving)
        {
            // Movemos el personaje hacia la posición objetivo
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            // Si hemos alcanzado la posición objetivo, dejamos de movernos
            if ((Vector2)transform.position == targetPosition)
            {
                isMoving = false;
                CentroPixel();
            }
        }
        void CentroPixel()
        {
            transform.position = new Vector2(
                Mathf.Round(transform.position.x / gridSize) * gridSize,
                Mathf.Round(transform.position.y / gridSize) * gridSize
                );
        }
    }
}

