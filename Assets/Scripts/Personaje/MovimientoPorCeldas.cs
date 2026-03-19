using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sincroniza el peinado (u otro accesorio) con la animacion del cuerpo: mismo frame AP/PA/Perfil L/R y 0-3 Caminar.
[DefaultExecutionOrder(-100)]
public class MovimientoPorCeldas : MonoBehaviour
{
    [Header("Grid")]
    private float cellSize = 1.0f; // 1 celda = 1 unidad
    [SerializeField] private float moveSpeed = 5.0f;
    private float _baseMoveSpeed;
    [SerializeField] private Vector2 gridOrigin = new Vector2(-0.5f, 1.2f); 

    [Header("Map Collider (bloqueo por celdas)")]
    [Tooltip("Asigna el layer del tilemap de paredes (ej. MapCollider1). Si se pierde, en Start se intenta usar 'MapCollider1'.")]
    [SerializeField] private LayerMask mapColliderLayers;
    [Tooltip("Opcional: Grid del tilemap para alinear el chequeo con las celdas reales (evita bloquear una celda antes).")]
    [SerializeField] private Grid mapGrid;
    private LayerMask _effectiveMapLayers;

    private Vector2 targetPosition;   
    private Animator animator;       
    private Animator[] animatorsHijos;
    private Vector2 movementDirection;
    private bool isMoving = false;

    /// <summary>True si el personaje se est? moviendo hacia una celda.</summary>
    public bool IsMoving => isMoving;    
    private Vector2 inputDirection;   

    private Vector2 lastInputDirection = Vector2.down;

    // Detecci?n de atascado contra Map Collider: si no avanzamos, cancelar y permitir otras direcciones
    private float _lastDistToTarget = float.MaxValue;
    private int _stuckFrames;
    private float _moveStartTime;
    private const int StuckFrameThreshold = 3;

    private Inventario inventario;
    private Rigidbody2D _rb;
    private SpriteRenderer _spriteForFlip;
    private Sprite _spriteAP, _spritePA, _spritePerfilL, _spritePerfilR;
    private static bool _loggedStaticSpriteOnce;

    [Header("Rotar en celda (sin moverse)")]
    [Tooltip("Mantener esta tecla + flecha para solo cambiar la orientacion. Si Control no va, prueba LeftAlt en el Inspector.")]
    [SerializeField] private KeyCode teclaRotar = KeyCode.LeftControl;

    [Header("Correr (Shift)")]
    [Tooltip("Puntos extra de velocidad de movimiento mientras se mantiene Shift (LeftShift o RightShift).")]
    [SerializeField] private float bonusVelocidadCorrer = 1f;

    [Header("Ataque / Gizmo")]
    [SerializeField] private Transform attackCheck;      //
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Sprites estaticos (opcional)")]
    [Tooltip("Asigna aqui los 4 sprites si no cargan desde Resources. AP=abajo, PA=arriba, Perfil L/R=izq/der.")]
    [SerializeField] private Sprite spriteAPInspector, spritePAInspector, spritePerfilLInspector, spritePerfilRInspector;

    // Offsets del gizmo por direccion
    [SerializeField] private Vector2 offsetRight = new Vector2(0.6f, 0f);
    [SerializeField] private Vector2 offsetLeft = new Vector2(-0.6f, 0f);
    [SerializeField] private Vector2 offsetUp = new Vector2(0f, 0.6f);
    [SerializeField] private Vector2 offsetDown = new Vector2(0f, -0.6f);

    [Header("Peinado / accesorios (sincronizado con caminar)")]
    [Tooltip("SpriteRenderer del pelo. Si esta vacio, se busca un hijo llamado \"hair\".")]
    [SerializeField] private SpriteRenderer hairSpriteRenderer;
    [Tooltip("Nombre del peinado para cargar sprites desde Resources (ej: Peinado 1 Marron). Debe coincidir con los nombres AP 0 Caminar, PA 1, etc.")]
    [SerializeField] private string hairSpritePrefix = "Peinado 1 Marr?n";
    [Tooltip("Ruta en Resources donde estan los sprites del peinado (sin nombre del sprite).")]
    [SerializeField] private string hairResourcesPath = "Sprites/Peinado/Marron/Sprites/";

    [Header("Armadura (sincronizada con direccion)")]
    [SerializeField] private string armorSpritePrefix = "Armadura 2";
    [SerializeField] private string armorResourcesPath = "Sprites/Armors/Armor2/Sprites/";

    private SpriteRenderer _bodySpriteForSync;
    private Dictionary<string, Sprite> _hairSpriteCache = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> _armorSpriteCache = new Dictionary<string, Sprite>();

    [Header("Ataque parado (solo Espacio)")]
    [Tooltip("Duracion aproximada de la animacion de ataque en segundos (para volver a estado parado).")]
    [SerializeField] private float duracionAtaqueParado = 0.45f;
    private bool _attackingParado;
    private float _attackEndTime;
    private bool _attackQueued;
    private Transform _manoOTool;

    [Header("Corte (combo: segundo Espacio)")]
    [SerializeField] private float duracionCorteParado = 0.45f;
    private bool _corteParado;
    private float _corteEndTime;
    private bool _corteQueued;

    [Header("Combo Ataque → Corte")]
    [Tooltip("Tiempo máximo (seg) entre el ataque y el corte para que cuente como combo.")]
    [SerializeField] private float comboTimeout = 0.8f;
    private int _comboStep;
    private float _comboLastAttackTime;

    // --- Profundidad Y (sorting basado en fila de grid) ---
    private const int PrecisionOrdenY = 100;
    private const int OffsetTipoJugador = 20;
    private const int OffsetTipoArma = 22;
    private SpriteRenderer[] _sortRenderers;
    private int[] _sortOffsets;

    // Hashes de estados de ataque del BaseAnimator (Base Layer) para detectar cuándo la animación ha terminado
    private static readonly int HashAtacarAP = Animator.StringToHash("Base Layer.Atacar AP");
    private static readonly int HashAtacarPA = Animator.StringToHash("Base Layer.Atacar PA");
    private static readonly int HashAtacarPerfilL = Animator.StringToHash("Base Layer.Atacar Perfil L");
    private static readonly int HashAtacarPerfilR = Animator.StringToHash("Base Layer.Atacar Perfil R");

    private static readonly int HashCorteAP = Animator.StringToHash("Base Layer.Corte AP");
    private static readonly int HashCortePA = Animator.StringToHash("Base Layer.Corte PA");
    private static readonly int HashCortePerfilL = Animator.StringToHash("Base Layer.Corte Perfil L");
    private static readonly int HashCortePerfilR = Animator.StringToHash("Base Layer.Corte Perfil R");

    private const float AtaqueTimeoutSeguridad = 2f;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        animatorsHijos = GetComponentsInChildren<Animator>();
        inventario = GetComponent<Inventario>();
        _rb = GetComponent<Rigidbody2D>();
        if (mapGrid == null)
        {
            foreach (var g in FindObjectsOfType<Grid>())
                if (g.transform.Find("Map Collider 1") != null) { mapGrid = g; break; }
            if (mapGrid == null) mapGrid = FindObjectOfType<Grid>();
        }
        // Animaciones usa layers 8 y 9; otros escenas 10 y 11. Incluir todos para que funcione en cualquier escena.
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
        targetPosition = SnapToGrid(transform.position);
        transform.position = targetPosition;
        _baseMoveSpeed = moveSpeed;
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.interpolation = RigidbodyInterpolation2D.None;
            _rb.position = targetPosition;
        }
        if (GetComponent<Collider2D>() == null)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.8f, 0.8f);
            box.offset = Vector2.zero;
            box.isTrigger = false;
        }

        if (_spriteAP == null)
        {
            _spriteAP = Resources.Load<Sprite>("Sprites/Player Base/Sprites/PJ Cuerpo Base AP 0 Caminar");
            _spritePA = Resources.Load<Sprite>("Sprites/Player Base/Sprites/PJ Cuerpo Base PA 0 Caminar");
            _spritePerfilL = Resources.Load<Sprite>("Sprites/Player Base/Sprites/PJ Cuerpo Base Perfil L 0 Caminar");
            _spritePerfilR = Resources.Load<Sprite>("Sprites/Player Base/Sprites/PJ Cuerpo Base Perfil R 0 Caminar");
            if (_spriteAP == null || _spritePA == null || _spritePerfilL == null || _spritePerfilR == null)
                Debug.LogWarning("MovimientoPorCeldas: No se cargaron todos los sprites de direccion desde Resources. Asignalos en el Inspector (Sprites estaticos) o comprueba la ruta en Assets/Resources/Sprites/Player Base/Sprites/.");
        }
        if (spriteAPInspector != null) _spriteAP = spriteAPInspector;
        if (spritePAInspector != null) _spritePA = spritePAInspector;
        if (spritePerfilLInspector != null) _spritePerfilL = spritePerfilLInspector;
        if (spritePerfilRInspector != null) _spritePerfilR = spritePerfilRInspector;

        if (hairSpriteRenderer == null)
        {
            var t = transform.Find("head/hair");
            if (t != null) hairSpriteRenderer = t.GetComponent<SpriteRenderer>();
            if (hairSpriteRenderer == null)
                foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
                    if (sr.gameObject.name.ToLowerInvariant().Contains("hair")) { hairSpriteRenderer = sr; break; }
        }
        _bodySpriteForSync = null;
        if (animatorsHijos != null)
            foreach (var a in animatorsHijos)
                if (a != null)
                {
                    var sr = a.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sprite != null && (sr.sprite.name.Contains("Cuerpo Base") || sr.sprite.name.Contains("Armadura") || sr.sprite.name.Contains("AP") || sr.sprite.name.Contains("PA")))
                    { _bodySpriteForSync = sr; break; }
                }
        if (_bodySpriteForSync == null && animatorsHijos != null && animatorsHijos.Length > 0)
        {
            var sr = animatorsHijos[0].GetComponent<SpriteRenderer>();
            if (sr != null) _bodySpriteForSync = sr;
        }
        var rootSr = GetComponent<SpriteRenderer>();
        if (rootSr != null && _bodySpriteForSync != null) rootSr.enabled = false;

        _manoOTool = inventario != null && inventario.mano != null ? inventario.mano : null;
        if (_manoOTool == null)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (n == "mano" || n == "tool") { _manoOTool = t; break; }
            }
        }

        ActualizarOrientacionYGizmo();
        InicializarSortingProfundidad();
    }

    void InicializarSortingProfundidad()
    {
        _sortRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        _sortOffsets = new int[_sortRenderers.Length];
        int refOrder = _sortRenderers.Length > 0 ? _sortRenderers[0].sortingOrder : 0;
        for (int i = 0; i < _sortRenderers.Length; i++)
            _sortOffsets[i] = _sortRenderers[i].sortingOrder - refOrder;
    }

    void FixedUpdate()
    {
        if (inventario != null && inventario.isActive) return;

        Vector2 currentPosition = _rb != null ? _rb.position : (Vector2)transform.position;
        Vector2 newPosition = Vector2.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.fixedDeltaTime);

        if (_rb != null)
            _rb.MovePosition(newPosition);
        else
            transform.position = newPosition;

        Vector2 actualPosition = _rb != null ? _rb.position : (Vector2)transform.position;

        if ((actualPosition - targetPosition).sqrMagnitude < 0.0001f)
        {
            if (_rb != null) _rb.position = targetPosition;
            else transform.position = targetPosition;
            isMoving = false;
            _stuckFrames = 0;
            _lastDistToTarget = float.MaxValue;
            if (_attackQueued)
            {
                _attackQueued = false;
                var ataque = GetComponent<AtaqueyInteraccion>();
                if (ataque != null)
                    IniciarAtaqueParado(ataque);
            }
            else if (_corteQueued)
            {
                _corteQueued = false;
                var ataque = GetComponent<AtaqueyInteraccion>();
                if (ataque != null)
                    IniciarCorteParado(ataque);
            }
            else
            {
                DetectMovementInput();
            }
        }
        else if (isMoving)
        {
            float dist = Vector2.Distance(actualPosition, targetPosition);
            float timeMoving = Time.time - _moveStartTime;
            float maxTimeForCell = (cellSize / moveSpeed) * 1.05f;
            bool timeout = timeMoving > maxTimeForCell;
            if (dist >= _lastDistToTarget - 0.0001f)
                _stuckFrames++;
            else
                _stuckFrames = 0;
            _lastDistToTarget = dist;
            if (_stuckFrames >= StuckFrameThreshold || timeout)
            {
                targetPosition = SnapToGrid(actualPosition);
                if (_rb != null) _rb.position = targetPosition;
                else transform.position = targetPosition;
                isMoving = false;
                _stuckFrames = 0;
                _lastDistToTarget = float.MaxValue;
            }
        }
    }

    void Update()
    {
        ProcesarRotacionEnCelda();
        if (inventario != null && inventario.isActive)
        {
            ActualizarOrientacionYGizmo();
            return;
        }

        if (_comboStep > 0 && Time.time - _comboLastAttackTime > comboTimeout)
            _comboStep = 0;

        ProcesarEntradaAtaque();

        if (_rb != null)
            transform.position = _rb.position;
        Vector2 pos = (Vector2)transform.position;
        movementDirection = targetPosition - pos;
        UpdateAnimations();
        ActualizarOrientacionYGizmo();

        // Shift: aumentar velocidad de movimiento; al soltar vuelve a la base
        if (!_attackingParado && !_corteParado)
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            moveSpeed = shift ? _baseMoveSpeed + bonusVelocidadCorrer : _baseMoveSpeed;
        }
    }

    void LateUpdate()
    {
        if (_sortRenderers == null) return;

        Vector2 celdaActual = GetPosicionCeldaActual();
        int fila;
        if (mapGrid != null)
        {
            // Alineamos la fila con la rejilla real del mapa, igual que el entorno.
            Vector3Int cell = mapGrid.WorldToCell(new Vector3(celdaActual.x, celdaActual.y, 0f));
            fila = cell.y;
        }
        else
        {
            fila = Mathf.RoundToInt(celdaActual.y / cellSize);
        }
        int baseOrden = -fila * PrecisionOrdenY;

        for (int i = 0; i < _sortRenderers.Length; i++)
        {
            if (_sortRenderers[i] == null) continue;
            bool esArma = _manoOTool != null && _sortRenderers[i].transform.IsChildOf(_manoOTool);
            int offsetTipo = esArma ? OffsetTipoArma : OffsetTipoJugador;
            _sortRenderers[i].sortingOrder = baseOrden + offsetTipo + _sortOffsets[i];
        }
    }

    /// <summary>
    /// Si el jugador mantiene Control + direcci?n, actualiza solo la orientaci?n (sin mover).
    /// </summary>
    void ProcesarRotacionEnCelda()
    {
        if (_attackingParado || _corteParado) return;

        bool modifier = Input.GetKey(teclaRotar);
        if (!modifier) return;

        Vector2 rotDirection = Vector2.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKeyDown(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.UpArrow)) rotDirection = Vector2.up;
        if (Input.GetKey(KeyCode.S) || Input.GetKeyDown(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.DownArrow)) rotDirection = Vector2.down;
        if (Input.GetKey(KeyCode.A) || Input.GetKeyDown(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.LeftArrow)) rotDirection = Vector2.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKeyDown(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.RightArrow)) rotDirection = Vector2.right;

        if (rotDirection != Vector2.zero)
        {
            lastInputDirection = rotDirection;
            AplicarParametrosAnimator(lastInputDirection.x, lastInputDirection.y, false);
        }
    }

    void DetectMovementInput()
    {
        inputDirection = Vector2.zero;

        if (_attackingParado || _corteParado)
            return;

        // Nota: si pulsas varias teclas a la vez, la ?ltima l?nea evaluada puede sobrescribir.
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) inputDirection = Vector2.up;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) inputDirection = Vector2.down;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) inputDirection = Vector2.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) inputDirection = Vector2.right;

        // Control + flecha: solo rotar en la celda (cambiar orientaci?n sin moverse)
        bool modifier = Input.GetKey(teclaRotar);
        if (modifier && inputDirection != Vector2.zero)
        {
            lastInputDirection = inputDirection;
            return;
        }

        if (inputDirection != Vector2.zero)
        {
            Vector2 newTargetPosition = targetPosition + inputDirection * cellSize;

            if (IsOrthogonalMove(newTargetPosition) && !IsCellBlockedByMapCollider(newTargetPosition) && !IsCellOccupiedByEntity(newTargetPosition))
            {
                targetPosition = newTargetPosition;
                isMoving = true;
                _moveStartTime = Time.time;
            }
        }
    }

    void UpdateAnimations()
    {
        // Solo animar andar cuando realmente nos movemos; si estamos bloqueados (tecla pulsada pero sin avanzar) no andar
        bool isMovingAnim = isMoving;

        if (_attackingParado || _corteParado)
        {
            AplicarParametrosAnimator(lastInputDirection.x, lastInputDirection.y, false);
            return;
        }

        // Solo actualizamos la ultima direccion cuando hay entrada Y no estamos rotando con Control (para que Control+flecha no se sobrescriba)
        bool modifier = Input.GetKey(teclaRotar);
        if (inputDirection != Vector2.zero && !modifier)
        {
            lastInputDirection = inputDirection;
        }

        // Siempre usar la ?ltima direcci?n para las animaciones
        AplicarParametrosAnimator(lastInputDirection.x, lastInputDirection.y, isMovingAnim);
    }

    void AplicarParametrosAnimator(float h, float v, bool moving)
    {
        if (animatorsHijos != null)
            foreach (var a in animatorsHijos)
                if (a != null)
                {
                    a.SetFloat("Horizontal", h);
                    a.SetFloat("Vertical", v);
                    a.SetBool("IsMoving", moving);
                }
    }

    static bool EsHashAtaque(int hash)
    {
        return hash == HashAtacarAP || hash == HashAtacarPA
            || hash == HashAtacarPerfilL || hash == HashAtacarPerfilR;
    }

    static bool EsHashCorte(int hash)
    {
        return hash == HashCorteAP || hash == HashCortePA
            || hash == HashCortePerfilL || hash == HashCortePerfilR;
    }

    static bool EsHashAccion(int hash)
    {
        return EsHashAtaque(hash) || EsHashCorte(hash);
    }

    /// <summary>True si el Animator está en un estado de acción (ataque o corte) o en transición hacia/desde uno.</summary>
    static bool EstaEnEstadoAccion(Animator a)
    {
        if (a == null || !a.enabled || a.layerCount == 0) return false;
        if (EsHashAccion(a.GetCurrentAnimatorStateInfo(0).fullPathHash)) return true;
        if (a.IsInTransition(0) && EsHashAccion(a.GetNextAnimatorStateInfo(0).fullPathHash)) return true;
        return false;
    }

    void ProcesarEntradaAtaque()
    {
        // Mientras dura la animación de ataque parado, ignorar nuevas pulsaciones
        if (_attackingParado || _attackQueued || _corteParado || _corteQueued)
            return;

        if (inventario == null || !inventario.TieneArmaEquipada)
            return;

        var ataque = GetComponent<AtaqueyInteraccion>();
        if (ataque == null) return;

        // Si está equipada una antorcha, no debe ejecutarse ataque/corte con Espacio.
        if (EstaEquipadaAntorcha())
            return;

        if (Input.GetKeyDown(KeyCode.Space) && ataque.timeNextAttack <= 0f)
        {
            bool esCorteCombo = _comboStep == 1 && (Time.time - _comboLastAttackTime) <= comboTimeout;
            bool esCorteEntorno = ataque.HayEntornoCortableDelante();
            bool esCorte = esCorteCombo || esCorteEntorno;

            if (isMoving)
            {
                if (esCorte)
                    _corteQueued = true;
                else
                    _attackQueued = true;
            }
            else
            {
                if (esCorte)
                    IniciarCorteParado(ataque);
                else
                    IniciarAtaqueParado(ataque);
            }
        }
    }

    void IniciarAtaqueParado(AtaqueyInteraccion ataque)
    {
        if (ataque == null) return;

        ataque.timeNextAttack = ataque.timeIdle;
        _attackQueued = false;

        AplicarParametrosAnimator(lastInputDirection.x, lastInputDirection.y, false);
        ReproducirAtaqueDirecto();

        _attackingParado = true;
        _attackEndTime = Time.time + duracionAtaqueParado;

        // Marcamos que hemos hecho el primer ataque del combo,
        // pero el tiempo del combo empezará a contarse cuando termine la animación.
        _comboStep = 1;
    }

    void IniciarCorteParado(AtaqueyInteraccion ataque)
    {
        if (ataque == null) return;

        ataque.timeNextAttack = ataque.timeIdle;
        _corteQueued = false;

        AplicarParametrosAnimator(lastInputDirection.x, lastInputDirection.y, false);
        ReproducirCorteDirecto();

        _corteParado = true;
        _corteEndTime = Time.time + duracionCorteParado;

        _comboStep = 0;
    }

    // --- Orientacion: al estar parado usamos sprite por codigo; al andar el Animator controla el sprite ---
    void ActualizarOrientacionYGizmo()
    {
        bool estabaAtacandoParado = _attackingParado;

        if (_attackingParado && animator != null)
        {
            bool pasadoGracia = Time.time >= _attackEndTime;
            bool timeout = Time.time >= _attackEndTime + AtaqueTimeoutSeguridad;
            if (timeout || (pasadoGracia && !EstaEnEstadoAccion(animator)))
            {
                _attackingParado = false;
                moveSpeed = _baseMoveSpeed;
            }
        }
        if (_corteParado && animator != null)
        {
            bool pasadoGracia = Time.time >= _corteEndTime;
            bool timeout = Time.time >= _corteEndTime + AtaqueTimeoutSeguridad;
            if (timeout || (pasadoGracia && !EstaEnEstadoAccion(animator)))
            {
                _corteParado = false;
                moveSpeed = _baseMoveSpeed;
            }
        }
        // Aquí es cuando realmente termina la animación de ataque parado.
        // A partir de este momento empieza a contar la ventana de combo.
        if (estabaAtacandoParado && !_attackingParado)
        {
            _comboStep = 1;
            _comboLastAttackTime = Time.time;
        }

        bool accionParada = !isMoving && (_attackingParado || _corteParado);

        bool antorchaEquipada = EstaEquipadaAntorcha();

        if (!isMoving && !accionParada && !antorchaEquipada && Input.GetKeyDown(KeyCode.Space)
            && inventario != null && inventario.TieneArmaEquipada)
        {
            var ataque = GetComponent<AtaqueyInteraccion>();
            if (ataque != null && ataque.timeNextAttack <= 0f)
            {
                bool esCorte = _comboStep == 1 && (Time.time - _comboLastAttackTime) <= comboTimeout;
                ataque.timeNextAttack = ataque.timeIdle;
                AplicarParametrosAnimator(lastInputDirection.x, lastInputDirection.y, false);
                if (esCorte)
                {
                    ReproducirCorteDirecto();
                    _corteParado = true;
                    _corteEndTime = Time.time + duracionCorteParado;
                    _comboStep = 0;
                }
                else
                {
                    ReproducirAtaqueDirecto();
                    _attackingParado = true;
                    _attackEndTime = Time.time + duracionAtaqueParado;
                    _comboStep = 1;
                    _comboLastAttackTime = Time.time;
                }
            }
        }

        accionParada = !isMoving && (_attackingParado || _corteParado);

        if (animatorsHijos != null)
        {
            if (accionParada)
            {
                foreach (var a in animatorsHijos)
                    if (a != null) a.enabled = true;
            }
            else
            {
                foreach (var a in animatorsHijos)
                {
                    if (a == null) continue;
                    bool esArma = _manoOTool != null && a.transform.IsChildOf(_manoOTool);
                    if (esArma)
                    {
                        a.enabled = true;
                        if (!isMoving && !_attackingParado && !_corteParado)
                        {
                            string estado = NombreEstadoEstatico(lastInputDirection);
                            AnimatorStateInfo info = a.GetCurrentAnimatorStateInfo(0);
                            if (!info.IsName(estado))
                                a.Play(estado, 0, 0f);
                        }
                        continue;
                    }
                    a.enabled = isMoving;
                }
            }
        }

        SpriteRenderer srGizmo = spriteRenderer != null ? spriteRenderer : (_spriteForFlip != null ? _spriteForFlip : (_spriteForFlip = GetComponent<SpriteRenderer>() != null ? GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>()));
        if (srGizmo != null) srGizmo.flipX = false;

        if (!isMoving && !accionParada && _spriteAP != null && _spritePA != null && _spritePerfilL != null && _spritePerfilR != null)
        {
            Sprite s = DireccionASpriteCuerpo(lastInputDirection);
            if (s != null)
            {
                if (_bodySpriteForSync != null) { _bodySpriteForSync.enabled = true; _bodySpriteForSync.flipX = false; _bodySpriteForSync.sprite = s; }
                ActualizarPeinadoEstatico(lastInputDirection);
                ActualizarArmaduraEstatica(lastInputDirection);
                if (!_loggedStaticSpriteOnce) { _loggedStaticSpriteOnce = true; Debug.Log("MovimientoPorCeldas: sprite estatico aplicado (parado)."); }
            }
        }
        if (isMoving && !_attackingParado && _bodySpriteForSync != null && _bodySpriteForSync.sprite != null)
        {
            string bodyName = _bodySpriteForSync.sprite.name;
            string suffix = ObtenerSufijoDireccionFrame(bodyName);
            if (!string.IsNullOrEmpty(suffix))
            {
                if (hairSpriteRenderer != null)
                {
                    Sprite hairSprite = CargarSpritePeinado(hairSpritePrefix + suffix);
                    if (hairSprite != null) { hairSpriteRenderer.flipX = false; hairSpriteRenderer.sprite = hairSprite; }
                }
                if (inventario != null && inventario.slotArmadura != null)
                {
                    Sprite armorSprite = CargarSpriteArmadura(armorSpritePrefix + suffix);
                    if (armorSprite != null)
                    {
                        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
                        {
                            if (sr == null) continue;
                            if (!sr.gameObject.name.Equals("armor", System.StringComparison.OrdinalIgnoreCase)) continue;
                            var anim = sr.GetComponent<Animator>();
                            if (anim != null) anim.enabled = false;
                            sr.gameObject.SetActive(true);
                            sr.enabled = true;
                            sr.flipX = false;
                            sr.sprite = armorSprite;
                        }
                    }
                }
                else
                {
                    OcultarArmaduraVisual();
                }
            }
        }

        // Gizmo / ataque: offset segun direccion (solo si hay referencia)
        if (attackCheck == null) return;
        Vector2 gizmoOffset;
        if (Mathf.Abs(lastInputDirection.x) > Mathf.Abs(lastInputDirection.y))
            gizmoOffset = (lastInputDirection.x > 0) ? offsetRight : offsetLeft;
        else
            gizmoOffset = (lastInputDirection.y > 0) ? offsetUp : offsetDown;
        attackCheck.position = (Vector2)transform.position + gizmoOffset;
    }

    /// <summary>
    /// Snaps to la rejilla del personaje (gridOrigin + cellSize) para que el 3x3 quede centrado en la casilla.
    /// No usamos el Grid del tilemap aqu? para no desalinear con los sprites.
    /// </summary>
    Vector2 SnapToGrid(Vector2 position)
    {
        float snappedX = Mathf.Round((position.x - gridOrigin.x) / cellSize) * cellSize + gridOrigin.x;
        float snappedY = Mathf.Round((position.y - gridOrigin.y) / cellSize) * cellSize + gridOrigin.y;
        return new Vector2(snappedX, snappedY);
    }

    /// <summary>
    /// Devuelve la posicion actual del personaje encajada al centro de la celda (para soltar objetos en la misma casilla).
    /// Si hay mapGrid (tilemap), usa su rejilla para que el objeto quede cuadrado con las celdas visibles.
    /// </summary>
    public Vector2 GetPosicionCeldaActual()
    {
        Vector2 pos = _rb != null ? _rb.position : (Vector2)transform.position;
        if (mapGrid != null)
        {
            Vector3Int cell = mapGrid.WorldToCell(pos);
            return (Vector2)mapGrid.GetCellCenterWorld(cell);
        }
        return SnapToGrid(pos);
    }

    bool IsOrthogonalMove(Vector2 newPosition)
    {
        Vector2 difference = newPosition - (Vector2)transform.position;
        return (Mathf.Abs(difference.x) > 0 && Mathf.Abs(difference.y) == 0) ||
               (Mathf.Abs(difference.y) > 0 && Mathf.Abs(difference.x) == 0);
    }

    /// <summary>
    /// Comprueba si la celda destino tiene colisi?n con Map Collider (no se puede caminar).
    /// Usamos OverlapPoint en el centro de la celda para no bloquear por colliders que se solapan de la celda vecina.
    /// </summary>
    bool IsCellBlockedByMapCollider(Vector2 cellCenter)
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

    bool IsCellOccupiedByEntity(Vector2 cellCenter)
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
            if (hit.GetComponent<NPCMovimientoAleatorio>() != null)
                return true;
        }
        return false;
    }

    // Permite a otros scripts obtener la direcci?n actual
    public Vector2 GetLastInputDirection()
    {
        return lastInputDirection;
    }

    // (Opcional) Dibuja una ayuda visual en el editor
    void OnDrawGizmosSelected()
    {
        if (attackCheck == null) return;
        Gizmos.DrawWireSphere(attackCheck.position, 0.2f);
    }


    public void setVelocidad(float velocidad)
    {
        moveSpeed = velocidad;
    }

    /// <summary>Extrae el sufijo de direccion y frame del nombre del sprite del cuerpo (ej: " AP 1 Caminar", " Perfil R 2 Caminar").</summary>
    string ObtenerSufijoDireccionFrame(string bodySpriteName)
    {
        if (string.IsNullOrEmpty(bodySpriteName)) return null;
        string[] markers = { " AP ", " PA ", " Perfil L ", " Perfil R " };
        foreach (string m in markers)
        {
            int i = bodySpriteName.IndexOf(m, System.StringComparison.OrdinalIgnoreCase);
            if (i >= 0) return bodySpriteName.Substring(i);
        }
        return null;
    }

    Sprite CargarSpritePeinado(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;
        if (_hairSpriteCache.TryGetValue(spriteName, out Sprite cached)) return cached;
        string path = (string.IsNullOrEmpty(hairResourcesPath) ? "" : hairResourcesPath.TrimEnd('/') + "/") + spriteName;
        Sprite s = Resources.Load<Sprite>(path);
        if (s != null) _hairSpriteCache[spriteName] = s;
        return s;
    }

    /// <summary>
    /// Recalcula la lista de Animators hijos. Llamar despues de instanciar o destruir un arma equipada.
    /// </summary>
    public void RefrescarAnimatorsHijos()
    {
        animatorsHijos = GetComponentsInChildren<Animator>();
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        RefrescarSortingProfundidad();
    }

    void RefrescarSortingProfundidad()
    {
        var nuevos = GetComponentsInChildren<SpriteRenderer>(true);
        var offsetsNuevos = new int[nuevos.Length];
        for (int i = 0; i < nuevos.Length; i++)
        {
            bool encontrado = false;
            if (_sortRenderers != null)
                for (int j = 0; j < _sortRenderers.Length; j++)
                    if (_sortRenderers[j] == nuevos[i])
                    {
                        offsetsNuevos[i] = _sortOffsets[j];
                        encontrado = true;
                        break;
                    }
            if (!encontrado)
                offsetsNuevos[i] = 0;
        }
        _sortRenderers = nuevos;
        _sortOffsets = offsetsNuevos;
    }

    /// <summary>
    /// Fuerza el Animator del arma equipada al estado estatico que coincide con la direccion actual.
    /// Llamar justo despues de equipar un arma para evitar que quede en el estado por defecto.
    /// </summary>
    public void ForzarEstadoArmaParado()
    {
        if (_manoOTool == null || animatorsHijos == null) return;
        string estado = NombreEstadoEstatico(lastInputDirection);
        foreach (var a in animatorsHijos)
        {
            if (a == null) continue;
            if (!a.transform.IsChildOf(_manoOTool)) continue;
            a.SetFloat("Horizontal", lastInputDirection.x);
            a.SetFloat("Vertical", lastInputDirection.y);
            a.SetBool("IsMoving", false);
            a.Play(estado, 0, 0f);
        }
    }

    /// <summary>
    /// Reproduce directamente el estado de ataque correcto en TODOS los Animators,
    /// sin depender de triggers ni del estado previo del Animator.
    /// </summary>
    void ReproducirAtaqueDirecto()
    {
        if (animatorsHijos == null) return;
        string estadoAtaque = NombreEstadoAtaque(lastInputDirection);
        bool antorchaEquipada = EstaEquipadaAntorcha();
        foreach (var a in animatorsHijos)
        {
            if (a == null) continue;
            // Si hay antorcha equipada, evitamos que el Animator del arma reproduzca
            // animaciones de ataque/corte que heredan clips del controller base (p.ej. Hacha).
            if (antorchaEquipada && _manoOTool != null && a.transform.IsChildOf(_manoOTool))
                continue;
            a.enabled = true;
            a.Play(estadoAtaque, 0, 0f);
        }
    }

    void ReproducirCorteDirecto()
    {
        if (animatorsHijos == null) return;
        string estadoCorte = NombreEstadoCorte(lastInputDirection);
        bool antorchaEquipada = EstaEquipadaAntorcha();
        foreach (var a in animatorsHijos)
        {
            if (a == null) continue;
            if (antorchaEquipada && _manoOTool != null && a.transform.IsChildOf(_manoOTool))
                continue;
            a.enabled = true;
            a.Play(estadoCorte, 0, 0f);
        }
    }

    /// <summary>
    /// Detecta si el arma equipada es una antorcha (normal o encendida) comprobando el nombre del Animator controller del arma.
    /// </summary>
    bool EstaEquipadaAntorcha()
    {
        if (_manoOTool == null || animatorsHijos == null) return false;

        foreach (var a in animatorsHijos)
        {
            if (a == null) continue;
            if (!a.transform.IsChildOf(_manoOTool)) continue;

            var ctrl = a.runtimeAnimatorController;
            if (ctrl == null) continue;

            // Normalmente el controller se llama "Antorcha" o "Antorcha Encendida"
            if (!string.IsNullOrEmpty(ctrl.name) && ctrl.name.Contains("Antorcha"))
                return true;
        }

        return false;
    }

    static string NombreEstadoAtaque(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x > 0 ? "Atacar Perfil R" : "Atacar Perfil L";
        return direction.y > 0 ? "Atacar PA" : "Atacar AP";
    }

    static string NombreEstadoCorte(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x > 0 ? "Corte Perfil R" : "Corte Perfil L";
        return direction.y > 0 ? "Corte PA" : "Corte AP";
    }

    /// <summary>AP=abajo, PA=arriba, Perfil L=izq, Perfil R=der. Misma logica para cuerpo y accesorios.</summary>
    static string DireccionASufijoEstatico(Vector2 direction)
    {
        if (direction.y > 0f) return " PA 0 Caminar";   // arriba
        if (direction.y < 0f) return " AP 0 Caminar";  // abajo
        if (direction.x < 0f) return " Perfil L 0 Caminar"; // izquierda
        if (direction.x > 0f) return " Perfil R 0 Caminar"; // derecha
        return " AP 0 Caminar"; // por defecto abajo
    }

    static string NombreEstadoEstatico(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x > 0 ? "Perfil R Estatico" : "Perfil L Estatico";
        return direction.y > 0 ? "PA Estatico" : "AP Estatico";
    }

    Sprite DireccionASpriteCuerpo(Vector2 direction)
    {
        if (direction.y > 0f) return _spritePA;
        if (direction.y < 0f) return _spriteAP;
        if (direction.x < 0f) return _spritePerfilL;
        if (direction.x > 0f) return _spritePerfilR;
        return _spriteAP;
    }

    /// <summary>Parado + Control+flecha: pone el pelo con el sprite correcto. Arriba=PA 0, Abajo=AP 0, Izq=Perfil L 0, Der=Perfil R 0.</summary>
    void ActualizarPeinadoEstatico(Vector2 direction)
    {
        string nombreSpritePelo = NombreSpritePeinadoEstatico(direction);
        Sprite hairSprite = CargarSpritePeinado(nombreSpritePelo);
        if (hairSprite == null) return;

        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null) continue;
            string n = sr.gameObject.name.ToLowerInvariant();
            if (!n.Contains("hair") && !n.Contains("peinado")) continue;

            var anim = sr.GetComponent<Animator>();
            if (anim != null) anim.enabled = false;
            sr.gameObject.SetActive(true);
            sr.enabled = true;
            sr.flipX = false;
            sr.sprite = hairSprite;
        }
    }

    Sprite CargarSpriteArmadura(string spriteName)
    {
        if (_armorSpriteCache.TryGetValue(spriteName, out var cached)) return cached;
        string path = (string.IsNullOrEmpty(armorResourcesPath) ? "" : armorResourcesPath.TrimEnd('/') + "/") + spriteName;
        var s = Resources.Load<Sprite>(path);
        if (s != null) _armorSpriteCache[spriteName] = s;
        return s;
    }

    void ActualizarArmaduraEstatica(Vector2 direction)
    {
        if (inventario == null || inventario.slotArmadura == null)
        {
            OcultarArmaduraVisual();
            return;
        }
        string nombreSprite = armorSpritePrefix + DireccionASufijoEstatico(direction);
        Sprite armorSprite = CargarSpriteArmadura(nombreSprite);
        if (armorSprite == null) return;
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null) continue;
            if (!sr.gameObject.name.Equals("armor", System.StringComparison.OrdinalIgnoreCase)) continue;
            var anim = sr.GetComponent<Animator>();
            if (anim != null) anim.enabled = false;
            sr.gameObject.SetActive(true);
            sr.enabled = true;
            sr.flipX = false;
            sr.sprite = armorSprite;
        }
    }

    void OcultarArmaduraVisual()
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null) continue;
            if (!sr.gameObject.name.Equals("armor", System.StringComparison.OrdinalIgnoreCase)) continue;
            sr.gameObject.SetActive(false);
        }
    }

    /// <summary>Nombre exacto del sprite del pelo: Peinado 1 Marr?n PA/AP/Perfil L/Perfil R 0 Caminar</summary>
    string NombreSpritePeinadoEstatico(Vector2 direction)
    {
        string sufijo = DireccionASufijoEstatico(direction);
        return hairSpritePrefix + sufijo;
    }

}