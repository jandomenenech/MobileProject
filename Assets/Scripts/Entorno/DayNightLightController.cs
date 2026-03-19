using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controla el color y la intensidad de un Global Light 2D
/// en función de la hora del juego que expone el TimeManager.
/// 
/// - Usa cuatro tramos:
///   * Noche profunda  : 22:00 - 6:00
///   * Amanecer        : 6:00  - 10:00
///   * Pleno día       : 10:00 - 16:00
///   * Atardecer       : 16:00 - 22:00
/// - Entre tramos se hace una interpolación suave (Lerp) entre color/intensidad.
/// </summary>
public class DayNightLightController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Referencia al TimeManager de la escena.")]
    public TimeManager timeManager;

    [Tooltip("Light2D global que actúa como luz ambiental (Global Light 2D).")]
    public Light2D globalLight;

    [Header("Colores")]
    [Tooltip("Color de la luz en pleno día (10:00 - 16:00).")]
    public Color colorDia = new Color(1f, 0.96f, 0.9f);     // blanco cálido

    [Tooltip("Color de la luz en noche profunda (22:00 - 6:00).")]
    public Color colorNoche = new Color(0.1f, 0.15f, 0.3f); // azul oscuro

    [Header("Intensidades")]
    [Tooltip("Intensidad en pleno día.")]
    public float intensidadDia = 1.2f;

    [Tooltip("Intensidad en noche profunda.")]
    public float intensidadNoche = 0.2f;

    [Header("Horas clave (0-24)")]
    [Tooltip("Inicio del amanecer (se pasa de noche a día).")]
    [Range(0f, 24f)]
    public float horaAmanecerInicio = 6f;

    [Tooltip("Fin del amanecer (a partir de aquí es pleno día).")]
    [Range(0f, 24f)]
    public float horaAmanecerFin = 10f;

    [Tooltip("Inicio del atardecer (se pasa de día a noche).")]
    [Range(0f, 24f)]
    public float horaAtardecerInicio = 16f;

    [Tooltip("Fin del atardecer (a partir de aquí es noche profunda).")]
    [Range(0f, 24f)]
    public float horaAtardecerFin = 22f;

    void Reset()
    {
        // Intentar autocompletar referencias cuando se añade el componente.
        if (timeManager == null)
            timeManager = FindObjectOfType<TimeManager>();

        if (globalLight == null)
            globalLight = GetComponent<Light2D>();
    }

    void Awake()
    {
        if (timeManager == null)
            timeManager = FindObjectOfType<TimeManager>();

        if (globalLight == null)
            globalLight = GetComponent<Light2D>();
    }

    void Update()
    {
        if (timeManager == null || globalLight == null)
            return;

        ActualizarLuz(timeManager.horaActual);
    }

    /// <summary>
    /// Calcula el color e intensidad de la luz para una hora concreta.
    /// </summary>
    /// <param name="hora">Hora del juego en rango 0-24.</param>
    void ActualizarLuz(float hora)
    {
        // Ordenar mentalmente los tramos:
        // Noche profunda : [horaAtardecerFin, 24) U [0, horaAmanecerInicio)
        // Amanecer       : [horaAmanecerInicio, horaAmanecerFin)
        // Pleno día      : [horaAmanecerFin, horaAtardecerInicio)
        // Atardecer      : [horaAtardecerInicio, horaAtardecerFin)

        // Pleno día -> 0
        if (hora >= horaAmanecerFin && hora < horaAtardecerInicio)
        {
            globalLight.color = colorDia;
            globalLight.intensity = intensidadDia;
            return;
        }

        // Noche profunda -> 1
        if (hora >= horaAtardecerFin || hora < horaAmanecerInicio)
        {
            globalLight.color = colorNoche;
            globalLight.intensity = intensidadNoche;
            return;
        }

        // Amanecer: noche (1) -> día (0)
        if (hora >= horaAmanecerInicio && hora < horaAmanecerFin)
        {
            float t = Mathf.InverseLerp(horaAmanecerInicio, horaAmanecerFin, hora);
            globalLight.color = Color.Lerp(colorNoche, colorDia, t);
            globalLight.intensity = Mathf.Lerp(intensidadNoche, intensidadDia, t);
            return;
        }

        // Atardecer: día (0) -> noche (1)
        if (hora >= horaAtardecerInicio && hora < horaAtardecerFin)
        {
            float t = Mathf.InverseLerp(horaAtardecerInicio, horaAtardecerFin, hora);
            globalLight.color = Color.Lerp(colorDia, colorNoche, t);
            globalLight.intensity = Mathf.Lerp(intensidadDia, intensidadNoche, t);
            return;
        }

        // Caso de seguridad (no debería llegar aquí).
        globalLight.color = colorDia;
        globalLight.intensity = intensidadDia;
    }
}

