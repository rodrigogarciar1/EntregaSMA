using UnityEngine;
using UnityEngine.AI;
using System;

/// <summary>
///  Se mueve entre puntos generados aleatoriamente en el área donde se perdió 
/// de vista al jugador, girando sobre sí mismo durante un tiempo en cada uno.
/// </summary>
public class Registrar : NPCBehaviour
{   
    /// <summary>
    /// Usado para controlar que se ejecute bien el comportamiento
    /// </summary>
    private bool isSearching = false;

    /// <summary>
    /// Cantidad de puntos generados
    /// </summary>
    public int numberOfChecks = 3;

    /// <summary>
    /// Cantidad de puntos a los que se ha ido
    /// </summary>
    private int checksDone = 0;

    /// <summary>
    /// Tiempo a estar en cada punto
    /// </summary>
    public float checkingTime = 5f;
    /// <summary>
    /// Tiempo que queda en cada punto
    /// </summary>
    private float timeLeftToCheck;

    /// <summary>
    /// Tiempo rotando en una misma direccion
    /// </summary>
    private float timeRotating = 0f;

    /// <summary>
    /// Cantidad minima de tiempo en el que se rota en una misma direccion
    /// </summary>
    public float minRotationTime = 1f;

    /// <summary>
    /// Cantidad maxima de tiempo en la que se rota en una misma direccion
    /// </summary>
    public float maxRotationTime = 2.5f;

    /// <summary>
    /// Velocidad de la rotacion
    /// </summary>
    public float rotationSpeed = 25f;

    /// <summary>
    /// Sentido de la rotacion. 1 es horario y -1 antihorario
    /// </summary>
    int rotationFactor = 1;

    /// <summary>
    /// Rango maximo en el que se generan los puntos
    /// </summary>
    public float maxRegisterRange = 6f;

    private void Start()
    {
        cerebro = GetComponent<CerebroSubsumido>();
    }

    override public (Type, string, bool)[] neededSensorState()
    {
        return new (Type, string, bool)[]{(typeof(Vision), "Player", false)};
    }

    override public bool cumplePrecondiciones()
    {
        return cerebro.baseConocimiento.LastPlayerSighting != null && cerebro.baseConocimiento.PlayerPosition == null;
    }

    /// <summary>
    /// Escoge un punto alatorio dentro del rango se mueve hacia el
    /// </summary>
    void setRandomDestination()
    {   
        // Punto aleatorio dentro del rango
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * maxRegisterRange;
        randomDirection += transform.position;

        // Anula la y para que se ponga en el suelo
        randomDirection.y = 0;
        NavMeshHit hit;

        // Si se puede, se dirige hacia el punto generado
        if (NavMesh.SamplePosition(randomDirection, out hit, maxRegisterRange, NavMesh.AllAreas))
        {
            cerebro.navAgent.SetDestination(hit.position);
        }
    }

    /// <summary>
    /// Girar sobre si mismo de forma pseudoaleatoria
    /// </summary>
    void checkInSpot()
    {
        if (timeLeftToCheck > 0f)
        {   
            timeLeftToCheck -= Time.deltaTime;
            timeRotating -= Time.deltaTime;
            if (timeRotating <= 0f)
            {   
                // Se escoge un numero aleatorio dentro del rango
                timeRotating = UnityEngine.Random.Range(minRotationTime, maxRotationTime);

                // Se genera un numero aleatorio entre 0 y 1 para elegir el sentido de rotacion
                if (UnityEngine.Random.value < 0.5f)
                    rotationFactor = 1;
                else
                    rotationFactor = -1;
            }
            // Se rota
            transform.Rotate(Vector3.up, rotationFactor * rotationSpeed * Time.deltaTime);
        }
        // Cuando acaba de comprobar in situ
        else
        {   
            // Se cambia al siguiente
            checksDone++;
            timeLeftToCheck = checkingTime;
            setRandomDestination();
        }
    }
    override public void ejecutar()
    {   
        // Si no se estaba ejecutando ya
        if (!isSearching) {
        // Se incializa todo y se empieza a buscar
        checksDone = 0;
        timeLeftToCheck = checkingTime;
        isSearching = true;
        setRandomDestination();
        }

        // Si todavia tiene comprobaciones que hacer
        if (checksDone < numberOfChecks) {
            // Y ha llegado a su destino
            if (cerebro.navAgent.remainingDistance <= 2) {   
                checkInSpot();
            }
        } else {
            isSearching = false;
            cerebro.RunNextBehaviour();
        }
    }

     override public void terminate()
    {
        isSearching = false;
    }
}
