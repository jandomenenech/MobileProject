using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra mensajes breves cuando se añaden objetos al inventario.
/// Usa un Text en UI y se oculta automáticamente tras unos segundos.
/// </summary>
public class MensajeInventario : MonoBehaviour
{
    public static MensajeInventario Instancia { get; private set; }

    [Header("Referencia UI")]
    [SerializeField] private Text texto;

    [Header("Duración")]
    [SerializeField] private float duracionPorDefecto = 2f;

    private float tiempoRestante;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;

        if (texto == null)
            texto = GetComponentInChildren<Text>();

        Ocultar();
    }

    void Update()
    {
        if (tiempoRestante <= 0f) return;

        tiempoRestante -= Time.deltaTime;
        if (tiempoRestante <= 0f)
            Ocultar();
    }

    public static void MostrarMensaje(string mensaje, float duracion = -1f)
    {
        if (Instancia == null || Instancia.texto == null)
            return;

        Instancia.MostrarInterno(mensaje, duracion);
    }

    void MostrarInterno(string mensaje, float duracion)
    {
        if (string.IsNullOrEmpty(mensaje))
            texto.text = string.Empty;
        else
            texto.text = mensaje.ToUpperInvariant();
        texto.enabled = true;
        texto.gameObject.SetActive(true);
        tiempoRestante = duracion > 0f ? duracion : duracionPorDefecto;
    }

    void Ocultar()
    {
        if (texto != null)
        {
            texto.text = string.Empty;
            texto.enabled = false;
            texto.gameObject.SetActive(false);
        }
        tiempoRestante = 0f;
    }
}

