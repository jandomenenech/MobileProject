using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Debe ir en el GameObject "Menú crafteo slot descripción".
/// Muestra el título y la descripción del objeto seleccionado en el catálogo de crafteo.
/// Si no hay Text asignados, los crea como hijos automáticamente.
/// </summary>
public class CrafteoDescripcionSlot : MonoBehaviour
{
    [Tooltip("Fuente para título y descripción (asigna aquí la fuente 'pixeled').")]
    public Font fuentePixeled;

    [Tooltip("Texto donde se mostrará el título. Si está vacío se crea uno.")]
    public Text textoTitulo;

    [Tooltip("Texto donde se mostrará la descripción. Si está vacío se crea uno.")]
    public Text textoDescripcion;

    void Awake()
    {
        AsegurarTextos();
    }

    /// <summary>
    /// Asegura que existan textoTitulo y textoDescripcion: busca en hijos o los crea.
    /// </summary>
    private void AsegurarTextos()
    {
        if (textoTitulo != null && textoDescripcion != null) return;

        Text[] textos = GetComponentsInChildren<Text>(true);
        if (textos.Length > 0 && textoTitulo == null)
            textoTitulo = textos[0];
        if (textos.Length > 1 && textoDescripcion == null)
            textoDescripcion = textos[1];
        if (textos.Length == 1 && textoDescripcion == null)
            textoDescripcion = textos[0];

        if (textoTitulo == null)
            textoTitulo = CrearTextoHijo("Titulo", true);
        if (textoDescripcion == null)
            textoDescripcion = CrearTextoHijo("Descripcion", false);

        AplicarEstiloTexto(textoTitulo, true);
        AplicarEstiloTexto(textoDescripcion, false);
    }

    /// <summary>
    /// Crea un GameObject hijo con RectTransform y Text según los valores pedidos (título o descripción).
    /// </summary>
    private Text CrearTextoHijo(string nombre, bool esTitulo)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        if (esTitulo)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector3(0f, 0f, 0f);
            rt.offsetMin = new Vector2(0f, -5.768f);
            rt.offsetMax = new Vector2(0f, 0f);
        }
        else
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(0f, -2.384186e-07f);
            rt.offsetMax = new Vector2(0f, -5.768f);
        }
        rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);

        Text txt = go.AddComponent<Text>();
        txt.font = fuentePixeled != null ? fuentePixeled : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = esTitulo ? 2 : 1;
        txt.lineSpacing = 1f;
        txt.supportRichText = true;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.alignByGeometry = false;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Truncate;
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize = esTitulo ? 2 : 1;
        txt.resizeTextMaxSize = esTitulo ? 2 : 1;
        txt.color = esTitulo ? Color.white : new Color32(0xCF, 0xCF, 0x00, 0xFF);
        txt.raycastTarget = false;

        // Ajustar márgenes horizontales (Left = 1, Right = 1)
        ConfigurarMargenesHorizontales(rt);

        return txt;
    }

    /// <summary>
    /// Configura los márgenes horizontales del RectTransform a Left = 1 y Right = 1,
    /// manteniendo el anclaje vertical y los offsets Y actuales.
    /// </summary>
    private void ConfigurarMargenesHorizontales(RectTransform rt)
    {
        if (rt == null) return;

        // Asegurar estirado horizontal si no lo está ya.
        if (!Mathf.Approximately(rt.anchorMin.x, 0f) || !Mathf.Approximately(rt.anchorMax.x, 1f))
        {
            rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
            rt.anchorMax = new Vector2(1f, rt.anchorMax.y);
        }

        var offsetMin = rt.offsetMin;
        var offsetMax = rt.offsetMax;
        offsetMin.x = 1f;
        offsetMax.x = -1f;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    /// <summary>
    /// Aplica el estilo correcto a un Text ya existente (título o descripción).
    /// </summary>
    private void AplicarEstiloTexto(Text txt, bool esTitulo)
    {
        if (txt == null) return;
        RectTransform rt = txt.rectTransform;

        txt.font = fuentePixeled != null ? fuentePixeled : txt.font;
        txt.fontSize = esTitulo ? 2 : 1;
        txt.lineSpacing = 1f;
        txt.supportRichText = true;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.alignByGeometry = false;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Truncate;
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize = esTitulo ? 2 : 1;
        txt.resizeTextMaxSize = esTitulo ? 2 : 1;
        txt.color = esTitulo ? Color.white : new Color32(0xCF, 0xCF, 0x00, 0xFF);

        // Asegurar márgenes horizontales correctos también para textos ya existentes.
        ConfigurarMargenesHorizontales(rt);
    }

    /// <summary>
    /// Muestra el título y la descripción recibidos.
    /// </summary>
    public void Mostrar(string titulo, string descripcion)
    {
        AsegurarTextos();

        if (textoTitulo != null)
            textoTitulo.text = titulo ?? string.Empty;

        if (textoDescripcion != null)
            textoDescripcion.text = descripcion ?? string.Empty;
    }

    /// <summary>
    /// Limpia los textos (opcional, por si quieres vaciar el panel).
    /// </summary>
    public void Limpiar()
    {
        if (textoTitulo != null)
            textoTitulo.text = string.Empty;
        if (textoDescripcion != null)
            textoDescripcion.text = string.Empty;
    }
}

