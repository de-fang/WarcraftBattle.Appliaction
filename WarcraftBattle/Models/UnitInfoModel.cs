using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace WarcraftBattle.Models
{
    public class UnitInfoModel : INotifyPropertyChanged
    {
        private Engine.Entity _entity;
        private ImageSource _icon;
        private bool _isSelected;
        private double _hpPct;
        private Brush _hpColor;
        private Action<object> _onClick;

        public Engine.Entity Entity
        {
            get => _entity;
            set => SetProperty(ref _entity, value);
        }

        public ImageSource Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public double HpPct
        {
            get => _hpPct;
            set => SetProperty(ref _hpPct, value);
        }

        public Brush HpColor
        {
            get => _hpColor;
            set => SetProperty(ref _hpColor, value);
        }

        public Action<object> OnClick
        {
            get => _onClick;
            set => SetProperty(ref _onClick, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Execute()
        {
            OnClick?.Invoke(this);
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
