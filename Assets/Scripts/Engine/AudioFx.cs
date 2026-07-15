using UnityEngine;

namespace MiniMart.Engine
{
    /// <summary>
    /// Tiny procedural SFX bus — no audio assets needed. Clips are short synthesized
    /// blips generated once and reused: coin pickup, sale ding, purchase fanfare,
    /// level-up arpeggio. Volume kept low so it reads as feedback, not noise.
    /// </summary>
    public static class AudioFx
    {
        private static AudioSource source;
        private static AudioSource musicSource;
        private static AudioClip coinClip, saleClip, purchaseClip, levelUpClip;
        private const int Rate = 22050;

        // Real clips from the imported SFX pack (Assets/Resources/SFX). Loaded once;
        // every event falls back to the old procedural tone when a clip is missing,
        // so the game never goes silent on a broken import.
        private static bool packLoaded;
        private static AudioClip[] packCoins;
        private static AudioClip packPurchase, packLevelUp, packUi, packLose;

        private static void LoadPack()
        {
            if (packLoaded) return;
            packLoaded = true;
            packCoins = new[]
            {
                Resources.Load<AudioClip>("SFX/coin01"),
                Resources.Load<AudioClip>("SFX/coin02"),
                Resources.Load<AudioClip>("SFX/coin03"),
            };
            packPurchase = Resources.Load<AudioClip>("SFX/powerup01");
            packLevelUp  = Resources.Load<AudioClip>("SFX/powerup03");
            packUi       = Resources.Load<AudioClip>("SFX/interface01");
            packLose     = Resources.Load<AudioClip>("SFX/lose01");
        }

        private static AudioSource Source
        {
            get
            {
                if (source == null)
                {
                    var go = new GameObject("AudioFx");
                    Object.DontDestroyOnLoad(go);
                    source = go.AddComponent<AudioSource>();
                    source.spatialBlend = 0f; // 2D UI-style feedback
                    source.volume = 0.5f;
                }
                return source;
            }
        }

        public static void Coin()
        {
            LoadPack();
            // Random variation between the three coin clips — one identical ding
            // repeated hundreds of times per session fatigues fast.
            var c = packCoins?[Random.Range(0, packCoins.Length)];
            Play(c != null ? c : coinClip ??= Tone(new[] { (880f, 0.06f), (1320f, 0.07f) }));
        }

        public static void Sale()
        {
            LoadPack();
            Play(packUi != null ? packUi : saleClip ??= Tone(new[] { (660f, 0.07f), (990f, 0.09f) }));
        }

        public static void Purchase()
        {
            LoadPack();
            Play(packPurchase != null ? packPurchase : purchaseClip ??= Tone(new[] { (523f, 0.08f), (659f, 0.08f), (784f, 0.12f) }));
        }

        public static void LevelUp()
        {
            LoadPack();
            Play(packLevelUp != null ? packLevelUp : levelUpClip ??= Tone(new[] { (523f, 0.1f), (659f, 0.1f), (784f, 0.1f), (1047f, 0.18f) }));
        }

        /// <summary>Thief escape / order expired — soft negative feedback (pack-only;
        /// silence is acceptable when the clip is missing).</summary>
        public static void Lose()
        {
            LoadPack();
            Play(packLose);
        }

        /// <summary>Starts the calm shop background loop (Resources/Music/shop_loop).
        /// Quiet by design — atmosphere, not a concert. The Settings volume slider
        /// still governs it via AudioListener.volume. Safe to call repeatedly.</summary>
        public static void StartMusic()
        {
            if (musicSource != null) return;
            var clip = Resources.Load<AudioClip>("Music/shop_loop");
            if (clip == null) return; // no asset -> no music, never an error
            var go = new GameObject("Bgm");
            Object.DontDestroyOnLoad(go);
            musicSource = go.AddComponent<AudioSource>();
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.volume = 0.18f;
            musicSource.spatialBlend = 0f;
            musicSource.Play();
        }

        // A busy store fires dozens of coin/sale events per second, which exhausts
        // Unity's virtual audio channels ("Ran out of virtual channels" warnings)
        // and turns feedback into noise. One shot per clip per 80ms is inaudibly
        // different but keeps the mixer healthy.
        private const float MinInterval = 0.08f;
        private static readonly System.Collections.Generic.Dictionary<AudioClip, float> lastPlayed =
            new System.Collections.Generic.Dictionary<AudioClip, float>();

        private static void Play(AudioClip clip)
        {
            if (clip == null) return;
            if (lastPlayed.TryGetValue(clip, out float t) && Time.unscaledTime - t < MinInterval) return;
            lastPlayed[clip] = Time.unscaledTime;
            Source.PlayOneShot(clip);
        }

        /// <summary>Builds one clip from a sequence of (frequency, duration) notes with a soft decay.</summary>
        private static AudioClip Tone((float freq, float dur)[] notes)
        {
            float total = 0f;
            foreach (var n in notes) total += n.dur;
            int samples = Mathf.CeilToInt(total * Rate);
            var data = new float[samples];

            int cursor = 0;
            foreach (var (freq, dur) in notes)
            {
                int count = Mathf.CeilToInt(dur * Rate);
                for (int i = 0; i < count && cursor < samples; i++, cursor++)
                {
                    float t = (float)i / Rate;
                    float env = 1f - (float)i / count;              // linear decay per note
                    data[cursor] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.35f;
                }
            }

            var clip = AudioClip.Create($"tone_{notes[0].freq}", samples, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
