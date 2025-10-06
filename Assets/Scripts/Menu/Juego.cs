using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Juego
{
    public List<Personaje> personajes;
    public List<Casa> casas;
    public List<Recurso> recursos;
}

[System.Serializable]
public class Personaje
{
    public string nombre;
    public int edad;
    public bool estaVivo;
    public List<Personaje> hijos;
}

[System.Serializable]
public class Casa
{
    public string ubicacion;
    public int nivel;
}

[System.Serializable]
public class Recurso
{
    public string tipo;
    public int cantidad;
}

