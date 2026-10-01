using UnityEngine;

namespace CoinConvoy
{
    // Ditempel ke: GameObject "MusicManager" di scene Game
    // Musik jadi lebih intens saat pemain dikejar dekat
    [RequireComponent(typeof(AudioSource))]
    public class MusicManager : MonoBehaviour
    {
        [Header("Musik")]
        [SerializeField] private AudioClip normalTrack;   // Tenang
        [SerializeField] private AudioClip dangerTrack;   // tegang (saat dikejar dekat)
        [SerializeField] private float fadeSpeed = 2f;

        private AudioSource source;
        private bool dangerMode;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            if (normalTrack != null)
            {
                source.clip = normalTrack;
                source.Play();
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.IsEnded) return;
            if (dangerTrack == null || normalTrack == null) return;

            // Musik tegang aktif bila gerbang terbuka (akhir misi) ATAU waktu tinggal < 30 detik
            bool danger = GameManager.Instance.GateOpen ||
                          GameManager.Instance.TimeRemaining < 30f;

            if (danger && !dangerMode)
            {
                dangerMode = true;
                SwitchTrack(dangerTrack);
            }
            else if (!danger && dangerMode)
            {
                dangerMode = false;
                SwitchTrack(normalTrack);
            }
        }

        private void SwitchTrack(AudioClip next)
        {
            source.Stop();
            source.clip = next;
            source.volume = 0.3f;
            source.Play();
        }
    }
}
