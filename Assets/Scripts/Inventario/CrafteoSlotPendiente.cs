using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Se coloca en el GameObject "Menú Crafteo Slot Pendiente".
/// Muestra el objeto crafteado y permite al jugador recogerlo (click) al inventario.
/// </summary>
public class CrafteoSlotPendiente : MonoBehaviour
{
    [Tooltip("Referencia al inventario del jugador. Si no puedes asignarlo, usa 'Jugador' y arrastra el GameObject del personaje.")]
    public Inventario inventarioJugador;

    [Tooltip("Alternativa: arrastra el GameObject del personaje (el que tiene el componente Inventario).")]
    public GameObject jugador;

    private GameObject prefabPendiente;
    private Image imagen;

    void Awake()
    {
        imagen = GetComponent<Image>();
        if (imagen == null)
            imagen = gameObject.AddComponent<Image>();

        ResolverInventario();

        // Sin Button: el objeto se lleva al inventario solo arrastrando (SlotPendienteDrag).
        ActualizarVisual();
    }

    void ResolverInventario()
    {
        if (inventarioJugador != null) return;
        if (jugador != null)
            inventarioJugador = jugador.GetComponent<Inventario>();
        if (inventarioJugador == null)
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
                inventarioJugador = playerGo.GetComponent<Inventario>();
        }
        if (inventarioJugador == null)
            inventarioJugador = FindFirstObjectByType<Inventario>();
    }

    /// <summary>
    /// Devuelve true si hay un objeto pendiente de recoger.
    /// </summary>
    public bool TieneObjeto()
    {
        return prefabPendiente != null;
    }

    /// <summary>
    /// Devuelve el sprite del objeto pendiente (para el ghost al arrastrar).
    /// </summary>
    public Sprite GetSpritePendiente()
    {
        if (prefabPendiente == null) return null;
        return ObtenerSpriteDePrefab(prefabPendiente);
    }

    /// <summary>
    /// Coloca el prefab del objeto crafteado en el slot pendiente y muestra su sprite.
    /// </summary>
    public void ColocarObjeto(GameObject prefab)
    {
        prefabPendiente = prefab;
        ActualizarVisual();
    }

    /// <summary>
    /// El jugador recoge el objeto pendiente y lo mete en el primer slot libre del inventario.
    /// </summary>
    public void RecogerAlInventario()
    {
        int slotLibre = inventarioJugador != null && inventarioJugador.inventario != null
            ? inventarioJugador.inventario.FindIndex(item => item == null)
            : -1;
        RecogerAlInventarioEnSlot(slotLibre >= 0 ? slotLibre : -1);
    }

    /// <summary>
    /// Mueve el objeto pendiente al slot del inventario indicado (por ejemplo al soltar arrastrando).
    /// Si slotIndex es válido y está vacío, lo pone ahí; si no, lo pone en el primer hueco libre.
    /// </summary>
    public bool RecogerAlInventarioEnSlot(int slotIndex)
    {
        if (prefabPendiente == null) return false;
        ResolverInventario();
        if (inventarioJugador == null || inventarioJugador.inventario == null) return false;

        int destino = slotIndex;
        if (destino < 0 || destino >= inventarioJugador.inventario.Count || inventarioJugador.inventario[destino] != null)
            destino = inventarioJugador.inventario.FindIndex(item => item == null);

        if (destino < 0)
        {
            Debug.Log("CrafteoSlotPendiente: inventario lleno.");
            return false;
        }

        GameObject instancia = Object.Instantiate(prefabPendiente);
        instancia.name = prefabPendiente.name;
        instancia.SetActive(false);

        inventarioJugador.inventario[destino] = instancia;
        inventarioJugador.SetCantidadEnSlot(destino, 1);

        prefabPendiente = null;
        ActualizarVisual();

        InventarioGrafico ui = inventarioJugador.inv;
        if (ui == null)
        {
            var todos = FindObjectsByType<InventarioGrafico>(FindObjectsSortMode.None);
            foreach (var g in todos)
                if (g.inv == inventarioJugador) { ui = g; break; }
        }
        if (ui != null)
            ui.imagenesInventario();

        return true;
    }

    private void ActualizarVisual()
    {
        if (imagen == null) return;

        if (prefabPendiente != null)
        {
            Sprite sprite = ObtenerSpriteDePrefab(prefabPendiente);
            imagen.sprite = sprite;
            imagen.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            imagen.raycastTarget = true;
            // Para que el arrastre funcione, este GameObject debe recibir el click: desactivar raycast en hijos.
            foreach (Transform child in transform)
            {
                var childImg = child.GetComponent<Image>();
                if (childImg != null)
                    childImg.raycastTarget = false;
            }
        }
        else
        {
            imagen.sprite = null;
            imagen.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    private Sprite ObtenerSpriteDePrefab(GameObject prefab)
    {
        if (prefab == null) return null;

        var recogible = prefab.GetComponent<ObjetoRecogible>();
        if (recogible != null && recogible.textura != null)
            return InventarioGrafico.ObtenerSpriteDesdeTextura(recogible.textura);

        var sr = prefab.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
            return sr.sprite;

        return null;
    }
}
