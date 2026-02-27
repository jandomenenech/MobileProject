using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Permite arrastrar un objeto de inventario de un slot a otro manteniendo el click y soltando en el destino.
/// Se asigna a cada celda/slot del inventario (al mismo GameObject que tiene el Image del slot).
/// </summary>
[RequireComponent(typeof(Image))]
public class SlotInventarioDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Tooltip("Índice de este slot (0 = primer slot). Se asigna automáticamente desde InventarioGrafico.")]
    public int slotIndex;

    [Tooltip("Referencia al panel de inventario. Se asigna automáticamente desde InventarioGrafico.")]
    public InventarioGrafico inventarioGrafico;

    private RectTransform ghostRect;
    private GameObject ghost;
    private RectTransform contenedorGhost;

    private Inventario Inv => inventarioGrafico != null ? inventarioGrafico.inv : null;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Inv == null || Inv.inventario == null || slotIndex < 0 || slotIndex >= Inv.inventario.Count)
            return;
        if (Inv.inventario[slotIndex] == null)
            return;

        if (inventarioGrafico == null || inventarioGrafico.celdas == null || slotIndex >= inventarioGrafico.celdas.Count)
            return;

        Image slotImage = inventarioGrafico.celdas[slotIndex];
        if (slotImage == null || slotImage.sprite == null)
            return;

        RectTransform slotRect = slotImage.rectTransform;
        var canvas = inventarioGrafico.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        RectTransform canvasRect = canvas.rootCanvas.GetComponent<RectTransform>();
        if (canvasRect == null) return;
        contenedorGhost = canvasRect;

        ghost = new GameObject("DragGhost");
        ghost.transform.SetParent(canvasRect, false);
        ghost.transform.SetAsLastSibling();

        var ghostImage = ghost.AddComponent<Image>();
        ghostImage.sprite = slotImage.sprite;
        ghostImage.color = new Color(1f, 1f, 1f, 0.85f);
        ghostImage.raycastTarget = false;

        ghostRect = ghost.GetComponent<RectTransform>();
        if (ghostRect == null) ghostRect = ghost.AddComponent<RectTransform>();
        ghostRect.anchorMin = new Vector2(0.5f, 0.5f);
        ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
        ghostRect.pivot = new Vector2(0.5f, 0.5f);

        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);
        Vector2 min = canvasRect.InverseTransformPoint(corners[0]);
        Vector2 max = canvasRect.InverseTransformPoint(corners[2]);
        ghostRect.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));

        ActualizarPosicionGhost(eventData);
    }

    void ActualizarPosicionGhost(PointerEventData eventData)
    {
        if (ghostRect == null || contenedorGhost == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            contenedorGhost,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);
        ghostRect.anchoredPosition = localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        ActualizarPosicionGhost(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghost != null)
        {
            // Si soltamos sobre otro slot, OnDrop ya hizo el intercambio; no soltar al mundo
            bool soltadoEnOtroSlot = false;
            if (eventData.pointerCurrentRaycast.isValid && eventData.pointerCurrentRaycast.gameObject != null)
            {
                var targetSlot = eventData.pointerCurrentRaycast.gameObject.GetComponent<SlotInventarioDrag>();
                var targetBaul = eventData.pointerCurrentRaycast.gameObject.GetComponent<SlotBaulDrag>();
                if ((targetSlot != null && targetSlot != this) || targetBaul != null)
                    soltadoEnOtroSlot = true;
            }
            if (!soltadoEnOtroSlot && Inv != null)
                Inv.SoltarObjetoEnMundo(slotIndex);

            Destroy(ghost);
            ghost = null;
            ghostRect = null;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        var origenSlot = eventData.pointerDrag.GetComponent<SlotInventarioDrag>();
        var origenArmadura = eventData.pointerDrag.GetComponent<SlotArmaduraDrop>();
        var origenBaul = eventData.pointerDrag.GetComponent<SlotBaulDrag>();

        if (origenArmadura != null && Inv != null)
        {
            Inv.MoverArmaduraAGridSlot(slotIndex);
            return;
        }
        if (origenBaul != null && Inv != null && Inv.BaulAbierto != null)
        {
            Inv.RecibirItemDesdeBaul(Inv.BaulAbierto, origenBaul.slotIndex, slotIndex);
            return;
        }
        if (origenSlot == null || origenSlot == this) return;
        if (origenSlot.slotIndex == slotIndex) return;
        if (Inv == null || Inv.inventario == null) return;
        Inv.MoverObjetoEntreSlots(origenSlot.slotIndex, slotIndex);
    }
}
