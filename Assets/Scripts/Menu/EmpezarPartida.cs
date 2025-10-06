using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmpezarPartida : MonoBehaviour
{
    [SerializeField] private GameObject guardar;
    [SerializeField] private GameObject inputNombre;
    [SerializeField] private GameObject titulo;
    [SerializeField] private GameObject empezarPartida;
    [SerializeField] private GameObject volver;
    void Start()
    {
        Juego juego = new Juego();
    }


    void Update()
    {
        
    }

    public void cambiarPagina()
    {
        guardar.SetActive(true);
        inputNombre.SetActive(true);
        titulo.SetActive(false);
        empezarPartida.SetActive(false);
        volver.SetActive(true);
    }

    public void volverPagina()
    {
        guardar.SetActive(false);
        inputNombre.SetActive(false);
        titulo.SetActive(true);
        empezarPartida.SetActive(true);
        volver.SetActive(false);
    }

    public void guardarDatos()
    {
        
    }
}
