using UnityEngine;

public class ControladorJugador : MonoBehaviour
{
    public float speed = 5f;
    public float runMultiplier = 2f;
    public float rotationSpeed = 120f;

    [Header("Ruido")]
    public float walkNoiseRadius = 2f;
    public float runNoiseRadius = 6f;
    public Transform noiseSphere;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        float moveVertical = Input.GetAxis("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        float currentSpeed = isRunning ? speed * runMultiplier : speed;

        Vector3 movement = transform.forward * moveVertical * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);

        float turn = Input.GetAxis("Horizontal") * rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));

        UpdateNoise(moveVertical, isRunning);
    }

    private void UpdateNoise(float moveInput, bool isRunning)
    {
        float radius = 0f;

        if (Mathf.Abs(moveInput) > 0.1f)
        {
            // Andar: radio fijo
            radius = isRunning ? runNoiseRadius : walkNoiseRadius;
        }

        // Escala = diámetro
        noiseSphere.localScale = Vector3.one * radius * 2f;
    }
}