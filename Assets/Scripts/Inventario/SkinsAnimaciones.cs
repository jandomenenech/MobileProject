using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinsAnimaciones : MonoBehaviour
{
    public Animator player;

    private Animator animator;
    // Start is called before the first frame update
    void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Transform parent = transform.parent;
            if(parent != null)
            {
                player = parent.GetComponent<Animator>();
            }
            if (player == null)
            {
                Debug.LogError("⚠ No se asignó el Animator base para sincronizar.");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(player == null || animator == null) return;

        animator.SetFloat("Horizontal", player.GetFloat("Horizontal"));
        animator.SetFloat("Vertical", player.GetFloat("Vertical"));
        animator.SetBool("IsMoving", player.GetBool("IsMoving"));
    }
}
