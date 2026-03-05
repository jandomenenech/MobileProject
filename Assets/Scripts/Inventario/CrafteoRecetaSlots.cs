using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Se coloca en el GameObject "Menú crafte slots" (contenedor con un slot por cada ingrediente).
/// Muestra en cada slot el icono del ingrediente y la cantidad necesaria.
/// Si se asigna inventarioJugador, las cantidades se muestran en blanco si tienes suficiente y en rojo si te faltan.
/// </summary>
public class CrafteoRecetaSlots : MonoBehaviour
{
    [Tooltip("Slots donde se muestran los ingredientes. Si está vacío se usan los hijos directos.")]
    public List<Image> slots = new List<Image>();

    [Tooltip("Inventario del jugador. Si está asignado, las cantidades se pintan en rojo cuando faltan y en blanco cuando tienes suficiente.")]
    public Inventario inventarioJugador;

    [Tooltip("Catálogo de crafteo. Si está asignado, los colores se actualizan al cambiar el inventario (soltar/recoger ítems).")]
    public CrafteoCatalogoGrid catalogo;

    private float _ultimoRefresh;
    private const float IntervaloRefresh = 0.25f;

    void Awake()
    {
        if (slots == null || slots.Count == 0)
        {
            slots = new List<Image>();
            foreach (Transform child in transform)
            {
                Image img = child.GetComponent<Image>();
                if (img != null)
                    slots.Add(img);
            }
        }
    }

    /// <summary>
    /// Rellena los slots con los ingredientes de la receta (icono + cantidad).
    /// Si inventarioJugador está asignado, pinta la cantidad en blanco (suficiente) o rojo (faltan).
    /// </summary>
    public void MostrarReceta(List<CrafteoCatalogoGrid.IngredienteReceta> receta)
    {
        if (receta == null)
        {
            LimpiarSlots();
            return;
        }

        if (inventarioJugador == null)
            inventarioJugador = FindFirstObjectByType<Inventario>();

        for (int i = 0; i < (slots != null ? slots.Count : 0); i++)
        {
            Image slot = slots[i];
            if (slot == null) continue;

            if (i < receta.Count)
            {
                CrafteoCatalogoGrid.IngredienteReceta ing = receta[i];
                slot.sprite = ing != null ? ing.icono : null;
                slot.enabled = ing != null && ing.icono != null;
                slot.color = slot.enabled ? Color.white : new Color(1f, 1f, 1f, 0f);

                int cantidad = ing != null ? ing.cantidad : 0;
                bool suficiente = true;
                if (inventarioJugador != null && ing != null && ing.textura != null)
                {
                    int disponible = ContarItemPorTextura(ing.textura);
                    suficiente = disponible >= ing.cantidad;
                }
                ActualizarCantidad(slot.transform, cantidad, suficiente);
            }
            else
            {
                slot.sprite = null;
                slot.enabled = false;
                slot.color = new Color(1f, 1f, 1f, 0f);
                ActualizarCantidad(slot.transform, 0, true);
            }
        }
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        if (catalogo == null)
            catalogo = FindFirstObjectByType<CrafteoCatalogoGrid>();
        if (catalogo == null) return;

        if (Time.unscaledTime - _ultimoRefresh < IntervaloRefresh) return;
        _ultimoRefresh = Time.unscaledTime;

        var entrada = catalogo.GetEntradaSeleccionada();
        if (entrada != null && entrada.receta != null && entrada.receta.Count > 0)
            MostrarReceta(entrada.receta);
    }

    private int ContarItemPorTextura(Texture textura)
    {
        if (inventarioJugador == null || textura == null || inventarioJugador.inventario == null) return 0;
        string nombreTextura = textura.name ?? string.Empty;
        int total = 0;
        for (int i = 0; i < inventarioJugador.inventario.Count; i++)
        {
            GameObject obj = inventarioJugador.inventario[i];
            if (obj == null) continue;
            var rec = obj.GetComponent<ObjetoRecogible>();
            if (rec == null || rec.textura == null) continue;

            bool mismo = (rec.textura == textura);
            if (!mismo && !string.IsNullOrEmpty(nombreTextura) && !string.IsNullOrEmpty(rec.textura.name))
                mismo = string.Equals(rec.textura.name, nombreTextura, System.StringComparison.OrdinalIgnoreCase);

            if (mismo)
                total += inventarioJugador.GetCantidadEnSlot(i);
        }
        return total;
    }

    /// <summary>
    /// Muestra vacío todos los slots.
    /// </summary>
    public void LimpiarSlots()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) continue;
            slots[i].sprite = null;
            slots[i].enabled = false;
            slots[i].color = new Color(1f, 1f, 1f, 0f);
            ActualizarCantidad(slots[i].transform, 0, true);
        }
    }

    private void ActualizarCantidad(Transform slotParent, int cantidad, bool suficiente = true)
    {
        Transform tCant = slotParent.Find("Cantidad");
        Text txt = tCant != null ? tCant.GetComponent<Text>() : slotParent.GetComponentInChildren<Text>(true);
        if (txt == null && cantidad > 0)
        {
            GameObject go = new GameObject("Cantidad");
            go.transform.SetParent(slotParent, false);
            go.transform.SetAsLastSibling();
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 4;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.LowerRight;
            txt.alignByGeometry = true;
            txt.color = suficiente ? Color.white : Color.red;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.resizeTextForBestFit = false;
        }
        if (txt != null)
        {
            txt.fontSize = 4;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.LowerRight;
            txt.alignByGeometry = true;
            txt.color = suficiente ? Color.white : Color.red;
            txt.text = cantidad > 0 ? cantidad.ToString() : string.Empty;
            txt.gameObject.SetActive(cantidad > 0);
        }
    }
}
