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
        // Solo si ambos animators están asignados
        if (player == null || animator == null) return;

        // Copia los parámetros del Animator del personaje al del objeto
        animator.SetFloat("Horizontal", player.GetFloat("Horizontal"));
        animator.SetFloat("Vertical", player.GetFloat("Vertical"));
        animator.SetBool("IsMoving", player.GetBool("IsMoving"));
        if (ataque.timeNextAttack > 0)
        {
            ataque.timeNextAttack -= Time.deltaTime;
        }
        if (Input.GetKeyDown(KeyCode.Space) && ataque.timeNextAttack <= 0)
        {
            animator.SetTrigger("Atacar");

            ataque.timeNextAttack = ataque.timeIdle;
        }
    }
}
