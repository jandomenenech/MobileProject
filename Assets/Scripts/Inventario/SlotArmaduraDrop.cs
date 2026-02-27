using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Slot especial "Slot armadura": acepta objetos de tipo armadura arrastrados desde el inventario
/// y permite arrastrar la armadura de vuelta a un slot del grid.
/// </summary>
[RequireComponent(typeof(Image))]
public class SlotArmaduraDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public InventarioGrafico inventarioGrafico;

    private RectTransform ghostRect;
    private GameObject ghost;
    private RectTransform contenedorGhost;

    private Inventario Inv => inventarioGrafico != null ? inventarioGrafico.inv : null;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null || Inv == null) return;
        var origen = eventData.pointerDrag.GetComponent<SlotInventarioDrag>();
        if (origen == null) return;
        if (Inv.inventario == null || origen.slotIndex < 0 || origen.slotIndex >= Inv.inventario.Count) return;
        GameObject obj = Inv.inventario[origen.slotIndex];
        if (obj == null || !Inventario.EsObjetoArmadura(obj)) return;
        UnityEngine.Debug.Log("[SlotArmadura] Drop recibido: poniendo armadura en slot.");
        Inv.PonerEnSlotArmadura(origen.slotIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Inv == null || Inv.slotArmadura == null) return;
        if (inventarioGrafico == null || inventarioGrafico.slotArmaduraImage == null) return;
        Image slotImage = inventarioGrafico.slotArmaduraImage;
        if (slotImage == null || slotImage.sprite == null) return;

        var canvas = inventarioGrafico.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        RectTransform canvasRect = canvas.rootCanvas.GetComponent<RectTransform>();
        if (canvasRect == null) return;
        contenedorGhost = canvasRect;

        ghost = new GameObject("DragGhostArmadura");
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
        RectTransformUtility.ScreenPointToLocalPointInRectangle(contenedorGhost, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
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
            if (eventData.pointerCurrentRaycast.isValid && eventData.pointerCurrentRaycast.gameObject != null)
            {
                var targetSlot = eventData.pointerCurrentRaycast.gameObject.GetComponent<SlotInventarioDrag>();
                if (targetSlot != null && Inv != null)
                    Inv.MoverArmaduraAGridSlot(targetSlot.slotIndex);
            }
            Destroy(ghost);
            ghost = null;
            ghostRect = null;
        }
    }
}
