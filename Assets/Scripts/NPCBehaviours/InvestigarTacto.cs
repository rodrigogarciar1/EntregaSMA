using UnityEngine;
using System;
using UnityEngine.AI;

/// <summary>
/// Se gira al detectar que lo tocan
/// </summary>
public class InvestigarTacto : NPCBehaviour
{   
    /// <summary>
    /// Tiempo en segundos que el agente investigará el contacto
    /// </summary>
    public float wait = 4f;

    /// <summary>
    /// Rotación inicial del agente antes de comenzar a investigar
    /// </summary>
    private Quaternion initialRotation;
    
    /// <summary>
    /// Ángulo acumulado 
    /// </summary>
    private float lookAngle = 0f;
    
    /// <summary>
    /// Dirección de la rotación
    /// </summary>
    private float lookDirection = 1f;
    
    /// <summary>
    /// Ángulo máximo que el agente mirará a cada lado durante la investigación
    /// </summary>
    public float maxLookAngle = 60f;
    
    /// <summary>
    /// Velocidad de rotación durante la búsqueda
    /// </summary>
    public float rotationSpeed = 20f; 
    
    /// <summary>
    /// Velocidad de giro 
    /// </summary>
    public float turnspeed = 25f;
    
    /// <summary>
    /// Para saber si sigue investigando
    /// </summary>
    private bool isInvestigating = false;
    
    /// <summary>
    /// Temporizador para controlar la duración de la investigación
    /// </summary>
    private float timer;

    private void Awake()
    {
        cerebro = GetComponent<CerebroSubsumido>();
    }

    public override (Type, string, bool)[] neededSensorState()
    {
        // Se activa cuando el sensor de oído (Touch) detecta al "Player"
        return new (Type, string, bool)[] { (typeof(Touch), "Player", true) };
    }

    public override bool cumplePrecondiciones()
    {   
        return cerebro.baseConocimiento.LastPlayerSighting != null;
    }

        public override void ejecutar()
    {
        if (!isInvestigating)
        {
            isInvestigating = true;
            timer = wait;
            initialRotation = transform.rotation * Quaternion.Euler(0, 180f, 0);
            cerebro.navAgent.isStopped = true;
            lookAngle = 0f;
        }

        // Se gira para investigar el tacto que haya notado
        transform.rotation = Quaternion.RotateTowards(transform.rotation, initialRotation, turnspeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, initialRotation) < 5f)
        {
            lookAngle += rotationSpeed * lookDirection * Time.deltaTime;
            if (Mathf.Abs(lookAngle) >= maxLookAngle) lookDirection *= -1f;
            
            transform.rotation = initialRotation * Quaternion.Euler(0, lookAngle, 0);
        }

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Debug.Log("<color=cyan>[InvestigarTacto]</color> Fin de investigación.");
            terminate();
        }
    }


    public override void terminate()
    {
        cerebro.navAgent.isStopped = false;
        isInvestigating = false;
    }
}