using UnityEngine;

// Se ejecuta después de MovimientoPorCeldas para reaplicar el +1 de sortingOrder.
[DefaultExecutionOrder(1000)]
public class AntorchaEncendidaSortingOffset : MonoBehaviour
{
    [SerializeField] private int offsetOrder = 1;

    private SpriteRenderer _myRenderer;
    private SpriteRenderer _baseRenderer;

    private void Awake()
    {
        _myRenderer = GetComponent<SpriteRenderer>();

        // Si el SpriteRenderer está en el propio hijo, con esto basta; si no, lo buscamos en descendientes.
        if (_myRenderer == null)
            _myRenderer = GetComponentInChildren<SpriteRenderer>(true);

        _baseRenderer = null;
        if (_myRenderer != null)
        {
            // Busca el SpriteRenderer más cercano en los padres que no sea el propio.
            var renderers = GetComponentsInParent<SpriteRenderer>(true);
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    var r = renderers[i];
                    if (r == null) continue;
                    if (r == _myRenderer) continue;
                    _baseRenderer = r;
                    break;
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (_myRenderer == null || _baseRenderer == null) return;

        // Reaplica el +1 cada frame (MovimientoPorCeldas también recalcula sortingOrder en LateUpdate).
        _myRenderer.sortingOrder = _baseRenderer.sortingOrder + offsetOrder;
    }
}

