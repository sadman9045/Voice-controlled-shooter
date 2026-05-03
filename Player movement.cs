using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform cameraTransform;
    public Animator animator;

    public float speed = 7f;
    public float rotationSpeed = 10f;
    public float fireRotateSpeed = 25f;

    float gravity = -25f;
    float yVelocity;

    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        bool isFiring = Input.GetButton("Fire1");

        if (isFiring)
        {
            RotateToCamera();
        }
        else
        {
            MovePlayer();
        }

        ApplyGravity();
    }

    void MovePlayer()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(h, 0f, v).normalized;
        bool isRunning = direction.magnitude >= 0.1f;

        if (animator != null)
            animator.SetBool("IsRunning", isRunning);

        if (isRunning)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;

            Quaternion rotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }
    }

    void RotateToCamera()
    {
        Vector3 dir = cameraTransform.forward;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, fireRotateSpeed * Time.deltaTime);
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && yVelocity < 0)
            yVelocity = -2f;

        yVelocity += gravity * Time.deltaTime;
        controller.Move(Vector3.up * yVelocity * Time.deltaTime);
    }
}
