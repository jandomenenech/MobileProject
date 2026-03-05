using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Se coloca en el mismo GameObject que "Menú Crafteo Slot Pendiente" (el que tiene CrafteoSlotPendiente e Image).
/// Permite arrastrar el objeto del slot pendiente y soltarlo sobre un slot del inventario del jugador.
/// </summary>
[RequireComponent(typeof(Image))]
public class SlotPendienteDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("Referencia al slot pendiente. Si está vacío se busca en este GameObject.")]
    public CrafteoSlotPendiente slotPendiente;

    private RectTransform ghostRect;
    private GameObject ghost;
    private RectTransform contenedorGhost;

    void Awake()
    {
        if (slotPendiente == null)
            slotPendiente = GetComponent<CrafteoSlotPendiente>();
        if (slotPendiente == null)
            slotPendiente = GetComponentInParent<CrafteoSlotPendiente>();
        var img = GetComponent<Image>();
        if (img != null)
            img.raycastTarget = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slotPendiente == null || !slotPendiente.TieneObjeto()) return;

        Image slotImage = GetComponent<Image>();
        Sprite spriteParaGhost = (slotImage != null && slotImage.sprite != null) ? slotImage.sprite : (slotPendiente != null ? slotPendiente.GetSpritePendiente() : null);
        if (spriteParaGhost == null) return;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        RectTransform canvasRect = canvas.rootCanvas.GetComponent<RectTransform>();
        if (canvasRect == null) return;
        contenedorGhost = canvasRect;

        ghost = new GameObject("DragGhostPendiente");
        ghost.transform.SetParent(canvasRect, false);
        ghost.transform.SetAsLastSibling();

        var ghostImage = ghost.AddComponent<Image>();
        ghostImage.sprite = spriteParaGhost;
        ghostImage.color = new Color(1f, 1f, 1f, 0.85f);
        ghostImage.raycastTarget = false;

        // El Image ya añade RectTransform al GameObject; usarlo en lugar de AddComponent para evitar aviso y NRE.
        ghostRect = ghost.GetComponent<RectTransform>();
        if (ghostRect == null) return;
        ghostRect.anchorMin = new Vector2(0.5f, 0.5f);
        ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
        ghostRect.pivot = new Vector2(0.5f, 0.5f);

        RectTransform slotRect = slotImage != null ? slotImage.rectTransform : GetComponent<RectTransform>();
        if (slotRect == null) return;
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
            if (slotPendiente != null && slotPendiente.TieneObjeto())
            {
                // Buscar slot de inventario: primero el hit actual, luego RaycastAll por si el menú crafteo tapa el inventario.
                SlotInventarioDrag slotInv = null;
                if (eventData.pointerCurrentRaycast.isValid && eventData.pointerCurrentRaycast.gameObject != null)
                {
                    slotInv = eventData.pointerCurrentRaycast.gameObject.GetComponent<SlotInventarioDrag>();
                    if (slotInv == null)
                        slotInv = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<SlotInventarioDrag>();
                }
                if (slotInv == null && EventSystem.current != null)
                {
                    var results = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(eventData, results);
                    foreach (var r in results)
                    {
                        if (r.gameObject == null || r.gameObject == ghost) continue;
                        slotInv = r.gameObject.GetComponent<SlotInventarioDrag>();
                        if (slotInv == null) slotInv = r.gameObject.GetComponentInParent<SlotInventarioDrag>();
                        if (slotInv != null) break;
                    }
                }
                if (slotInv != null && slotInv.inventarioGrafico != null && slotInv.inventarioGrafico.inv != null)
                {
                    slotPendiente.RecogerAlInventarioEnSlot(slotInv.slotIndex);
                }
            }

            Destroy(ghost);
            ghost = null;
            ghostRect = null;
        }
    }
}
