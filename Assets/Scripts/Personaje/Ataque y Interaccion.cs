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
        if (timeNextAttack > 0)
        {
            timeNextAttack -= Time.deltaTime;
        }
        if (Input.GetKeyDown(KeyCode.Space) && timeNextAttack <= 0)
        {
            /* animator.SetTrigger("Atacar");*/
            /* Attack();*/
            timeNextAttack = timeIdle;
        }
    }

    private void Attack()
    {
        Collider2D[] objeto = Physics2D.OverlapCircleAll(attackCheck.position, radiusAttack);
        foreach (Collider2D collision in objeto)
        {
            if (collision.CompareTag("Arbusto"))
            {
                collision.transform.GetComponent<Arbusto>().cortarArbusto();
                Debug.Log("Tocado");
            }
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackCheck.position, radiusAttack);
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