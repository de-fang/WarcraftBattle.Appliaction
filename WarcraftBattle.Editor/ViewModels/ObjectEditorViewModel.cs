using System.Collections.Generic;
using Caliburn.Micro;
using WarcraftBattle.Editor.Properties;

namespace WarcraftBattle.Editor.ViewModels
{
    public class ObjectEditorViewModel : Screen
    {
        private ObjectCategoryViewModel _selectedCategory;
        private ObjectEntryViewModel _selectedItem;
        private readonly Dictionary<ObjectCategoryKind, BindableCollection<ObjectEntryViewModel>> _categoryItems;

        public ObjectEditorViewModel()
        {
            DisplayName = StringResources.ObjectEditorTitle;
            Categories = new BindableCollection<ObjectCategoryViewModel>
            {
                new ObjectCategoryViewModel(StringResources.ObjectCategoryUnits, ObjectCategoryKind.Units),
                new ObjectCategoryViewModel(StringResources.ObjectCategoryBuildings, ObjectCategoryKind.Buildings),
                new ObjectCategoryViewModel(StringResources.ObjectCategoryObstacles, ObjectCategoryKind.Obstacles),
                new ObjectCategoryViewModel(StringResources.ObjectCategoryProjectiles, ObjectCategoryKind.Projectiles)
            };

            _categoryItems = new Dictionary<ObjectCategoryKind, BindableCollection<ObjectEntryViewModel>>
            {
                [ObjectCategoryKind.Units] = new BindableCollection<ObjectEntryViewModel>
                {
                    new UnitEntryViewModel("Footman", "Assets/Images/Units/footman.png")
                    {
                        Cost = 100,
                        Hp = 420,
                        Damage = 12,
                        Range = 1.5
                    },
                    new UnitEntryViewModel("Archer", "Assets/Images/Units/archer.png")
                    {
                        Cost = 120,
                        Hp = 260,
                        Damage = 18,
                        Range = 5
                    }
                },
                [ObjectCategoryKind.Buildings] = new BindableCollection<ObjectEntryViewModel>
                {
                    new BuildingEntryViewModel("Barracks", "Assets/Images/Buildings/barracks.png")
                    {
                        Width = 160,
                        Height = 120,
                        Hp = 1500
                    },
                    new BuildingEntryViewModel("Tower", "Assets/Images/Buildings/tower.png")
                    {
                        Width = 80,
                        Height = 140,
                        Hp = 900
                    }
                },
                [ObjectCategoryKind.Obstacles] = new BindableCollection<ObjectEntryViewModel>
                {
                    new ObstacleEntryViewModel("Rock", "Assets/Images/Obstacles/rock.png")
                    {
                        Width = 80,
                        Height = 60,
                        HasCollision = true
                    },
                    new ObstacleEntryViewModel("Tree", "Assets/Images/Obstacles/tree.png")
                    {
                        Width = 60,
                        Height = 90,
                        HasCollision = true
                    }
                },
                [ObjectCategoryKind.Projectiles] = new BindableCollection<ObjectEntryViewModel>
                {
                    new ProjectileEntryViewModel("Arrow", "Assets/Images/Projectiles/arrow.png")
                    {
                        Speed = 14,
                        Scale = 1
                    },
                    new ProjectileEntryViewModel("Fireball", "Assets/Images/Projectiles/fireball.png")
                    {
                        Speed = 10,
                        Scale = 1.2
                    }
                }
            };

            SelectedCategory = Categories[0];
        }

        public BindableCollection<ObjectCategoryViewModel> Categories { get; }

        public BindableCollection<ObjectEntryViewModel> Items { get; private set; } =
            new BindableCollection<ObjectEntryViewModel>();

        public ObjectCategoryViewModel SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory == value)
                {
                    return;
                }

                _selectedCategory = value;
                NotifyOfPropertyChange();
                LoadCategoryItems();
            }
        }

        public ObjectEntryViewModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value)
                {
                    return;
                }

                _selectedItem = value;
                NotifyOfPropertyChange();
            }
        }

        private void LoadCategoryItems()
        {
            Items.Clear();
            if (_selectedCategory == null)
            {
                return;
            }

            if (_categoryItems.TryGetValue(_selectedCategory.Kind, out var list))
            {
                foreach (var item in list)
                {
                    Items.Add(item);
                }

                SelectedItem = Items.Count > 0 ? Items[0] : null;
            }
        }
    }
}
