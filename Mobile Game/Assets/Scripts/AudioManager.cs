using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource bgmSource;
    private AudioSource seSource;
    private Coroutine bgmSequenceCoroutine;

    private AudioClip[] seClips;
    private AudioClip bgmClip;
    private AudioClip countdownBeepClip;
    private AudioClip countdownStartClip;

    private const int sampleRate = 44100;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudio()
    {
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = 0.22f;

        seSource = gameObject.AddComponent<AudioSource>();
        seSource.volume = 0.55f;

        // 音源のプロシージャル合成
        GenerateSEClips();
        
        // 用意されたBGMをロードする。失敗した場合はプロシージャル生成する
        bgmClip = Resources.Load<AudioClip>("Music/Title");
        if (bgmClip == null)
        {
            Debug.LogWarning("External BGM clip 'Music/Title' not found in Resources. Generating synthwave BGM instead.");
            GenerateBGMClip();
        }
        
        GenerateCountdownClips();

        if (bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.Play();
        }
    }

    public void PlayTapSound(TargetShape shape)
    {
        int index = (int)shape;
        if (seClips != null && index >= 0 && index < seClips.Length && seClips[index] != null)
        {
            seSource.pitch = Random.Range(0.92f, 1.08f);
            seSource.PlayOneShot(seClips[index]);
        }
    }

    // ★ 新規：カウントダウン音を鳴らすメソッド ★
    public void PlayCountdownSound(bool isStart)
    {
        if (seSource == null) return;
        
        seSource.pitch = 1.0f; // カウントダウン音はピッチを固定して正確に伝える
        if (isStart)
        {
            if (countdownStartClip != null) seSource.PlayOneShot(countdownStartClip);
        }
        else
        {
            if (countdownBeepClip != null) seSource.PlayOneShot(countdownBeepClip);
        }
    }

    private void GenerateSEClips()
    {
        seClips = new AudioClip[3];
        seClips[0] = CreateCircleSE();
        seClips[1] = CreateTriangleSE();
        seClips[2] = CreateStarSE();
    }

    private void GenerateCountdownClips()
    {
        // 3, 2, 1 の「ピッ」音
        float beepDuration = 0.08f;
        int beepSamples = (int)(sampleRate * beepDuration);
        float[] beepData = new float[beepSamples];
        for (int i = 0; i < beepSamples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = 880f; // A5の高い澄んだ音
            float envelope = Mathf.Clamp01(1f - t / beepDuration);
            beepData[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
        }
        countdownBeepClip = AudioClip.Create("CountdownBeep", beepSamples, 1, sampleRate, false);
        countdownBeepClip.SetData(beepData, 0);

        // START! の「ピーン！」音
        float startDuration = 0.35f;
        int startSamples = (int)(sampleRate * startDuration);
        float[] startData = new float[startSamples];
        for (int i = 0; i < startSamples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = 1320f; // E6の超高音
            float envelope = Mathf.Exp(-8f * t); // 長めの美しい減衰
            // 美しいハモりを出すために3倍音(オクターブ上)を薄く混ぜる
            float val = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t);
            startData[i] = val * envelope * 0.45f;
        }
        countdownStartClip = AudioClip.Create("CountdownStart", startSamples, 1, sampleRate, false);
        countdownStartClip.SetData(startData, 0);
    }

    private AudioClip CreateCircleSE()
    {
        float duration = 0.12f;
        int numSamples = (int)(sampleRate * duration);
        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(700f, 280f, t / duration);
            float envelope = Mathf.Clamp01(1f - t / duration);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope;
        }

        AudioClip clip = AudioClip.Create("SECIRCLE", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateTriangleSE()
    {
        float duration = 0.22f;
        int numSamples = (int)(sampleRate * duration);
        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(1200f, 850f, t / duration);
            float envelope = Mathf.Exp(-7f * t);

            float val = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * freq * 2.02f * t);
            samples[i] = val * envelope * 0.45f;
        }

        AudioClip clip = AudioClip.Create("SETRIANGLE", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateStarSE()
    {
        float duration = 0.38f;
        int numSamples = (int)(sampleRate * duration);
        float[] samples = new float[numSamples];
        float[] pitches = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            
            int pitchIndex = Mathf.FloorToInt((t / duration) * pitches.Length);
            pitchIndex = Mathf.Clamp(pitchIndex, 0, pitches.Length - 1);
            float freq = pitches[pitchIndex];

            float envelope = Mathf.Clamp01(1f - t / duration);
            float vibrato = 1.0f + 0.02f * Mathf.Sin(2f * Mathf.PI * 18f * t);
            
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * vibrato * t) * envelope * 0.45f;
        }

        AudioClip clip = AudioClip.Create("SESTAR", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void GenerateBGMClip()
    {
        float duration = 4.0f;
        int numSamples = (int)(sampleRate * duration);
        float[] samples = new float[numSamples];

        float[] baseFreqs = new float[] { 110.0f, 87.3f, 98.0f, 82.4f };
        float[] scale = new float[] { 440.0f, 523.25f, 587.33f, 659.25f, 783.99f };
        int[] melodyPattern = new int[] { 0, -1, 2, 3, -1, 1, 4, -1, 3, -1, 2, 0, -1, 4, 1, -1 };

        float basePhase = 0f;
        float leadPhase = 0f;

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float beat = (t / 4.0f) * 8f; 
            int chordIndex = Mathf.FloorToInt(t) % 4;
            float currentBaseFreq = baseFreqs[chordIndex];

            // 1. ベースシンセ
            float beatProgress = beat - Mathf.Floor(beat);
            float baseEnvelope = Mathf.Exp(-3.8f * beatProgress);
            
            basePhase += (currentBaseFreq / sampleRate);
            if (basePhase > 1.0f) basePhase -= 1.0f;
            float baseWave = 2f * basePhase - 1f;
            
            float baseSignal = baseWave * baseEnvelope * 0.12f;

            // 2. ドラムス
            float kickProgress = (t * 2f) - Mathf.Floor(t * 2f);
            float kickEnvelope = Mathf.Exp(-11f * kickProgress);
            float kickFreq = Mathf.Lerp(140f, 38f, kickProgress);
            float kickSignal = Mathf.Sin(2f * Mathf.PI * kickFreq * (kickProgress * 0.45f)) * kickEnvelope * 0.4f;

            // 3. リードシンセ
            float step = (t / 4.0f) * 32f;
            int stepIndex = Mathf.FloorToInt(step) % 16;
            float stepProgress = step - Mathf.Floor(step);
            
            float leadSignal = 0f;
            int note = melodyPattern[stepIndex];
            if (note >= 0)
            {
                float leadFreq = scale[note];
                
                if (chordIndex == 1) leadFreq *= 1.2f;
                if (chordIndex == 2) leadFreq *= 1.5f;
                
                float leadEnvelope = Mathf.Exp(-7.5f * stepProgress);
                
                leadPhase += (leadFreq / sampleRate);
                if (leadPhase > 1.0f) leadPhase -= 1.0f;
                float leadWave = 2f * Mathf.Abs(2f * leadPhase - 1f) - 1f;
                
                leadSignal = leadWave * leadEnvelope * 0.07f;
            }

            samples[i] = baseSignal + kickSignal + leadSignal;
        }

        bgmClip = AudioClip.Create("SYNTHWAVE_BGM", numSamples, 1, sampleRate, false);
        bgmClip.SetData(samples, 0);
    }

    public void PlayBGM(string clipName)
    {
        if (bgmSource == null) return;

        // ジングル再生シーケンスが走っている場合は停止する
        if (bgmSequenceCoroutine != null)
        {
            StopCoroutine(bgmSequenceCoroutine);
            bgmSequenceCoroutine = null;
        }

        // すでに同じクリップがロードされて再生中の場合は無視する
        string clipNameOnly = System.IO.Path.GetFileNameWithoutExtension(clipName);
        if (bgmSource.clip != null && (bgmSource.clip.name == clipName || bgmSource.clip.name == clipNameOnly) && bgmSource.isPlaying) return;

        AudioClip clip = Resources.Load<AudioClip>(clipName);
        if (clip != null)
        {
            bgmSource.Stop();
            bgmSource.clip = clip;
            bgmSource.loop = true; // BGMはループ再生
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning($"BGM clip '{clipName}' not found in Resources.");
        }
    }

    public void PlayJingleAndThenBGM(string jingleName, string nextBgmName)
    {
        if (bgmSource == null) return;

        if (bgmSequenceCoroutine != null)
        {
            StopCoroutine(bgmSequenceCoroutine);
        }

        bgmSequenceCoroutine = StartCoroutine(JingleSequence(jingleName, nextBgmName));
    }

    private System.Collections.IEnumerator JingleSequence(string jingleName, string nextBgmName)
    {
        AudioClip jingleClip = Resources.Load<AudioClip>(jingleName);
        if (jingleClip != null)
        {
            bgmSource.Stop();
            bgmSource.clip = jingleClip;
            bgmSource.loop = false; // ジングルはループしない
            bgmSource.Play();

            // ジングル再生終了まで待機
            yield return new WaitForSeconds(jingleClip.length);
        }
        else
        {
            Debug.LogWarning($"Jingle clip '{jingleName}' not found in Resources.");
        }

        // 次のBGMを再生
        AudioClip nextClip = Resources.Load<AudioClip>(nextBgmName);
        if (nextClip != null)
        {
            bgmSource.clip = nextClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning($"Next BGM clip '{nextBgmName}' not found in Resources.");
        }

        bgmSequenceCoroutine = null;
    }
}
