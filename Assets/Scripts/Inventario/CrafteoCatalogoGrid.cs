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
    [Tooltip("Raíz del menú al que pertenece este catálogo. Si se asigna, las búsquedas automáticas se limitan a esta jerarquía.")]
    public Transform raizMenu;

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

        [Tooltip("Tiempo en segundos que tarda el crafteo (ej: 6). La barra de progreso se muestra durante este tiempo.")]
        [Min(0.1f)]
        public float tiempoCrafteoSegundos = 6f;
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

        ResolverReferenciasLocales();

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
        if (descripcionSlot != null) return;

        var raiz = ObtenerRaizBusqueda();
        if (raiz != null)
            descripcionSlot = BuscarComponenteMasCercanoEnRaiz<CrafteoDescripcionSlot>(raiz);

        if (descripcionSlot == null)
        {
            var encontrados = FindObjectsByType<CrafteoDescripcionSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (encontrados != null && encontrados.Length == 1)
                descripcionSlot = encontrados[0];
        }
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
        if (descripcionSlot == null || slotsReceta == null)
            ResolverReferenciasLocales();
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

    private void ResolverReferenciasLocales()
    {
        if (descripcionSlot == null)
            BuscarDescripcionSlot();

        if (slotsReceta == null)
        {
            var raiz = ObtenerRaizBusqueda();
            if (raiz != null)
                slotsReceta = BuscarComponenteMasCercanoEnRaiz<CrafteoRecetaSlots>(raiz);

            if (slotsReceta == null)
            {
                var encontrados = FindObjectsByType<CrafteoRecetaSlots>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (encontrados != null && encontrados.Length == 1)
                    slotsReceta = encontrados[0];
            }
        }
    }

    private Transform ObtenerRaizBusqueda()
    {
        if (raizMenu != null)
            return raizMenu;

        var canvasPadre = GetComponentInParent<Canvas>(true);
        return canvasPadre != null ? canvasPadre.transform : transform.root;
    }

    private T BuscarComponenteMasCercanoEnRaiz<T>(Transform raiz) where T : Component
    {
        if (raiz == null) return null;

        var candidatos = raiz.GetComponentsInChildren<T>(true);
        if (candidatos == null || candidatos.Length == 0) return null;

        T mejor = null;
        int mejorDistancia = int.MaxValue;
        for (int i = 0; i < candidatos.Length; i++)
        {
            var candidato = candidatos[i];
            if (candidato == null || candidato.transform == transform) continue;

            int distancia = DistanciaJerarquica(transform, candidato.transform);
            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejor = candidato;
            }
        }

        return mejor;
    }

    private int DistanciaJerarquica(Transform origen, Transform destino)
    {
        if (origen == null || destino == null) return int.MaxValue;
        if (origen == destino) return 0;

        Dictionary<Transform, int> distanciasOrigen = new Dictionary<Transform, int>();
        int pasos = 0;
        Transform actual = origen;
        while (actual != null)
        {
            distanciasOrigen[actual] = pasos;
            pasos++;
            actual = actual.parent;
        }

        pasos = 0;
        actual = destino;
        while (actual != null)
        {
            if (distanciasOrigen.TryGetValue(actual, out int desdeOrigen))
                return desdeOrigen + pasos;
            pasos++;
            actual = actual.parent;
        }

        return int.MaxValue;
    }
}
