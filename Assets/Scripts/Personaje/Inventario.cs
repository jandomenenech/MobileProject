using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventario : MonoBehaviour
{
    public List<GameObject> inventario;
    public GameObject objeto;
    public GameObject inventarioGrafico;
    public InventarioGrafico inv;
    public ObjetoRecogible recoger;

    public bool isActive = false;


    void Start()
    {
        objeto = null;
        inventario = new List<GameObject>();
        inventarioGrafico.SetActive(isActive);
    }

    // Update is called once per frame
    void Update()
    {
        obtenerObjeto();
        //soltarObjeto();
        activarInventario();
    }

    public void obtenerObjeto() {
        if (Input.GetKeyDown(KeyCode.E))// || Input.GetMouseButtonDown(0))
        {
            if(objeto != null)
            {
                RecogerObjeto();
                inv.imagenesInventario();
            }
            
        }
    }

    private void RecogerObjeto()
    {
        inventario.Add(objeto);
        objeto.SetActive(false);
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Recogible"))
        {
            Debug.Log("Objeto en rango: " + collision.gameObject.name);
            objeto = collision.gameObject;
           // recoger = objeto.GetComponent<ObjetoRecogible>();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Recogible"))
        {
            Debug.Log("Objeto no en rango: " + collision.gameObject.name);
            objeto = null;
        }
        
    }

 /* private void soltarObjeto()
{
    if (Input.GetKeyDown(KeyCode.P))
    {
        for (int i = 0; i < inventario.Count; i++)
        {
            if (inventario[i] != null)
            {
                GameObject g = inventario[i];
                g.transform.position = transform.position;
                g.SetActive(true);
                inventario.RemoveAt(i);

                Debug.Log("Soltar");
                inv.imagenesInventario();

                return;
            }
        }
    }
}*/


    private void activarInventario()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if(isActive == false)
            {
                isActive = true;
                inventarioGrafico.SetActive(isActive);
                inv.imagenesInventario();
                
            }
            else if (isActive == true)
            {
                isActive = false;
                inventarioGrafico.SetActive(isActive);
                inv.imagenesInventario();
            }
            
        }
    }

            
}
