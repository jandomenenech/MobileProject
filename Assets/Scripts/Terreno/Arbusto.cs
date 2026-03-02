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

    [Header("Frutos recolectables")]
    [Tooltip("Hijo visual de los frutos (se oculta al recoger). Asigna el GameObject 'Arbusto 2 frutos'.")]
    [SerializeField] private GameObject frutosVisual;
    [Tooltip("Prefab del objeto acumulable que se da al jugador al recoger frutos (ej. Frambuesas).")]
    [SerializeField] private GameObject prefabFruto;
    private bool frutosRecogidos;

    [Header("Botín al cortar")]
    [Tooltip("Prefab que se instanciará en la celda del arbusto al cortarlo por primera vez (ej. Rama).")]
    [SerializeField] private GameObject prefabRamas;

    void Start()
    {
        arbusto = GetComponent<SpriteRenderer>();

        if (frutosVisual == null)
        {
            foreach (Transform hijo in transform)
            {
                if (hijo.name.IndexOf("Frutos", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    frutosVisual = hijo.gameObject;
                    break;
                }
            }
        }
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

    public bool TieneFrutos => !frutosRecogidos && frutosVisual != null && frutosVisual.activeSelf;

    /// <summary>
    /// Oculta el visual de los frutos y devuelve una instancia del prefab fruto
    /// para que el inventario lo recoja. Devuelve null si ya se recogieron o no hay prefab.
    /// </summary>
    public GameObject RecogerFrutos()
    {
        if (frutosRecogidos || frutosVisual == null) return null;

        frutosRecogidos = true;
        frutosVisual.SetActive(false);

        if (prefabFruto == null) return null;

        GameObject fruto = Instantiate(prefabFruto);
        fruto.SetActive(false);
        return fruto;
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

            // Calcular posición de drop ANTES de desactivar el collider.
            Vector2 dropPos = transform.position;
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                Bounds b = col.bounds;
                dropPos = new Vector2(b.center.x, b.min.y + 0.5f);
            }

            // Instanciar botín (ramas) en la celda del arbusto en el segundo golpe.
            if (prefabRamas != null)
            {
                Instantiate(prefabRamas, dropPos, Quaternion.identity);
            }

            arbusto.enabled = false;

            if (col != null)
                col.enabled = false;

            if (efectoCorteRamas != null)
            {
                efectoCorteRamas.SetActive(true);
            }
        }
    }
}
