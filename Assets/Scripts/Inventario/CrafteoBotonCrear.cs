using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Se coloca en el GameObject que realmente se pulsa (el que tiene la Image del botón).
/// Ese objeto debe tener un componente Image (o Graphic) con "Raycast Target" activado.
/// También responde al componente Button si está en el mismo objeto o en un hijo.
/// </summary>
public class CrafteoBotonCrear : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Referencia al catálogo de crafteo (CrafteoCatalogoGrid).")]
    public CrafteoCatalogoGrid catalogo;

    [Tooltip("Referencia al inventario del jugador. Si no puedes asignarlo, usa 'Jugador' y arrastra el GameObject del personaje.")]
    public Inventario inventarioJugador;

    [Tooltip("Alternativa: arrastra el GameObject del personaje (el que tiene el componente Inventario). Se usa si inventarioJugador está vacío.")]
    public GameObject jugador;

    [Tooltip("Referencia al slot pendiente donde aparecerá el objeto crafteado.")]
    public CrafteoSlotPendiente slotPendiente;

    [Header("Depuración")]
    [Tooltip("Si está activo, escribe en la consola el motivo por el que no se puede craftear.")]
    public bool logDepuracion = true;

    void Awake()
    {
        EnlazarBoton();
    }

    void OnEnable()
    {
        EnlazarBoton();
        // Asegurar que este objeto puede recibir clicks (necesario para IPointerClickHandler).
        var graphic = GetComponent<Graphic>();
        if (graphic != null && !graphic.raycastTarget)
            graphic.raycastTarget = true;
    }

    /// <summary>
    /// Llamado cuando se hace click sobre este GameObject (debe tener Image/Graphic con Raycast Target activado).
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        IntentarCraftear();
    }

    void EnlazarBoton()
    {
        ResolverReferencias();
        // Buscar el Button en este objeto, en el padre o en los hijos.
        Button btn = GetComponent<Button>();
        if (btn == null)
            btn = GetComponentInParent<Button>();
        if (btn == null)
            btn = GetComponentInChildren<Button>(true);
        if (btn == null)
        {
            btn = gameObject.AddComponent<Button>();
            Debug.Log("[Crafteo] Se añadió un componente Button a '" + gameObject.name + "'.");
        }
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(IntentarCraftear);
        }
    }

    void ResolverReferencias()
    {
        if (catalogo == null)
            catalogo = FindFirstObjectByType<CrafteoCatalogoGrid>();
        if (inventarioJugador == null && jugador != null)
            inventarioJugador = jugador.GetComponent<Inventario>();
        if (inventarioJugador == null)
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
                inventarioJugador = playerGo.GetComponent<Inventario>();
        }
        if (inventarioJugador == null)
            inventarioJugador = FindFirstObjectByType<Inventario>();
        if (slotPendiente == null)
            slotPendiente = FindFirstObjectByType<CrafteoSlotPendiente>();
    }

    /// <summary>
    /// Lógica principal: comprobar ingredientes, consumirlos y crear el resultado.
    /// </summary>
    public void IntentarCraftear()
    {
        // Este log siempre sale si el click llega al botón. Si no ves nada en consola, el click no está llegando.
        Debug.Log("[Crafteo] Botón 'Crear' pulsado.");

        ResolverReferencias();

        if (catalogo == null) { Debug.Log("[Crafteo] No se encontró el catálogo (CrafteoCatalogoGrid)."); return; }
        if (inventarioJugador == null) { Debug.Log("[Crafteo] No se encontró el inventario del jugador. Asigna 'Jugador' en el botón al personaje."); return; }
        if (slotPendiente == null) { Debug.Log("[Crafteo] No se encontró el slot pendiente (CrafteoSlotPendiente)."); return; }

        CrafteoCatalogoGrid.EntradaCrafteo entrada = catalogo.GetEntradaSeleccionada();
        if (entrada == null) { Debug.Log("[Crafteo] No hay receta seleccionada. Haz click antes en el hacha en el catálogo."); return; }
        if (entrada.receta == null || entrada.receta.Count == 0) { Debug.Log("[Crafteo] La receta no tiene ingredientes configurados."); return; }
        if (entrada.prefabResultado == null)
        {
            Debug.LogWarning("[Crafteo] Falta 'Prefab Resultado' en la receta del hacha. En el catálogo > Entradas > Element 0 asigna el prefab del hacha 1.");
            return;
        }

        if (slotPendiente.TieneObjeto())
        {
            Debug.Log("[Crafteo] Ya hay un objeto en el slot pendiente. Haz click en el slot Pendiente para recogerlo al inventario."); return;
        }

        if (!TieneIngredientes(entrada.receta))
        {
            LogFaltanIngredientes(entrada.receta);
            return;
        }

        ConsumirIngredientes(entrada.receta);
        slotPendiente.ColocarObjeto(entrada.prefabResultado);

        if (inventarioJugador.inv != null)
            inventarioJugador.inv.imagenesInventario();

        Debug.Log("[Crafteo] Objeto crafteado. Aparece en el slot Pendiente.");
    }

    /// <summary>
    /// Comprueba si el inventario del jugador contiene todos los ingredientes necesarios.
    /// </summary>
    private bool TieneIngredientes(List<CrafteoCatalogoGrid.IngredienteReceta> receta)
    {
        foreach (var ingrediente in receta)
        {
            if (ingrediente == null) continue;
            if (ingrediente.textura == null)
                return false;
            int disponible = ContarItemPorTextura(ingrediente.textura);
            if (disponible < ingrediente.cantidad)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Escribe en consola qué ingredientes faltan (cantidad que tienes vs cantidad necesaria).
    /// </summary>
    private void LogFaltanIngredientes(List<CrafteoCatalogoGrid.IngredienteReceta> receta)
    {
        for (int i = 0; i < receta.Count; i++)
        {
            var ing = receta[i];
            if (ing == null) continue;
            if (ing.textura == null)
            {
                Debug.Log("[Crafteo] Ingrediente " + (i + 1) + " no tiene 'Textura' asignada. En la receta del hacha asigna la misma textura que tiene el ObjetoRecogible de rama/piedra.");
                return;
            }
            int disponible = ContarItemPorTextura(ing.textura);
            if (disponible < ing.cantidad)
            {
                Debug.Log("[Crafteo] Faltan ingredientes. Ingrediente " + (i + 1) + " (textura '" + ing.textura.name + "'): tienes " + disponible + ", necesitas " + ing.cantidad + ". Asigna la 'Textura' correcta en la receta si no coincide con tus ítems.");
                return;
            }
        }
        Debug.Log("[Crafteo] No tienes suficientes ingredientes. Revisa que la 'Textura' de cada ingrediente en la receta sea la misma que la del ObjetoRecogible de rama y piedra.");
    }

    /// <summary>
    /// Cuenta cuántas unidades de un item (por textura) tiene el jugador. Compara por referencia o por nombre de textura.
    /// </summary>
    private int ContarItemPorTextura(Texture textura)
    {
        if (textura == null) return 0;
        if (inventarioJugador.inventario == null) return 0;
        string nombreTextura = textura.name;
        int total = 0;
        for (int i = 0; i < inventarioJugador.inventario.Count; i++)
        {
            GameObject obj = inventarioJugador.inventario[i];
            if (obj == null) continue;
            var rec = obj.GetComponent<ObjetoRecogible>();
            if (rec == null || rec.textura == null) continue;
            bool coincide = (rec.textura == textura) || (rec.textura.name == nombreTextura);
            if (coincide)
                total += inventarioJugador.GetCantidadEnSlot(i);
        }
        return total;
    }

    /// <summary>
    /// Consume los ingredientes del inventario del jugador según la receta.
    /// </summary>
    private void ConsumirIngredientes(List<CrafteoCatalogoGrid.IngredienteReceta> receta)
    {
        foreach (var ingrediente in receta)
        {
            if (ingrediente == null || ingrediente.textura == null) continue;
            int porConsumir = ingrediente.cantidad;
            string nombreTextura = ingrediente.textura.name;
            if (inventarioJugador.inventario == null) continue;

            for (int i = 0; i < inventarioJugador.inventario.Count && porConsumir > 0; i++)
            {
                GameObject obj = inventarioJugador.inventario[i];
                if (obj == null) continue;
                var rec = obj.GetComponent<ObjetoRecogible>();
                if (rec == null || rec.textura == null) continue;
                bool coincide = (rec.textura == ingrediente.textura) || (rec.textura.name == nombreTextura);
                if (!coincide) continue;

                int enSlot = inventarioJugador.GetCantidadEnSlot(i);
                if (enSlot <= porConsumir)
                {
                    porConsumir -= enSlot;
                    inventarioJugador.inventario[i] = null;
                    inventarioJugador.SetCantidadEnSlot(i, 0);
                }
                else
                {
                    inventarioJugador.SetCantidadEnSlot(i, enSlot - porConsumir);
                    porConsumir = 0;
                }
            }
        }
    }
}
