using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class AnimatorOverrideBatchGenerator
{
    [MenuItem("Tools/Generar Nuevo Animator por Skin")]
    public static void GenerarOverrides()
    {
        string animBasePath = "Assets/Animations/";
        string controllerBasePath = animBasePath + "BaseAnimator.controller";
        string outputOverridesPath = animBasePath + "Overrides/";

        AnimatorController baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerBasePath);
        if (baseController == null)
        {
            Debug.LogError("❌ No se encontró el controller base en: " + controllerBasePath);
            return;
        }

        Directory.CreateDirectory(outputOverridesPath);

        foreach (string subFolder in Directory.GetDirectories(animBasePath))
        {
            string folderName = Path.GetFileName(subFolder);
            if (folderName == "Overrides") continue;

            string overridesPath = Path.Combine(outputOverridesPath, folderName + ".overrideController");
            AnimatorOverrideController aoc = new AnimatorOverrideController(baseController);
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();

            foreach (var clip in baseController.animationClips)
            {
                // Busca por el patrón: "Armadura 2 Perfil L Estatico.anim", etc.
                string expectedClipName = folderName + " " + clip.name + ".anim";
                string newClipPath = Path.Combine(animBasePath, folderName, expectedClipName);
                AnimationClip newClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(newClipPath);

                if (newClip != null)
                {
                    overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(clip, newClip));
                    Debug.Log($"✅ [{folderName}] reemplazado: {clip.name}");
                }
                else
                {
                    overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(clip, clip));
                    Debug.LogWarning($"⚠ [{folderName}] faltante: {clip.name} (se usará original)");
                }
            }

            aoc.ApplyOverrides(overrides);
            AssetDatabase.CreateAsset(aoc, overridesPath);
            Debug.Log($"🎯 Override creado: {overridesPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ Todos los AnimatorOverrideController han sido generados.");
    }
}

