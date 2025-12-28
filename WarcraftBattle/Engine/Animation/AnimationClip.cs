using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WarcraftBattle.Engine.Animation
{
    public struct SpriteFrame
    {
        public BitmapSource Sheet;
        public Int32Rect SourceRect;
        public double PixelWidth => SourceRect.Width;
        public double PixelHeight => SourceRect.Height;
    }

    public class AnimationClip
    {
        public string Name { get; set; } = "";
        public List<SpriteFrame> Frames { get; set; } = new List<SpriteFrame>();
        public double FrameRate { get; set; } = 10.0;
        public bool Loop { get; set; } = true;
        public double Scale { get; set; } = 1.0;

        // [New] Keyframe Events: FrameIndex -> EventName
        public Dictionary<int, string> Events { get; set; } = new Dictionary<int, string>();
    }

    public class Animator
    {
        private Dictionary<string, AnimationClip> _clips = new Dictionary<string, AnimationClip>();
        private AnimationClip? _currentClip;
        private double _timer;
        private int _frameIndex;
        
        // [New] Animation Groups support
        private Dictionary<string, List<string>> _animationGroups = new Dictionary<string, List<string>>();
        private string _logicState = "";
        private System.Random _rnd = new System.Random();

        public bool IsPlaying { get; private set; }
        public bool IsFinished { get; private set; }

        // [New] Event for keyframes
        public event Action<string> OnFrameEvent;

        // [�޸�] �����������ԣ��� GameSurfaceSkia ��ȡ��ǰ֡��Ϣ
        public int CurrentFrameIndex => _frameIndex;
        public string CurrentAnimName => !string.IsNullOrEmpty(_logicState) ? _logicState : (_currentClip?.Name ?? "");

        public void RegisterGroup(string stateName, List<string> variantNames)
        {
            _animationGroups[stateName] = variantNames;
        }

        public void AddClip(AnimationClip clip)
        {
            if (clip == null) return;
            _clips[clip.Name] = clip;
            if (_currentClip == null) Play(clip.Name);
        }

        public void Play(string clipName, bool reset = false)
        {
            string targetClipName = clipName;

            // Check for animation groups
            if (_animationGroups.TryGetValue(clipName, out var variants) && variants.Count > 0)
            {
                // If currently playing a variant of this group, and not finished/resetting, maintain it
                if (!reset && !IsFinished && _currentClip != null && variants.Contains(_currentClip.Name))
                {
                    _logicState = clipName;
                    return;
                }
                
                // Pick a random variant
                targetClipName = variants[_rnd.Next(variants.Count)];
            }

            if (!_clips.ContainsKey(targetClipName)) return;
            var clip = _clips[targetClipName];
            // ͬһû꣬Ͳ
            if (_currentClip == clip && !reset && !IsFinished) { _logicState = clipName; return; }

            _currentClip = clip;
            _logicState = clipName;
            _timer = 0;
            _frameIndex = 0;
            IsPlaying = true;
            IsFinished = false;
        }

        public void Update(double dt)
        {
            if (_currentClip == null || !IsPlaying) return;

            _timer += dt;
            double frameDuration = 1.0 / _currentClip.FrameRate;

            if (_timer >= frameDuration)
            {
                _timer -= frameDuration;
                _frameIndex++;

                // [New] Trigger keyframe event if one exists for the new frame
                if (_currentClip.Events.TryGetValue(_frameIndex, out var eventName))
                {
                    OnFrameEvent?.Invoke(eventName);
                }

                if (_frameIndex >= _currentClip.Frames.Count)
                {
                    // [New] Check for rare idle animation when a loop finishes
                    if (_currentClip.Loop && _logicState == "Idle" && _rnd.NextDouble() < 0.05) // 5% chance
                    {
                        // Try to play a rare variant, if it exists
                        if (_animationGroups.TryGetValue("Idle", out var variants))
                        {
                            var rareVariant = variants.FirstOrDefault(v => v != "Idle"); // Find one that isn't the base "Idle"
                            if (rareVariant != null)
                            {
                                Play(rareVariant, true); // Force play the rare one
                                return; // Exit early to start the new animation
                            }
                        }
                    }

                    if (_currentClip.Loop)
                    {
                        _frameIndex = 0;
                    }
                    else
                    {
                        _frameIndex = _currentClip.Frames.Count - 1;
                        IsFinished = true;
                        IsPlaying = false;

                        // If a non-looping rare idle finished, switch back to default idle
                        if (_logicState == "Idle" && _currentClip.Name != "Idle")
                        {
                            Play("Idle", true);
                        }
                    }
                }
            }
        }

        public SpriteFrame? GetCurrentFrame()
        {
            if (_currentClip == null || _currentClip.Frames.Count == 0) return null;
            return _currentClip.Frames[_frameIndex];
        }

        public double GetScale() => _currentClip?.Scale ?? 1.0;
    }
}