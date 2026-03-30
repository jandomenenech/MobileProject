using UnityEngine;

/// <summary>
/// Los clips del Animator en 'body' disparan eventos (Attack, OnAttackStart, OnAttackEnd).
/// Unity solo busca el receptor en el mismo GameObject que el Animator; la lógica vive en
/// <see cref="AtaqueyInteraccion"/> en el personaje raíz. Este script reenvía y elimina los avisos de consola.
/// </summary>
[DisallowMultipleComponent]
public class EventosAnimacionAtaqueBody : MonoBehaviour
{
    private AtaqueyInteraccion _ataque;

    void Awake()
    {
        _ataque = GetComponentInParent<AtaqueyInteraccion>();
    }

    public void OnAttackStart()
    {
        if (_ataque != null)
            _ataque.OnAttackStart();
    }

    public void OnAttackEnd()
    {
        if (_ataque != null)
            _ataque.OnAttackEnd();
    }

    /// <summary>Llamado desde eventos de animación con nombre "Attack".</summary>
    public void Attack()
    {
        if (_ataque != null)
            _ataque.detectarAtaque();
    }
}
