using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class Inventario : MonoBehaviour
{
    public List<GameObject> inventario;
    // Cantidad por slot (1 = objeto único, >1 = acumulado)
    [SerializeField] private List<int> cantidades;
    public GameObject objeto;
    public GameObject inventarioGrafico;
    public InventarioGrafico inv;
    public ObjetoRecogible recoger;
    public Arbusto arbusto;


    [Header("Arma equipada")]
    [Tooltip("Transform donde se instancia el arma (hijo 'mano' o 'tool').")]
    public Transform mano;
    [Tooltip("Material para los SpriteRenderers del arma equipada (p. ej. Sprites-Lit para iluminación 2D). Si se asigna, se aplica al equipar cualquier arma.")]
    public Material materialArmasLit;
    private GameObject armaInstancia;
    private DatosArma armaEquipadaDatos;
    public DatosArma ArmaEquipadaDatos => armaEquipadaDatos;

    private const string TagRecogible = "Recogible";
    private const string TagConstruccionColocable = "ConstruccionColocable";

    [HideInInspector] public bool tieneArmaEquipada = false;

    public bool TieneArmaEquipada => tieneArmaEquipada;

    [Header("Armadura")]
    [Tooltip("Objeto en el slot de armadura (null si no hay ninguna).")]
    [HideInInspector] public GameObject slotArmadura;
    [Tooltip("Visual de la armadura equipada (se activa al tener armadura en el slot). Asigna el GameObject que muestra Armadura 2.")]
    public GameObject armaduraEquipadaVisual;

    public bool isActive = false;

    [Header("Menús")]
    [Tooltip("Menú de crafteo que debe abrirse junto con el inventario al pulsar C.")]
    public GameObject menuCrafteo;
    [Tooltip("Menú de construcción que debe abrirse junto con el inventario al pulsar B.")]
    public GameObject menuConstruccion;

    [Header("Baúl")]
    [Tooltip("Baúl cuyo inventario está abierto (null si ninguno).")]
    [HideInInspector] public BaulInteractuable BaulAbierto;

    [Header("Cadáver")]
    [Tooltip("Cadáver cuyo inventario está abierto (null si ninguno).")]
    [HideInInspector] public CadaverInteractuable CadaverAbierto;

    [Header("Hoguera")]
    [Tooltip("Hoguera cuyo inventario está abierto (null si ninguna).")]
    [HideInInspector] public HogueraInteractuable HogueraAbierto;

    void Start()
    {
        objeto = null;
        inventario = new List<GameObject>();
        inventarioGrafico.SetActive(isActive);
        int cantidadSlots = inv.celdas.Count;
        while (inventario.Count < cantidadSlots)
        {
            inventario.Add(null);
        }
        InicializarCantidades();
        DesequiparArma();
        DesequiparArmadura();
    }

    void Update()
    {
        activarInventario();
        TryAccionConF();
        TryAccionConE();
        ProcesarTeclasAccesoRapido();
        if (tieneArmaEquipada && armaInstancia != null)
            ActualizarArmaEquipada();
    }

    void LateUpdate()
    {
        if (armaInstancia != null)
            armaInstancia.transform.localPosition = Vector3.zero;
    }

    private void RecogerObjeto()
    {
        if (objeto == null) return;

        var recogible = objeto.GetComponent<ObjetoRecogible>();
        string nombreLimpio = NombreLimpioObjeto(objeto);

        // Si es acumulable, intentamos apilarlo en un slot existente del mismo tipo.
        if (recogible != null && recogible.categoria == CategoriaObjeto.Acumulable)
        {
            AsegurarTamanioCantidades();
            for (int i = 0; i < inventario.Count; i++)
            {
                GameObject existente = inventario[i];
                if (existente == null) continue;

                var recExistente = existente.GetComponent<ObjetoRecogible>();
                if (recExistente == null) continue;

                // Mismo tipo si comparten textura y categoría acumulable.
                if (recExistente.categoria == CategoriaObjeto.Acumulable &&
                    recExistente.textura == recogible.textura)
                {
                    if (cantidades[i] <= 0) cantidades[i] = 1;
                    cantidades[i]++;
                    Debug.Log($"Inventario: acumulada '{objeto.name}' en el slot {i + 1}. Cantidad ahora: {cantidades[i]}.");
                    MensajeInventario.MostrarMensaje($"Añadida {nombreLimpio} al inventario (x{cantidades[i]})");
                    // Eliminamos el objeto físico recogido (ya representado en el stack).
                    Destroy(objeto);
                    objeto = null;
                    inv.imagenesInventario();
                    return;
                }
            }
        }

        // Buscar primer espacio vacío para poner el objeto (o el inicio de un nuevo stack).
        int indiceLibre = inventario.FindIndex(item => item == null);

        if (indiceLibre != -1)
        {
            inventario[indiceLibre] = objeto;
            AsegurarTamanioCantidades();
            cantidades[indiceLibre] = 1;
            Debug.Log($"Inventario: añadido '{objeto.name}' en el slot {indiceLibre + 1}.");
        }
        else
        {
            inventario.Add(objeto);
            AsegurarTamanioCantidades();
            while (cantidades.Count < inventario.Count)
                cantidades.Add(0);
            cantidades[inventario.Count - 1] = 1;
            Debug.Log($"Inventario: añadido '{objeto.name}' al final (sin huecos libres previos).");
        }

        MensajeInventario.MostrarMensaje($"Añadido {nombreLimpio} al inventario");

        ResetearEstadoRecogible(objeto);
        objeto.SetActive(false);
        inv.imagenesInventario();
    }

    /// <summary>
    /// Intercambia o mueve el objeto entre dos slots (para drag &amp; drop en la UI).
    /// </summary>
    public void MoverObjetoEntreSlots(int desde, int hasta)
    {
        if (desde == hasta) return;
        if (inventario == null || desde < 0 || desde >= inventario.Count || hasta < 0 || hasta >= inventario.Count)
            return;

        GameObject temp = inventario[desde];
        inventario[desde] = inventario[hasta];
        inventario[hasta] = temp;

        AsegurarTamanioCantidades();
        int tempCantidad = cantidades[desde];
        cantidades[desde] = cantidades[hasta];
        cantidades[hasta] = tempCantidad;

        if (inv != null)
            inv.imagenesInventario();
    }

    /// <summary>
    /// Suelta el objeto del slot en el mundo, en la posici�n actual del personaje.
    /// Si el objeto es el hacha y est� equipada, la desequipa antes.
    /// </summary>
    public void SoltarObjetoEnMundo(int slotIndex)
    {
        if (inventario == null || slotIndex < 0 || slotIndex >= inventario.Count) return;
        GameObject obj = inventario[slotIndex];
        if (obj == null) return;

        var recogible = obj.GetComponent<ObjetoRecogible>();
        bool usarTagConstruccionColocable = recogible != null && recogible.usarTagConstruccionColocableAlSoltar;

        if (ObjetoRecogible.EsEquipable(obj) && tieneArmaEquipada)
            DesequiparArma();

        AsegurarTamanioCantidades();
        int cantidadActual = cantidades[slotIndex] <= 0 ? 1 : cantidades[slotIndex];

        var movimiento = GetComponent<MovimientoPorCeldas>();
        Vector2 posCelda = movimiento != null ? movimiento.GetPosicionCeldaActual() : (Vector2)transform.position;

        // Si es acumulable y hay más de 1 unidad, solo soltamos 1 y reducimos la cantidad.
        if (recogible != null && recogible.categoria == CategoriaObjeto.Acumulable && cantidadActual > 1)
        {
            cantidades[slotIndex] = cantidadActual - 1;

            GameObject copia = Instantiate(obj, posCelda, Quaternion.identity);
            ResetearEstadoRecogible(copia);
            if (usarTagConstruccionColocable)
                SetTagSafe(copia, TagConstruccionColocable);
            copia.SetActive(true);

            if (inv != null)
                inv.imagenesInventario();
            return;
        }

        // Caso normal: soltamos todo el stack (o un único objeto)
        obj.transform.position = posCelda;
        ResetearEstadoRecogible(obj);
        if (usarTagConstruccionColocable)
            SetTagSafe(obj, TagConstruccionColocable);
        obj.SetActive(true);
        inventario[slotIndex] = null;
        cantidades[slotIndex] = 0;
        if (inv != null)
            inv.imagenesInventario();
    }


 /* private void soltarObjeto()
{
    if (Input.GetKeyDown(KeyCode.P))
    {
        for (int i = 0; i < inventario.Count; i++)
        {
            if (inventario[i] != null)
            {
                GameObject g = inventario[i];
                g.transform.position = transform.position;
                g.SetActive(true);
                inventario.RemoveAt(i);

                Debug.Log("Soltar");
                inv.imagenesInventario();

                return;
            }
        }
    }
}*/


    private void activarInventario()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isActive == false)
            {
                isActive = true;
                inventarioGrafico.SetActive(isActive);
                inv.imagenesInventario();
            }
            else if (isActive == true)
            {
                isActive = false;
                inventarioGrafico.SetActive(isActive);
                if (menuCrafteo != null)
                    menuCrafteo.SetActive(false);
                if (menuConstruccion != null)
                    menuConstruccion.SetActive(false);
                inv.imagenesInventario();
                if (BaulAbierto != null)
                {
                    if (BaulAbierto.panelInventarioBaul != null)
                        BaulAbierto.panelInventarioBaul.SetActive(false);
                    BaulAbierto.Cerrar();
                    BaulAbierto = null;
                }
                if (CadaverAbierto != null)
                {
                    if (CadaverAbierto.panelInventarioCadaver != null)
                        CadaverAbierto.panelInventarioCadaver.SetActive(false);
                    CadaverAbierto.Cerrar();
                    CadaverAbierto = null;
                }
                if (HogueraAbierto != null)
                {
                    if (HogueraAbierto.panelInventarioHoguera != null)
                        HogueraAbierto.panelInventarioHoguera.SetActive(false);
                    HogueraAbierto.Cerrar();
                    HogueraAbierto = null;
                }
            }
        }

        // Tecla C: abrir/cerrar menú de crafteo junto con el inventario.
        if (Input.GetKeyDown(KeyCode.C))
        {
            // Si el inventario no está abierto, abrirlo primero.
            if (!isActive)
            {
                isActive = true;
                inventarioGrafico.SetActive(true);
                if (inv != null)
                    inv.imagenesInventario();
            }

            // Alternar el menú de crafteo.
            if (menuCrafteo != null)
            {
                if (menuConstruccion != null)
                    menuConstruccion.SetActive(false);
                menuCrafteo.SetActive(!menuCrafteo.activeSelf);
            }
        }

        // Tecla B: abrir/cerrar menú de construcción junto con el inventario.
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (menuConstruccion == null)
                menuConstruccion = BuscarMenuConstruccionEnEscena();

            // Si el inventario no está abierto, abrirlo primero.
            if (!isActive)
            {
                isActive = true;
                inventarioGrafico.SetActive(true);
                if (inv != null)
                    inv.imagenesInventario();
            }

            // Alternar el menú de construcción.
            if (menuConstruccion != null)
            {
                if (menuCrafteo != null)
                    menuCrafteo.SetActive(false);
                menuConstruccion.SetActive(!menuConstruccion.activeSelf);
            }
            else
            {
                Debug.LogWarning("Inventario: no se encontró 'menuConstruccion'. Asigna la referencia en Inspector o renombra el panel como 'Menu Construccion'/'Menú Construcción'.");
            }
        }
    }

    private GameObject BuscarMenuConstruccionEnEscena()
    {
        string[] candidatos =
        {
            "Menu Construccion",
            "Menú Construcción",
            "Menú Construccion",
            "Menu Construcción",
            "menu construccion",
            "menu construcción"
        };

        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t == null || t.gameObject == null) continue;
            if (!t.gameObject.scene.IsValid()) continue;
            if (t.hideFlags != HideFlags.None) continue;

            foreach (var nombre in candidatos)
            {
                if (t.name.Equals(nombre, System.StringComparison.OrdinalIgnoreCase))
                    return t.gameObject;
            }
        }

        return null;
    }

    /// <summary>
    /// Abre el inventario del personaje y el panel del cadáver. Llamado por CadaverInteractuable.Abrir().
    /// </summary>
    public void AbrirInventarioConCadaver(CadaverInteractuable cadaver)
    {
        if (cadaver == null) return;

        GameObject panelCadaver = cadaver.panelInventarioCadaver;
        if (panelCadaver == null)
        {
            // Intentamos primero por nombre exacto (solo activos)
            panelCadaver = GameObject.Find("Inventario Cadáver");

            // Si no existe o está desactivado, buscamos entre todos los objetos (incluyendo inactivos)
            if (panelCadaver == null)
            {
                var invCadavers = Resources.FindObjectsOfTypeAll<InventarioCadaverGrafico>();
                if (invCadavers != null && invCadavers.Length > 0 && invCadavers[0] != null)
                    panelCadaver = invCadavers[0].gameObject;
            }

            if (panelCadaver != null)
                cadaver.panelInventarioCadaver = panelCadaver;
            else
                Debug.LogWarning("Inventario: no se encontró ningún panel de Inventario Cadáver en la escena. Añade un objeto con InventarioCadaverGrafico.");
        }

        CadaverAbierto = cadaver;
        BaulAbierto = null;
        isActive = true;
        inventarioGrafico.SetActive(true);
        inv.imagenesInventario();

        if (panelCadaver != null)
        {
            panelCadaver.SetActive(true);
            panelCadaver.transform.SetAsLastSibling();
            var invCadaver = panelCadaver.GetComponent<InventarioCadaverGrafico>();
            if (invCadaver != null)
                invCadaver.Refrescar();
        }
    }

    /// <summary>
    /// Abre el inventario del personaje y el panel del baúl. Llamado por BaulInteractuable.Abrir().
    /// </summary>
    public void AbrirInventarioConBaul(BaulInteractuable baul)
    {
        if (baul == null) return;

        GameObject panelBaul = baul.panelInventarioBaul;
        if (panelBaul == null)
        {
            panelBaul = GameObject.Find("Inventario Ba�l");
            if (panelBaul != null)
                baul.panelInventarioBaul = panelBaul;
            else
                Debug.LogWarning("Inventario: no se encontr� el panel 'Inventario Ba�l'. Asigna 'Panel Inventario Baul' en el componente BaulInteractuable del ba�l.");
        }

        BaulAbierto = baul;
        CadaverAbierto = null;
        isActive = true;
        inventarioGrafico.SetActive(true);
        inv.imagenesInventario();

        if (panelBaul != null)
        {
            panelBaul.SetActive(true);
            panelBaul.transform.SetAsLastSibling();
            var invBaul = panelBaul.GetComponent<InventarioBaulGrafico>();
            if (invBaul != null)
                invBaul.Refrescar();
        }
    }

    /// <summary>
    /// Abre el inventario del personaje y el panel de la hoguera. Llamado por HogueraInteractuable.Abrir().
    /// </summary>
    public void AbrirInventarioConHoguera(HogueraInteractuable hoguera)
    {
        if (hoguera == null) return;

        GameObject panelHoguera = hoguera.panelInventarioHoguera;
        if (panelHoguera == null)
        {
            panelHoguera = GameObject.Find("Inventario Hoguera");
            if (panelHoguera == null)
            {
                var invHogueras = Resources.FindObjectsOfTypeAll<InventarioHogueraGrafico>();
                if (invHogueras != null && invHogueras.Length > 0 && invHogueras[0] != null)
                    panelHoguera = invHogueras[0].gameObject;
            }
            if (panelHoguera != null)
                hoguera.panelInventarioHoguera = panelHoguera;
            else
                Debug.LogWarning("Inventario: no se encontró ningún panel 'Inventario Hoguera'. Crea un panel con InventarioHogueraGrafico y nómbralo 'Inventario Hoguera'.");
        }

        HogueraAbierto = hoguera;
        BaulAbierto = null;
        CadaverAbierto = null;
        isActive = true;
        inventarioGrafico.SetActive(true);
        inv.imagenesInventario();

        if (panelHoguera != null)
        {
            panelHoguera.SetActive(true);
            panelHoguera.transform.SetAsLastSibling();
            var invHoguera = panelHoguera.GetComponent<InventarioHogueraGrafico>();
            if (invHoguera != null)
                invHoguera.Refrescar();
        }
    }

    /// <summary>
    /// Mueve un objeto del baúl al slot del jugador.
    /// Si el slot del jugador ya contiene un acumulable del mismo tipo, se acumula en lugar de intercambiar.
    /// </summary>
    public void RecibirItemDesdeBaul(BaulInteractuable baul, int slotBaul, int slotJugador)
    {
        if (baul == null || inventario == null || slotJugador < 0 || slotJugador >= inventario.Count) return;

        GameObject itemBaul = baul.GetContenido(slotBaul);
        if (itemBaul == null) return;

        GameObject itemJugador = inventario[slotJugador];

        var recBaul = itemBaul.GetComponent<ObjetoRecogible>();
        var recJugador = itemJugador != null ? itemJugador.GetComponent<ObjetoRecogible>() : null;

        // Caso especial: ambos son acumulables del mismo tipo → acumular en el slot del jugador.
        if (itemJugador != null &&
            recBaul != null && recJugador != null &&
            recBaul.categoria == CategoriaObjeto.Acumulable &&
            recJugador.categoria == CategoriaObjeto.Acumulable &&
            recBaul.textura == recJugador.textura)
        {
            int cantidadBaul = baul.GetCantidad(slotBaul);
            int cantidadJugador = GetCantidadEnSlot(slotJugador);

            int suma = Mathf.Max(1, cantidadJugador) + Mathf.Max(1, cantidadBaul);

            AsegurarTamanioCantidades();
            cantidades[slotJugador] = suma;

            // Vaciar slot del baúl
            baul.SetContenido(slotBaul, null);
            baul.SetCantidad(slotBaul, 0);

            if (inv != null) inv.imagenesInventario();
            if (baul.panelInventarioBaul != null)
            {
                var invBaul = baul.panelInventarioBaul.GetComponent<InventarioBaulGrafico>();
                if (invBaul != null) invBaul.Refrescar();
            }
            return;
        }

        // Comportamiento por defecto: intercambio de objetos entre baúl y jugador.
        int cantidadBaulDefault = baul.GetCantidad(slotBaul);
        int cantidadJugadorDefault = GetCantidadEnSlot(slotJugador);

        baul.SetContenido(slotBaul, itemJugador);
        inventario[slotJugador] = itemBaul;

        AsegurarTamanioCantidades();
        cantidades[slotJugador] = cantidadBaulDefault;
        baul.SetCantidad(slotBaul, cantidadJugadorDefault);

        if (inv != null) inv.imagenesInventario();
        if (baul.panelInventarioBaul != null)
        {
            var invBaul = baul.panelInventarioBaul.GetComponent<InventarioBaulGrafico>();
            if (invBaul != null) invBaul.Refrescar();
        }
    }

    /// <summary>
    /// Mueve un objeto del cadáver al slot del jugador.
    /// Si el slot del jugador ya contiene un acumulable del mismo tipo, se acumula en lugar de intercambiar.
    /// </summary>
    public void RecibirItemDesdeCadaver(CadaverInteractuable cadaver, int slotCadaver, int slotJugador)
    {
        if (cadaver == null || inventario == null || slotJugador < 0 || slotJugador >= inventario.Count) return;

        GameObject itemCadaver = cadaver.GetContenido(slotCadaver);
        if (itemCadaver == null) return;

        GameObject itemJugador = inventario[slotJugador];

        var recCadaver = itemCadaver.GetComponent<ObjetoRecogible>();
        var recJugador = itemJugador != null ? itemJugador.GetComponent<ObjetoRecogible>() : null;

        // Caso especial: ambos son acumulables del mismo tipo → acumular en el slot del jugador.
        if (itemJugador != null &&
            recCadaver != null && recJugador != null &&
            recCadaver.categoria == CategoriaObjeto.Acumulable &&
            recJugador.categoria == CategoriaObjeto.Acumulable &&
            recCadaver.textura == recJugador.textura)
        {
            int cantidadCadaver = cadaver.GetCantidad(slotCadaver);
            int cantidadJugador = GetCantidadEnSlot(slotJugador);

            int suma = Mathf.Max(1, cantidadJugador) + Mathf.Max(1, cantidadCadaver);

            AsegurarTamanioCantidades();
            cantidades[slotJugador] = suma;

            // Vaciar slot del cadáver
            cadaver.SetContenido(slotCadaver, null);
            cadaver.SetCantidad(slotCadaver, 0);

            if (inv != null) inv.imagenesInventario();
            if (cadaver.panelInventarioCadaver != null)
            {
                var invCadaver = cadaver.panelInventarioCadaver.GetComponent<InventarioCadaverGrafico>();
                if (invCadaver != null) invCadaver.Refrescar();
            }
            return;
        }

        // Comportamiento por defecto: intercambio de objetos entre cadáver y jugador.
        int cantidadCadaverDefault = cadaver.GetCantidad(slotCadaver);
        int cantidadJugadorDefault = GetCantidadEnSlot(slotJugador);

        cadaver.SetContenido(slotCadaver, itemJugador);
        inventario[slotJugador] = itemCadaver;

        AsegurarTamanioCantidades();
        cantidades[slotJugador] = cantidadCadaverDefault;
        cadaver.SetCantidad(slotCadaver, cantidadJugadorDefault);

        if (inv != null) inv.imagenesInventario();
        if (cadaver.panelInventarioCadaver != null)
        {
            var invCadaver = cadaver.panelInventarioCadaver.GetComponent<InventarioCadaverGrafico>();
            if (invCadaver != null) invCadaver.Refrescar();
        }
    }

    /// <summary>
    /// Mueve un objeto del inventario del jugador al slot del cadáver (intercambio si el slot tiene objeto).
    /// </summary>
    public void EnviarItemAlCadaver(CadaverInteractuable cadaver, int slotJugador, int slotCadaver)
    {
        if (cadaver == null || inventario == null || slotJugador < 0 || slotJugador >= inventario.Count) return;
        GameObject itemJugador = inventario[slotJugador];
        if (itemJugador == null) return;

        if (ObjetoRecogible.EsEquipable(itemJugador) && tieneArmaEquipada)
            DesequiparArma();
        if (slotArmadura == itemJugador)
        {
            DesequiparArmadura();
            slotArmadura = null;
        }

        int cantidadJugador = GetCantidadEnSlot(slotJugador);
        int cantidadCadaver = cadaver.GetCantidad(slotCadaver);

        GameObject itemCadaver = cadaver.GetContenido(slotCadaver);
        inventario[slotJugador] = itemCadaver;
        cadaver.SetContenido(slotCadaver, itemJugador);

        AsegurarTamanioCantidades();
        cantidades[slotJugador] = cantidadCadaver;
        cadaver.SetCantidad(slotCadaver, cantidadJugador);
        if (inv != null) inv.imagenesInventario();
        if (cadaver.panelInventarioCadaver != null)
        {
            var invCadaver = cadaver.panelInventarioCadaver.GetComponent<InventarioCadaverGrafico>();
            if (invCadaver != null) invCadaver.Refrescar();
        }
    }

    /// <summary>
    /// Mueve un objeto del inventario del jugador al slot de la hoguera. Si el objeto es Rama, enciende el fuego.
    /// </summary>
    public void EnviarItemAlHoguera(HogueraInteractuable hoguera, int slotJugador, int slotHoguera)
    {
        if (hoguera == null || inventario == null || slotJugador < 0 || slotJugador >= inventario.Count) return;
        GameObject itemJugador = inventario[slotJugador];
        if (itemJugador == null) return;

        if (ObjetoRecogible.EsEquipable(itemJugador) && tieneArmaEquipada)
            DesequiparArma();
        if (slotArmadura == itemJugador)
        {
            DesequiparArmadura();
            slotArmadura = null;
        }

        int cantidadJugador = GetCantidadEnSlot(slotJugador);
        int cantidadHoguera = hoguera.GetCantidad(slotHoguera);

        GameObject itemHoguera = hoguera.GetContenido(slotHoguera);
        inventario[slotJugador] = itemHoguera;
        hoguera.SetContenido(slotHoguera, itemJugador);

        AsegurarTamanioCantidades();
        cantidades[slotJugador] = cantidadHoguera;
        hoguera.SetCantidad(slotHoguera, cantidadJugador);

        if (NombreLimpioObjeto(itemJugador).Trim().Equals("Rama", System.StringComparison.OrdinalIgnoreCase))
            hoguera.EncenderFuego();

        if (inv != null) inv.imagenesInventario();
        if (hoguera.panelInventarioHoguera != null)
        {
            var invHoguera = hoguera.panelInventarioHoguera.GetComponent<InventarioHogueraGrafico>();
            if (invHoguera != null) invHoguera.Refrescar();
        }
    }

    /// <summary>Mueve la armadura del slot armadura al cadáver.</summary>
    public void EnviarArmaduraAlCadaver(CadaverInteractuable cadaver, int slotCadaver)
    {
        if (cadaver == null || slotArmadura == null) return;
        GameObject itemCadaver = cadaver.GetContenido(slotCadaver);
        cadaver.SetContenido(slotCadaver, slotArmadura);
        slotArmadura = itemCadaver;
        if (slotArmadura != null)
            EquiparArmadura(slotArmadura);
        else
            DesequiparArmadura();
        if (inv != null) inv.imagenesInventario();
        if (cadaver.panelInventarioCadaver != null)
        {
            var invCadaver = cadaver.panelInventarioCadaver.GetComponent<InventarioCadaverGrafico>();
            if (invCadaver != null) invCadaver.Refrescar();
        }
    }

    /// <summary>
    /// Mueve un objeto de la hoguera al slot del jugador.
    /// </summary>
    public void RecibirItemDesdeHoguera(HogueraInteractuable hoguera, int slotHoguera, int slotJugador)
    {
        if (hoguera == null || inventario == null || slotJugador < 0 || slotJugador >= inventario.Count) return;

        GameObject itemHoguera = hoguera.GetContenido(slotHoguera);
        if (itemHoguera == null) return;

        GameObject itemJugador = inventario[slotJugador];

        var recHoguera = itemHoguera.GetComponent<ObjetoRecogible>();
        var recJugador = itemJugador != null ? itemJugador.GetComponent<ObjetoRecogible>() : null;

        if (itemJugador != null &&
            recHoguera != null && recJugador != null &&
            recHoguera.categoria == CategoriaObjeto.Acumulable &&
            recJugador.categoria == CategoriaObjeto.Acumulable &&
            recHoguera.textura == recJugador.textura)
        {
            int cantidadHoguera = hoguera.GetCantidad(slotHoguera);
            int cantidadJugador = GetCantidadEnSlot(slotJugador);
            int suma = Mathf.Max(1, cantidadJugador) + Mathf.Max(1, cantidadHoguera);
            AsegurarTamanioCantidades();
            cantidades[slotJugador] = suma;
            hoguera.SetContenido(slotHoguera, null);
            hoguera.SetCantidad(slotHoguera, 0);
            if (inv != null) inv.imagenesInventario();
            if (hoguera.panelInventarioHoguera != null)
            {
                var invHoguera = hoguera.panelInventarioHoguera.GetComponent<InventarioHogueraGrafico>();
                if (invHoguera != null) invHoguera.Refrescar();
            }
            return;
        }

        int cantidadHogueraDefault = hoguera.GetCantidad(slotHoguera);
        int cantidadJugadorDefault = GetCantidadEnSlot(slotJugador);
        hoguera.SetContenido(slotHoguera, itemJugador);
        inventario[slotJugador] = itemHoguera;
        AsegurarTamanioCantidades();
        cantidades[slotJugador] = cantidadHogueraDefault;
        hoguera.SetCantidad(slotHoguera, cantidadJugadorDefault);
        if (inv != null) inv.imagenesInventario();
        if (hoguera.panelInventarioHoguera != null)
        {
            var invHoguera = hoguera.panelInventarioHoguera.GetComponent<InventarioHogueraGrafico>();
            if (invHoguera != null) invHoguera.Refrescar();
        }
    }

    /// <summary>Mueve la armadura del slot armadura a la hoguera.</summary>
    public void EnviarArmaduraAlHoguera(HogueraInteractuable hoguera, int slotHoguera)
    {
        if (hoguera == null || slotArmadura == null) return;
        GameObject itemHoguera = hoguera.GetContenido(slotHoguera);
        hoguera.SetContenido(slotHoguera, slotArmadura);
        slotArmadura = itemHoguera;
        if (slotArmadura != null)
            EquiparArmadura(slotArmadura);
        else
            DesequiparArmadura();
        if (inv != null) inv.imagenesInventario();
        if (hoguera.panelInventarioHoguera != null)
        {
            var invHoguera = hoguera.panelInventarioHoguera.GetComponent<InventarioHogueraGrafico>();
            if (invHoguera != null) invHoguera.Refrescar();
        }
    }

    /// <summary>
    /// Mueve un objeto del inventario del jugador al slot del baúl (intercambio si el slot tiene objeto).
    /// </summary>
    public void EnviarItemAlBaul(BaulInteractuable baul, int slotJugador, int slotBaul)
    {
        if (baul == null || inventario == null || slotJugador < 0 || slotJugador >= inventario.Count) return;
        GameObject itemJugador = inventario[slotJugador];
        if (itemJugador == null) return;

        if (ObjetoRecogible.EsEquipable(itemJugador) && tieneArmaEquipada)
            DesequiparArma();
        if (slotArmadura == itemJugador)
        {
            DesequiparArmadura();
            slotArmadura = null;
        }

        // Cantidades de acumulables: intercambiamos también los contadores entre inventario y baúl.
        int cantidadJugador = GetCantidadEnSlot(slotJugador);
        int cantidadBaul = baul.GetCantidad(slotBaul);

        GameObject itemBaul = baul.GetContenido(slotBaul);
        inventario[slotJugador] = itemBaul;
        baul.SetContenido(slotBaul, itemJugador);

        // Actualizar cantidades asociadas a los objetos intercambiados.
        AsegurarTamanioCantidades();
        cantidades[slotJugador] = cantidadBaul;
        baul.SetCantidad(slotBaul, cantidadJugador);
        if (inv != null) inv.imagenesInventario();
        if (baul.panelInventarioBaul != null)
        {
            var invBaul = baul.panelInventarioBaul.GetComponent<InventarioBaulGrafico>();
            if (invBaul != null) invBaul.Refrescar();
        }
    }

    /// <summary>Mueve la armadura del slot armadura al ba�l.</summary>
    public void EnviarArmaduraAlBaul(BaulInteractuable baul, int slotBaul)
    {
        if (baul == null || slotArmadura == null) return;
        GameObject itemBaul = baul.GetContenido(slotBaul);
        baul.SetContenido(slotBaul, slotArmadura);
        slotArmadura = itemBaul;
        if (slotArmadura != null)
            EquiparArmadura(slotArmadura);
        else
            DesequiparArmadura();
        if (inv != null) inv.imagenesInventario();
        if (baul.panelInventarioBaul != null)
        {
            var invBaul = baul.panelInventarioBaul.GetComponent<InventarioBaulGrafico>();
            if (invBaul != null) invBaul.Refrescar();
        }
    }

    /// <summary>
    /// Accion contextual con F: si hay un ba�l delante, lo abre; si hay un objeto
    /// recogible (hacha o armadura) delante, lo recoge.
    /// </summary>
    private void TryAccionConF()
    {
        if (!Input.GetKeyDown(KeyCode.F)) return;
        if (inventario == null || inv == null) return;

        if (isActive && BaulAbierto != null)
        {
            if (BaulAbierto.panelInventarioBaul != null)
                BaulAbierto.panelInventarioBaul.SetActive(false);
            BaulAbierto.Cerrar();
            BaulAbierto = null;
            return;
        }

        if (isActive && CadaverAbierto != null)
        {
            if (CadaverAbierto.panelInventarioCadaver != null)
                CadaverAbierto.panelInventarioCadaver.SetActive(false);
            CadaverAbierto.Cerrar();
            CadaverAbierto = null;
            return;
        }

        if (isActive && HogueraAbierto != null)
        {
            if (HogueraAbierto.panelInventarioHoguera != null)
                HogueraAbierto.panelInventarioHoguera.SetActive(false);
            HogueraAbierto.Cerrar();
            HogueraAbierto = null;
            return;
        }

        if (isActive) return;

        MovimientoPorCeldas move = GetComponent<MovimientoPorCeldas>();
        if (move == null) return;

        Vector2 playerPos = transform.position;
        Vector2 dir = move.GetLastInputDirection();
        if (dir.sqrMagnitude < 0.01f) return;

        const float cellSize = 1f;
        Vector2 cellInFront = playerPos + dir * cellSize;
        Collider2D[] hits = Physics2D.OverlapCircleAll(cellInFront, 0.55f);
        foreach (Collider2D col in hits)
        {
            if (col == null || !col.gameObject.activeInHierarchy) continue;

            GameObject go = col.gameObject;

            // 1) Si es un arbusto con frutos, recoger frutos SOLO si la celda justo delante coincide
            //    con la celda base del arbusto (evita diagonales o posiciones incorrectas).
            if (go.CompareTag("Arbusto"))
            {
                var arb = go.GetComponent<Arbusto>();
                if (arb != null && arb.TieneFrutos)
                {
                    // Celda en la que el jugador está y celda justo delante (grid-alineado)
                    Vector2 celdaJugador = move.GetPosicionCeldaActual();
                    Vector2 celdaEnfrente = celdaJugador + dir * cellSize;

                    // Aproximamos la celda base del arbusto con el centro de su collider
                    var col2D = col as Collider2D;
                    Vector2 baseArbusto = col2D != null ? (Vector2)col2D.bounds.center : (Vector2)go.transform.position;

                    // Si la distancia entre la celda objetivo y la base del arbusto es demasiado grande,
                    // no estamos mirando correctamente a su celda.
                    if (Vector2.Distance(baseArbusto, celdaEnfrente) > 0.3f)
                        continue;

                    GameObject fruto = arb.RecogerFrutos();
                    if (fruto != null)
                    {
                        objeto = fruto;
                        RecogerObjeto();
                        objeto = null;
                    }
                    return;
                }
                continue;
            }

            // 2) Si es un cadáver interactuable, abrirlo (celda adyacente mirándolo).
            var cadaver = go.GetComponent<CadaverInteractuable>();
            if (cadaver != null)
            {
                Vector2 celdaJugador = move.GetPosicionCeldaActual();
                Vector2 celdaEnfrente = celdaJugador + dir * cellSize;
                if (Vector2.Distance((Vector2)go.transform.position, celdaEnfrente) <= 0.5f)
                {
                    cadaver.Abrir();
                    return;
                }
                continue;
            }

            // 3) Si es un baúl interactuable, abrirlo (solo desde la casilla inferior mirándolo).
            var baul = go.GetComponent<BaulInteractuable>();
            if (baul != null)
            {
                Vector2 delta = (Vector2)go.transform.position - playerPos;
                bool mismaColumna = Mathf.Abs(delta.x) < 0.3f * cellSize;
                bool baulArriba = delta.y > 0.3f * cellSize;
                bool mirandoArriba = dir.y > 0.7f && Mathf.Abs(dir.x) < 0.2f;
                if (mismaColumna && baulArriba && mirandoArriba)
                {
                    baul.Abrir();
                    return;
                }
                continue;
            }

            // 4) Si es una hoguera interactuable, abrirla (celda adyacente mirándola).
            var hoguera = go.GetComponent<HogueraInteractuable>();
            if (hoguera != null)
            {
                Vector2 celdaJugador = move.GetPosicionCeldaActual();
                Vector2 celdaEnfrente = celdaJugador + dir * cellSize;
                if (Vector2.Distance((Vector2)go.transform.position, celdaEnfrente) <= 0.5f)
                {
                    hoguera.Abrir();
                    return;
                }
                continue;
            }

            // 5) Si es un objeto recogible con categoria asignada, recogerlo
            //    SOLO si está en la celda justo enfrente del jugador.
            if (!go.CompareTag(TagRecogible)) continue;
            if (go.CompareTag(TagConstruccionColocable)) continue;

            var recogible = go.GetComponent<ObjetoRecogible>();
            if (recogible == null || recogible.categoria == CategoriaObjeto.Ninguno) continue;

            {
                Vector2 celdaJugador = move.GetPosicionCeldaActual();
                Vector2 celdaEnfrente = celdaJugador + dir * cellSize;
                if (Vector2.Distance((Vector2)go.transform.position, celdaEnfrente) > 0.5f)
                    continue;
            }

            objeto = go;
            RecogerObjeto();
            objeto = null;
            return;
        }
    }

    /// <summary>
    /// Acción con E: encender o apagar la hoguera que está delante. Solo se puede encender si hay al menos una Rama en el inventario de la hoguera.
    /// </summary>
    private void TryAccionConE()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        MovimientoPorCeldas move = GetComponent<MovimientoPorCeldas>();
        if (move == null) return;

        Vector2 playerPos = transform.position;
        Vector2 dir = move.GetLastInputDirection();
        if (dir.sqrMagnitude < 0.01f) return;

        const float cellSize = 1f;
        Vector2 cellInFront = playerPos + dir * cellSize;
        Collider2D[] hits = Physics2D.OverlapCircleAll(cellInFront, 0.55f);
        foreach (Collider2D col in hits)
        {
            if (col == null || !col.gameObject.activeInHierarchy) continue;
            GameObject go = col.gameObject;

            var hoguera = go.GetComponent<HogueraInteractuable>();
            if (hoguera != null)
            {
                Vector2 celdaJugador = move.GetPosicionCeldaActual();
                Vector2 celdaEnfrente = celdaJugador + dir * cellSize;
                if (Vector2.Distance((Vector2)go.transform.position, celdaEnfrente) <= 0.5f)
                {
                    hoguera.IntentarEncenderOApagar();
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Teclas 1?4: usan el objeto de los slots 1?4 del inventario.
    /// De momento, para probar, si el slot tiene un hacha se equipa / desequipa.
    /// </summary>
    private void ProcesarTeclasAccesoRapido()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) UsarSlotRapido(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) UsarSlotRapido(1);
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) UsarSlotRapido(2);
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) UsarSlotRapido(3);
    }

    private void UsarSlotRapido(int index)
    {
        if (inventario == null) return;
        if (index < 0 || index >= inventario.Count) return;

        GameObject item = inventario[index];
        if (item == null) return;

        var recogible = item.GetComponent<ObjetoRecogible>();
        if (recogible == null) return;

        if (recogible.categoria == CategoriaObjeto.Arma || recogible.categoria == CategoriaObjeto.Herramienta)
        {
            if (!tieneArmaEquipada)
            {
                DatosArma datos = recogible.datosArma;
                if (datos == null || datos.prefabVisual == null)
                {
                    Debug.LogWarning($"Acceso rapido {index + 1}: el equipable '{item.name}' no tiene DatosArma o prefabVisual asignado.");
                    return;
                }
                Debug.Log($"Acceso rapido {index + 1}: equipar desde el slot {index + 1}.");
                EquiparArma(datos);
            }
            else
            {
                Debug.Log($"Acceso rapido {index + 1}: desequipar.");
                DesequiparArma();
            }
        }
    }

    public void EquiparArma(DatosArma datos)
    {
        if (datos == null || datos.prefabVisual == null)
        {
            Debug.LogWarning("Inventario: DatosArma o prefabVisual es null. No se puede equipar.");
            return;
        }

        Transform puntoEquipo = mano != null ? mano : BuscarManoOTool();
        if (puntoEquipo == null)
        {
            Debug.LogWarning("Inventario: no hay transform 'mano' asignado ni hijo 'mano'/'tool'. Asigna 'mano' en el Inspector.");
            return;
        }

        DesequiparArma();

        puntoEquipo.gameObject.SetActive(true);
        Transform p = puntoEquipo.transform.parent;
        while (p != null && p != transform)
        {
            if (!p.gameObject.activeSelf)
                p.gameObject.SetActive(true);
            p = p.parent;
        }

        var srMano = puntoEquipo.GetComponent<SpriteRenderer>();
        if (srMano != null)
            srMano.enabled = false;

        armaInstancia = Instantiate(datos.prefabVisual, puntoEquipo.position, puntoEquipo.rotation);
        armaInstancia.transform.SetParent(puntoEquipo);
        armaInstancia.transform.localPosition = Vector3.zero;
        armaInstancia.transform.localRotation = Quaternion.identity;
        armaInstancia.transform.localScale = Vector3.one;
        armaInstancia.SetActive(true);
        armaEquipadaDatos = datos;

        // Si es la antorcha, añadimos el controlador de encendido con E.
        // (El prefab actual de antorcha no trae el script en este proyecto.)
        {
            var anim = armaInstancia.GetComponentInChildren<Animator>(true);
            if (anim != null && anim.runtimeAnimatorController != null && anim.runtimeAnimatorController.name.Contains("Antorcha"))
            {
                if (armaInstancia.GetComponent<AntorchaLuz>() == null)
                    armaInstancia.AddComponent<AntorchaLuz>();
            }
        }

        // Asegura que el hijo "antorcha encendida" quede siempre 1 orden por encima.
        {
            Transform tEncendida = null;
            foreach (Transform t in armaInstancia.GetComponentsInChildren<Transform>(true))
            {
                if (t == null) continue;
                var n = t.name != null ? t.name.ToLowerInvariant() : "";
                if (n.Contains("antorcha") && n.Contains("encendida"))
                {
                    tEncendida = t;
                    break;
                }
            }

            if (tEncendida != null)
            {
                if (tEncendida.GetComponent<AntorchaEncendidaSortingOffset>() == null)
                    tEncendida.gameObject.AddComponent<AntorchaEncendidaSortingOffset>();
            }
        }

        foreach (var skin in armaInstancia.GetComponentsInChildren<SkinsAnimaciones>(true))
            Destroy(skin);

        foreach (var a in armaInstancia.GetComponentsInChildren<Animator>(true))
        {
            if (a == null) continue;
            a.keepAnimatorStateOnDisable = true;
            a.enabled = true;
        }

        SpriteRenderer srCuerpo = null;
        var bodyT = transform.Find("body");
        if (bodyT != null) srCuerpo = bodyT.GetComponent<SpriteRenderer>();
        if (srCuerpo == null)
        {
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null) continue;
                if (sr.transform.IsChildOf(puntoEquipo)) continue;
                srCuerpo = sr;
                break;
            }
        }

        foreach (var sr in armaInstancia.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null) continue;
            sr.gameObject.SetActive(true);
            sr.enabled = true;
            try { sr.sortingLayerName = "Player"; } catch { }
            if (materialArmasLit != null)
                sr.sharedMaterial = materialArmasLit;
        }

        tieneArmaEquipada = true;

        var move = GetComponent<MovimientoPorCeldas>();
        if (move != null)
        {
            move.RefrescarAnimatorsHijos();
            move.ForzarEstadoArmaParado();
        }

        ActualizarArmaEquipada();
    }

    public void DesequiparArma()
    {
        if (armaInstancia != null)
        {
            Destroy(armaInstancia);
            armaInstancia = null;
        }
        armaEquipadaDatos = null;

        Transform puntoEquipo = mano ?? (transform.Find("legs/tool") ?? BuscarManoOTool());
        if (puntoEquipo != null)
        {
            foreach (Transform child in puntoEquipo)
                Destroy(child.gameObject);
            var srMano = puntoEquipo.GetComponent<SpriteRenderer>();
            if (srMano != null) srMano.enabled = true;
        }

        tieneArmaEquipada = false;

        var move = GetComponent<MovimientoPorCeldas>();
        if (move != null) move.RefrescarAnimatorsHijos();
    }

    void ActualizarArmaEquipada()
    {
        if (armaInstancia == null) return;

        Transform puntoEquipo = mano != null ? mano : BuscarManoOTool();
        if (puntoEquipo != null)
            puntoEquipo.localPosition = Vector3.zero;

        armaInstancia.transform.localPosition = Vector3.zero;
    }

    Transform BuscarManoOTool()
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            if (n == "mano" || n == "tool") return t;
        }
        return null;
    }

    static bool EsArmadura(GameObject obj)
    {
        return ObjetoRecogible.EsArmaduraPorCategoria(obj);
    }

    public static bool EsObjetoArmadura(GameObject obj)
    {
        return EsArmadura(obj);
    }

    public void EquiparArmadura(GameObject obj)
    {
        GameObject visual = ObtenerVisualArmadura();
        if (visual != null)
        {
            visual.SetActive(true);
            // Activar tambien los padres (ej. "body") para que el armor sea visible
            Transform p = visual.transform.parent;
            while (p != null && p != transform)
            {
                if (!p.gameObject.activeSelf)
                    p.gameObject.SetActive(true);
                p = p.parent;
            }
            Debug.Log("[Inventario] Armadura equipada: activado '" + visual.name + "'");
        }
        else
            Debug.LogWarning("[Inventario] No se encontro el visual de armadura (objeto 'armor') en el personaje.");

        var move = GetComponent<MovimientoPorCeldas>();
        if (move != null) move.RefrescarAnimatorsHijos();
    }

    GameObject ObtenerVisualArmadura()
    {
        if (armaduraEquipadaVisual != null) return armaduraEquipadaVisual;
        // Buscar solo en el personaje (no en transform.root para no pillar el UI "Slot Armadura")
        Transform donde = transform;
        foreach (Transform t in donde.GetComponentsInChildren<Transform>(true))
        {
            if (t == donde) continue;
            string n = t.name ?? "";
            if (n.Equals("armor", System.StringComparison.OrdinalIgnoreCase))
                return t.gameObject;
        }
        foreach (Transform t in donde.GetComponentsInChildren<Transform>(true))
        {
            if (t == donde) continue;
            string n = t.name ?? "";
            if (n.IndexOf("Armadura", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return t.gameObject;
        }
        return null;
    }

    public void DesequiparArmadura()
    {
        GameObject visual = ObtenerVisualArmadura();
        if (visual != null)
            visual.SetActive(false);

        var move = GetComponent<MovimientoPorCeldas>();
        if (move != null) move.RefrescarAnimatorsHijos();
    }

    public void PonerEnSlotArmadura(int gridSlotIndex)
    {
        if (inventario == null || gridSlotIndex < 0 || gridSlotIndex >= inventario.Count) return;
        GameObject obj = inventario[gridSlotIndex];
        if (obj == null || !EsArmadura(obj)) return;
        GameObject anterior = slotArmadura;
        inventario[gridSlotIndex] = anterior;
        slotArmadura = obj;
        UnityEngine.Debug.Log("[Inventario] PonerEnSlotArmadura: equipando '" + obj.name + "'");
        EquiparArmadura(obj);
        if (inv != null) inv.imagenesInventario();
    }

    public void MoverArmaduraAGridSlot(int gridSlotIndex)
    {
        if (slotArmadura == null) return;
        if (inventario == null || gridSlotIndex < 0 || gridSlotIndex >= inventario.Count) return;
        GameObject temp = inventario[gridSlotIndex];
        inventario[gridSlotIndex] = slotArmadura;
        slotArmadura = temp;
        if (slotArmadura != null)
            EquiparArmadura(slotArmadura);
        else
            DesequiparArmadura();
        if (inv != null) inv.imagenesInventario();
    }

    void InicializarCantidades()
    {
        if (cantidades == null) cantidades = new List<int>();
        while (cantidades.Count < inventario.Count)
            cantidades.Add(0);
    }

    void AsegurarTamanioCantidades()
    {
        if (cantidades == null) cantidades = new List<int>();
        while (cantidades.Count < inventario.Count)
            cantidades.Add(0);
    }

    public int GetCantidadEnSlot(int index)
    {
        if (inventario == null || index < 0 || index >= inventario.Count) return 0;
        AsegurarTamanioCantidades();
        int c = cantidades[index];
        if (c <= 0 && inventario[index] != null) return 1;
        return c;
    }

    public void SetCantidadEnSlot(int index, int valor)
    {
        if (index < 0) return;
        AsegurarTamanioCantidades();
        if (index < cantidades.Count)
            cantidades[index] = Mathf.Max(0, valor);
    }

    void ResetearEstadoRecogible(GameObject obj)
    {
        if (obj == null) return;
        var rec = obj.GetComponent<ObjetoRecogible>();
        if (rec == null) return;
        rec.esRecogido = false;
        rec.player = null;
    }

    private static void SetTagSafe(GameObject obj, string tagName)
    {
        if (obj == null || string.IsNullOrEmpty(tagName)) return;
        try
        {
            obj.tag = tagName;
        }
        catch (UnityEngine.UnityException)
        {
            Debug.LogWarning($"Inventario: el tag '{tagName}' no existe. Crea el tag en Unity (Tags & Layers) para que funcione la restriccion de pickup.");
        }
    }

    string NombreLimpioObjeto(GameObject obj)
    {
        if (obj == null) return string.Empty;
        string n = obj.name ?? string.Empty;
        int idx = n.IndexOf("(Clone)");
        if (idx >= 0) n = n.Substring(0, idx);
        return n.Trim();
    }
}
