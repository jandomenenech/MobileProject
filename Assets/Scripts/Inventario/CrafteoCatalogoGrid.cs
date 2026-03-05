using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Se coloca en el GameObject que tiene el Grid Layout Group del catálogo de crafteo.
/// Rellena cada slot hijo con un sprite y permite seleccionar un slot para mostrar
/// la descripción en "Menú crafteo slot descripción".
/// </summary>
[RequireComponent(typeof(GridLayoutGroup))]
public class CrafteoCatalogoGrid : MonoBehaviour
{
    [System.Serializable]
    public class IngredienteReceta
    {
        [Tooltip("Sprite del ingrediente (ej: rama inventario, piedra).")]
        public Sprite icono;

        [Tooltip("Cantidad necesaria (ej: 10).")]
        public int cantidad = 1;

        [Tooltip("Textura del ObjetoRecogible de este ingrediente (para buscarlo en el inventario del jugador).")]
        public Texture textura;
    }

    [System.Serializable]
    public class EntradaCrafteo
    {
        [Tooltip("Icono que se mostrará en el slot (por ejemplo, el sprite del hacha).")]
        public Sprite icono;

        [Tooltip("Título de la receta (se muestra en el panel de descripción).")]
        public string titulo;

        [Tooltip("Descripción larga de la receta.")]
        [TextArea]
        public string descripcion;

        [Tooltip("Ingredientes necesarios (cada uno se muestra en un slot de 'Menú crafte slots' con su cantidad).")]
        public List<IngredienteReceta> receta = new List<IngredienteReceta>();

        [Tooltip("Prefab del objeto que se crea al craftear (ej: prefab del hacha 1).")]
        public GameObject prefabResultado;
    }

    [Tooltip("Entradas del catálogo; índice 0 = Slot 1 (Hacha 1).")]
    public List<EntradaCrafteo> entradas = new List<EntradaCrafteo>();

    [Tooltip("Referencia al panel 'Menú crafteo slot descripción'.")]
    public CrafteoDescripcionSlot descripcionSlot;

    [Tooltip("Referencia al contenedor 'Menú crafte slots' donde se muestran los ingredientes de la receta.")]
    public CrafteoRecetaSlots slotsReceta;

    [HideInInspector] public int indiceSeleccionado = 0;

    void Awake()
    {
        // Asegurar lista y una entrada por defecto para el hacha de piedra.
        if (entradas == null)
            entradas = new List<EntradaCrafteo>();
        if (entradas.Count == 0)
        {
            entradas.Add(new EntradaCrafteo
            {
                icono = null,
                titulo = "HACHA DE PIEDRA",
                descripcion = "HACHA DE PIEDRA. NO ES LA MEJOR HERRAMIENTO, PERO CUMPLE SU FUNCIÓN TALANDO",
                receta = new List<IngredienteReceta>
                {
                    new IngredienteReceta { cantidad = 10 }, // Rama inventario – asignar sprite en Inspector
                    new IngredienteReceta { cantidad = 10 }  // Piedra – asignar sprite en Inspector
                }
            });
        }

        if (descripcionSlot == null)
            BuscarDescripcionSlot();
        if (slotsReceta == null)
            slotsReceta = FindFirstObjectByType<CrafteoRecetaSlots>();

        RefrescarSlots();
    }

    void Start()
    {
        // Seleccionar por defecto la primera entrada (hacha 1) cuando todo esté inicializado.
        if (entradas != null && entradas.Count > 0 && descripcionSlot != null)
            OnSlotSeleccionado(0);
    }

    /// <summary>
    /// Busca el panel de descripción incluso si está inactivo (menú crafteo cerrado al cargar).
    /// </summary>
    private void BuscarDescripcionSlot()
    {
        var encontrados = FindObjectsByType<CrafteoDescripcionSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (encontrados != null && encontrados.Length > 0)
            descripcionSlot = encontrados[0];
    }

    void OnValidate()
    {
        if (entradas == null)
            entradas = new List<EntradaCrafteo>();
    }

    /// <summary>
    /// Rellena los slots hijos con los sprites configurados (slot 1 = entradas[0].icono, etc.)
    /// y configura el click de selección.
    /// </summary>
    public void RefrescarSlots()
    {
        int index = 0;
        foreach (Transform child in transform)
        {
            if (child == null) continue;

            AplicarSpriteASlot(child.gameObject, index);
            ConfigurarSeleccion(child.gameObject, index);
            index++;
        }
    }

    /// <summary>
    /// Aplica el sprite correspondiente a un slot concreto.
    /// </summary>
    private void AplicarSpriteASlot(GameObject slot, int index)
    {
        Image img = slot.GetComponent<Image>();
        if (img == null)
            img = slot.AddComponent<Image>();

        if (index >= 0 && index < entradas.Count && entradas[index] != null)
        {
            img.sprite = entradas[index].icono;
            img.enabled = entradas[index].icono != null;
        }
        else
        {
            img.sprite = null;
            img.enabled = false;
        }
    }

    private void ConfigurarSeleccion(GameObject slot, int index)
    {
        Button btn = slot.GetComponent<Button>();
        if (btn == null)
            btn = slot.AddComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnSlotSeleccionado(index));
    }

    /// <summary>
    /// Llamado al seleccionar un slot del catálogo (por click).
    /// Actualiza el panel de descripción con el título y la descripción del objeto.
    /// </summary>
    public void OnSlotSeleccionado(int index)
    {
        if (descripcionSlot == null)
            BuscarDescripcionSlot();
        if (descripcionSlot == null) return;
        if (entradas == null || index < 0 || index >= entradas.Count) return;

        indiceSeleccionado = index;

        EntradaCrafteo entrada = entradas[index];
        if (entrada == null) return;

        descripcionSlot.Mostrar(entrada.titulo, entrada.descripcion);

        if (slotsReceta != null && entrada.receta != null)
            slotsReceta.MostrarReceta(entrada.receta);
    }

    /// <summary>
    /// Devuelve la entrada actualmente seleccionada en el catálogo (null si no hay ninguna).
    /// </summary>
    public EntradaCrafteo GetEntradaSeleccionada()
    {
        if (entradas == null || indiceSeleccionado < 0 || indiceSeleccionado >= entradas.Count)
            return null;
        return entradas[indiceSeleccionado];
    }
}
