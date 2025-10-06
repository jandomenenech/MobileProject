using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class ObjetoRecogible : MonoBehaviour
{
    public bool esRecogido = false;
    public GameObject player;
    public Texture textura;

    void Start()
    {
        player = null;
        esRecogido = false;
    }

    // Update is called once per frame
    void Update()
    {
        enPosesion();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            player = collision.gameObject;
            recoger();
            soltar();

        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            //player = null;
        }
    }
    private void enPosesion()
    {
        if (esRecogido)
        {
            gameObject.transform.position = player.transform.position;
            Debug.Log("Sigo al personaje");
        }
    }

    private void soltarObjeto()
    {
        esRecogido = false;
        player = null;
    }
    private void recoger()
    {
        if (Input.GetKeyDown(KeyCode.E) && player != null)
        {
            esRecogido = true;
            Debug.Log("Recogido");
        }
    }

    private void soltar()
    {
        if (Input.GetKeyDown(KeyCode.P) && esRecogido)
        {
            gameObject.transform.position = player.transform.position;
            gameObject.SetActive(true);
            soltarObjeto();
            Debug.Log("Soltar");
        }
    }
}
