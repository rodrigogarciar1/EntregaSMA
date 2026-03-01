using UnityEngine;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

/// <summary>
/// Almacena la informacion pertienente para la ejecucion de los comportamientos.
/// </summary>
public class BaseConocimiento : MonoBehaviour
{   
    /// <summary>
    /// Lista de todas las reliquias que se conocen y se cree que no han sido robadas.
    /// </summary>
    public List<GameObject> relicList;

    /// <summary>
    /// Posicion acutal del jugador. null si se desconoce.
    /// </summary>
    public Transform? PlayerPosition = null;
    
    /// <summary>
    /// Posicion de la salida del mapa.
    /// </summary>
    public Transform Salida;


    /// <summary>
    /// Posicion en la que se vio por ulitma vez al jugador. null si no se dio el caso.
    /// </summary>
    public Transform? LastPlayerSighting = null;

    public bool PlayerHeard = false;

    /// <summary>
    /// Objeto correspondiente a la reliquia asignada. null si no hay una.
    /// </summary>
    public GameObject? reliquiaCercana = null;

    public bool reliquiaComprobada = false;
    /// <summary>
    /// Para activar los comportamientos de custodia de reliquia
    /// </summary>
    public bool AlertaRobo = false;
    /// <summary>
    /// Para evitar conflictos al investigar el ruido
    /// </summary>
    public bool investigatingNoise = false;
}

