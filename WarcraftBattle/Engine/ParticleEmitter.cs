using System;
using System.Windows.Media;

namespace WarcraftBattle.Engine
{
    public class EmitterConfig
    {
        public bool IsOneShot { get; set; }
        public int OneShotCount { get; set; }
        public float EmitRate { get; set; } = 10; // particles per second
        public double EmitterLife { get; set; } = -1; // -1 for infinite

        public double ParticleLifeMin { get; set; } = 1.0;
        public double ParticleLifeMax { get; set; } = 2.0;

        public float AngleMin { get; set; } = 0;
        public float AngleMax { get; set; } = 360;

        public float SpeedMin { get; set; } = 50;
        public float SpeedMax { get; set; } = 100;

        public double Gravity { get; set; } = 0;

        public Color StartColor { get; set; } = Colors.White;
        public Color EndColor { get; set; } = Color.FromArgb(0, 255, 255, 255);

        public double StartScaleMin { get; set; } = 1.0;
        public double StartScaleMax { get; set; } = 1.0;
        public double EndScaleMin { get; set; } = 0.1;
        public double EndScaleMax { get; set; } = 0.1;

        public float AngularVelocityMin { get; set; } = 0;
        public float AngularVelocityMax { get; set; } = 0;

        public ParticleBlendMode BlendMode { get; set; } = ParticleBlendMode.Normal;
        public bool StaysOnGround { get; set; } = false;
    }

    public class ParticleEmitter
    {
        public double X,
            Y;
        public bool IsFinished { get; private set; }

        private EmitterConfig _config;
        private GameEngine _engine;
        private Entity _followTarget;
        private float _emitCounter;
        private double _life;
        private Random _rand = new Random();

        public ParticleEmitter(
            double x,
            double y,
            EmitterConfig config,
            GameEngine engine,
            Entity followTarget = null
        )
        {
            X = x;
            Y = y;
            _config = config;
            _engine = engine;
            _followTarget = followTarget;
            _life = config.EmitterLife;

            if (_config.IsOneShot)
            {
                for (int i = 0; i < config.OneShotCount; i++)
                {
                    EmitParticle();
                }
                IsFinished = true;
            }
        }

        public void Update(double dt)
        {
            if (IsFinished)
                return;

            if (_followTarget != null)
            {
                X = _followTarget.X;
                Y = _followTarget.Y;
            }

            if (_life > 0)
            {
                _life -= dt;
                if (_life <= 0)
                {
                    IsFinished = true;
                    return;
                }
            }

            _emitCounter += (float)dt * _config.EmitRate;
            while (_emitCounter > 1)
            {
                EmitParticle();
                _emitCounter -= 1;
            }
        }

        private void EmitParticle()
        {
            // var p = _engine.ParticlePool.Get();
            int index = _engine.GetNewParticleIndex();
            if (index == -1)
                return; // Particle system is full

            double angleRad =
                (_config.AngleMin + _rand.NextDouble() * (_config.AngleMax - _config.AngleMin))
                * (Math.PI / 180.0);
            double speed =
                _config.SpeedMin + _rand.NextDouble() * (_config.SpeedMax - _config.SpeedMin);

            ref var p = ref _engine.Particles[index];
            p.Active = true;
            p.X = this.X;
            p.Y = this.Y;
            p.VelX = Math.Cos(angleRad) * speed;
            p.VelY = Math.Sin(angleRad) * speed;
            p.Life =
                _config.ParticleLifeMin
                + _rand.NextDouble() * (_config.ParticleLifeMax - _config.ParticleLifeMin);
            p.MaxLife = p.Life;
            p.Gravity = _config.Gravity;
            p.StartScale =
                _config.StartScaleMin
                + _rand.NextDouble() * (_config.StartScaleMax - _config.StartScaleMin);
            p.EndScale =
                _config.EndScaleMin
                + _rand.NextDouble() * (_config.EndScaleMax - _config.EndScaleMin);
            p.Scale = p.StartScale;
            p.Rotation = 0;
            p.AngularVelocity =
                _config.AngularVelocityMin
                + _rand.NextDouble() * (_config.AngularVelocityMax - _config.AngularVelocityMin);
            p.StartColor = _config.StartColor;
            p.EndColor = _config.EndColor;
            p.BlendMode = _config.BlendMode;
            p.StaysOnGround = _config.StaysOnGround;
        }
    }
}
