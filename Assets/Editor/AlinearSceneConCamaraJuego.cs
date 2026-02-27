using UnityEngine;
using UnityEditor;

/// <summary>
/// Alinea la vista de Scene con la cámara del juego para que el Canvas/UI
/// se vea en la Scene al mismo tamaño que en el Game.
/// </summary>
public static class AlinearSceneConCamaraJuego
{
    [MenuItem("View/Alinear Scene View con cámara del juego")]
    public static void Alinear()
    {
        Camera cam = null;
        GameObject mainCamGo = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCamGo != null)
            cam = mainCamGo.GetComponent<Camera>();
        if (cam == null)
            cam = Object.FindObjectOfType<Camera>();
        if (cam == null)
        {
            Debug.LogWarning("No hay ninguna cámara en la escena.");
            return;
        }

        SceneView view = SceneView.lastActiveSceneView;
        if (view == null)
        {
            Debug.LogWarning("Abre la ventana Scene para usar esta opción.");
            return;
        }

        view.AlignViewToObject(cam.transform);
        view.Repaint();
    }
}
