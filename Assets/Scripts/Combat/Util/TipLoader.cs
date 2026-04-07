using UnityEngine;

using System.Collections.Generic;

public static class TipLoader {
    public static string GetRandomTip(int unit) {
        string resourcePath = $"Questions/{unit}/tips";
        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);

        if (jsonAsset == null)
            return "";

        TipList tipList = JsonUtility.FromJson<TipList>(jsonAsset.text);

        if (tipList == null || tipList.tips == null || tipList.tips.Count == 0)
            return "";

        int index = Random.Range(0, tipList.tips.Count);
        return tipList.tips[index];
    }
}

[System.Serializable]
public class TipList {
    public List<string> tips;
}
