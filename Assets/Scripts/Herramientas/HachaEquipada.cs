using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(SkinsAnimaciones))]
public class HachaEquipada : MonoBehaviour
{
    private SkinsAnimaciones skins;

    void Start()
    {
        skins = GetComponent<SkinsAnimaciones>();

        // Asigna automáticamente el Animator del padre si no se ha hecho
        if (skins.player == null)
        {
            Animator parentAnimator = GetComponentInParent<Animator>();
            if (parentAnimator != null)
            {
                skins.player = parentAnimator;
            }
        }

        // Reiniciar posición y rotación local
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}

