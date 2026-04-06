using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Entidad que ocupa una o más celdas del tilemap.
/// </summary>
public interface IOcupanteCeldas
{
    Grid GridReferencia { get; }

    /// <summary>Añade todas las celdas ocupadas (índices del grid de referencia). No limpia la lista.</summary>
    void AcumularCeldasOcupadas(List<Vector3Int> salida);

    /// <summary>True si esta entidad ocupa la celda indicada (mismo grid).</summary>
    bool OcupaCelda(Vector3Int celda);
}

/// <summary>
/// Comprueba si el centro de una celda está ocupado por otro personaje/NPC (incluye multicle da).
/// </summary>
public static class CeldaOcupacionUtil
{
    public static bool EstaCeldaOcupadaPorEntidad(Grid mapGrid, Vector2 cellCenterWorld, LayerMask capasMapaParaExcluirComoEntidad, Transform excluirRoot)
    {
        Vector2 checkPoint = cellCenterWorld;
        Vector3Int? queryCell = null;
        if (mapGrid != null)
        {
            queryCell = mapGrid.WorldToCell(cellCenterWorld);
            checkPoint = mapGrid.GetCellCenterWorld(queryCell.Value);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPoint, 0.3f);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit.transform.root == excluirRoot) continue;
            if (hit.isTrigger) continue;
            int layer = hit.gameObject.layer;
            if (((1 << layer) & capasMapaParaExcluirComoEntidad) != 0) continue;

            var ocupante = hit.GetComponentInParent<IOcupanteCeldas>();
            if (ocupante != null)
            {
                if (queryCell.HasValue && mapGrid != null && ocupante.GridReferencia == mapGrid)
                {
                    if (ocupante.OcupaCelda(queryCell.Value))
                        return true;
                }
                continue;
            }

            if (hit.GetComponent<MovimientoPorCeldas>() != null
                || hit.GetComponent<NPCMovimientoAleatorio>() != null)
                return true;
        }

        return false;
    }
}
