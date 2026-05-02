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

    private Queue<Message> mailbox = new Queue<Message>();

    private Dictionary<string, List<Message>> conversations 
        = new Dictionary<string, List<Message>>();

    /// <summary>
    /// Diccionario donde se guardan las precondiciones de sensor de los comportamientos.
    /// Ordenado segun el orden de <see cref="subsimido"/></summary>
    private Dictionary<(Type, string, bool), List<NPCBehaviour>> dict 
        = new Dictionary<(Type, string, bool), List<NPCBehaviour>>();

    private bool elpipas = false;
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
        CerebroSubsumido[] todos = FindObjectsOfType<CerebroSubsumido>();
        Message hello = new Message(Performative.Inform, this.gameObject, Message_Types.Register);

        // 3. Se lo enviamos a todos (incluidos nosotros mismos, para que aparezcamos en la lista)
        foreach (var receptor in todos)
        {
            receptor.ReceiveMessage(hello);
        }
        if (this.gameObject.tag != "Agente_camara") 
        {
            baseConocimiento.agentes.Add(this.gameObject);
        }

        navAgent = GetComponent<NavMeshAgent>();
        navAgent.speed = agentSpeed;
    }

    private void Update()
    {
        ProcessMessages();
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
                    Message Inform = Message.InformPlayerSeen(gameObject, obj.transform.position);
                    ReceiveMessage(Inform);
                    Broadcast(Inform);
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
                    Broadcast(Message.InformReliquiaRobada(gameObject, obj)); // Creo que esta mal, con pasarle solo el objeto robado deberia estar
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

    public void ReceiveMessage(Message msg)
    {
        mailbox.Enqueue(msg);
        LogMessage(msg);
    }


    public void Broadcast(Message msg)
    {
        LogMessage(msg); 
        // buscar cada agente en escena con cerebro y que no importe el orden
        foreach (GameObject agenteObj in baseConocimiento.agentes) { // Queda verificar esto, es nuevo por asi decir, para arreglar lo de la camara que reciba
            if (agenteObj != null && agenteObj != this.gameObject)
            {
                CerebroSubsumido destino = agenteObj.GetComponent<CerebroSubsumido>();
                if (destino != null) destino.ReceiveMessage(msg);
            }
        }
    }

    private void LogMessage(Message msg)
    {
        if (!conversations.ContainsKey(msg.ConvID)) conversations[msg.ConvID] = new List<Message>();
        conversations[msg.ConvID].Add(msg);
    }

    public List<Message> GetConversation(string convId)
    {
        return conversations.TryGetValue(convId, out var history) ? history : new List<Message>();
    }

    private void ProcessMessages()
    {
        while (mailbox.Count > 0)
        {
            Message msg = mailbox.Dequeue();

            switch (msg.messageType)
            {
                case Message_Types.Register:
                    if (!baseConocimiento.agentes.Contains(msg.sender) &&
                        msg.sender.tag != "Agente_camara")
                    {
                        baseConocimiento.agentes.Add(msg.sender);
                        // Debug.Log($"[{gameObject.name}] ha registrado a: {msg.sender.name} ({msg.sender.tag})");
                    }
                    break;

                case Message_Types.ReliquiaRobada:
                    if (msg.performative == Performative.Inform)
                    {
                        if (msg.reliquia != null && baseConocimiento.relicList.Contains(msg.reliquia))
                        {
                            baseConocimiento.relicList.Remove(msg.reliquia);
                            if (baseConocimiento.reliquiaCercana == msg.reliquia)
                                baseConocimiento.reliquiaCercana = null;
                        }
                        baseConocimiento.AlertaRobo = true;
                        Debug.Log($"<color=red>[{gameObject.name}]</color> Reliquia robada: {msg.reliquia?.name} — notificada por {msg.sender?.name}");
                    }
                    break;

                case Message_Types.PlayerSeen:
                    baseConocimiento.AlertaRobo = true;
                    baseConocimiento.LastPlayerSighting = baseConocimiento.PlayerPosition;

                    Debug.Log($"<color=yellow>[{gameObject.name}]</color> Alerta: jugador visto por {msg.sender?.name}");
                    if (msg.sender != gameObject) break;
                    // if (gameObject.tag == "Agente_camara") break; 
                    elpipas = true;
                    // Subasta
                    string convId = Message.NewConvID().ToString();
                    baseConocimiento.convIdSubasta = convId;
                    baseConocimiento.propuestasRecibidas.Clear();

                    foreach (GameObject agente in baseConocimiento.agentes)
                    {
                        Message cfp = Message.CFPChasePlayer(gameObject, msg.position.Value);
                        cfp.ConvID = convId;
                        cfp.receiver = agente;
                        Send(cfp);
                    }
                    break;

                case Message_Types.ChasePlayer:

                    if (msg.performative == Performative.CFP)
                    {
                        float distancia = Vector3.Distance(transform.position, msg.position.Value);

                        Message propuesta = Message.ProposeChase(
                            gameObject,
                            msg.sender,
                            distancia,
                            msg.ConvID
                        );

                        Send(propuesta);
                    }

                    if (msg.performative == Performative.Propose)
                    {
                        if (msg.ConvID != baseConocimiento.convIdSubasta)
                            break;
                        if (!elpipas) break;
                        baseConocimiento.propuestasRecibidas.Add(msg);

                        if (baseConocimiento.propuestasRecibidas.Count == baseConocimiento.agentes.Count)
                        {
                            Debug.Log($"[{gameObject.name}] todas las propuestas recibidas");

                            var ordenadas = baseConocimiento.propuestasRecibidas
                                .OrderBy(p => p.proposalValue)
                                .ToList();

                            // Chase
                            if (ordenadas.Count > 0)
                            {
                                Send(Message.AcceptProposal(
                                    gameObject,
                                    ordenadas[0].sender,
                                    Message_Types.ChasePlayer,
                                    msg.ConvID
                                ));
                            }

                            // Flanqueo
                            if (ordenadas.Count > 1)
                            {
                                Send(Message.AcceptProposal(
                                    gameObject,
                                    ordenadas[1].sender,
                                    Message_Types.FlanqueoPlayer,
                                    msg.ConvID
                                ));
                            }

                            // Cerco
                            for (int i = 2; i < ordenadas.Count; i++)
                            {
                                Send(Message.AcceptProposal(
                                    gameObject,
                                    ordenadas[i].sender,
                                    Message_Types.CercoPlayer,
                                    msg.ConvID
                                ));
                            }
                        }
                    }

                    if (msg.performative == Performative.AcceptProposal)
                    {
                        baseConocimiento.mision = msg.messageType;
                        Debug.Log($"[{gameObject.name}] acepta rol: {msg.messageType}");
                        SetMisionBehaviour(msg.messageType);
                    }

                    break;

                case Message_Types.FlanqueoPlayer:
                    if (msg.performative == Performative.AcceptProposal)
                    {
                        baseConocimiento.mision = msg.messageType;
                        Debug.Log($"[{gameObject.name}] acepta rol: {msg.messageType}");
                        SetMisionBehaviour(msg.messageType);
                    }
                    break;

                case Message_Types.CercoPlayer:
                    if (msg.performative == Performative.AcceptProposal)
                    {
                        baseConocimiento.mision = msg.messageType;
                        Debug.Log($"[{gameObject.name}] acepta rol: {msg.messageType}");
                        SetMisionBehaviour(msg.messageType);
                    }
                    break;
            }
        }
    }
    public void Send(Message msg)
    {
        if (msg.receiver == null) return;

        CerebroSubsumido destino = msg.receiver.GetComponent<CerebroSubsumido>();
        if (destino != null)
        {
            destino.ReceiveMessage(msg);
            LogMessage(msg);
        }
    }
    public void SetMisionBehaviour(Message_Types mision)
    {
        NPCBehaviour target = null;
        foreach (NPCBehaviour b in subsumido)
        {
            if ((mision == Message_Types.FlanqueoPlayer && b is Flanqueo) ||
                (mision == Message_Types.CercoPlayer    && b is Cerco)    ||
                (mision == Message_Types.ChasePlayer    && b is Chase))
            {
                target = b;
                break;
            }
        }
        if (target == null) return;

        if (behaviourQueue.Count > 0) behaviourQueue.Peek().terminate();
        behaviourQueue.Clear();
        behaviourQueue.Enqueue(target);
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