using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Define la intención del mensaje, siguiendo el estándar FIPA-ACL.
/// </summary>
public enum Performative
{
    Inform,         // Informar
    Request,        // See something say something
    CFP,            // Call For Proposals
    Propose,        // Respuesta a un CFP
    AcceptProposal, // Asignación de rol 
    Refuse          // Rechazo de una propuesta 
}

/// <summary>
/// Especifica el dominio 
/// </summary>
public enum Message_Types
{
    Register,           
    PlayerSeen,         
    PlayerHeard,        
    PlayerLost,         
    ReliquiaRobada,     
    ChasePlayer,        
    FlanqueoPlayer,     
    CercoPlayer,        
    InvestigatePosition,
    GuardRelic          
}

/// <summary>
/// Representa un mensaje FIPA intercambiado entre agentes del sistema.
/// </summary>
public class Message
{
    /// <summary>Intención del mensaje.</summary>
    public Performative performative;

    /// <summary>Agente que envía el mensaje.</summary>
    public GameObject sender;

    /// <summary>
    /// Agente destinatario.
    /// </summary>
    public GameObject receiver;

    /// <summary>Tipo del mensaje.</summary>
    public Message_Types messageType;

    /// <summary>
    /// Posición relevante asociada al mensaje (jugador o reliquia).
    /// </summary>
    public Vector3? position;

    /// <summary>
    /// Coste calculado por el agente.
    /// </summary>
    public float proposalValue;

    /// <summary>
    /// Identificador de conversación.
    /// </summary>
    public string ConvID;

    /// <summary> Instante de creación del mensaje </summary>
    public float timestamp;

    /// <summary>
    /// Referencia de la reliquia afectada.
    /// </summary>
    public GameObject reliquia;

    private static int nextConvID = 0;

    /// <summary>Genera un nuevo id.</summary>
    public static int NewConvID() => nextConvID++;

    /// <summary>
    /// Constructor base.
    /// </summary>
    public Message(Performative performative, GameObject sender, Message_Types messageType, GameObject receiver = null)
    {
        this.performative = performative;
        this.sender       = sender;
        this.receiver     = receiver;
        this.messageType  = messageType;
        this.timestamp    = Time.time;
        this.ConvID       = NewConvID().ToString();
    }


    /// <summary>Notifica a todos los agentes que el jugador ha sido visto en una posición concreta.</summary>
    public static Message InformPlayerSeen(GameObject sender, Vector3 playerPos)
    {
        Message msg = new Message(Performative.Inform, sender, Message_Types.PlayerSeen);
        msg.position = playerPos;
        return msg;
    }

    /// <summary>Notifica a todos los agentes que se ha detectado un ruido en una posición concreta.</summary>
    public static Message InformPlayerHeard(GameObject sender, Vector3 noisePos)
    {
        Message msg = new Message(Performative.Inform, sender, Message_Types.PlayerHeard);
        msg.position = noisePos;
        return msg;
    }

    /// <summary>Notifica que el agente ha perdido el rastro del jugador.</summary>
    public static Message InformPlayerLost(GameObject sender)
    {
        return new Message(Performative.Inform, sender, Message_Types.PlayerLost);
    }

    /// <summary>
    /// Notifica que una reliquia ha sido robada.
    /// </summary>
    public static Message InformReliquiaRobada(GameObject sender, GameObject reliquia)
    {
        Message msg = new Message(Performative.Inform, sender, Message_Types.ReliquiaRobada);
        msg.position = reliquia.transform.position;
        msg.reliquia = reliquia;
        return msg;
    }

    /// <summary>
    /// Inicia una subasta. 
    /// </summary>
    public static Message CFPChasePlayer(GameObject sender, Vector3 playerPos)
    {
        Message msg = new Message(Performative.CFP, sender, Message_Types.ChasePlayer);
        msg.position = playerPos;
        return msg;
    }

    /// <summary>
    /// Respuesta a un CFP. 
    /// </summary>
    public static Message ProposeChase(GameObject sender, GameObject initiator, float distance, string convId)
    {
        Message msg = new Message(Performative.Propose, sender, Message_Types.ChasePlayer, initiator);
        msg.proposalValue = distance;
        msg.ConvID        = convId;
        return msg;
    }

    /// <summary>
    /// Asigna un rol a un agente concreto tras evaluar todas las propuestas.
    /// </summary>
    public static Message AcceptProposal(GameObject sender, GameObject winner, Message_Types task, string convId)
    {
        Message msg = new Message(Performative.AcceptProposal, sender, task, winner);
        msg.ConvID = convId;
        return msg;
    }

    /// <summary>Rechaza una propuesta.</summary>
    public static Message RefuseMessage(GameObject sender, GameObject receiver, Message_Types task)
    {
        return new Message(Performative.Refuse, sender, task, receiver);
    }

    /// <summary>Representación del mensaje.</summary>
    public override string ToString()
    {
        return $"[{performative}] {messageType} | from: {sender?.name ?? "?"} → {receiver?.name ?? "broadcast"} | pos: {position} | val: {proposalValue} | conv: {ConvID}";
    }
}