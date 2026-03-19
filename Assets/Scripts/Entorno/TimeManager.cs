using UnityEngine;

/// <summary>
/// Control central del tiempo del juego.
/// - Gestiona la hora (0-24) y el paso de los días.
/// - Permite configurar cuántos minutos REALES dura un día completo del juego.
/// - Otros sistemas pueden leer la hora actual o si es de día / de noche.
/// </summary>
public class TimeManager : MonoBehaviour
{
    [Header("Configuración del día")]
    [Tooltip("Minutos REALES que dura un día completo del juego (0-24 horas de juego).")]
    public float minutosPorDia = 20f;

    [Tooltip("Hora inicial del juego (0-24). Ej: 8 = 8:00 de la mañana.")]
    [Range(0f, 24f)]
    public float horaInicial = 8f;

    [Header("Estado actual (solo lectura en el inspector)")]
    [Tooltip("Hora actual del juego en rango 0-24.")]
    [Range(0f, 24f)]
    public float horaActual = 8f;

    [Tooltip("Horas totales acumuladas desde el inicio de la partida (no se reinicia al cambiar de día).")]
    public float tiempoTotalHoras;

    [Tooltip("Número de días completos transcurridos (cada vez que se supera las 24 horas).")]
    public int diasTranscurridos;

    /// <summary>
    /// Velocidad del tiempo en horas de juego por segundo real.
    /// Se calcula en base a minutosPorDia.
    /// </summary>
    public float VelocidadTiempo
    {
        get
        {
            if (minutosPorDia <= 0.01f)
                return 0f;
            return 24f / (minutosPorDia * 60f);
        }
    }

    /// <summary>
    /// Devuelve true si la hora actual está dentro del rango de día (por defecto 6-18).
    /// </summary>
    public bool EsDeDia => horaActual >= 6f && horaActual < 18f;

    /// <summary>
    /// Devuelve true si actualmente es de noche (cualquier hora fuera del rango de día).
    /// </summary>
    public bool EsDeNoche => !EsDeDia;

    void Start()
    {
        horaActual = Mathf.Clamp(horaInicial, 0f, 24f);
    }

    void Update()
    {
        float vel = VelocidadTiempo;
        if (vel <= 0f)
            return;

        float deltaHoras = Time.deltaTime * vel;

        horaActual += deltaHoras;
        tiempoTotalHoras += deltaHoras;

        if (horaActual >= 24f)
        {
            horaActual -= 24f;
            diasTranscurridos++;
        }
    }

    /// <summary>
    /// Devuelve un valor normalizado 0-1 representando el progreso del día actual.
    /// 0 = 0:00, 0.5 = 12:00, 1 = 24:00 (vuelve a 0).
    /// </summary>
    public float GetProgresoDelDia()
    {
        return horaActual / 24f;
    }

    /// <summary>
    /// Devuelve true si la horaActual está en el rango [inicio, fin),
    /// teniendo en cuenta rangos que cruzan la medianoche (por ejemplo 22-4).
    /// </summary>
    public bool EstaEnRango(float inicio, float fin)
    {
        if (inicio < 0f) inicio = 0f;
        if (fin > 24f) fin = 24f;

        if (inicio < fin)
        {
            // Rango normal, no cruza medianoche.
            return horaActual >= inicio && horaActual < fin;
        }
        else if (inicio > fin)
        {
            // Rango que cruza medianoche. Ej: 22-4 => [22,24) U [0,4)
            return horaActual >= inicio || horaActual < fin;
        }
        else
        {
            // inicio == fin -> se considera todo el día.
            return true;
        }
    }
}

