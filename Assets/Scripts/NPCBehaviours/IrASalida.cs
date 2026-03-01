using UnityEngine;
using UnityEngine.AI;
using System;
using System.Linq;
/// <summary>
/// Cuando se han robado las reliquias va a la Salida
/// </summary>
public class IrASalida : NPCBehaviour
{
    /// <summary>
    /// Tiempo en segundos 
    /// </summary>
    public float checkingTime = 4f;

    /// <summary>
    /// Distancia máxima del radio de patrulla 
    /// </summary>
    public float maxPatrolRange = 5f;

    /// <summary>
    /// Distancia mínima del radio de patrulla 
    /// </summary>
    public float minPatrolRange = 2f;

    /// <summary>
    /// Velocidad de rotación 
    /// </summary>
    public float rotationSpeed = 25f;

    /// <summary>
    /// Tiempo restante para finalizar checkeo
    /// </summary>
    private float timeLeftToCheck;

    /// <summary>
    /// Tiempo restante antes de cambiar la dirección de rotación
    /// </summary>
    private float timeRotating = 0f;

    /// <summary>
    /// Dirección de rotación 
    /// </summary>
    private int rotationFactor = 1;

    /// <summary>
    /// Para saber si sigue patrullando
    /// </summary>
    private bool isPatrolling = false;

    /// <summary>
    /// Último destino 
    /// </summary>
    private Vector3 lastDestination;

    private void Awake()
    {
        cerebro = GetComponent<CerebroSubsumido>();
    }

    override public (Type, string, bool)[] neededSensorState()
    {
        return new (Type, string, bool)[]{(typeof(Vision), "Player", false),  (typeof(Vision), "Reliquia", true)};
    }

    public override bool cumplePrecondiciones()
    {
        // Para ir a la salida debe haberse perdido de vista al jugador en algún momento
        return !cerebro.baseConocimiento.relicList.Any();
    }

    /// <summary>
    /// Escoge un punto aleatorio dentro del rango se mueve hacia el
    /// </summary>
    void setRandomDestination()
    {
        Vector3 centro = cerebro.baseConocimiento.Salida.position;
        NavMeshHit hit;

        // Generar dirección aleatoria en el círculo 
        Vector2 rng = UnityEngine.Random.insideUnitCircle.normalized;
        float distancia = UnityEngine.Random.Range(minPatrolRange, maxPatrolRange);
        Vector3 dir = new Vector3(rng.x, 0, rng.y);
        Vector3 randomDirection = dir * distancia + centro;
        /// Actualiza la dirección si fue un hit dentro del rango
        if (NavMesh.SamplePosition(randomDirection, out hit, maxPatrolRange, NavMesh.AllAreas))
        {
            lastDestination = hit.position;
            cerebro.navAgent.SetDestination(hit.position);
        }
    }

    /// <summary>
    /// Gira de manera aleatoria
    /// </summary>
    void checkInSpot()
    {
        if (timeLeftToCheck > 0f)
        {
            timeLeftToCheck -= Time.deltaTime;
            timeRotating -= Time.deltaTime;

            if (timeRotating <= 0f)
            {  /// Si sale más de 0.5 gira a la izquierda si no a la derecha
                timeRotating = UnityEngine.Random.Range(1f, 2.5f);
                rotationFactor = UnityEngine.Random.value < 0.5f ? 1 : -1;
            }

            transform.Rotate(Vector3.up, rotationFactor * rotationSpeed * Time.deltaTime);
        }
        else
        {
            // Al terminar el tiempo, simplemente busca otro punto y reinicia el timer
            timeLeftToCheck = checkingTime;
            setRandomDestination();
        }
    }

    override public void ejecutar()
    {
        if (!isPatrolling)
        {
            timeLeftToCheck = checkingTime;
            setRandomDestination();
            isPatrolling = true;
            Debug.Log("<color=cyan>[IrASalida]</color> Vigilancia indefinida iniciada.");
        }

        if (cerebro.navAgent.remainingDistance <= 1f)
        {
            checkInSpot();
        }
    }

    override public void terminate()
    {
        isPatrolling = false;
        cerebro.navAgent.isStopped = false;
    }
}