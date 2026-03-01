using UnityEngine;
using System.Collections;

public class DoorInteraction : MonoBehaviour
{
    [Header("Configuración de la puerta")]
    public float openAngle = 90f;
    public float openSpeed = 3f;

    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private Coroutine _currentCoroutine;
    private bool isOpen = false;
    private bool abiertaPorJugador = false; // El jugador controla manualmente
    private bool playerEnRango = false;
    private bool agenteEnRango = false;

    void Start()
    {
        _closedRotation = transform.rotation;
        _openRotation = Quaternion.Euler(transform.eulerAngles + new Vector3(0, openAngle, 0));
    }

    void Update()
    {
        if (playerEnRango && Input.GetKeyDown(KeyCode.E))
        {
            abiertaPorJugador = true;
            ToggleDoorState();
        }

        if (agenteEnRango && !isOpen)
        {
            abiertaPorJugador = false;
            ToggleDoorState();
        }

        if (!agenteEnRango && !playerEnRango && isOpen && !abiertaPorJugador)
        {
            ToggleDoorState();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerEnRango = true;

        if (other.CompareTag("Agent") && !isOpen)
        {
            abiertaPorJugador = false;
            ToggleDoorState();
            agenteEnRango = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerEnRango = false;

        if (other.CompareTag("Agent"))
        {
            agenteEnRango = false;

            if (isOpen && !abiertaPorJugador)
                ToggleDoorState();
        }
    }

    void ToggleDoorState()
    {
        if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
        _currentCoroutine = StartCoroutine(ToggleDoor());
    }

    private IEnumerator ToggleDoor()
    {
        Quaternion targetRotation = isOpen ? _closedRotation : _openRotation;
        isOpen = !isOpen;

        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.01f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                openSpeed * 100f * Time.deltaTime
            );            
            yield return null;
        }

        transform.rotation = targetRotation;
    }
}