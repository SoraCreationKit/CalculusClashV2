using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Encounter : MonoBehaviour {
    [SerializeField] 
    public GameObject player;
    public GameObject transitionPanel;
    public Region activationRegion;

    public string[] enemyIds;
    public string currentRoom;
    public float encounterPercentagePerSecond;

    private float _timer;

    private void Start() {
        var seed = DateTime.Now.Ticks.GetHashCode();
        Random.InitState(seed);
    }

    private void Update() {
        if (!GameManager.instance.isBattleHandled) {
            player.transform.position = new Vector2(
                GameManager.instance.roomPositions[currentRoom].X,
                GameManager.instance.roomPositions[currentRoom].Y
            );

            GameManager.instance.isBattleHandled = true;
        }

        if (!activationRegion.Contains(player.transform.position.x, player.transform.position.y)
            || GameManager.instance.isDialoguePlaying || GameManager.instance.isBattlePlaying)
            return;

        _timer += Time.deltaTime;

        if (_timer < 1f) return;

        _timer = 0f;

        var roll = Random.Range(1, 101);
        if (roll == 1) {
            TriggerLagrange();
        } else if (roll < encounterPercentagePerSecond) {
            TriggerEncounter();
        }
    }

    private void TriggerEncounter() {
        var updatedPosition = new Position(player.transform.position.x, player.transform.position.y);
        GameManager.instance.roomPositions[currentRoom] = updatedPosition;

        GameManager.instance.encounterEnemyId = enemyIds[Random.Range(0, enemyIds.Length)];
        GameManager.instance.previousScene = currentRoom;

        GameManager.instance.isBattlePlaying = true;
        StartCoroutine(FadeAndLoadFight());
    }

    private void TriggerLagrange() {
        var updatedPosition = new Position(player.transform.position.x, player.transform.position.y);
        GameManager.instance.roomPositions[currentRoom] = updatedPosition;

        GameManager.instance.encounterEnemyId = "lagrange_reaper";
        GameManager.instance.previousScene = currentRoom;

        GameManager.instance.isBattlePlaying = true;
        StartCoroutine(FadeAndLoadFight());
    }

    private IEnumerator FadeAndLoadFight() {
        var alertClip = Resources.Load<AudioClip>("SFX/alert");

        if (alertClip != null) {
            var audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.clip = alertClip;
            audioSource.Play();

            yield return new WaitForSeconds(alertClip.length);

            Destroy(audioSource);
        }

        if (transitionPanel != null) {
            var panelImage = transitionPanel.GetComponent<Image>();

            if (panelImage != null) {
                transitionPanel.SetActive(true);

                var color = panelImage.color;
                var duration = 0.3f;
                var elapsed = 0f;

                color.a = 0f;
                panelImage.color = color;

                while (elapsed < duration) {
                    elapsed += Time.deltaTime;
                    color.a = Mathf.Clamp01(elapsed / duration);
                    panelImage.color = color;

                    yield return null;
                }

                color.a = 1f;
                panelImage.color = color;
            }
        }

        GameManager.instance.isBattleHandled = false;
        SceneManager.LoadScene("Fight");
    }
}