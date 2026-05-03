using UnityEngine;
using System;
using UnityEngine.AI;

/// <summary>
/// Se posiciona en un punto aleatorio alrededor del jugador
/// para rodearlo junto a otros agentes.
/// </summary>
public class Cerco : NPCBehaviour
{
    /// <summary>Radio del cerco alrededor del jugador.</summary>
    public float cercoRadius = 5f;

    /// <summary>Distancia mínima al destino para considerar que ha llegado.</summary>
    public float recalcDistance = 1.5f;

    /// <summary>Tiempo en posición antes de pasar al siguiente comportamiento.</summary>
    public float tiempoEnPosicion = 8f;

    /// <summary>Tiempo restante en posición.</summary>
    private float timer;

    /// <summary>Controla que el comportamiento se inicialice una sola vez.</summary>
    private bool isCercando = false;

    private void Awake() => cerebro = GetComponent<CerebroSubsumido>();

    public override (Type, string, bool)[] neededSensorState()
    {
        return new (Type, string, bool)[] { (typeof(Vision), "Player", true) };
    }

    public override bool cumplePrecondiciones()
    {
        return cerebro.baseConocimiento.mision == Message_Types.CercoPlayer
            && (cerebro.baseConocimiento.PlayerPosition != null
                || cerebro.baseConocimiento.isThereMissionTarget);
    }

    public override void ejecutar()
    {
        // Inicialización: calcular posición de cerco y arrancar timer
        if (!isCercando)
        {
            Vector3 posJugador = cerebro.baseConocimiento.PlayerPosition != null
                ? cerebro.baseConocimiento.PlayerPosition.position
                : cerebro.baseConocimiento.MissionTarget;

            Vector2 rng = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 targetPos = posJugador + new Vector3(rng.x, 0, rng.y) * cercoRadius;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, cercoRadius, NavMesh.AllAreas))
                targetPos = hit.position;

            cerebro.navAgent.SetDestination(targetPos);
            timer = tiempoEnPosicion;
            isCercando = true;

            Debug.Log($"<color=cyan>[Cerco]</color> {gameObject.name} posición de cerco: {targetPos}");
        }

        // Una vez llegado, cuenta el tiempo y pasa al siguiente
        if (!cerebro.navAgent.pathPending && cerebro.navAgent.remainingDistance <= recalcDistance)
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                Debug.Log($"<color=cyan>[Cerco]</color> {gameObject.name} tiempo en posición agotado.");
                isCercando = false;
                cerebro.RunNextBehaviour();
            }
        }
    }

    public override void terminate()
    {
        isCercando = false;
    }
}