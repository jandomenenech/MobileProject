using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class Inventario : MonoBehaviour
{
    public List<GameObject> inventario;
    public GameObject objeto;
    public GameObject inventarioGrafico;
    public InventarioGrafico inv;
    public ObjetoRecogible recoger;
    public Arbusto arbusto;


    [Header("Arma equipada")]
    [Tooltip("Transform donde se instancia el arma (hijo 'mano' o 'tool').")]
    public Transform mano;
    private GameObject armaInstancia;
    private DatosArma armaEquipadaDatos;

    [HideInInspector] public bool tieneArmaEquipada = false;

    public bool TieneArmaEquipada => tieneArmaEquipada;

    [Header("Armadura")]
    [Tooltip("Objeto en el slot de armadura (null si no hay ninguna).")]
    [HideInInspector] public GameObject slotArmadura;
    [Tooltip("Visual de la armadura equipada (se activa al tener armadura en el slot). Asigna el GameObject que muestra Armadura 2.")]
    public GameObject armaduraEquipadaVisual;

    public bool isActive = false;

    [Header("Ba�l")]
    [Tooltip("Ba�l cuyo inventario est� abierto (null si ninguno).")]
    [HideInInspector] public BaulInteractuable BaulAbierto;

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
        DesequiparArma();
        DesequiparArmadura();
    }

    void Update()
    {
        obtenerObjeto();
        activarInventario();
        TryAccionConF();
        ProcesarTeclasAccesoRapido();
        if (tieneArmaEquipada && armaInstancia != null)
            ActualizarArmaEquipada();
    }

    void LateUpdate()
    {
        if (armaInstancia != null)
            armaInstancia.transform.localPosition = Vector3.zero;
    }

    public void obtenerObjeto() {
        if (Input.GetKeyDown(KeyCode.E))// || Input.GetMouseButtonDown(0))
        {
            if(objeto != null)
            {
                RecogerObjeto();
                inv.imagenesInventario();
            }
            
        }
    }

    private void RecogerObjeto()
    {
        if (objeto == null) return;

        // Buscar primer espacio vac�o
        int indiceLibre = inventario.FindIndex(item => item == null);

        if (indiceLibre != -1)
        {
            inventario[indiceLibre] = objeto;
            Debug.Log($"Inventario: a�adido '{objeto.name}' en el slot {indiceLibre + 1}.");
        }
        else
        {
            inventario.Add(objeto); // Opcional: agrega al final si no hay espacio
            Debug.Log($"Inventario: a�adido '{objeto.name}' al final (sin huecos libres previos).");
        }

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

        if (ObjetoRecogible.EsArma(obj) && tieneArmaEquipada)
            DesequiparArma();

        var movimiento = GetComponent<MovimientoPorCeldas>();
        Vector2 posCelda = movimiento != null ? movimiento.GetPosicionCeldaActual() : (Vector2)transform.position;
        obj.transform.position = posCelda;
        obj.SetActive(true);
        inventario[slotIndex] = null;
        if (inv != null)
            inv.imagenesInventario();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Recogible"))
        {
            Debug.Log("Objeto en rango: " + collision.gameObject.name);
            objeto = collision.gameObject;
           // recoger = objeto.GetComponent<ObjetoRecogible>();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Recogible"))
        {
            Debug.Log("Objeto no en rango: " + collision.gameObject.name);
            objeto = null;
        }
        
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
                inv.imagenesInventario();
                if (BaulAbierto != null)
                {
                    if (BaulAbierto.panelInventarioBaul != null)
                        BaulAbierto.panelInventarioBaul.SetActive(false);
                    BaulAbierto.Cerrar();
                    BaulAbierto = null;
                }
            }
        }
    }

    /// <summary>
    /// Abre el inventario del personaje y el panel del ba�l. Llamado por BaulInteractuable.Abrir().
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
    /// Mueve un objeto del ba�l al slot del jugador (intercambio si el slot tiene objeto).
    /// </summary>
    public void RecibirItemDesdeBaul(BaulInteractuable baul, int slotBaul, int slotJugador)
    {
        if (baul == null || inventario == null || slotJugador < 0 || slotJugador >= inventario.Count) return;
        GameObject itemBaul = baul.GetContenido(slotBaul);
        GameObject itemJugador = inventario[slotJugador];
        baul.SetContenido(slotBaul, itemJugador);
        inventario[slotJugador] = itemBaul;
        if (inv != null) inv.imagenesInventario();
        if (baul.panelInventarioBaul != null)
        {
            var invBaul = baul.panelInventarioBaul.GetComponent<InventarioBaulGrafico>();
            if (invBaul != null) invBaul.Refrescar();
        }
    }

    /// <summary>
    /// Mueve un objeto del inventario del jugador al slot del ba�l (intercambio si el slot tiene objeto).
    /// </summary>
    public void EnviarItemAlBaul(BaulInteractuable baul, int slotJugador, int slotBaul)
    {
        if (baul == null || inventario == null || slotJugador < 0 || slotJugador >= inventario.Count) return;
        GameObject itemJugador = inventario[slotJugador];
        if (itemJugador == null) return;

        if (ObjetoRecogible.EsArma(itemJugador) && tieneArmaEquipada)
            DesequiparArma();
        if (slotArmadura == itemJugador)
        {
            DesequiparArmadura();
            slotArmadura = null;
        }

        GameObject itemBaul = baul.GetContenido(slotBaul);
        inventario[slotJugador] = itemBaul;
        baul.SetContenido(slotBaul, itemJugador);
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

            Vector2 toObject = ((Vector2)col.transform.position - playerPos).normalized;
            if (Vector2.Dot(toObject, dir) < 0.7f) continue;

            GameObject go = col.gameObject;

            // 1) Si es un baúl interactuable, abrirlo (solo desde la casilla inferior mirándolo).
            var baul = go.GetComponent<BaulInteractuable>();
            if (baul != null)
            {
                Vector2 delta = (Vector2)go.transform.position - playerPos;
                // Queremos: jugador justo debajo del baúl (misma columna) y mirando hacia arriba.
                bool mismaColumna = Mathf.Abs(delta.x) < 0.3f * cellSize;
                bool baulArriba = delta.y > 0.3f * cellSize;
                bool mirandoArriba = dir.y > 0.7f && Mathf.Abs(dir.x) < 0.2f;
                if (mismaColumna && baulArriba && mirandoArriba)
                {
                    baul.Abrir();
                    return;
                }

                // Si este collider es un baúl pero no cumple las condiciones (por ejemplo,
                // es el baúl de al lado), seguimos buscando otros colliders en el bucle.
                continue;
            }

            // 2) Si es un objeto recogible con categoria asignada, recogerlo.
            if (!go.CompareTag("Recogible")) continue;

            var recogible = go.GetComponent<ObjetoRecogible>();
            if (recogible == null || recogible.categoria == CategoriaObjeto.Ninguno) continue;

            objeto = go;
            RecogerObjeto();
            objeto = null;
            return;
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

        if (recogible.categoria == CategoriaObjeto.Arma)
        {
            if (!tieneArmaEquipada)
            {
                DatosArma datos = recogible.datosArma;
                if (datos == null || datos.prefabVisual == null)
                {
                    Debug.LogWarning($"Acceso rapido {index + 1}: el arma '{item.name}' no tiene DatosArma o prefabVisual asignado.");
                    return;
                }
                Debug.Log($"Acceso rapido {index + 1}: equipar arma desde el slot {index + 1}.");
                EquiparArma(datos);
            }
            else
            {
                Debug.Log($"Acceso rapido {index + 1}: desequipar arma.");
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
            if (srCuerpo != null)
            {
                sr.sortingLayerID = srCuerpo.sortingLayerID;
                sr.sortingOrder = srCuerpo.sortingOrder + 5;
            }
            else
            {
                try { sr.sortingLayerName = "Player"; } catch { }
                sr.sortingOrder = 1;
            }
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
}
