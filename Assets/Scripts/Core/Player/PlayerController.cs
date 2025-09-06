using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public enum AxisLock { LockZ, LockX }
    [Header("2.5D Mode")]
    public AxisLock lockAxis = AxisLock.LockZ;   // LockZ = side-scroller (gerak X), LockX = rail (gerak Z)
    public float lockValue = 0f;                 // Nilai posisi tetap untuk axis yang dikunci

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float rotationSmoothing = 12f;
    public bool faceAlongMovement = true;        // Rotasi mengikuti arah gerak (rasa third-person)

    [Header("Jump & Gravity")]
    public float jumpHeight = 2f;
    public float gravity = -25f;
    [Range(0f, 1f)] public float airControl = 0.6f;

    [Header("Ground Check")]
    public Transform groundCheck;                // letakkan di kaki
    public float groundRadius = 0.25f;
    public LayerMask groundMask = ~0;

    CharacterController cc;
    float verticalVel;
    bool isGrounded;

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        // Auto buat groundCheck jika belum ada
        if (groundCheck == null)
        {
            var go = new GameObject("GroundCheck");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0, -cc.height * 0.5f + cc.skinWidth + 0.05f, 0);
            groundCheck = go.transform;
        }

        // Inisialisasi nilai kunci sumbu dari posisi awal bila 0
        if (Mathf.Approximately(lockValue, 0f))
        {
            lockValue = (lockAxis == AxisLock.LockZ) ? transform.position.z : transform.position.x;
        }
    }

    void Update()
    {
        // --- Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask, QueryTriggerInteraction.Ignore);
        if (isGrounded && verticalVel < 0f) verticalVel = -2f;

        // --- Input (Old Input System)
        float h = Input.GetAxisRaw("Horizontal");
        float v = 0f; // default 2.5D: hanya 1 sumbu gerak

        // Mode rail (gerak Z), kunci X
        if (lockAxis == AxisLock.LockX)
        {
            v = Input.GetAxisRaw("Vertical");
            h = 0f;
        }

        // --- Vektor gerak di world
        Vector3 move = Vector3.zero;
        if (lockAxis == AxisLock.LockZ)
        {          // side-scroller
            move = new Vector3(h, 0f, 0f);
        }
        else
        {                                   // rail depan-belakang
            move = new Vector3(0f, 0f, v);
        }
        move = move.normalized;

        float control = isGrounded ? 1f : airControl;
        Vector3 horizontalVel = move * moveSpeed * control;

        // --- Jump
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            verticalVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        verticalVel += gravity * Time.deltaTime;

        Vector3 velocity = new Vector3(horizontalVel.x, verticalVel, horizontalVel.z);
        cc.Move(velocity * Time.deltaTime);

        // --- Kunci sumbu (tetap di rel/plane)
        Vector3 pos = transform.position;
        if (lockAxis == AxisLock.LockZ) pos.z = lockValue;
        else pos.x = lockValue;
        transform.position = pos;

        // --- Rotasi hadap arah gerak (rasa third-person)
        if (faceAlongMovement)
        {
            Vector3 lookDir = new Vector3(horizontalVel.x, 0f, horizontalVel.z);
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmoothing * Time.deltaTime);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (lockAxis == AxisLock.LockZ)
        {
            Gizmos.DrawLine(new Vector3(-999f, transform.position.y, lockValue),
                            new Vector3(+999f, transform.position.y, lockValue));
        }
        else
        {
            Gizmos.DrawLine(new Vector3(lockValue, transform.position.y, -999f),
                            new Vector3(lockValue, transform.position.y, +999f));
        }
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
    }
}
