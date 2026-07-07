using UnityEngine;

public class TargetObject : MonoBehaviour
{
    public float lifeTime = 3.0f; // 消滅までの時間

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    // オブジェクトクリック（タップ）されたときに呼ばれる
    void OnMouseDown()
    {
        int scoreToAdd = 10;
        TargetEffect effect = GetComponent<TargetEffect>();
        
        if (effect != null)
        {
            // 絞り込まれた3つの形状（Shape）に応じてスコアを変化させる
            switch (effect.CurrentShape)
            {
                case TargetShape.Circle:
                    scoreToAdd = 5; // 丸は5点
                    break;
                case TargetShape.Triangle:
                    scoreToAdd = 10; // 三角は10点
                    break;
                case TargetShape.Star:
                    scoreToAdd = 20; // 星は20点
                    break;
            }

            // 決定された形状に応じた固有のSEを鳴らす
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayTapSound(effect.CurrentShape);
            }

            // 破裂エフェクトの再生
            effect.PlayPopEffect();
        }

        // 決定されたスコアを加算
        if (ScoreManager.instance != null)
        {
            ScoreManager.instance.AddScore(scoreToAdd);
        }

        // オブジェクト削除
        Destroy(gameObject);
    }
}