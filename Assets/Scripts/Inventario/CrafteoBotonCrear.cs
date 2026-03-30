using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Se coloca en el GameObject que realmente se pulsa (el que tiene la Image del botón).
/// Ese objeto debe tener un componente Image (o Graphic) con "Raycast Target" activado.
/// También responde al componente Button si está en el mismo objeto o en un hijo.
/// </summary>
public class CrafteoBotonCrear : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Tooltip("Raíz del menú al que pertenece este botón. Si se asigna, las búsquedas automáticas se limitan a esta jerarquía.")]
    public Transform raizMenu;

    [Header("Sprite al pulsar")]
    [Tooltip("Sprite que se muestra mientras se mantiene pulsado el botón. Si está vacío, no se cambia el sprite.")]
    public Sprite spritePulsado;

    [Tooltip("Image del botón. Si no se asigna, se usa el del mismo GameObject.")]
    public Image imageBoton;

    [Tooltip("Referencia al catálogo de crafteo (CrafteoCatalogoGrid).")]
    public CrafteoCatalogoGrid catalogo;

    [Tooltip("Referencia al inventario del jugador. Si no puedes asignarlo, usa 'Jugador' y arrastra el GameObject del personaje.")]
    public Inventario inventarioJugador;

    [Tooltip("Alternativa: arrastra el GameObject del personaje (el que tiene el componente Inventario). Se usa si inventarioJugador está vacío.")]
    public GameObject jugador;

    [Tooltip("Referencia al slot pendiente donde aparecerá el objeto crafteado.")]
    public CrafteoSlotPendiente slotPendiente;

    [Tooltip("Barra de progreso del crafteo (Menú Crafteo Slot Barra proceso). Si no se asigna, se busca en la escena.")]
    public CrafteoBarraProgreso barraProgreso;

    [Header("Depuración")]
    [Tooltip("Si está activo, escribe en la consola el motivo por el que no se puede craftear.")]
    public bool logDepuracion = true;

    Sprite _spriteNormalGuardado;
    bool _crafteando;

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

    public void OnPointerDown(PointerEventData eventData)
    {
        if (spritePulsado == null) return;
        Image img = GetImage();
        if (img == null) return;
        _spriteNormalGuardado = img.sprite;
        img.sprite = spritePulsado;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        RestaurarSpriteNormal();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RestaurarSpriteNormal();
    }

    void RestaurarSpriteNormal()
    {
        if (_spriteNormalGuardado == null) return;
        Image img = GetImage();
        if (img != null)
        {
            img.sprite = _spriteNormalGuardado;
            _spriteNormalGuardado = null;
        }
    }

    Image GetImage()
    {
        if (imageBoton != null) return imageBoton;
        return GetComponent<Image>();
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
        {
            var raiz = ObtenerRaizBusqueda();
            if (raiz != null)
                catalogo = BuscarComponenteMasCercanoEnRaiz<CrafteoCatalogoGrid>(raiz);

            if (catalogo == null)
            {
                var catalogos = FindObjectsByType<CrafteoCatalogoGrid>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (catalogos != null && catalogos.Length == 1)
                    catalogo = catalogos[0];
            }
        }
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
        {
            var raiz = ObtenerRaizBusqueda();
            if (raiz != null)
                slotPendiente = BuscarComponenteMasCercanoEnRaiz<CrafteoSlotPendiente>(raiz);

            if (slotPendiente == null)
            {
                var slotsPendientes = FindObjectsByType<CrafteoSlotPendiente>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (slotsPendientes != null && slotsPendientes.Length == 1)
                    slotPendiente = slotsPendientes[0];
            }
        }

        if (barraProgreso == null)
        {
            var raiz = ObtenerRaizBusqueda();
            if (raiz != null)
                barraProgreso = BuscarComponenteMasCercanoEnRaiz<CrafteoBarraProgreso>(raiz);

            if (barraProgreso == null)
            {
                var barras = FindObjectsByType<CrafteoBarraProgreso>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (barras != null && barras.Length == 1)
                    barraProgreso = barras[0];
            }
        }
    }

    /// <summary>
    /// Lógica principal: comprobar ingredientes, iniciar crafteo con tiempo y barra de progreso.
    /// </summary>
    public void IntentarCraftear()
    {
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
            Debug.LogWarning("[Crafteo] Falta 'Prefab Resultado' en la receta. Asigna el prefab en el catálogo > Entradas.");
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

        if (_crafteando)
        {
            if (logDepuracion) Debug.Log("[Crafteo] Ya hay un crafteo en curso.");
            return;
        }

        float tiempo = entrada.tiempoCrafteoSegundos > 0f ? entrada.tiempoCrafteoSegundos : 6f;
        StartCoroutine(CraftearConProgreso(entrada, tiempo));
    }

    IEnumerator CraftearConProgreso(CrafteoCatalogoGrid.EntradaCrafteo entrada, float tiempoSegundos)
    {
        _crafteando = true;

        ConsumirIngredientes(entrada.receta);
        if (inventarioJugador.inv != null)
            inventarioJugador.inv.imagenesInventario();

        if (barraProgreso != null)
            barraProgreso.Mostrar();

        yield return null; // Un frame para que se dibuje la barra al 0%

        float transcurrido = 0f;
        while (transcurrido < tiempoSegundos)
        {
            transcurrido += Time.deltaTime;
            float t = Mathf.Clamp01(transcurrido / tiempoSegundos);
            if (barraProgreso != null)
                barraProgreso.SetProgreso(t);
            yield return null;
        }

        if (barraProgreso != null)
            barraProgreso.Ocultar();

        slotPendiente.ColocarObjeto(entrada.prefabResultado);
        if (inventarioJugador.inv != null)
            inventarioJugador.inv.imagenesInventario();

        _crafteando = false;
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
