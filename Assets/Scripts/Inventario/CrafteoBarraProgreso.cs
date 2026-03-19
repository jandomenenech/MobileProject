using UnityEngine;

/// <summary>
/// Se coloca en el GameObject "Menú Crafteo Slot Barra proceso".
/// La barra de progreso se controla con el ancho del RectTransform: 0% = ancho 0, 100% = ancho completo.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CrafteoBarraProgreso : MonoBehaviour
{
    [Tooltip("Ancho del RectTransform al 100% de progreso.")]
    public float anchoCompleto = 36.99228f;

    RectTransform _rect;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        // Ocultar por defecto: solo se muestra al pulsar Crear y durante el tiempo de crafteo.
        Ocultar();
    }

    /// <summary>
    /// Muestra la barra y pone el progreso a 0 (ancho 0).
    /// </summary>
    public void Mostrar()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();
        gameObject.SetActive(true);
        if (_rect != null)
            _rect.sizeDelta = new Vector2(0f, _rect.sizeDelta.y);
    }

    /// <summary>
    /// Oculta la barra.
    /// </summary>
    public void Ocultar()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Establece el progreso de la barra (0 = ancho 0, 1 = ancho completo).
    /// </summary>
    public void SetProgreso(float t)
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();
        if (_rect != null)
        {
            float ancho = Mathf.Clamp01(t) * anchoCompleto;
            _rect.sizeDelta = new Vector2(ancho, _rect.sizeDelta.y);
        }
    }
}
