using UnityEngine;

public class Spike : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private Transform respawnPoint;

    private PlayerController playerController;
    private Rigidbody2D playerRigidbody;
    private Collider2D spikeCollider;
    private Collider2D playerCollider;
    private Animator spikeAnimator;
    private Vector2 roomStartPosition;
    private bool hasRoomStartPosition;
    private bool hasTeleportedThisWindow;

    private const float KExtrudeStartTime = 0.014f;
    private const float KExtrudeEndTime = 0.043f;

    private void Awake()
    {
        spikeAnimator = GetComponent<Animator>();
        spikeCollider = GetComponent<Collider2D>();

        if (player == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                player = playerController.gameObject;
            }
        }
        else
        {
            playerController = player.GetComponent<PlayerController>();
        }

        if (player == null)
        {
            return;
        }

        playerRigidbody = player.GetComponent<Rigidbody2D>();
        playerCollider = player.GetComponent<Collider2D>();
        roomStartPosition = respawnPoint != null ? (Vector2)respawnPoint.position : player.transform.position;
        hasRoomStartPosition = true;
    }

    private void FixedUpdate()
    {
        if (!IsSpikeDangerous())
        {
            hasTeleportedThisWindow = false;
            return;
        }

        if (IsPlayerTouchingSpike())
        {
            TeleportPlayerToRoomStart();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryTeleportPlayer(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryTeleportPlayer(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTeleportPlayer(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryTeleportPlayer(other.gameObject);
    }

    private bool IsPlayerTouchingSpike()
    {
        if (spikeCollider == null || playerCollider == null)
        {
            return false;
        }

        return spikeCollider.IsTouching(playerCollider);
    }

    private void TryTeleportPlayer(GameObject target)
    {
        if (!IsSpikeDangerous() || hasTeleportedThisWindow || playerController == null)
        {
            return;
        }

        if (target.GetComponentInParent<PlayerController>() != playerController)
        {
            return;
        }

        TeleportPlayerToRoomStart();
    }

    private bool IsSpikeDangerous()
    {
        if (spikeAnimator == null)
        {
            return false;
        }

        AnimatorStateInfo stateInfo = spikeAnimator.GetCurrentAnimatorStateInfo(0);
        float timeInState = Mathf.Repeat(stateInfo.normalizedTime, 1f) * stateInfo.length;

        return timeInState >= KExtrudeStartTime && timeInState <= KExtrudeEndTime;
    }

    private void TeleportPlayerToRoomStart()
    {
        if (!hasRoomStartPosition)
        {
            return;
        }

        hasTeleportedThisWindow = true;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            playerRigidbody.position = roomStartPosition;
        }
        else
        {
            player.transform.position = roomStartPosition;
        }
    }
}
