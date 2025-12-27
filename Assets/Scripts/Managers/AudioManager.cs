using UnityEngine;
using System.Collections.Generic;
using MasterCheff.Core;

namespace MasterCheff.Managers
{
    /// <summary>
    /// Audio Manager - Handles all game audio (music, SFX, UI sounds)
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _uiSource;

        [Header("Settings")]
        [SerializeField] private int _sfxPoolSize = 10;
        [SerializeField] private float _defaultMusicVolume = 0.7f;
        [SerializeField] private float _defaultSFXVolume = 1f;

        // Audio pools
        private List<AudioSource> _sfxPool;
        private int _currentPoolIndex = 0;

        // Volume settings
        private float _masterVolume = 1f;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;

        // State
        private bool _isMusicMuted = false;
        private bool _isSFXMuted = false;

        // Properties
        public float MasterVolume => _masterVolume;
        public float MusicVolume => _musicVolume;
        public float SFXVolume => _sfxVolume;
        public bool IsMusicMuted => _isMusicMuted;
        public bool IsSFXMuted => _isSFXMuted;

        protected override void OnSingletonAwake()
        {
            InitializeAudioSources();
            LoadVolumeSettings();
        }

        private void InitializeAudioSources()
        {
            // Create music source if not assigned
            if (_musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                _musicSource = musicObj.AddComponent<AudioSource>();
                _musicSource.loop = true;
                _musicSource.playOnAwake = false;
            }

            // Create UI source if not assigned
            if (_uiSource == null)
            {
                GameObject uiObj = new GameObject("UISource");
                uiObj.transform.SetParent(transform);
                _uiSource = uiObj.AddComponent<AudioSource>();
                _uiSource.playOnAwake = false;
            }

            // Create SFX pool
            _sfxPool = new List<AudioSource>();
            GameObject sfxContainer = new GameObject("SFXPool");
            sfxContainer.transform.SetParent(transform);

            for (int i = 0; i < _sfxPoolSize; i++)
            {
                GameObject sfxObj = new GameObject($"SFXSource_{i}");
                sfxObj.transform.SetParent(sfxContainer.transform);
                AudioSource source = sfxObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                _sfxPool.Add(source);
            }

            Debug.Log("[AudioManager] Initialized");
        }

        private void LoadVolumeSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            _musicVolume = PlayerPrefs.GetFloat("MusicVolume", _defaultMusicVolume);
            _sfxVolume = PlayerPrefs.GetFloat("SFXVolume", _defaultSFXVolume);
            _isMusicMuted = PlayerPrefs.GetInt("MusicMuted", 0) == 1;
            _isSFXMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;

            UpdateMusicVolume();
        }

        public void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", _masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", _musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", _sfxVolume);
            PlayerPrefs.SetInt("MusicMuted", _isMusicMuted ? 1 : 0);
            PlayerPrefs.SetInt("SFXMuted", _isSFXMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        #region Music

        public void PlayMusic(AudioClip clip, bool fadeIn = true, float fadeTime = 1f)
        {
            if (clip == null) return;

            if (fadeIn && _musicSource.isPlaying)
            {
                StartCoroutine(CrossFadeMusic(clip, fadeTime));
            }
            else
            {
                _musicSource.clip = clip;
                _musicSource.Play();
            }
        }

        public void StopMusic(bool fadeOut = true, float fadeTime = 1f)
        {
            if (fadeOut)
            {
                StartCoroutine(FadeOutMusic(fadeTime));
            }
            else
            {
                _musicSource.Stop();
            }
        }

        public void PauseMusic()
        {
            _musicSource.Pause();
        }

        public void ResumeMusic()
        {
            _musicSource.UnPause();
        }

        private System.Collections.IEnumerator CrossFadeMusic(AudioClip newClip, float fadeTime)
        {
            float startVolume = _musicSource.volume;

            // Fade out
            while (_musicSource.volume > 0)
            {
                _musicSource.volume -= startVolume * Time.unscaledDeltaTime / fadeTime;
                yield return null;
            }

            // Switch clip
            _musicSource.clip = newClip;
            _musicSource.Play();

            // Fade in
            while (_musicSource.volume < startVolume)
            {
                _musicSource.volume += startVolume * Time.unscaledDeltaTime / fadeTime;
                yield return null;
            }

            _musicSource.volume = startVolume;
        }

        private System.Collections.IEnumerator FadeOutMusic(float fadeTime)
        {
            float startVolume = _musicSource.volume;

            while (_musicSource.volume > 0)
            {
                _musicSource.volume -= startVolume * Time.unscaledDeltaTime / fadeTime;
                yield return null;
            }

            _musicSource.Stop();
            _musicSource.volume = startVolume;
        }

        #endregion

        #region SFX

        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || _isSFXMuted) return;

            AudioSource source = GetAvailableSFXSource();
            source.clip = clip;
            source.volume = _sfxVolume * _masterVolume * volumeScale;
            source.Play();
        }

        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            if (clip == null || _isSFXMuted) return;

            AudioSource.PlayClipAtPoint(clip, position, _sfxVolume * _masterVolume * volumeScale);
        }

        public void PlaySFXWithPitch(AudioClip clip, float pitch, float volumeScale = 1f)
        {
            if (clip == null || _isSFXMuted) return;

            AudioSource source = GetAvailableSFXSource();
            source.clip = clip;
            source.volume = _sfxVolume * _masterVolume * volumeScale;
            source.pitch = pitch;
            source.Play();
        }

        public void PlayRandomSFX(AudioClip[] clips, float volumeScale = 1f)
        {
            if (clips == null || clips.Length == 0) return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            PlaySFX(clip, volumeScale);
        }

        private AudioSource GetAvailableSFXSource()
        {
            AudioSource source = _sfxPool[_currentPoolIndex];
            _currentPoolIndex = (_currentPoolIndex + 1) % _sfxPool.Count;
            return source;
        }

        #endregion

        #region UI Sounds

        public void PlayUISound(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || _isSFXMuted) return;

            _uiSource.PlayOneShot(clip, _sfxVolume * _masterVolume * volumeScale);
        }

        #endregion

        #region Volume Control

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            UpdateMusicVolume();
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            UpdateMusicVolume();
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
        }

        private void UpdateMusicVolume()
        {
            _musicSource.volume = _isMusicMuted ? 0f : _musicVolume * _masterVolume;
        }

        public void ToggleMusicMute()
        {
            _isMusicMuted = !_isMusicMuted;
            UpdateMusicVolume();
        }

        public void ToggleSFXMute()
        {
            _isSFXMuted = !_isSFXMuted;
        }

        public void SetMusicMuted(bool muted)
        {
            _isMusicMuted = muted;
            UpdateMusicVolume();
        }

        public void SetSFXMuted(bool muted)
        {
            _isSFXMuted = muted;
        }

        #endregion
    }
}


