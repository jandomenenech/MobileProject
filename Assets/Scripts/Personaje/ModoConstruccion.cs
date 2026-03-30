using System.Collections.Generic;
using UnityEngine;

public class ModoConstruccion : MonoBehaviour
{
    private const float CellSize = 1.0f;
    private static readonly Color ColorLibre = new Color(0f, 1f, 0f, 0.5f);
    private static readonly Color ColorOcupada = new Color(1f, 0f, 0f, 0.5f);

    private Inventario inventario;
    private MovimientoPorCeldas movimiento;

    private bool enModoPreview;
    private GameObject previewInstance;
    private int indiceColocableActual;
    private readonly List<int> indicesColocables = new List<int>();
    private bool celdaLibre;

    public bool EnModoPreview => enModoPreview;

    void Awake()
    {
        inventario = GetComponent<Inventario>();
        movimiento = GetComponent<MovimientoPorCeldas>();
    }

    void Update()
    {
        if (inventario == null || movimiento == null) return;

        // Cancelar preview siempre, incluso si el inventario UI se abrió en este frame
        if (enModoPreview && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)))
        {
            CancelarPreview();
            return;
        }

        if (inventario.isActive) return;

        if (enModoPreview)
        {
            ProcesarScroll();
            ActualizarPreview();

            if (Input.GetKeyDown(KeyCode.Space) && celdaLibre)
                ColocarObjeto();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Space) && TieneMartilloEquipado())
                IntentarEntrarEnPreview();
        }
    }

    bool TieneMartilloEquipado()
    {
        if (!inventario.tieneArmaEquipada) return false;
        DatosArma datos = inventario.ArmaEquipadaDatos;
        if (datos == null) return false;
        return datos.name.Contains("Martillo");
    }

    void IntentarEntrarEnPreview()
    {
        ActualizarListaColocables();
        if (indicesColocables.Count == 0) return;

        indiceColocableActual = 0;
        enModoPreview = true;
        CrearPreview();
    }

    void ProcesarScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.01f || indicesColocables.Count <= 1) return;

        indiceColocableActual += scroll > 0f ? 1 : -1;
        indiceColocableActual = ((indiceColocableActual % indicesColocables.Count) + indicesColocables.Count) % indicesColocables.Count;
        ReconstruirPreview();
    }

    void ActualizarListaColocables()
    {
        indicesColocables.Clear();
        for (int i = 0; i < inventario.inventario.Count; i++)
        {
            GameObject obj = inventario.inventario[i];
            if (obj == null) continue;
            var rec = obj.GetComponent<ObjetoRecogible>();
            if (rec != null && rec.usarTagConstruccionColocableAlSoltar)
                indicesColocables.Add(i);
        }
    }

    Vector2 ObtenerCeldaPreview()
    {
        Vector2 celdaJugador = movimiento.GetPosicionCeldaActual();
        Vector2 dir = movimiento.GetLastInputDirection();
        return celdaJugador + dir * CellSize;
    }

    void CrearPreview()
    {
        if (indicesColocables.Count == 0) return;

        int slotIndex = indicesColocables[indiceColocableActual];
        GameObject original = inventario.inventario[slotIndex];
        if (original == null) { CancelarPreview(); return; }

        previewInstance = Instantiate(original, (Vector3)ObtenerCeldaPreview(), Quaternion.identity);
        previewInstance.SetActive(true);

        foreach (var col in previewInstance.GetComponentsInChildren<Collider2D>(true))
            col.enabled = false;

        foreach (var mb in previewInstance.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null) mb.enabled = false;
        }

        ActualizarTinte();
    }

    void ActualizarPreview()
    {
        if (previewInstance == null) return;
        previewInstance.transform.position = (Vector3)ObtenerCeldaPreview();
        ActualizarTinte();
    }

    void ActualizarTinte()
    {
        if (previewInstance == null) return;

        celdaLibre = !HayObstaculoEnCelda((Vector2)previewInstance.transform.position);
        Color tinte = celdaLibre ? ColorLibre : ColorOcupada;

        foreach (var sr in previewInstance.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr != null) sr.color = tinte;
        }
    }

    bool HayObstaculoEnCelda(Vector2 posicion)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(posicion, 0.3f);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit.isTrigger) continue;
            if (hit.transform.root == transform.root) continue;
            return true;
        }
        return false;
    }

    void ReconstruirPreview()
    {
        if (previewInstance != null)
            Destroy(previewInstance);

        ActualizarListaColocables();
        if (indicesColocables.Count == 0) { CancelarPreview(); return; }
        if (indiceColocableActual >= indicesColocables.Count)
            indiceColocableActual = 0;

        CrearPreview();
    }

    void ColocarObjeto()
    {
        if (indicesColocables.Count == 0 || previewInstance == null) return;

        int slotIndex = indicesColocables[indiceColocableActual];
        GameObject objInventario = inventario.inventario[slotIndex];
        if (objInventario == null) { CancelarPreview(); return; }

        Vector2 posColocacion = previewInstance.transform.position;
        int cantidad = inventario.GetCantidadEnSlot(slotIndex);
        var recogible = objInventario.GetComponent<ObjetoRecogible>();
        bool esAcumulable = recogible != null && recogible.categoria == CategoriaObjeto.Acumulable;

        if (esAcumulable && cantidad > 1)
        {
            inventario.SetCantidadEnSlot(slotIndex, cantidad - 1);
            GameObject colocado = Instantiate(objInventario, posColocacion, Quaternion.identity);
            ActivarColocado(colocado);
        }
        else
        {
            inventario.inventario[slotIndex] = null;
            inventario.SetCantidadEnSlot(slotIndex, 0);
            objInventario.transform.position = posColocacion;
            ActivarColocado(objInventario);
        }

        if (inventario.inv != null)
            inventario.inv.imagenesInventario();

        Destroy(previewInstance);
        previewInstance = null;
        enModoPreview = false;
    }

    void ActivarColocado(GameObject obj)
    {
        obj.SetActive(true);
        try { obj.tag = "ConstruccionColocable"; }
        catch (UnityException) { }

        foreach (var col in obj.GetComponentsInChildren<Collider2D>(true))
            col.enabled = true;

        var rec = obj.GetComponent<ObjetoRecogible>();
        if (rec != null)
        {
            rec.esRecogido = false;
            rec.player = null;
        }
    }

    void CancelarPreview()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
        enModoPreview = false;
    }
}
