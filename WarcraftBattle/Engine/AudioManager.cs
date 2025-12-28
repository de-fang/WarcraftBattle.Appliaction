using System;
using System.Collections.Generic;
using System.Windows.Media;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.Engine
{
    public interface IAudioManager
    {
        void Update(double dt);
        void PlaySound(string soundName, PointD? location = null, double baseVolume = 1.0);
        void PlayMusic(string musicName);
    }

    public class AudioManager : IAudioManager
    {
        private GameEngine _engine;
        private Dictionary<string, MediaPlayer> _soundCache = new Dictionary<string, MediaPlayer>();
        private Dictionary<string, string> _soundPaths = new Dictionary<string, string>();
        private Dictionary<string, int> _clipPlayCounts = new Dictionary<string, int>();

        private Queue<MediaPlayer> _playerPool = new Queue<MediaPlayer>();
        private int _activeCount = 0;
        private const int MaxConcurrentSounds = 32;

        // Simple cache for loaded sounds to avoid re-opening files too often, 
        // though MediaPlayer is designed for streaming. For short SFX, SoundPlayer might be better but strictly .wav.
        // We'll stick to MediaPlayer for versatility (mp3/wav) but we need to be careful about concurrency.
        // Actually, creating a new MediaPlayer for each concurrent sound is safer for overlapping sounds.

        public AudioManager(GameEngine engine)
        {
            _engine = engine;
            InitializeEventHandlers();
            LoadSoundLibrary();
        }

        public void Update(double dt)
        {
            _clipPlayCounts.Clear();
        }

        private void InitializeEventHandlers()
        {
            EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
            EventBus.Subscribe<ProjectileHitEvent>(OnProjectileHit);
            EventBus.Subscribe<UnitCommandEvent>(OnUnitCommand);
        }

        private void LoadSoundLibrary()
        {
            // Register paths (simulated since we don't have actual files in this environment)
            // In a real app, scan a directory or read xml config.
            _soundPaths["Die"] = "Sounds/die.wav";
            _soundPaths["Explosion"] = "Sounds/explosion.wav";
            _soundPaths["Hit"] = "Sounds/hit.wav";
            _soundPaths["YesSir"] = "Sounds/yessir.wav";
            _soundPaths["Attack"] = "Sounds/attack.wav";
        }

        private void OnUnitDied(UnitDiedEvent e)
        {
            PlaySound("Die", new PointD(e.Victim.X, e.Victim.Y));
        }

        private void OnProjectileHit(ProjectileHitEvent e)
        {
            if (e.Projectile.Type == Shared.Enums.UnitType.Siege)
            {
                PlaySound("Explosion", new PointD(e.Projectile.X, e.Projectile.Y));
            }
            else
            {
                PlaySound("Hit", new PointD(e.Projectile.X, e.Projectile.Y));
            }
        }

        private void OnUnitCommand(UnitCommandEvent e)
        {
            if (e.Unit.Team == Shared.Enums.TeamType.Human)
            {
                // Play confirmation sound
                PlaySound("YesSir", new PointD(e.Unit.X, e.Unit.Y));
            }
        }

        public void PlaySound(string soundName, PointD? location = null, double baseVolume = 1.0)
        {
            if (!_soundPaths.ContainsKey(soundName)) return;

            if (_clipPlayCounts.TryGetValue(soundName, out int count))
            {
                if (count >= 3) return;
            }
            else
            {
                _clipPlayCounts[soundName] = 0;
            }

            string path = _soundPaths[soundName];

            // Spatial volume calculation
            double volume = baseVolume;
            if (location.HasValue && _engine != null)
            {
                // Calculate distance from screen center
                double viewW = _engine.ViewportWidth / _engine.Zoom;
                double viewH = _engine.ViewportHeight / _engine.Zoom;
                double cx = _engine.CameraX + viewW / 2;
                double cy = _engine.CameraY + viewH / 2;
                PointD centerWorld = GameEngine.IsoToWorld(cx, cy);

                double distSq = Math.Pow(location.Value.X - centerWorld.X, 2) + Math.Pow(location.Value.Y - centerWorld.Y, 2);

                // Distance Culling: > 1.5 * ScreenWidth
                double cullDist = viewW * 1.5;
                if (distSq > cullDist * cullDist) return;

                double distFactor = 1.0 - (distSq / (cullDist * cullDist));
                volume *= Math.Max(0, distFactor);
            }

            if (volume <= 0.05) return;

            // [Object Pool] Limit concurrent sounds
            if (_activeCount >= MaxConcurrentSounds) return;

            MediaPlayer player;
            if (_playerPool.Count > 0)
            {
                player = _playerPool.Dequeue();
            }
            else
            {
                player = new MediaPlayer();
                player.MediaEnded += OnMediaEnded;
            }

            try
            {
                player.Open(new Uri(path, UriKind.RelativeOrAbsolute));
                player.Volume = volume;
                player.Play();
                _activeCount++;
                _clipPlayCounts[soundName]++;
            }
            catch
            {
                // If play fails, return to pool
                _playerPool.Enqueue(player);
            }
        }

        private void OnMediaEnded(object sender, EventArgs e)
        {
            if (sender is MediaPlayer player)
            {
                player.Stop();
                player.Close();
                _playerPool.Enqueue(player);
                _activeCount--;
            }
        }

        public void PlayMusic(string musicName)
        {
            // Placeholder for music logic
        }
    }
}
