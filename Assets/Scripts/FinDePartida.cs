using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para reiniciar o cambiar de escena

public class FinDePartida : MonoBehaviour
{
    // Puedes escribir el nombre de la escena de "Game Over" o la escena actual para reiniciar
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

        // Opción A: Cargar una escena de menú o créditos
        // SceneManager.LoadScene(nombreEscenaDestino);

        // Opción B: Simplemente cerrar la aplicación (solo funciona en el juego builded, no en el editor)
        // Application.Quit();

        // Opción C: Si estás en el editor de Unity, esto detendrá el modo Play
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}