using UnityEngine;

public class GameLogic : MonoBehaviour
{
    private Juego estadoActual;

    private void Start()
    {
        estadoActual = GameDataManager.Instance.CargarJuego();
        if (estadoActual == null)
        {
            estadoActual = new Juego();
            
        }
    }

   /*ublic void AlcanzarNuevoNivel(int nivel)
    {
        estadoActual.nivel = nivel;
        GameDataManager.Instance.GuardarJuego(estadoActual);
        Debug.Log("Nivel guardado: " + nivel);
    }*/

}


