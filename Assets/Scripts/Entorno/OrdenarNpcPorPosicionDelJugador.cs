using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sorting basado en fila de grid para NPCs y criaturas.
/// Misma fórmula que el jugador:
///   sortingOrder = -fila * precisionOrden + offsetOrden
/// donde la fila sale de mapGrid.WorldToCell(posición del NPC).
/// No mira al jugador, solo a su propia fila.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class OrdenarNpcPorPosicionDelJugador : MonoBehaviour
{
    [Header("Profundidad Y (fila de grid)")]
    [Tooltip("Multiplicador de precisión (debe coincidir con el resto de entidades, normalmente 100).")]
    [SerializeField] private int precisionOrden = 100;
    [Tooltip("Offset de tipo para NPCs dentro de la fila. Usa 20 para coincidir con el jugador.")]
    [SerializeField] private int offsetOrden = 20;
    [Tooltip("Tamaño de celda (solo se usa si no hay Grid, normalmente 1).")]
    [SerializeField] private float cellSize = 1f;

    [Header("Grid (opcional)")]
    [Tooltip("Si se asigna, la fila se calcula usando este Grid (WorldToCell), igual que el jugador.")]
    [SerializeField] private Grid mapGrid;

    private SpriteRenderer _sr;

    private static readonly List<OrdenarNpcPorPosicionDelJugador> Instancias = new List<OrdenarNpcPorPosicionDelJugador>();
    public static IReadOnlyList<OrdenarNpcPorPosicionDelJugador> TodasLasInstancias => Instancias;

    // Posición de referencia para otros sistemas (hierba, arbustos).
    // Usamos la posición del transform (pies), coherente con el cálculo de fila.
    public Vector2 PosicionReferencia => (Vector2)transform.position;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        Instancias.Add(this);

        if (mapGrid == null)
        {
            var grids = FindObjectsOfType<Grid>();
            if (grids != null && grids.Length > 0)
            {
                // Prioridad: usar explícitamente el Grid llamado "Grid (2)" (mismo que el personaje).
                foreach (var g in grids)
                {
                    if (g != null && g.name == "Grid (2)")
                    {
                        mapGrid = g;
                        break;
                    }
                }

                // Fallback: si no encontramos "Grid (2)", usamos el primero.
                if (mapGrid == null)
                    mapGrid = grids[0];
            }
        }
    }

    void OnDestroy()
    {
        Instancias.Remove(this);
    }

    void LateUpdate()
    {
        if (_sr == null) return;

        // Para el sorting usamos la base del Collider2D si existe (celda base),
        // y si no, la posición del transform (pies del NPC).
        float xNpc;
        float yNpc;
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            Bounds b = col.bounds;
            xNpc = b.center.x;
            yNpc = b.min.y + 0.5f * cellSize;
        }
        else
        {
            xNpc = transform.position.x;
            yNpc = transform.position.y;
        }

        int fila;
        if (mapGrid != null)
        {
            Vector3Int cell = mapGrid.WorldToCell(new Vector3(xNpc, yNpc, 0f));
            fila = cell.y;
        }
        else
        {
            fila = Mathf.RoundToInt(yNpc / cellSize);
        }

        _sr.sortingOrder = -fila * precisionOrden + offsetOrden;
    }
}
