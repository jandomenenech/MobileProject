using System.Collections;
using System.Collections.Generic;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UI;

public class InventarioGrafico : MonoBehaviour
{
    public List<RawImage> celdas;
    public Inventario inv;
    public GameObject o;

    void Start()
    {
        o = GetComponent<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void imagenesInventario()
    {
        for (int i = 0; i < celdas.Count; i++)
        {
            if (i < inv.inventario.Count && inv.inventario[i] != null)
            {
                GameObject o = inv.inventario[i];
                ObjetoRecogible objeto = o.GetComponent<ObjetoRecogible>();
                celdas[i].texture = objeto.textura;
                celdas[i].color = new Color(1f, 1f, 1f, 1f);
            }
            else
            {
                // LIMPIA la celda si no hay objeto
                celdas[i].texture = null;
                celdas[i].color = new Color(1f, 1f, 1f, 0f);
            }
        }
    }

    private void prueba()
    {
        foreach (RawImage t in celdas)
        {
            for(int i = 0; i < inv.inventario.Count; i++)
            {
                o = inv.inventario[i];
                if (o != null)
                {
                    ObjetoRecogible objeto = o.GetComponent<ObjetoRecogible>();
                    celdas[i].texture = objeto.textura;
                    celdas[i].color = new Color(1f, 1f, 1f, 1f);
                }
                else
                {
                    celdas[i].texture = null;
                    celdas[i].color = new Color(1f, 1f, 1f, 0f);
                }
                o = null;
            }
            

        }
    }


}
