using System.Collections.Generic;
using System.IO;
using FoodsBasketGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FoodsBasketEditor
{
    public static class FoodsBasketSceneBuilder
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string AutoBuildFlag = "FoodsBasket_AutoBuild_v2";

        [MenuItem("Tools/Foods Basket/Build Simple Scene")]
        public static void BuildPlayableScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before building the Foods Basket scene.");
                return;
            }

            EnsureTexturesAreSprites();

            Scene scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            ClearScene(scene);

            CreateCamera();
            Sprite backgroundSprite = LoadSprite("background");
            Sprite basketSprite = LoadSprite("basket");
            Sprite heartSprite = LoadSprite("heart");

            if (backgroundSprite != null)
            {
                CreateBackground(backgroundSprite);
            }

            Canvas canvas = CreateCanvas();
            EnsureEventSystem();

            GameObject systems = new GameObject("GameSystems");
            FoodsBasketGameController controller = systems.AddComponent<FoodsBasketGameController>();
            FoodSpawner spawner = systems.AddComponent<FoodSpawner>();
            NutritionMeterSystem nutrition = systems.AddComponent<NutritionMeterSystem>();

            GameObject basketObject = CreateBasket(basketSprite);
            BasketZone basketZone = basketObject.AddComponent<BasketZone>();

            Slider glucoseSlider = CreateHorizontalSlider(canvas.transform, "GlucoseBar", new Vector2(0.985f, 0.92f), new Color(0.93f, 0.33f, 0.33f));
            Slider fatsSlider = CreateHorizontalSlider(canvas.transform, "FatsBar", new Vector2(0.985f, 0.83f), new Color(0.95f, 0.79f, 0.24f));
            Slider carbsSlider = CreateHorizontalSlider(canvas.transform, "CarbsBar", new Vector2(0.985f, 0.74f), new Color(0.27f, 0.78f, 0.37f));
            CreateLabel(canvas.transform, "GlucoseLabel", "GLUCOSE", new Vector2(0.77f, 0.92f));
            CreateLabel(canvas.transform, "FatsLabel", "FATS", new Vector2(0.77f, 0.83f));
            CreateLabel(canvas.transform, "CarbsLabel", "CARBS", new Vector2(0.77f, 0.74f));
            Image[] hearts = CreateHearts(canvas.transform, heartSprite);
            (GameObject startPanel, RectTransform startButtonRect) = CreateStartPanel(canvas.transform, controller);
            (GameObject gameOverPanel, RectTransform replayButtonRect) = CreateGameOverPanel(canvas.transform, controller);

            nutrition.Configure(glucoseSlider, fatsSlider, carbsSlider);
            spawner.SetFoods(BuildFoodDefinitions());
            controller.Configure(spawner, basketZone, nutrition, hearts, startPanel, startButtonRect, gameOverPanel, replayButtonRect);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeObject = systems;
            Debug.Log("FoodsBasket simple scene created successfully.");
        }

        [InitializeOnLoadMethod]
        private static void AutoBuildOnFirstLoad()
        {
            if (EditorPrefs.GetBool(AutoBuildFlag))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                if (Object.FindObjectOfType<FoodsBasketGameController>() != null)
                {
                    EditorPrefs.SetBool(AutoBuildFlag, true);
                    return;
                }

                BuildPlayableScene();
                EditorPrefs.SetBool(AutoBuildFlag, true);
            };
        }

        private static void EnsureTexturesAreSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string lower = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

                if (lower == "background" || lower == "basket" || IsFoodAsset(lower))
                {
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null)
                    {
                        continue;
                    }

                    bool changed = false;

                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }

                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }

                    if (changed)
                    {
                        importer.SaveAndReimport();
                    }
                }
            }
        }

        private static void ClearScene(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.74f, 0.87f, 0.96f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateBackground(Sprite sprite)
        {
            GameObject background = new GameObject("Background");
            SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -10;
            background.transform.position = new Vector3(0f, 0f, 5f);

            Vector2 spriteSize = sprite.bounds.size;
            Camera camera = Camera.main;
            float worldHeight = camera.orthographicSize * 2f;
            float worldWidth = worldHeight * camera.aspect;
            background.transform.localScale = new Vector3(worldWidth / spriteSize.x, worldHeight / spriteSize.y, 1f);
        }

        private static GameObject CreateBasket(Sprite basketSprite)
        {
            GameObject basketObject = new GameObject("Basket");
            basketObject.transform.position = new Vector3(0f, -4.35f, 0f);

            SpriteRenderer renderer = basketObject.AddComponent<SpriteRenderer>();
            renderer.sprite = basketSprite;
            renderer.sortingOrder = 1;

            BoxCollider2D collider2D = basketObject.AddComponent<BoxCollider2D>();
            collider2D.isTrigger = true;
            collider2D.size = new Vector2(15.8f, 4.1f);
            collider2D.offset = new Vector2(0f, 1.2f);

            if (basketSprite != null)
            {
                Vector2 size = basketSprite.bounds.size;
                if (size.x > 0.01f && size.y > 0.01f)
                {
                    basketObject.transform.localScale = new Vector3(14.8f / size.x, 4.5f / size.y, 1f);
                }
            }

            return basketObject;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static Slider CreateHorizontalSlider(Transform parent, string objectName, Vector2 anchor, Color fillColor)
        {
            Slider slider = DefaultControls.CreateSlider(new DefaultControls.Resources()).GetComponent<Slider>();
            slider.name = objectName;
            slider.transform.SetParent(parent, false);
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 5f;
            slider.value = 0f;
            slider.wholeNumbers = false;

            RectTransform rectTransform = slider.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(1f, 0.5f);
            rectTransform.sizeDelta = new Vector2(520f, 56f);
            rectTransform.anchoredPosition = Vector2.zero;

            Image background = slider.GetComponentInChildren<Image>();
            if (background != null)
            {
                background.color = new Color(0.16f, 0.14f, 0.11f, 0.92f);
            }

            if (slider.fillRect != null)
            {
                Image fill = slider.fillRect.GetComponent<Image>();
                if (fill != null)
                {
                    fill.color = fillColor;
                    fill.type = Image.Type.Simple;
                }
            }

            if (slider.handleRect != null)
            {
                slider.handleRect.gameObject.SetActive(false);
            }

            return slider;
        }

        private static void CreateLabel(Transform parent, string objectName, string textValue, Vector2 anchor)
        {
            GameObject labelObject = new GameObject(objectName);
            labelObject.transform.SetParent(parent, false);

            Text label = labelObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 34;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.text = textValue;

            Outline outline = labelObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.08f, 0.06f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);

            RectTransform rectTransform = label.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.sizeDelta = new Vector2(260f, 52f);
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private static Image[] CreateHearts(Transform parent, Sprite heartSprite)
        {
            Image[] hearts = new Image[5];

            for (int i = 0; i < hearts.Length; i++)
            {
                GameObject heartObject = new GameObject("Heart" + (i + 1));
                heartObject.transform.SetParent(parent, false);

                Image image = heartObject.AddComponent<Image>();
                image.sprite = heartSprite;
                image.preserveAspect = true;

                RectTransform rectTransform = image.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.03f + (i * 0.045f), 0.94f);
                rectTransform.anchorMax = new Vector2(0.03f + (i * 0.045f), 0.94f);
                rectTransform.pivot = new Vector2(0f, 0.5f);
                rectTransform.sizeDelta = new Vector2(54f, 54f);
                rectTransform.anchoredPosition = Vector2.zero;

                hearts[i] = image;
            }

            return hearts;
        }

        private static (GameObject panel, RectTransform buttonRect) CreateGameOverPanel(Transform parent, FoodsBasketGameController controller)
        {
            GameObject panelObject = new GameObject("GameOverPanel");
            panelObject.transform.SetParent(parent, false);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.72f);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 320f);
            panelRect.anchoredPosition = Vector2.zero;

            CreatePanelText(panelObject.transform, "GameOverText", "GAME OVER", new Vector2(0.5f, 0.7f), 54);
            CreatePanelText(panelObject.transform, "PlayAgainText", "Play Again?", new Vector2(0.5f, 0.5f), 34);

            GameObject buttonObject = new GameObject("ReplayButton");
            buttonObject.transform.SetParent(panelObject.transform, false);

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.95f, 0.91f, 0.75f, 0.95f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(controller.RestartGame);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.24f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.24f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(300f, 84f);
            buttonRect.anchoredPosition = Vector2.zero;

            CreateButtonText(buttonObject.transform, "ReplayButtonText", "REPLAY");
            panelObject.SetActive(false);
            return (panelObject, buttonRect);
        }

        private static (GameObject panel, RectTransform buttonRect) CreateStartPanel(Transform parent, FoodsBasketGameController controller)
        {
            GameObject panelObject = new GameObject("StartPanel");
            panelObject.transform.SetParent(parent, false);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.62f);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 320f);
            panelRect.anchoredPosition = Vector2.zero;

            CreatePanelText(panelObject.transform, "WelcomeText", "WELCOME", new Vector2(0.5f, 0.7f), 54);
            CreatePanelText(panelObject.transform, "StartPromptText", "Foods Basket", new Vector2(0.5f, 0.5f), 34);

            GameObject buttonObject = new GameObject("StartButton");
            buttonObject.transform.SetParent(panelObject.transform, false);

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.95f, 0.91f, 0.75f, 0.95f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(controller.StartGame);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.24f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.24f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(300f, 84f);
            buttonRect.anchoredPosition = Vector2.zero;

            CreateButtonText(buttonObject.transform, "StartButtonText", "START");
            return (panelObject, buttonRect);
        }

        private static void CreatePanelText(Transform parent, string objectName, string value, Vector2 anchor, int fontSize)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;

            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(3f, -3f);

            RectTransform rectTransform = text.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(520f, 70f);
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private static void CreateButtonText(Transform parent, string objectName, string value)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 34;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.18f, 0.14f, 0.1f);
            text.text = value;

            RectTransform rectTransform = text.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(260f, 60f);
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private static List<FoodDefinition> BuildFoodDefinitions()
        {
            string[] orderedNames =
            {
                "apple", "banana", "tomato", "strawberry", "raspberry_transparent", "berries",
                "broccoli", "lettuce", "onion", "beans", "egg", "chicken", "fush",
                "avocado", "almonds", "pasta", "burger", "fries", "cake", "donut",
                "candy", "lollipop", "soda", "bubbletea", "pancakes"
            };

            List<FoodDefinition> result = new List<FoodDefinition>();

            foreach (string assetName in orderedNames)
            {
                Sprite sprite = LoadSprite(assetName);
                if (sprite == null)
                {
                    continue;
                }

                result.Add(CreateDefinition(assetName, sprite));
            }

            return result;
        }

        private static FoodDefinition CreateDefinition(string assetName, Sprite sprite)
        {
            FoodDefinition definition = new FoodDefinition
            {
                id = assetName,
                displayName = Nicify(assetName),
                sprite = sprite,
                glucosePoints = 0f,
                carbsPoints = 0f,
                fatsPoints = 0f,
                moveSpeed = 2f,
                visualScale = 0.38f
            };

            switch (assetName)
            {
                case "broccoli":
                    definition.glucosePoints = 0f;
                    definition.carbsPoints = 0f;
                    definition.fatsPoints = 0f;
                    definition.moveSpeed = 1.8f;
                    definition.visualScale = 0.34f;
                    break;
                case "tomato":
                    definition.glucosePoints = 1f;
                    definition.carbsPoints = 0f;
                    definition.fatsPoints = 0f;
                    definition.moveSpeed = 1.8f;
                    definition.visualScale = 0.34f;
                    break;
                case "lettuce":
                    definition.glucosePoints = 0f;
                    definition.carbsPoints = 0f;
                    definition.fatsPoints = 0f;
                    definition.moveSpeed = 1.8f;
                    definition.visualScale = 0.34f;
                    break;
                case "onion":
                    definition.glucosePoints = 1f;
                    definition.carbsPoints = 0f;
                    definition.fatsPoints = 0f;
                    definition.moveSpeed = 1.8f;
                    definition.visualScale = 0.34f;
                    break;
                case "avocado":
                    definition.glucosePoints = 0f;
                    definition.carbsPoints = 0f;
                    definition.fatsPoints = 3f;
                    definition.moveSpeed = 1.7f;
                    definition.visualScale = 0.35f;
                    break;
                case "garlic":
                    definition.glucosePoints = 0f;
                    definition.carbsPoints = 0f;
                    definition.fatsPoints = 0f;
                    break;
                case "strawberry":
                case "raspberry_transparent":
                case "berries":
                    definition.glucosePoints = 1f;
                    definition.carbsPoints = 1f;
                    definition.fatsPoints = 0f;
                    definition.moveSpeed = 2.1f;
                    definition.visualScale = 0.36f;
                    break;
                case "apple":
                    definition.glucosePoints = 2f;
                    definition.carbsPoints = 1f;
                    definition.fatsPoints = 0f;
                    definition.moveSpeed = 2.1f;
                    definition.visualScale = 0.36f;
                    break;
                case "banana":
                    definition.glucosePoints = 3f;
                    definition.carbsPoints = 2f;
                    definition.fatsPoints = 0f;
                    definition.moveSpeed = 2.1f;
                    definition.visualScale = 0.36f;
                    break;
                case "beans":
                    definition.glucosePoints = 1f;
                    definition.carbsPoints = 3f;
                    definition.fatsPoints = 0f;
                    definition.moveSpeed = 1.9f;
                    definition.visualScale = 0.34f;
                    break;
                case "egg":
                case "fush":
                case "chicken":
                    definition.glucosePoints = 0f;
                    definition.carbsPoints = 0f;
                    definition.fatsPoints = 2f;
                    definition.moveSpeed = 2f;
                    definition.visualScale = 0.36f;
                    break;
                case "almonds":
                    definition.glucosePoints = 0f;
                    definition.carbsPoints = 0f;
                    definition.fatsPoints = 3f;
                    definition.moveSpeed = 1.7f;
                    definition.visualScale = 0.35f;
                    break;
                case "pasta":
                    definition.glucosePoints = 2f;
                    definition.carbsPoints = 4f;
                    definition.fatsPoints = 0f;
                    definition.moveSpeed = 2.15f;
                    definition.visualScale = 0.39f;
                    break;
                case "candy":
                    definition.glucosePoints = 5f;
                    definition.carbsPoints = 1f;
                    definition.fatsPoints = 0f;
                    definition.moveSpeed = 2.45f;
                    definition.visualScale = 0.38f;
                    break;
                case "lollipop":
                    definition.glucosePoints = 5f;
                    definition.carbsPoints = 1f;
                    definition.fatsPoints = 0f;
                    definition.moveSpeed = 2.45f;
                    definition.visualScale = 0.38f;
                    break;
                case "cake":
                    definition.glucosePoints = 5f;
                    definition.carbsPoints = 3f;
                    definition.fatsPoints = 3f;
                    definition.moveSpeed = 2.45f;
                    definition.visualScale = 0.38f;
                    break;
                case "donut":
                    definition.glucosePoints = 5f;
                    definition.carbsPoints = 3f;
                    definition.fatsPoints = 3f;
                    definition.moveSpeed = 2.45f;
                    definition.visualScale = 0.38f;
                    break;
                case "pancakes":
                    definition.glucosePoints = 5f;
                    definition.carbsPoints = 3f;
                    definition.fatsPoints = 3f;
                    definition.moveSpeed = 2.15f;
                    definition.visualScale = 0.39f;
                    break;
                case "soda":
                    definition.glucosePoints = 5f;
                    definition.carbsPoints = 0f;
                    definition.fatsPoints = 0f;
                    definition.moveSpeed = 2.45f;
                    definition.visualScale = 0.38f;
                    break;
                case "burger":
                    definition.glucosePoints = 3f;
                    definition.carbsPoints = 3f;
                    definition.fatsPoints = 5f;
                    definition.moveSpeed = 2.35f;
                    definition.visualScale = 0.4f;
                    break;
                case "fries":
                    definition.glucosePoints = 2f;
                    definition.carbsPoints = 3f;
                    definition.fatsPoints = 5f;
                    definition.moveSpeed = 2.35f;
                    definition.visualScale = 0.4f;
                    break;
                case "bubbletea":
                    definition.glucosePoints = 4f;
                    definition.carbsPoints = 0f;
                    definition.fatsPoints = 1f;
                    definition.moveSpeed = 2.45f;
                    definition.visualScale = 0.38f;
                    break;
            }

            return definition;
        }

        private static string Nicify(string assetName)
        {
            string clean = assetName.Replace("_transparent", string.Empty).Replace("_", " ");
            return char.ToUpper(clean[0]) + clean.Substring(1);
        }

        private static bool IsFoodAsset(string assetName)
        {
            return assetName != "background" && assetName != "basket";
        }

        private static Sprite LoadSprite(string baseName)
        {
            string[] guids = AssetDatabase.FindAssets(baseName + " t:Sprite", new[] { "Assets" });
            if (guids.Length == 0)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
