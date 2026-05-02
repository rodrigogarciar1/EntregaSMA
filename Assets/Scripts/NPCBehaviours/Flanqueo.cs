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

    public override bool cumplePrecondiciones()
    {   
        Debug.Log($"{cerebro.baseConocimiento.mision}, Message_Types.FlanqueoPlayer");
        return cerebro.baseConocimiento.mision == Message_Types.FlanqueoPlayer
            && cerebro.baseConocimiento.PlayerPosition != null;
    }

    public override void ejecutar()
    {
        Transform jugador = cerebro.baseConocimiento.PlayerPosition;

        // Calcula el punto opuesto: desde el agente, pasando por el jugador, y más allá
        Vector3 dirAgente = (jugador.position - transform.position).normalized;
        targetPos = jugador.position + dirAgente * flanqueoDistance;

        cerebro.navAgent.SetDestination(targetPos);

        Debug.Log($"<color=magenta>[Flanqueo]</color> {gameObject.name} flanqueando hacia {targetPos}");
    }

    public override void terminate()
    {
        posicionCalculada = false;
    }
}