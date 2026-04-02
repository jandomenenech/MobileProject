using UnityEngine;

/// <summary>
/// Sorting basado en fila de grid para objetos de entorno (hierba, arbustos, troncos).
/// Fórmula: sortingOrder = -fila * precisionOrden + offsetOrden
/// El offsetOrden (25 por defecto) es mayor que el del jugador (20) y NPC (10),
/// por lo que en la misma fila el entorno se renderiza POR DELANTE.
/// Si el jugador baja una fila, su base order sube +100 y pasa al frente automáticamente.
/// </summary>
[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(SpriteRenderer))]
public class OrdenarPorPosicionDelJugador : MonoBehaviour
{
    [Header("Sprites según celda (solo jugador)")]
    [Tooltip("Sprite por defecto (cuando el jugador NO está en la misma casilla base). Si se deja vacío se usa el sprite inicial.")]
    [SerializeField] private Sprite spritePorDefecto;
    [Tooltip("Sprite cuando el jugador está en la MISMA casilla base que esta hierba.")]
    [SerializeField] private Sprite spriteJugadorEnMismaCelda;

    [Header("Celda de referencia de este objeto")]
    [Tooltip("Si está activo (por defecto), usa siempre la posición actual del objeto. Al duplicar, cada copia funcionará correctamente.")]
    [SerializeField] private bool usarPosicionActual = true;
    [SerializeField] private Vector2 centroCelda = Vector2.zero;
    [Tooltip("Altura del sprite medida en número de casillas (1 = ocupa una casilla de alto, 2 = dos casillas, etc.).")]
    [SerializeField] private int alturaEnCeldas = 1;
    [Tooltip("Tamaño de una casilla en unidades de mundo (debería coincidir con cellSize de MovimientoPorCeldas, normalmente 1).")]
    [SerializeField] private float cellSize = 1f;
    [Tooltip("Tolerancia vertical para considerar que jugador y hierba están en la misma casilla (en unidades de mundo).")]
    [SerializeField] private float toleranciaMismaFila = 0.01f;

    [Header("Profundidad Y (fila de grid)")]
    [Tooltip("Multiplicador de precisión. Con 100, cada fila de grid da 100 valores de sortingOrder.")]
    [SerializeField] private int precisionOrden = 100;
    [Tooltip("Offset de tipo (positivo = más al frente). Hierba/Arbusto=25, Jugador=20, NPC=10.")]
    [SerializeField] private int offsetOrden = 25;
    [Tooltip("Si está activo, usa la base del Collider2D para calcular la fila (recomendado para árboles altos). Si se desactiva, usa centroCelda/alturaEnCeldas.")]
    [SerializeField] private bool usarBaseCollider = true;

    [Header("Grid (opcional)")]
    [Tooltip("Si se asigna, la fila se calcula usando este Grid (WorldToCell), igual que el jugador.")]
    [SerializeField] private Grid mapGrid;

    [Header("Animación (misma celda que el jugador, como el sprite)")]
    [Tooltip("Si está activo, el Animator NO avanza hasta que el jugador entre en la misma celda (evita que el clip se reproduzca al dar Play).")]
    [SerializeField] private bool animacionSoloCuandoJugadorEnMismaCelda = true;
    [Tooltip("Al salir de la celda, desactiva el Animator para que no siga en bucle hasta volver a entrar (no aplica mientras espera a ocultar el hijo al terminar el clip).")]
    [SerializeField] private bool desactivarAnimadorAlSalirDeCelda = true;
    [Tooltip("Cuando el clip del estado termina (sin Loop Time), desactiva el GameObject del hijo animado.")]
    [SerializeField] private bool ocultarHijoAlTerminarAnimacion = true;
    [Tooltip("Si está activo, tras ocultar el hijo una vez no se vuelve a reproducir la animación al pisar otra vez la celda.")]
    [SerializeField] private bool animacionSoloUnaVez = true;
    [Tooltip("GameObject con Animator (normalmente un hijo). Si está vacío y la búsqueda está activa, se busca por nombre.")]
    [SerializeField] private GameObject hijoAnimacionMismaCelda;
    [Tooltip("Nombre del hijo si no asignaste la referencia manualmente (ej. diente de león 1 petalos).")]
    [SerializeField] private string nombreHijoAnimacionBusqueda = "diente de león 1 petalos";
    [SerializeField] private bool buscarHijoAnimacionPorNombre = true;
    [Tooltip("Nombre del estado en el Animator Controller (debe coincidir con el estado, p. ej. dientedeleón1petalos).")]
    [SerializeField] private string nombreEstadoAnimacionMismaCelda = "dientedeleón1petalos";
    [Tooltip("Capa del Animator donde está el estado.")]
    [SerializeField] private int capaAnimacion = 0;
    [Tooltip("Si está asignado, se usa en lugar del Animator obtenido del hijo.")]
    [SerializeField] private Animator animadorAnimacionMismaCelda;

    // Legacy fields: se mantienen para no romper la serialización de escenas existentes.
    [HideInInspector] [SerializeField] private Transform jugador;
    [HideInInspector] [SerializeField] private SpriteRenderer jugadorRenderer;
    [HideInInspector] [SerializeField] private int offsetCuandoJugadorArribaOMisma = +1;
    [HideInInspector] [SerializeField] private int offsetCuandoJugadorAbajo = -1;
    [HideInInspector] [SerializeField] private bool usarCentroSprite = false;
    [HideInInspector] [SerializeField] private int ordenFrente = 80;
    [HideInInspector] [SerializeField] private int ordenDetras = 79;

    private MovimientoPorCeldas _movimientoJugador;
    private SpriteRenderer _sr;
    private bool _estaEnMismaCelda;
    private Animator _animadorMismaCeldaResuelto;
    private bool _esperandoOcultarHijoTrasAnimacion;
    private bool _animacionHijoConsumida;
    private int _hashNombreEstadoAnimacion;
    private bool _haEntradoAlEstadoAnimacion;
    private float _tiempoLimiteOcultarHijo = -1f;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (spritePorDefecto == null && _sr != null)
            spritePorDefecto = _sr.sprite;

        if (centroCelda == Vector2.zero)
            centroCelda = transform.position;

        _movimientoJugador = FindObjectOfType<MovimientoPorCeldas>();

        if (mapGrid == null && _movimientoJugador != null)
        {
            // Intentamos reutilizar el mismo Grid que usa el jugador, si existe.
            var grids = FindObjectsOfType<Grid>();
            if (grids != null && grids.Length > 0)
                mapGrid = grids[0];
        }

        ResolverAnimacionMismaCelda();
        if (TieneAnimacionMismaCelda() && animacionSoloCuandoJugadorEnMismaCelda)
            PausarAnimadorMismaCelda();
    }

    void LateUpdate()
    {
        if (_sr == null) return;

        Vector2 centro = usarPosicionActual ? (Vector2)transform.position : centroCelda;

        float yBase;
        var col = GetComponent<Collider2D>();
        if (usarBaseCollider && col != null)
        {
            // Anclar a la base real del collider (punto de apoyo visual), útil para árboles altos.
            Bounds b = col.bounds;
            yBase = b.min.y + 0.5f * cellSize;
        }
        else
        {
            // Comportamiento original: usar centroCelda y alturaEnCeldas.
            yBase = centro.y;
            if (alturaEnCeldas > 1)
                yBase -= (alturaEnCeldas - 1) * 0.5f * cellSize;
        }

        int fila;
        if (mapGrid != null)
        {
            // Alineamos la fila con la rejilla real del mapa (mismas celdas que el jugador).
            Vector3Int cell = mapGrid.WorldToCell(new Vector3(centro.x, yBase, 0f));
            fila = cell.y;
        }
        else
        {
            fila = Mathf.RoundToInt(yBase / cellSize);
        }

        int orden = -fila * precisionOrden + offsetOrden;
        _sr.sortingOrder = orden;

        // --- Misma celda que el jugador: sprites (hierba pradera) y/o animación en hijo ---
        bool quiereSpritesMismaCelda = spriteJugadorEnMismaCelda != null;
        bool quiereAnimacionMismaCelda = TieneAnimacionMismaCelda();

        if (TieneAnimacionMismaCelda() && ocultarHijoAlTerminarAnimacion)
            OcultarHijoSiAnimacionTerminada();

        if ((!quiereSpritesMismaCelda && !quiereAnimacionMismaCelda) || _movimientoJugador == null)
            return;

        bool mismaCelda = JugadorEnMismaCeldaBase(centro, yBase);

        bool entrandoMismaCelda = mismaCelda && !_estaEnMismaCelda;
        bool saliendoMismaCelda = !mismaCelda && _estaEnMismaCelda;

        if (quiereSpritesMismaCelda)
        {
            if (entrandoMismaCelda)
                _sr.sprite = spriteJugadorEnMismaCelda;
            else if (saliendoMismaCelda && spritePorDefecto != null)
                _sr.sprite = spritePorDefecto;
        }

        if (saliendoMismaCelda && quiereAnimacionMismaCelda && animacionSoloCuandoJugadorEnMismaCelda && desactivarAnimadorAlSalirDeCelda)
        {
            bool dejarTerminarClip = ocultarHijoAlTerminarAnimacion && _esperandoOcultarHijoTrasAnimacion;
            if (!dejarTerminarClip)
                PausarAnimadorMismaCelda();
        }

        if (entrandoMismaCelda && quiereAnimacionMismaCelda)
            ReproducirAnimacionMismaCelda();

        _estaEnMismaCelda = mismaCelda;
    }

    private void ResolverAnimacionMismaCelda()
    {
        if (animadorAnimacionMismaCelda != null)
        {
            _animadorMismaCeldaResuelto = animadorAnimacionMismaCelda;
            return;
        }

        if (hijoAnimacionMismaCelda == null && buscarHijoAnimacionPorNombre && !string.IsNullOrEmpty(nombreHijoAnimacionBusqueda))
            hijoAnimacionMismaCelda = BuscarHijoPorNombre(transform, nombreHijoAnimacionBusqueda);

        if (hijoAnimacionMismaCelda != null)
            _animadorMismaCeldaResuelto = hijoAnimacionMismaCelda.GetComponent<Animator>();
    }

    private static GameObject BuscarHijoPorNombre(Transform raiz, string nombre)
    {
        foreach (Transform t in raiz)
        {
            if (string.Equals(t.name, nombre, System.StringComparison.OrdinalIgnoreCase))
                return t.gameObject;
            GameObject encontrado = BuscarHijoPorNombre(t, nombre);
            if (encontrado != null)
                return encontrado;
        }
        return null;
    }

    private bool TieneAnimacionMismaCelda()
    {
        return _animadorMismaCeldaResuelto != null && !string.IsNullOrEmpty(nombreEstadoAnimacionMismaCelda);
    }

    /// <summary>
    /// Misma lógica que el sprite "jugador en misma celda": con Grid se comparan índices de celda; sin Grid, tolerancia en mundo.
    /// </summary>
    private bool JugadorEnMismaCeldaBase(Vector2 centro, float yBase)
    {
        Vector3 posJugador = _movimientoJugador.transform.position;

        if (mapGrid != null)
        {
            Vector3Int celdaJ = mapGrid.WorldToCell(posJugador);
            Vector3Int celdaO = mapGrid.WorldToCell(new Vector3(centro.x, yBase, 0f));
            return celdaJ == celdaO;
        }

        Vector2 celdaJugador = _movimientoJugador.GetPosicionCeldaActual();
        return
            Mathf.Abs(celdaJugador.x - centro.x) <= cellSize * 0.1f &&
            Mathf.Abs(celdaJugador.y - yBase) <= toleranciaMismaFila;
    }

    private void PausarAnimadorMismaCelda()
    {
        if (_animadorMismaCeldaResuelto == null) return;
        _animadorMismaCeldaResuelto.enabled = false;
    }

    private void ReproducirAnimacionMismaCelda()
    {
        if (_animadorMismaCeldaResuelto == null || string.IsNullOrEmpty(nombreEstadoAnimacionMismaCelda))
            return;

        if (animacionSoloUnaVez && _animacionHijoConsumida)
            return;

        GameObject objetivo = ObtenerGameObjectVisualAnimacion();
        if (objetivo != null && !objetivo.activeSelf)
            objetivo.SetActive(true);

        _hashNombreEstadoAnimacion = Animator.StringToHash(nombreEstadoAnimacionMismaCelda);
        _haEntradoAlEstadoAnimacion = false;
        _tiempoLimiteOcultarHijo = -1f;

        _animadorMismaCeldaResuelto.enabled = true;
        _animadorMismaCeldaResuelto.Play(nombreEstadoAnimacionMismaCelda, capaAnimacion, 0f);
        _animadorMismaCeldaResuelto.Update(0f);

        if (ocultarHijoAlTerminarAnimacion)
        {
            _esperandoOcultarHijoTrasAnimacion = true;
            ProgramarOcultarPorDuracionEstado();
        }
    }

    private void ProgramarOcultarPorDuracionEstado()
    {
        var st = _animadorMismaCeldaResuelto.GetCurrentAnimatorStateInfo(capaAnimacion);
        var clips = _animadorMismaCeldaResuelto.GetCurrentAnimatorClipInfo(capaAnimacion);
        bool clipEnLoop = clips.Length > 0 && clips[0].clip != null && clips[0].clip.isLooping;
        if (clipEnLoop)
            return;

        float speed = Mathf.Max(0.0001f, Mathf.Abs(_animadorMismaCeldaResuelto.speed * st.speedMultiplier));
        if (st.length > 0.001f)
            _tiempoLimiteOcultarHijo = Time.time + st.length / speed;
    }

    private GameObject ObtenerGameObjectVisualAnimacion()
    {
        if (hijoAnimacionMismaCelda != null)
            return hijoAnimacionMismaCelda;
        if (animadorAnimacionMismaCelda != null)
            return animadorAnimacionMismaCelda.gameObject;
        if (_animadorMismaCeldaResuelto != null)
            return _animadorMismaCeldaResuelto.gameObject;
        return null;
    }

    private bool EstadoEsElDeLaAnimacion(AnimatorStateInfo st)
    {
        if (string.IsNullOrEmpty(nombreEstadoAnimacionMismaCelda))
            return false;
        return st.IsName(nombreEstadoAnimacionMismaCelda) || st.shortNameHash == _hashNombreEstadoAnimacion;
    }

    private void OcultarHijoSiAnimacionTerminada()
    {
        if (!_esperandoOcultarHijoTrasAnimacion || _animadorMismaCeldaResuelto == null)
            return;

        if (!_animadorMismaCeldaResuelto.enabled || !_animadorMismaCeldaResuelto.gameObject.activeInHierarchy)
            return;

        if (_tiempoLimiteOcultarHijo > 0f && Time.time >= _tiempoLimiteOcultarHijo)
        {
            AplicarOcultarHijoTrasAnimacion();
            return;
        }

        bool enTransicion = _animadorMismaCeldaResuelto.IsInTransition(capaAnimacion);
        var st = _animadorMismaCeldaResuelto.GetCurrentAnimatorStateInfo(capaAnimacion);
        bool enEstadoAnim = EstadoEsElDeLaAnimacion(st);

        if (enEstadoAnim)
            _haEntradoAlEstadoAnimacion = true;

        float nt = st.normalizedTime;
        if (st.loop)
            nt %= 1f;

        // Fin dentro del mismo estado (clip sin loop): normalizedTime llega a 1 o más
        if (enEstadoAnim && nt >= 0.999f && !enTransicion)
        {
            AplicarOcultarHijoTrasAnimacion();
            return;
        }

        // El Animator pasó a otro estado tras reproducir el nuestro (transición al terminar)
        if (_haEntradoAlEstadoAnimacion && !enEstadoAnim && !enTransicion)
        {
            AplicarOcultarHijoTrasAnimacion();
            return;
        }
    }

    private void AplicarOcultarHijoTrasAnimacion()
    {
        _tiempoLimiteOcultarHijo = -1f;

        GameObject objetivo = ObtenerGameObjectVisualAnimacion();
        if (objetivo != null)
            objetivo.SetActive(false);

        if (_animadorMismaCeldaResuelto != null)
            _animadorMismaCeldaResuelto.enabled = false;

        _esperandoOcultarHijoTrasAnimacion = false;
        _haEntradoAlEstadoAnimacion = false;
        if (animacionSoloUnaVez)
            _animacionHijoConsumida = true;
    }
}
