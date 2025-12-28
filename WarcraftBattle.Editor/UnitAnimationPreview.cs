using Caliburn.Micro;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WarcraftBattle.Engine.Animation;

namespace WarcraftBattle.Editor
{
    public class UnitAnimationPreview : PropertyChangedBase
    {
        private ImageSource? _frame;
        private double _scale = 1.0;

        public UnitAnimationPreview(string name, Animator animator)
        {
            Name = name;
            Animator = animator;
        }

        public string Name { get; }
        public Animator Animator { get; }

        public ImageSource? Frame
        {
            get => _frame;
            private set
            {
                _frame = value;
                NotifyOfPropertyChange(() => Frame);
            }
        }

        public double Scale
        {
            get => _scale;
            private set
            {
                _scale = value;
                NotifyOfPropertyChange(() => Scale);
            }
        }

        public void Update(double dt)
        {
            Animator.Update(dt);
            var frame = Animator.GetCurrentFrame();
            if (frame == null)
            {
                return;
            }

            var sprite = frame.Value;
            Frame = new CroppedBitmap(sprite.Sheet, sprite.SourceRect);
            Scale = Math.Abs(Animator.GetScale());
        }
    }
}
