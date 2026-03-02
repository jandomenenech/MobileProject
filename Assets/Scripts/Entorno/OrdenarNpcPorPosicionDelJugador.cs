using UnityEngine;

/// <summary>
/// Ordena un NPC (como el conejo) por delante o por detrás del jugador
/// según si el jugador está en la casilla inferior o en la misma/superior.
/// Pensado para personajes que comparten la misma lógica de profundidad que
/// la hierba/arbustos, pero aplicados al propio SpriteRenderer del NPC.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class OrdenarNpcPorPosicionDelJugador : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private MovimientoPorCeldas jugadorMovimiento;
    [SerializeField] private SpriteRenderer jugadorRenderer;

    [Header("Parámetros de celda")]
    [Tooltip("Tamaño de una casilla en unidades de mundo (debería coincidir con cellSize de MovimientoPorCeldas, normalmente 1).")]
    [SerializeField] private float cellSize = 1f;
    [Tooltip("Tolerancia vertical para considerar que el jugador está en la casilla inferior (en unidades de mundo).")]
    [SerializeField] private float toleranciaMismaFila = 0.01f;

    private SpriteRenderer _sr;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();

        if (jugadorMovimiento == null)
            jugadorMovimiento = FindObjectOfType<MovimientoPorCeldas>();

        if (jugadorMovimiento != null && jugadorRenderer == null)
            jugadorRenderer = jugadorMovimiento.GetComponentInChildren<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (_sr == null || jugadorMovimiento == null || jugadorRenderer == null) return;

        // Celda actual del jugador usando la misma lógica de grid que el personaje.
        Vector2 celdaJugador = jugadorMovimiento.GetPosicionCeldaActual();
        float yJugador = celdaJugador.y;

        // Casilla de referencia del NPC.
        // Usamos el centro vertical del sprite, que se alinea mejor con la celda
        // con la que camina el conejo (NPCMovimientoAleatorio centra el sprite en la celda).
        float yBaseNpc = _sr.bounds.center.y;

        int offset;

        // Jugador en casilla INFERIOR (Y menor) -> NPC por DEBAJO del jugador.
        if (yJugador < yBaseNpc - toleranciaMismaFila)
        {
            offset = -1;
        }
        else
        {
            // Misma casilla (dentro de la tolerancia) o casilla SUPERIOR -> NPC por ENCIMA del jugador.
            offset = +1;
        }

        _sr.sortingLayerID = jugadorRenderer.sortingLayerID;
        _sr.sortingOrder = jugadorRenderer.sortingOrder + offset;
    }
}

