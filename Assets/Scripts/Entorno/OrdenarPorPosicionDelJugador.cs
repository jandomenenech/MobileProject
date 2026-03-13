using UnityEngine;

/// <summary>
/// Sorting basado en fila de grid para objetos de entorno (hierba, arbustos, troncos).
/// Fórmula: sortingOrder = -fila * precisionOrden + offsetOrden
/// El offsetOrden (25 por defecto) es mayor que el del jugador (20) y NPC (10),
/// por lo que en la misma fila el entorno se renderiza POR DELANTE.
/// Si el jugador baja una fila, su base order sube +100 y pasa al frente automáticamente.
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

    [Header("Profundidad Y (fila de grid)")]
    [Tooltip("Multiplicador de precisión. Con 100, cada fila de grid da 100 valores de sortingOrder.")]
    [SerializeField] private int precisionOrden = 100;
    [Tooltip("Offset de tipo (positivo = más al frente). Hierba/Arbusto=25, Jugador=20, NPC=10.")]
    [SerializeField] private int offsetOrden = 25;
    [Tooltip("Si está activo, usa la base del Collider2D para calcular la fila (recomendado para árboles altos). Si se desactiva, usa centroCelda/alturaEnCeldas.")]
    [SerializeField] private bool usarBaseCollider = true;

    [Header("Grid (opcional)")]
    [Tooltip("Si se asigna, la fila se calcula usando este Grid (WorldToCell), igual que el jugador.")]
    [SerializeField] private Grid mapGrid;

    // Legacy fields: se mantienen para no romper la serialización de escenas existentes.
    [HideInInspector] [SerializeField] private Transform jugador;
    [HideInInspector] [SerializeField] private SpriteRenderer jugadorRenderer;
    [HideInInspector] [SerializeField] private int offsetCuandoJugadorArribaOMisma = +1;
    [HideInInspector] [SerializeField] private int offsetCuandoJugadorAbajo = -1;
    [HideInInspector] [SerializeField] private bool usarCentroSprite = false;
    [HideInInspector] [SerializeField] private int ordenFrente = 80;
    [HideInInspector] [SerializeField] private int ordenDetras = 79;

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

        if (mapGrid == null && _movimientoJugador != null)
        {
            // Intentamos reutilizar el mismo Grid que usa el jugador, si existe.
            var grids = FindObjectsOfType<Grid>();
            if (grids != null && grids.Length > 0)
                mapGrid = grids[0];
        }
    }

    void LateUpdate()
    {
        if (_sr == null) return;

        Vector2 centro = usarPosicionActual ? (Vector2)transform.position : centroCelda;

        float yBase;
        var col = GetComponent<Collider2D>();
        if (usarBaseCollider && col != null)
        {
            // Anclar a la base real del collider (punto de apoyo visual), útil para árboles altos.
            Bounds b = col.bounds;
            yBase = b.min.y + 0.5f * cellSize;
        }
        else
        {
            // Comportamiento original: usar centroCelda y alturaEnCeldas.
            yBase = centro.y;
            if (alturaEnCeldas > 1)
                yBase -= (alturaEnCeldas - 1) * 0.5f * cellSize;
        }

        int fila;
        if (mapGrid != null)
        {
            // Alineamos la fila con la rejilla real del mapa (mismas celdas que el jugador).
            Vector3Int cell = mapGrid.WorldToCell(new Vector3(centro.x, yBase, 0f));
            fila = cell.y;
        }
        else
        {
            fila = Mathf.RoundToInt(yBase / cellSize);
        }

        int orden = -fila * precisionOrden + offsetOrden;
        _sr.sortingOrder = orden;

        // --- Cambio de sprite cuando el JUGADOR pisa la misma casilla base ---
        if (spriteJugadorEnMismaCelda == null || _movimientoJugador == null) return;

        Vector2 celdaJugador = _movimientoJugador.GetPosicionCeldaActual();
        bool mismaCelda =
            Mathf.Abs(celdaJugador.x - centro.x) <= cellSize * 0.1f &&
            Mathf.Abs(celdaJugador.y - yBase) <= toleranciaMismaFila;

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
