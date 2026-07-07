using System.Collections;
using UnityEngine;

public enum TargetShape { Circle, Triangle, Star }

public class TargetEffect : MonoBehaviour
{
    private Vector3 prefabScale; // プレハブ自体の初期スケール
    private Vector3 originalScale; // 形状別に調整された出現後のスケール
    private Color neonColor = Color.white;
    private SpriteRenderer spriteRenderer;
    private GameObject glowObj;
    
    public TargetShape CurrentShape { get; private set; } = TargetShape.Circle;

    private void Awake()
    {
        prefabScale = transform.localScale;
        transform.localScale = Vector3.zero;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // スポーン時にGameManagerから呼ばれ、形状、ネオンカラー、個別サイズ、光彩を設定する
    public void Initialize(Color color, TargetShape shape)
    {
        neonColor = color;
        CurrentShape = shape;

        // ★ 改善：丸が大きすぎる問題に対応し、各ターゲットの大きさを微調整 ★
        // 獲得スコア：Circle (5点) / Triangle (10点) / Star (20点)
        float sizeMultiplier = 0.64f; // デフォルト (Circle)
        float colliderRadius = 0.98f;

        switch (shape)
        {
            case TargetShape.Circle:
                sizeMultiplier = 0.64f;   // 大きすぎた丸を0.64fに縮小（すっきりしたサイズに）
                colliderRadius = 0.98f;   // 当たりやすさはしっかりキープ
                break;
            case TargetShape.Triangle:
                sizeMultiplier = 0.55f;   // 三角は0.55f
                colliderRadius = 0.85f;
                break;
            case TargetShape.Star:
                sizeMultiplier = 0.40f;   // 星型は0.40f
                colliderRadius = 0.62f;
                break;
        }

        originalScale = prefabScale * sizeMultiplier;

        // 当たり判定（コライダー）の半径をサイズに応じて個別適用
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.radius = colliderRadius;
        }

        // スプライトのロードと設定
        string spriteName = "Neon" + shape.ToString();
        Sprite newSprite = Resources.Load<Sprite>("M_b/" + spriteName);
        if (newSprite == null)
        {
            newSprite = Resources.Load<Sprite>("Sprites/" + spriteName);
        }

        if (newSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = newSprite;
            spriteRenderer.color = color;
        }

        // 古いGlowオブジェクトがあれば削除して再生成
        if (glowObj != null) Destroy(glowObj);

        // 疑似ネオンGlowの作成（背後に少し大きめの半透明スプライトを配置）
        glowObj = new GameObject("NeonGlow");
        glowObj.transform.SetParent(transform, false);
        glowObj.transform.localPosition = Vector3.zero;
        glowObj.transform.localRotation = Quaternion.identity;
        glowObj.transform.localScale = Vector3.one * 1.35f;

        SpriteRenderer glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        if (spriteRenderer != null && newSprite != null)
        {
            glowRenderer.sprite = newSprite;
            glowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1; // 背面に
        }
        glowRenderer.color = new Color(color.r, color.g, color.b, 0.4f);
    }

    private void Start()
    {
        StartCoroutine(PopInCoroutine());
        // 2重のネオンウェーブを時間差で広げる演出
        StartCoroutine(NeonWaveCoroutine(1.0f, 2.5f, 0.35f, 0f));     // 第1波
        StartCoroutine(NeonWaveCoroutine(1.0f, 3.2f, 0.45f, 0.08f));   // 第2波
    }

    private IEnumerator PopInCoroutine()
    {
        float duration = 0.25f;
        float elapsed = 0f;
        float s = 1.70158f; // Overshoot強度

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t -= 1f;
            float val = t * t * ((s + 1f) * t + s) + 1f;
            
            transform.localScale = originalScale * Mathf.Max(0, val);
            yield return null;
        }
        
        transform.localScale = originalScale;
    }

    private IEnumerator NeonWaveCoroutine(float startScale, float endScale, float duration, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        GameObject waveObj = new GameObject("NeonWave");
        waveObj.transform.SetParent(transform, false);
        waveObj.transform.localPosition = Vector3.zero;
        waveObj.transform.localRotation = Quaternion.identity;
        waveObj.transform.localScale = Vector3.one * startScale;

        SpriteRenderer waveRenderer = waveObj.AddComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            waveRenderer.sprite = spriteRenderer.sprite;
            waveRenderer.sortingOrder = spriteRenderer.sortingOrder - 2;
        }
        
        float elapsed = 0f;
        Color startColor = new Color(neonColor.r, neonColor.g, neonColor.b, 0.6f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(startScale, endScale, t * (2f - t));
            float alpha = Mathf.Lerp(0.6f, 0f, t);

            waveObj.transform.localScale = Vector3.one * scale;
            if (waveRenderer != null)
            {
                waveRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            yield return null;
        }

        Destroy(waveObj);
    }

    public void PlayPopEffect()
    {
        GameObject effectObj = new GameObject("TargetPopEffect");
        effectObj.transform.position = transform.position;

        ParticleSystem ps = effectObj.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.42f);
        main.stopAction = ParticleSystemStopAction.Destroy;

        // 限界速度設定（急減速物理）
        var limitVelocity = ps.limitVelocityOverLifetime;
        limitVelocity.enabled = true;
        limitVelocity.separateAxes = false; // 個別軸制御を無効
        
        // 警告回避：すべての制限軸のモードをConstant（定数）で明示的に統一
        limitVelocity.limitX = new ParticleSystem.MinMaxCurve(0.4f);
        limitVelocity.limitY = new ParticleSystem.MinMaxCurve(0.4f);
        limitVelocity.limitZ = new ParticleSystem.MinMaxCurve(0.4f);
        limitVelocity.limit = new ParticleSystem.MinMaxCurve(0.4f);

        limitVelocity.dampen = 0.20f; 

        var trails = ps.trails;
        trails.enabled = true;
        trails.ratio = 0.6f;
        trails.lifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
        
        AnimationCurve trailSizeCurve = new AnimationCurve();
        trailSizeCurve.AddKey(0f, 1f);
        trailSizeCurve.AddKey(1f, 0f);
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, trailSizeCurve);

        var emission = ps.emission;
        emission.rateOverTime = 0;

        var shape = ps.shape;

        // 形状に応じたド派手な爆発エフェクト
        switch (CurrentShape)
        {
            case TargetShape.Circle:
                main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 9f);
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 38) });
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.12f;
                break;

            case TargetShape.Triangle:
                main.startSpeed = new ParticleSystem.MinMaxCurve(7f, 11f);
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 34) });
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.radius = 0.05f;
                shape.angle = 40f;
                effectObj.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0, 3) * 120f);
                break;

            case TargetShape.Star:
                main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 48) });
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.18f;

                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(-360f, 360f);

                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = 2.2f;
                noise.frequency = 0.6f;
                break;
        }

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(neonColor, 0.20f), 
                new GradientColorKey(neonColor * 0.5f, 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(1f, 0.4f),
                new GradientAlphaKey(0f, 1f) 
            }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1.3f);
        curve.AddKey(0.5f, 0.8f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        ParticleSystemRenderer renderer = effectObj.GetComponent<ParticleSystemRenderer>();
        Material particleMat = Resources.Load<Material>("NeonParticle");
        if (particleMat != null)
        {
            renderer.material = particleMat;
            renderer.trailMaterial = particleMat;
        }

        ps.Play();
    }
}
