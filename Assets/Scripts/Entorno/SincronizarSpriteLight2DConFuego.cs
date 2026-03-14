using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Sincroniza el sprite de una Sprite Light 2D con el sprite actual del SpriteRenderer del fuego,
/// para que la luz siga la animación del fuego. Colocar en el mismo GameObject que tenga
/// SpriteRenderer (fuego) y Light2D (tipo Sprite), o asignar las referencias en el Inspector.
/// </summary>
public class SincronizarSpriteLight2DConFuego : MonoBehaviour
{
    [Tooltip("SpriteRenderer del fuego (animado). Si no se asigna, se usa el del mismo GameObject.")]
    public SpriteRenderer spriteRendererFuego;

    [Tooltip("Light 2D tipo Sprite. Si no se asigna, se usa el del mismo GameObject.")]
    public Light2D light2D;

    private FieldInfo _fieldSprite;
    private bool _usarReflexion;

    void Awake()
    {
        if (spriteRendererFuego == null)
            spriteRendererFuego = GetComponent<SpriteRenderer>();
        if (light2D == null)
            light2D = GetComponent<Light2D>();

        if (spriteRendererFuego == null)
        {
            Debug.LogWarning("SincronizarSpriteLight2DConFuego: no hay SpriteRenderer asignado ni en este GameObject.", this);
            enabled = false;
            return;
        }
        if (light2D == null)
        {
            Debug.LogWarning("SincronizarSpriteLight2DConFuego: no hay Light2D asignado ni en este GameObject.", this);
            enabled = false;
            return;
        }

        if (light2D.lightType != Light2D.LightType.Sprite)
        {
            Debug.LogWarning("SincronizarSpriteLight2DConFuego: la Light2D no es de tipo Sprite. Cámbiala a Sprite en el Inspector.", this);
            enabled = false;
            return;
        }

        var tipo = typeof(Light2D);
        _fieldSprite = tipo.GetField("m_LightCookieSprite", BindingFlags.Instance | BindingFlags.NonPublic);
        _usarReflexion = _fieldSprite != null;
    }

    void LateUpdate()
    {
        Sprite s = spriteRendererFuego.sprite;
        if (s == null) return;

        if (_usarReflexion && _fieldSprite != null)
            _fieldSprite.SetValue(light2D, s);
        else
        {
            try
            {
                var prop = typeof(Light2D).GetProperty("lightCookieSprite", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.CanWrite)
                    prop.SetValue(light2D, s);
            }
            catch { }
        }
    }
}
