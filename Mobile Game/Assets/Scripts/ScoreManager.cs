using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;
    
    // スコア用のネオンゲージUI
    public Slider scoreSlider;
    
    // 数値だけを表示するネオンデジタルテキスト (例: "150 / 500")
    public TextMeshProUGUI scoreValText;
    
    public static int score = 0;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        UpdateScoreSlider();
    }

    // スコアを増やすメソッド
    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreSlider();
    }

    private void UpdateScoreSlider()
    {
        if (scoreSlider != null)
        {
            // スコアを500を目標値として0〜1に正規化
            float targetValue = Mathf.Clamp01((float)score / 500f);
            scoreSlider.value = targetValue;

            // スコアカウンターの中身画像（Fill）を取得
            Image fillImage = scoreSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                // 500点以上に達したら、バーの色をシアンからクリア達成を祝うゴールドに変更
                if (score >= 500)
                {
                    fillImage.color = new Color(1f, 0.85f, 0.1f); // ネオンゴールド
                    
                    // スライダーのアウトライン（Glow）もゴールドに変更
                    Outline outline = scoreSlider.GetComponent<Outline>();
                    if (outline != null)
                    {
                        outline.effectColor = new Color(1f, 0.85f, 0.1f, 0.8f);
                    }
                }
                else
                {
                    fillImage.color = new Color(0f, 0.9f, 1f); // 通常時はネオンシアン
                }
            }
        }

        // デジタルスコア数値の更新
        if (scoreValText != null)
        {
            scoreValText.text = score.ToString() + " / 500";
            
            // 500点以上に達したら、テキストの色もゴールドに変更
            if (score >= 500)
            {
                scoreValText.color = new Color(1f, 0.85f, 0.1f);
            }
            else
            {
                scoreValText.color = new Color(0f, 0.9f, 1f);
            }
        }
    }
}