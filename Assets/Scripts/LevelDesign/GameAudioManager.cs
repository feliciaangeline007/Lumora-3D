using System.Collections;
using UnityEngine;

namespace EnchantedForest
{
    /// <summary>
    /// Audio Manager terpusat untuk semua efek suara dan musik latar (BGM) game Lumora.
    ///
    /// Fitur:
    ///   - BGM dengan smooth fade in/out saat ganti lagu
    ///   - Efek suara: koin, lompat, mendarat, langkah kaki, bounce pad, victory, api/kristal
    ///   - Fallback prosedural jika clip belum diassign (tidak ada suara mati mendadak)
    ///   - Volume master yang bisa diubah runtime
    ///
    /// CARA PAKAI:
    ///   Cukup assign clip-clip di Inspector — script ini sudah di-generate otomatis
    ///   oleh LumoraLevelBuilder ke setiap scene.
    /// </summary>
    public class GameAudioManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static GameAudioManager Instance { get; private set; }

        // ── Audio Sources ────────────────────────────────────────────────────
        [Header("Audio Sources (dibuat otomatis jika kosong)")]
        public AudioSource sfxSource;
        public AudioSource musicSource;
        public AudioSource ambientSource;   // Khusus ambien loop (ForestAmbience, dll.)

        // ── Volume Master ────────────────────────────────────────────────────
        [Header("Volume")]
        [Range(0f, 1f)] public float masterVolume   = 1.0f;
        [Range(0f, 1f)] public float musicVolume    = 0.45f;
        [Range(0f, 1f)] public float ambientVolume  = 0.30f;
        [Range(0f, 1f)] public float sfxVolume      = 0.85f;

        // ── BGM & Ambien (assign di Inspector atau lewat LumoraLevelBuilder) ─
        [Header("BGM & Ambien Level")]
        [Tooltip("Musik latar utama level ini (loop)")]
        public AudioClip bgmClip;

        [Tooltip("Suara ambien lingkungan (angin hutan, gua menetes, dll.) — diputar bersamaan dengan BGM")]
        public AudioClip ambientClip;

        // Alias untuk kompatibilitas pemanggil lama / script editor
        public AudioClip ambientMusicClip
        {
            get => ambientClip;
            set => ambientClip = value;
        }

        [Tooltip("Durasi fade in/out BGM saat mulai atau berpindah (detik)")]
        [Range(0f, 5f)]
        public float bgmFadeDuration = 2.0f;

        // ── SFX Clips ────────────────────────────────────────────────────────
        [Header("SFX — Koin & Kolektibel")]
        public AudioClip coinClip;

        [Header("SFX — Pergerakan Player")]
        public AudioClip jumpClip;
        public AudioClip landClip;
        public AudioClip[] footstepClips;

        [Header("SFX — Obstacle & Lingkungan")]
        public AudioClip bounceClip;
        public AudioClip crystalChimeClip;  // Suara kristal saat disentuh / berdenyut
        public AudioClip fireClip;           // Suara obor / api di Kuil Reruntuhan

        [Header("SFX — Momen Penting")]
        public AudioClip victoryClip;        // Saat menyentuh portal finish
        public AudioClip checkpointClip;     // Saat menyentuh kristal checkpoint

        // ── Private State ────────────────────────────────────────────────────
        private float _stepTimer;
        private float _lastStepInterval = 0.45f;
        private Coroutine _bgmFadeCoroutine;

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            // Singleton — satu AudioManager per scene
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureAudioSources();
        }

        private void Start()
        {
            ApplyVolumes();
            StartBGM(bgmClip);
            StartAmbient(ambientClip);
        }

        // ── Setup AudioSources ────────────────────────────────────────────────
        private void EnsureAudioSources()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f; // 2D
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.spatialBlend = 0f; // 2D
                musicSource.priority = 0;      // Prioritas tertinggi
            }

            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
                ambientSource.spatialBlend = 0f;
                ambientSource.priority = 64;
            }
        }

        private void ApplyVolumes()
        {
            if (musicSource  != null) musicSource.volume  = musicVolume  * masterVolume;
            if (ambientSource != null) ambientSource.volume = ambientVolume * masterVolume;
            if (sfxSource    != null) sfxSource.volume    = sfxVolume    * masterVolume;
        }

        // ── BGM Control ───────────────────────────────────────────────────────

        /// <summary>Mulai BGM dengan fade in. Aman dipanggil bahkan jika clip null.</summary>
        public void StartBGM(AudioClip clip)
        {
            if (musicSource == null) return;

            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);

            if (clip == null)
            {
                // Tidak ada clip — matikan musik tanpa error
                musicSource.Stop();
                return;
            }

            _bgmFadeCoroutine = StartCoroutine(FadeInBGM(clip));
        }

        /// <summary>Ganti BGM dengan fade out → fade in (cocok untuk transisi level).</summary>
        public void SwitchBGM(AudioClip newClip)
        {
            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);
            _bgmFadeCoroutine = StartCoroutine(CrossfadeBGM(newClip));
        }

        /// <summary>Stop BGM dengan fade out halus.</summary>
        public void StopBGM()
        {
            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);
            _bgmFadeCoroutine = StartCoroutine(FadeOutBGM());
        }

        private IEnumerator FadeInBGM(AudioClip clip)
        {
            float targetVol = musicVolume * masterVolume;
            musicSource.volume = 0f;
            musicSource.clip = clip;
            musicSource.Play();

            float t = 0f;
            while (t < bgmFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(0f, targetVol, t / bgmFadeDuration);
                yield return null;
            }
            musicSource.volume = targetVol;
        }

        private IEnumerator FadeOutBGM()
        {
            float startVol = musicSource.volume;
            float t = 0f;
            while (t < bgmFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, t / bgmFadeDuration);
                yield return null;
            }
            musicSource.Stop();
            musicSource.volume = musicVolume * masterVolume;
        }

        private IEnumerator CrossfadeBGM(AudioClip newClip)
        {
            yield return FadeOutBGM();
            if (newClip != null) yield return FadeInBGM(newClip);
        }

        // ── Ambien Control ────────────────────────────────────────────────────

        public void StartAmbient(AudioClip clip)
        {
            if (ambientSource == null || clip == null) return;
            ambientSource.clip = clip;
            ambientSource.volume = ambientVolume * masterVolume;
            ambientSource.Play();
        }

        public void StopAmbient()
        {
            if (ambientSource != null) ambientSource.Stop();
        }

        // ── SFX Playback ──────────────────────────────────────────────────────

        public void PlayCoinSound(Vector3 position)
        {
            PlayAtPoint(coinClip, position, sfxVolume * masterVolume * 0.85f);
        }

        public void PlayBounceSound(Vector3 position)
        {
            PlayAtPoint(bounceClip, position, sfxVolume * masterVolume);
        }

        public void PlayJumpSound()
        {
            PlayOneShot(jumpClip, sfxVolume * masterVolume * 0.7f);
        }

        public void PlayLandSound()
        {
            PlayOneShot(landClip, sfxVolume * masterVolume * 0.6f);
        }

        public void PlayVictorySound()
        {
            PlayOneShot(victoryClip, sfxVolume * masterVolume);
        }

        public void PlayCheckpointSound()
        {
            // Gunakan crystalChimeClip jika ada, fallback ke coinClip
            AudioClip clip = crystalChimeClip != null ? crystalChimeClip : coinClip;
            PlayOneShot(clip, sfxVolume * masterVolume * 0.75f, pitchOverride: 0.82f);
        }

        public void PlayCrystalAmbient(Vector3 position)
        {
            if (crystalChimeClip != null)
                PlayAtPoint(crystalChimeClip, position, sfxVolume * masterVolume * 0.4f);
        }

        public void PlayFireCrackle(Vector3 position)
        {
            PlayAtPoint(fireClip, position, sfxVolume * masterVolume * 0.5f);
        }

        /// <summary>Dipanggil dari PlayerController3D saat langkah kaki.</summary>
        public void PlayFootstep()
        {
            if (footstepClips == null || footstepClips.Length == 0) return;
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            PlayOneShot(clip, sfxVolume * masterVolume * 0.35f,
                pitchOverride: Random.Range(0.88f, 1.12f));
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private void PlayOneShot(AudioClip clip, float volume, float pitchOverride = 1f)
        {
            if (clip == null || sfxSource == null) return;
            float prevPitch = sfxSource.pitch;
            sfxSource.pitch = pitchOverride;
            sfxSource.PlayOneShot(clip, volume);
            sfxSource.pitch = prevPitch;
        }

        private void PlayAtPoint(AudioClip clip, Vector3 position, float volume)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Update volume realtime saat slider digeser di Inspector
            ApplyVolumes();
        }
#endif
    }
}
