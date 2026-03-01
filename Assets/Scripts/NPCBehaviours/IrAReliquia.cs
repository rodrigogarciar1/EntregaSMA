using UnityEngine;
using System;
/// <summary>
/// Se mueve a la reliquia que tenga que custodiar
/// </summary>
public class IrAReliquia : NPCBehaviour
{   private bool isMoving = false;
    private void Awake() => cerebro = GetComponent<CerebroSubsumido>();

    public override (Type, string, bool)[] neededSensorState()
    {
        return new (Type, string, bool)[] { (typeof(Vision), "Player", false), (typeof(Vision), "Reliquia", true)};
    }
    public override bool cumplePrecondiciones()
    {
        return cerebro.baseConocimiento.LastPlayerSighting !=null || cerebro.baseConocimiento.AlertaRobo;
    }


    public override void ejecutar()
    {
        // Verificar primero si la reliquia actual es válida
        if (cerebro.baseConocimiento.reliquiaCercana == null || 
            !cerebro.baseConocimiento.reliquiaCercana.activeInHierarchy)
        {
            // Buscar una nueva reliquia cercana
            GameObject siguiente = BuscarMasCercana();
            if (siguiente != null)
            {
                cerebro.baseConocimiento.reliquiaCercana = siguiente;
                cerebro.baseConocimiento.AlertaRobo = true;
                cerebro.baseConocimiento.reliquiaComprobada = false;
                cerebro.navAgent.SetDestination(siguiente.transform.position);
                Debug.Log($"<color=cyan>[IrAReliquia]</color> Yendo a nueva reliquia: {siguiente.name}");
                return;
            }
            else
            {
                // No hay reliquias disponibles, pasar al siguiente comportamiento
                Debug.Log("<color=orange>[IrAReliquia]</color> No hay reliquias disponibles.");
                cerebro.RunNextBehaviour();
                return;
            }
        }

        // Si llegamos aquí, reliquiaCercana es válida
        if (!cerebro.baseConocimiento.reliquiaComprobada)
        {
            cerebro.navAgent.SetDestination(cerebro.baseConocimiento.reliquiaCercana.transform.position);
            Debug.Log($"<color=cyan>[IrAReliquia]</color> Yendo a proteger: {cerebro.baseConocimiento.reliquiaCercana.name}");
            return;
        }

        // Si ya está comprobada, pasar al siguiente comportamiento
        cerebro.RunNextBehaviour();
    }

    private GameObject BuscarMasCercana()
{
    GameObject masCercana = null;
    float menorDist = float.MaxValue;

    if (cerebro.baseConocimiento.relicList == null || cerebro.baseConocimiento.relicList.Count == 0)
    {
        return null;
    }

    for (int i = 0; i < cerebro.baseConocimiento.relicList.Count; i++)
    {
        GameObject relic = cerebro.baseConocimiento.relicList[i];
        
        // Saltar reliquias nulas o destruidas
        if (relic == null || !relic.activeInHierarchy) continue;

        float d = Vector3.Distance(transform.position, relic.transform.position);
        if (d < menorDist)
        {
            menorDist = d;
            masCercana = relic;
        }
    }

    return masCercana;
}
    public override void terminate()
    {
        isMoving = false;
    }
}