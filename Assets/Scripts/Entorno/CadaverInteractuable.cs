using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cadáver de NPC que puede abrirse con la tecla de acción (F) cuando el personaje
/// está en una celda adyacente mirándolo. Contiene un inventario (ej. carne) que se muestra al abrir.
/// Reutilizable para distintos cadáveres (conejo, otros NPCs).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CadaverInteractuable : MonoBehaviour
{
    [Tooltip("Panel UI del Inventario Cadáver (se activa al abrir).")]
    public GameObject panelInventarioCadaver;

    [Tooltip("Referencia al Inventario del personaje (para abrir inventario + cadáver).")]
    public Inventario inventarioJugador;

    [Tooltip("Número de slots del inventario del cadáver.")]
    public int cantidadSlots = 6;

    [Header("Botín inicial (varios objetos)")]
    [Tooltip("Lista de objetos iniciales: cada entrada ocupa un slot consecutivo (0, 1, 2…). Tiene prioridad sobre carne legacy si no se asigna desde el NPC.")]
    public LootInicialCadaverEntrada[] lootInicial;

    [Tooltip("Prefab de Carne de conejo para rellenar inicialmente (conejo). Asignar en el prefab del cadáver.")]
    public GameObject prefabCarneConejo;

    [Tooltip("Cantidad de carne de conejo inicial en el slot 0 (solo para cadáver de conejo).")]
    public int cantidadCarneInicial = 1;

    /// <summary>
    /// Si el NPC asigna botín al instanciar el cadáver, se usa en lugar de lootInicial del prefab.
    /// </summary>
    private LootInicialCadaverEntrada[] _lootDesdeNpc;

    private List<GameObject> _contenido;
    private List<int> _cantidades;
    private bool _abierto;

    void Awake()
    {
        _contenido = new List<GameObject>();
        _cantidades = new List<int>();
        for (int i = 0; i < cantidadSlots; i++)
        {
            _contenido.Add(null);
            _cantidades.Add(0);
        }
    }

    void Start()
    {
        InicializarContenido();
    }

    /// <summary>Llamado desde el NPC justo después de instanciar el cadáver (antes de Start).</summary>
    public void EstablecerLootDesdeNPC(LootInicialCadaverEntrada[] entradas)
    {
        _lootDesdeNpc = entradas;
    }

    private void InicializarContenido()
    {
        LootInicialCadaverEntrada[] fuente = null;
        if (_lootDesdeNpc != null && _lootDesdeNpc.Length > 0)
            fuente = _lootDesdeNpc;
        else if (lootInicial != null && lootInicial.Length > 0)
            fuente = lootInicial;

        if (fuente != null && fuente.Length > 0)
        {
            int slot = 0;
            foreach (var e in fuente)
            {
                if (e == null || e.prefab == null) continue;
                int c = Mathf.Max(1, e.cantidad);
                if (slot >= _contenido.Count) break;
                GameObject go = Instantiate(e.prefab);
                go.name = e.prefab.name;
                go.SetActive(false);
                _contenido[slot] = go;
                _cantidades[slot] = c;
                slot++;
            }
            return;
        }

        if (prefabCarneConejo != null && cantidadCarneInicial > 0)
        {
            GameObject carne = Instantiate(prefabCarneConejo);
            carne.name = prefabCarneConejo.name;
            carne.SetActive(false);
            _contenido[0] = carne;
            _cantidades[0] = cantidadCarneInicial;
        }
    }

    public GameObject GetContenido(int slot)
    {
        if (slot < 0 || slot >= _contenido.Count) return null;
        return _contenido[slot];
    }

    public void SetContenido(int slot, GameObject obj)
    {
        if (slot < 0 || slot >= _contenido.Count) return;
        _contenido[slot] = obj;
        if (obj == null && slot >= 0 && slot < _cantidades.Count)
            _cantidades[slot] = 0;
    }

    public void IntercambiarSlots(int slotA, int slotB)
    {
        if (slotA == slotB || slotA < 0 || slotB < 0 || slotA >= _contenido.Count || slotB >= _contenido.Count) return;
        GameObject t = _contenido[slotA];
        _contenido[slotA] = _contenido[slotB];
        _contenido[slotB] = t;
        if (_cantidades != null && slotA < _cantidades.Count && slotB < _cantidades.Count)
        {
            int c = _cantidades[slotA];
            _cantidades[slotA] = _cantidades[slotB];
            _cantidades[slotB] = c;
        }
    }

    public int GetCantidad(int slot)
    {
        if (_cantidades == null || slot < 0 || slot >= _cantidades.Count) return 0;
        return _cantidades[slot];
    }

    public void SetCantidad(int slot, int cantidad)
    {
        if (slot < 0) return;
        while (_cantidades.Count <= slot)
            _cantidades.Add(0);
        _cantidades[slot] = Mathf.Max(0, cantidad);
    }

    public void Abrir()
    {
        if (_abierto) return;

        _abierto = true;

        if (panelInventarioCadaver == null)
        {
            panelInventarioCadaver = GameObject.Find("Inventario Cadáver");
            if (panelInventarioCadaver == null)
            {
                var invCadaver = FindFirstObjectByType<InventarioCadaverGrafico>();
                if (invCadaver != null)
                    panelInventarioCadaver = invCadaver.gameObject;
            }
        }

        if (inventarioJugador != null)
            inventarioJugador.AbrirInventarioConCadaver(this);
        else if (panelInventarioCadaver != null)
            panelInventarioCadaver.SetActive(true);
    }

    public void Cerrar()
    {
        if (!_abierto) return;
        _abierto = false;
    }
}
