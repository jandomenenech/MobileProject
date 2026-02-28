using UnityEngine;

[CreateAssetMenu(fileName = "NuevaArma", menuName = "Juego/Datos Arma")]
public class DatosArma : ScriptableObject
{
    [Header("Visual")]
    [Tooltip("Prefab con SpriteRenderer + Animator + AnimatorOverrideController.")]
    public GameObject prefabVisual;

    [Header("Sprites")]
    [Tooltip("Prefijo del nombre de sprite (ej: Hacha 1, Espada 1).")]
    public string spritePrefix = "Hacha 1";

    [Tooltip("Ruta en Resources para sprites de caminar/estatico (sin nombre del sprite).")]
    public string spritesResourcePath = "Sprites/Hacha/Hacha1/Sprites/";

    [Tooltip("Ruta en Resources para sprites de ataque (sin nombre del sprite).")]
    public string attackSpritesResourcePath = "Sprites/Hacha/Atacar/";
}
