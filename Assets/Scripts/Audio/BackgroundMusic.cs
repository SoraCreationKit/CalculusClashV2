using UnityEngine;

public class BGMManager : MonoBehaviour {
    [SerializeField]
    public AudioClip[] bgmClips;
    private AudioSource audioSource;
    private bool paused = false;

    private void Start() {

    }

    private void Update() {
        if (GameManager.instance.isBattlePlaying) {
            audioSource.Pause();
            paused = true;
            return;
        }

        if (paused) {
            paused = false;
            audioSource.UnPause();
        }
    }

    private void PlayRandomBGM() {
        
    }
}