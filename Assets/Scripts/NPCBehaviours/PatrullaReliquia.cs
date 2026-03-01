using UnityEngine;
using UnityEngine.AI;
using System;

/// <summary>
/// Patrulla alrededor de los objetos
/// </summary>

public class PatrullaReliquia : NPCBehaviour
{   
    /// <summary>
    /// Tiempo por checkeo
    /// </summary>
    public float checkingTime = 4f;
    /// <summary>
    /// Rango máximo de la patrulla
    /// </summary>
    public float maxPatrolRange = 4f;
    /// <summary>
    /// Velocidad de rotacion
    /// </summary>
    public float rotationSpeed = 25f;
    /// <summary>
    /// Tiempo que quede por mirar
    /// </summary>
    private float timeLeftToCheck;
    /// <summary>
    /// tiempo de rotacion
    /// </summary>
    private float timeRotating = 0f;
    /// <summary>
    /// Direccion de giro 
    /// </summary>
    private int rotationFactor = 1;
    /// <summary>
    /// Para saber si está patrullando 
    /// </summary>
    private bool isPatrolling = false;

    private void Awake()
    {
        cerebro = GetComponent<CerebroSubsumido>();
    }

    override public (Type, string, bool)[] neededSensorState()
    {
        return new (Type, string, bool)[]{(typeof(Vision), "Player", false),  (typeof(Vision), "Reliquia", true),
        (typeof(Hearing), "Noise", false), (typeof(Touch), "Player", false)};
    }

    override public bool cumplePrecondiciones()
    {
        // Se mantiene activo mientras la reliquia esté en su sitio y no haya alerta de robo
        return cerebro.baseConocimiento.reliquiaCercana != null && 
        (cerebro.baseConocimiento.LastPlayerSighting != null || cerebro.baseConocimiento.AlertaRobo);
    }

    void setRandomDestination()
    {
        Vector3 centro = cerebro.baseConocimiento.reliquiaCercana.transform.position;
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * maxPatrolRange + centro;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, maxPatrolRange, NavMesh.AllAreas))
            cerebro.navAgent.SetDestination(hit.position);
    }

    void checkInSpot()
    {
        if (timeLeftToCheck > 0f)
        {
            timeLeftToCheck -= Time.deltaTime;
            timeRotating -= Time.deltaTime;

            if (timeRotating <= 0f)
            {
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
            Debug.Log("<color=cyan>[PatrullaReliquia]</color> Vigilancia indefinida iniciada.");
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