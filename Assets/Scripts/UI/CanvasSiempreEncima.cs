using UnityEngine;

/// <summary>
/// Fuerza a que este Canvas se dibuje siempre por encima de Map Jugable 1 y el resto:
/// usa la Sorting Layer "UI" (la última del proyecto) y orden alto para ir encima de todo.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class CanvasSiempreEncima : MonoBehaviour
{
    [Tooltip("Si está activo, este Canvas usa la capa UI para dibujarse encima de todo.")]
    [SerializeField] private bool activo = true;

    [Tooltip("Orden de dibujo del Canvas (cuanto más alto, más encima).")]
    [SerializeField] private int sortingOrder = 32767;

    private const string SortingLayerUI = "UI";
    private Canvas _canvas;

    void Awake()
    {
        _canvas = GetComponent<Canvas>();
        if (!activo || _canvas == null) return;
        ConfigurarCanvasEncima();
    }

    void OnEnable()
    {
        if (!activo) return;
        if (_canvas == null) _canvas = GetComponent<Canvas>();
        if (_canvas == null) return;
        ConfigurarCanvasEncima();
    }

    void LateUpdate()
    {
        if (!activo || _canvas == null) return;
        if (_canvas.sortingLayerName != SortingLayerUI || _canvas.sortingOrder != sortingOrder)
            ConfigurarCanvasEncima();
    }

    void ConfigurarCanvasEncima()
    {
        if (_canvas == null) return;

        int layerId = SortingLayer.NameToID(SortingLayerUI);
        if (layerId == 0)
        {
            Debug.LogWarning("CanvasSiempreEncima: No existe la Sorting Layer '" + SortingLayerUI + "'. Añádela en Edit > Project Settings > Tags and Layers (al final de Sorting Layers).");
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = sortingOrder;
            return;
        }

        _canvas.renderMode = RenderMode.ScreenSpaceCamera;
        _canvas.worldCamera = Camera.main;
        _canvas.planeDistance = 100f;
        _canvas.overrideSorting = true;
        _canvas.sortingLayerID = layerId;
        _canvas.sortingOrder = sortingOrder;
    }
}
