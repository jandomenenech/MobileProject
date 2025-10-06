using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventarioEquipo : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public RawImage rawImage;
    public RectTransform areaInventario;
    public Inventario inventario;
    public int indexEnInventario;

    private Transform originalParent;
    private Vector2 originalPosition;
    private CanvasGroup canvasGroup;
    private bool dropExitoso = false;

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

        // Si no hubo drop exitoso y se soltó fuera del área del inventario
        if (!dropExitoso && !RectTransformUtility.RectangleContainsScreenPoint(areaInventario, Input.mousePosition))
        {
            Debug.Log("Soltar fuera del inventario");

            if (inventario != null && indexEnInventario >= 0 && indexEnInventario < inventario.inventario.Count)
            {
                GameObject objetoASoltar = inventario.inventario[indexEnInventario];

                if (objetoASoltar != null)
                {
                    objetoASoltar.transform.position = inventario.transform.position;
                    objetoASoltar.SetActive(true);

                    inventario.inventario[indexEnInventario] = null;
                    rawImage.texture = null;
                    rawImage.color = new Color(1f, 1f, 1f, 0f);

                    inventario.inv.imagenesInventario();
                }
            }
        }

        transform.position = originalPosition;
        dropExitoso = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventarioInteractivo slotOrigen = eventData.pointerDrag.GetComponent<InventarioInteractivo>();
        if (slotOrigen != null)
        {
            dropExitoso = true;

            bool indicesValidos =
                inventario != null &&
                slotOrigen.inventario != null &&
                indexEnInventario >= 0 && indexEnInventario < inventario.inventario.Count &&
                slotOrigen.indexEnInventario >= 0 && slotOrigen.indexEnInventario < slotOrigen.inventario.inventario.Count;

            if (indicesValidos)
            {
                // Intercambiar objetos reales (pueden ser null)
                GameObject tempObjeto = inventario.inventario[indexEnInventario];
                inventario.inventario[indexEnInventario] = slotOrigen.inventario.inventario[slotOrigen.indexEnInventario];
                slotOrigen.inventario.inventario[slotOrigen.indexEnInventario] = tempObjeto;

                // Intercambiar texturas visuales (pueden ser null)
                Texture tempTexture = rawImage.texture;
                rawImage.texture = slotOrigen.rawImage.texture;
                slotOrigen.rawImage.texture = tempTexture;

                // Intercambiar colores (para visibilidad del slot)
                Color tempColor = rawImage.color;
                rawImage.color = slotOrigen.rawImage.color;
                slotOrigen.rawImage.color = tempColor;

                inventario.inv.imagenesInventario();
            }
            else
            {
                Debug.LogWarning("Índices fuera de rango o inventarios no asignados.");
            }
        }
    }
}

