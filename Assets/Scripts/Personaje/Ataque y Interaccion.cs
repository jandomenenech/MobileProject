using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtaqueyInteraccion : MonoBehaviour
{
    [SerializeField] private Arbusto arbusto;
    [SerializeField] public int damage;
    [SerializeField] private Transform attackCheck;
    [SerializeField] private float radiusAttack;
    private Animator animator;
    public LayerMask layerEnemy;
    [SerializeField] public float timeNextAttack;
    [SerializeField] public float timeIdle;
    [SerializeField] private MovimientoPorCeldas move;

    private void Start()
    {
        animator = GetComponent<Animator>();
        move = GetComponent<MovimientoPorCeldas>();
    }
    private void Update()
    {
        // Solo gestionamos el cooldown; la entrada de ataque (Espacio) la controla MovimientoPorCeldas.
        if (timeNextAttack > 0)
        {
            timeNextAttack -= Time.deltaTime;
        }
    }

    [SerializeField] private float cellSize = 1f;

    private void Attack()
    {
        if (move == null) return;
        Vector2 facing = move.GetLastInputDirection();
        if (facing.sqrMagnitude < 0.01f) return;
        facing.Normalize();

        Vector2 celdaJugador = move.GetPosicionCeldaActual();
        Vector2 celdaAtacada = celdaJugador + facing * cellSize;
        Vector2 boxSize = new Vector2(cellSize * 0.9f, cellSize * 0.9f);

        Collider2D[] objeto = Physics2D.OverlapBoxAll(celdaAtacada, boxSize, 0f);
        foreach (Collider2D collision in objeto)
        {
            if (!collision.CompareTag("Arbusto")) continue;
            collision.transform.GetComponent<Arbusto>().cortarArbusto();
            Debug.Log("Tocado");
        }
    }
    private void OnDrawGizmos()
    {
        if (attackCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackCheck.position, radiusAttack);
        }
    }

    public void gizosOrient()
    {
        attackCheck.transform.position = new Vector2(attackCheck.position.x, attackCheck.position.y);

    }

    public void OnAttackStart()
    {
        move.setVelocidad(0);
    }

    public void OnAttackEnd()
    {
        move.setVelocidad(5);
    }


    public void detectarAtaque()
    {
        Attack();
    }
}