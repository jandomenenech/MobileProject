using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// NPC que se mueve por celdas de forma aleatoria, con pausas.
/// Usa los mismos colliders del mapa (Map Collider 1 y 2) que el personaje.
/// Pensado para el conejo u otros NPCs con animaciones por orientación (AP, PA, Perfil).
/// </summary>
public class NPCMovimientoAleatorio : MonoBehaviour
{
    [Header("Grid (igual que el personaje)")]
    [SerializeField] private float cellSize = 1.0f;
    [SerializeField] private Vector2 gridOrigin = new Vector2(-0.5f, 1.2f);
    [Tooltip("Offset pivot → centro del sprite (3x5). Si hay SpriteRenderer se calcula solo al inicio; si no, usa este valor.")]
    [SerializeField] private Vector2 offsetCentroSpriteFallback = new Vector2(1.5f, 2.5f);
    [Tooltip("Duración en segundos de cada movimiento completo (ej. 4 = 4 s para recorrer las celdas). Así la animación de 0,5 s puede terminar.")]
    [SerializeField] private float duracionMovimientoSegundos = 4f;
    [Tooltip("Celdas que avanza cada vez que decide moverse (ej. 2 = siempre avanza de 2 en 2).")]
    [SerializeField] private int celdasPorMovimiento = 2;

    [Header("Map Collider (bloqueo por celdas)")]
    [Tooltip("Layers del tilemap de paredes. Si está vacío, se usan MapCollider1 y MapCollider2.")]
    [SerializeField] private LayerMask mapColliderLayers;
    [Tooltip("Grid del tilemap para alinear el chequeo con las celdas (opcional).")]
    [SerializeField] private Grid mapGrid;
    private LayerMask _effectiveMapLayers;

    [Header("Comportamiento aleatorio")]
    [Tooltip("Tiempo mínimo de pausa entre movimientos (segundos).")]
    [SerializeField] private float pausaMin = 0.5f;
    [Tooltip("Tiempo máximo de pausa entre movimientos (segundos).")]
    [SerializeField] private float pausaMax = 2.5f;
    [Tooltip("Probabilidad (0-1) de moverse cuando toca; si no, se queda parado otro ciclo.")]
    [Range(0f, 1f)]
    [SerializeField] private float probabilidadMoverse = 0.7f;

    [Header("Animador (conejo)")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // Nombres de estados del Animator del conejo (si usas otro controlador, ajústalos)
    private const string EstadoAP = "Conejo Caminar AP";
    private const string EstadoPA = "Conejo Caminar PA";
    private const string EstadoPerfil = "Conejo Caminar Perfil";

    private Vector2 targetPosition;
    private Vector2 _moveStartPosition;
    private Vector2 _offsetCentroSprite; // offset desde transform hasta centro visual del sprite (celda del medio 3x5)
    private float _worldCellSize;       // tamaño de una celda en mundo (del Grid si hay, si no cellSize)
    private Rigidbody2D _rb;
    private Vector2 _lastDirection = Vector2.down;
    private bool _isMoving;
    private float _moveStartTime;

    private void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        // Calcular offset al centro del sprite (celda del medio del 3x5)
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Vector3 centroLocal = spriteRenderer.sprite.bounds.center;
            Vector3 centroMundo = spriteRenderer.transform.TransformPoint(centroLocal);
            _offsetCentroSprite = (Vector2)centroMundo - (Vector2)transform.position;
        }
        else if (spriteRenderer != null)
        {
            Vector3 centroMundo = spriteRenderer.bounds.center;
            _offsetCentroSprite = (Vector2)centroMundo - (Vector2)transform.position;
        }
        else
            _offsetCentroSprite = offsetCentroSpriteFallback;
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.simulated = true;
            _rb.useFullKinematicContacts = false;
            _rb.gravityScale = 0f;
            _rb.interpolation = RigidbodyInterpolation2D.None;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        if (GetComponent<Collider2D>() == null)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1f, 1f);
            box.offset = Vector2.zero;
            box.isTrigger = false;
        }

        if (mapGrid == null)
        {
            foreach (var g in FindObjectsOfType<Grid>())
            {
                if (g.transform.Find("Map Collider 1") != null) { mapGrid = g; break; }
            }
            if (mapGrid == null)
            {
                int layerMap = LayerMask.NameToLayer("MapCollider1");
                foreach (var tm in FindObjectsOfType<Tilemap>())
                {
                    if (tm.gameObject.layer == layerMap && tm.layoutGrid != null) { mapGrid = tm.layoutGrid; break; }
                }
            }
            if (mapGrid == null) mapGrid = FindObjectOfType<Grid>();
        }

        // Misma lógica de layers que MovimientoPorCeldas: MapCollider1, MapCollider2 y fallback 3840
        _effectiveMapLayers = mapColliderLayers;
        if (_effectiveMapLayers == 0)
        {
            int L1 = LayerMask.NameToLayer("MapCollider1");
            int L2 = LayerMask.NameToLayer("MapCollider2");
            if (L1 >= 0) _effectiveMapLayers = (LayerMask)(1 << L1);
            if (L2 >= 0) _effectiveMapLayers |= (LayerMask)(1 << L2);
        }
        _effectiveMapLayers |= (LayerMask)((1 << 8) | (1 << 9));
        if (_effectiveMapLayers == 0) _effectiveMapLayers = (LayerMask)3840;

        if (mapGrid != null)
            _worldCellSize = ((Vector2)mapGrid.GetCellCenterWorld(new Vector3Int(1, 0, 0)) - (Vector2)mapGrid.GetCellCenterWorld(Vector3Int.zero)).x;
        else
            _worldCellSize = cellSize;

        // Centrar en celda: la celda del medio del sprite (3x5) queda en el centro de la celda del juego
        Vector2 centroActual = (Vector2)transform.position + _offsetCentroSprite;
        targetPosition = SnapToGrid(centroActual);
        Vector2 posTransform = targetPosition - _offsetCentroSprite;
        transform.position = posTransform;
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.interpolation = RigidbodyInterpolation2D.None;
            _rb.position = posTransform;
        }

        ActualizarAnimacion(_lastDirection, false);
        StartCoroutine(CicloAleatorio());
    }

    private void FixedUpdate()
    {
        if (!_isMoving) return;

        float elapsed = Time.time - _moveStartTime;
        float t = Mathf.Clamp01(elapsed / duracionMovimientoSegundos);
        Vector2 centroCelda = Vector2.Lerp(_moveStartPosition, targetPosition, t);
        Vector2 posTransform = centroCelda - _offsetCentroSprite;

        if (_rb != null)
            _rb.MovePosition(posTransform);
        else
            transform.position = posTransform;

        if (t >= 1f)
        {
            Vector2 posFinal = targetPosition - _offsetCentroSprite;
            if (_rb != null) _rb.position = posFinal;
            else transform.position = posFinal;
            _isMoving = false;
            ActualizarAnimacion(_lastDirection, false);
        }
    }

    private void Update()
    {
        if (_rb != null)
            transform.position = _rb.position;
        // Cuando está parado, mantener centrado en la celda por si hubo desajuste
        if (!_isMoving)
        {
            Vector2 posDeseada = targetPosition - _offsetCentroSprite;
            if (_rb != null)
                _rb.position = posDeseada;
            else
                transform.position = posDeseada;
        }
    }

    private void LateUpdate()
    {
        // Mantener el flip correcto en perfil por si el Animator lo sobrescribe
        if (animator != null && Mathf.Abs(_lastDirection.x) > Mathf.Abs(_lastDirection.y))
            AplicarFlipX(_lastDirection.x > 0);
    }

    private IEnumerator CicloAleatorio()
    {
        while (true)
        {
            float pausa = Random.Range(pausaMin, pausaMax);
            yield return new WaitForSeconds(pausa);

            if (_isMoving) continue;

            if (Random.value > probabilidadMoverse)
                continue;

            Vector2 dir = ElegirDireccionAleatoria();
            // Centro actual del sprite (por si hay desajuste) y celda en la que estamos
            Vector2 posActual = _rb != null ? _rb.position : (Vector2)transform.position;
            Vector2 centroActual = posActual + _offsetCentroSprite;
            Vector2 celdaActual = SnapToGrid(centroActual);
            targetPosition = celdaActual; // mantener targetPosition sincronizado

            // Comprobar que todas las celdas del trayecto (1, 2, ... celdasPorMovimiento) estén libres
            bool todasLibres = true;
            for (int k = 1; k <= celdasPorMovimiento && todasLibres; k++)
            {
                Vector2 celda = celdaActual + dir * (k * _worldCellSize);
                if (IsCellBlockedByMapCollider(celda))
                    todasLibres = false;
            }

            if (todasLibres && celdasPorMovimiento > 0)
            {
                _moveStartPosition = celdaActual;
                targetPosition = celdaActual + dir * (celdasPorMovimiento * _worldCellSize);
                _lastDirection = dir;
                _isMoving = true;
                _moveStartTime = Time.time;
                ActualizarAnimacion(_lastDirection, true);
            }
        }
    }

    private Vector2 ElegirDireccionAleatoria()
    {
        int i = Random.Range(0, 4);
        switch (i)
        {
            case 0: return Vector2.up;
            case 1: return Vector2.down;
            case 2: return Vector2.left;
            default: return Vector2.right;
        }
    }

    /// <summary>
    /// Actualiza la animación según la dirección. Al parar muestra el primer frame (estático).
    /// </summary>
    private void ActualizarAnimacion(Vector2 direction, bool moving)
    {
        if (animator == null) return;

        if (moving)
            animator.speed = 1f;
        else
            animator.speed = 0f;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            animator.Play(EstadoPerfil, 0, 0f);
            AplicarFlipX(direction.x > 0);
        }
        else if (direction.y > 0)
        {
            animator.Play(EstadoPA, 0, 0f);
            AplicarFlipX(false);
        }
        else
        {
            animator.Play(EstadoAP, 0, 0f);
            AplicarFlipX(false);
        }
    }

    /// <summary>
    /// Aplica flipX a todos los SpriteRenderers del conejo (mismo perfil invertido = mirar izquierda).
    /// </summary>
    private void AplicarFlipX(bool invertir)
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
            if (sr != null) sr.flipX = invertir;
    }

    /// <summary>
    /// Snaps a world position to the center of a cell. Si hay mapGrid, usa el Grid del tilemap para coincidir con los cuadrados.
    /// </summary>
    private Vector2 SnapToGrid(Vector2 position)
    {
        if (mapGrid != null)
        {
            Vector3Int cell = mapGrid.WorldToCell(position);
            return (Vector2)mapGrid.GetCellCenterWorld(cell);
        }
        float snappedX = Mathf.Round((position.x - gridOrigin.x) / cellSize) * cellSize + gridOrigin.x;
        float snappedY = Mathf.Round((position.y - gridOrigin.y) / cellSize) * cellSize + gridOrigin.y;
        return new Vector2(snappedX, snappedY);
    }

    private bool IsCellBlockedByMapCollider(Vector2 cellCenter)
    {
        if (_effectiveMapLayers == 0) return false;
        Vector2 checkPoint = cellCenter;
        if (mapGrid != null)
        {
            Vector3Int cell = mapGrid.WorldToCell(cellCenter);
            checkPoint = mapGrid.GetCellCenterWorld(cell);
        }
        return Physics2D.OverlapPoint(checkPoint, _effectiveMapLayers) != null
            || Physics2D.OverlapCircle(checkPoint, 0.05f, _effectiveMapLayers) != null;
    }
}
