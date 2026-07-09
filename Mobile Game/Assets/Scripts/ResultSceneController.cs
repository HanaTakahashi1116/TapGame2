using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultSceneController : MonoBehaviour
{
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI newRecordText;

    public Button retryButton;
    public Button titleButton;

    private void Start()
    {
        EnsureManagersExist();

        int finalScore = ScoreManager.score;
        bool isClear = finalScore >= 500;

        // クリア / ゲームオーバーの演出切り替えとネオンGlow適用
        if (isClear)
        {
            resultText.text = "CLEAR!";
            ApplyNeonGlow(resultText, new Color(1f, 0.85f, 0.1f), 3.5f); // ネオンゴールド/イエロー
            PlayConfettiEffect();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayJingleAndThenBGM("Music/Game Clear", "Music/Title");
            }
        }
        else
        {
            resultText.text = "GAME OVER";
            ApplyNeonGlow(resultText, new Color(1f, 0.15f, 0.15f), 3.5f); // ネオンレッド
            
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayJingleAndThenBGM("Music/Game Over", "Music/Title");
            }
        }

        // スコアとハイスコアのGlow適用
        ApplyNeonGlow(scoreText, Color.white, 1.5f);

        // ハイスコア処理
        int prevHighScore = PlayerPrefs.GetInt("HighScore", 0);
        bool isNewRecord = finalScore > prevHighScore && finalScore > 0;

        if (isNewRecord)
        {
            PlayerPrefs.SetInt("HighScore", finalScore);
            PlayerPrefs.Save();
            newRecordText.gameObject.SetActive(true);
            ApplyNeonGlow(newRecordText, new Color(1f, 0.05f, 0.6f), 2.5f); // ネオンピンク
            StartCoroutine(AnimateNewRecordText());
        }
        else
        {
            newRecordText.gameObject.SetActive(false);
        }

        highScoreText.text = "High Score: " + PlayerPrefs.GetInt("HighScore", prevHighScore);
        ApplyNeonGlow(highScoreText, new Color(0f, 0.8f, 1f), 1.8f); // ネオンシアン

        // ボタンのリスナー登録とアニメーション
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryButtonClicked);
            StartCoroutine(AnimateButtonHover(retryButton));
        }
        if (titleButton != null)
        {
            titleButton.onClick.AddListener(OnTitleButtonClicked);
            StartCoroutine(AnimateButtonHover(titleButton));
        }

        // スコアカウントアップ開始
        StartCoroutine(CountUpScore(finalScore));
    }

    private void ApplyNeonGlow(TextMeshProUGUI text, Color glowColor, float size)
    {
        text.fontStyle = FontStyles.Bold | FontStyles.Italic;
        
        // テキスト個別のマテリアルインスタンスを取得して設定
        Material mat = text.fontMaterial;
        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor("_UnderlayColor", new Color(glowColor.r, glowColor.g, glowColor.b, 0.85f));
        mat.SetFloat("_UnderlayOffsetX", 0f); // 完全に文字の真後ろに配置（Zファイティングなし）
        mat.SetFloat("_UnderlayOffsetY", 0f);
        mat.SetFloat("_UnderlayDilate", size); // 光の太さ
        mat.SetFloat("_UnderlaySoftness", 0.55f); // ぼかし
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

    private IEnumerator CountUpScore(int targetScore)
    {
        scoreText.text = "Score: 0";
        yield return new WaitForSeconds(0.3f); // 少し待ってから開始

        float duration = 1.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            // イージング（easeOutQuad）
            percent = percent * (2f - percent);
            
            int currentDisplayScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, percent));
            scoreText.text = "Score: " + currentDisplayScore;

            // スコアカウンターを少しパルス（ポップ）させる
            float scale = 1f + 0.05f * (1f - percent) * Mathf.Sin(elapsed * 30f);
            scoreText.transform.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        scoreText.text = "Score: " + targetScore;
        scoreText.transform.localScale = Vector3.one;
    }

    private IEnumerator AnimateNewRecordText()
    {
        Vector3 originalScale = newRecordText.transform.localScale;
        while (true)
        {
            float scaleFactor = 1f + Mathf.Sin(Time.time * 8f) * 0.1f;
            newRecordText.transform.localScale = originalScale * scaleFactor;
            yield return null;
        }
    }

    private IEnumerator AnimateButtonHover(Button btn)
    {
        Vector3 originalScale = btn.transform.localScale;
        float delay = Random.Range(0f, 0.5f);
        yield return new WaitForSeconds(delay);

        while (true)
        {
            float scaleFactor = 1f + Mathf.Sin(Time.time * 2f) * 0.03f;
            btn.transform.localScale = originalScale * scaleFactor;
            yield return null;
        }
    }

    private void OnRetryButtonClicked()
    {
        ScoreManager.score = 0;
        TransitionManager.Instance.TransitionToScene("GameScene");
    }

    private void OnTitleButtonClicked()
    {
        TransitionManager.Instance.TransitionToScene("TitleScene");
    }

    private void PlayConfettiEffect()
    {
        GameObject confettiObj = new GameObject("ConfettiEffect");
        confettiObj.transform.position = new Vector3(0f, 6f, 0f); // 画面上部から
        confettiObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // 下向き

        ParticleSystem ps = confettiObj.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 4.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.gravityModifier = 0.2f;
        main.startColor = new ParticleSystem.MinMaxGradient(GetConfettiGradient());

        var emission = ps.emission;
        emission.rateOverTime = 25f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(7f, 1f, 1f);

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);

        ParticleSystemRenderer renderer = confettiObj.GetComponent<ParticleSystemRenderer>();
        Material particleMat = Resources.Load<Material>("NeonParticle");
        if (particleMat != null)
        {
            renderer.material = particleMat;
        }

        ps.Play();
    }

    private Gradient GetConfettiGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.3f, 0.3f), 0f),    // ネオンレッド
                new GradientColorKey(new Color(0.3f, 1f, 0.3f), 0.25f), // ネオングリーン
                new GradientColorKey(new Color(0.3f, 0.5f, 1f), 0.5f),  // ネオンブルー
                new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0.75f), // ネオンイエロー
                new GradientColorKey(new Color(1f, 0.3f, 0.9f), 1f)     // ネオンピンク
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        return gradient;
    }
}
