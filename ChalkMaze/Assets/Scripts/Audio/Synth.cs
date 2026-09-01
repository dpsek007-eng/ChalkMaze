using UnityEngine;

namespace ChalkMaze
{
    /// 오디오 파일을 임포트하지 않는다. 스프라이트를 코드로 그리듯 소리도 코드로 만든다.
    /// 저작권 문제가 원천적으로 없고, 톤을 코드에서 바로 조정할 수 있다.
    public static class Synth
    {
        public const int SampleRate = 44100;

        public static float[] Buffer(float seconds)
            => new float[Mathf.Max(1, Mathf.CeilToInt(SampleRate * seconds))];

        /// 지수 감쇠 포락선. attack 만큼 올라갔다가 남은 구간을 decay 로 내려간다.
        public static float Env(int i, int len, float attack, float curve)
        {
            float t = i / (float)len;
            float a = attack <= 0f ? 1f : Mathf.Clamp01(t / attack);
            float d = Mathf.Exp(-curve * Mathf.Max(0f, t - attack));
            return a * d;
        }

        public static void AddNoise(float[] buf, float amp, System.Random rng)
        {
            for (int i = 0; i < buf.Length; i++)
                buf[i] += (float)(rng.NextDouble() * 2.0 - 1.0) * amp;
        }

        /// 주파수가 from → to 로 미끄러지는 사인파
        public static void AddSine(float[] buf, float from, float to, float amp,
                                   float attack, float curve)
        {
            double phase = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = i / (float)buf.Length;
                double f = Mathf.Lerp(from, to, t);
                phase += 2.0 * Mathf.PI * f / SampleRate;
                buf[i] += Mathf.Sin((float)phase) * amp * Env(i, buf.Length, attack, curve);
            }
        }

        /// 1차 저역통과. cutoff 가 fromHz → toHz 로 이동한다.
        public static void LowPass(float[] buf, float fromHz, float toHz)
        {
            float y = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = i / (float)buf.Length;
                float fc = Mathf.Lerp(fromHz, toHz, t);
                float a = 1f - Mathf.Exp(-2f * Mathf.PI * fc / SampleRate);
                y += a * (buf[i] - y);
                buf[i] = y;
            }
        }

        /// 1차 고역통과 (저역통과 결과를 원본에서 뺀다)
        public static void HighPass(float[] buf, float hz)
        {
            float y = 0f;
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * hz / SampleRate);
            for (int i = 0; i < buf.Length; i++)
            {
                y += a * (buf[i] - y);
                buf[i] -= y;
            }
        }

        public static void Shape(float[] buf, float attack, float curve)
        {
            for (int i = 0; i < buf.Length; i++)
                buf[i] *= Env(i, buf.Length, attack, curve);
        }

        /// 클리핑을 막고 최대 진폭을 peak 에 맞춘다
        public static void Normalize(float[] buf, float peak)
        {
            float max = 0f;
            for (int i = 0; i < buf.Length; i++) max = Mathf.Max(max, Mathf.Abs(buf[i]));
            if (max < 1e-6f) return;
            float g = peak / max;
            for (int i = 0; i < buf.Length; i++) buf[i] *= g;
        }

        /// 끝을 부드럽게 잘라 뚝 끊기는 소리를 없앤다
        public static void FadeOut(float[] buf, float seconds)
        {
            int n = Mathf.Min(buf.Length, Mathf.CeilToInt(SampleRate * seconds));
            for (int i = 0; i < n; i++)
            {
                int idx = buf.Length - n + i;
                buf[idx] *= 1f - (i / (float)n);
            }
        }

        public static AudioClip ToClip(string name, float[] buf)
        {
            var c = AudioClip.Create(name, buf.Length, 1, SampleRate, false);
            c.SetData(buf, 0);
            return c;
        }
    }
}
