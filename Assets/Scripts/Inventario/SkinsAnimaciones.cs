using UnityEngine;

public class SkinsAnimaciones : MonoBehaviour
{
    [Tooltip("Animator del jugador principal al que se sincroniza")]
    public Animator player;
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();

        // Si no se asigna manualmente, intenta encontrarlo en el padre
        if (player == null)
        {
            Transform parent = transform.parent;
            if (parent != null)
            {
                player = parent.GetComponentInParent<Animator>();
            }

            if (player == null)
            {
                Debug.LogError("⚠ No se asignó el Animator base para sincronizar.");
            }
        }
    }

    void Update()
    {
        if (player == null || animator == null) return;

        // Copiar parámetros
        animator.SetFloat("Horizontal", player.GetFloat("Horizontal"));
        animator.SetFloat("Vertical", player.GetFloat("Vertical"));
        animator.SetBool("IsMoving", player.GetBool("IsMoving"));
    }

    /// <summary>
    /// Dispara el trigger de ataque en este Animator, sincronizado con el del jugador.
    /// Llamado desde AtaqueyInteraccion cuando el jugador ataca.
    /// </summary>
    public void TriggerAtacar()
    {
        if (animator == null) return;
        animator.SetTrigger("Atacar");
    }
}
