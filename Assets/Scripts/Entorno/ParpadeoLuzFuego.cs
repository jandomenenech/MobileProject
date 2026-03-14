using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Hace parpadear la intensidad de una Light 2D (p. ej. "Iluminación fuego hoguera")
/// variando un poco la intensidad de forma suave para simular el fuego.
/// </summary>
public class ParpadeoLuzFuego : MonoBehaviour
{
    [Tooltip("Light 2D a la que aplicar el parpadeo. Si no se asigna, se usa la del mismo GameObject.")]
    public Light2D light2D;

    [Tooltip("Intensidad base de la luz.")]
    [Range(0.1f, 5f)]
    public float intensidadBase = 1f;

    [Tooltip("Cuánto varía la intensidad (arriba/abajo respecto a la base).")]
    [Range(0f, 2f)]
    public float variacion = 0.3f;

    [Tooltip("Velocidad del parpadeo (más alto = parpadeo más rápido).")]
    [Range(0.5f, 20f)]
    public float velocidad = 6f;

    [Tooltip("Offset en el ruido para que varias luces no parpadeen igual.")]
    public float offsetSemilla = 0f;

    void Awake()
    {
        if (light2D == null)
            light2D = GetComponent<Light2D>();

        if (light2D == null)
        {
            Debug.LogWarning("ParpadeoLuzFuego: no hay Light2D asignada ni en este GameObject.", this);
            enabled = false;
            return;
        }
    }

    void Update()
    {
        // Perlin noise da una variación suave y natural
        float t = Time.time * velocidad + offsetSemilla;
        float ruido = Mathf.PerlinNoise(t, 0f);
        float intensidad = intensidadBase + (ruido - 0.5f) * 2f * variacion;
        light2D.intensity = Mathf.Max(0.01f, intensidad);
    }

    void OnDisable()
    {
        if (light2D != null)
            light2D.intensity = intensidadBase;
    }
}
