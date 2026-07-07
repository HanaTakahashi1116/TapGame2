using UnityEngine;

public class TapEffectManager : MonoBehaviour
{
    public static TapEffectManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mousePos.z = 10f; // カメラの少し手前
                Vector3 worldPos = mainCam.ScreenToWorldPoint(mousePos);
                PlayTapEffect(worldPos);
            }
        }
    }

    public void PlayTapEffect(Vector3 position)
    {
        GameObject effectObj = new GameObject("TapEffectParticle");
        effectObj.transform.position = position;

        ParticleSystem ps = effectObj.AddComponent<ParticleSystem>();
        
        // 基本設定
        var main = ps.main;
        // main.duration 設定は再生中のエラー回避のため削除
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f); // 寿命を短くしてキレを出す
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f); // 速度を速くして鋭さを出す
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.gravityModifier = 0.5f; // 少し落ちる
        main.stopAction = ParticleSystemStopAction.Destroy; // 自動消滅

        // エミッション（単発バースト）
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 16) }); // 粒子数を増加

        // 形状（球体状に広がる）
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.03f;

        // 色の変化（白からゴールド/パープルへフェードアウト）
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0f), 
                new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0.4f), // ゴールド
                new GradientColorKey(new Color(0.7f, 0.4f, 1f), 1f)     // パープル
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(1f, 0.4f),
                new GradientAlphaKey(0f, 1f) 
            }
        );
        colorOverLifetime.color = gradient;

        // サイズの変化（徐々に小さく）
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(0.6f, 0.7f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // レンダラー設定（デフォルトのパーティクルマテリアルを適用）
        ParticleSystemRenderer renderer = effectObj.GetComponent<ParticleSystemRenderer>();
        Material particleMat = Resources.Load<Material>("NeonParticle");
        if (particleMat != null)
        {
            renderer.material = particleMat;
        }

        ps.Play();
    }
}
