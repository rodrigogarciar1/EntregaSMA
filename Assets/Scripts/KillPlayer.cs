using UnityEngine;
using UnityEngine.SceneManagement;

public class KillPlayer : MonoBehaviour
{
    public BaseConocimiento bs;

    private void Awake()
    {
        bs = GetComponent<BaseConocimiento>();
    }
private void OnTriggerStay(Collider other)
{
    if (other.CompareTag("Player"))
    {
        if (bs != null && bs.PlayerPosition != null) 
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
}