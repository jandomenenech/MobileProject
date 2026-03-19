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

    [Header("Salud")]
    [Tooltip("Puntos de salud máximos del arbusto/árbol. Cada golpe resta una cantidad de daño.")]
    [SerializeField] private int saludMaxima = 3;
    private int saludActual;

    [Header("Impacto al recibir golpe")]
    [Tooltip("GameObject que contiene la animación 'Árbol Impacto Anímación' (normalmente un hijo del tronco).")]
    [SerializeField] private GameObject impactoAnimacion;
    [Tooltip("Si está activo, la animación de impacto se desactiva sola cuando termina.")]
    [SerializeField] private bool autoDesactivarImpacto = true;

    [Header("Estado final")]
    [Tooltip("Sprite que se mostrará cuando la salud llegue a 0 (por ejemplo, 'Árbol 3 tocón'). Si se deja vacío se mantiene el comportamiento anterior.")]
    [SerializeField] private Sprite spriteFinal;
    [Tooltip("Si está activo y hay spriteFinal, se mantiene ese sprite en lugar de desactivar el SpriteRenderer.")]
    [SerializeField] private bool mantenerSpriteFinal = false;
    [Tooltip("Si está activo, el collider NO se desactiva al destruir (el tocón sigue bloqueando).")]
    [SerializeField] private bool mantenerColliderTrasDestruir = false;
    [Tooltip("Si está activo, el GameObject no se desactiva al terminar la animación de ramas (se queda el tocón en escena).")]
    [SerializeField] private bool mantenerObjetoTrasDestruir = false;

    [Header("Frutos recolectables")]
    [Tooltip("Hijo visual de los frutos (se oculta al recoger). Asigna el GameObject 'Arbusto 2 frutos'.")]
    [SerializeField] private GameObject frutosVisual;
    [Tooltip("Prefab del objeto acumulable que se da al jugador al recoger frutos (ej. Frambuesas).")]
    [SerializeField] private GameObject prefabFruto;
    private bool frutosRecogidos;

    [Header("Botín al cortar")]
    [Tooltip("Prefab que se instanciará en la celda del arbusto al cortarlo por primera vez (ej. Rama).")]
    [SerializeField] private GameObject prefabRamas;
    [Tooltip("Segundo prefab opcional que se instanciará al cortar (ej. otro tipo de recurso).")]
    [SerializeField] private GameObject prefabBotinExtra1;
    [Tooltip("Tercer prefab opcional que se instanciará al cortar.")]
    [SerializeField] private GameObject prefabBotinExtra2;
    [Tooltip("Cuarto prefab opcional que se instanciará al cortar.")]
    [SerializeField] private GameObject prefabBotinExtra3;

    [Header("Sorting efectos corte (fila de grid)")]
    [Tooltip("Offset de tipo para los efectos de corte (mayor = más al frente). Debería ser > Jugador (20). Por defecto 30.")]
    [SerializeField] private int offsetOrdenEfectos = 30;
    [Tooltip("Tamaño de una celda en unidades de mundo (debe coincidir con MovimientoPorCeldas, normalmente 1).")]
    [SerializeField] private float cellSize = 1f;

    private const int PrecisionOrdenY = 100;

    void Start()
    {
        arbusto = GetComponent<SpriteRenderer>();
        saludActual = Mathf.Max(1, saludMaxima);

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

    void LateUpdate()
    {
        SincronizarOrdenFrutosConArbusto();
        ActualizarSortingEfectosCorte();
    }

    void Update()
    {
        // Apagado automático de la animación de ramas (fase final)
        if (destruido && efectoCorteRamas != null && efectoCorteRamas.activeInHierarchy)
        {
            var animRamas = efectoCorteRamas.GetComponent<Animator>();
            if (animRamas != null && animRamas.enabled)
            {
                var stateRamas = animRamas.GetCurrentAnimatorStateInfo(0);
                if (stateRamas.normalizedTime >= 1f && !animRamas.IsInTransition(0))
                {
                    // Siempre ocultamos el efecto de ramas cuando termina su animación.
                    efectoCorteRamas.SetActive(false);

                    // Opcionalmente, también desactivamos todo el objeto si no queremos dejar el tocón en escena.
                    if (!mantenerObjetoTrasDestruir)
                        gameObject.SetActive(false);
                }
            }
        }

        // Apagado automático de la animación de impacto del árbol (si se ha configurado así)
        if (autoDesactivarImpacto && impactoAnimacion != null && impactoAnimacion.activeInHierarchy)
        {
            var animImpacto = impactoAnimacion.GetComponent<Animator>();
            if (animImpacto != null && animImpacto.enabled)
            {
                var stateImpacto = animImpacto.GetCurrentAnimatorStateInfo(0);
                if (stateImpacto.normalizedTime >= 1f && !animImpacto.IsInTransition(0))
                {
                    impactoAnimacion.SetActive(false);
                }
            }
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

    /// <summary>
    /// Aplica daño genérico al arbusto/árbol. El primer daño activa el efecto de hojas;
    /// cuando la salud llega a 0 se pasa a la fase de ramas y destrucción.
    /// </summary>
    /// <param name="danio">Cantidad de puntos de salud a restar (mínimo 1).</param>
    public void RecibirDanio(int danio)
    {
        if (destruido)
            return;

        if (danio <= 0)
            danio = 1;

        // Reproducir animación de impacto en cada golpe
        if (impactoAnimacion != null)
        {
            var animImpacto = impactoAnimacion.GetComponent<Animator>();
            if (!impactoAnimacion.activeInHierarchy)
                impactoAnimacion.SetActive(true);
            if (animImpacto != null)
            {
                // Reiniciamos la animación desde el principio
                animImpacto.Play(0, 0, 0f);
            }
        }

        // Primer impacto: activar estado "cortado" y efecto de hojas
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

        saludActual -= danio;
        if (saludActual > saludMaxima) saludActual = saludMaxima;
        if (saludActual > 0) return;

        // Salud agotada: activar fase de ramas y "destruir" el arbusto/árbol
        Vector2 dropPos = transform.position;
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            Bounds b = col.bounds;
            dropPos = new Vector2(b.center.x, b.min.y + 0.5f);
        }

        void InstanciarBotin(GameObject prefab)
        {
            if (prefab == null) return;
            Instantiate(prefab, dropPos, Quaternion.identity);
        }

        // Botín al cortar: se instancian todos los prefabs configurados.
        InstanciarBotin(prefabRamas);
        InstanciarBotin(prefabBotinExtra1);
        InstanciarBotin(prefabBotinExtra2);
        InstanciarBotin(prefabBotinExtra3);

        if (arbusto != null)
        {
            if (spriteFinal != null && mantenerSpriteFinal)
            {
                arbusto.enabled = true;
                arbusto.sprite = spriteFinal;
            }
            else
            {
                arbusto.enabled = false;
            }
        }

        if (col != null && !mantenerColliderTrasDestruir)
            col.enabled = false;

        if (efectoCorteRamas != null)
        {
            efectoCorteRamas.SetActive(true);
        }

        destruido = true;
    }

    /// <summary>
    /// Atajo para herramientas actuales que llaman a este método.
    /// Equivale a hacer 1 punto de daño.
    /// </summary>
    public void cortarArbusto()
    {
        RecibirDanio(1);
    }

    private void SincronizarOrdenFrutosConArbusto()
    {
        if (arbusto == null || frutosVisual == null) return;

        var srFrutos = frutosVisual.GetComponent<SpriteRenderer>();
        if (srFrutos == null) return;

        srFrutos.sortingLayerID = arbusto.sortingLayerID;
        srFrutos.sortingOrder = arbusto.sortingOrder + 1;
    }

    private void ActualizarSortingEfectosCorte()
    {
        if (efectoCorte == null && efectoCorteRamas == null) return;

        int fila = Mathf.RoundToInt(transform.position.y / cellSize);
        int orden = -fila * PrecisionOrdenY + offsetOrdenEfectos;

        void AplicarSorting(GameObject go)
        {
            if (go == null) return;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) return;
            try { sr.sortingLayerName = "Player"; } catch { }
            sr.sortingOrder = orden;
        }

        AplicarSorting(efectoCorte);
        AplicarSorting(efectoCorteRamas);
    }
}
