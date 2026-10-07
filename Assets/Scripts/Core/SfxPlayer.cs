using UnityEngine;

namespace RadiantOrchard
{
    // Central place for one-shot gameplay SFX. Clips are synthesized
    // placeholders assigned by GenerateGameplaySFX.cs (Tools/Radiant Orchard/
    // Generate Gameplay SFX) — the same procedural-audio approach
    // GenerateAmbientAudio.cs already uses for the ambient loops, applied to
    // actual gameplay feedback (Issue Register: Zero gameplay SFX / no music).
    // Swap the clip fields for real sound design later; every call site below
    // stays the same.
    public class SfxPlayer : MonoBehaviour
    {
        public static SfxPlayer Instance { get; private set; }

        /// <summary>
        /// Runtime setup for scenes that don't have SfxPlayer hand-wired in the
        /// editor (Tools → Radiant Orchard → Generate Gameplay SFX) — loads the
        /// same clips from Resources/Audio/SFX so Instance is never null and
        /// every PlayX() call actually makes a sound.
        /// </summary>
        public static void Ensure()
        {
            if (!Application.isPlaying) return;
            if (Instance != null) return;

            var go = new GameObject("SfxPlayer");
            var player = go.AddComponent<SfxPlayer>();
            player.source = go.AddComponent<AudioSource>();
            player.source.playOnAwake = false;
            player.source.spatialBlend = 0f;

            player.gestureSuccess = Resources.Load<AudioClip>("Audio/SFX/SFX_GestureSuccess");
            player.tonicPop       = Resources.Load<AudioClip>("Audio/SFX/SFX_TonicPop");
            player.heal            = Resources.Load<AudioClip>("Audio/SFX/SFX_Heal");
            player.quizCorrect    = Resources.Load<AudioClip>("Audio/SFX/SFX_QuizCorrect");
            player.quizIncorrect  = Resources.Load<AudioClip>("Audio/SFX/SFX_QuizIncorrect");
            player.levelComplete  = Resources.Load<AudioClip>("Audio/SFX/SFX_LevelComplete");
        }

        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip gestureSuccess;
        [SerializeField] private AudioClip tonicPop;
        [SerializeField] private AudioClip heal;
        [SerializeField] private AudioClip quizCorrect;
        [SerializeField] private AudioClip quizIncorrect;
        [SerializeField] private AudioClip levelComplete;

        [Header("Ambient background music (separate looping source)")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip ambientMusic;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (source == null) source = GetComponent<AudioSource>();

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.volume = musicVolume;
                musicSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            if (ambientMusic != null && musicSource != null && !musicSource.isPlaying)
            {
                musicSource.clip = ambientMusic;
                musicSource.Play();
            }
        }

        public void PlayGestureSuccess() => Play(gestureSuccess);
        public void PlayTonicPop() => Play(tonicPop);
        public void PlayHeal() => Play(heal);
        public void PlayQuizCorrect() => Play(quizCorrect);
        public void PlayQuizIncorrect() => Play(quizIncorrect);
        public void PlayLevelComplete() => Play(levelComplete);

        private void Play(AudioClip clip)
        {
            if (source == null || clip == null) return;
            source.PlayOneShot(clip);
        }
    }
}
