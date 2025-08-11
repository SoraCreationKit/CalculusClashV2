using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class CombatManager : MonoBehaviour {
    [SerializeField] 
    public Image enemyImage;
    public Image questionImage;
    public Slider damageBar;

    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI playerLivesText;
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI questionDebugObject;

    public GameObject textbox;
    public GameObject questionBox;
    public GameObject questionPanel;
    public GameObject itemPanel;
    public GameObject starText;

    public List<GameObject> buttons;
    public List<GameObject> questionButtons;
    public List<TextMeshProUGUI> itemOptions;

    public int playerLives = 3;

    private int buttonIndex;
    private int itemIndex;
    private int questionButtonIndex;
    private int dialogueIndex;

    private EnemyData currEnemy;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioSource buttonSource;

    private float currEnemyHp;

    private bool isButtonEnabled;
    private bool isQuestionEnabled;

    private bool skipTypewriter;
    private bool waitingForEnter;

    private readonly Dictionary<string, bool> actionLocks = new();

    private List<Sprite> menuSelectedSprites;
    private List<Sprite> menuUnselectedSprites;

    private List<Sprite> questionSelectedSprites;
    private List<Sprite> questionUnselectedSprites;

    private ItemList items;
    private readonly List<ItemData> displayableItems = new();

    private readonly Color defaultColor = Color.white;
    private readonly Color selectedColor = new(123f / 255f, 163f / 255f, 217f / 255f);

    private readonly float typewriterSpeed = 0.08f;

    private Vector2 previousBossPosition;

    private Coroutine questionTimerCoroutine;
    private float questionTimeLimit = 30f;
    private bool questionTimeExpired;

    private enum CombatState {
        Dialogue,
        PlayerChoice,
        Question,
        EnemyTurn,
        ShowTip,
        ShowItems,
        End
    }

    private CombatState state = CombatState.Dialogue;

    private QuestionData currentQuestion;
    private int correctAnswerIndex;

    public void Start() {
        var seed = DateTime.Now.Ticks.GetHashCode();
        Random.InitState(seed);

        // Todo: Implement proper enemy loading via GameManager.
        if (GameManager.instance != null) {
            if (GameManager.instance.encounterEnemyId != null)
                currEnemy = EnemyLoader.LoadEnemy(GameManager.instance.encounterEnemyId);
            else
                currEnemy = EnemyLoader.LoadEnemy("dummy");
        }
        else {
            currEnemy = EnemyLoader.LoadEnemy("dummy");
        }

        if (currEnemy != null) {
            currEnemyHp = currEnemy.health;

            var sprite = Resources.Load<Sprite>("EnemySprites/" + Path.GetFileNameWithoutExtension(currEnemy.sprite));

            enemyImage.sprite = sprite;
            enemyImage.preserveAspect = true;

            var imageRect = enemyImage.rectTransform;
            var targetHeight = imageRect.rect.height;

            if (sprite != null) {
                var aspect = sprite.rect.width / sprite.rect.height;
                imageRect.sizeDelta = new Vector2(targetHeight * aspect, targetHeight);
            }
        }

        // Load locks:
        actionLocks["main"] = true;
        actionLocks["menu"] = true;
        actionLocks["question"] = true;
        actionLocks["item"] = true;

        // Hide the question panel by default
        if (questionPanel != null)
            questionPanel.SetActive(false);

        // Load items
        items = ItemLoader.GetAllItems();
        Debug.Log($"Loaded items: {items?.items?.Count ?? -1}");

        // Populate displayableItems with all items
        displayableItems.Clear();
        if (items != null && items.items != null)
            foreach (var item in items.items)
                displayableItems.Add(item);

        // Load button sprites
        string[] spriteNames = { "Solve", "Help", "Item", "Skip" };
        menuSelectedSprites = new List<Sprite>();
        menuUnselectedSprites = new List<Sprite>();

        foreach (var spriteName in spriteNames) {
            var selected = Resources.Load<Sprite>("UI/Combat/" + spriteName + "Selected");
            var unselected = Resources.Load<Sprite>("UI/Combat/" + spriteName);

            menuSelectedSprites.Add(selected);
            menuUnselectedSprites.Add(unselected);
        }

        // Load question sprites
        string[] questionSpriteNames = { "A", "B", "C", "D" };
        questionSelectedSprites = new List<Sprite>();
        questionUnselectedSprites = new List<Sprite>();

        foreach (var spriteName in questionSpriteNames) {
            var selected = Resources.Load<Sprite>("UI/Combat/" + spriteName + "Selected");
            var unselected = Resources.Load<Sprite>("UI/Combat/" + spriteName);

            questionSelectedSprites.Add(selected);
            questionUnselectedSprites.Add(unselected);
        }

        // Load the boss music.
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = currEnemy.musicVolume;

        var bossTheme = Resources.Load<AudioClip>("Audio/EnemyMusic/" + currEnemy.music.Replace(".mp3", ""));

        if (bossTheme != null) {
            musicSource.clip = bossTheme;
            musicSource.Play();
        }

        // Load the SFX player.
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        // Load the button player.
        buttonSource = gameObject.AddComponent<AudioSource>();
        buttonSource.loop = false;
        buttonSource.playOnAwake = false;

        // Start the dialogue.
        NextDialogue();
    }

    public void Update() {
        switch (state) {
            case CombatState.Dialogue:
                HandleDialogueInput();
                break;

            case CombatState.PlayerChoice:
                HandlePlayerChoiceInput();
                break;

            case CombatState.Question:
                HandleQuestionInput();
                break;

            case CombatState.ShowTip:
                HandleDialogueInput();
                break;

            case CombatState.ShowItems:
                HandleItemPanelInput();
                break;
        }

        DebugLocks();
        UpdateStar();
    }

    // UPDATE METHODS
    private void UpdateQuestionButtonSprites() {
        for (var i = 0; i < questionButtons.Count; ++i) {
            var img = questionButtons[i].GetComponent<Image>();

            img.sprite = i == questionButtonIndex ? questionSelectedSprites[i] : questionUnselectedSprites[i];
        }
    }

    private void UpdateStar() {
        starText.SetActive(!string.IsNullOrEmpty(dialogueText.text) ? true : false);
    }

    private void UpdateButtonSprites() {
        for (var i = 0; i < buttons.Count; ++i) {
            var img = buttons[i].GetComponent<Image>();

            img.sprite = i == buttonIndex ? menuSelectedSprites[i] : menuUnselectedSprites[i];
        }
    }

    private void UpdateItemOptions() {
        var optionCount = itemOptions != null ? itemOptions.Count : 0;
        var displayCount = displayableItems != null ? displayableItems.Count : 0;

        for (var i = 0; i < optionCount; ++i)
            if (i < displayCount) {
                var item = displayableItems[i];
                var amount = 0;

                if (GameManager.GetInstance() != null)
                    amount = GameManager.GetInstance().GetItemAmount(item.itemName);

                if (itemOptions[i] != null) {
                    itemOptions[i].gameObject.SetActive(true);
                    itemOptions[i].text = $"{item.itemName} X{amount}";
                    itemOptions[i].color = i == itemIndex ? selectedColor : defaultColor;
                }
            }
            else {
                if (itemOptions[i] != null)
                    itemOptions[i].gameObject.SetActive(false);
            }
    }

    // HANDLE METHODS
    private void HandlePlayerChoiceInput() {
        if (IsActionUnlocked("main"))
            RegisterLock("main");
        else
            return;

        if (!isButtonEnabled) return;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            if (buttonIndex < buttons.Count - 1)
                buttonIndex++;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            if (buttonIndex > 0)
                buttonIndex--;

        UpdateButtonSprites();

        if (!Input.GetKeyDown(KeyCode.Return)) return;

        RegisterUnlock("main");

        var selectClip = Resources.Load<AudioClip>("Audio/Combat/select");
        if (selectClip != null && sfxSource != null) {
            buttonSource.clip = selectClip;
            buttonSource.Play();
        }

        switch (buttonIndex) {
            case 0: // Solve
                StartQuestion();
                break;
            case 1: // Help
                ShowTip();
                break;
            case 2: // Item
                ShowItems();
                break;
            case 3: // Skip
                break;
        }
    }

    private void HandleQuestionInput() {
        if (IsActionUnlocked("question"))
            RegisterLock("question");
        else
            return;

        if (!isQuestionEnabled) return;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            if (questionButtonIndex < questionButtons.Count - 1)
                questionButtonIndex++;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            if (questionButtonIndex > 0)
                questionButtonIndex--;

        UpdateQuestionButtonSprites();

        if (!Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) return;

        isQuestionEnabled = false;

        var correct = questionButtonIndex == correctAnswerIndex;

        if (enemyImage != null)
            enemyImage.gameObject.SetActive(true);

        if (enemyImage != null)
            StartCoroutine(SlideBossImage(previousBossPosition));

        if (correct) {
            var randomIndex = Random.Range(0, currEnemy.correct.Length);
            dialogueText.text = currEnemy.correct[randomIndex];

            var damage = (float)Math.Round(20 * damageBar.value);

            StartCoroutine(HandleDamageAndContinue(damage));
            currEnemyHp -= damage;
        }
        else {
            playerLives -= 1;
            playerLivesText.text = playerLives.ToString();

            var randomIndex = Random.Range(0, currEnemy.wrong.Length);
            dialogueText.text = currEnemy.wrong[randomIndex];

            if (playerLives <= 0) {
                if (questionPanel != null)
                    questionPanel.SetActive(false);

                if (enemyImage != null)
                    enemyImage.gameObject.SetActive(true);

                GameOver();
                return;
            }

            RegisterUnlock("question");
        }

        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (enemyImage != null)
            enemyImage.gameObject.SetActive(true);

        state = CombatState.PlayerChoice;
        EnablePlayerChoice();
    }

    private void HandleDialogueInput() {
        if (!Input.GetKeyDown(KeyCode.Return)) return;

        if (waitingForEnter)
            waitingForEnter = false;
        else
            skipTypewriter = true;
    }

    private void HandleItemPanelInput() {
        if (IsActionUnlocked("item"))
            RegisterLock("item");
        else
            return;

        if (displayableItems.Count == 0) {
            if (!Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape)) return;

            if (itemPanel != null)
                itemPanel.SetActive(false);

            state = CombatState.PlayerChoice;
            EnablePlayerChoice();

            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) {
            if (itemIndex < displayableItems.Count - 1) itemIndex++;
            UpdateItemOptions();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) {
            if (itemIndex > 0) itemIndex--;
            UpdateItemOptions();
        }

        if (Input.GetKeyDown(KeyCode.Return)) UseSelectedItem();

        if (!Input.GetKeyDown(KeyCode.Escape)) {
            RegisterUnlock("item");
            return;
        }

        if (itemPanel != null)
            itemPanel.SetActive(false);

        RegisterUnlock("item");

        state = CombatState.PlayerChoice;
        EnablePlayerChoice();
    }

    private IEnumerator HandleDamageAndContinue(float damage) {
        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (enemyImage != null)
            enemyImage.gameObject.SetActive(true);

        yield return StartCoroutine(ShowDamageText(damage));

        if (currEnemyHp <= 0) {
            EndCombat();
            yield break;
        }

        RegisterUnlock("question");

        state = CombatState.PlayerChoice;
        EnablePlayerChoice();
    }

    // COROUTINES
    private IEnumerator ShowDialogue() {
        textbox.SetActive(true);
        questionBox.SetActive(false);

        if (questionPanel != null)
            questionPanel.SetActive(false);
        if (enemyImage != null)
            enemyImage.gameObject.SetActive(true);

        while (dialogueIndex < currEnemy.dialogue.Length) {
            yield return StartCoroutine(TypeText(currEnemy.dialogue[dialogueIndex].text));
            waitingForEnter = true;

            while (waitingForEnter) yield return null;

            dialogueIndex++;
        }

        state = CombatState.PlayerChoice;
        EnablePlayerChoice();
    }

    private IEnumerator TypeText(string fullText) {
        dialogueText.text = "";
        skipTypewriter = false;

        foreach (var c in fullText) {
            if (skipTypewriter) {
                dialogueText.text = fullText;
                break;
            }

            dialogueText.text += c;

            yield return new WaitForSeconds(typewriterSpeed);
        }
    }

    private IEnumerator ShowTipCoroutine(string tip) {
        textbox.SetActive(true);
        questionBox.SetActive(false);

        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (enemyImage != null)
            enemyImage.gameObject.SetActive(true);

        yield return StartCoroutine(TypeText(tip));

        waitingForEnter = true;
        while (waitingForEnter) yield return null;

        state = CombatState.PlayerChoice;
        EnablePlayerChoice();
    }

    private IEnumerator ShowDamageText(float damageAmount) {
        if (damageText == null || enemyImage == null)
            yield break;

        if (currEnemy.banter != null && currEnemy.banter.Length > 0) {
            var randomIndex = Random.Range(0, currEnemy.banter.Length);
            dialogueText.text = currEnemy.banter[randomIndex];
        }
        else {
            dialogueText.text = "";
        }

        var hitClip = Resources.Load<AudioClip>("Audio/Combat/hit");
        if (hitClip != null && sfxSource != null) {
            sfxSource.clip = hitClip;
            sfxSource.Play();
        }

        isButtonEnabled = false;

        var waitTime = 0.75f;
        while (sfxSource != null && sfxSource.isPlaying && sfxSource.time < waitTime)
            yield return null;

        damageText.text = $"-{damageAmount}";
        damageText.gameObject.SetActive(true);

        var damageRect = damageText.rectTransform;
        var damageOriginalPos = damageRect.anchoredPosition;

        var enemyRect = enemyImage.rectTransform;
        var enemyOriginalPos = enemyRect.anchoredPosition;

        var bounceDuration = 0.4f;
        var totalDuration = 1.0f;
        var jumpHeight = 40f;
        var shakeAmount = 20f;
        var shakeCount = 3;

        var elapsed = 0f;

        while (elapsed < bounceDuration) {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / bounceDuration);

            var yOffset = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            damageRect.anchoredPosition = damageOriginalPos + Vector2.up * yOffset;

            var xOffset = Mathf.Sin(t * Mathf.PI * shakeCount) * shakeAmount;
            enemyRect.anchoredPosition = enemyOriginalPos + Vector2.right * xOffset;

            yield return null;
        }

        damageRect.anchoredPosition = damageOriginalPos;
        enemyRect.anchoredPosition = enemyOriginalPos;

        yield return new WaitForSeconds(totalDuration - bounceDuration);

        damageText.gameObject.SetActive(false);

        isButtonEnabled = true;
    }

    private IEnumerator SlideBossImage(Vector2 targetAnchoredPosition, float duration = 0.3f) {
        var enemyRect = enemyImage.rectTransform;
        var start = enemyRect.anchoredPosition;
        var elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            enemyRect.anchoredPosition = Vector2.Lerp(start, targetAnchoredPosition, t);
            yield return null;
        }

        enemyRect.anchoredPosition = targetAnchoredPosition;
    }

    private IEnumerator QuestionTimerRoutine(float duration) {
        var timer = duration;

        while (timer > 0f) {
            timer -= Time.deltaTime;

            if (damageBar != null)
                damageBar.value = Mathf.Clamp01(timer / duration);

            yield return null;
        }

        if (damageBar != null)
            damageBar.value = 0f;
    }

    // Temporary POCO to store QuestionData and associated unitId
    private class QuestionWithUnit {
        public int UnitId { get; }
        public QuestionData Question { get; }
        public QuestionWithUnit(int unitId, QuestionData question) {
            UnitId = unitId;
            Question = question;
        }
    }

    // MISC
    private void StartQuestion() {
        isButtonEnabled = false;
        isQuestionEnabled = true;
        state = CombatState.Question;

        var spriteList = new List<Sprite>();
        var questionWithUnits = new List<QuestionWithUnit>();

        foreach (var unitId in currEnemy.unit) {
            Sprite sprite;
            var q = QuestionLoader.GetRandomQuestion(unitId, out sprite);

            if (q != null) {
                spriteList.Add(sprite);
                questionWithUnits.Add(new QuestionWithUnit(unitId, q));
            }
        }

        var randomIndex = Random.Range(0, questionWithUnits.Count);

        currentQuestion = questionWithUnits[randomIndex].Question;
        questionDebugObject.text = $"U: {questionWithUnits[randomIndex].UnitId} Q: {currentQuestion.spriteIndex}";
        var questionSprite = spriteList[randomIndex];

        correctAnswerIndex = currentQuestion.correct;

        textbox.SetActive(false);
        questionBox.SetActive(true);
        questionImage.sprite = questionSprite;
        questionImage.preserveAspect = true;

        if (questionPanel != null && questionImage != null) {
            var panelRect = questionPanel.GetComponent<RectTransform>();
            var imageRect = questionImage.rectTransform;

            if (panelRect != null && imageRect != null) imageRect.sizeDelta = panelRect.rect.size;
        }

        if (questionPanel != null)
            questionPanel.SetActive(true);

        if (enemyImage != null) {
            enemyImage.gameObject.SetActive(true);

            var enemyRect = enemyImage.rectTransform;

            previousBossPosition = enemyRect.anchoredPosition;

            var slideAmount = 600f;
            var targetPos = previousBossPosition + new Vector2(-slideAmount, 0);

            StopCoroutine("SlideBossImage");
            StartCoroutine(SlideBossImage(targetPos));
        }

        if (questionPanel != null)
            questionPanel.SetActive(true);

        if (damageBar != null)
            damageBar.value = 1f;

        questionTimeExpired = false;
        if (questionTimerCoroutine != null)
            StopCoroutine(questionTimerCoroutine);

        questionTimerCoroutine = StartCoroutine(QuestionTimerRoutine(currEnemy.qt));

        // TODO: Find better implementation.
        // if (enemyImage != null)
        // enemyImage.gameObject.SetActive(false);

        questionButtonIndex = 0;
        UpdateQuestionButtonSprites();
    }

    private void NextDialogue() {
        dialogueIndex = 0;
        state = CombatState.Dialogue;

        StartCoroutine(ShowDialogue());
    }

    private void UseSelectedItem() {
        if (displayableItems.Count == 0) return;
        var selectedItem = displayableItems[itemIndex];

        if (GameManager.GetInstance().GetItemAmount(selectedItem.itemName) <= 0) return;

        // Example: Remove one from inventory
        GameManager.GetInstance().RemoveItem(selectedItem.itemName);

        // TODO: Apply item effect here (e.g., heal, buff, etc.)
        // For now, just show a message
        // StartCoroutine(ShowItemUsedCoroutine(selectedItem.itemName));

        // Hide item panel
        if (itemPanel != null)
            itemPanel.SetActive(false);
    }

    private void EnablePlayerChoice() {
        isButtonEnabled = true;
        buttonIndex = 0;

        UpdateButtonSprites();

        textbox.SetActive(true);
        questionBox.SetActive(false);
        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (enemyImage != null)
            enemyImage.gameObject.SetActive(true);
    }

    private void EnemyAttack() {
        playerLives -= 1;
        playerLivesText.text = "Lives: " + playerLives;

        if (playerLives <= 0) {
            GameOver();
            return;
        }

        state = CombatState.PlayerChoice;
        EnablePlayerChoice();
    }

    private void ShowTip() {
        isButtonEnabled = false;

        state = CombatState.ShowTip;

        var index = Random.Range(0, currEnemy.unit.Length);
        var randomUnit = currEnemy.unit[index];
        var tip = TipLoader.GetRandomTip(randomUnit);

        StartCoroutine(ShowTipCoroutine(tip));
    }

    private void ShowItems() {
        isButtonEnabled = false;
        state = CombatState.ShowItems;
        itemIndex = 0;

        if (itemPanel != null)
            itemPanel.SetActive(true);
        if (textbox != null)
            textbox.SetActive(false);
        if (questionBox != null)
            questionBox.SetActive(false);
        if (questionPanel != null)
            questionPanel.SetActive(false);
        if (starText != null)
            starText.SetActive(false);

        UpdateItemOptions();
    }

    private void EndCombat() {
        state = CombatState.End;

        Debug.Log("Enemy Defeated");

        GameManager.instance.isBattlePlaying = false;

        if (currEnemy.id.ToLower() == "chen" || currEnemy.name.ToLower() == "chen") Application.Quit();

        if (currEnemy.id.ToLower() == "aureli" || currEnemy.name.ToLower() == "aureli")
            GameManager.instance.hasWonGame = true;

        SceneManager.LoadScene(GameManager.GetInstance().previousScene);
    }

    private void GameOver() {
        SceneManager.LoadScene("FailedGame");
    }

    // Helper Methods
    private bool IsActionUnlocked(string action) {
        return actionLocks.TryGetValue(action, out var unlocked) && unlocked;
    }

    private void RegisterLock(string action) {
        foreach (var key in new List<string>(actionLocks.Keys))
            actionLocks[key] = false;

        actionLocks[action] = true;
    }

    private void RegisterUnlock(string action) {
        foreach (var key in new List<string>(actionLocks.Keys))
            actionLocks[key] = true;
    }

#pragma warning disable
    private void DebugLocks() {
        return;

        foreach (var key in new List<string>(actionLocks.Keys))
            Debug.Log(key + " -> " + actionLocks[key]);
    }
#pragma warning restore
}