using UnityEngine;

/// <summary>
    /// Clase abstracta de la cual se derivan todos los sensores 
/// </summary>
public abstract class Sensor : MonoBehaviour
{   
    /// <summary>
    /// 
    /// </summary>
    public CerebroSubsumido cerebro;

    /// <summary>
    /// Notifica al cerebro de que un objeto se ha percibido/dejado de percibir
    /// </summary>
    /// <param name="obj"> Objeto en cuestion </param>
    /// <param name="state">True si se ha percibido, False si se deja de percibir</param>
    public virtual void alertNewState(GameObject obj, bool state)
    {
        if (cerebro != null)
        {   
            cerebro.Notify(GetType(), obj, state);
            
        }
    }
}