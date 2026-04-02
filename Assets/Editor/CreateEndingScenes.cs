using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Editor utility to create the EndingWin and EndingLose scenes with all UI wired up.
/// Run via menu: Tools > Create Ending Scenes
/// Safe to delete this file after scenes are created.
/// </summary>
public class CreateEndingScenes
{
    [MenuItem("Tools/Create Ending Scenes")]
    public static void CreateScenes()
    {
        CreateEndingScene("EndingWin", new string[]
        {
            "[Win Epilogue 1] The ink recedes. The Ink Blot King is no more. The scroll glows softly, its words intact.",
            "[Win Epilogue 2] The desk is quiet now. The coder stirs, unaware of the battle that saved everything.",
            "[Win Epilogue 3] You did it. The defenders rest. The story is safe... for now."
        });

        CreateEndingScene("EndingLose", new string[]
        {
            "[Lose Epilogue 1] The ink spreads. The scroll's words blur and dissolve, consumed by darkness.",
            "[Lose Epilogue 2] The desk falls silent. The coder will wake to find their work erased, never knowing what happened.",
            "[Lose Epilogue 3] The defenders fought bravely, but it wasn't enough. The story is lost."
        });

        // Add scenes to build settings
        AddScenesToBuildSettings();

        Debug.Log("Ending scenes created and added to build settings!");
    }

    private static void CreateEndingScene(string sceneName, string[] storyTexts)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // --- Canvas ---
        GameObject canvasObj = new GameObject("EndingCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // --- Background Image ---
        GameObject bgObj = new GameObject("BackgroundImage");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = sceneName == "EndingWin" ? new Color(0.1f, 0.1f, 0.15f) : new Color(0.05f, 0.02f, 0.02f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        StretchFull(bgRect);

        // --- Story Text ---
        GameObject storyTextObj = new GameObject("StoryText");
        storyTextObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI storyTMP = storyTextObj.AddComponent<TextMeshProUGUI>();
        storyTMP.text = "";
        storyTMP.fontSize = 32;
        storyTMP.color = Color.white;
        storyTMP.alignment = TextAlignmentOptions.Center;
        storyTMP.enableWordWrapping = true;
        RectTransform storyRect = storyTextObj.GetComponent<RectTransform>();
        storyRect.anchorMin = new Vector2(0.15f, 0.3f);
        storyRect.anchorMax = new Vector2(0.85f, 0.7f);
        storyRect.offsetMin = Vector2.zero;
        storyRect.offsetMax = Vector2.zero;

        // --- Click Prompt ---
        GameObject clickPromptObj = new GameObject("ClickPrompt");
        clickPromptObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI clickTMP = clickPromptObj.AddComponent<TextMeshProUGUI>();
        clickTMP.text = "Click to continue...";
        clickTMP.fontSize = 20;
        clickTMP.color = new Color(1f, 1f, 1f, 0.6f);
        clickTMP.alignment = TextAlignmentOptions.Center;
        RectTransform clickRect = clickPromptObj.GetComponent<RectTransform>();
        clickRect.anchorMin = new Vector2(0.3f, 0.1f);
        clickRect.anchorMax = new Vector2(0.7f, 0.18f);
        clickRect.offsetMin = Vector2.zero;
        clickRect.offsetMax = Vector2.zero;
        clickPromptObj.SetActive(false);

        // --- Fade Panel ---
        GameObject fadeObj = new GameObject("FadePanel");
        fadeObj.transform.SetParent(canvasObj.transform, false);
        Image fadeImage = fadeObj.AddComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = false;
        CanvasGroup fadeCG = fadeObj.AddComponent<CanvasGroup>();
        fadeCG.alpha = 0f;
        fadeCG.blocksRaycasts = false;
        RectTransform fadeRect = fadeObj.GetComponent<RectTransform>();
        StretchFull(fadeRect);

        // --- Credits Container ---
        GameObject creditsObj = new GameObject("CreditsContainer");
        creditsObj.transform.SetParent(canvasObj.transform, false);
        RectTransform creditsRect = creditsObj.AddComponent<RectTransform>();
        // Anchor at top-center, pivot at top so it starts below screen
        creditsRect.anchorMin = new Vector2(0.2f, 1f);
        creditsRect.anchorMax = new Vector2(0.8f, 1f);
        creditsRect.pivot = new Vector2(0.5f, 1f);
        creditsRect.anchoredPosition = new Vector2(0f, 0f);
        creditsRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup vlg = creditsObj.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 30f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = creditsObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Credit entries
        string[] creditEntries = new string[]
        {
            "<size=48>DeskTD</size>",
            "",
            "<size=28>Game Design</size>\n<size=22>[Your Name]</size>",
            "<size=28>Programming</size>\n<size=22>[Your Name]</size>",
            "<size=28>Art & Animation</size>\n<size=22>[Your Name]</size>",
            "<size=28>Audio & Music</size>\n<size=22>[Your Name]</size>",
            "<size=28>Story & Writing</size>\n<size=22>[Your Name]</size>",
            "",
            "<size=28>Special Thanks</size>\n<size=22>[Names here]</size>",
            "",
            "<size=24>Thank you for playing!</size>"
        };

        for (int i = 0; i < creditEntries.Length; i++)
        {
            GameObject entry = new GameObject($"CreditEntry_{i}");
            entry.transform.SetParent(creditsObj.transform, false);
            TextMeshProUGUI tmp = entry.AddComponent<TextMeshProUGUI>();
            tmp.text = creditEntries[i];
            tmp.fontSize = 24;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.richText = true;

            LayoutElement le = entry.AddComponent<LayoutElement>();
            le.preferredHeight = string.IsNullOrEmpty(creditEntries[i]) ? 60f : -1f;
        }

        creditsObj.SetActive(false);

        // --- Main Menu Button ---
        GameObject btnObj = new GameObject("MainMenuButton");
        btnObj.transform.SetParent(canvasObj.transform, false);
        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.2f, 0.25f);
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.3f, 0.3f, 0.4f);
        cb.pressedColor = new Color(0.15f, 0.15f, 0.2f);
        btn.colors = cb;

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.35f, 0.05f);
        btnRect.anchorMax = new Vector2(0.65f, 0.12f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnTMP = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnTMP.text = "Main Menu";
        btnTMP.fontSize = 28;
        btnTMP.color = Color.white;
        btnTMP.alignment = TextAlignmentOptions.Center;
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        StretchFull(btnTextRect);

        btnObj.SetActive(false);

        // --- Ending Controller ---
        GameObject controllerObj = new GameObject("EndingController");
        EndingSceneController controller = controllerObj.AddComponent<EndingSceneController>();

        // Wire up serialized fields via SerializedObject
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("backgroundImage").objectReferenceValue = bgImage;
        so.FindProperty("storyText").objectReferenceValue = storyTMP;

        SerializedProperty storyArray = so.FindProperty("storyPanelTexts");
        storyArray.arraySize = storyTexts.Length;
        for (int i = 0; i < storyTexts.Length; i++)
            storyArray.GetArrayElementAtIndex(i).stringValue = storyTexts[i];

        so.FindProperty("timeBetweenChars").floatValue = 0.03f;
        so.FindProperty("clickPrompt").objectReferenceValue = clickPromptObj;
        so.FindProperty("fadeCanvasGroup").objectReferenceValue = fadeCG;
        so.FindProperty("fadeDuration").floatValue = 1.5f;
        so.FindProperty("creditsContainer").objectReferenceValue = creditsRect;
        so.FindProperty("creditsScrollSpeed").floatValue = 60f;
        so.FindProperty("mainMenuButton").objectReferenceValue = btnObj;
        so.FindProperty("musicVolume").floatValue = 0.5f;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Wire button OnClick to controller.OnMainMenuClicked
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            btn.onClick,
            controller.OnMainMenuClicked
        );

        // Save scene
        string path = $"Assets/Scenes/{sceneName}.unity";
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"Created scene: {path}");
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void AddScenesToBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        string winPath = "Assets/Scenes/EndingWin.unity";
        string losePath = "Assets/Scenes/EndingLose.unity";

        bool hasWin = false, hasLose = false;
        foreach (var s in scenes)
        {
            if (s.path == winPath) hasWin = true;
            if (s.path == losePath) hasLose = true;
        }

        if (!hasWin) scenes.Add(new EditorBuildSettingsScene(winPath, true));
        if (!hasLose) scenes.Add(new EditorBuildSettingsScene(losePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("Build settings updated with ending scenes.");
    }
}
