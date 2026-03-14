using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hoguera que puede abrirse con la tecla de acción (F) cuando el personaje está en celda adyacente.
/// Contiene un inventario vacío (tipo cadáver). Si se deposita "Rama" en su inventario, se reproduce la animación de fuego.
/// </summary>
public class HogueraInteractuable : MonoBehaviour
{
    [Tooltip("Panel UI del Inventario Hoguera (se activa al abrir).")]
    public GameObject panelInventarioHoguera;

    [Tooltip("Referencia al Inventario del personaje (para abrir inventario + hoguera).")]
    public Inventario inventarioJugador;

    [Tooltip("Número de slots del inventario de la hoguera.")]
    public int cantidadSlots = 6;

    [Tooltip("Hijo con el Animator de la animación 'Fuego Hoguera Animación'. Se activa y reproduce al depositar Rama.")]
    public Animator animadorFuego;

    [Tooltip("Nombre del estado de animación del fuego (ej. 'Fuego Hoguera Animación'). Si está vacío, se usa el estado por defecto del Animator.")]
    public string nombreEstadoFuego = "Fuego Hoguera Animación";

    private List<GameObject> _contenido;
    private List<int> _cantidades;
    private bool _abierto;
    private bool _fuegoEncendido;

    public bool FuegoEncendido => _fuegoEncendido;

    void Awake()
    {
        _contenido = new List<GameObject>();
        _cantidades = new List<int>();
        for (int i = 0; i < cantidadSlots; i++)
        {
            _contenido.Add(null);
            _cantidades.Add(0);
        }
        if (animadorFuego == null)
            animadorFuego = GetComponentInChildren<Animator>();
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

    /// <summary>
    /// Enciende el fuego de la hoguera (activa el hijo y reproduce la animación).
    /// Se llama cuando se deposita Rama en el inventario de la hoguera o al pulsar E con ramas en el inventario.
    /// </summary>
    public void EncenderFuego()
    {
        if (_fuegoEncendido) return;
        _fuegoEncendido = true;
        if (animadorFuego != null)
        {
            animadorFuego.gameObject.SetActive(true);
            animadorFuego.enabled = true;
            if (!string.IsNullOrEmpty(nombreEstadoFuego))
                animadorFuego.Play(nombreEstadoFuego, 0, 0f);
        }
    }

    /// <summary>
    /// Apaga el fuego de la hoguera (desactiva el hijo del fuego).
    /// </summary>
    public void ApagarFuego()
    {
        if (!_fuegoEncendido) return;
        _fuegoEncendido = false;
        if (animadorFuego != null)
            animadorFuego.gameObject.SetActive(false);
    }

    /// <summary>
    /// Indica si hay al menos una Rama en el inventario de la hoguera.
    /// </summary>
    public bool TieneRamasEnInventario()
    {
        for (int i = 0; i < _contenido.Count; i++)
        {
            GameObject obj = _contenido[i];
            if (obj == null) continue;
            string nombre = (obj.name ?? string.Empty);
            int idx = nombre.IndexOf("(Clone)");
            if (idx >= 0) nombre = nombre.Substring(0, idx);
            if (nombre.Trim().Equals("Rama", System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Enciende o apaga el fuego según el estado actual. Solo enciende si hay al menos una Rama en el inventario de la hoguera.
    /// Llamado al pulsar E mirando a la hoguera.
    /// </summary>
    public void IntentarEncenderOApagar()
    {
        if (_fuegoEncendido)
        {
            ApagarFuego();
            return;
        }
        if (TieneRamasEnInventario())
            EncenderFuego();
    }

    public void Abrir()
    {
        if (_abierto) return;
        _abierto = true;

        if (panelInventarioHoguera == null)
        {
            panelInventarioHoguera = GameObject.Find("Inventario Hoguera");
            if (panelInventarioHoguera == null)
            {
                var invHogueras = Resources.FindObjectsOfTypeAll<InventarioHogueraGrafico>();
                if (invHogueras != null && invHogueras.Length > 0 && invHogueras[0] != null)
                    panelInventarioHoguera = invHogueras[0].gameObject;
            }
        }

        if (inventarioJugador != null)
            inventarioJugador.AbrirInventarioConHoguera(this);
        else if (panelInventarioHoguera != null)
            panelInventarioHoguera.SetActive(true);
    }

    public void Cerrar()
    {
        if (!_abierto) return;
        _abierto = false;
    }
}
