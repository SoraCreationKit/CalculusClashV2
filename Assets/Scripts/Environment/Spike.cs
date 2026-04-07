using UnityEngine;

public class Spike : MonoBehaviour {
    [SerializeField] private GameObject player;
    [SerializeField] private Transform respawnPoint;

    private PlayerCtrl _PlayerCtrl;
    private Rigidbody2D _playerRigidBody;

    private Collider2D _spikeCollider;
    private Collider2D _playerCollider;

    private Animator _spikeAnimator;
    private Vector2 _roomStartPosition;

    private bool _hasRoomStartPosition;
    private bool _hasTeleportedThisWindow;

    private const float KExtrudeStartTime = 0.014f;
    private const float KExtrudeEndTime = 0.043f;

    private void Awake() {
        _spikeAnimator = GetComponent<Animator>();
        _spikeCollider = GetComponent<Collider2D>();

        if (player == null) {
            _PlayerCtrl = FindFirstObjectByType<PlayerCtrl>();

            if (_PlayerCtrl != null) {
                player = _PlayerCtrl.gameObject;
            }
        } else {
            _PlayerCtrl = player.GetComponent<PlayerCtrl>();
        }

        if (player == null) {
            return;
        }

        _playerRigidBody = player.GetComponent<Rigidbody2D>();
        _playerCollider = player.GetComponent<Collider2D>();

        _roomStartPosition = respawnPoint != null ? (Vector2)respawnPoint.position : player.transform.position;
        _hasRoomStartPosition = true;
    }

    private void FixedUpdate() {
        if (!IsSpikeDangerous()) {
            _hasTeleportedThisWindow = false;
            return;
        }

        if (IsPlayerTouchingSpike()) {
            TeleportPlayerToRoomStart();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        TryTeleportPlayer(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision) {
        TryTeleportPlayer(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        TryTeleportPlayer(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other) {
        TryTeleportPlayer(other.gameObject);
    }

    private bool IsPlayerTouchingSpike() {
        if (_spikeCollider == null || _playerCollider == null) {
            return false;
        }

        return _spikeCollider.IsTouching(_playerCollider);
    }

    private void TryTeleportPlayer(GameObject target) {
        if (!IsSpikeDangerous() || _hasTeleportedThisWindow || _PlayerCtrl == null) {
            return;
        }

        if (target.GetComponentInParent<PlayerCtrl>() != _PlayerCtrl) {
            return;
        }

        TeleportPlayerToRoomStart();
    }

    private bool IsSpikeDangerous() {
        if (_spikeAnimator == null) {
            return false;
        }

        AnimatorStateInfo stateInfo = _spikeAnimator.GetCurrentAnimatorStateInfo(0);
        var timeInState = Mathf.Repeat(stateInfo.normalizedTime, 1f) * stateInfo.length;

        return timeInState >= KExtrudeStartTime && timeInState <= KExtrudeEndTime;
    }

    private void TeleportPlayerToRoomStart() {
        if (!_hasRoomStartPosition) {
            return;
        }

        _hasTeleportedThisWindow = true;

        if (_playerRigidBody != null) {
            _playerRigidBody.linearVelocity = Vector2.zero;
            _playerRigidBody.angularVelocity = 0f;
            _playerRigidBody.position = _roomStartPosition;
        } else {
            player.transform.position = _roomStartPosition;
        }
    }
}