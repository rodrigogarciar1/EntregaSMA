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

    /// <summary>Tiempo en posición antes de pasar al siguiente comportamiento.</summary>
    public float tiempoEnPosicion = 8f;

    /// <summary>Tiempo restante en posición.</summary>
    private float timer;

    /// <summary>Controla que el comportamiento se inicialice una sola vez.</summary>
    private bool isFlanqueando = false;

    private Vector3 targetPos;

    private void Awake() => cerebro = GetComponent<CerebroSubsumido>();

    public override (Type, string, bool)[] neededSensorState()
    {
        return new (Type, string, bool)[] { (typeof(Vision), "Player", true) };
    }

    public override bool cumplePrecondiciones()
    {
        return cerebro.baseConocimiento.mision == Message_Types.FlanqueoPlayer
            && (cerebro.baseConocimiento.PlayerPosition != null
                || cerebro.baseConocimiento.isThereMissionTarget);
    }

    public override void ejecutar()
    {
        // Calcular posición de flanqueo 
        if (!isFlanqueando)
        {
            Vector3 posJugador = cerebro.baseConocimiento.PlayerPosition != null
                ? cerebro.baseConocimiento.PlayerPosition.position
                : cerebro.baseConocimiento.MissionTarget;

            Vector3 dirAgente = (posJugador - transform.position).normalized;
            targetPos = posJugador + dirAgente * flanqueoDistance;

            cerebro.navAgent.SetDestination(targetPos);
            timer = tiempoEnPosicion;
            isFlanqueando = true;

            Debug.Log($"<color=magenta>[Flanqueo]</color> {gameObject.name} flanqueando hacia {targetPos}");
        }

        // Una vez llegado, cuenta el tiempo y pasa al siguiente
        if (!cerebro.navAgent.pathPending && cerebro.navAgent.remainingDistance <= 1f)
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                Debug.Log($"<color=magenta>[Flanqueo]</color> {gameObject.name} tiempo en posición agotado.");
                cerebro.baseConocimiento.mision = null;
                cerebro.baseConocimiento.isThereMissionTarget = false;
                isFlanqueando = false;
                cerebro.RunNextBehaviour();
            }
        }
    }

    public override void terminate()
    {
        isFlanqueando = false;
    }
}