using System.Collections.Generic;
using UnityEngine;

namespace ChalkMaze
{
    public enum Sound
    {
        Step,       // 발소리
        Bump,       // 벽에 막힘
        Chalk,      // 분필을 긋는다
        Erase,      // 분필을 회수한다
        FireLit,    // 화톳불 점화
        TorchOut,   // 횃불이 꺼진다
        Pickup,     // 아이템을 줍는다
        Key,        // 열쇠를 찾는다
        Dig,        // 삽으로 벽을 뚫는다
        Clear       // 층 돌파
    }

    /// 소리를 전부 코드로 합성한다. 임포트할 오디오 파일이 없다.
    public sealed class Sfx : MonoBehaviour
    {
        public static Sfx I { get; private set; }

        AudioSource _src;
        Dictionary<Sound, AudioClip[]> _bank;
        System.Random _rng;
        int _stepFlip;

        void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this;

            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;

            _rng = new System.Random(20260901);
            _bank = Bake();
        }

        public bool Muted
        {
            get => PlayerProfile.Muted;
            set { PlayerProfile.Muted = value; }
        }

        public void Play(Sound s, float volume = 1f)
        {
            if (Muted || _src == null) return;
            if (!_bank.TryGetValue(s, out var arr) || arr.Length == 0) return;

            // 발소리는 번갈아 재생해 기계적으로 들리지 않게 한다
            var clip = arr.Length == 1 ? arr[0] : arr[_stepFlip++ % arr.Length];
            _src.PitchedOneShot(clip, volume, 1f + (float)(_rng.NextDouble() - 0.5) * 0.08f);
        }

        // ── 합성 ──────────────────────────────────────
        /// 정적으로 둔다. 에디터 편집 모드에서는 Awake 가 호출되지 않아
        /// 컴포넌트를 붙이는 것만으로는 소리가 구워지지 않기 때문이다.
        public static Dictionary<Sound, AudioClip[]> Bake()
        {
            var rng = new System.Random(20260901);
            return new Dictionary<Sound, AudioClip[]>
            {
                [Sound.Step]     = new[] { Step(rng, 760f), Step(rng, 620f), Step(rng, 880f) },
                [Sound.Bump]     = new[] { Bump(rng) },
                [Sound.Chalk]    = new[] { Chalk(rng, false) },
                [Sound.Erase]    = new[] { Chalk(rng, true) },
                [Sound.FireLit]  = new[] { FireLit(rng) },
                [Sound.TorchOut] = new[] { TorchOut(rng) },
                [Sound.Pickup]   = new[] { TwoTone(880f, 1320f, 0.075f) },
                [Sound.Key]      = new[] { TwoTone(1320f, 1760f, 0.09f) },
                [Sound.Dig]      = new[] { Dig(rng) },
                [Sound.Clear]    = new[] { Chord() },
            };
        }

        /// 돌바닥을 딛는 짧은 소리
        static AudioClip Step(System.Random _rng, float cutoff)
        {
            var b = Synth.Buffer(0.075f);
            Synth.AddNoise(b, 1f, _rng);
            Synth.LowPass(b, cutoff, cutoff * 0.45f);
            Synth.Shape(b, 0.02f, 34f);
            Synth.AddSine(b, 150f, 90f, 0.25f, 0.01f, 40f);
            Synth.Normalize(b, 0.32f);
            Synth.FadeOut(b, 0.012f);
            return Synth.ToClip("step", b);
        }

        /// 벽에 부딪힌 둔탁한 소리
        static AudioClip Bump(System.Random _rng)
        {
            var b = Synth.Buffer(0.13f);
            Synth.AddNoise(b, 0.5f, _rng);
            Synth.LowPass(b, 340f, 140f);
            Synth.AddSine(b, 105f, 62f, 0.9f, 0.005f, 24f);
            Synth.Shape(b, 0.008f, 20f);
            Synth.Normalize(b, 0.42f);
            Synth.FadeOut(b, 0.02f);
            return Synth.ToClip("bump", b);
        }

        /// 분필이 벽을 긁는 소리. 회수할 때는 더 짧고 낮게.
        static AudioClip Chalk(System.Random _rng, bool erase)
        {
            var b = Synth.Buffer(erase ? 0.10f : 0.15f);
            Synth.AddNoise(b, 1f, _rng);
            Synth.HighPass(b, erase ? 1400f : 2200f);
            Synth.LowPass(b, 6000f, 3200f);
            // 손이 미끄러지는 느낌 — 진폭을 흔든다
            for (int i = 0; i < b.Length; i++)
                b[i] *= 0.7f + 0.3f * Mathf.Sin(i * 0.011f);
            Synth.Shape(b, 0.06f, erase ? 16f : 9f);
            Synth.Normalize(b, 0.26f);
            Synth.FadeOut(b, 0.02f);
            return Synth.ToClip("chalk", b);
        }

        /// 불이 확 붙는 소리
        static AudioClip FireLit(System.Random _rng)
        {
            var b = Synth.Buffer(0.85f);
            Synth.AddNoise(b, 1f, _rng);
            Synth.LowPass(b, 3200f, 420f);
            Synth.Shape(b, 0.03f, 5.5f);
            Synth.AddSine(b, 110f, 300f, 0.5f, 0.06f, 3.2f);
            Synth.AddSine(b, 220f, 600f, 0.22f, 0.06f, 3.6f);
            Synth.Normalize(b, 0.55f);
            Synth.FadeOut(b, 0.15f);
            return Synth.ToClip("firelit", b);
        }

        /// 횃불이 꺼지며 어둠에 잠기는 소리
        static AudioClip TorchOut(System.Random _rng)
        {
            var b = Synth.Buffer(1.1f);
            Synth.AddNoise(b, 1f, _rng);
            Synth.LowPass(b, 2600f, 180f);
            Synth.Shape(b, 0.02f, 4.2f);
            Synth.AddSine(b, 320f, 70f, 0.45f, 0.02f, 3.0f);
            Synth.Normalize(b, 0.48f);
            Synth.FadeOut(b, 0.25f);
            return Synth.ToClip("torchout", b);
        }

        /// 두 음이 이어지는 밝은 신호음
        static AudioClip TwoTone(float a, float bHz, float each)
        {
            var buf = Synth.Buffer(each * 2f);
            int half = buf.Length / 2;
            var n1 = new float[half];
            var n2 = new float[buf.Length - half];
            Synth.AddSine(n1, a, a, 0.8f, 0.01f, 9f);
            Synth.AddSine(n2, bHz, bHz, 0.8f, 0.01f, 8f);
            for (int i = 0; i < half; i++) buf[i] = n1[i];
            for (int i = 0; i < n2.Length; i++) buf[half + i] += n2[i];
            Synth.Normalize(buf, 0.38f);
            Synth.FadeOut(buf, 0.02f);
            return Synth.ToClip("twotone", buf);
        }

        /// 삽이 벽을 무너뜨리는 소리
        static AudioClip Dig(System.Random _rng)
        {
            // 벽이 무너지는 소리다. 밝으면 경쾌하게 들려서 무게가 사라진다.
            var b = Synth.Buffer(0.5f);
            Synth.AddNoise(b, 1f, _rng);
            Synth.LowPass(b, 700f, 130f);
            Synth.Shape(b, 0.01f, 7f);
            Synth.AddSine(b, 95f, 42f, 1.1f, 0.004f, 8f);
            Synth.AddSine(b, 190f, 84f, 0.4f, 0.004f, 11f);
            Synth.Normalize(b, 0.5f);
            Synth.FadeOut(b, 0.05f);
            return Synth.ToClip("dig", b);
        }

        /// 층을 돌파했을 때 오르는 세 음
        static AudioClip Chord()
        {
            var b = Synth.Buffer(0.9f);
            float[] notes = { 523.25f, 659.25f, 783.99f };
            for (int n = 0; n < notes.Length; n++)
            {
                int start = (int)(Synth.SampleRate * 0.11f * n);
                int len = b.Length - start;
                if (len <= 0) break;
                var v = new float[len];
                Synth.AddSine(v, notes[n], notes[n], 0.7f, 0.02f, 3.4f);
                for (int i = 0; i < len; i++) b[start + i] += v[i];
            }
            Synth.Normalize(b, 0.42f);
            Synth.FadeOut(b, 0.12f);
            return Synth.ToClip("chord", b);
        }
    }

    static class AudioSourceExt
    {
        /// 피치를 살짝 흔들어 같은 소리가 반복돼도 지겹지 않게 한다
        public static void PitchedOneShot(this AudioSource src, AudioClip clip, float vol, float pitch)
        {
            src.pitch = pitch;
            src.PlayOneShot(clip, vol);
        }
    }
}
