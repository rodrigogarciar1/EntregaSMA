using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Data.SqlTypes;
using System.Buffers.Text;
using System.Reflection;
/// <summary>
/// Estructura reactiva subsumida para realizar comportamientos segun las percepciones de los sensores.
/// </summary>
public class CerebroSubsumido : MonoBehaviour
{   
    ///  <summary>Actuador que permite al agente moverse.</summary>
    public NavMeshAgent navAgent;

    ///  <summary>Almacen del estado conocido del mundo al que acceden los comportamientos.</summary>
    public BaseConocimiento baseConocimiento;

    ///  <summary>Velocidad de movimiento</summary>
    public float agentSpeed = 3.5f;

    ///  <summary>Comportamiento ejecutado cuando el agente no puede hacer nada.<para/>
    /// Usado para prevenir softlocks de los agentes.</summary>
    public NPCBehaviour fallbackBehaviour;
    ///  <summary>Array que determina los comportamientos conocidos asi como su prioridad 
    /// (la posicion 0 es la de maxima prioridad).</summary>
    public NPCBehaviour[] subsumido;

    ///  <summary>Cola de comportamientos que cumplieros los requisitos de sensor. Ordenada segun el
    /// orden de <see cref="subsimido"/></summary>
    public Queue<NPCBehaviour> behaviourQueue = new Queue<NPCBehaviour>();

    /// <summary>
    /// Diccionario donde se guardan las precondiciones de sensor de los comportamientos.
    /// Ordenado segun el orden de <see cref="subsimido"/></summary>
    private Dictionary<(Type, string, bool), List<NPCBehaviour>> dict 
        = new Dictionary<(Type, string, bool), List<NPCBehaviour>>();


    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();

        // Se llena el diccionario con los comportamientos y sus activaciones pertinentes
        foreach (NPCBehaviour behaviour in subsumido)
        {
            foreach ((Type, string, bool) state in behaviour.neededSensorState())
            {
                if (!dict.ContainsKey(state))
                {
                    dict.Add(state, new List<NPCBehaviour>());
                }
                dict[state].Add(behaviour);
            }
        }
    }

    private void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.speed = agentSpeed;
    }

    private void Update()
    {
        RunCurrentBehaviour();
    }

    /// <summary>
    /// Avisa al cerebro de que se ha producido el cambio mediante los sensores
    /// </summary>
    /// <param name="sensorType">Tipo de sensor que pericibio el cambio</param>
    /// <param name="obj">Objeto que ha cambiado</param>
    /// <param name="seeing">true si se ha percibido, false si se deja de percibir</param>
    public void Notify(Type sensorType, GameObject obj, bool seeing){   

        string tag = obj.tag; // Se identifica el tipo de objeto
        checkRelevantStateChange(sensorType, obj, tag, seeing); // Se cambia la base de conocimiento si es necesario

        List<NPCBehaviour> possibleBehaviours; // Lista de comportamientos que cumplen los requisitos de sensores

        // Se comprueba si hay algun comportamiento que se active con la informacion recibida
        if (!dict.TryGetValue((sensorType, tag, seeing), out possibleBehaviours))
        {
            return;
        }
        
        // Si la cola de comportamientos tenia alguno ejecutandose
        if (behaviourQueue.Count > 0)
        {   
            // Se avisa al comportamiento de que se va a acabar para que reinicie los valores que necesite
            NPCBehaviour current = behaviourQueue.Peek();
            current.terminate();
        }
        // Se limpia la cola para hacer sitio a los nuevos comportamientos
        behaviourQueue.Clear();
        // Se encolan
        foreach (NPCBehaviour behaviour in possibleBehaviours){
                behaviourQueue.Enqueue(behaviour);
        }
        RunCurrentBehaviour();
    }
    /// <summary>
    /// Cambia la base de conocimiento acorde a lo que se recibe.
    /// </summary>
    /// <param name="sensorType">Tipo de sensor que percibio el cambio</param>
    /// <param name="obj">Objeto percibido</param>
    /// <param name="tag">Tag del objeto</param>
    /// <param name="seeing">true si se ha percibido, false si se deja de percibir</param>
    public void checkRelevantStateChange(Type sensorType, GameObject obj, string tag, bool seeing)
    {
        if (sensorType == typeof(Vision))
        {   
            // Si se percive al jugador por la vision
            if (tag == "Player")
            {
                if (seeing)
                {
                    baseConocimiento.PlayerPosition = obj.transform;
                    baseConocimiento.LastPlayerSighting = obj.transform; 
                }
                else
                {
                    baseConocimiento.PlayerPosition = null;
                }
                return;
            }
            // Si se percive a la reliquia por la vision
            if (tag == "Reliquia")
            {
                baseConocimiento.reliquiaComprobada = seeing;
                if (seeing)
                {   
                    MeshRenderer mr = obj.GetComponentInChildren<MeshRenderer>();

                    if (!mr.enabled)
                    {
                        if (baseConocimiento.relicList.Contains(obj))
                        {
                            baseConocimiento.relicList.Remove(obj);
                            baseConocimiento.reliquiaCercana = null;
                            baseConocimiento.AlertaRobo = true;
                        }
                    }
                }
                return;
            }
        }
        // Si se escucha un ruido
        if (sensorType == typeof(Hearing) && tag == "Noise")
        {
            baseConocimiento.PlayerHeard = seeing;
            if (seeing)
            {
                baseConocimiento.LastPlayerSighting = obj.transform; 
            }
            return;
        }
        // Si toca al jugador
        if (sensorType == typeof(Touch) && tag == "Player")
        {
            if (seeing)
            {
                baseConocimiento.LastPlayerSighting = obj.transform;
            }
            return;
        }
    }
    
    /// <summary>
    /// Ejecuta el siguiente comportamiento en la <see cref="behaviourQueue"/>, 
    /// si no cumple las precondiciones se comprueba el siguiente.
    /// </summary>
    public void RunCurrentBehaviour()
    {
        if (behaviourQueue.Count > 0) {
            if (behaviourQueue.Peek().cumplePrecondiciones()) {
                NPCBehaviour current = behaviourQueue.Peek();   
                current.ejecutar();
            } else {
                RunNextBehaviour();    
            }
        } else {
            // Por si se queda sin comportamientos posibles
            fallbackBehaviour.ejecutar();
        }
    }

    /// <summary>
    /// Desencola el comportamiento actual y ejecuta el siguiente
    /// </summary>
    public void RunNextBehaviour()
    {   
        behaviourQueue.Peek().terminate();
        behaviourQueue.Dequeue();
        RunCurrentBehaviour();
    }

}
