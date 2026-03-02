using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public enum CategoriaObjeto
{
    Ninguno = 0,
    Arma = 1,
    Herramienta = 2,
    Armadura = 3,
    Recurso = 4,
    Acumulable = 5
}

public class ObjetoRecogible : MonoBehaviour
{
    [Header("Clasificacion")]
    public CategoriaObjeto categoria = CategoriaObjeto.Ninguno;

    [Header("Datos del arma (solo si categoria = Arma)")]
    [Tooltip("Asigna un asset DatosArma para que el sistema de equipamiento sepa que prefab visual instanciar.")]
    public DatosArma datosArma;

    public bool esRecogido = false;
    public GameObject player;
    public Texture textura;

    public static bool EsArma(GameObject obj)
    {
        if (obj == null) return false;
        var rec = obj.GetComponent<ObjetoRecogible>();
        return rec != null && rec.categoria == CategoriaObjeto.Arma;
    }

    public static bool EsArmaduraPorCategoria(GameObject obj)
    {
        if (obj == null) return false;
        var rec = obj.GetComponent<ObjetoRecogible>();
        return rec != null && rec.categoria == CategoriaObjeto.Armadura;
    }

    public static DatosArma ObtenerDatosArma(GameObject obj)
    {
        if (obj == null) return null;
        var rec = obj.GetComponent<ObjetoRecogible>();
        return rec != null ? rec.datosArma : null;
    }

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
