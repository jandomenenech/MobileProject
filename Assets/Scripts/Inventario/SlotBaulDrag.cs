using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Slot del inventario del baúl: permite arrastrar ítems al inventario del personaje
/// y recibir ítems desde él; también intercambiar entre slots del baúl.
/// </summary>
[RequireComponent(typeof(Image))]
public class SlotBaulDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public int slotIndex;
    public InventarioBaulGrafico inventarioBaulGrafico;

    private RectTransform ghostRect;
    private GameObject ghost;
    private RectTransform contenedorGhost;

    private Inventario Inv => inventarioBaulGrafico != null ? inventarioBaulGrafico.inventario : null;
    private BaulInteractuable Baul => Inv != null ? Inv.BaulAbierto : null;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Baul == null || inventarioBaulGrafico == null || inventarioBaulGrafico.celdas == null ||
            slotIndex < 0 || slotIndex >= inventarioBaulGrafico.celdas.Count)
            return;
        if (Baul.GetContenido(slotIndex) == null) return;

        Image slotImage = inventarioBaulGrafico.celdas[slotIndex];
        if (slotImage == null || slotImage.sprite == null) return;

        var canvas = inventarioBaulGrafico.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        RectTransform canvasRect = canvas.rootCanvas.GetComponent<RectTransform>();
        if (canvasRect == null) return;
        contenedorGhost = canvasRect;

        ghost = new GameObject("DragGhostBaul");
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
        if (eventData.pointerDrag == null || Baul == null || Inv == null) return;

        var origenSlot = eventData.pointerDrag.GetComponent<SlotInventarioDrag>();
        var origenBaul = eventData.pointerDrag.GetComponent<SlotBaulDrag>();
        var origenArmadura = eventData.pointerDrag.GetComponent<SlotArmaduraDrop>();

        if (origenArmadura != null && origenArmadura.inventarioGrafico != null)
        {
            Inv.EnviarArmaduraAlBaul(Baul, slotIndex);
            if (inventarioBaulGrafico != null) inventarioBaulGrafico.Refrescar();
            return;
        }
        if (origenSlot != null && origenSlot.inventarioGrafico != null)
        {
            Inv.EnviarItemAlBaul(Baul, origenSlot.slotIndex, slotIndex);
            if (inventarioBaulGrafico != null) inventarioBaulGrafico.Refrescar();
            return;
        }
        if (origenBaul != null && origenBaul != this)
        {
            Baul.IntercambiarSlots(origenBaul.slotIndex, slotIndex);
            if (inventarioBaulGrafico != null) inventarioBaulGrafico.Refrescar();
        }
    }
}
