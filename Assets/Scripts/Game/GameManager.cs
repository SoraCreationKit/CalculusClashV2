using UnityEngine;

using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {
    public static GameManager instance;

    private AudioSource musicSource;

    public const string KVersion = "1.8.0";
    public const string KBuildDate = "08-10-2025";
    public const bool KDevelopmentBuild = false;
    public const int KRoomAmount = 10;

    public bool isSafe = true;
    public bool wentPrevious = false;
    public bool isBattleHandled = true;
    public bool isDialoguePlaying = false;
    public bool isBattlePlaying = false;
    public bool hasWonGame = false;

    public string encounterEnemyId;
    public string previousScene;

    public string currentBackgroundMusic;

    public float transferPositionX, transferPositionY;

    public int currentRoom = 1;

    public Dictionary<string, int> playerItems = new Dictionary<string, int>();
    public Dictionary<string, Position> roomPositions = new Dictionary<string, Position>();

    // i hate this design but we have no other options........
    public Dictionary<string, string> randomAccess = new Dictionary<string, string>();
    public Dictionary<string, Vector2> previousPositions = new Dictionary<string, Vector2>();

    private void Awake() {
        if (instance == null) {
            instance = this;

            musicSource = GetComponent<AudioSource>();

            DontDestroyOnLoad(gameObject);

            var allItems = ItemLoader.GetAllItems();
            if (allItems != null && allItems.items != null)
                foreach (var item in allItems.items)
                    playerItems.TryAdd(item.itemName, 0);

            for (var i = 0; i < KRoomAmount + 1; ++i) roomPositions["room" + i] = new Position(0, 0);

            for (var i = 0; i < KRoomAmount + 1; ++i) roomPositions["Room" + i] = new Position(0, 0);
        } else {
            Destroy(gameObject);
        }
    }

    public static GameManager GetInstance() {
        return instance;
    }

    public void PlayMusic() {
        Debug.Log("Playing music.");

        if (!this.musicSource.isPlaying) {
            this.musicSource.Play();
        }
    }

    public void SetEncounter(string enemyId, string returnScene) {
        this.encounterEnemyId = enemyId;
        this.previousScene = returnScene;
    }

    public IEnumerator FadeOutMusic(int seconds) {
        var currentVolume = this.musicSource.volume;

        var time = 0f;
        while (time < seconds) {
            time += Time.unscaledDeltaTime;
            this.musicSource.volume = Mathf.Lerp(currentVolume, 0f, time / seconds);
            yield return null;
        }

        this.musicSource.volume = 0f;
        this.musicSource.Stop();
    }

    public void StopMusic() {
        this.musicSource.Stop();
    }

    /*
     * Item Methods
     * These methods are used to interact with the items system, you can use the following to do so:
     */

    public void AddItem(string itemName, int amount = 1) {
        if (playerItems.TryAdd(itemName, amount)) {
            playerItems[itemName] += amount;
        }
        else {
            playerItems[itemName] = amount;
        }
    }

    public bool RemoveItem(string itemName, int amount = 1) {
        if (!playerItems.ContainsKey(itemName) || playerItems[itemName] <= amount) {
            return false;
        }

        playerItems[itemName] -= amount;
        return true;
    }

    public int GetItemAmount(string itemName) {
        return playerItems.ContainsKey(itemName) ? playerItems[itemName] : 0;
    }
}
