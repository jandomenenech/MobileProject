using UnityEngine;

// Justo después de MovimientoPorCeldas (-100): mismos parámetros antes del tick interno de Animator.
[DefaultExecutionOrder(-99)]
public class SkinsAnimaciones : MonoBehaviour
{
    [Tooltip("Animator del jugador principal al que se sincroniza")]
    public Animator player;
    [SerializeField] private AtaqueyInteraccion ataque;
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();

        // Si no se asigna manualmente, intenta encontrarlo en el padre (cuerpo), nunca el propio Animator de la armadura.
        if (player == null)
        {
            Transform parent = transform.parent;
            if (parent != null)
                player = parent.GetComponentInParent<Animator>();

            if (player == animator)
                player = parent != null ? parent.GetComponent<Animator>() : null;

            if (player == null)
                Debug.LogError($"SkinsAnimaciones en '{name}': no hay Animator del cuerpo para sincronizar. Asigna 'Player' al Animator del objeto body.");
        }

        if (player != null && player.runtimeAnimatorController == null)
            Debug.LogWarning($"SkinsAnimaciones en '{name}': el Animator del cuerpo ('{player.name}') no tiene Runtime Animator Controller asignado. La armadura no podrá seguir estados por Animator; asigna el mismo controller base (p. ej. BaseAnimator) en el body.");
    }

    void Update()
    {
        if (animator == null) return;

        // Fuente principal: MovimientoPorCeldas. Evita depender de una referencia "player" mal asignada
        // en un rig concreto de armadura (p. ej. Armadura 1) y mantiene sincronía estable.
        MovimientoPorCeldas move = GetComponentInParent<MovimientoPorCeldas>();
        if (move != null)
        {
            Vector2 d = move.GetLastInputDirection();
            animator.SetFloat("Horizontal", d.x);
            animator.SetFloat("Vertical", d.y);
            animator.SetBool("IsMoving", move.IsMoving);
        }
        else if (player != null)
        {
            animator.SetFloat("Horizontal", player.GetFloat("Horizontal"));
            animator.SetFloat("Vertical", player.GetFloat("Vertical"));
            animator.SetBool("IsMoving", player.GetBool("IsMoving"));
        }

        // Los ataques (trigger "Atacar") los dispara MovimientoPorCeldas para que
        // personaje y hacha vayan sincronizados y respeten el cooldown y la cola de ataque.
    }

    

}
