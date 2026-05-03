using UnityEngine;
using UnityEngine.SceneManagement; 

public class FinDePartida : MonoBehaviour
{

    [SerializeField] private string nombreEscenaDestino = "MenuPrincipal";

    private void OnTriggerEnter(Collider other)
    {
        // Comprobamos si el objeto que ha entrado tiene el Tag "Player"
        if (other.CompareTag("Player"))
        {
            TerminarJuego();
        }
    }

    private void TerminarJuego()
    {
        Debug.Log("¡Partida terminada! El jugador ha llegado al destino.");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}