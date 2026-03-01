using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arbusto : MonoBehaviour
{
    private SpriteRenderer arbusto;
    public Sprite arbustoCortado;
    [SerializeField] private GameObject efectoCorte;
    [SerializeField] private GameObject efectoCorteRamas;
    private bool cortado;
    private bool destruido;

    void Start()
    {
        arbusto = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!destruido || efectoCorteRamas == null || !efectoCorteRamas.activeInHierarchy)
            return;

        var anim = efectoCorteRamas.GetComponent<Animator>();
        if (anim == null || !anim.enabled) return;

        var state = anim.GetCurrentAnimatorStateInfo(0);
        if (state.normalizedTime >= 1f && !anim.IsInTransition(0))
        {
            gameObject.SetActive(false);
        }
    }

    public void cortarArbusto()
    {
        if (!cortado)
        {
            cortado = true;
            foreach (Transform hijo in transform)
            {
                hijo.gameObject.SetActive(false);
            }
            arbusto.sprite = arbustoCortado;

            if (efectoCorte != null)
            {
                efectoCorte.SetActive(true);
            }
        }
        else if (!destruido)
        {
            destruido = true;
            arbusto.enabled = false;

            var col = GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false;

            if (efectoCorteRamas != null)
            {
                efectoCorteRamas.SetActive(true);
            }
        }
    }
}
