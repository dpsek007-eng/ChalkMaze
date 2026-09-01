using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChalkMaze.EditorTools
{
    /// 합성한 효과음을 WAV 로 뽑는다. 귀로 확인하고 톤을 조정하기 위한 도구.
    public static class ExportSfx
    {
        [MenuItem("분필 미로/7. 효과음 WAV 내보내기")]
        public static void Run()
        {
            string dir = System.Environment.GetEnvironmentVariable("CM_SFX_DIR");
            if (string.IsNullOrEmpty(dir)) dir = "/tmp/chalkmaze-sfx";
            Directory.CreateDirectory(dir);

            var bank = Sfx.Bake();

            int n = 0;
            foreach (var kv in bank)
            {
                for (int i = 0; i < kv.Value.Length; i++)
                {
                    var clip = kv.Value[i];
                    var samples = new float[clip.samples];
                    clip.GetData(samples, 0);

                    float peak = 0f, rms = 0f;
                    foreach (var s in samples) { peak = Mathf.Max(peak, Mathf.Abs(s)); rms += s * s; }
                    rms = Mathf.Sqrt(rms / Mathf.Max(1, samples.Length));

                    string name = kv.Value.Length > 1 ? $"{kv.Key}-{i + 1}" : kv.Key.ToString();
                    WriteWav(Path.Combine(dir, name + ".wav"), samples, clip.frequency);
                    Debug.Log($"[SFX] {name,-12} {clip.length * 1000f,6:F0}ms  peak {peak:F3}  rms {rms:F4}");
                    n++;
                }
            }
            Debug.Log($"[SFX] {n}개 → {dir}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void WriteWav(string path, float[] samples, int rate)
        {
            using var fs = new FileStream(path, FileMode.Create);
            using var w = new BinaryWriter(fs);
            int dataLen = samples.Length * 2;

            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + dataLen);
            w.Write(new[] { 'W', 'A', 'V', 'E' });
            w.Write(new[] { 'f', 'm', 't', ' ' });
            w.Write(16);
            w.Write((short)1);              // PCM
            w.Write((short)1);              // mono
            w.Write(rate);
            w.Write(rate * 2);              // byte rate
            w.Write((short)2);              // block align
            w.Write((short)16);             // bits
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(dataLen);
            foreach (var s in samples)
                w.Write((short)(Mathf.Clamp(s, -1f, 1f) * 32767f));
        }
    }
}
