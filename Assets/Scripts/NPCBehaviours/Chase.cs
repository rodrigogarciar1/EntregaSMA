using UnityEngine;
using System;
using UnityEngine.AI;

/// <summary>
/// Persigue al jugador si lo ve.
/// </summary>
public class Chase : NPCBehaviour
{
    private GameObject jugador;
    private void Start()
    {
        jugador = GameObject.FindWithTag("Player");
        cerebro = GetComponent<CerebroSubsumido>();
    }
    
    override public (Type, string, bool)[] neededSensorState()
    {
        return new (Type, string, bool)[]{(typeof(Vision), "Player", true)};
    }

    // override public bool cumplePrecondiciones()
    // {   
    //     return cerebro.baseConocimiento.PlayerPosition != null && cerebro.baseConocimiento.mision == Message_Types.ChasePlayer;
    // }
    override public bool cumplePrecondiciones()
    {
        return cerebro.baseConocimiento.mision == Message_Types.ChasePlayer
            && (cerebro.baseConocimiento.PlayerPosition != null
                || cerebro.baseConocimiento.isThereMissionTarget);
    }
    override public void ejecutar()
    {   Vector3 destino = cerebro.baseConocimiento.PlayerPosition != null
        ? cerebro.baseConocimiento.PlayerPosition.position
        : cerebro.baseConocimiento.MissionTarget;
        // Navega hacia la posicion actual del jugador
        cerebro.navAgent.destination = jugador.transform.position;
    }


}

