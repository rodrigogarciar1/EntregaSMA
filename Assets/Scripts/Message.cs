using UnityEngine;
using System;
using System.Collections.Generic;


public enum Performative
{
    Inform,          // See something say something
    Request,         
    CFP,             // Subasta
    Propose,         
    AcceptProposal,  
    Refuse           
}


public enum Message_Types
{   Register,
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


public class Message
{
    public Performative performative;
    public GameObject sender;
    public GameObject receiver;
    public Message_Types messageType;
    public Vector3? position;
    public float proposalValue;
    public string ConvID;
    public float timestamp;

    public GameObject reliquia;
    private static int nextConvID = 0;
    public static int NewConvID() => nextConvID++;

    public Message(Performative performative, GameObject sender, Message_Types messageType, GameObject receiver = null)
    {   
        this.performative = performative;
        this.sender       = sender;
        this.receiver     = receiver;
        this.messageType  = messageType;
        this.timestamp    = Time.time;
        this.ConvID       = NewConvID().ToString();;
    }


    public static Message InformPlayerSeen(GameObject sender, Vector3 playerPos)
    {
        Message msg = new Message(Performative.Inform, sender, Message_Types.PlayerSeen);
        msg.position = playerPos;
        return msg;
    }

    public static Message InformPlayerHeard(GameObject sender, Vector3 noisePos)
    {
        Message msg = new Message(Performative.Inform, sender, Message_Types.PlayerHeard);
        msg.position = noisePos;
        return msg;
    }

    public static Message InformPlayerLost(GameObject sender)
    {
        return new Message(Performative.Inform, sender, Message_Types.PlayerLost);
    }

    public static Message InformReliquiaRobada(GameObject sender, GameObject reliquia)
    {
        Message msg = new Message(Performative.Inform, sender, Message_Types.ReliquiaRobada);
        msg.position = reliquia.transform.position;
        msg.reliquia = reliquia; // informar de que reliquia se ha robado
        return msg;
    }
    // no funciona es una idea
    public static Message CFPChasePlayer(GameObject sender, Vector3 playerPos)
    {
        Message msg = new Message(Performative.CFP, sender, Message_Types.ChasePlayer);
        msg.position = playerPos;
        return msg;
    }

    public static Message ProposeChase(GameObject sender, GameObject initiator, float distance, string convId)
    {
        Message msg = new Message(Performative.Propose, sender, Message_Types.ChasePlayer, initiator);
        msg.proposalValue = distance;
        msg.ConvID        = convId;
        return msg;
    }

    public static Message AcceptProposal(GameObject sender, GameObject winner, Message_Types task, string convId)
    {
        Message msg = new Message(Performative.AcceptProposal, sender, task, winner);
        msg.ConvID = convId;
        return msg;
    }

    public static Message RefuseMessage(GameObject sender, GameObject receiver, Message_Types task)
    {
        return new Message(Performative.Refuse, sender, task, receiver);
    }


    public override string ToString()
    {
        return $"[{performative}] {messageType} | from: {sender?.name ?? "?"} → {receiver?.name ?? "broadcast"} | pos: {position} | val: {proposalValue} | conv: {ConvID}";
    }
}