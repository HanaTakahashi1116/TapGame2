using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class SceneSetupHelper
{
    [MenuItem("Tools/Setup Project Scenes")]
    public static void SetupProjectScenes()
    {
        Debug.Log("Starting Scene Setup...");

        // 0. Resourcesフォルダとアセットの作成
        CreateNeonMaterial();
        CreateShapeSprites();

        // 1. TitleScene の作成
        SetupTitleScene();

        // 2. GameScene のカスタマイズ（ネオンテーマ適用 ＆ カウントダウンUI ＆ ネオンゲージUI）
        SetupGameScene();

        // 3. ResultScene の作成
        SetupResultScene();

        // 4. Circle プレハブに TargetEffect をアタッチ
        SetupCirclePrefab();

        // 5. Build Settings にシーンを登録
        RegisterScenesInBuildSettings();

        // 6. 最後にTitleSceneを開き、再生ボタンでタイトルから始まるようにする
        EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity");

        Debug.Log("Scene Setup Completed Successfully!");
    }

    private static void ApplyNeonGlow(TextMeshProUGUI text, Color glowColor, float size)
    {
        text.fontStyle = FontStyles.Bold | FontStyles.Italic;
        text.color = glowColor; // 元のネオン色を文字色に設定
        
        Material mat = text.fontMaterial;
        mat.DisableKeyword("UNDERLAY_ON");
    }

    private static void CreateNeonMaterial()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        string matPath = "Assets/Resources/NeonParticle.mat";
        Material neonMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (neonMat == null)
        {
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            neonMat = new Material(shader);
            
            if (shader != null && shader.name.Contains("Particles"))
            {
                neonMat.SetFloat("_BlendOp", (float)UnityEngine.Rendering.BlendOp.Add);
                neonMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                neonMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                neonMat.SetFloat("_Blend", 1f);
            }

            AssetDatabase.CreateAsset(neonMat, matPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Created NeonParticle material at " + matPath);
        }
    }

    private static void CreateShapeSprites()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Sprites"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            AssetDatabase.CreateFolder("Assets/Resources", "Sprites");
        }

        CreateCircleSprite();
        CreateTriangleSprite();
        CreateStarSprite();
        
        AssetDatabase.SaveAssets();
        Debug.Log("Neon Shape Sprites (Circle, Triangle, Star) generated successfully!");
    }

    private static void SaveSprite(Texture2D tex, string name)
    {
        byte[] bytes = tex.EncodeToPNG();
        string path = "Assets/Resources/Sprites/" + name + ".png";
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
        Object.DestroyImmediate(tex);
    }

    private static void CreateCircleSprite()
    {
        Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float dx = x - 128f;
                float dy = y - 128f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(115f - dist);
                if (dist <= 115f)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        SaveSprite(tex, "NeonCircle");
    }

    private static void CreateTriangleSprite()
    {
        Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float px = (x - 128f) / 115f;
                float py = (y - 128f) / 115f;
                
                if (py >= -0.6f && (py <= 1.2f + 2.0f * px) && (py <= 1.2f - 2.0f * px))
                {
                    float d1 = py - (-0.6f);
                    float d2 = (1.2f + 2.0f * px) - py;
                    float d3 = (1.2f - 2.0f * px) - py;
                    float minDist = Mathf.Min(d1, Mathf.Min(d2, d3)) * 80f;
                    float alpha = Mathf.Clamp01(minDist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        SaveSprite(tex, "NeonTriangle");
    }

    private static void CreateStarSprite()
    {
        Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float dx = x - 128f;
                float dy = y - 128f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / 115f;
                float theta = Mathf.Atan2(dy, dx);
                
                float starR = 0.65f + 0.35f * Mathf.Cos(5f * theta);
                
                if (r <= starR)
                {
                    float alpha = Mathf.Clamp01((starR - r) * 8f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        SaveSprite(tex, "NeonStar");
    }

    private static void SetupTitleScene()
    {
        Scene titleScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.02f, 0.02f, 0.05f); // ディープスペースブラック
        }

        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasObj.AddComponent<GraphicRaycaster>();

        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // タイトルテキスト
        GameObject titleTextObj = new GameObject("TitleText");
        titleTextObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "TAP TAP\nCIRCLE";
        titleText.fontSize = 120;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.characterSpacing = 20f;
        ApplyNeonGlow(titleText, new Color(1f, 0.05f, 0.6f), 3.0f);
        
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0f, 400f);
        titleRect.sizeDelta = new Vector2(900f, 300f);

        // スタートテキスト
        GameObject startTextObj = new GameObject("StartText");
        startTextObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI startText = startTextObj.AddComponent<TextMeshProUGUI>();
        startText.text = "Tap to Start";
        startText.fontSize = 65;
        startText.alignment = TextAlignmentOptions.Center;
        startText.color = Color.white;
        startText.characterSpacing = 8f;
        ApplyNeonGlow(startText, new Color(0f, 0.9f, 1f), 1.8f);
        
        RectTransform startRect = startText.GetComponent<RectTransform>();
        startRect.anchoredPosition = new Vector2(0f, -300f);
        startRect.sizeDelta = new Vector2(800f, 150f);

        // ★ 豪華背景浮遊パーティクル「ネオン・スターダスト」 ★
        GameObject bgParticles = new GameObject("BackgroundParticles");
        bgParticles.transform.position = new Vector3(0f, -5f, 5f);
        bgParticles.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem ps = bgParticles.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 8f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.45f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
        
        // グラデーションカラー（シアンとネオンピンクが混ざり合う幻想的な宇宙ダスト）
        Gradient colorGrad = new Gradient();
        colorGrad.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(0f, 0.9f, 1f), 0f), 
                new GradientColorKey(new Color(1f, 0.05f, 0.6f), 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.18f, 0f), 
                new GradientAlphaKey(0.18f, 1f) 
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(colorGrad);
        main.maxParticles = 65;

        var emission = ps.emission;
        emission.rateOverTime = 6f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(10f, 10f, 1f);

        // ノイズを有効にして漂う星屑の動きを優雅にする
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.28f;
        noise.frequency = 0.45f;

        ParticleSystemRenderer renderer = bgParticles.GetComponent<ParticleSystemRenderer>();
        Material particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/NeonParticle.mat");
        if (particleMat != null) renderer.material = particleMat;

        ps.Play();

        // ★ 豪華：タイトル画面外周のネオンフレーム枠線の追加 ★
        GameObject vignetteObj = new GameObject("NeonVignette");
        vignetteObj.transform.SetParent(canvasObj.transform, false);
        vignetteObj.transform.SetAsFirstSibling(); // 最背面に

        RectTransform vigRect = vignetteObj.AddComponent<RectTransform>();
        vigRect.anchorMin = Vector2.zero;
        vigRect.anchorMax = Vector2.one;
        vigRect.sizeDelta = new Vector2(-45f, -45f); // 画面端から少しだけ内側に

        Image vigImage = vignetteObj.AddComponent<Image>();
        vigImage.color = new Color(0f, 0f, 0f, 0f); // 中身は完全に透明で枠のみ

        Outline outline = vignetteObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.05f, 0.6f, 0.32f); // 薄いピンクのネオン境界線
        outline.effectDistance = new Vector2(5f, 5f);

        // コントローラーのアタッチ
        GameObject controllerObj = new GameObject("TitleSceneController");
        TitleSceneController controller = controllerObj.AddComponent<TitleSceneController>();
        controller.titleText = titleText;
        controller.startText = startText;

        EditorSceneManager.SaveScene(titleScene, "Assets/Scenes/TitleScene.unity");
    }

    private static void SetupGameScene()
    {
        string scenePath = "Assets/Scenes/GameScene.unity";
        Scene gameScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.02f, 0.02f, 0.05f);
        }

        CleanUpOldNeonObjects();
        CleanUpOldTextUI();

        // 背景浮遊パーティクル
        GameObject bgParticles = new GameObject("BackgroundParticles");
        bgParticles.transform.position = new Vector3(0f, -5f, 5f);
        bgParticles.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem ps = bgParticles.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 8f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startColor = new Color(0.2f, 0.6f, 1f, 0.12f);
        main.maxParticles = 40;

        var emission = ps.emission;
        emission.rateOverTime = 4f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(10f, 10f, 1f);

        ParticleSystemRenderer renderer = bgParticles.GetComponent<ParticleSystemRenderer>();
        Material particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/NeonParticle.mat");
        if (particleMat != null) renderer.material = particleMat;
        ps.Play();

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            GameObject uiGroupObj = new GameObject("GameUIGroup");
            uiGroupObj.transform.SetParent(canvas.transform, false);
            RectTransform uiGroupRect = uiGroupObj.AddComponent<RectTransform>();
            uiGroupRect.anchorMin = Vector2.zero;
            uiGroupRect.anchorMax = Vector2.one;
            uiGroupRect.sizeDelta = Vector2.zero;

            Slider timeSlider = CreateNeonSlider(uiGroupObj.transform, "TimeBar", new Color(1f, 0.9f, 0.1f), new Vector2(0f, -80f), new Vector2(900f, 35f));
            timeSlider.value = 1f;

            Slider scoreSlider = CreateNeonSlider(uiGroupObj.transform, "ScoreBar", new Color(0f, 0.9f, 1f), new Vector2(0f, -145f), new Vector2(900f, 35f));
            scoreSlider.value = 0f;

            GameObject scoreValObj = new GameObject("ScoreValText");
            scoreValObj.transform.SetParent(uiGroupObj.transform, false);
            TextMeshProUGUI scoreValText = scoreValObj.AddComponent<TextMeshProUGUI>();
            scoreValText.text = "0 / 500";
            scoreValText.fontSize = 60;
            scoreValText.alignment = TextAlignmentOptions.Center;
            scoreValText.color = new Color(0f, 0.9f, 1f);
            scoreValText.characterSpacing = 8f;
            ApplyNeonGlow(scoreValText, new Color(0f, 0.9f, 1f), 1.8f);

            RectTransform scoreValRect = scoreValText.GetComponent<RectTransform>();
            scoreValRect.anchoredPosition = new Vector2(0f, -210f);
            scoreValRect.sizeDelta = new Vector2(500f, 80f);

            GameObject countdownObj = new GameObject("CountdownText");
            countdownObj.transform.SetParent(canvas.transform, false);
            TextMeshProUGUI countdownText = countdownObj.AddComponent<TextMeshProUGUI>();
            countdownText.text = "3";
            countdownText.fontSize = 180;
            countdownText.alignment = TextAlignmentOptions.Center;
            countdownText.color = Color.white;
            ApplyNeonGlow(countdownText, new Color(1f, 0.05f, 0.6f), 3.5f);

            RectTransform countdownRect = countdownText.GetComponent<RectTransform>();
            countdownRect.anchoredPosition = new Vector2(0f, 100f);
            countdownRect.sizeDelta = new Vector2(800f, 300f);

            GameManager gm = Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.timeSlider = timeSlider;
                gm.countdownText = countdownText;
                gm.gameUIGroup = uiGroupObj;
            }

            ScoreManager sm = Object.FindAnyObjectByType<ScoreManager>();
            if (sm != null)
            {
                sm.scoreSlider = scoreSlider;
                sm.scoreValText = scoreValText;
            }

            string vignetteName = "NeonVignette";
            GameObject existingVignette = canvas.transform.Find(vignetteName)?.gameObject;
            if (existingVignette != null) Object.DestroyImmediate(existingVignette);

            GameObject vignetteObj = new GameObject(vignetteName);
            vignetteObj.transform.SetParent(canvas.transform, false);
            vignetteObj.transform.SetAsFirstSibling();

            RectTransform vigRect = vignetteObj.AddComponent<RectTransform>();
            vigRect.anchorMin = Vector2.zero;
            vigRect.anchorMax = Vector2.one;
            vigRect.sizeDelta = Vector2.zero;

            Image vigImage = vignetteObj.AddComponent<Image>();
            vigImage.color = new Color(0f, 0f, 0f, 0f);
            vigImage.raycastTarget = false;

            Outline outline = vignetteObj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.05f, 0.6f, 0.22f);
            outline.effectDistance = new Vector2(8f, 8f);
        }

        EditorSceneManager.SaveScene(gameScene);
        Debug.Log("Successfully customized GameScene with Neon Theme & Gauge UI.");
    }

    private static Slider CreateNeonSlider(Transform parent, string name, Color themeColor, Vector2 anchoredPos, Vector2 size)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);
        
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchoredPosition = anchoredPos;
        sliderRect.sizeDelta = size;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        bgImage.type = Image.Type.Sliced;
        bgImage.color = new Color(0.04f, 0.04f, 0.1f, 0.7f);
        
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = new Vector2(-8f, -8f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        fillImage.type = Image.Type.Sliced;
        fillImage.color = themeColor;
        
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;

        Outline outline = sliderObj.AddComponent<Outline>();
        outline.effectColor = new Color(themeColor.r, themeColor.g, themeColor.b, 0.75f);
        outline.effectDistance = new Vector2(4f, 4f);

        return slider;
    }

    private static void CleanUpOldNeonObjects()
    {
        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj != null && (obj.name.Contains("_Glow") || obj.name.Contains("TextGlowSync_") || obj.name.Contains("BackgroundParticles") || obj.name == "TimeBar" || obj.name == "ScoreBar" || obj.name == "ScoreValText" || obj.name == "CountdownText" || obj.name == "GameUIGroup" || obj.name == "NeonVignette"))
            {
                Object.DestroyImmediate(obj);
            }
        }
    }

    private static void CleanUpOldTextUI()
    {
        var allTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        foreach (var txt in allTexts)
        {
            if (txt != null && (txt.gameObject.name.ToLower().Contains("score") || txt.gameObject.name.ToLower().Contains("timer")))
            {
                Object.DestroyImmediate(txt.gameObject);
            }
        }
    }

    private static void SetupResultScene()
    {
        Scene resultScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.02f, 0.02f, 0.05f);
        }

        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasObj.AddComponent<GraphicRaycaster>();

        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // リザルトテキスト
        GameObject resultTextObj = new GameObject("ResultText");
        resultTextObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI resultText = resultTextObj.AddComponent<TextMeshProUGUI>();
        resultText.text = "CLEAR!";
        resultText.fontSize = 120;
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        resultText.characterSpacing = 15f;
        ApplyNeonGlow(resultText, new Color(0.2f, 1f, 0.4f), 3.0f);
        
        RectTransform resultRect = resultText.GetComponent<RectTransform>();
        resultRect.anchoredPosition = new Vector2(0f, 500f);
        resultRect.sizeDelta = new Vector2(900f, 200f);

        // スコアテキスト
        GameObject scoreTextObj = new GameObject("ScoreText");
        scoreTextObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI scoreText = scoreTextObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = "Score: 0";
        scoreText.fontSize = 90;
        scoreText.alignment = TextAlignmentOptions.Center;
        scoreText.color = Color.white;
        scoreText.characterSpacing = 5f;
        ApplyNeonGlow(scoreText, Color.white, 1.5f);
        
        RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
        scoreRect.anchoredPosition = new Vector2(0f, 200f);
        scoreRect.sizeDelta = new Vector2(800f, 150f);

        // 新記録テキスト
        GameObject newRecordTextObj = new GameObject("NewRecordText");
        newRecordTextObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI newRecordText = newRecordTextObj.AddComponent<TextMeshProUGUI>();
        newRecordText.text = "NEW RECORD!";
        newRecordText.fontSize = 45;
        newRecordText.alignment = TextAlignmentOptions.Center;
        newRecordText.color = Color.white;
        newRecordText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        newRecordText.characterSpacing = 10f;
        ApplyNeonGlow(newRecordText, new Color(1f, 0.05f, 0.6f), 2.5f);
        
        RectTransform newRecordRect = newRecordText.GetComponent<RectTransform>();
        newRecordRect.anchoredPosition = new Vector2(0f, 320f);
        newRecordRect.sizeDelta = new Vector2(600f, 100f);

        // ハイスコアテキスト
        GameObject highScoreTextObj = new GameObject("HighScoreText");
        highScoreTextObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI highScoreText = highScoreTextObj.AddComponent<TextMeshProUGUI>();
        highScoreText.text = "High Score: 0";
        highScoreText.fontSize = 60;
        highScoreText.alignment = TextAlignmentOptions.Center;
        highScoreText.color = Color.white;
        highScoreText.characterSpacing = 5f;
        ApplyNeonGlow(highScoreText, new Color(0f, 0.8f, 1f), 1.8f);
        
        RectTransform highScoreRect = highScoreText.GetComponent<RectTransform>();
        highScoreRect.anchoredPosition = new Vector2(0f, 50f);
        highScoreRect.sizeDelta = new Vector2(800f, 100f);

        // ボタン用の背景スプライト
        Sprite buttonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        // Retry ボタン
        GameObject retryButtonObj = new GameObject("RetryButton");
        retryButtonObj.transform.SetParent(canvasObj.transform, false);
        Image retryImage = retryButtonObj.AddComponent<Image>();
        retryImage.sprite = buttonSprite;
        retryImage.type = Image.Type.Sliced;
        retryImage.color = new Color(0.05f, 0.05f, 0.12f);
        
        Button retryButton = retryButtonObj.AddComponent<Button>();
        
        RectTransform retryRect = retryButtonObj.GetComponent<RectTransform>();
        retryRect.anchoredPosition = new Vector2(0f, -200f);
        retryRect.sizeDelta = new Vector2(450f, 120f);

        Outline retryOutline = retryButtonObj.AddComponent<Outline>();
        retryOutline.effectColor = new Color(0f, 0.9f, 1f, 0.8f);
        retryOutline.effectDistance = new Vector2(4f, 4f);

        GameObject retryTextObj = new GameObject("Text");
        retryTextObj.transform.SetParent(retryButtonObj.transform, false);
        TextMeshProUGUI retryText = retryTextObj.AddComponent<TextMeshProUGUI>();
        retryText.text = "Retry";
        retryText.fontSize = 45;
        retryText.alignment = TextAlignmentOptions.Center;
        retryText.color = Color.white;
        RectTransform retryTextRect = retryText.GetComponent<RectTransform>();
        retryTextRect.anchorMin = Vector2.zero;
        retryTextRect.anchorMax = Vector2.one;
        retryTextRect.sizeDelta = Vector2.zero;

        // Title ボタン
        GameObject titleButtonObj = new GameObject("TitleButton");
        titleButtonObj.transform.SetParent(canvasObj.transform, false);
        Image titleImage = titleButtonObj.AddComponent<Image>();
        titleImage.sprite = buttonSprite;
        titleImage.type = Image.Type.Sliced;
        titleImage.color = new Color(0.05f, 0.05f, 0.12f);
        
        Button titleButton = titleButtonObj.AddComponent<Button>();
        
        RectTransform titleRect = titleButtonObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0f, -380f);
        titleRect.sizeDelta = new Vector2(450f, 120f);

        Outline titleOutline = titleButtonObj.AddComponent<Outline>();
        titleOutline.effectColor = new Color(1f, 0.05f, 0.6f, 0.8f);
        titleOutline.effectDistance = new Vector2(4f, 4f);

        GameObject titleTextObj = new GameObject("Text");
        titleTextObj.transform.SetParent(titleButtonObj.transform, false);
        TextMeshProUGUI titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Title";
        titleText.fontSize = 45;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        RectTransform titleTextRect = titleText.GetComponent<RectTransform>();
        titleTextRect.anchorMin = Vector2.zero;
        titleTextRect.anchorMax = Vector2.one;
        titleTextRect.sizeDelta = Vector2.zero;

        // 背景浮遊パーティクル
        GameObject bgParticles = new GameObject("BackgroundParticles");
        bgParticles.transform.position = new Vector3(0f, -5f, 5f);
        bgParticles.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem ps = bgParticles.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 8f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startColor = new Color(1f, 0.05f, 0.6f, 0.12f);
        main.maxParticles = 30;

        var emission = ps.emission;
        emission.rateOverTime = 3f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(10f, 10f, 1f);

        ParticleSystemRenderer renderer = bgParticles.GetComponent<ParticleSystemRenderer>();
        Material particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/NeonParticle.mat");
        if (particleMat != null) renderer.material = particleMat;
        ps.Play();

        GameObject controllerObj = new GameObject("ResultSceneController");
        ResultSceneController controller = controllerObj.AddComponent<ResultSceneController>();
        controller.resultText = resultText;
        controller.scoreText = scoreText;
        controller.highScoreText = highScoreText;
        controller.newRecordText = newRecordText;
        controller.retryButton = retryButton;
        controller.titleButton = titleButton;

        EditorSceneManager.SaveScene(resultScene, "Assets/Scenes/ResultScene.unity");
    }

    private static void SetupCirclePrefab()
    {
        string prefabPath = "Assets/Resources/M_b/Circle.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            prefabPath = "Assets/Scenes/Circle.prefab";
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
        if (prefab != null)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance != null)
            {
                if (instance.GetComponent<TargetEffect>() == null)
                {
                    instance.AddComponent<TargetEffect>();
                }
                
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                Object.DestroyImmediate(instance);
                Debug.Log("Successfully attached TargetEffect to Circle prefab.");
            }
        }
        else
        {
            Debug.LogWarning("Circle prefab not found at: " + prefabPath);
        }
    }

    private static void RegisterScenesInBuildSettings()
    {
        string[] scenePaths = new string[]
        {
            "Assets/Scenes/TitleScene.unity",
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/ResultScene.unity"
        };

        EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[scenePaths.Length];
        for (int i = 0; i < scenePaths.Length; i++)
        {
            buildScenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);
        }

        EditorBuildSettings.scenes = buildScenes;
        Debug.Log("Registered scenes in Build Settings.");
    }
}
public class TextGlowSynchronizer : MonoBehaviour
{
    public TextMeshProUGUI mainText;
    public TextMeshProUGUI glowText;

    private void Update()
    {
        if (mainText != null && glowText != null)
        {
            glowText.text = mainText.text;
            Color mainColor = mainText.color;
            if (mainColor.r > 0.8f && mainColor.g > 0.8f && mainColor.b < 0.2f)
            {
                glowText.color = new Color(0.2f, 1f, 0.4f, 0.45f);
            }
            else if (mainColor.r < 0.2f && mainColor.g > 0.8f && mainColor.b > 0.8f)
            {
                glowText.color = new Color(0f, 0.9f, 1f, 0.45f);
            }
            else
            {
                glowText.color = new Color(1f, 0.05f, 0.6f, 0.45f);
            }
        }
    }
}
