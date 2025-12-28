using System.Windows.Media;

namespace WarcraftBattle.Engine
{
    // [Optimization] Data-Oriented Design for Particles
    public struct ParticleData
    {
        public bool Active;
        public double X,
            Y;
        public double VelX,
            VelY;
        public double Gravity;
        public double Life,
            MaxLife;
        public double Rotation,
            AngularVelocity;
        public double Scale,
            StartScale,
            EndScale;
        public Color StartColor,
            EndColor;
        public ParticleBlendMode BlendMode;
        public bool StaysOnGround;
        private bool _onGround;

        public void Update(double dt)
        {
            Life -= dt;
            if (Life <= 0)
                Active = false;
            // The rest of the logic will be in the main engine loop for better cache performance
        }
    }
}
