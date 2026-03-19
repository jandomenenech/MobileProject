using UnityEngine;

[DefaultExecutionOrder(2000)]
public class AntorchaLuz : MonoBehaviour
{
    [Header("Modo 1: hijo fuego (si existe)")]
    [Tooltip("Si el prefab tiene un hijo con la animación, se activa/desactiva con E.")]
    [SerializeField] private GameObject fuego; // hijo con la animación de la llama

    [Header("Modo 2: cambio AnimatorOverrideController (fallback)")]
    [Tooltip("Ruta en Resources (sin extensión) del AnimatorOverrideController 'Antorcha Encendida'.")]
    [SerializeField] private string resourcesPathOverrideEncendida = "Sprites/Hacha/Antorcha/Antorcha Encendida";

    [Header("Iluminación suelo antorcha")]
    [Tooltip("Referencia opcional al objeto 'Iluminación Suelo Antorcha' dentro del prefab. Si no se asigna, se intenta auto-buscar por nombre.")]
    [SerializeField] private GameObject iluminacionSueloAntorcha;

    private bool encendida = false;
    private Animator _animator;
    private RuntimeAnimatorController _controladorNormal;
    private AnimatorOverrideController _controladorEncendida;

    private void Awake()
    {
        _animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
        if (_animator != null)
            _controladorNormal = _animator.runtimeAnimatorController;

        if (!string.IsNullOrEmpty(resourcesPathOverrideEncendida))
            _controladorEncendida = Resources.Load<AnimatorOverrideController>(resourcesPathOverrideEncendida);

        // Empieza apagada
        if (fuego != null)
            fuego.SetActive(false);

        // Auto-localiza iluminación si no está asignada en el Inspector.
        if (iluminacionSueloAntorcha == null)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t == null) continue;
                string n = t.name != null ? t.name.ToLowerInvariant() : "";
                // Coincidimos por palabras clave, tolerando cambios menores de nombre.
                if (n.Contains("ilumin") && (n.Contains("antorcha") || n.Contains("suelo")) )
                {
                    iluminacionSueloAntorcha = t.gameObject;
                    break;
                }
            }
        }

        if (iluminacionSueloAntorcha != null)
            iluminacionSueloAntorcha.SetActive(false);

        // Si no hay hijo fuego, usamos el animator para mostrar la versión encendida
        if (fuego == null && _animator != null && _controladorNormal != null)
            _animator.runtimeAnimatorController = _controladorNormal;
    }

    private void Update()
    {
        // Solo si la antorcha está activa en la escena (equipada)
        if (!gameObject.activeInHierarchy) return;

        // Pulsar E alterna encendida/apagada
        if (Input.GetKeyDown(KeyCode.L))
        {
            encendida = !encendida;
            if (fuego != null)
            {
                fuego.SetActive(encendida);
                if (iluminacionSueloAntorcha != null)
                    iluminacionSueloAntorcha.SetActive(encendida);
                return;
            }

            // Fallback: cambiar el controlador del Animator para alternar sprites/animaciones
            if (_animator == null || _controladorNormal == null)
                return;

            if (encendida)
            {
                if (_controladorEncendida == null)
                {
                    Debug.LogWarning($"AntorchaLuz: no encontré override encendida en Resources '{resourcesPathOverrideEncendida}'.");
                    return;
                }

                _animator.runtimeAnimatorController = _controladorEncendida;
                _animator.Rebind();
            }
            else
            {
                _animator.runtimeAnimatorController = _controladorNormal;
                _animator.Rebind();
            }

            if (iluminacionSueloAntorcha != null)
                iluminacionSueloAntorcha.SetActive(encendida);
        }
    }

    private void LateUpdate()
    {
        // Inventario activará todos los SpriteRenderer del arma al equipar.
        // Con esto, garantizamos que "antorcha encendida" NO se vea hasta que pulses la tecla.
        if (fuego != null)
        {
            if (!encendida && fuego.activeSelf)
                fuego.SetActive(false);

            if (iluminacionSueloAntorcha != null && !encendida && iluminacionSueloAntorcha.activeSelf)
                iluminacionSueloAntorcha.SetActive(false);
        }
        else if (_animator != null && _controladorNormal != null)
        {
            // Modo fallback: si no está encendida, mantenemos el controlador normal.
            if (!encendida && _animator.runtimeAnimatorController != _controladorNormal)
            {
                _animator.runtimeAnimatorController = _controladorNormal;
                _animator.Rebind();
            }

            if (iluminacionSueloAntorcha != null && !encendida && iluminacionSueloAntorcha.activeSelf)
                iluminacionSueloAntorcha.SetActive(false);
        }
    }
}
