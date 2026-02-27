using UnityEngine;

/// <summary>
/// Crea/ajusta un BoxCollider2D para que bloquee SOLO la casilla base
/// de un sprite que ocupa varias celdas de alto (baúles, rocas, árboles, etc.).
/// Funciona con el sistema de MovimientoPorCeldas porque usa las mismas capas de colisión (MapCollider1, etc.).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class BloquearCeldaBase : MonoBehaviour
{
    [Header("Dimensiones en celdas")]
    [Tooltip("Altura del sprite en número de celdas (1 = 1 casilla, 2 = dos casillas de alto, etc.).")]
    [SerializeField] private int alturaEnCeldas = 1;

    [Tooltip("Tamaño de una casilla en unidades de mundo (debería coincidir con cellSize de MovimientoPorCeldas, normalmente 1).")]
    [SerializeField] private float cellSize = 1f;

    [Header("Layer para bloqueo")]
    [Tooltip("Si está activado, intenta poner este objeto en la capa 'MapCollider1' automáticamente.")]
    [SerializeField] private bool asignarLayerMapCollider1 = true;

    private BoxCollider2D _col;

    void Reset()
    {
        _col = GetComponent<BoxCollider2D>();
        ConfigurarCollider();
    }

    void OnValidate()
    {
        _col = GetComponent<BoxCollider2D>();
        if (!Application.isPlaying)
            ConfigurarCollider();
    }

    void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        ConfigurarCollider();
    }

    private void ConfigurarCollider()
    {
        if (_col == null) return;

        // Queremos una caja de 1x1 (cellSize x cellSize) que represente SOLO la casilla base.
        _col.isTrigger = false;
        _col.size = new Vector2(cellSize, cellSize);

        // Si el sprite ocupa varias celdas de alto y el pivot está aproximadamente centrado,
        // la casilla base está por debajo del pivot: desplazamos el collider hacia abajo.
        float desplazamiento = 0f;
        if (alturaEnCeldas > 1)
        {
            // Ejemplo: alturaEnCeldas = 2 => desplazamiento = 0.5 * cellSize hacia abajo.
            desplazamiento = (alturaEnCeldas - 1) * 0.5f * cellSize;
        }

        _col.offset = new Vector2(0f, -desplazamiento);

        // Intentar asignar la capa correcta para que MovimientoPorCeldas lo trate como muro.
        if (asignarLayerMapCollider1)
        {
            int layer = LayerMask.NameToLayer("MapCollider1");
            if (layer >= 0)
            {
                gameObject.layer = layer;
            }
        }
    }
}

