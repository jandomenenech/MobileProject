using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arbusto : MonoBehaviour
{
    private SpriteRenderer arbusto;
    public Sprite arbustoCortado;

    // Start is called before the first frame update
    void Start()
    {
        arbusto = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        estadoArbusto();
    }

    public void cortarArbusto()
    {
        foreach (Transform hijo in transform)
        {
            hijo.gameObject.SetActive(false);
        }
        arbusto.sprite = arbustoCortado;
    }


    private void estadoArbusto()
    {
        arbusto.sprite = arbusto.sprite;
    }
}
