using UnityEngine;

public class Hearing : Sensor
{   public MeshRenderer debugMesh; // arrastrar el MeshRenderer de la esfera

    private SphereCollider hearingCollider;
    public float hearingradius = 2f;

    private void Update()
    {
        // mantener el tamaño de la esfera igual al radio de escucha
        if (debugMesh != null)
            debugMesh.transform.localScale = Vector3.one * hearingradius * 2f;
        
        if (hearingCollider != null)
            hearingCollider.radius = hearingradius;
    }
    private void Awake()
    {
        cerebro = GetComponentInParent<CerebroSubsumido>();
        hearingCollider = GetComponent<SphereCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Noise")) return;
        if (cerebro.baseConocimiento.investigatingNoise)
        return;
        alertNewState(other.gameObject, true);
         // Visualización rápida: dibuja una línea desde agente al ruido
    Debug.DrawLine(transform.position, other.transform.position, Color.green, 1f);
    Debug.Log($"<color=green>[Hearing]</color> Ruido detectado en {other.transform.position}");
    }



    public override void alertNewState(GameObject obj, bool state)
    {
        if (cerebro == null) return;
        if (state){
            cerebro.baseConocimiento.LastPlayerSighting = obj.transform;
        }
        cerebro.Notify(
            typeof(Hearing),
            obj,
            state
        );
    }
    private void OnValidate()
    {
    if (hearingCollider == null)
        hearingCollider = GetComponent<SphereCollider>();

    if (hearingCollider != null)
        hearingCollider.radius = hearingradius;
    }
    private void OnDrawGizmos()
    {
        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc == null) return;

        Gizmos.color = new Color(0f, 0f, 1f, 0.25f);

        Vector3 worldCenter = transform.position + sc.center;
        Gizmos.DrawSphere(worldCenter, sc.radius * transform.lossyScale.x);
    }
}