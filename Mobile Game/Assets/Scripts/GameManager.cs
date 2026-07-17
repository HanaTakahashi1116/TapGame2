using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public GameObject targetPrefab;
    public float spawnInterval = 0.55f;

    public Vector2 spawnRangeMin = new Vector2(-2.5f, -4.5f);
    public Vector2 spawnRangeMax = new Vector2(2.5f, 4.5f);

    // タイマー用のネオンゲージUI
    public Slider timeSlider;
    private float timeRemaining = 30.0f; // 残り時間（30秒）
    private bool isGameOver = false;
    private bool isGameClear = false;

    // カウントダウン制御用の変数
    public TextMeshProUGUI countdownText; 
    private bool isPlaying = false; 

    // ★ 新規：メインゲームのUI（ゲージや点数）をまとめるグループオブジェクト ★
    public GameObject gameUIGroup;

    // 画面サイズに合わせた動的な生成範囲・UI回避領域用の変数
    private float dynamicAvoidMinY = 0f;
    private float dynamicAvoidMaxY = 0f;
    private bool hasAvoidanceRange = false;

    private Color[] neonColors = new Color[]
    {
        new Color(1f, 0.05f, 0.6f), // ネオンピンク
        new Color(0f, 0.9f, 1f),    // ネオンシアン
        new Color(0.15f, 1f, 0.35f),// ネオングリーン
        new Color(1f, 0.9f, 0f)     // ネオンイエロー
    };

    void Start()
    {
        // ターゲットの生成スピードを調整（0.35秒からちょうど良い0.45秒に緩和）
        spawnInterval = 0.45f;

        // UIと画面アスペクト比に基づき、動的に生成範囲とUI回避領域を計算
        CalculateSpawnAndAvoidanceRanges();

        EnsureManagersExist();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("Music/Game");
        }
        ScoreManager.score = 0; // スコアをリセット
        timeRemaining = 30.0f;  // タイマーリセット
        isGameOver = false;     // ステートリセット
        isGameClear = false;
        isPlaying = false;      // カウントダウンが完了するまでプレイ状態はOFF

        // ★ カウントダウン中はメインゲームUIを非表示にする ★
        if (gameUIGroup != null)
        {
            gameUIGroup.SetActive(false);
        }

        if (timeSlider != null)
        {
            timeSlider.value = 1f; 
        }

        // カウントダウン開始
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            StartCoroutine(CountdownCoroutine());
        }
        else
        {
            StartGamePlay();
        }
    }

    private void EnsureManagersExist()
    {
        if (TransitionManager.Instance == null)
        {
            new GameObject("TransitionManager").AddComponent<TransitionManager>();
        }
        if (TapEffectManager.Instance == null)
        {
            new GameObject("TapEffectManager").AddComponent<TapEffectManager>();
        }
        if (AudioManager.Instance == null)
        {
            new GameObject("AudioManager").AddComponent<AudioManager>();
        }
    }

    private void CalculateSpawnAndAvoidanceRanges()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        // 画面のアスペクト比とカメラサイズから、画面外に出ない安全な世界座標を計算
        float orthographicSize = mainCam.orthographicSize;
        float aspect = (float)Screen.width / Screen.height;
        float halfWidth = orthographicSize * aspect;
        float halfHeight = orthographicSize;

        // ターゲットが画面の端で見切れないようにするための余白 (マージン)
        float marginX = 0.65f; 
        float marginY = 0.65f; 

        spawnRangeMin = new Vector2(-halfWidth + marginX, -halfHeight + marginY);
        spawnRangeMax = new Vector2(halfWidth - marginX, halfHeight - marginY);

        // gameUIGroup内のUI要素のワールド座標範囲を動的に取得して回避エリアを設定
        if (gameUIGroup != null)
        {
            RectTransform[] uiTransforms = gameUIGroup.GetComponentsInChildren<RectTransform>(true);
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            bool foundUI = false;

            Vector3[] corners = new Vector3[4];
            foreach (RectTransform rt in uiTransforms)
            {
                // gameUIGroup自身（画面全体を覆う親オブジェクト）は除く
                if (rt == gameUIGroup.GetComponent<RectTransform>()) continue;

                rt.GetWorldCorners(corners);
                for (int i = 0; i < 4; i++)
                {
                    if (corners[i].y < minY) minY = corners[i].y;
                    if (corners[i].y > maxY) maxY = corners[i].y;
                }
                foundUI = true;
            }

            if (foundUI)
            {
                // UIの上下に安全マージンを設ける
                float uiSafetyMargin = 0.4f;
                dynamicAvoidMinY = minY - uiSafetyMargin;
                dynamicAvoidMaxY = maxY + uiSafetyMargin;
                hasAvoidanceRange = true;
            }
        }
    }

    private IEnumerator CountdownCoroutine()
    {
        for (int count = 3; count >= 1; count--)
        {
            countdownText.text = count.ToString();
            
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCountdownSound(false);
            }

            yield return StartCoroutine(AnimateCountdownText(count.ToString(), new Color(1f, 0.05f, 0.6f))); // ネオンピンク
        }

        countdownText.text = "START!";
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCountdownSound(true);
        }

        // ★ START!と同時にゲームUIを表示し、ゲームを開始！ ★
        StartGamePlay();

        yield return StartCoroutine(AnimateCountdownText("START!", new Color(0f, 0.9f, 1f))); // ネオンシアン

        yield return new WaitForSeconds(0.2f);
        countdownText.gameObject.SetActive(false);
    }

    private IEnumerator AnimateCountdownText(string targetText, Color glowColor)
    {
        float duration = 0.85f;
        float elapsed = 0f;
        
        Material mat = countdownText.fontMaterial;
        mat.DisableKeyword("UNDERLAY_ON");

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scale = Mathf.Lerp(2.2f, 0.8f, t);
            float alpha = Mathf.Lerp(1f, 0f, t);

            countdownText.transform.localScale = Vector3.one * scale;
            countdownText.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);

            yield return null;
        }
    }

    private void StartGamePlay()
    {
        // メインゲームのUIを表示
        if (gameUIGroup != null)
        {
            gameUIGroup.SetActive(true);
        }

        isPlaying = true;
        InvokeRepeating("SpawnObject", 0f, spawnInterval);
    }

    void Update()
    {
        if (!isPlaying || isGameOver || isGameClear) return;

        timeRemaining -= Time.deltaTime;

        if (timeSlider != null)
        {
            timeSlider.value = Mathf.Clamp01(timeRemaining / 30.0f);
            
            Image fillImage = timeSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                if (timeRemaining <= 7.0f)
                {
                    float blink = 0.6f + Mathf.PingPong(Time.time * 6f, 0.4f);
                    fillImage.color = new Color(1f, 0.1f, 0.1f) * blink;
                }
                else
                {
                    fillImage.color = new Color(1f, 0.9f, 0.1f);
                }
            }
        }

        if (timeRemaining <= 0)
        {
            int currentScore = ScoreManager.score;

            if (currentScore >= 500)
            {
                GameClear();
            }
            else
            {
                GameOver();
            }
        }
    }

    void SpawnObject()
    {
        if (isGameOver || isGameClear) return;

        Vector3 spawnPosition = Vector3.zero;
        bool positionFound = false;
        int maxAttempts = 15;
        float minDistance = 1.35f;

        // UI（タイム・スコアのバーおよびテキスト）と被らないようにするためのY座標除外設定
        float avoidMinY = hasAvoidanceRange ? dynamicAvoidMinY : -2.4f;
        float avoidMaxY = hasAvoidanceRange ? dynamicAvoidMaxY : 0.0f;

        GameObject[] existingObjects = GameObject.FindGameObjectsWithTag("Respawn");

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float randomX = Random.Range(spawnRangeMin.x, spawnRangeMax.x);
            
            // Y座標は除外範囲を避けて、下部エリアまたは上部エリアから選択
            float randomY;
            float lowerHeight = Mathf.Max(0f, avoidMinY - spawnRangeMin.y);
            float upperHeight = Mathf.Max(0f, spawnRangeMax.y - avoidMaxY);
            float totalHeight = lowerHeight + upperHeight;
            
            if (totalHeight > 0f)
            {
                if (Random.Range(0f, totalHeight) < lowerHeight)
                {
                    randomY = Random.Range(spawnRangeMin.y, avoidMinY); // 下部エリア
                }
                else
                {
                    randomY = Random.Range(avoidMaxY, spawnRangeMax.y); // 上部エリア
                }
            }
            else
            {
                randomY = Random.Range(spawnRangeMin.y, spawnRangeMax.y);
            }

            Vector3 testPos = new Vector3(randomX, randomY, 0f);

            bool isOverlapping = false;
            foreach (GameObject obj in existingObjects)
            {
                if (obj != null && Vector3.Distance(testPos, obj.transform.position) < minDistance)
                {
                    isOverlapping = true;
                    break;
                }
            }

            if (!isOverlapping)
            {
                spawnPosition = testPos;
                positionFound = true;
                break;
            }
        }

        if (positionFound)
        {
            GameObject newObj = Instantiate(targetPrefab, spawnPosition, Quaternion.identity);

            TargetEffect effect = newObj.GetComponent<TargetEffect>();
            if (effect != null)
            {
                Color selectedColor = neonColors[Random.Range(0, neonColors.Length)];
                TargetShape selectedShape = (TargetShape)Random.Range(0, 3);
                effect.Initialize(selectedColor, selectedShape);
            }
        }
    }

    void GameOver()
    {
        isGameOver = true;
        timeRemaining = 0;
        if (timeSlider != null) timeSlider.value = 0f;

        // UIを隠してゲームオーバーを際立たせる（オプション、ゲーム終了時の見た目も良くなる）
        if (gameUIGroup != null) gameUIGroup.SetActive(false);

        CancelInvoke("SpawnObject");

        GameObject[] remainingObjects = GameObject.FindGameObjectsWithTag("Respawn");
        foreach (GameObject obj in remainingObjects)
        {
            Destroy(obj);
        }

        StartCoroutine(TransitionToResultDelay());
    }

    void GameClear()
    {
        isGameClear = true;
        timeRemaining = 0;
        if (timeSlider != null) timeSlider.value = 0f;

        if (gameUIGroup != null) gameUIGroup.SetActive(false);

        CancelInvoke("SpawnObject");

        GameObject[] remainingObjects = GameObject.FindGameObjectsWithTag("Respawn");
        foreach (GameObject obj in remainingObjects)
        {
            Destroy(obj);
        }

        StartCoroutine(TransitionToResultDelay());
    }

    private IEnumerator TransitionToResultDelay()
    {
        yield return new WaitForSeconds(1.0f);
        TransitionManager.Instance.TransitionToScene("ResultScene");
    }
}