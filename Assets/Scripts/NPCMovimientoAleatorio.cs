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

    [Header("Detección y huida del jugador")]
    [Tooltip("Radio de detección en celdas. Si el jugador está a esta distancia o menos, el NPC huye.")]
    [SerializeField] private int radioDeteccionCeldas = 4;
    [Tooltip("Pausa entre intentos de huida (más corta = reacciona más rápido).")]
    [SerializeField] private float pausaHuida = 0.15f;
    [Tooltip("Celdas que avanza al huir (puede ser distinto al movimiento normal).")]
    [SerializeField] private int celdasPorHuida = 2;

    [Header("Animador (conejo)")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // Nombres de estados del Animator del conejo (si usas otro controlador, ajústalos)
    private const string EstadoAP = "Conejo Caminar AP";
    private const string EstadoPA = "Conejo Caminar PA";
    private const string EstadoPerfil = "Conejo Caminar Perfil";

    [Header("Efecto de impacto")]
    [Tooltip("Color del flash al recibir un golpe (rojo semi-transparente por defecto).")]
    [SerializeField] private Color colorImpacto = new Color(1f, 0f, 0f, 0.6f);
    [Tooltip("Duración del flash en segundos.")]
    [SerializeField] private float duracionFlash = 0.7f;

    [Header("Salud")]
    [Tooltip("Vida máxima del NPC.")]
    [SerializeField] private int vidaMaxima = 15;

    [Header("Cadáver")]
    [Tooltip("Sprite a usar cuando el conejo muere (si prefabCadaver no está asignado).")]
    [SerializeField] private Sprite spriteCadaver;
    [Tooltip("Prefab del cadáver con inventario interactuable (CadaverInteractuable). Si se asigna, se instancia en vez del sprite simple.")]
    [SerializeField] private GameObject prefabCadaver;
    [Tooltip("Prefab de Carne de conejo para el inventario del cadáver. Asignar en el conejo o en el prefab del cadáver.")]
    [SerializeField] private GameObject prefabCarneConejo;

    private int vidaActual;
    private Vector2 targetPosition;
    private Vector2 _moveStartPosition;
    private Vector2 _offsetCentroSprite;
    private float _worldCellSize;
    private Rigidbody2D _rb;
    private Vector2 _lastDirection = Vector2.down;
    private bool _isMoving;
    private float _moveStartTime;
    private Coroutine _flashCoroutine;
    private Transform _jugador;

    [Header("Barra de vida")]
    [Tooltip("Distancia vertical entre el conejo y la barra de vida (en unidades mundo).")]
    [SerializeField] private float alturaBarraSobreConejo = 0.6f;
    [Tooltip("Desplazamiento horizontal de la barra (0 = centrada).")]
    [SerializeField] private float offsetHorizontalBarra = 0f;
    [Tooltip("Largo total de la barra en unidades mundo.")]
    [SerializeField] private float longitudBarra = 0.8f;
    [Tooltip("Grosor de la barra en unidades mundo.")]
    [SerializeField] private float grosorBarra = 0.08f;
    [Tooltip("Color de la vida restante.")]
    [SerializeField] private Color colorBarra = Color.green;
    [Tooltip("Color del fondo (vida perdida).")]
    [SerializeField] private Color colorBarraFondo = new Color(0.2f, 0f, 0f, 0.7f);

    private Transform _barraRoot;
    private LineRenderer _barraVida;
    private LineRenderer _barraVidaFondo;

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

        vidaActual = Mathf.Max(1, vidaMaxima);
        CrearBarraVida();

        var mov = FindObjectOfType<MovimientoPorCeldas>();
        if (mov != null) _jugador = mov.transform;

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
        if (animator != null && Mathf.Abs(_lastDirection.x) > Mathf.Abs(_lastDirection.y))
            AplicarFlipX(_lastDirection.x > 0);

        ActualizarBarraVidaCompleta();
    }

    private IEnumerator CicloAleatorio()
    {
        while (true)
        {
            bool jugadorCerca = JugadorEnRangoDeteccion();

            float pausa = jugadorCerca
                ? pausaHuida
                : Random.Range(pausaMin, pausaMax);
            yield return new WaitForSeconds(pausa);

            if (_isMoving) continue;

            if (!jugadorCerca && Random.value > probabilidadMoverse)
                continue;

            Vector2 posActual = _rb != null ? _rb.position : (Vector2)transform.position;
            Vector2 centroActual = posActual + _offsetCentroSprite;
            Vector2 celdaActual = SnapToGrid(centroActual);
            targetPosition = celdaActual;

            Vector2 dir;
            int celdas;

            if (jugadorCerca)
            {
                dir = DireccionHuida(celdaActual);
                celdas = celdasPorHuida;
            }
            else
            {
                dir = ElegirDireccionAleatoria();
                celdas = celdasPorMovimiento;
            }

            bool todasLibres = true;
            for (int k = 1; k <= celdas && todasLibres; k++)
            {
                Vector2 celda = celdaActual + dir * (k * _worldCellSize);
                if (IsCellBlockedByMapCollider(celda) || IsCellOccupiedByEntity(celda))
                    todasLibres = false;
            }

            if (!todasLibres && jugadorCerca)
            {
                dir = ElegirDireccionHuidaAlternativa(celdaActual, dir, celdas);
                if (dir == Vector2.zero)
                    continue;
            }
            else if (!todasLibres)
            {
                continue;
            }

            if (celdas > 0)
            {
                _moveStartPosition = celdaActual;
                targetPosition = celdaActual + dir * (celdas * _worldCellSize);
                _lastDirection = dir;
                _isMoving = true;
                _moveStartTime = Time.time;
                ActualizarAnimacion(_lastDirection, true);
            }
        }
    }

    private bool JugadorEnRangoDeteccion()
    {
        if (_jugador == null) return false;
        Vector2 posNpc = _rb != null ? _rb.position : (Vector2)transform.position;
        Vector2 centroNpc = posNpc + _offsetCentroSprite;
        float distancia = Vector2.Distance(centroNpc, _jugador.position);
        return distancia <= radioDeteccionCeldas * _worldCellSize;
    }

    private Vector2 DireccionHuida(Vector2 celdaActual)
    {
        if (_jugador == null) return ElegirDireccionAleatoria();
        Vector2 delta = celdaActual - (Vector2)_jugador.position;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x > 0 ? Vector2.right : Vector2.left;
        return delta.y > 0 ? Vector2.up : Vector2.down;
    }

    private Vector2 ElegirDireccionHuidaAlternativa(Vector2 celdaActual, Vector2 dirBloqueada, int celdas)
    {
        Vector2[] alternativas;
        if (dirBloqueada == Vector2.up || dirBloqueada == Vector2.down)
            alternativas = new[] { Vector2.left, Vector2.right, -dirBloqueada };
        else
            alternativas = new[] { Vector2.up, Vector2.down, -dirBloqueada };

        foreach (var dir in alternativas)
        {
            bool libre = true;
            for (int k = 1; k <= celdas && libre; k++)
            {
                Vector2 celda = celdaActual + dir * (k * _worldCellSize);
                if (IsCellBlockedByMapCollider(celda) || IsCellOccupiedByEntity(celda))
                    libre = false;
            }
            if (libre) return dir;
        }
        return Vector2.zero;
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

    private bool IsCellOccupiedByEntity(Vector2 cellCenter)
    {
        Vector2 checkPoint = cellCenter;
        if (mapGrid != null)
        {
            Vector3Int cell = mapGrid.WorldToCell(cellCenter);
            checkPoint = mapGrid.GetCellCenterWorld(cell);
        }
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPoint, 0.3f);
        foreach (var hit in hits)
        {
            if (hit.transform.root == transform.root) continue;
            if (hit.isTrigger) continue;
            int layer = hit.gameObject.layer;
            if (((1 << layer) & _effectiveMapLayers) != 0) continue;
            if (hit.GetComponent<MovimientoPorCeldas>() != null
                || hit.GetComponent<NPCMovimientoAleatorio>() != null)
                return true;
        }
        return false;
    }

    // --- Salud ---

    /// <summary>
    /// Aplica daño al NPC, actualiza la barra de vida y reproduce el flash de impacto.
    /// </summary>
    public void RecibirDanio(int cantidad)
    {
        if (vidaActual <= 0) return;

        if (cantidad <= 0) cantidad = 1;

        vidaActual -= cantidad;
        if (vidaActual < 0) vidaActual = 0;

        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashImpacto());

        if (vidaActual <= 0)
            Morir();
    }

    private void Morir()
    {
        Vector2 posBase = _rb != null ? _rb.position : (Vector2)transform.position;
        Vector2 centroActual = posBase + _offsetCentroSprite;
        Vector2 centroCelda = SnapToGrid(centroActual);

        try
        {
            if (prefabCadaver != null)
            {
                GameObject cadaver = Instantiate(prefabCadaver, centroCelda, Quaternion.identity);
                cadaver.name = prefabCadaver.name;

                var cadaverInteract = cadaver.GetComponent<CadaverInteractuable>();
                if (cadaverInteract != null)
                {
                    var jugador = FindObjectOfType<Inventario>();
                    if (jugador != null)
                        cadaverInteract.inventarioJugador = jugador;
                    if (prefabCarneConejo != null)
                        cadaverInteract.prefabCarneConejo = prefabCarneConejo;
                }
            }
            else if (spriteCadaver != null)
            {
                GameObject cadaver = new GameObject("Conejo Cadáver");
                cadaver.transform.position = centroCelda;

                var srCadaver = cadaver.AddComponent<SpriteRenderer>();
                srCadaver.sprite = spriteCadaver;

                if (spriteRenderer != null)
                {
                    srCadaver.sortingLayerID = spriteRenderer.sortingLayerID;
                    srCadaver.sortingOrder = spriteRenderer.sortingOrder;
                }
            }
        }
        catch
        {
            // En caso de cualquier problema al crear el cadáver, no bloquear la destrucción
        }

        Destroy(gameObject);
    }

    private void CrearBarraVida()
    {
        if (_barraRoot != null) return;

        // Raíz de la barra, posicionada sobre el conejo
        _barraRoot = new GameObject("BarraVidaRoot").transform;
        _barraRoot.SetParent(transform);
        _barraRoot.localRotation = Quaternion.identity;
        _barraRoot.localScale = Vector3.one;

        var goFondo = new GameObject("Fondo");
        goFondo.transform.SetParent(_barraRoot, false);
        _barraVidaFondo = goFondo.AddComponent<LineRenderer>();
        InicializarLineRenderer(_barraVidaFondo);

        var goVida = new GameObject("Relleno");
        goVida.transform.SetParent(_barraRoot, false);
        _barraVida = goVida.AddComponent<LineRenderer>();
        InicializarLineRenderer(_barraVida);
    }

    private void InicializarLineRenderer(LineRenderer lr)
    {
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.numCapVertices = 2;
        Material mat = (spriteRenderer != null && spriteRenderer.sharedMaterial != null)
            ? spriteRenderer.sharedMaterial
            : new Material(Shader.Find("Sprites/Default"));
        lr.material = mat;
    }

    private void ActualizarBarraVidaCompleta()
    {
        if (_barraVida == null || _barraVidaFondo == null || spriteRenderer == null) return;

        float topY = spriteRenderer.bounds.max.y;
        Vector3 centro = new Vector3(
            spriteRenderer.bounds.center.x + offsetHorizontalBarra,
            topY + alturaBarraSobreConejo,
            0f);

        float half = longitudBarra * 0.5f;
        Vector3 izq = centro + Vector3.left * half;
        Vector3 der = centro + Vector3.right * half;

        float ratio = vidaMaxima > 0 ? Mathf.Clamp01((float)vidaActual / vidaMaxima) : 0f;

        // Si está al 100% de vida, ocultar completamente la barra.
        if (ratio >= 0.999f)
        {
            _barraVida.enabled = false;
            _barraVidaFondo.enabled = false;
            return;
        }

        _barraVida.enabled = true;
        _barraVidaFondo.enabled = true;

        _barraVidaFondo.startWidth = grosorBarra;
        _barraVidaFondo.endWidth = grosorBarra;
        _barraVidaFondo.startColor = colorBarraFondo;
        _barraVidaFondo.endColor = colorBarraFondo;
        _barraVidaFondo.SetPosition(0, izq);
        _barraVidaFondo.SetPosition(1, der);

        Vector3 derVida = izq + Vector3.right * (longitudBarra * ratio);

        _barraVida.startWidth = grosorBarra;
        _barraVida.endWidth = grosorBarra;
        _barraVida.startColor = colorBarra;
        _barraVida.endColor = colorBarra;
        _barraVida.SetPosition(0, izq);
        _barraVida.SetPosition(1, derVida);

        int layerId = spriteRenderer.sortingLayerID;
        int baseOrder = spriteRenderer.sortingOrder;
        _barraVidaFondo.sortingLayerID = layerId;
        _barraVidaFondo.sortingOrder = baseOrder + 1;
        _barraVida.sortingLayerID = layerId;
        _barraVida.sortingOrder = baseOrder + 2;
    }

    /// <summary>
    /// Aplica un flash de color al NPC.
    /// </summary>
    private IEnumerator FlashImpacto()
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        var coloresOriginales = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            coloresOriginales[i] = renderers[i].color;

        Color tinte = colorImpacto;
        foreach (var sr in renderers)
            sr.color = tinte;

        float timer = 0f;
        while (timer < duracionFlash)
        {
            timer += Time.deltaTime;
            float t = timer / duracionFlash;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null)
                    renderers[i].color = Color.Lerp(tinte, coloresOriginales[i], t);
            yield return null;
        }

        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null)
                renderers[i].color = coloresOriginales[i];

        _flashCoroutine = null;
    }
}
