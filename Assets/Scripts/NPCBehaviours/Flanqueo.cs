using UnityEngine;
using System;

/// <summary>
/// Se mueve al lado opuesto del jugador respecto al agente Chase,
/// para cortar su posible huida.
/// </summary>
public class Flanqueo : NPCBehaviour
{
    /// <summary>Distancia a la que se posiciona detrás del jugador.</summary>
    public float flanqueoDistance = 4f;

    private Vector3 targetPos;
    private bool posicionCalculada = false;

    private void Awake() => cerebro = GetComponent<CerebroSubsumido>();

    public override (Type, string, bool)[] neededSensorState()
    {
        // Se activa igual que Chase: cuando se ve al jugador
        return new (Type, string, bool)[] { (typeof(Vision), "Player", true) };
    }

    // public override bool cumplePrecondiciones()
    //     {
    //         return cerebro.baseConocimiento.mision == Message_Types.FlanqueoPlayer
    //             && cerebro.baseConocimiento.PlayerPosition != null;
    //     }

    public override bool cumplePrecondiciones()
    {
        return cerebro.baseConocimiento.mision == Message_Types.FlanqueoPlayer
            && (cerebro.baseConocimiento.PlayerPosition != null
                || cerebro.baseConocimiento.isThereMissionTarget);
    }
    // public override void ejecutar()
    // {
    //     // Usamos PlayerPosition si lo vemos, o LastPlayerSighting si no
    //     Transform jugador = cerebro.baseConocimiento.PlayerPosition ?? cerebro.baseConocimiento.LastPlayerSighting;
        
    //     if (jugador == null) return;

    //     Vector3 dirAgente = (jugador.position - transform.position).normalized;
    //     targetPos = jugador.position + dirAgente * flanqueoDistance;

    //     cerebro.navAgent.SetDestination(targetPos);
    //     Debug.Log($"<color=magenta>[Flanqueo]</color> {gameObject.name} flanqueando hacia {targetPos}");
    // }
    
    public override void ejecutar()
    {
        Vector3 posJugador = cerebro.baseConocimiento.PlayerPosition != null
            ? cerebro.baseConocimiento.PlayerPosition.position
            : cerebro.baseConocimiento.MissionTarget;

        Vector3 dirAgente = (posJugador - transform.position).normalized;
        targetPos = posJugador + dirAgente * flanqueoDistance;
        cerebro.navAgent.SetDestination(targetPos);
        Debug.Log($"<color=magenta>[Flanqueo]</color> {gameObject.name} flanqueando hacia {targetPos}");
    }
    public override void terminate()
    {
        posicionCalculada = false;
    }
}