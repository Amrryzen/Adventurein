using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PauseMenuCanvasSetup
{
    private const string ResourceFolder = "Assets/Resources";
    private const string PrefabPath = ResourceFolder + "/PauseMenuCanvas.prefab";
    private const string UiAssetFolder = "Assets/Scenes/Aset Game Scene/UI Aset/";

    [MenuItem("Assets/Adventurein/Build Pause Menu Canvas")]
    private static void BuildPauseMenuCanvas()
    {
        Canvas canvas = FindPauseCanvas();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog(
                "Pause Menu",
                "Buka scene Stage1 yang berisi Canvas/P_Pause sebelum membuat prefab pause menu.",
                "OK");
            return;
        }

        Transform panel = canvas.transform.Find("P_Pause");
        if (panel == null)
        {
            EditorUtility.DisplayDialog("Pause Menu", "Canvas tidak memiliki child P_Pause.", "OK");
            return;
        }

        Sprite panelSprite = LoadSprite("P_PauseAsset 54.png");
        Sprite resumeSprite = LoadSprite("B_ResumeAsset 51.png");
        Sprite restartSprite = LoadSprite("B_RestartAsset 53.png");
        Sprite exitSprite = LoadSprite("B_ExitAsset 52.png");
        Sprite sliderBackground = LoadSprite("BG_SliderAsset 55.png");
        Sprite sliderFill = LoadSprite("Slider_FillAsset 60.png");
        Sprite sliderHandle = LoadSprite("Control_sliderAsset 58.png");
        Sprite checkbox = LoadSprite("CheckboxAsset 59.png");

        if (panelSprite == null || resumeSprite == null || restartSprite == null || exitSprite == null ||
            sliderBackground == null || sliderFill == null || sliderHandle == null || checkbox == null)
        {
            EditorUtility.DisplayDialog(
                "Pause Menu",
                "Satu atau lebih sprite pause menu belum ter-import sebagai Sprite.",
                "OK");
            return;
        }

        AturSprite(panel.GetComponent<Image>(), panelSprite);
        AturTombol(panel, "B_Resume", resumeSprite);
        AturTombol(panel, "B_Restart", restartSprite);
        AturTombol(panel, "B_Exit", exitSprite);

        Slider musicSlider = panel.Find("Slider")?.GetComponent<Slider>();
        if (musicSlider == null)
        {
            EditorUtility.DisplayDialog("Pause Menu", "P_Pause tidak memiliki Slider musik.", "OK");
            return;
        }

        AturSlider(musicSlider, sliderBackground, sliderFill, sliderHandle);

        Transform sfxSliderTransform = panel.Find("Slider_SFX");
        Slider sfxSlider;
        if (sfxSliderTransform == null)
        {
            GameObject sliderObject = Object.Instantiate(musicSlider.gameObject, panel);
            sliderObject.name = "Slider_SFX";
            RectTransform rect = sliderObject.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(
                rect.anchoredPosition.x,
                musicSlider.GetComponent<RectTransform>().anchoredPosition.y - 199f);
            sfxSlider = sliderObject.GetComponent<Slider>();
        }
        else
        {
            sfxSlider = sfxSliderTransform.GetComponent<Slider>();
        }

        AturSlider(sfxSlider, sliderBackground, sliderFill, sliderHandle);
        AturToggle(panel, "Toggle_MuteBGM", checkbox, 270f, 142f);
        AturToggle(panel, "Toggle_MuteSFX", checkbox, 270f, -57f);

        if (canvas.transform.localScale == Vector3.zero)
            canvas.transform.localScale = Vector3.one;

        panel.gameObject.SetActive(false);
        if (!AssetDatabase.IsValidFolder(ResourceFolder))
            AssetDatabase.CreateFolder("Assets", "Resources");

        PrefabUtility.SaveAsPrefabAsset(canvas.gameObject, PrefabPath);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Pause Canvas prefab dibuat: {PrefabPath}");
        EditorUtility.DisplayDialog(
            "Pause Menu",
            "Pause Canvas siap. Stage* akan menggunakan UI ini otomatis, kecuali Lobby.",
            "OK");
    }

    private static Canvas FindPauseCanvas()
    {
        return Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.transform.Find("P_Pause") != null);
    }

    private static Sprite LoadSprite(string fileName)
    {
        string path = UiAssetFolder + fileName;
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static void AturSprite(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.preserveAspect = true;
        EditorUtility.SetDirty(image);
    }

    private static void AturTombol(Transform parent, string nama, Sprite sprite)
    {
        Transform buttonTransform = parent.Find(nama);
        if (buttonTransform == null)
            return;

        Button button = buttonTransform.GetComponent<Button>();
        Image image = buttonTransform.GetComponent<Image>();
        AturSprite(image, sprite);
        if (button != null && image != null)
            button.targetGraphic = image;
    }

    private static void AturSlider(
        Slider slider,
        Sprite backgroundSprite,
        Sprite fillSprite,
        Sprite handleSprite)
    {
        if (slider == null)
            return;

        AturSprite(slider.transform.Find("Background")?.GetComponent<Image>(), backgroundSprite);
        AturSprite(slider.transform.Find("Fill Area/Fill")?.GetComponent<Image>(), fillSprite);
        AturSprite(slider.transform.Find("Handle Slide Area/Handle")?.GetComponent<Image>(), handleSprite);
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        EditorUtility.SetDirty(slider);
    }

    private static void AturToggle(
        Transform parent,
        string nama,
        Sprite sprite,
        float x,
        float y)
    {
        Transform existing = parent.Find(nama);
        Toggle toggle;

        if (existing == null)
        {
            GameObject toggleObject = new GameObject(
                nama,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Toggle));
            toggleObject.layer = parent.gameObject.layer;
            toggleObject.transform.SetParent(parent, false);

            RectTransform rect = toggleObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(44f, 48f);

            Image checkmark = new GameObject(
                "Checkmark",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)).GetComponent<Image>();
            checkmark.transform.SetParent(toggleObject.transform, false);
            RectTransform checkmarkRect = checkmark.rectTransform;
            checkmarkRect.anchorMin = new Vector2(0.2f, 0.2f);
            checkmarkRect.anchorMax = new Vector2(0.8f, 0.8f);
            checkmarkRect.offsetMin = Vector2.zero;
            checkmarkRect.offsetMax = Vector2.zero;
            checkmark.sprite = sprite;
            checkmark.preserveAspect = true;
            checkmark.raycastTarget = false;
            checkmark.color = new Color(1f, 0.92f, 0.72f, 1f);
            checkmark.canvasRenderer.SetAlpha(0f);

            Image background = toggleObject.GetComponent<Image>();
            background.sprite = sprite;
            background.preserveAspect = true;
            background.raycastTarget = true;

            toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = checkmark;
            toggle.isOn = false;
        }
        else
        {
            toggle = existing.GetComponent<Toggle>();
            RectTransform rect = existing.GetComponent<RectTransform>();
            if (rect != null)
                rect.anchoredPosition = new Vector2(x, y);

            Image background = existing.GetComponent<Image>();
            AturSprite(background, sprite);
            if (toggle != null)
                toggle.targetGraphic = background;
        }

        EditorUtility.SetDirty(toggle);
    }
}
