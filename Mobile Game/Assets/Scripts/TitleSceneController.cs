using System.Collections;
using UnityEngine;
using TMPro;

public class TitleSceneController : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI startText;
    private bool isStarting = false;

    private void Awake()
    {
        EnsureManagersExist();
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

    private void Start()
    {
        isStarting = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("Music/Title");
        }
        
        // 初期状態では文字を半透明（または非表示）にして点灯演出へ
        if (titleText != null)
        {
            titleText.color = new Color(1f, 1f, 1f, 0f);
            titleText.fontMaterial.DisableKeyword("UNDERLAY_ON");
        }
        if (startText != null)
        {
            startText.color = new Color(1f, 1f, 1f, 0f);
            startText.fontMaterial.DisableKeyword("UNDERLAY_ON");
        }

        StartCoroutine(TitleSequenceCoroutine());
    }

    private void Update()
    {
        // 画面タップで開始
        if (Input.GetMouseButtonDown(0) && !isStarting)
        {
            isStarting = true;
            StartGame();
        }
    }

    // ★ 豪華：ネオンの瞬き（点灯）シーケンスと、待機中の鼓動（パルス）アニメーション ★
    private IEnumerator TitleSequenceCoroutine()
    {
        yield return new WaitForSeconds(0.3f); // 起動直後に少し間を空ける

        Color targetTitleColor = new Color(1f, 0.05f, 0.6f); // ネオンピンク
        Color targetStartColor = new Color(0f, 0.9f, 1f);   // ネオンシアン

        // 1. タイトルテキストのネオン点灯（チカチカッとネオンサインが灯るような演出）
        if (titleText != null)
        {
            float[] flashIntervals = new float[] { 0.08f, 0.04f, 0.12f, 0.05f, 0.18f };
            Material titleMat = titleText.fontMaterial;

            foreach (float interval in flashIntervals)
            {
                // 点灯
                titleText.color = new Color(targetTitleColor.r, targetTitleColor.g, targetTitleColor.b, 1f);
                titleMat.SetFloat("_UnderlaySoftness", 0.1f); // 光彩を鋭く
                yield return new WaitForSeconds(interval);

                // 消灯
                titleText.color = new Color(targetTitleColor.r, targetTitleColor.g, targetTitleColor.b, 0.08f);
                titleMat.SetFloat("_UnderlaySoftness", 0.9f); // ボケボケに
                yield return new WaitForSeconds(interval);
            }

            // 完全に点灯
            titleText.color = new Color(targetTitleColor.r, targetTitleColor.g, targetTitleColor.b, 1f);
            titleMat.SetFloat("_UnderlaySoftness", 0.55f);
        }

        // 2. スタートテキストの点灯
        if (startText != null)
        {
            yield return new WaitForSeconds(0.15f);
            startText.color = new Color(targetStartColor.r, targetStartColor.g, targetStartColor.b, 1f);
        }

        // 3. ループ待機アニメーション（スタートテキストをゆっくりとネオンの脈動のように明滅）
        float elapsed = 0f;
        Vector3 origTitleScale = titleText != null ? titleText.transform.localScale : Vector3.one;

        while (!isStarting)
        {
            elapsed += Time.deltaTime;
            
            // スタートテキストをゆっくり明滅＆伸縮
            if (startText != null)
            {
                float wave = Mathf.Sin(elapsed * 4.2f); // 鼓動スピード
                float alpha = 0.45f + (wave + 1f) * 0.275f; // アルファ 0.45〜1.0
                float scale = 1.0f + (wave + 1f) * 0.02f; // 僅かにパルス
                
                startText.color = new Color(targetStartColor.r, targetStartColor.g, targetStartColor.b, alpha);
                startText.transform.localScale = Vector3.one * scale;
            }

            // タイトルテキストもゆっくり呼吸するように鼓動
            if (titleText != null)
            {
                float titleWave = Mathf.Sin(elapsed * 1.6f);
                titleText.transform.localScale = origTitleScale * (1.0f + (titleWave + 1f) * 0.012f);
            }

            yield return null;
        }
    }

    private void StartGame()
    {
        if (AudioManager.Instance != null)
        {
            // スタートホイッスルを鳴らす
            AudioManager.Instance.PlayCountdownSound(true); 
        }

        StartCoroutine(StartTransitionSequence());
    }

    private IEnumerator StartTransitionSequence()
    {
        // タップされた瞬間に文字を GO!! に変更
        if (startText != null)
        {
            startText.text = "GO!!";
            startText.color = new Color(0f, 0.9f, 1f, 1f); // ネオンシアン
            Material startMat = startText.fontMaterial;
            startMat.DisableKeyword("UNDERLAY_ON");
        }

        // 超高速テンポ：0.13秒間だけ「チチチッ！」と高速明滅させて即座に遷移
        float elapsed = 0f;
        bool isVisible = true;
        while (elapsed < 0.13f)
        {
            elapsed += Time.deltaTime;
            isVisible = !isVisible;
            if (startText != null) startText.color = new Color(0f, 0.9f, 1f, isVisible ? 1.0f : 0.15f);
            yield return new WaitForSeconds(0.03f); // 超高速チカチカ
        }

        if (startText != null) startText.gameObject.SetActive(false);

        // ゲームシーンへフェード遷移
        TransitionManager.Instance.TransitionToScene("GameScene");
    }
}
