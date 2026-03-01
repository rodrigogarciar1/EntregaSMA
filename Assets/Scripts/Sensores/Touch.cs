using UnityEngine;

public class Touch : Sensor
{
    public MeshRenderer debugMesh;

    private void Awake()
    {
        cerebro = GetComponentInParent<CerebroSubsumido>();
    }

    private void OnTriggerEnter(Collider other)
    {   
        if (!other.CompareTag("Player")) return;

        Debug.Log($"[Touch] Agente tocó al jugador en {other.transform.position}");

        alertNewState(other.gameObject, true);
    }

    public override void alertNewState(GameObject obj, bool state)
    {
        if (cerebro == null) return;

        cerebro.Notify(
            typeof(Touch),
            obj,
            state
        );
    }
}