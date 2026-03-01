using UnityEngine;
using UnityEngine.Events;
public class PlayerInventory : MonoBehaviour
{
    public int NumberOfRelics { get; private set; }
    public UnityEvent<PlayerInventory> RelicCollected;
    public void RelicsCollected()
    {
        NumberOfRelics++;
        RelicCollected.Invoke(this);
    }

}
