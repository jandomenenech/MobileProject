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

    [Header("Sangre al golpear criaturas")]
    [Tooltip("Prefabs de charcos de sangre que se instanciarán en la celda del NPC golpeado (puedes asignar uno o varios).")]
    [SerializeField] private GameObject[] sangreCharcoPrefabs;
    [Tooltip("Offset opcional desde el centro del collider del NPC.")]
    [SerializeField] private Vector2 sangreOffset = Vector2.zero;

    private HashSet<Vector2Int> _celdasConSangre = new HashSet<Vector2Int>();

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
            if (collision == null) continue;

            // Arbustos
            if (collision.CompareTag("Arbusto"))
            {
                var a = collision.GetComponent<Arbusto>();
                if (a != null)
                {
                    a.cortarArbusto();
                    Debug.Log("Arbusto golpeado");
                }
            }

            // Criaturas/NPCs (conejo u otros que usen NPCMovimientoAleatorio)
            var npc = collision.GetComponent<NPCMovimientoAleatorio>();
            if (npc != null)
            {
                npc.RecibirDanio(damage);
                CrearCharcoSangre(collision);
            }
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

    private Vector2Int PosACelda(Vector2 pos)
    {
        return new Vector2Int(Mathf.RoundToInt(pos.x / cellSize), Mathf.RoundToInt(pos.y / cellSize));
    }

    private void CrearCharcoSangre(Collider2D objetivo)
    {
        if (sangreCharcoPrefabs == null || sangreCharcoPrefabs.Length == 0) return;
        if (objetivo == null) return;

        Vector2 pos = (Vector2)objetivo.bounds.center + sangreOffset;
        Vector2Int celda = PosACelda(pos);

        if (_celdasConSangre.Contains(celda))
            return;

        int index = Random.Range(0, sangreCharcoPrefabs.Length);
        GameObject prefab = sangreCharcoPrefabs[index];
        if (prefab == null) return;

        Instantiate(prefab, pos, Quaternion.identity);
        _celdasConSangre.Add(celda);
    }
}