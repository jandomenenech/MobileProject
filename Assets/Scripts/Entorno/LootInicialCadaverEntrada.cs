using UnityEngine;

/// <summary>
/// Una fila de botín inicial para el inventario del cadáver (prefab + cuántas unidades en ese slot).
/// </summary>
[System.Serializable]
public class LootInicialCadaverEntrada
{
    [Tooltip("Prefab del objeto (debe tener ObjetoRecogible si es para inventario).")]
    public GameObject prefab;

    [Tooltip("Cantidad en este slot (para objetos acumulables). Mínimo 1.")]
    [Min(1)]
    public int cantidad = 1;
}
