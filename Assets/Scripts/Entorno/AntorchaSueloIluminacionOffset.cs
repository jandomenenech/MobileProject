using UnityEngine;

/// <summary>
/// Reubica la "Iluminación suelo antorcha" según la dirección actual del jugador (Animator: Horizontal/Vertical).
/// El objeto debe estar bajo Antorcha para que use localPosition.
/// </summary>
public class AntorchaSueloIluminacionOffset : MonoBehaviour
{
    [SerializeField] private Animator sourceAnimator;
    [SerializeField] private bool useLocalPosition = true;

    [Header("Offsets AP (abajo)")]
    [SerializeField] private Vector2 apOffset = new Vector2(-0.23f, 0.02f); // Estático AP o Caminando AP

    [Header("Offsets PA (arriba)")]
    [SerializeField] private Vector2 paOffset = new Vector2(0.23f, 0.5f); // Estático PA o Caminando PA

    [Header("Offsets Perfil")]
    [SerializeField] private Vector2 perfilROffset = new Vector2(0.48f, 0.02f); // Perfil R
    [SerializeField] private Vector2 perfilLOffset = new Vector2(-0.48f, 0.5f); // Perfil L

    private void LateUpdate()
    {
        if (sourceAnimator == null)
        {
            // Auto-deteccion: el objeto está bajo el arma, y el arma está bajo el jugador.
            // Priorizamos el Animator del jugador usando MovimientoPorCeldas como ancla.
            var move = GetComponentInParent<MovimientoPorCeldas>(true);
            if (move != null)
                sourceAnimator = move.GetComponent<Animator>();

            // Fallback: cualquier Animator en padres.
            if (sourceAnimator == null)
                sourceAnimator = GetComponentInParent<Animator>(true);

            // Si sigue null, no hacemos nada.
            if (sourceAnimator == null) return;
        }

        float h = sourceAnimator.GetFloat("Horizontal");
        float v = sourceAnimator.GetFloat("Vertical");

        float x;
        float y;

        // AP/PA si predomina Vertical; Perfil si predomina Horizontal.
        if (Mathf.Abs(h) > Mathf.Abs(v))
        {
            // Perfil
            if (h > 0f)
            {
                x = perfilROffset.x;
                y = perfilROffset.y;
            }
            else
            {
                x = perfilLOffset.x;
                y = perfilLOffset.y;
            }
        }
        else
        {
            // AP/PA
            if (v > 0f)
            {
                x = paOffset.x;
                y = paOffset.y;
            }
            else
            {
                // v==0 cae a AP (por defecto hacia abajo)
                x = apOffset.x;
                y = apOffset.y;
            }
        }

        if (useLocalPosition)
        {
            var p = transform.localPosition;
            transform.localPosition = new Vector3(x, y, p.z);
        }
        else
        {
            var p = transform.position;
            transform.position = new Vector3(x, y, p.z);
        }
    }
}

