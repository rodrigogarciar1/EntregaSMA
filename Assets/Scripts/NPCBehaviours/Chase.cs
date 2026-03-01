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

    override public bool cumplePrecondiciones()
    {   
        return cerebro.baseConocimiento.PlayerPosition != null;
    }

    override public void ejecutar()
    {   
        // Navega hacia la posicion actual del jugador
        cerebro.navAgent.destination = jugador.transform.position;
    }


}

