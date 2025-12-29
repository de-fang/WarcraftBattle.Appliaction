namespace WarcraftBattle.Editor.ViewModels
{
    public class UnitEntryViewModel : ObjectEntryViewModel
    {
        private int _cost;
        private double _hp;
        private double _damage;
        private double _range;

        public UnitEntryViewModel(string name, string imagePath) : base(name, imagePath)
        {
        }

        public int Cost
        {
            get => _cost;
            set
            {
                if (_cost == value)
                {
                    return;
                }

                _cost = value;
                NotifyOfPropertyChange();
            }
        }

        public double Hp
        {
            get => _hp;
            set
            {
                if (_hp == value)
                {
                    return;
                }

                _hp = value;
                NotifyOfPropertyChange();
            }
        }

        public double Damage
        {
            get => _damage;
            set
            {
                if (_damage == value)
                {
                    return;
                }

                _damage = value;
                NotifyOfPropertyChange();
            }
        }

        public double Range
        {
            get => _range;
            set
            {
                if (_range == value)
                {
                    return;
                }

                _range = value;
                NotifyOfPropertyChange();
            }
        }
    }
}
