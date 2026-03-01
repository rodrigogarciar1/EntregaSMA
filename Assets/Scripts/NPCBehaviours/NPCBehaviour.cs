using UnityEngine;
using System;

/// <summary>
/// Clase de comportamiento de agente abstracta
/// </summary>
public abstract class NPCBehaviour : MonoBehaviour
{   
    public CerebroSubsumido cerebro;

    /// <returns>Array de condiciones de sensores necesarias para que se ejecute. 
    /// Cada posicion del array se interpreta como un OR.</returns>
    public virtual (Type, string, bool)[] neededSensorState()
    {
        return new (Type, string, bool)[0];
    }

    public virtual bool cumplePrecondiciones()
    {
        return false;
    }

    /// <summary>
    /// Hace el comportamiento.
    /// </summary>
    public virtual void ejecutar()
    {   
    }

    /// <summary>
    /// Reinicia todas las posibles variables que puedan necesitar ser reiniciadas.
    /// </summary>
    public virtual void terminate(){}
}
