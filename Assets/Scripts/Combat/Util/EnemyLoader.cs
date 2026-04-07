using UnityEngine;

public static class EnemyLoader {
    public static EnemyData LoadEnemy(string enemyId) {
        string resourcePath = $"Enemies/{enemyId.ToLower()}/{enemyId.ToLower()}";
        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);

        if (jsonAsset != null) {
            return JsonUtility.FromJson<EnemyData>(jsonAsset.text);
        }

        Debug.LogError("Enemy JSON not found in Resources at: " + resourcePath);
        return null;
    }
}

[System.Serializable]
public class EnemyData {
    public string id;
    public string name;
    public int[] unit;
    public int health;
    public int qt;
    public DialogueLine[] dialogue;
    public string[] banter;
    public string[] correct;
    public string[] wrong;
    public string sprite;
    public string music;
    public float musicVolume;

    [System.Serializable]
    public class DialogueLine {
        public string text;
        public float duration;
    }
}
