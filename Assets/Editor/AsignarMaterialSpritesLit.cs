using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Asigna el material "Sprites-Lit" a todos los SpriteRenderer de la escena activa.
/// Menú: Tools → Aplicar material Sprites-Lit a todos los sprites.
/// </summary>
public static class AsignarMaterialSpritesLit
{
    private const string NombreMaterial = "Sprites-Lit";

    [MenuItem("Tools/Aplicar material Sprites-Lit a todos los sprites")]
    public static void Aplicar()
    {
        Material mat = BuscarMaterial(NombreMaterial);
        if (mat == null)
        {
            Debug.LogError($"No se encontró ningún material llamado '{NombreMaterial}' en el proyecto. Créalo y asígnale el shader URP 2D Sprite-Lit-Default.");
            return;
        }

        SpriteRenderer[] renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (renderers == null || renderers.Length == 0)
        {
            Debug.Log("No hay ningún SpriteRenderer en la escena.");
            return;
        }
        int asignados = 0;
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;
            sr.sharedMaterial = mat;
            asignados++;
            if (PrefabUtility.IsPartOfAnyPrefab(sr.gameObject))
                EditorUtility.SetDirty(sr.gameObject);
        }

        if (EditorSceneManager.GetActiveScene().isDirty == false && asignados > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"Material '{NombreMaterial}' aplicado a {asignados} SpriteRenderer(s). Guarda la escena (Ctrl+S) para conservar los cambios.");
    }

    private static Material BuscarMaterial(string nombre)
    {
        string[] guids = AssetDatabase.FindAssets($"t:Material {nombre}");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null && m.name == nombre)
                return m;
        }
        return null;
    }
}
