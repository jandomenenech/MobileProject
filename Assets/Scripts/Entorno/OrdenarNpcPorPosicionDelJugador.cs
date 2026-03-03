using UnityEngine;

/// <summary>
/// Sorting basado en Y para NPCs y criaturas.
/// Usa la misma fórmula que OrdenarPorPosicionDelJugador y MovimientoPorCeldas:
///   sortingOrder = -RoundToInt(yBase * precision)
/// Esto garantiza orden correcto contra jugador, arbustos y otros NPCs sin
/// necesidad de referenciar a ninguna entidad concreta.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class OrdenarNpcPorPosicionDelJugador : MonoBehaviour
{
    [Header("Profundidad Y")]
    [Tooltip("Multiplicador de precisión (debe coincidir con el resto de entidades, normalmente 100).")]
    [SerializeField] private int precisionOrden = 100;
    [Tooltip("Offset manual al sortingOrder (positivo = más al frente).")]
    [SerializeField] private int offsetOrden = 0;
    [Tooltip("Usa el centro del bounds del sprite (recomendado para NPCs cuyo pivot no coincide con el centro visual).")]
    [SerializeField] private bool usarCentroSprite = true;

    // Legacy fields: se mantienen para no romper la serialización.
    [HideInInspector] [SerializeField] private MovimientoPorCeldas jugadorMovimiento;
    [HideInInspector] [SerializeField] private SpriteRenderer jugadorRenderer;
    [HideInInspector] [SerializeField] private float cellSize = 1f;
    [HideInInspector] [SerializeField] private float toleranciaMismaFila = 0.01f;

    private SpriteRenderer _sr;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (_sr == null) return;

        float yBase = usarCentroSprite ? _sr.bounds.center.y : transform.position.y;
        _sr.sortingOrder = -Mathf.RoundToInt(yBase * precisionOrden) + offsetOrden;
    }
}

