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
            Sprite infoSprite = LoadSprite("info-removebg-preview");
            Sprite startTitleSprite = LoadSprite("start-game-reference");
            Sprite startButtonSprite = LoadSprite("start-button-reference");
            Sprite gameOverSprite = LoadSprite("game-over-reference");

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

            Slider glucoseSlider = CreateHorizontalSlider(canvas.transform, "GlucoseBar", new Vector2(0.985f, 0.84f), new Color(0.93f, 0.33f, 0.33f));
            Slider fatsSlider = CreateHorizontalSlider(canvas.transform, "FatsBar", new Vector2(0.985f, 0.75f), new Color(0.95f, 0.79f, 0.24f));
            Slider carbsSlider = CreateHorizontalSlider(canvas.transform, "CarbsBar", new Vector2(0.985f, 0.66f), new Color(0.27f, 0.78f, 0.37f));
            CreateLabel(canvas.transform, "GlucoseLabel", "GLUCOSE", new Vector2(0.77f, 0.84f));
            CreateLabel(canvas.transform, "FatsLabel", "FATS", new Vector2(0.77f, 0.75f));
            CreateLabel(canvas.transform, "CarbsLabel", "CARBS", new Vector2(0.77f, 0.66f));
            Text timerLabel = CreateLabel(canvas.transform, "TimerLabel", "Time: 00:00", new Vector2(0.03f, 0.87f), TextAnchor.MiddleLeft, 30, new Vector2(320f, 52f));
            Text bestTimeLabel = null;
            RectTransform infoButtonRect = CreateInfoButton(canvas.transform, controller, infoSprite);
            Text highScoreNoticeLabel = null;
            Image[] hearts = CreateHearts(canvas.transform, heartSprite);
            (GameObject startPanel, RectTransform startButtonRect, RectTransform highScoresButtonRect) = CreateStartPanel(canvas.transform, controller, startTitleSprite, startButtonSprite);
            (GameObject gameOverPanel, RectTransform replayButtonRect) = CreateGameOverPanel(canvas.transform, controller, gameOverSprite);
            GameObject highScoresPanel = null;
            RectTransform closeHighScoresButtonRect = null;
            Text highScoresText = null;
            (GameObject infoPanel, RectTransform closeInfoButtonRect, Text infoPanelText) = CreateInfoPanel(canvas.transform, controller);

            nutrition.Configure(glucoseSlider, fatsSlider, carbsSlider);
            spawner.SetFoods(BuildFoodDefinitions());
            controller.Configure(spawner, basketZone, nutrition, hearts, timerLabel, bestTimeLabel, highScoreNoticeLabel, infoButtonRect, infoPanel, closeInfoButtonRect, infoPanelText, startPanel, startButtonRect, highScoresButtonRect, gameOverPanel, replayButtonRect, highScoresPanel, closeHighScoresButtonRect, highScoresText);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeObject = systems;
            Debug.Log("FoodsBasket simple scene created successfully.");
        }

        private static void EnsureTexturesAreSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string lower = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

                if (lower == "background" || lower == "basket" || lower == "bricks" || lower == "start-game-reference" || lower == "start-button-reference" || lower == "game-over-reference" || IsFoodAsset(lower))
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
            camera.backgroundColor = Color.white;
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

        private static Text CreateLabel(Transform parent, string objectName, string textValue, Vector2 anchor)
        {
            return CreateLabel(parent, objectName, textValue, anchor, TextAnchor.MiddleLeft, 34, new Vector2(260f, 52f));
        }

        private static Text CreateLabel(Transform parent, string objectName, string textValue, Vector2 anchor, TextAnchor alignment, int fontSize, Vector2 size)
        {
            GameObject labelObject = new GameObject(objectName);
            labelObject.transform.SetParent(parent, false);

            Text label = labelObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = alignment;
            label.text = textValue;

            Outline outline = labelObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.08f, 0.06f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);

            RectTransform rectTransform = label.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = Vector2.zero;
            return label;
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

        private static (GameObject panel, RectTransform buttonRect) CreateGameOverPanel(Transform parent, FoodsBasketGameController controller, Sprite gameOverSprite)
        {
            GameObject panelObject = new GameObject("GameOverPanel");
            panelObject.transform.SetParent(parent, false);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(1f, 1f, 1f, 0f);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 360f);
            panelRect.anchoredPosition = Vector2.zero;

            CreateOverlay(panelObject.transform, "GameOverBlurOverlay", new Color(1f, 1f, 1f, 0.32f));

            if (gameOverSprite != null)
            {
                CreateSpriteImage(panelObject.transform, "GameOverReferenceImage", gameOverSprite, new Vector2(0.5f, 0.52f), new Vector2(520f, 520f));
            }
            else
            {
                CreatePanelText(panelObject.transform, "GameOverText", "GAME OVER", new Vector2(0.5f, 0.7f), 54);
                CreatePanelText(panelObject.transform, "PlayAgainText", "CONTINUE?", new Vector2(0.5f, 0.5f), 34);
            }

            GameObject buttonObject = new GameObject("ReplayButton");
            buttonObject.transform.SetParent(panelObject.transform, false);

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.95f, 0.91f, 0.75f, 0.95f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(controller.RestartGame);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.06f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.06f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(340f, 84f);
            buttonRect.anchoredPosition = Vector2.zero;

            CreateButtonText(buttonObject.transform, "ReplayButtonText", "CONTINUE", 28, new Color(0.18f, 0.14f, 0.1f));
            panelObject.SetActive(false);
            return (panelObject, buttonRect);
        }

        private static (GameObject panel, RectTransform startButtonRect, RectTransform highScoresButtonRect) CreateStartPanel(Transform parent, FoodsBasketGameController controller, Sprite startTitleSprite, Sprite startButtonSprite)
        {
            GameObject panelObject = new GameObject("StartPanel");
            panelObject.transform.SetParent(parent, false);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(1f, 1f, 1f, 0.28f);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 360f);
            panelRect.anchoredPosition = Vector2.zero;

            RectTransform cardRect = CreateSolidPanel(panelObject.transform, "WelcomeCard", new Vector2(760f, 420f), new Vector2(0f, 0f), new Color(0f, 0f, 0f, 0f));

            if (startTitleSprite != null)
            {
                CreateSpriteImage(cardRect, "StartTitleImage", startTitleSprite, new Vector2(0.5f, 0.73f), new Vector2(520f, 300f));
            }
            else
            {
                CreatePanelText(cardRect, "WelcomeText", "WELCOME", new Vector2(0.5f, 0.67f), 54);
                Text subtitleFallback = CreatePanelText(cardRect, "StartPromptText", "foods basket", new Vector2(0.5f, 0.49f), 28);
                subtitleFallback.fontStyle = FontStyle.Bold;
            }

            GameObject buttonObject = new GameObject("StartButton");
            buttonObject.transform.SetParent(cardRect, false);

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = Color.white;
            buttonImage.sprite = startButtonSprite;
            buttonImage.preserveAspect = true;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(controller.StartGame);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.25f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.25f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = startButtonSprite != null ? new Vector2(360f, 120f) : new Vector2(250f, 58f);
            buttonRect.anchoredPosition = Vector2.zero;

            if (startButtonSprite == null)
            {
                Outline startOutline = buttonObject.AddComponent<Outline>();
                startOutline.effectColor = new Color(0.83f, 0.55f, 0.61f, 0.9f);
                startOutline.effectDistance = new Vector2(2f, -2f);
                CreateButtonText(buttonObject.transform, "StartButtonText", "START", 32, new Color(0.45f, 0.53f, 0.47f, 1f));
            }

            return (panelObject, buttonRect, null);
        }

        private static (GameObject panel, RectTransform closeButtonRect, Text highScoresText) CreateHighScoresPanel(Transform parent, FoodsBasketGameController controller)
        {
            GameObject panelObject = new GameObject("HighScoresPanel");
            panelObject.transform.SetParent(parent, false);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.72f);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 360f);
            panelRect.anchoredPosition = Vector2.zero;

            RectTransform cardRect = CreateSolidPanel(panelObject.transform, "HighScoresCard", new Vector2(600f, 250f), Vector2.zero, new Color(0.61f, 0.75f, 0.38f, 1f));

            Text titleText = CreatePanelText(cardRect, "HighScoresTitle", "Best Times:", new Vector2(0.5f, 0.8f), 34);
            titleText.alignment = TextAnchor.MiddleLeft;
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(360f, 50f);
            titleRect.anchoredPosition = new Vector2(-90f, 0f);

            GameObject scoresTextObject = new GameObject("HighScoresText");
            scoresTextObject.transform.SetParent(cardRect, false);

            Text scoresText = scoresTextObject.AddComponent<Text>();
            scoresText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            scoresText.fontSize = 28;
            scoresText.fontStyle = FontStyle.Bold;
            scoresText.alignment = TextAnchor.UpperLeft;
            scoresText.color = Color.white;
            scoresText.text = "1.\n2.\n3.\n4.\n5.";

            RectTransform scoresTextRect = scoresText.GetComponent<RectTransform>();
            scoresTextRect.anchorMin = new Vector2(0.5f, 0.58f);
            scoresTextRect.anchorMax = new Vector2(0.5f, 0.58f);
            scoresTextRect.pivot = new Vector2(0f, 1f);
            scoresTextRect.sizeDelta = new Vector2(320f, 180f);
            scoresTextRect.anchoredPosition = new Vector2(-120f, 30f);

            Outline scoresOutline = scoresTextObject.AddComponent<Outline>();
            scoresOutline.effectColor = new Color(0.45f, 0.56f, 0.28f, 0.5f);
            scoresOutline.effectDistance = new Vector2(1f, -1f);

            GameObject closeButtonObject = new GameObject("CloseHighScoresButton");
            closeButtonObject.transform.SetParent(cardRect, false);

            Image closeButtonImage = closeButtonObject.AddComponent<Image>();
            closeButtonImage.color = new Color(0.88f, 0.88f, 0.88f, 1f);

            Button closeButton = closeButtonObject.AddComponent<Button>();
            closeButton.targetGraphic = closeButtonImage;
            closeButton.onClick.AddListener(controller.HideHighScores);

            RectTransform closeButtonRect = closeButtonObject.GetComponent<RectTransform>();
            closeButtonRect.anchorMin = new Vector2(0.5f, 0.18f);
            closeButtonRect.anchorMax = new Vector2(0.5f, 0.18f);
            closeButtonRect.pivot = new Vector2(0.5f, 0.5f);
            closeButtonRect.sizeDelta = new Vector2(250f, 58f);
            closeButtonRect.anchoredPosition = new Vector2(0f, -70f);

            Outline closeButtonOutline = closeButtonObject.AddComponent<Outline>();
            closeButtonOutline.effectColor = new Color(0.83f, 0.55f, 0.61f, 0.9f);
            closeButtonOutline.effectDistance = new Vector2(2f, -2f);

            CreateButtonText(closeButtonObject.transform, "CloseHighScoresButtonText", "CLOSE", 28, new Color(0.45f, 0.53f, 0.47f, 1f));
            panelObject.SetActive(false);
            return (panelObject, closeButtonRect, scoresText);
        }

        private static RectTransform CreateInfoButton(Transform parent, FoodsBasketGameController controller, Sprite infoSprite)
        {
            GameObject buttonObject = new GameObject("InfoButton");
            buttonObject.transform.SetParent(parent, false);

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.sprite = infoSprite;
            buttonImage.color = new Color(1f, 0.58f, 0.78f, 1f);
            buttonImage.preserveAspect = true;

            Outline buttonOutline = buttonObject.AddComponent<Outline>();
            buttonOutline.effectColor = new Color(1f, 1f, 1f, 0.95f);
            buttonOutline.effectDistance = new Vector2(2f, -2f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(controller.ShowInfoPanel);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.97f, 0.94f);
            buttonRect.anchorMax = new Vector2(0.97f, 0.94f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(70f, 70f);
            buttonRect.anchoredPosition = Vector2.zero;
            return buttonRect;
        }

        private static (GameObject panel, RectTransform closeButtonRect, Text infoText) CreateInfoPanel(Transform parent, FoodsBasketGameController controller)
        {
            GameObject panelObject = new GameObject("InfoPanel");
            panelObject.transform.SetParent(parent, false);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.72f);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(860f, 460f);
            panelRect.anchoredPosition = Vector2.zero;

            RectTransform cardRect = CreateSolidPanel(panelObject.transform, "InfoCard", new Vector2(720f, 340f), Vector2.zero, new Color(1f, 0.58f, 0.78f, 1f));

            GameObject closeButtonObject = new GameObject("CloseInfoButton");
            closeButtonObject.transform.SetParent(cardRect, false);

            Image closeButtonImage = closeButtonObject.AddComponent<Image>();
            closeButtonImage.color = new Color(0f, 0f, 0f, 0.001f);

            Button closeButton = closeButtonObject.AddComponent<Button>();
            closeButton.targetGraphic = closeButtonImage;
            closeButton.onClick.AddListener(controller.HideInfoPanel);

            RectTransform closeButtonRect = closeButtonObject.GetComponent<RectTransform>();
            closeButtonRect.anchorMin = new Vector2(0.92f, 0.88f);
            closeButtonRect.anchorMax = new Vector2(0.92f, 0.88f);
            closeButtonRect.pivot = new Vector2(0.5f, 0.5f);
            closeButtonRect.sizeDelta = new Vector2(50f, 50f);
            closeButtonRect.anchoredPosition = Vector2.zero;

            CreateButtonText(closeButtonObject.transform, "CloseInfoButtonText", "X", 32, Color.white);

            GameObject infoTextObject = new GameObject("InfoPanelText");
            infoTextObject.transform.SetParent(cardRect, false);

            Text infoText = infoTextObject.AddComponent<Text>();
            infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            infoText.fontSize = 26;
            infoText.alignment = TextAnchor.MiddleCenter;
            infoText.color = Color.white;
            infoText.text = "Information coming soon.";

            Outline infoOutline = infoTextObject.AddComponent<Outline>();
            infoOutline.effectColor = Color.black;
            infoOutline.effectDistance = new Vector2(2f, -2f);

            RectTransform infoTextRect = infoText.GetComponent<RectTransform>();
            infoTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            infoTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            infoTextRect.pivot = new Vector2(0.5f, 0.5f);
            infoTextRect.sizeDelta = new Vector2(620f, 230f);
            infoTextRect.anchoredPosition = new Vector2(0f, -20f);

            panelObject.SetActive(false);
            return (panelObject, closeButtonRect, infoText);
        }

        private static RectTransform CreateSolidPanel(Transform parent, string objectName, Vector2 size, Vector2 anchoredPosition, Color color)
        {
            GameObject panelObject = new GameObject(objectName);
            panelObject.transform.SetParent(parent, false);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = color;

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = size;
            panelRect.anchoredPosition = anchoredPosition;
            return panelRect;
        }

        private static void CreateOverlay(Transform parent, string objectName, Color color)
        {
            GameObject overlayObject = new GameObject(objectName);
            overlayObject.transform.SetParent(parent, false);

            Image overlayImage = overlayObject.AddComponent<Image>();
            overlayImage.color = color;

            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.pivot = new Vector2(0.5f, 0.5f);
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
        }

        private static void CreateSpriteImage(Transform parent, string objectName, Sprite sprite, Vector2 anchor, Vector2 size)
        {
            GameObject imageObject = new GameObject(objectName);
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;

            RectTransform rectTransform = image.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = Vector2.zero;
        }


        private static Text CreatePanelText(Transform parent, string objectName, string value, Vector2 anchor, int fontSize)
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
            return text;
        }

        private static void CreateButtonText(Transform parent, string objectName, string value)
        {
            CreateButtonText(parent, objectName, value, 34, new Color(0.18f, 0.14f, 0.1f), Vector2.zero);
        }

        private static void CreateButtonText(Transform parent, string objectName, string value, int fontSize, Color color)
        {
            CreateButtonText(parent, objectName, value, fontSize, color, Vector2.zero);
        }

        private static void CreateButtonText(Transform parent, string objectName, string value, int fontSize, Color color, Vector2 anchoredPosition)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = value;

            RectTransform rectTransform = text.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(260f, 60f);
            rectTransform.anchoredPosition = anchoredPosition;
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

            string selectedPath = null;
            for (int i = 0; i < guids.Length; i++)
            {
                string candidatePath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.Equals(Path.GetFileNameWithoutExtension(candidatePath), baseName, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(Path.GetExtension(candidatePath), ".png", System.StringComparison.OrdinalIgnoreCase))
                {
                    selectedPath = candidatePath;
                    break;
                }

                if (selectedPath == null)
                {
                    selectedPath = candidatePath;
                }
            }

            if (string.IsNullOrEmpty(selectedPath))
            {
                selectedPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(selectedPath);
        }
    }
}
