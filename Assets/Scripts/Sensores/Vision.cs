using UnityEngine;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Imita la vision humana usando colisiones con una cuña redondeada
/// </summary>
public class Vision : Sensor
{
    ///  <summary> Longitud del rango de vision desde el centro del agente </summary>
    public float distancia = 10;

    ///  <summary> Amplitud en grados desde el centro hasta uno de los limites laterales</summary>
    public float angulo = 30;
    ///  <summary> Altura desde la base de la cuña</summary>
    public float altura = 1.0f;
    ///  <summary> Cantidad de unidades que se desplaza verticalmente hacia abajo 
    /// la base de la cuña respecto al centro</summary>
    public float alturaBase = 0.5f;

    ///  <summary> Color del gizmo de la mesh</summary>
    public Color meshColor;

    ///  <summary> La cuña </summary>
    Mesh mesh;

    ///  <summary> Matrial para renderizar la mesh en tiempo de ejecución </summary>
    public Material material;

    ///  <summary> Veces por frame que se comprueba lo que se encuentra dentro de la mesh </summary>
    public int scanFrequecy = 30;

    ///  <summary> Capas que puede percibir la vision </summary>
    public LayerMask layers;

    ///  <summary> Capas que bloquean la vision </summary>
    public LayerMask oclusionLayers;

    ///  <summary> Lista de objetos que se encuentran actualmente dentro de la vision </summary>
    public List<GameObject> CurrentObjects = new List<GameObject>();

    ///  <summary> Lista de objetos que se encuentraban dentro de la vision la ultima vez que se comprobo </summary>
    public List<GameObject> PastObjects = new List<GameObject>();

    ///  <summary> Array de colliders que estan dentro de la vision </summary>
    Collider[] colliders = new Collider[50];
    ///  <summary> Cantidad de objetos percibidos actualmente </summary>
    int count;

    ///  <summary> Cantidad de tiempo que tiene que pasar entre scans </summary>
    float scanInterval;
    ///  <summary> Cantidad de tiempo que falta para el proximo scan </summary>
    float scanTimer;

    ///  <summary> Cantidad de triangulos que componen la curvatura de la cuña </summary>
    [Range(2, 50)] public int segments = 10;

    private void Awake()
    {
        cerebro = GetComponent<CerebroSubsumido>();
    }

    private void Start()
    {   
        scanInterval = 1.0f / scanFrequecy;
    } 

    void Update()
    {
        UpdateWedgeMesh(); /// Se calcula la Mesh
        DrawVisionCone();  /// Se dibuja

        /// Comprobador de si se tiene que hacer el scan este frame
        scanTimer -= Time.deltaTime;
        if (scanTimer < 0)
        {
            scanTimer += scanInterval;
            Scan();
        }
    }

    /// <summary>
    /// Renderiza la cuña en tiempo de ejecucion con el material proporcionado
    /// </summary>
    private void DrawVisionCone()
    {
        if (mesh == null) return;
        RenderParams rparams = new RenderParams(material);
        Graphics.RenderMesh(rparams, mesh, 0, transform.localToWorldMatrix);
    }

    /// <summary>
    /// Detecta todos los objetos que se encuentran en la cuña en este frame, asi como los que ya no estan
    /// respecto al inmediatamente anterior.
    /// </summary>
    private void Scan()
    {
        /// Comprueba todos los objetos que se encuentran el la capa seleccionada dentro de una esfera al rededor del agente
        /// con radio <see cref="distancia"/>
        count = Physics.OverlapSphereNonAlloc(transform.position, distancia, colliders, layers, QueryTriggerInteraction.Collide);
        PastObjects = new List<GameObject>(CurrentObjects); // Se almacenan todos los objetos del Scan anterior en una lista
        CurrentObjects.Clear();
        /// Actualizacion de la lista de objetos acutales
        for (int i = 0; i< count; ++i)
        {
            GameObject obj = colliders[i].gameObject;

            if (isDetected(obj))
            {
                CurrentObjects.Add(obj);
            }
        }

        /// Los objetos que acaban de entrar a la vision
        List<GameObject> additions = CurrentObjects.Except(PastObjects).ToList();
        /// Los objetos que acaban de salir de la vision 
        List<GameObject> subtractions = PastObjects.Except(CurrentObjects).ToList();

        /// Se llama a alertNewState para que avise al cerebro de los cambios pertinentes
        foreach (GameObject obj in additions)
        { 
            alertNewState(obj, true);
        }
        foreach (GameObject obj in subtractions)
        {
            alertNewState(obj, false);
            
        }
    }
    /// <summary>
    /// Comprueba si un objeto se encuentra dentro de lo que deberia ver el agente
    /// </summary>
    /// <param name="obj">GameObject a comprobar</param>
    /// <returns>Booleaneo</returns>
    public bool isDetected(GameObject obj)
    {   
        // Se toma el origen como el centro del agente
        Vector3 origin = transform.position;
        /// Se resta al eje y el desplazamiento que se haya hecho con <see cref="alturaBase"/>
        origin.y -= alturaBase;
        // Se tima el centro del objeto objetivo como destino
        Vector3 dest = obj.transform.position;
        /// Se calucla el vector que une ambos centros
        Vector3 direction = dest-origin;

        // Si el objeto esta 
        if (direction.y < 0 || direction.y > altura)
        {
            return false;
        }

        // Si la magitud del vector es mayor que la distancia maxima de vision no se ve
        if (direction.magnitude > distancia)
        {
            return false;
        }

        // Eliminamos la componente y para calcular el angulo de manera bidimensional
        direction.y = 0;
        float deltaAngle = Vector3.Angle(direction, transform.forward);
        // Si el angulo que forman es mayor que el de vision, no lo ve
        if (deltaAngle > angulo)
        {
            return false;
        }

        origin.y += altura/2;
        dest.y = origin.y;
        // Si la linea que une ambos pasa por un objeto en la capa de oclusion, no lo ve
        if (Physics.Linecast(origin, dest, oclusionLayers))
        {
            return false;
        }
        return true;
    }

    private void OnValidate()
    {
        mesh = CreateWedgeMesh();
    }

    /// <summary>
    /// Crea una cuña con los parametros especificados previamente
    /// </summary>
    /// <returns> La cuña </returns>
    Mesh CreateWedgeMesh()
    {
        Mesh mesh = new Mesh();

        // Calculo de la cantidad de triangulos y vertices que componen la cuña
        int numTriangulos = (segments * 4) +2 +2;
        int numVertices = numTriangulos * 3;

        Vector3[] vertices = new Vector3[numVertices];
        int[] triangulos = new int[numVertices];

        // Calculo de los puntos clave de la forma general de la cuña
        Vector3 bottomCenter = Vector3.zero - Vector3.up * alturaBase;
        Vector3 bottomLeft = Quaternion.Euler(0, -angulo, 0) * Vector3.forward * distancia - Vector3.up * alturaBase;
        Vector3 bottomRight = Quaternion.Euler(0, angulo, 0) * Vector3.forward * distancia - Vector3.up * alturaBase;

        Vector3 topCenter = bottomCenter + Vector3.up * altura;
        Vector3 topLeft = bottomLeft + Vector3.up * altura;
        Vector3 topRight = bottomRight + Vector3.up * altura;

        int vert = 0;
        // Formulacion de los laterales

        //izquierda
        vertices[vert++] = bottomCenter;
        vertices[vert++] = bottomLeft;
        vertices[vert++] = topLeft;

        vertices[vert++] = topLeft;
        vertices[vert++] = topCenter;
        vertices[vert++] = bottomCenter;

        //derecha
        vertices[vert++] = bottomCenter;
        vertices[vert++] = topCenter;
        vertices[vert++] = topRight;

        vertices[vert++] = topRight;
        vertices[vert++] = bottomRight;
        vertices[vert++] = bottomCenter;


        float currentAngle= -angulo;
        float deltaAngle = (angulo * 2) / segments;
        // Este bucle crea la curvatura de la cuña
        for(int i = 0; i<segments; ++i)
        {   
            bottomLeft = Quaternion.Euler(0, currentAngle, 0) * Vector3.forward * distancia - Vector3.up * alturaBase;
            bottomRight = Quaternion.Euler(0, currentAngle + deltaAngle, 0) * Vector3.forward * distancia - Vector3.up * alturaBase;

            topLeft = bottomLeft + Vector3.up * altura;
            topRight = bottomRight + Vector3.up * altura;

            //frente
            vertices[vert++] = bottomLeft;
            vertices[vert++] = bottomRight;
            vertices[vert++] = topRight;

            vertices[vert++] = topRight;
            vertices[vert++] = topLeft;
            vertices[vert++] = bottomLeft;

            //arriba
            vertices[vert++] = topCenter;
            vertices[vert++] = topLeft;
            vertices[vert++] = topRight;

            //abajo
            vertices[vert++] = bottomCenter;
            vertices[vert++] = bottomRight;
            vertices[vert++] = bottomLeft;

            currentAngle += deltaAngle;
        }
        

        for(int i = 0; i<numVertices; ++i)
        {
            triangulos[i] = i;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangulos;
        mesh.RecalculateNormals();

        return mesh;
    }
    
    /// <summary>
    /// Recalcula la mesh para que no atraviese los objetos marcados con la capa
    /// de oclusion en runtime
    /// </summary>
    private void UpdateWedgeMesh()
    {
        int numTriangles = (segments * 4) + 4;
        int numVertices = numTriangles * 3;

        Vector3[] vertices = new Vector3[numVertices];
        int[] triangles = new int[numVertices];

        Vector3 origin = transform.position;
        Vector3 bottomCenter = Vector3.zero - Vector3.up * alturaBase;
        Vector3 topCenter = bottomCenter + Vector3.up * altura;

        float currentAngle = -angulo;
        float deltaAngle = (angulo * 2) / segments;
        int vert = 0;

        for (int i = 0; i < segments; i++)
        {
            float angleA = currentAngle;
            float angleB = currentAngle + deltaAngle;

            // Direcciones relativas al frente del objeto
            Vector3 dirA = Quaternion.Euler(0, angleA, 0) * Vector3.forward;
            Vector3 dirB = Quaternion.Euler(0, angleB, 0) * Vector3.forward;
            float distA = GetDistance(transform.TransformDirection(dirA));
            float distB = GetDistance(transform.TransformDirection(dirB));

            Vector3 bL = bottomCenter + dirA * distA;
            Vector3 bR = bottomCenter + dirB * distB;
            Vector3 tL = bL + Vector3.up * altura;
            Vector3 tR = bR + Vector3.up * altura;

            // frente
            vertices[vert++] = bL; vertices[vert++] = bR; vertices[vert++] = tR;
            vertices[vert++] = tR; vertices[vert++] = tL; vertices[vert++] = bL;

            // arriba
            vertices[vert++] = topCenter; vertices[vert++] = tL; vertices[vert++] = tR;

            // abajo
            vertices[vert++] = bottomCenter; vertices[vert++] = bR; vertices[vert++] = bL;

            if (i == 0) { // izquierda
                vertices[vert++] = bottomCenter; vertices[vert++] = bL; vertices[vert++] = tL;
                vertices[vert++] = tL; vertices[vert++] = topCenter; vertices[vert++] = bottomCenter;
            }
            if (i == segments - 1) { // derecha
                vertices[vert++] = bottomCenter; vertices[vert++] = topCenter; vertices[vert++] = tR;
                vertices[vert++] = tR; vertices[vert++] = bR; vertices[vert++] = bottomCenter;
            }

            currentAngle += deltaAngle;
        }

        for (int i = 0; i < vert; i++) triangles[i] = i;

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    /// <summary>
    /// Calcula la distancia al objeto de la capa de oclusion mas cercano o al maximo de la cuña
    /// </summary>
    /// <param name="worldDirection">Posicion del agente respecto al mundo</param>
    /// <returns>Distancia al objeto o maximo de distancia</returns>
    private float GetDistance(Vector3 worldDirection)
    {
        Vector3 rayOrigin = transform.position + Vector3.up * (altura / 2 - alturaBase);
    
        if (Physics.Raycast(rayOrigin, worldDirection, out RaycastHit hit, distancia, oclusionLayers))
        {
            return hit.distance;
        }
        return distancia;
    }

    private void OnDrawGizmos()
    {
        if (mesh)
        {
            Gizmos.color = meshColor;
            Gizmos.DrawMesh(mesh, transform.position, transform.rotation);
        }

        Gizmos.DrawWireSphere(transform.position, distancia);
        for(int i = 0; i < count; ++i)
        {
            Gizmos.DrawSphere(colliders[i].transform.position, 0.2f);
        }

        Gizmos.color = Color.green;
        foreach(GameObject obj in CurrentObjects)
        {
            Gizmos.DrawSphere(obj.transform.position, 0.2f);
        }
    }
}