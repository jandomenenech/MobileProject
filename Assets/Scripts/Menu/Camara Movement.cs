using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.RuleTile.TilingRuleOutput;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private GameObject player; // El jugador que seguir� la c�mara.
    private Vector3 offset;

    void Start()
    {
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


