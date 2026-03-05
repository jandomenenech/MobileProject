using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra en el "Menú Crafteo Slot Catálogo" el nombre del objeto crafteable seleccionado.
/// De momento solo tiene una entrada por defecto: "Hacha de piedra", pero la lista se puede
/// ampliar desde el Inspector y navegar llamando a Siguiente()/Anterior() desde botones.
/// </summary>
[RequireComponent(typeof(Image))]
public class CrafteoCatalogoSlot : MonoBehaviour
{
    [Tooltip("Lista de nombres de objetos que se pueden craftear.")]
    public List<string> nombresObjetos = new List<string>() { "Hacha de piedra" };

    [Tooltip("Índice del objeto actualmente seleccionado en la lista.")]
    public int indiceActual = 0;

    [Tooltip("Texto donde se mostrará el nombre del objeto seleccionado.")]
    public Text etiqueta;

    void Awake()
    {
        // Aseguramos al menos un elemento por defecto.
        if (nombresObjetos == null || nombresObjetos.Count == 0)
            nombresObjetos = new List<string> { "Hacha de piedra" };

        if (etiqueta == null)
        {
            // Intentar encontrar una etiqueta existente en hijos.
            etiqueta = GetComponentInChildren<Text>(true);

            // Si no existe ninguna, crear una nueva.
            if (etiqueta == null)
            {
                GameObject go = new GameObject("Etiqueta");
                go.transform.SetParent(transform, false);
                go.transform.SetAsLastSibling();

                etiqueta = go.AddComponent<Text>();
                etiqueta.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                etiqueta.alignment = TextAnchor.MiddleLeft;
                etiqueta.fontStyle = FontStyle.Bold;
                etiqueta.color = Color.white;
                etiqueta.raycastTarget = false;
                etiqueta.horizontalOverflow = HorizontalWrapMode.Overflow;
                etiqueta.verticalOverflow = VerticalWrapMode.Overflow;
                etiqueta.resizeTextForBestFit = false;
                etiqueta.fontSize = 6;

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = new Vector2(2f, 2f);
                rt.offsetMax = new Vector2(-2f, -2f);
            }
        }

        ClampIndice();
        ActualizarEtiqueta();
    }

    void OnValidate()
    {
        if (nombresObjetos == null || nombresObjetos.Count == 0)
            nombresObjetos = new List<string> { "Hacha de piedra" };

        ClampIndice();
    }

    void ClampIndice()
    {
        if (nombresObjetos == null || nombresObjetos.Count == 0)
        {
            indiceActual = 0;
            return;
        }
        if (indiceActual < 0) indiceActual = 0;
        if (indiceActual >= nombresObjetos.Count) indiceActual = nombresObjetos.Count - 1;
    }

    void ActualizarEtiqueta()
    {
        if (etiqueta == null) return;
        if (nombresObjetos == null || nombresObjetos.Count == 0)
        {
            etiqueta.text = string.Empty;
            return;
        }

        etiqueta.text = nombresObjetos[indiceActual];
    }

    /// <summary>
    /// Avanza al siguiente objeto del catálogo (pensado para llamarse desde un botón).
    /// </summary>
    public void Siguiente()
    {
        if (nombresObjetos == null || nombresObjetos.Count == 0) return;
        indiceActual = (indiceActual + 1) % nombresObjetos.Count;
        ActualizarEtiqueta();
    }

    /// <summary>
    /// Retrocede al objeto anterior del catálogo.
    /// </summary>
    public void Anterior()
    {
        if (nombresObjetos == null || nombresObjetos.Count == 0) return;
        indiceActual = (indiceActual - 1 + nombresObjetos.Count) % nombresObjetos.Count;
        ActualizarEtiqueta();
    }

    /// <summary>
    /// Devuelve el nombre del objeto actualmente seleccionado.
    /// </summary>
    public string GetNombreSeleccionado()
    {
        if (nombresObjetos == null || nombresObjetos.Count == 0) return string.Empty;
        ClampIndice();
        return nombresObjetos[indiceActual];
    }
}

