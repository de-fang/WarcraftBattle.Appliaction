using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;

namespace WarcraftBattle.Editor.ViewModels
{
    public abstract class ObjectEntryViewModel : PropertyChangedBase
    {
        private string _name;
        private string _imagePath;
        private ImageSource _previewImage;

        protected ObjectEntryViewModel(string name, string imagePath)
        {
            _name = name;
            _imagePath = imagePath;
            UpdatePreview();
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                {
                    return;
                }

                _name = value;
                NotifyOfPropertyChange();
            }
        }

        public string ImagePath
        {
            get => _imagePath;
            set
            {
                if (_imagePath == value)
                {
                    return;
                }

                _imagePath = value;
                NotifyOfPropertyChange();
                UpdatePreview();
            }
        }

        public ImageSource PreviewImage
        {
            get => _previewImage;
            private set
            {
                if (_previewImage == value)
                {
                    return;
                }

                _previewImage = value;
                NotifyOfPropertyChange();
            }
        }

        protected void UpdatePreview()
        {
            if (string.IsNullOrWhiteSpace(ImagePath))
            {
                PreviewImage = null;
                return;
            }

            var path = ImagePath;
            if (!File.Exists(path))
            {
                PreviewImage = null;
                return;
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new System.Uri(path, System.UriKind.RelativeOrAbsolute);
            bitmap.EndInit();
            bitmap.Freeze();
            PreviewImage = bitmap;
        }
    }
}
