using UnityEngine;

using System.Collections.Generic;

public static class ItemLoader {
    public static ItemList GetAllItems() {
        string resourcePath = "Items/items";
        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);

        if (jsonAsset == null) {
            Debug.LogError("Item JSON not found in Resources at: " + resourcePath);
            return null;
        }

        string rawJson = jsonAsset.text;
        string wrappedJson = "{\"items\":" + rawJson + "}";

        ItemList items = JsonUtility.FromJson<ItemList>(wrappedJson);

        if (items.items == null || items.items.Count == 0) {
            Debug.LogWarning("No items found in list.");
            return null;
        }

        return items;
    }
}

[System.Serializable]
public class ItemData {
    public string itemName;
    public string id;
    public float damageBuff;
    public float liveAmount;
}

[System.Serializable]
public class ItemList {
    public List<ItemData> items;
}