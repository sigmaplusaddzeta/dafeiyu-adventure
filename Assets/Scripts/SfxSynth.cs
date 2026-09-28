using UnityEngine;

namespace Fishy
{
    /// 8-bit 音效合成：用代码生成 AudioClip（与网页版 WebAudio 版本音色一致）
    public static class SfxSynth
    {
        const int SR = 22050;
        public static bool Muted;
        static AudioSource _sfx, _bgm;

        enum Wave { Square, Triangle }

        class Builder
        {
            public readonly float[] Buf;
            readonly int _sr;
            public Builder(float durSec) { _sr = SR; Buf = new float[Mathf.CeilToInt(durSec * SR)]; }

            public void Tone(float at, float f0, float f1, float dur, Wave w, float vol)
            {
                int start = Mathf.FloorToInt(at * _sr);
                int len = Mathf.FloorToInt(dur * _sr);
                for (int i = 0; i < len; i++)
                {
                    int idx = start + i;
                    if (idx >= Buf.Length) break;
                    float t = (float)i / _sr;
                    float f = Mathf.Lerp(f0, f1, t / dur);
                    float ph = Mathf.Sin(2f * Mathf.PI * f * t + f0 * at * 6.283f * 0f);
                    float s;
                    if (w == Wave.Square) s = ph >= 0 ? 0.6f : -0.6f;
                    else s = Mathf.Asin(Mathf.Clamp(ph, -1f, 1f)) / 1.5708f;
                    // 包络：2ms 起音，末段 15% 释放
                    float env = Mathf.Min(1f, t / 0.002f);
                    float rel = dur - t;
                    if (rel < dur * 0.15f) env *= rel / (dur * 0.15f);
                    Buf[idx] = Mathf.Clamp(Buf[idx] + s * vol * env, -1f, 1f);
                }
            }

            public void Noise(float at, float dur, float vol)
            {
                int start = Mathf.FloorToInt(at * _sr);
                int len = Mathf.FloorToInt(dur * _sr);
                float last = 0f;
                for (int i = 0; i < len; i++)
                {
                    int idx = start + i;
                    if (idx >= Buf.Length) break;
                    float t = (float)i / _sr;
                    float n = Random.value * 2f - 1f;
                    last = Mathf.Lerp(last, n, 0.35f); // 简易低通
                    float env = Mathf.Exp(-5f * t / dur);
                    Buf[idx] = Mathf.Clamp(Buf[idx] + last * vol * env, -1f, 1f);
                }
            }

            public AudioClip Build(string name)
            {
                var clip = AudioClip.Create(name, Buf.Length, 1, SR, false);
                clip.SetData(Buf, 0);
                return clip;
            }
        }

        static AudioClip _jump, _coin, _bump, _break, _stomp, _die, _flag, _clear;
        static AudioClip _bgmClip;
        static bool _built;

        public static void Ensure(AudioSource sfx, AudioSource bgm)
        {
            _sfx = sfx; _bgm = bgm;
            if (!_built) { BuildAll(); _built = true; }
            ApplyMute();
        }

        static void ApplyMute()
        {
            if (_sfx != null) _sfx.mute = Muted;
            if (_bgm != null) _bgm.mute = Muted;
        }

        public static void ToggleMute()
        {
            Muted = !Muted;
            ApplyMute();
        }

        static void BuildAll()
        {
            // 跳跃 240→520Hz
            var b = new Builder(0.15f); b.Tone(0, 240, 520, 0.13f, Wave.Square, 0.5f);
            _jump = b.Build("jump");

            // 金币 B5 + E6
            b = new Builder(0.32f);
            b.Tone(0f, 988, 988, 0.05f, Wave.Square, 0.55f);
            b.Tone(0.05f, 1319, 1319, 0.25f, Wave.Square, 0.55f);
            _coin = b.Build("coin");

            // 撞块
            b = new Builder(0.1f); b.Tone(0, 110, 80, 0.08f, Wave.Square, 0.6f);
            _bump = b.Build("bump");

            // 碎砖
            b = new Builder(0.3f);
            b.Noise(0, 0.25f, 0.9f);
            b.Tone(0, 200, 90, 0.15f, Wave.Triangle, 0.55f);
            _break = b.Build("break");

            // 踩扁
            b = new Builder(0.14f); b.Noise(0, 0.12f, 0.9f);
            _stomp = b.Build("stomp");

            // 死亡 E4 C4 G3
            b = new Builder(0.7f);
            b.Tone(0f, 330, 330, 0.12f, Wave.Square, 0.55f);
            b.Tone(0.12f, 262, 262, 0.12f, Wave.Square, 0.55f);
            b.Tone(0.24f, 196, 196, 0.3f, Wave.Square, 0.55f);
            _die = b.Build("die");

            // 旗杆上行音阶
            b = new Builder(0.6f);
            for (int i = 0; i < 8; i++) b.Tone(i * 0.06f, 392 + i * 98, 392 + i * 98, 0.06f, Wave.Square, 0.45f);
            _flag = b.Build("flag");

            // 通关琶音 C5 E5 G5 C6 E5 G5 C6 E6
            string[] seq = { "C5", "E5", "G5", "C6", "E5", "G5", "C6", "E6" };
            float[] fr = new float[seq.Length];
            for (int i = 0; i < seq.Length; i++) fr[i] = NoteFreq(seq[i]);
            b = new Builder(1.3f);
            for (int i = 0; i < seq.Length; i++) b.Tone(i * 0.11f, fr[i], fr[i], 0.14f, Wave.Square, 0.5f);
            _clear = b.Build("clear");

            // BGM：64 步旋律循环
            string[] mel = {
                "E5","G5","A5","G5","E5",null,"C5",null,"D5","E5","F5","E5","D5",null,"B4",null,
                "E5","G5","A5","G5","C6",null,"B5","A5","G5","E5","F5","G5","E5",null,null,null,
                "C5","D5","E5","G5","A5",null,"G5",null,"F5","G5","A5","F5","E5",null,"D5",null,
                "E5","F5","G5","E5","C5",null,"D5","B4","C5",null,null,null,null,null,null,null
            };
            string[] bass = { "C3", "C3", "F3", "C3", "C3", "G3", "C3", "G3" };
            const float STEP = 0.16f;
            b = new Builder(mel.Length * STEP + 0.5f);
            for (int i = 0; i < mel.Length; i++)
            {
                if (mel[i] != null)
                {
                    float f = NoteFreq(mel[i]);
                    b.Tone(i * STEP, f, f, 0.13f, Wave.Square, 0.3f);
                }
                if (i % 8 == 0)
                {
                    float fb = NoteFreq(bass[i / 8 % bass.Length]);
                    b.Tone(i * STEP, fb, fb, 0.5f, Wave.Triangle, 0.38f);
                }
            }
            _bgmClip = b.Build("bgm");
        }

        static float NoteFreq(string n)
        {
            switch (n)
            {
                case "C3": return 131; case "D3": return 147; case "E3": return 165;
                case "F3": return 175; case "G3": return 196; case "A3": return 220; case "B3": return 247;
                case "C4": return 262; case "D4": return 294; case "E4": return 330;
                case "F4": return 349; case "G4": return 392; case "A4": return 440; case "B4": return 494;
                case "C5": return 523; case "D5": return 587; case "E5": return 659;
                case "F5": return 698; case "G5": return 784; case "A5": return 880; case "B5": return 988;
                case "C6": return 1047; case "E6": return 1319;
                default: return 440;
            }
        }

        public static void Jump() { if (_sfx != null && _jump != null) _sfx.PlayOneShot(_jump); }
        public static void Coin() { if (_sfx != null && _coin != null) _sfx.PlayOneShot(_coin); }
        public static void Bump() { if (_sfx != null && _bump != null) _sfx.PlayOneShot(_bump); }
        public static void Break() { if (_sfx != null && _break != null) _sfx.PlayOneShot(_break); }
        public static void Stomp() { if (_sfx != null && _stomp != null) _sfx.PlayOneShot(_stomp); }
        public static void Die() { if (_sfx != null && _die != null) _sfx.PlayOneShot(_die); }
        public static void Flag() { if (_sfx != null && _flag != null) _sfx.PlayOneShot(_flag); }
        public static void Clear() { if (_sfx != null && _clear != null) _sfx.PlayOneShot(_clear); }

        public static void StartBGM()
        {
            if (_bgm == null || _bgmClip == null) return;
            _bgm.clip = _bgmClip;
            _bgm.loop = true;
            if (!_bgm.isPlaying) _bgm.Play();
        }
        public static void StopBGM() { if (_bgm != null && _bgm.isPlaying) _bgm.Stop(); }
    }
}
