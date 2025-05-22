using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventarioInteractivo : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public RawImage rawImage;
    private Transform originalParent;
    private Vector2 originalPosition;
    private CanvasGroup canvasGroup;
    public RectTransform areaInventario; 
    public Inventario inventario;
    public int indexEnInventario;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        canvasGroup = GetComponent<CanvasGroup>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalPosition = transform.position;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (!RectTransformUtility.RectangleContainsScreenPoint(areaInventario, Input.mousePosition))
        {
            Debug.Log("Soltar fuera del inventario");

            if (inventario != null && indexEnInventario < inventario.inventario.Count)
            {
                GameObject objetoASoltar = inventario.inventario[indexEnInventario];

                if (objetoASoltar != null)
                {
                    objetoASoltar.transform.position = inventario.transform.position;
                    objetoASoltar.SetActive(true);
                    inventario.inventario.RemoveAt(indexEnInventario);
                    rawImage.texture = null;
                    rawImage.color = new Color(1f, 1f, 1f, 0f);

                    inventario.inv.imagenesInventario(); 
                }
            }
        }
        else
        {
            transform.position = originalPosition;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventarioInteractivo slotOrigen = eventData.pointerDrag.GetComponent<InventarioInteractivo>();
        if (slotOrigen != null)
        {
            Texture temp = rawImage.texture;
            rawImage.texture = slotOrigen.rawImage.texture;
            slotOrigen.rawImage.texture = temp;

            float alphaTemp = rawImage.color.a;
            Color c1 = rawImage.color;
            Color c2 = slotOrigen.rawImage.color;

            rawImage.color = new Color(c2.r, c2.g, c2.b, c2.a);
            slotOrigen.rawImage.color = new Color(c1.r, c1.g, c1.b, c1.a);
        }
    }

}
