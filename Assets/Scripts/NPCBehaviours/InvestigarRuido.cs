using UnityEngine;
using System;
using UnityEngine.AI;
using System.Reflection;

/// <summary>
/// Se mueve entre una serie de puntos predeterminados
/// </summary>
public class InvestigarRuido : NPCBehaviour
{   
    /// <summary>
    /// Tiempo en segundos que el agente investigará 
    /// </summary>
    public float wait = 3f;

    /// <summary>
    /// Velocidad de rotación 
    /// </summary>
    public float rotationSpeed = 90f; 
    
    /// <summary>
    /// Velocidad de giro
    /// </summary>
    public float turnspeed = 120f;
    
    /// <summary>
    /// Para saber si sigue investigando
    /// </summary>
    private bool isInvestigating = false;
    
    /// <summary>
    /// Temporizador 
    /// </summary>
    private float timer;

    private void Awake()
    {
        cerebro = GetComponent<CerebroSubsumido>();
    }

    public override (Type, string, bool)[] neededSensorState()
    {
        // Se activa cuando el sensor de oído (Hearing) detecta al "Player"
        return new (Type, string, bool)[] { (typeof(Hearing), "Noise", true) };
    }

    public override bool cumplePrecondiciones()
    {   
        return cerebro.baseConocimiento.PlayerHeard == true &&
           cerebro.baseConocimiento.LastPlayerSighting != null;
    }

    public override void ejecutar()
    {
        if (cerebro.baseConocimiento.LastPlayerSighting == null)
        {
            Debug.LogWarning("<color=red>[InvestigarRuido]</color> No hay posición de ruido. Cancelando.");
            terminate();
            return;
        }

        Transform target = cerebro.baseConocimiento.LastPlayerSighting;

        if (!isInvestigating)
        {
            isInvestigating = true;
            cerebro.baseConocimiento.investigatingNoise = true; // Candado
            timer = wait;
            cerebro.navAgent.isStopped = false;
            cerebro.navAgent.SetDestination(target.position);

            Debug.Log("<color=cyan>[InvestigarRuido]</color> Yendo a investigar ruido en " + target.position);
        }

        Vector3 dir = target.position - transform.position;
        dir.y = 0;

        /// Cuando se encuentra en el punto del sonido rota para mover la vista de un lado a otro
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnspeed * Time.deltaTime);
        }

        if (!cerebro.navAgent.pathPending && cerebro.navAgent.remainingDistance < 1f)
        {
            cerebro.navAgent.isStopped = true;

            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);

            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                Debug.Log("<color=cyan>[InvestigarRuido]</color> Investigación completa.");
                terminate();
            }
        }
    }



    public override void terminate()
    {   cerebro.navAgent.isStopped = false;
        isInvestigating = false;
        cerebro.navAgent.isStopped = false;
        cerebro.baseConocimiento.PlayerHeard = false;
        cerebro.baseConocimiento.investigatingNoise = false; // Candado

        cerebro.baseConocimiento.PlayerPosition = null;
        Debug.Log("<color=cyan>[InvestigarRuido]</color> Terminado. Volviendo a Patrulla.");
    
    
    // IMPORTANTE: Vaciamos la cola para que el Cerebro ejecute el Fallback (Patrulla)
        cerebro.behaviourQueue.Clear();

    }
}