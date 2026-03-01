using UnityEngine;
using UnityEngine.AI;
using System;

/// <summary>
/// Se mueve entre una serie de puntos predeterminados
/// </summary>
public class Patrol : NPCBehaviour
{
    public Transform[] pathList;
    int currentDestination = 0;
    
    override public (Type, string, bool)[] neededSensorState()
    {
        return new (Type, string, bool)[]{(typeof(Vision), "Player", false)};
    }
    private void Awake()
    {
        cerebro = GetComponent<CerebroSubsumido>();
    }

    override public bool cumplePrecondiciones()
    {
        return cerebro.baseConocimiento.LastPlayerSighting == null;
    }

    /// <summary>
    /// Calcula el indice del proximo punto de patrulla
    /// </summary>
    /// <param name="currentDestination"></param>
    /// <returns>El indice</returns>
    int nextDestination(int currentDestination)
    {
        currentDestination++;
        if(currentDestination < pathList.Length){
            return currentDestination;
        }
        // Si se acaba el array, vuelve a empezar
        return 0;
    }

    
    override public void ejecutar()
    {   
        // Le asigna un destino si no tenia uno
        if(cerebro.navAgent.destination == null) {
         cerebro.navAgent.destination = pathList[currentDestination].position;
        }
        else
        {   
            // Se mueve hacia el punto hasta que queda a una distancia determinada y pasa al siguiente
            if (cerebro.navAgent.remainingDistance <= 2)
            {
                currentDestination = nextDestination(currentDestination);
                cerebro.navAgent.destination = pathList[currentDestination].position;
            }   
        }

    }
}
