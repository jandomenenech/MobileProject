using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance;

    private string saveFilePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
            saveFilePath = Application.persistentDataPath + "/juegoGuardado.json";
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void GuardarJuego(Juego estadoDelJuego)
    {
        string json = JsonUtility.ToJson(estadoDelJuego, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Juego guardado en: " + saveFilePath);
    }

    public Juego CargarJuego()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            Juego estadoCargado = JsonUtility.FromJson<Juego>(json);
            Debug.Log("Juego cargado desde: " + saveFilePath);
            return estadoCargado;
        }
        else
        {
            Debug.LogWarning("No se encontró ningún archivo de guardado.");
            return null;
        }
    }
}

