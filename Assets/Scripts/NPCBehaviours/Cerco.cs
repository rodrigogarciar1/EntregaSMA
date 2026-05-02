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

    /// <summary>Distancia mínima al destino para recalcular posición.</summary>
    public float recalcDistance = 1.5f;

    private Vector3 targetPos;
    private bool posicionCalculada = false;

    private void Awake() => cerebro = GetComponent<CerebroSubsumido>();

    public override (Type, string, bool)[] neededSensorState()
    {
        return new (Type, string, bool)[] { (typeof(Vision), "Player", true) };
    }

    public override bool cumplePrecondiciones()
    {
        return cerebro.baseConocimiento.mision == Message_Types.CercoPlayer
            && cerebro.baseConocimiento.PlayerPosition != null;
    }

    public override void ejecutar()
    {
        Transform jugador = cerebro.baseConocimiento.PlayerPosition;

        // Si no tiene posición o ya llegó, calcula una nueva aleatoria
        if (!posicionCalculada || cerebro.navAgent.remainingDistance <= recalcDistance)
        {
            Vector2 rng = UnityEngine.Random.insideUnitCircle.normalized;
            targetPos = jugador.position + new Vector3(rng.x, 0, rng.y) * cercoRadius;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, cercoRadius, NavMesh.AllAreas))
            {
                targetPos = hit.position;
            }

            cerebro.navAgent.SetDestination(targetPos);
            posicionCalculada = true;

            Debug.Log($"<color=cyan>[Cerco]</color> {gameObject.name} posición de cerco: {targetPos}");
        }
    }

    public override void terminate()
    {
        posicionCalculada = false;
    }
}