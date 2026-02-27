using UnityEngine;

/// <summary>
/// Hace que este SpriteRenderer se ordene por delante o por detrás del jugador
/// según si el jugador está en la misma / casilla superior o en la casilla inferior.
/// Pensado para arbustos, hierba alta, etc.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class OrdenarPorPosicionDelJugador : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform jugador;
    [SerializeField] private SpriteRenderer jugadorRenderer;
    [Tooltip("Componente MovimientoPorCeldas del jugador (se busca automáticamente si se deja vacío).")]
    [SerializeField] private MovimientoPorCeldas movimientoJugador;

    [Header("Sprites según celda")]
    [Tooltip("Sprite por defecto (cuando el jugador NO está en la misma casilla base). Si se deja vacío se usa el sprite inicial.")]
    [SerializeField] private Sprite spritePorDefecto;
    [Tooltip("Sprite cuando el jugador está en la MISMA casilla base que esta hierba.")]
    [SerializeField] private Sprite spriteJugadorEnMismaCelda;

    [Header("Celda de referencia de este objeto")]
    [Tooltip("Posición del pivot del sprite (normalmente la posición del GameObject).")]
    [SerializeField] private Vector2 centroCelda = Vector2.zero;
    [Tooltip("Altura del sprite medida en número de casillas (1 = ocupa una casilla de alto, 2 = dos casillas, etc.).")]
    [SerializeField] private int alturaEnCeldas = 1;
    [Tooltip("Tamaño de una casilla en unidades de mundo (debería coincidir con cellSize de MovimientoPorCeldas, normalmente 1).")]
    [SerializeField] private float cellSize = 1f;
    [Tooltip("Tolerancia vertical para considerar que jugador y hierba están en la misma casilla (en unidades de mundo).")]
    [SerializeField] private float toleranciaMismaFila = 0.01f;

    [Header("Offsets de orden respecto al jugador")]
    [Tooltip("Cuánto se suma al sortingOrder del jugador cuando está en la MISMA casilla o por ENCIMA (el jugador debe quedar DETRÁS).")]
    [SerializeField] private int offsetCuandoJugadorArribaOMisma = +1;

    [Tooltip("Cuánto se suma al sortingOrder del jugador cuando está en la casilla INFERIOR (el jugador debe quedar DELANTE).")]
    [SerializeField] private int offsetCuandoJugadorAbajo = -1;

    private SpriteRenderer _sr;
    private bool _estaEnMismaCelda;

    void Reset()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (spritePorDefecto == null && _sr != null)
            spritePorDefecto = _sr.sprite;

        if (centroCelda == Vector2.zero)
            centroCelda = transform.position;

        if (jugador == null)
        {
            movimientoJugador = FindObjectOfType<MovimientoPorCeldas>();
            if (movimientoJugador != null) jugador = movimientoJugador.transform;
        }
        if (jugador != null && jugadorRenderer == null)
            jugadorRenderer = jugador.GetComponentInChildren<SpriteRenderer>();
    }

    void Awake()
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (spritePorDefecto == null && _sr != null)
            spritePorDefecto = _sr.sprite;

        if (centroCelda == Vector2.zero)
            centroCelda = transform.position;

        if (jugador == null || movimientoJugador == null)
        {
            movimientoJugador = FindObjectOfType<MovimientoPorCeldas>();
            if (movimientoJugador != null)
                jugador = movimientoJugador.transform;
        }

        if (jugador != null && jugadorRenderer == null)
            jugadorRenderer = jugador.GetComponentInChildren<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (_sr == null) return;

        // Obtenemos la Y de la celda ACTUAL del jugador usando el mismo sistema que el movimiento por celdas.
        float yJugador;
        Vector2 celdaJugador;
        if (movimientoJugador != null)
        {
            celdaJugador = movimientoJugador.GetPosicionCeldaActual();
            yJugador = celdaJugador.y;
        }
        else if (jugador != null)
        {
            celdaJugador = jugador.position;
            yJugador = celdaJugador.y;
        }
        else
        {
            return;
        }

        // Calculamos la Y de la CASILLA BASE de esta hierba.
        // Si el sprite mide varias casillas de alto y el pivot está centrado,
        // la casilla base está por debajo del pivot.
        float yBaseHierba = centroCelda.y;
        if (alturaEnCeldas > 1)
        {
            // Ejemplo: alturaEnCeldas = 2 => desplazamiento = 0.5 * cellSize hacia abajo.
            float desplazamiento = (alturaEnCeldas - 1) * 0.5f * cellSize;
            yBaseHierba -= desplazamiento;
        }

        int offset;

        // Jugador en casilla INFERIOR (Y menor) -> hierba por DEBAJO del jugador.
        if (yJugador < yBaseHierba - toleranciaMismaFila)
        {
            offset = offsetCuandoJugadorAbajo;
        }
        else
        {
            // Misma casilla (dentro de la tolerancia) o casilla SUPERIOR -> hierba por ENCIMA del jugador.
            offset = offsetCuandoJugadorArribaOMisma;
        }

        // Aseguramos que usamos la misma sorting layer que el jugador, si la tenemos
        if (jugadorRenderer != null)
        {
            _sr.sortingLayerID = jugadorRenderer.sortingLayerID;
            _sr.sortingOrder = jugadorRenderer.sortingOrder + offset;
        }
        else
        {
            // Si no hay referencia al renderer del jugador, al menos aplicamos el offset sobre nuestro propio order
            _sr.sortingOrder = offset;
        }

        // --- Cambio de sprite según si el jugador está en la MISMA casilla base ---
        if (spriteJugadorEnMismaCelda != null)
        {
            // Consideramos misma casilla base si la posición de celda del jugador coincide en X
            // con el centro de la hierba y en Y con la casilla base, dentro de una pequeña tolerancia.
            bool mismaCelda =
                Mathf.Abs(celdaJugador.x - centroCelda.x) <= cellSize * 0.1f &&
                Mathf.Abs(celdaJugador.y - yBaseHierba) <= toleranciaMismaFila;

            if (mismaCelda && !_estaEnMismaCelda)
            {
                _estaEnMismaCelda = true;
                _sr.sprite = spriteJugadorEnMismaCelda;
            }
            else if (!mismaCelda && _estaEnMismaCelda)
            {
                _estaEnMismaCelda = false;
                if (spritePorDefecto != null)
                    _sr.sprite = spritePorDefecto;
            }
        }
    }
}

