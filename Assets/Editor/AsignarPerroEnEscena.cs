using UnityEngine;
using UnityEditor;

public static class AsignarPerroEnEscena
{
    [MenuItem("Tools/Asignar Perro - Sprite y Animator")]
    public static void Asignar()
    {
        var perro = GameObject.Find("Perro");
        if (perro == null)
        {
            Debug.LogWarning("No hay ningún GameObject llamado 'Perro' en la escena.");
            return;
        }

        var sr = perro.GetComponent<SpriteRenderer>();
        var anim = perro.GetComponent<Animator>();
        if (sr == null || anim == null)
        {
            Debug.LogWarning("El objeto Perro no tiene SpriteRenderer o Animator.");
            return;
        }

        Object[] sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Animales/Dogs.png");
        Sprite dogSprite = null;
        foreach (var o in sprites)
        {
            if (o is Sprite s && s.name == "Dogs_0")
            {
                dogSprite = s;
                break;
            }
        }
        if (dogSprite == null)
        {
            Debug.LogError("No se encontró el sprite Dogs_0 en Assets/Sprites/Animales/Dogs.png");
            return;
        }

        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Sprites/Animales/Dog.controller");
        if (controller == null)
        {
            Debug.LogError("No se encontró Assets/Sprites/Animales/Dog.controller");
            return;
        }

        sr.sprite = dogSprite;
        anim.runtimeAnimatorController = controller;
        EditorUtility.SetDirty(perro);
        Debug.Log("Perro: asignados sprite Dogs_0 y controller Dog.");
    }
}
