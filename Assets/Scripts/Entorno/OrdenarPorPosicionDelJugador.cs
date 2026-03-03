using UnityEngine;

/// <summary>
/// Sorting basado en Y (profundidad): Y más bajo = sortingOrder más alto = se renderiza delante.
/// Funciona universalmente contra jugador, NPCs y cualquier otra entidad con el mismo sistema.
/// Opcionalmente cambia el sprite cuando el jugador pisa la misma casilla (para arbustos, hierba, etc.).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class OrdenarPorPosicionDelJugador : MonoBehaviour
{
    [Header("Sprites según celda (solo jugador)")]
    [Tooltip("Sprite por defecto (cuando el jugador NO está en la misma casilla base). Si se deja vacío se usa el sprite inicial.")]
    [SerializeField] private Sprite spritePorDefecto;
    [Tooltip("Sprite cuando el jugador está en la MISMA casilla base que esta hierba.")]
    [SerializeField] private Sprite spriteJugadorEnMismaCelda;

    [Header("Celda de referencia de este objeto")]
    [Tooltip("Si está activo (por defecto), usa siempre la posición actual del objeto. Al duplicar, cada copia funcionará correctamente.")]
    [SerializeField] private bool usarPosicionActual = true;
    [SerializeField] private Vector2 centroCelda = Vector2.zero;
    [Tooltip("Altura del sprite medida en número de casillas (1 = ocupa una casilla de alto, 2 = dos casillas, etc.).")]
    [SerializeField] private int alturaEnCeldas = 1;
    [Tooltip("Tamaño de una casilla en unidades de mundo (debería coincidir con cellSize de MovimientoPorCeldas, normalmente 1).")]
    [SerializeField] private float cellSize = 1f;
    [Tooltip("Tolerancia vertical para considerar que jugador y hierba están en la misma casilla (en unidades de mundo).")]
    [SerializeField] private float toleranciaMismaFila = 0.01f;

    [Header("Profundidad Y")]
    [Tooltip("Multiplicador de precisión. Con 100, cada unidad Y da 100 valores de sortingOrder.")]
    [SerializeField] private int precisionOrden = 100;
    [Tooltip("Offset manual al sortingOrder (positivo = más al frente).")]
    [SerializeField] private int offsetOrden = 0;

    // Legacy fields: se mantienen para no romper la serialización de escenas existentes.
    [HideInInspector] [SerializeField] private Transform jugador;
    [HideInInspector] [SerializeField] private SpriteRenderer jugadorRenderer;
    [HideInInspector] [SerializeField] private int offsetCuandoJugadorArribaOMisma = +1;
    [HideInInspector] [SerializeField] private int offsetCuandoJugadorAbajo = -1;
    [HideInInspector] [SerializeField] private bool usarCentroSprite = false;

    private MovimientoPorCeldas _movimientoJugador;
    private SpriteRenderer _sr;
    private bool _estaEnMismaCelda;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (spritePorDefecto == null && _sr != null)
            spritePorDefecto = _sr.sprite;

        if (centroCelda == Vector2.zero)
            centroCelda = transform.position;

        _movimientoJugador = FindObjectOfType<MovimientoPorCeldas>();
        if (_movimientoJugador != null && jugadorRenderer == null)
            jugadorRenderer = _movimientoJugador.GetComponentInChildren<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (_sr == null) return;

        Vector2 centro = usarPosicionActual ? (Vector2)transform.position : centroCelda;
        float yBaseHierba = centro.y;
        if (alturaEnCeldas > 1)
            yBaseHierba -= (alturaEnCeldas - 1) * 0.5f * cellSize;

        // Forzar a usar la misma Sorting Layer que el jugador, como antes,
        // para que no cambie el comportamiento respecto a otros layers del escenario.
        if (_movimientoJugador != null && jugadorRenderer == null)
            jugadorRenderer = _movimientoJugador.GetComponentInChildren<SpriteRenderer>();
        if (jugadorRenderer != null)
            _sr.sortingLayerID = jugadorRenderer.sortingLayerID;

        _sr.sortingOrder = -Mathf.RoundToInt(yBaseHierba * precisionOrden) + offsetOrden;

        // --- Cambio de sprite cuando el JUGADOR pisa la misma casilla base ---
        if (spriteJugadorEnMismaCelda == null || _movimientoJugador == null) return;

        Vector2 celdaJugador = _movimientoJugador.GetPosicionCeldaActual();
        bool mismaCelda =
            Mathf.Abs(celdaJugador.x - centro.x) <= cellSize * 0.1f &&
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

