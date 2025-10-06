using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

public class AnimacionesAutomatizadas : MonoBehaviour
{
    [MenuItem("Tools/Crear Animaciones por Dirección")]
    public static void CrearAnimacionesPorDireccion()
    {
        string itemBase = "Hacha 1"; // Cambia esto según el ítem base
        string[] direcciones = { "AP", "PA", "Perfil L", "Perfil R" };
        string resourcePath = "Sprites/Hacha";

        Sprite[] allSprites = Resources.LoadAll<Sprite>(resourcePath);

        foreach (var s in allSprites)
            Debug.Log("📦 Sprite encontrado en Resources: " + s.name);

        if (allSprites.Length == 0)
        {
            Debug.LogError("No se encontraron sprites en: " + resourcePath);
            return;
        }

        foreach (string direccion in direcciones)
        {
            string nombreCompleto = itemBase + " " + direccion;
            
            // Filtrar sprites por dirección
            List<Sprite> matchedSprites = new List<Sprite>();
            foreach (var sprite in allSprites)
            {
                if (sprite.name.Contains(nombreCompleto))
                    matchedSprites.Add(sprite);
            }

            if (matchedSprites.Count == 0)
            {
                Debug.LogWarning("No se encontraron sprites para: " + nombreCompleto);
                continue;
            }

            Dictionary<string, Sprite> spritesPorIndice = new Dictionary<string, Sprite>();
            foreach (var sprite in matchedSprites)
            {
                foreach (string index in new[] { "0", "1", "2", "3" })
                {
                    if (sprite.name.Contains(" " + index + " Caminar"))
                    {
                        spritesPorIndice[index] = sprite;
                        break;
                    }
                }
            }

            // Mostrar encontrados
            Debug.Log("=== " + nombreCompleto + " ===");
            foreach (string index in new[] { "0", "1", "2", "3" })
            {
                if (spritesPorIndice.ContainsKey(index))
                    Debug.Log("Encontrado sprite con índice " + index + ": " + spritesPorIndice[index].name);
                else
                    Debug.LogWarning("FALTA sprite con índice " + index + " para " + nombreCompleto);
            }

            // Crear animación de caminar
            if (spritesPorIndice.ContainsKey("0") &&
                spritesPorIndice.ContainsKey("1") &&
                spritesPorIndice.ContainsKey("2") &&
                spritesPorIndice.ContainsKey("3"))
            {
                List<Sprite> orderedSprites = new List<Sprite>
                {
                    spritesPorIndice["2"],
                    spritesPorIndice["1"],
                    spritesPorIndice["2"],
                    spritesPorIndice["3"],
                    spritesPorIndice["0"]
                };

                AnimationClip caminarClip = CrearAnimacionDesdeSprites(orderedSprites, 0.1f);
                GuardarAnimacion(caminarClip, itemBase, nombreCompleto + " Caminar");
            }

            // Crear animación de estatico si existe el índice 0
            if (spritesPorIndice.ContainsKey("0"))
            {
                Sprite estaticoSprite = spritesPorIndice["0"];
                List<Sprite> estaticoFrames = new List<Sprite>
                {
                    estaticoSprite, estaticoSprite, estaticoSprite, estaticoSprite
                };

                AnimationClip estaticoClip = CrearAnimacionDesdeSprites(estaticoFrames, 0.1f); // 0.4s total
                string nombreEstatico = nombreCompleto + " Estatico";
                GuardarAnimacion(estaticoClip, itemBase, nombreEstatico);

                Debug.Log("Animación ESTÁTICO creada para: " + nombreEstatico);
            }
            else
            {
                Debug.LogWarning("No se encontró sprite índice 0 para ESTÁTICO en: " + nombreCompleto);
            }
        }
    }

    private static AnimationClip CrearAnimacionDesdeSprites(List<Sprite> sprites, float frameTime)
    {
        AnimationClip clip = new AnimationClip();
        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            keyFrames[i] = new ObjectReferenceKeyframe
            {
                time = i * frameTime,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyFrames);
        return clip;
    }

    private static void GuardarAnimacion(AnimationClip clip, string itemBase, string nombreAnim)
    {
        string animFolderPath = "Assets/Animations/" + itemBase;
        Directory.CreateDirectory(animFolderPath);
        string savePath = Path.Combine(animFolderPath, nombreAnim + ".anim");
        AssetDatabase.CreateAsset(clip, savePath);
        AssetDatabase.SaveAssets();
        Debug.Log("✅ Animación guardada en: " + savePath);
    }
}





