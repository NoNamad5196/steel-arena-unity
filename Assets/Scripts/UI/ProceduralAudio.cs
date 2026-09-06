using System;
using UnityEngine;

namespace ProtocolArena.UI
{
    internal enum GameSound
    {
        Confirm,
        Lock,
        Reward,
        Hit,
        Special,
        Heal,
        Victory,
        Defeat
    }

    internal sealed class ProceduralAudio : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private AudioSource source;
        private AudioClip confirm;
        private AudioClip lockSound;
        private AudioClip reward;
        private AudioClip hit;
        private AudioClip special;
        private AudioClip heal;
        private AudioClip victory;
        private AudioClip defeat;

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 0.32f;
            confirm = CreateTone("확인", 440f, 660f, 0.09f, false);
            lockSound = CreateTone("정지", 620f, 920f, 0.12f, false);
            reward = CreateTone("보상", 520f, 1040f, 0.22f, false);
            hit = CreateTone("타격", 145f, 75f, 0.1f, true);
            special = CreateTone("특수 공격", 260f, 780f, 0.18f, true);
            heal = CreateTone("회복", 390f, 880f, 0.3f, false);
            victory = CreateTone("승리", 440f, 1100f, 0.48f, false);
            defeat = CreateTone("패배", 300f, 95f, 0.42f, false);
        }

        public void Play(GameSound sound)
        {
            AudioClip clip = sound switch
            {
                GameSound.Confirm => confirm,
                GameSound.Lock => lockSound,
                GameSound.Reward => reward,
                GameSound.Hit => hit,
                GameSound.Special => special,
                GameSound.Heal => heal,
                GameSound.Victory => victory,
                _ => defeat
            };
            if (clip != null) source.PlayOneShot(clip);
        }

        private static AudioClip CreateTone(string name, float startFrequency, float endFrequency, float duration, bool noise)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var samples = new float[sampleCount];
            var random = new System.Random(name.GetHashCode());
            float phase = 0f;
            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)SampleRate;
                float progress = index / (float)Math.Max(1, sampleCount - 1);
                float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
                phase += frequency * Mathf.PI * 2f / SampleRate;
                float envelope = Mathf.Pow(1f - progress, 1.8f) * Mathf.Min(1f, time * 80f);
                float wave = Mathf.Sin(phase) * 0.7f + Mathf.Sin(phase * 0.5f) * 0.3f;
                if (noise) wave = wave * 0.62f + ((float)random.NextDouble() * 2f - 1f) * 0.38f;
                samples[index] = wave * envelope * 0.65f;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
