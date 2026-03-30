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
    private const string TagConstruccionColocable = "ConstruccionColocable";

    [Header("Clasificacion")]
    public CategoriaObjeto categoria = CategoriaObjeto.Ninguno;

    [Header("Datos equipables (Arma/Herramienta)")]
    [Tooltip("Asigna un asset DatosArma para que el sistema de equipamiento sepa qué prefab visual instanciar.")]
    public DatosArma datosArma;

    public bool esRecogido = false;
    public GameObject player;
    public Texture textura;

    [Header("Comportamiento en suelo (Construccion)")]
    [Tooltip("Si esta activado, cuando el objeto se suelta desde el inventario al mundo se marca con el tag 'ConstruccionColocable' para deshabilitar el pickup.")]
    public bool usarTagConstruccionColocableAlSoltar = false;

    public static bool EsArma(GameObject obj)
    {
        if (obj == null) return false;
        var rec = obj.GetComponent<ObjetoRecogible>();
        return rec != null && rec.categoria == CategoriaObjeto.Arma;
    }

    public static bool EsEquipable(GameObject obj)
    {
        if (obj == null) return false;
        var rec = obj.GetComponent<ObjetoRecogible>();
        return rec != null && (rec.categoria == CategoriaObjeto.Arma || rec.categoria == CategoriaObjeto.Herramienta);
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

    void OnEnable()
    {
        if (!usarTagConstruccionColocableAlSoltar) return;

        // Retagueamos al activarse para cubrir casos donde el objeto ya existe en la escena
        // (o cuando pasa al mundo tras soltarse desde el inventario).
        try
        {
            gameObject.tag = TagConstruccionColocable;
        }
        catch (UnityEngine.UnityException)
        {
            Debug.LogWarning($"ObjetoRecogible: el tag '{TagConstruccionColocable}' no existe. Crea el tag en Unity (Tags & Layers).");
        }
    }
}
