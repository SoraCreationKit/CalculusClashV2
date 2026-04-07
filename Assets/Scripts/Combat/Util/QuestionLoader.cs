using UnityEngine;

using System.IO;
using System.Collections.Generic;

public static class QuestionLoader {
    public static QuestionData GetRandomQuestion(int unit, out Sprite sprite) {
        sprite = null;

        string resourcePath = $"Questions/{unit}/questions";
        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);

        if (jsonAsset == null) {
            Debug.LogError("Question JSON not found: " + resourcePath);
            return null;
        }

        string rawJson = jsonAsset.text;
        string wrappedJson = "{\"questions\":" + rawJson + "}";
        QuestionDataList dataList = JsonUtility.FromJson<QuestionDataList>(wrappedJson);

        if (dataList.questions.Count == 0) {
            Debug.LogWarning("No questions found in list.");
            return null;
        }

        int randIndex = Random.Range(0, dataList.questions.Count);
        QuestionData question = dataList.questions[randIndex];

        string spritePath = $"Questions/{unit}/{question.spriteIndex}";
        sprite = Resources.Load<Sprite>(spritePath);

        if (sprite == null) {
            Debug.LogError("Sprite not found at: " + spritePath);
        }

        return question;
    }
}

[System.Serializable]
public class QuestionData {
    public int spriteIndex;
    public int spriteWidth;
    public int spriteHeight;
    public int correct;
}

[System.Serializable]
public class QuestionDataList {
    public List<QuestionData> questions;
}

