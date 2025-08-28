using UnityEngine;


public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    [Header("Music")]
    [SerializeField] private AudioClip themeMusic;
    
    [Header("Sound Effects")]
    [SerializeField] private AudioClip powerUpSound;
    [SerializeField] private AudioClip collectibleSound;
    [SerializeField] private AudioClip playerDamageSound;
    [SerializeField] private AudioClip enemyDamageSound;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.7f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeAudioSources()
    {
        // Create audio sources if they don't exist
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
        
        UpdateVolumes();
    }
    
    private void Start()
    {
        // Start playing theme music on game start
        PlayThemeMusic();
    }
    
    #region Music Methods
    
    public void PlayThemeMusic()
    {
        if (themeMusic != null && musicSource != null)
        {
            musicSource.clip = themeMusic;
            musicSource.Play();
        }
    }
    
    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }
    
    public void PauseMusic()
    {
        if (musicSource != null)
        {
            musicSource.Pause();
        }
    }
    
    public void ResumeMusic()
    {
        if (musicSource != null)
        {
            musicSource.UnPause();
        }
    }
    
    #endregion
    
    #region Sound Effect Methods
    
    public void PlayPowerUpSound()
    {
        PlaySFX(powerUpSound);
    }
    
    public void PlayCollectibleSound()
    {
        PlaySFX(collectibleSound);
    }
    
    public void PlayPlayerDamageSound()
    {
        PlaySFX(playerDamageSound);
    }
    
    public void PlayEnemyDamageSound()
    {
        PlaySFX(enemyDamageSound);
    }
    
    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
    
    // Generic method to play any audio clip
    public void PlaySound(AudioClip clip)
    {
        PlaySFX(clip);
    }
    
    #endregion
    
    #region Volume Control
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }
    
    private void UpdateVolumes()
    {
        if (musicSource != null)
            musicSource.volume = musicVolume;
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }
    
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
    
    #endregion
    
    #region Toggle Methods
    
    public void ToggleMusic()
    {
        if (musicSource != null)
        {
            if (musicSource.isPlaying)
                PauseMusic();
            else
                ResumeMusic();
        }
    }
    
    public void ToggleSFX()
    {
        sfxSource.mute = !sfxSource.mute;
    }
    
    #endregion
}