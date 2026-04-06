using System.Collections;
using UnityEngine;

/// <summary>
/// Flash de tinte rojo (o el color configurado) en todos los SpriteRenderer del personaje, igual que el impacto en NPCs.
/// El contraataque u otros sistemas pueden llamar <see cref="MostrarFlashImpacto"/>.
/// </summary>
public class ReceptorImpactoVisual : MonoBehaviour
{
    [Tooltip("Color del flash al recibir un impacto (p. ej. rojo semi-transparente).")]
    [SerializeField] private Color colorImpacto = new Color(1f, 0f, 0f, 0.6f);
    [Tooltip("Duración del flash en segundos.")]
    [SerializeField] private float duracionFlash = 0.7f;

    private Coroutine _flashCoroutine;

    /// <summary>Llamado desde animaciones de enemigos o detección de golpes (solo efecto visual).</summary>
    public void MostrarFlashImpacto()
    {
        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashImpacto());
    }

    private IEnumerator FlashImpacto()
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        var coloresOriginales = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            coloresOriginales[i] = renderers[i].color;

        Color tinte = colorImpacto;
        foreach (var sr in renderers)
            sr.color = tinte;

        float dur = Mathf.Max(0.01f, duracionFlash);
        float timer = 0f;
        while (timer < dur)
        {
            timer += Time.deltaTime;
            float t = timer / dur;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null)
                    renderers[i].color = Color.Lerp(tinte, coloresOriginales[i], t);
            yield return null;
        }

        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null)
                renderers[i].color = coloresOriginales[i];

        _flashCoroutine = null;
    }
}
