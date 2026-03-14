using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra el contenido de la hoguera abierta en un panel de slots y configura el drag & drop.
/// Asignar al GameObject del panel "Inventario Hoguera" y enlazar el Inventario del personaje.
/// </summary>
public class InventarioHogueraGrafico : MonoBehaviour
{
    public List<Image> celdas;
    [Tooltip("Inventario del personaje (para acceder a HogueraAbierto y mover ítems).")]
    public Inventario inventario;

    void Awake()
    {
        AutoRellenarCeldasSiEstanVacias();
        if (inventario == null)
            inventario = FindFirstObjectByType<Inventario>();
    }

    void Start()
    {
        AjustarTamanoPanel();
        DesactivarRaycastEnFondos();
        ConfigurarDragEnSlots();
    }

    void DesactivarRaycastEnFondos()
    {
        var graphic = GetComponent<UnityEngine.UI.Graphic>();
        if (graphic != null)
            graphic.raycastTarget = false;

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "InventorySlots")
            {
                var g = t.GetComponent<UnityEngine.UI.Graphic>();
                if (g != null)
                    g.raycastTarget = false;
                break;
            }
        }
    }

    void OnEnable()
    {
        AjustarTamanoPanel();
        Refrescar();
    }

    void OnRectTransformDimensionsChange()
    {
        AjustarTamanoPanel();
    }

    [Tooltip("Separación horizontal (panel a la derecha). 0 = no cambiar la posición.")]
    [SerializeField] private float separacionInventario = 0f;

    void AjustarTamanoPanel()
    {
        var rect = GetComponent<RectTransform>();
        if (rect == null) return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        if (separacionInventario > 0)
        {
            float offsetX = ResolucionPixelGrid.AnchoPixels * 0.5f + separacionInventario + ResolucionPixelGrid.AnchoPixels * 0.5f;
            rect.anchoredPosition = new Vector2(offsetX, 0f);
        }
        rect.sizeDelta = new Vector2(ResolucionPixelGrid.AnchoPixels, ResolucionPixelGrid.AltoPixels);
        rect.localScale = Vector3.one;
    }

    void AutoRellenarCeldasSiEstanVacias()
    {
        var nuevaLista = new List<Image>();

        Transform contenedor = null;
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "InventorySlots")
            {
                contenedor = t;
                break;
            }
        }
        if (contenedor == null) contenedor = transform;

        var imagenes = contenedor.GetComponentsInChildren<Image>(true);
        foreach (var img in imagenes)
        {
            if (img == null) continue;
            if (img.transform == contenedor) continue;
            nuevaLista.Add(img);
        }

        celdas = nuevaLista;
    }

    void ConfigurarDragEnSlots()
    {
        if (celdas == null || celdas.Count == 0) return;
        for (int i = 0; i < celdas.Count; i++)
        {
            if (celdas[i] == null) continue;
            celdas[i].raycastTarget = true;
            var slot = celdas[i].GetComponent<SlotHogueraDrag>();
            if (slot == null)
                slot = celdas[i].gameObject.AddComponent<SlotHogueraDrag>();
            slot.slotIndex = i;
            slot.inventarioHogueraGrafico = this;
        }
    }

    public void Refrescar()
    {
        if (inventario == null || inventario.HogueraAbierto == null || celdas == null) return;
        HogueraInteractuable hoguera = inventario.HogueraAbierto;
        for (int i = 0; i < celdas.Count; i++)
        {
            if (celdas[i] == null) continue;
            GameObject o = hoguera.GetContenido(i);
            if (o != null)
            {
                var ob = o.GetComponent<ObjetoRecogible>();
                if (ob != null)
                {
                    Sprite sprite = InventarioGrafico.ObtenerSpriteDesdeTextura(ob.textura);
                    celdas[i].sprite = sprite;
                    celdas[i].color = new Color(1f, 1f, 1f, sprite != null ? 1f : 0f);

                    int cantidad = 0;
                    if (ob.categoria == CategoriaObjeto.Acumulable)
                        cantidad = hoguera.GetCantidad(i);
                    ActualizarTextoCantidad(celdas[i], cantidad);
                }
                else
                {
                    celdas[i].sprite = null;
                    celdas[i].color = new Color(1f, 1f, 1f, 0f);
                    ActualizarTextoCantidad(celdas[i], 0);
                }
            }
            else
            {
                celdas[i].sprite = null;
                celdas[i].color = new Color(1f, 1f, 1f, 0f);
                ActualizarTextoCantidad(celdas[i], 0);
            }
        }
    }

    void ActualizarTextoCantidad(Image slotImage, int cantidad)
    {
        if (slotImage == null) return;

        Transform existente = slotImage.transform.Find("Cantidad");
        Text txt = existente != null ? existente.GetComponent<Text>() : null;

        if (txt == null)
        {
            GameObject go = new GameObject("Cantidad");
            go.transform.SetParent(slotImage.transform, false);
            go.transform.SetAsLastSibling();

            txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.alignment = TextAnchor.LowerRight;
            txt.fontStyle = FontStyle.Bold;
            txt.alignByGeometry = true;
            txt.color = Color.white;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.resizeTextForBestFit = false;
            txt.fontSize = 4;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        if (cantidad > 0)
        {
            txt.text = cantidad.ToString();
            txt.gameObject.SetActive(true);
        }
        else
        {
            txt.text = string.Empty;
            txt.gameObject.SetActive(false);
        }
    }
}
