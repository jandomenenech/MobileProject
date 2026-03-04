using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Slot del inventario del cadáver: permite arrastrar ítems al inventario del personaje
/// y recibir ítems desde él; también intercambiar entre slots del cadáver.
/// </summary>
[RequireComponent(typeof(Image))]
public class SlotCadaverDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public int slotIndex;
    public InventarioCadaverGrafico inventarioCadaverGrafico;

    private RectTransform ghostRect;
    private GameObject ghost;
    private RectTransform contenedorGhost;

    private Inventario Inv => inventarioCadaverGrafico != null ? inventarioCadaverGrafico.inventario : null;
    private CadaverInteractuable Cadaver => Inv != null ? Inv.CadaverAbierto : null;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Cadaver == null || inventarioCadaverGrafico == null || inventarioCadaverGrafico.celdas == null ||
            slotIndex < 0 || slotIndex >= inventarioCadaverGrafico.celdas.Count)
            return;
        if (Cadaver.GetContenido(slotIndex) == null) return;

        Image slotImage = inventarioCadaverGrafico.celdas[slotIndex];
        if (slotImage == null || slotImage.sprite == null) return;

        var canvas = inventarioCadaverGrafico.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        RectTransform canvasRect = canvas.rootCanvas.GetComponent<RectTransform>();
        if (canvasRect == null) return;
        contenedorGhost = canvasRect;

        ghost = new GameObject("DragGhostCadaver");
        ghost.transform.SetParent(canvasRect, false);
        ghost.transform.SetAsLastSibling();

        var ghostImage = ghost.AddComponent<Image>();
        ghostImage.sprite = slotImage.sprite;
        ghostImage.color = new Color(1f, 1f, 1f, 0.85f);
        ghostImage.raycastTarget = false;

        ghostRect = ghost.GetComponent<RectTransform>();
        if (ghostRect == null) ghostRect = ghost.AddComponent<RectTransform>();
        ghostRect.anchorMin = ghostRect.anchorMax = ghostRect.pivot = new Vector2(0.5f, 0.5f);

        Vector3[] corners = new Vector3[4];
        slotImage.rectTransform.GetWorldCorners(corners);
        Vector2 min = canvasRect.InverseTransformPoint(corners[0]);
        Vector2 max = canvasRect.InverseTransformPoint(corners[2]);
        ghostRect.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));

        ActualizarPosicionGhost(eventData);
    }

    void ActualizarPosicionGhost(PointerEventData eventData)
    {
        if (ghostRect == null || contenedorGhost == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            contenedorGhost, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
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
            Destroy(ghost);
            ghost = null;
            ghostRect = null;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null || Cadaver == null || Inv == null) return;

        var origenSlot = eventData.pointerDrag.GetComponent<SlotInventarioDrag>();
        var origenCadaver = eventData.pointerDrag.GetComponent<SlotCadaverDrag>();
        var origenArmadura = eventData.pointerDrag.GetComponent<SlotArmaduraDrop>();

        if (origenArmadura != null && origenArmadura.inventarioGrafico != null)
        {
            Inv.EnviarArmaduraAlCadaver(Cadaver, slotIndex);
            if (inventarioCadaverGrafico != null) inventarioCadaverGrafico.Refrescar();
            return;
        }
        if (origenSlot != null && origenSlot.inventarioGrafico != null)
        {
            Inv.EnviarItemAlCadaver(Cadaver, origenSlot.slotIndex, slotIndex);
            if (inventarioCadaverGrafico != null) inventarioCadaverGrafico.Refrescar();
            return;
        }
        if (origenCadaver != null && origenCadaver != this)
        {
            Cadaver.IntercambiarSlots(origenCadaver.slotIndex, slotIndex);
            if (inventarioCadaverGrafico != null) inventarioCadaverGrafico.Refrescar();
        }
    }
}
