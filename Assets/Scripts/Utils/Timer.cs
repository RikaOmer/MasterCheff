using UnityEngine;
using System;

namespace MasterCheff.Utils
{
    /// <summary>
    /// Simple timer utility for cooldowns, countdowns, etc.
    /// </summary>
    [System.Serializable]
    public class Timer
    {
        [SerializeField] private float _duration;
        
        private float _remainingTime;
        private bool _isRunning;
        private bool _isPaused;
        
        // Events
        public event Action OnTimerComplete;
        public event Action<float> OnTimerTick;
        
        // Properties
        public float Duration => _duration;
        public float RemainingTime => _remainingTime;
        public float ElapsedTime => _duration - _remainingTime;
        public float Progress => _duration > 0 ? ElapsedTime / _duration : 0f;
        public float RemainingProgress => _duration > 0 ? _remainingTime / _duration : 0f;
        public bool IsRunning => _isRunning;
        public bool IsPaused => _isPaused;
        public bool IsComplete => _remainingTime <= 0 && !_isRunning;
        
        public Timer(float duration)
        {
            _duration = duration;
            _remainingTime = duration;
            _isRunning = false;
            _isPaused = false;
        }
        
        /// <summary>
        /// Start the timer
        /// </summary>
        public void Start()
        {
            _remainingTime = _duration;
            _isRunning = true;
            _isPaused = false;
        }
        
        /// <summary>
        /// Start the timer with a custom duration
        /// </summary>
        public void Start(float duration)
        {
            _duration = duration;
            Start();
        }
        
        /// <summary>
        /// Stop the timer
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _isPaused = false;
        }
        
        /// <summary>
        /// Pause the timer
        /// </summary>
        public void Pause()
        {
            if (_isRunning)
            {
                _isPaused = true;
            }
        }
        
        /// <summary>
        /// Resume the timer
        /// </summary>
        public void Resume()
        {
            if (_isRunning)
            {
                _isPaused = false;
            }
        }
        
        /// <summary>
        /// Reset the timer to initial duration
        /// </summary>
        public void Reset()
        {
            _remainingTime = _duration;
            _isRunning = false;
            _isPaused = false;
        }
        
        /// <summary>
        /// Update the timer - call this in Update()
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_isRunning || _isPaused) return;
            
            _remainingTime -= deltaTime;
            OnTimerTick?.Invoke(_remainingTime);
            
            if (_remainingTime <= 0)
            {
                _remainingTime = 0;
                _isRunning = false;
                OnTimerComplete?.Invoke();
            }
        }
        
        /// <summary>
        /// Add time to the timer
        /// </summary>
        public void AddTime(float time)
        {
            _remainingTime = Mathf.Min(_remainingTime + time, _duration);
        }
        
        /// <summary>
        /// Subtract time from the timer
        /// </summary>
        public void SubtractTime(float time)
        {
            _remainingTime = Mathf.Max(_remainingTime - time, 0);
        }
        
        /// <summary>
        /// Get formatted time string (MM:SS)
        /// </summary>
        public string GetFormattedTime()
        {
            int minutes = Mathf.FloorToInt(_remainingTime / 60);
            int seconds = Mathf.FloorToInt(_remainingTime % 60);
            return $"{minutes:00}:{seconds:00}";
        }
        
        /// <summary>
        /// Get formatted time string with milliseconds (MM:SS:MS)
        /// </summary>
        public string GetFormattedTimeWithMs()
        {
            int minutes = Mathf.FloorToInt(_remainingTime / 60);
            int seconds = Mathf.FloorToInt(_remainingTime % 60);
            int milliseconds = Mathf.FloorToInt((_remainingTime * 1000) % 1000);
            return $"{minutes:00}:{seconds:00}:{milliseconds:000}";
        }
    }
    
    /// <summary>
    /// Countdown timer that counts down from a set time
    /// </summary>
    public class CountdownTimer : Timer
    {
        public CountdownTimer(float duration) : base(duration) { }
    }
    
    /// <summary>
    /// Cooldown timer for abilities/actions
    /// </summary>
    public class Cooldown
    {
        private float _cooldownDuration;
        private float _lastUseTime = float.MinValue;
        
        public float Duration => _cooldownDuration;
        public float RemainingTime => Mathf.Max(0, _cooldownDuration - (Time.time - _lastUseTime));
        public float Progress => _cooldownDuration > 0 ? 1f - (RemainingTime / _cooldownDuration) : 1f;
        public bool IsReady => Time.time >= _lastUseTime + _cooldownDuration;
        
        public Cooldown(float duration)
        {
            _cooldownDuration = duration;
        }
        
        /// <summary>
        /// Use the ability/action and start cooldown
        /// </summary>
        public bool Use()
        {
            if (!IsReady) return false;
            
            _lastUseTime = Time.time;
            return true;
        }
        
        /// <summary>
        /// Force reset the cooldown
        /// </summary>
        public void Reset()
        {
            _lastUseTime = float.MinValue;
        }
        
        /// <summary>
        /// Set new cooldown duration
        /// </summary>
        public void SetDuration(float duration)
        {
            _cooldownDuration = duration;
        }
    }
}

