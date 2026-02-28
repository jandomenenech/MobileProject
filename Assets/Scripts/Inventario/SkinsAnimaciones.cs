using UnityEngine;

public class SkinsAnimaciones : MonoBehaviour
{
    [Tooltip("Animator del jugador principal al que se sincroniza")]
    public Animator player;
    [SerializeField] private AtaqueyInteraccion ataque;
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

        // Copiar parámetros básicos del Animator principal (dirección y movimiento)
        animator.SetFloat("Horizontal", player.GetFloat("Horizontal"));
        animator.SetFloat("Vertical", player.GetFloat("Vertical"));
        animator.SetBool("IsMoving", player.GetBool("IsMoving"));
        // Los ataques (trigger "Atacar") los dispara MovimientoPorCeldas para que
        // personaje y hacha vayan sincronizados y respeten el cooldown y la cola de ataque.
    }

    

}
