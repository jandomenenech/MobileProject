using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Resolución en unidades del grid "Pixel" (1 pixel = 0.076923 unidades, 13×13 pixels = 1 celda).
/// El panel del inventario debe coincidir exactamente con lo que ve la cámara (221×124 pixels).
/// </summary>
public static class ResolucionPixelGrid
{
    public const float PixelSize = 0.076923f; // 1/13, tamaño de un pixel del grid Pixel
    public const int AnchoPixels = 221;
    public const int AltoPixels = 124;
    public static float OrthographicSize => (AltoPixels * PixelSize) * 0.5f; // 4.769231
}

public class InventarioGrafico : MonoBehaviour
{
    public List<Image> celdas;
    public Inventario inv;
    public GameObject o;

    [Tooltip("Slot de armadura (se rellena por nombre si esta vacio).")]
    public Image slotArmaduraImage;

    // Cache para no recrear Sprites cada vez
    static readonly Dictionary<Texture, Sprite> spriteCache = new Dictionary<Texture, Sprite>();

    void Awake()
    {
        AutoRellenarCeldasSiEstanVacias();
    }

    void Start()
    {
        AjustarTamanoPanelInventario();
        ConfigurarDragEnSlots();
        ConfigurarSlotArmadura();
    }

    void ConfigurarSlotArmadura()
    {
        BuscarSlotArmaduraSiFalta();
        if (slotArmaduraImage == null) return;
        var drop = slotArmaduraImage.GetComponent<SlotArmaduraDrop>();
        if (drop == null) drop = slotArmaduraImage.gameObject.AddComponent<SlotArmaduraDrop>();
        drop.inventarioGrafico = this;
    }

    /// <summary>
    /// Añade o configura SlotInventarioDrag en cada celda para permitir arrastrar objetos entre slots.
    /// </summary>
    void ConfigurarDragEnSlots()
    {
        if (celdas == null || celdas.Count == 0) return;
        for (int i = 0; i < celdas.Count; i++)
        {
            if (celdas[i] == null) continue;
            var slot = celdas[i].GetComponent<SlotInventarioDrag>();
            if (slot == null)
                slot = celdas[i].gameObject.AddComponent<SlotInventarioDrag>();
            slot.slotIndex = i;
            slot.inventarioGrafico = this;
        }
    }

    void OnEnable()
    {
        AjustarTamanoPanelInventario();
    }

    void OnRectTransformDimensionsChange()
    {
        AjustarTamanoPanelInventario();
    }

    /// <summary>
    /// Rellena siempre la lista de celdas buscando los RawImage de los slots
    /// dentro de un contenedor llamado "InventorySlots" (o, si no existe,
    /// usando todos los RawImage hijos). Así no hace falta arrastrar nada a mano.
    /// </summary>
    void AutoRellenarCeldasSiEstanVacias()
    {
        var nuevaLista = new List<Image>();

        // Intentar buscar en toda la jerarquía un objeto llamado "InventorySlots"
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

        foreach (Transform child in contenedor)
        {
            if (child.name.Contains("Armadura")) continue;
            var img = child.GetComponentInChildren<Image>(true);
            if (img != null)
                nuevaLista.Add(img);
        }

        celdas = nuevaLista;
        BuscarSlotArmaduraSiFalta();
    }

    void BuscarSlotArmaduraSiFalta()
    {
        if (slotArmaduraImage != null) return;
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            string n = t.name ?? "";
            bool esSlotArmadura = (n.IndexOf("Slot", System.StringComparison.OrdinalIgnoreCase) >= 0 && n.IndexOf("Armadura", System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (esSlotArmadura)
            {
                var img = t.GetComponentInChildren<Image>(true);
                if (img != null) { slotArmaduraImage = img; break; }
            }
        }
    }

    /// <summary>
    /// Ajusta el panel del inventario a 221×124 unidades de canvas y deja que el
    /// CanvasScaler (Scale With Screen Size) lo escale proporcionalmente a la resolución.
    /// </summary>
    void AjustarTamanoPanelInventario()
    {
        var rect = GetComponent<RectTransform>();
        if (rect == null) return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(ResolucionPixelGrid.AnchoPixels, ResolucionPixelGrid.AltoPixels);
        rect.localScale = Vector3.one;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void imagenesInventario()
    {
        if (inv == null || inv.inventario == null)
        {
            Debug.LogWarning("InventarioGrafico: referencia 'inv' nula o lista inventario nula.");
            return;
        }
        if (celdas == null || celdas.Count == 0)
        {
            Debug.LogWarning("InventarioGrafico: lista de celdas vacía.");
            return;
        }

        for (int i = 0; i < celdas.Count; i++)
        {
            if (celdas[i] == null) continue;

            if (i < inv.inventario.Count && inv.inventario[i] != null)
            {
                GameObject o = inv.inventario[i];
                ObjetoRecogible objeto = o.GetComponent<ObjetoRecogible>();
                if (objeto == null)
                {
                    Debug.LogWarning($"InventarioGrafico: el objeto '{o.name}' no tiene ObjetoRecogible.");
                    continue;
                }

                Sprite sprite = ObtenerSpriteDesdeTexto(objeto.textura);
                celdas[i].sprite = sprite;
                celdas[i].color = new Color(1f, 1f, 1f, sprite != null ? 1f : 0f);

                // Actualizar texto de cantidad SOLO si el objeto es acumulable.
                int cantidad = 0;
                if (objeto.categoria == CategoriaObjeto.Acumulable)
                    cantidad = inv.GetCantidadEnSlot(i);
                ActualizarTextoCantidad(celdas[i], cantidad);
            }
            else
            {
                celdas[i].sprite = null;
                celdas[i].color = new Color(1f, 1f, 1f, 0f);
                ActualizarTextoCantidad(celdas[i], 0);
            }
        }
        if (slotArmaduraImage != null)
        {
            if (inv.slotArmadura != null)
            {
                var ob = inv.slotArmadura.GetComponent<ObjetoRecogible>();
                if (ob != null)
                {
                    Sprite sprite = ObtenerSpriteDesdeTexto(ob.textura);
                    slotArmaduraImage.sprite = sprite;
                    slotArmaduraImage.color = new Color(1f, 1f, 1f, sprite != null ? 1f : 0f);
                }
            }
            else
            {
                slotArmaduraImage.sprite = null;
                slotArmaduraImage.color = new Color(1f, 1f, 1f, 0f);
            }
        }
    }

    Sprite ObtenerSpriteDesdeTexto(Texture textura)
    {
        return InventarioGrafico.ObtenerSpriteDesdeTextura(textura);
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
            txt.alignment = TextAnchor.LowerRight;      // abajo a la derecha
            txt.fontStyle = FontStyle.Bold;             // negrita
            txt.alignByGeometry = true;                 // Align by Geometry
            txt.color = Color.white;                    // blanco puro (FFFFFF)
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.resizeTextForBestFit = false;           // tamaño fijo
            txt.fontSize = 4;                           // Font Size 4

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

    /// <summary>Convierte una textura a Sprite (compartido con InventarioBaulGrafico).</summary>
    public static Sprite ObtenerSpriteDesdeTextura(Texture textura)
    {
        if (textura == null) return null;
        if (spriteCache.TryGetValue(textura, out var cached))
            return cached;
        var tex2D = textura as Texture2D;
        if (tex2D == null) return null;
        var rect = new Rect(0, 0, tex2D.width, tex2D.height);
        var sprite = Sprite.Create(tex2D, rect, new Vector2(0.5f, 0.5f), 100f);
        spriteCache[textura] = sprite;
        return sprite;
    }

    private void prueba()
    {
        foreach (Image t in celdas)
        {
            for(int i = 0; i < inv.inventario.Count; i++)
            {
                o = inv.inventario[i];
                if (o != null)
                {
                    ObjetoRecogible objeto = o.GetComponent<ObjetoRecogible>();
                    Sprite sprite = ObtenerSpriteDesdeTexto(objeto.textura);
                    celdas[i].sprite = sprite;
                    celdas[i].color = new Color(1f, 1f, 1f, sprite != null ? 1f : 0f);
                }
                else
                {
                    celdas[i].sprite = null;
                    celdas[i].color = new Color(1f, 1f, 1f, 0f);
                }
                o = null;
            }
            

        }
    }


}
