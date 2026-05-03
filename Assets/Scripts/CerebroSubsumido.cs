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
    /// Almacena los mensajes entrantes
    /// </summary>
    private Queue<Message> mailbox = new Queue<Message>();

    /// <summary>
    /// Organiza las conversaciones
    /// </summary>
    private Dictionary<string, List<Message>> conversations 
        = new Dictionary<string, List<Message>>();

    /// <summary>
    /// Diccionario donde se guardan las precondiciones de sensor de los comportamientos.
    /// Ordenado segun el orden de <see cref="subsimido"/></summary>
    private Dictionary<(Type, string, bool), List<NPCBehaviour>> dict 
        = new Dictionary<(Type, string, bool), List<NPCBehaviour>>();
    /// <summary>
    /// Candado para verificar que 1 solo agente inicia la subasta
    /// </summary>
    private bool itsme = false;

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
        /// Buscamos a todos los agentes
        CerebroSubsumido[] todos = FindObjectsOfType<CerebroSubsumido>();
        /// Indicamos quienes somos con el register
        Message hello = new Message(Performative.Inform, this.gameObject, Message_Types.Register);

        // Enviar a todos incluidos nosotros mismos para que aparezcamos en la lista, pero las cámaras no
        foreach (CerebroSubsumido receptor in todos)
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
        RunCurrentBehaviour();
        ProcessMessages();

    }

    /// <summary>
    /// Avisa al cerebro de que se ha producido el cambio mediante los sensores
    /// </summary>
    /// <param name="sensorType">Tipo de sensor que pericibio el cambio</param>
    /// <param name="obj">Objeto que ha cambiado</param>
    /// <param name="seeing">true si se ha percibido, false si se deja de percibir</param>
    public void Notify(Type sensorType, GameObject obj, bool seeing){   
        // Primero actualizamos los datos en la base de conocimiento sin tocar la cola
        string tag = obj.tag; // Se identifica el tipo de objeto
        checkRelevantStateChange(sensorType, obj, tag, seeing); // Se cambia la base de conocimiento si es necesario

        // FIX: Si el agente tiene una misión de combate, ignoramos otros sensores
        // Esto evita que "Vision: Reliquia" limpie la cola de la subasta
        if (baseConocimiento.mision == Message_Types.ChasePlayer ||
            baseConocimiento.mision == Message_Types.FlanqueoPlayer ||
            baseConocimiento.mision == Message_Types.CercoPlayer)
        {
            // Solo permitimos que la visión del jugador actualice la cola si es necesario
            if (!(sensorType == typeof(Vision) && obj.CompareTag("Player")))
            {
                return; 
            }
        }
        // fin fix
        // string tag = obj.tag; // Se identifica el tipo de objeto
        // checkRelevantStateChange(sensorType, obj, tag, seeing); // Se cambia la base de conocimiento si es necesario

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
                    baseConocimiento.MissionTarget = obj.transform.position;
                    baseConocimiento.isThereMissionTarget = true;
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

    /// <summary>
    /// Recibe un mensaje FIPA entrante y lo encola en el buzón del agente.
    /// </summary>
    /// <param name="msg">Mensaje recibido.</param>
    public void ReceiveMessage(Message msg)
    {
        mailbox.Enqueue(msg);
        LogMessage(msg);
    }

    /// <summary>
    /// Envía un mensaje FIPA a todos los agentes registrados en la base de conocimiento,
    /// excluye al propio emisor. Solo alcanza a agentes las cámaras no reciben
    /// mensajes.
    /// </summary>
    /// <param name="msg">Mensaje a difundir.</param>
    public void Broadcast(Message msg)
    {
        LogMessage(msg); 
        // buscar cada agente en escena 
        foreach (GameObject agenteObj in baseConocimiento.agentes) { 
            if (agenteObj != null && agenteObj != this.gameObject)
            {
                CerebroSubsumido destino = agenteObj.GetComponent<CerebroSubsumido>();
                if (destino != null) destino.ReceiveMessage(msg);
            }
        }
    }

    /// <summary>
    /// Registra un mensaje en el historial de conversaciones del agente,
    /// agrupándolo por ConvID. 
    /// </summary>
    /// <param name="msg">Mensaje a registrar.</param>
    private void LogMessage(Message msg)
    {
        if (!conversations.ContainsKey(msg.ConvID)) conversations[msg.ConvID] = new List<Message>();
        conversations[msg.ConvID].Add(msg);
    }

    /// <summary>
    /// Devuelve el historial de mensajes asociado a una conversación concreta.
    /// </summary>
    /// <param name="convId">Identificador de la conversación .</param>
    /// <returns>Lista de mensajes de la conversación</returns>
    public List<Message> GetConversation(string convId)
    {
        return conversations.TryGetValue(convId, out var history) ? history : new List<Message>();
    }
    /// <summary>
    /// Procesa todos los mensajes pendientes en el buzón del agente.
    /// Se ejecuta cada ciclo de Update:
    /// - INFORM: actualiza el estado del conocimiento (jugador visto, reliquia robada).
    /// - CFP: el agente calcula su coste y responde con PROPOSE.
    /// - PROPOSE: el iniciador recoge propuestas y asigna roles cuando las tiene todas.
    /// - ACCEPT_PROPOSAL: el agente acepta su rol y activa el comportamiento correspondiente.
    /// No ejecuta comportamientos directamente toda la lógica de ejecución
    /// queda delegada en la arquitectura de subsunción.
    /// </summary>
    private void ProcessMessages()
    {
        while (mailbox.Count > 0)
        {
            Message msg = mailbox.Dequeue();

            switch (msg.messageType)
            {   // Las cámaras no se registran como agentes para no participar en subastas.
                case Message_Types.Register:
                    if (!baseConocimiento.agentes.Contains(msg.sender) &&
                        msg.sender.tag != "Agente_camara")
                    {
                        baseConocimiento.agentes.Add(msg.sender);
                        // Debug.Log($"[{gameObject.name}] ha registrado a: {msg.sender.name} ({msg.sender.tag})");
                    }
                    break;
                // Elimina la reliquia de la lista conocida y activa la alerta de robo.
                // Si la reliquia era la asignada a este agente, la desvincula.
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
                // Solo el emisor original inicia el cfp.
                case Message_Types.PlayerSeen:
                    baseConocimiento.AlertaRobo = true;

                    if (msg.position.HasValue)
                    {
                    baseConocimiento.MissionTarget = msg.position.Value;
                    baseConocimiento.isThereMissionTarget = true;
                    }   

                    Debug.Log($"<color=yellow>[{gameObject.name}]</color> Alerta: jugador visto por {msg.sender?.name}");
                    if (msg.sender != gameObject) break;
                    // if (gameObject.tag == "Agente_camara") break; 
                    itsme = true;
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
                // El agente calcula su coste y responde con una propuesta
                case Message_Types.ChasePlayer:

                    if (msg.performative == Performative.CFP)
                    {   
                        // El agente calcula su coste y responde 
                        if (msg.position.HasValue)
                        {
                            baseConocimiento.MissionTarget = msg.position.Value;
                            baseConocimiento.isThereMissionTarget = true;
                        }

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
                        if (!itsme) break;
                        baseConocimiento.propuestasRecibidas.Add(msg);
                        // El iniciador recoge propuestas 
                        // Cuando tiene todas, ordena por coste y asigna roles
                        if (baseConocimiento.propuestasRecibidas.Count == baseConocimiento.agentes.Count)
                        {
                            Debug.Log($"[{gameObject.name}] todas las propuestas recibidas");

                            List<Message> ordenadas = baseConocimiento.propuestasRecibidas
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
                    // El agente acepta el rol Chase 
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

    /// <summary>
    /// Envía un mensaje FIPA a un agente concreto identificado por msg.receiver.
    /// A diferencia de Broadcast, este método es un envío dirigido: solo llega
    /// al destinatario especificado.
    /// </summary>
    /// <param name="msg">Mensaje a enviar. Debe tener receiver asignado.</param>
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
    
    /// <summary>
    /// Fuerza la activación inmediata del comportamiento de misión.
    /// Si no se encuentra el comportamiento correspondiente a la misión, no hace nada.
    /// </summary>
    /// <param name="mision">Tipo de misión asignada.</param>
    public void SetMisionBehaviour(Message_Types mision)
    {
        NPCBehaviour target = null;

        foreach (NPCBehaviour b in subsumido)
        {
            if ((mision == Message_Types.FlanqueoPlayer && b is Flanqueo) ||
                (mision == Message_Types.CercoPlayer && b is Cerco) ||
                (mision == Message_Types.ChasePlayer && b is Chase))
            {
                target = b;
                break;
            }
        }

        if (target == null) return;

        if (behaviourQueue.Count > 0)
            behaviourQueue.Peek().terminate();

        NPCBehaviour[] temp = behaviourQueue.ToArray();
        behaviourQueue.Clear();

        behaviourQueue.Enqueue(target);

        foreach (NPCBehaviour behaviour in temp)
        {
            behaviourQueue.Enqueue(behaviour);
        }

        Debug.Log($"<color=green>[{gameObject.name}]</color> Cola seteada con: {target.GetType().Name}, cumple: {target.cumplePrecondiciones()}");
    }

    /// <summary>
    /// Ejecuta el siguiente comportamiento en la <see cref="behaviourQueue"/>, 
    /// si no cumple las precondiciones se comprueba el siguiente.
    /// </summary>
    public void RunCurrentBehaviour()
    {
        if (behaviourQueue.Count > 0) {
            Debug.Log($"<color=white>[{gameObject.name}]</color> Ejecutando: {behaviourQueue.Peek().GetType().Name}, cumple: {behaviourQueue.Peek().cumplePrecondiciones()}");
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