using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Baúl que puede abrirse con la tecla de acción (F) cuando el personaje
/// está en la celda inferior mirándolo. Contiene un inventario de slots que se muestra al abrir.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BaulInteractuable : MonoBehaviour
{
    [Tooltip("Sprite que se mostrará cuando el baúl esté abierto (por ejemplo 'Baúl Abierto').")]
    public Sprite spriteAbierto;

    [Tooltip("Panel UI del Inventario Baúl (se activa al abrir).")]
    public GameObject panelInventarioBaul;

    [Tooltip("Referencia al Inventario del personaje (para abrir inventario + baúl).")]
    public Inventario inventarioJugador;

    [Tooltip("Número de slots del baúl (debe coincidir con las celdas del panel Inventario Baúl).")]
    public int cantidadSlots = 15;

    private SpriteRenderer _sr;
    private bool _abierto;
    private List<GameObject> _contenido;
    private Sprite _spriteCerrado;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null)
            _spriteCerrado = _sr.sprite;
        _contenido = new List<GameObject>();
        for (int i = 0; i < cantidadSlots; i++)
            _contenido.Add(null);
    }

    /// <summary>Obtiene el objeto en el slot (puede ser null).</summary>
    public GameObject GetContenido(int slot)
    {
        if (slot < 0 || slot >= _contenido.Count) return null;
        return _contenido[slot];
    }

    /// <summary>Coloca o quita el objeto en el slot (obj puede ser null).</summary>
    public void SetContenido(int slot, GameObject obj)
    {
        if (slot < 0 || slot >= _contenido.Count) return;
        _contenido[slot] = obj;
    }

    /// <summary>Intercambia el contenido de dos slots del baúl.</summary>
    public void IntercambiarSlots(int slotA, int slotB)
    {
        if (slotA == slotB || slotA < 0 || slotB < 0 || slotA >= _contenido.Count || slotB >= _contenido.Count) return;
        GameObject t = _contenido[slotA];
        _contenido[slotA] = _contenido[slotB];
        _contenido[slotB] = t;
    }

    public void Abrir()
    {
        if (_abierto) return;
        if (_sr == null)
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) return;
        }

        if (spriteAbierto == null)
        {
            Debug.LogWarning("BaulInteractuable: spriteAbierto no asignado en '" + name + "'. Asigna el sprite 'Baúl Abierto' en el Inspector.");
            return;
        }

        _sr.sprite = spriteAbierto;
        _abierto = true;

        if (panelInventarioBaul == null)
        {
            panelInventarioBaul = GameObject.Find("Inventario Baúl");
            if (panelInventarioBaul == null)
            {
                var invBaul = FindFirstObjectByType<InventarioBaulGrafico>();
                if (invBaul != null)
                    panelInventarioBaul = invBaul.gameObject;
            }
        }

        if (inventarioJugador != null)
            inventarioJugador.AbrirInventarioConBaul(this);
        else if (panelInventarioBaul != null)
            panelInventarioBaul.SetActive(true);
    }

    /// <summary>
    /// Cierra el baúl: restaura el sprite "Baúl cerrado". Llamado al cerrar con Tab o F.
    /// </summary>
    public void Cerrar()
    {
        if (!_abierto) return;
        _abierto = false;
        if (_sr != null && _spriteCerrado != null)
            _sr.sprite = _spriteCerrado;
    }
}

