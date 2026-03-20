using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private GameObject player; // El jugador que seguirá la cámara.
    private Vector3 offset;

    void Start()
    {
        // Se fuerza el zoom de la cámara al iniciar para evitar que el valor
        // cambie al entrar en Play.
        var cam = GetComponent<Camera>();
        if (cam != null && cam.orthographic)
            cam.orthographicSize = 6f;

        // Calculamos la diferencia inicial entre la cámara y el jugador.
        offset = transform.position - player.transform.position;
    }

    void Update()
    {
        // Actualizamos la posición de la cámara directamente para que siga al jugador.
        FollowPlayer();
    }

    private void FollowPlayer()
    {
        // La posición de la cámara se ajusta instantáneamente en cada frame.
        transform.position = player.transform.position + offset;
    }
}


