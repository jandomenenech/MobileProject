using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private GameObject player; // El jugador que seguir� la c�mara.
    [Tooltip("Tama�o ortogr�fico al entrar en Play (el script lo aplica en Start).")]
    [SerializeField] private float orthographicSizeForzado = 8f;
    private Vector3 offset;

    void Start()
    {
        // Se fuerza el zoom de la c�mara al iniciar para evitar que el valor
        // cambie al entrar en Play.
        var cam = GetComponent<Camera>();
        if (cam != null && cam.orthographic)
            cam.orthographicSize = orthographicSizeForzado;

        // Calculamos la diferencia inicial entre la c�mara y el jugador.
        offset = transform.position - player.transform.position;
    }

    void Update()
    {
        // Actualizamos la posici�n de la c�mara directamente para que siga al jugador.
        FollowPlayer();
    }

    private void FollowPlayer()
    {
        // La posici�n de la c�mara se ajusta instant�neamente en cada frame.
        transform.position = player.transform.position + offset;
    }
}


