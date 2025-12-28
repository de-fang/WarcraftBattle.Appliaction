using Caliburn.Micro;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging; // 必须引用，用于处理图标
using System.Windows.Threading;
using WarcraftBattle.Engine;
using WarcraftBattle.Engine.Animation;
using WarcraftBattle.Models;
using WarcraftBattle.Shared.Enums;
using WarcraftBattle.Shared.Models;

namespace WarcraftBattle.ViewModels
{
    public class ShellViewModel : Screen
    {
        private GameEngine _engine;
        private DispatcherTimer _uiTimer;
        private TimeSpan _lastRenderTime;



        // --- 数据集合 ---
        public BindableCollection<ActionButtonModel> UnitButtons { get; } = new BindableCollection<ActionButtonModel>();
        public BindableCollection<ActionButtonModel> BuildingButtons { get; } = new BindableCollection<ActionButtonModel>();
        public BindableCollection<SkillButtonModel> SkillButtons { get; } = new BindableCollection<SkillButtonModel>();
        public BindableCollection<ShopUnitItem> ShopUnitList { get; } = new BindableCollection<ShopUnitItem>();
        public BindableCollection<StageItemModel> StageList { get; } = new BindableCollection<StageItemModel>();
        public BindableCollection<UnitInfoModel> SelectedUnitsList { get; } = new BindableCollection<UnitInfoModel>();


        // --- 选中项 ---
        private ShopUnitItem? _selectedShopUnit;

        // --- UI 文本属性 ---
        private string _goldText = "0";
        private string _popText = "0/10";
        private string _waveText = "1.0";
        private string _honorText = "荣誉: 0";
        private string _levelText = "关卡";
        private string _speedText = "1x";
        private string _entityInfoText = "";

        private string _upgradeInfoTitle = "未选择";
        private string _upgradeInfoDesc = "请选择左侧兵种查看详情";
        private string _upgradeCostText = "";

        private string _resultTitle = "";
        private string _resultReward = "";
        private Brush _resultColor = Brushes.White;

        private int _lastGold = -1;
        private int _lastPop = -1;
        private int _lastMaxPop = -1;
        private double _lastWave = double.NaN;

        // --- 可见性属性 ---
        private Visibility _menuVisibility = Visibility.Visible;
        private Visibility _levelSelectVisibility = Visibility.Collapsed;
        private Visibility _shopVisibility = Visibility.Collapsed;
        private Visibility _gameVisibility = Visibility.Collapsed;
        private Visibility _resultVisibility = Visibility.Collapsed;

        private Visibility _entityInfoVisibility = Visibility.Collapsed;
        private Visibility _multiSelectionVisibility = Visibility.Collapsed;
        private Visibility _singleSelectionVisibility = Visibility.Collapsed;
        private Visibility _unitTabVisibility = Visibility.Collapsed;
        private Visibility _buildTabVisibility = Visibility.Visible;
        private Visibility _skillsVisibility = Visibility.Collapsed;
        private Visibility _towerBtnVisibility = Visibility.Collapsed;

        private Visibility _winButtonsVisibility = Visibility.Collapsed;
        private Visibility _loseButtonsVisibility = Visibility.Collapsed;
        private Visibility _upgradeBtnVisibility = Visibility.Collapsed;

        // --- 属性封装 ---
        public GameEngine Engine
        {
            get => _engine;
            set { _engine = value; NotifyOfPropertyChange(() => Engine); }
        }

        public string GoldText { get => _goldText; set => Set(ref _goldText, value); }
        public string PopText { get => _popText; set => Set(ref _popText, value); }
        public string WaveText { get => _waveText; set => Set(ref _waveText, value); }
        public string HonorText { get => _honorText; set => Set(ref _honorText, value); }
        public string LevelText { get => _levelText; set => Set(ref _levelText, value); }
        public string SpeedText { get => _speedText; set => Set(ref _speedText, value); }
        public string EntityInfoText { get => _entityInfoText; set => Set(ref _entityInfoText, value); }

        public string UpgradeInfoTitle { get => _upgradeInfoTitle; set => Set(ref _upgradeInfoTitle, value); }
        public string UpgradeInfoDesc { get => _upgradeInfoDesc; set => Set(ref _upgradeInfoDesc, value); }
        public string UpgradeCostText { get => _upgradeCostText; set => Set(ref _upgradeCostText, value); }

        public string ResultTitle { get => _resultTitle; set => Set(ref _resultTitle, value); }
        public string ResultReward { get => _resultReward; set => Set(ref _resultReward, value); }
        public Brush ResultColor { get => _resultColor; set => Set(ref _resultColor, value); }

        public Visibility MenuVisibility { get => _menuVisibility; set => Set(ref _menuVisibility, value); }
        public Visibility LevelSelectVisibility { get => _levelSelectVisibility; set => Set(ref _levelSelectVisibility, value); }
        public Visibility ShopVisibility { get => _shopVisibility; set => Set(ref _shopVisibility, value); }
        public Visibility GameVisibility { get => _gameVisibility; set => Set(ref _gameVisibility, value); }
        public Visibility ResultVisibility { get => _resultVisibility; set => Set(ref _resultVisibility, value); }

        public Visibility EntityInfoVisibility { get => _entityInfoVisibility; set => Set(ref _entityInfoVisibility, value); }
        public Visibility MultiSelectionVisibility { get => _multiSelectionVisibility; set => Set(ref _multiSelectionVisibility, value); }
        public Visibility SingleSelectionVisibility { get => _singleSelectionVisibility; set => Set(ref _singleSelectionVisibility, value); }
        public Visibility UnitTabVisibility { get => _unitTabVisibility; set => Set(ref _unitTabVisibility, value); }
        public Visibility BuildTabVisibility { get => _buildTabVisibility; set => Set(ref _buildTabVisibility, value); }
        public Visibility SkillsVisibility { get => _skillsVisibility; set => Set(ref _skillsVisibility, value); }
        public Visibility TowerBtnVisibility { get => _towerBtnVisibility; set => Set(ref _towerBtnVisibility, value); }

        public Visibility WinButtonsVisibility { get => _winButtonsVisibility; set => Set(ref _winButtonsVisibility, value); }
        public Visibility LoseButtonsVisibility { get => _loseButtonsVisibility; set => Set(ref _loseButtonsVisibility, value); }
        public Visibility UpgradeBtnVisibility { get => _upgradeBtnVisibility; set => Set(ref _upgradeBtnVisibility, value); }
        public string SelectedUnitName { get; set; }
        public ImageSource SelectedUnitIcon { get; set; }
        public string SelectedUnitStats { get; set; }
        public double SelectedUnitHp { get; set; }
        public double SelectedUnitSmoothHp { get; set; }
        public double SelectedUnitMaxHp { get; set; }
        public SolidColorBrush SelectedUnitHpColor { get; set; }
        public double SelectedUnitMana { get; set; }
        public double SelectedUnitMaxMana { get; set; }

        public ShellViewModel()
        {
            _engine = new GameEngine();
            DisplayName = _engine.GameTitle;

            // 初始化资源管理器 (只需调用一次)
            AssetManager.Init(_engine.BaseUnitStats, _engine.EffectConfigs);

            InitializeShopList(); // 初始化商店列表

            // 绑定引擎事件
            _engine.OnResourceUpdate += UpdateResources;
            _engine.OnGameOver += HandleGameOver;
            _engine.OnSelectionChanged += RefreshActionButtons;

            // 渲染循环
            CompositionTarget.Rendering += OnRendering;

            // UI 更新定时器 (低频)
            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _uiTimer.Tick += (s, e) => UpdateResources();
            _uiTimer.Start();

            _lastRenderTime = TimeSpan.FromTicks(DateTime.Now.Ticks);

            GoToMenu();
        }

        private void OnRendering(object sender, EventArgs e)
        {
            var args = (RenderingEventArgs)e;

            if (args.RenderingTime == _lastRenderTime) return;

            double dt = (args.RenderingTime - _lastRenderTime).TotalSeconds;
            _lastRenderTime = args.RenderingTime;

            // 防止异常卡顿导致 dt 过大
            if (dt > 0.1) dt = 0.1;

            _engine.Update(dt);
        }

        // =========================================================
        // [新增] 核心辅助方法：从配置提取第一帧作为图标
        // 解决了 UI 层找不到 AssetManager.GetIcon 的问题
        // =========================================================
        private ImageSource GetUnitIcon(string key)
        {
            // 1. 尝试从单位配置找
            if (_engine.BaseUnitStats.TryGetValue(key, out var stats) && stats.SpriteConfig != null)
            {
                return CropIcon(stats.SpriteConfig.Image, stats.SpriteConfig.FrameW, stats.SpriteConfig.FrameH);
            }
            // 2. 尝试从建筑配置找
            if (_engine.BuildingRegistry.TryGetValue(key, out var bStats) && bStats.SpriteConfig != null)
            {
                return CropIcon(bStats.SpriteConfig.Image, bStats.SpriteConfig.FrameW, bStats.SpriteConfig.FrameH);
            }
            // 3. 兜底：尝试从 AssetManager 的静态缓存拿
            if (AssetManager.StaticSprites.ContainsKey(key))
            {
                return AssetManager.StaticSprites[key];
            }
            // [Fix] Adapt to SpriteFrame change
            var animFrame = AssetManager.CreateAnimator(key)?.GetCurrentFrame();
            if (animFrame.HasValue)
            {
                return new CroppedBitmap(animFrame.Value.Sheet, animFrame.Value.SourceRect);
            }
            return null;
        }

        private ImageSource CropIcon(string imagePath, int w, int h)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath)) return null;
                // 处理 pack URI 路径 (WPF 资源路径格式)
                string fullPath = imagePath.StartsWith("pack:") ? imagePath : "pack://application:,,,/Assets/" + imagePath.TrimStart('/');

                // 如果是绝对路径，尝试相对化
                if (imagePath.Contains(":")) fullPath = new Uri(imagePath).AbsoluteUri;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(fullPath, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                // 裁剪 (0,0) 位置作为图标
                return new CroppedBitmap(bitmap, new Int32Rect(0, 0, w, h));
            }
            catch
            {
                return null;
            }
        }

        // =========================================================
        // 商店与关卡逻辑
        // =========================================================

        private void InitializeShopList()
        {
            ShopUnitList.Clear();
            foreach (var kvp in _engine.BaseUnitStats)
            {
                // 只显示我方单位
                if (kvp.Value.Faction == "Human")
                {
                    ShopUnitList.Add(new ShopUnitItem
                    {
                        UnitKey = kvp.Key,
                        Name = kvp.Value.Name,
                        Icon = GetUnitIcon(kvp.Key), // [绑定图标]
                        OnClick = (o) => SelectShopUnit((ShopUnitItem)o)
                    });
                }
            }
        }

        public void GoToLevelSelect()
        {
            StageList.Clear();
            foreach (var s in _engine.Stages.Values)
            {
                StageList.Add(new StageItemModel
                {
                    StageId = s.Id,
                    Title = s.Title,
                    IsLocked = s.Id > _engine.Player.MaxUnlockedStage,
                    OnClick = (o) => StartGame(((StageItemModel)o).StageId)
                });
            }
            SetVisibility(levelSelect: true);
        }

        // =========================================================
        // 游戏内操作面板逻辑
        // =========================================================

        private void RefreshActionButtons()
        {
            UnitButtons.Clear();
            BuildingButtons.Clear();
            SkillButtons.Clear();
            SelectedUnitsList.Clear();

            SkillsVisibility = Visibility.Collapsed;
            TowerBtnVisibility = Visibility.Collapsed;
            UnitTabVisibility = Visibility.Collapsed;
            BuildTabVisibility = Visibility.Collapsed;
            SingleSelectionVisibility = Visibility.Collapsed;
            MultiSelectionVisibility = Visibility.Collapsed;

            var sel = _engine.SelectedEntity;
            int selectionCount = _engine.SelectedEntities.Count;

            // Handle Multi Selection Display
            if (selectionCount > 1)
            {
                MultiSelectionVisibility = Visibility.Visible;
                EntityInfoVisibility = Visibility.Visible;
                foreach (var e in _engine.SelectedEntities)
                {
                    string key = (e is Unit u1) ? u1.Key : (e is Building b1 ? b1.Id : "");
                    SelectedUnitsList.Add(new UnitInfoModel
                    {
                        Entity = e,
                        Icon = GetUnitIcon(key),
                        IsSelected = e == sel,
                        HpPct = e.MaxHP > 0 ? e.HP / e.MaxHP : 0,
                        HpColor = (e.HP / e.MaxHP) > 0.5 ? Brushes.LimeGreen : Brushes.Red,
                        OnClick = (o) => { _engine.SelectedEntity = ((UnitInfoModel)o).Entity; RefreshActionButtons(); }
                    });
                }
            }
            else if (sel != null && !(sel is Obstacle))
            {
                SingleSelectionVisibility = Visibility.Visible;
                EntityInfoVisibility = Visibility.Visible;

                // [Fix] Reset HP values on selection change to avoid smoothing artifacts
                SelectedUnitHp = sel.HP;
                SelectedUnitSmoothHp = sel.HP;
                SelectedUnitMaxHp = sel.MaxHP;
                NotifyOfPropertyChange("SelectedUnitHp");
                NotifyOfPropertyChange("SelectedUnitSmoothHp");
                NotifyOfPropertyChange("SelectedUnitMaxHp");

                // Update Static Info
                if (sel is Unit uInfo)
                {
                    SelectedUnitName = uInfo.Stats.Name + $" (Lv.{_engine.GetUnitLevel(uInfo.Key)})";
                    SelectedUnitIcon = GetUnitIcon(uInfo.Key);
                    SelectedUnitStats = $"Dmg: {uInfo.Damage}   Arm: {uInfo.Stats.DefType}";

                }
                else if (sel is Building bInfo)
                {
                    SelectedUnitName = bInfo.Name;
                    SelectedUnitIcon = GetUnitIcon(bInfo.Id);
                    SelectedUnitStats = bInfo.Damage > 0 ? $"Dmg: {bInfo.Damage}" : "Structure";
                }
                NotifyOfPropertyChange("SelectedUnitName");
                NotifyOfPropertyChange("SelectedUnitStats");
                NotifyOfPropertyChange("SelectedUnitIcon");
            }
            else
            {
                EntityInfoVisibility = Visibility.Collapsed;
            }

            // A. 选中我方单位 -> 显示技能
            if (sel is Unit u && u.Team == Shared.Enums.TeamType.Human)
            {
                // --- Add Basic Commands ---
                SkillButtons.Add(new SkillButtonModel
                {
                    Name = "Move",
                    Hotkey = "M",
                    Cost = 0,
                    OnClick = (o) => { _engine.EnterTargetingMode(); } // Simplified move targeting
                });
                SkillButtons.Add(new SkillButtonModel
                {
                    Name = "Stop",
                    Hotkey = "S",
                    Cost = 0,
                    OnClick = (o) => { foreach (var e in _engine.SelectedEntities.OfType<Unit>()) e.OrderStop(); }
                });
                SkillButtons.Add(new SkillButtonModel
                {
                    Name = "Hold",
                    Hotkey = "H",
                    Cost = 0,
                    OnClick = (o) => { foreach (var e in _engine.SelectedEntities.OfType<Unit>()) e.OrderStop(); /* Should imply Hold Position */ }
                });
                SkillButtons.Add(new SkillButtonModel
                {
                    Name = "Attack",
                    Hotkey = "A",
                    Cost = 0,
                    OnClick = (o) => { _engine.EnterAttackMoveMode(); }
                });

                foreach (var s in u.SkillBook)
                {
                    SkillButtons.Add(new SkillButtonModel
                    {
                        Key = s.Definition.Key,
                        Name = s.Definition.Name,
                        Cost = s.Definition.Cost,
                        MaxCD = s.Definition.CD,
                        Desc = s.Definition.Desc,
                        Hotkey = "Q", // Needs better logic
                        OnClick = (o) =>
                        {
                            if (_engine.SelectedEntity is Unit unit)
                                _engine.PrepareSkill(unit, ((SkillButtonModel)o).Key);
                        }
                    });
                }
                SkillsVisibility = Visibility.Visible;
                return;
            }

            // B. 选中我方建筑 -> 显示生产列表
            if (sel is Building b && b.Team == Shared.Enums.TeamType.Human)
            {
                if (b.Damage > 0) TowerBtnVisibility = Visibility.Visible;

                var info = _engine.Buildings.FirstOrDefault(x => x.Name == b.Name);
                if (info != null && info.Produces.Count > 0)
                {
                    foreach (var key in info.Produces)
                    {
                        if (_engine.BaseUnitStats.TryGetValue(key, out var stats))
                        {
                            UnitButtons.Add(new ActionButtonModel
                            {
                                Id = key,
                                Name = stats.Name,
                                Cost = stats.Cost,
                                IsUnit = true,
                                Icon = GetUnitIcon(key), // [绑定图标]
                                Hotkey = "T",
                                OnClick = (o) =>
                                {
                                    _engine.SpawnUnit(((ActionButtonModel)o).Id, TeamType.Human);
                                    UpdateResources();
                                }
                            });
                        }
                    }
                    UnitTabVisibility = Visibility.Visible;
                    return;
                }
            }

            // C. 默认状态（无选中或选中无关物体） -> 显示建造列表
            if (selectionCount == 0)
            {
                BuildTabVisibility = Visibility.Visible;

                foreach (var bp in _engine.Buildings)
                {
                    // 过滤掉敌方建筑和特殊建筑
                    if (bp.Id.ToLower().Contains("orc") || bp.Id == "stronghold") continue;

                    int count = _engine.Entities.Count(e => e is Building bd && bd.Name == bp.Name && bd.Team == Shared.Enums.TeamType.Human);

                    BuildingButtons.Add(new ActionButtonModel
                    {
                        Id = bp.Id,
                        Name = bp.Name,
                        Cost = bp.Cost,
                        IsUnit = false,
                        IsMax = count >= bp.Max,
                        Icon = GetUnitIcon(bp.Id), // [绑定图标]
                        Hotkey = "B",
                        OnClick = (o) => _engine.StartPlacingBuilding(((ActionButtonModel)o).Id)
                    });
                }
            }
        }

        // =========================================================
        // 导航与状态管理
        // =========================================================
        public void GoToMenu() { _engine.State = GameState.Menu; SetVisibility(menu: true); }
        public void GoToShop() { UpdateShopState(); SetVisibility(shop: true); if (ShopUnitList.Count > 0) SelectShopUnit(ShopUnitList[0]); }

        public void StartGame(int level)
        {
            _engine.Start(level);
            LevelText = _engine.CurrentStageInfo?.Title ?? $"第 {level} 关";
            SpeedText = "1x";
            _engine.TimeScale = 1.0;
            SetVisibility(game: true);
            RefreshActionButtons();
            UpdateResources();
        }

        public void NextLevel() => StartGame(_engine.Stage + 1);
        public void RetryLevel() => StartGame(_engine.Stage);

        private void SetVisibility(bool menu = false, bool levelSelect = false, bool shop = false, bool game = false, bool result = false)
        {
            MenuVisibility = menu ? Visibility.Visible : Visibility.Collapsed;
            LevelSelectVisibility = levelSelect ? Visibility.Visible : Visibility.Collapsed;
            ShopVisibility = shop ? Visibility.Visible : Visibility.Collapsed;
            GameVisibility = game ? Visibility.Visible : Visibility.Collapsed;
            ResultVisibility = result ? Visibility.Visible : Visibility.Collapsed;
        }

        public void ToggleSpeed()
        {
            if (_engine.TimeScale == 1.0) { _engine.TimeScale = 2.0; SpeedText = "2x"; }
            else { _engine.TimeScale = 1.0; SpeedText = "1x"; }
        }

        public void UseTower() { _engine.EnterTargetingMode(); }

        public void SelectShopUnit(ShopUnitItem item)
        {
            if (_selectedShopUnit != null) _selectedShopUnit.IsSelected = false;
            _selectedShopUnit = item;
            if (_selectedShopUnit != null) _selectedShopUnit.IsSelected = true;
            UpdateShopDetail();
        }

        public void DoUpgrade()
        {
            if (_selectedShopUnit != null && _engine.BuyUnitUpgrade(_selectedShopUnit.UnitKey))
            {
                UpdateShopState();
                UpdateShopDetail();
            }
        }

        private void UpdateShopState()
        {
            HonorText = $"荣誉点: {_engine.Player.Honor}";
            foreach (var item in ShopUnitList) item.CurrentLevel = _engine.GetUnitLevel(item.UnitKey);
        }

        private void UpdateShopDetail()
        {
            if (_selectedShopUnit == null)
            {
                UpgradeInfoTitle = "未选择";
                UpgradeInfoDesc = "请选择左侧兵种";
                UpgradeBtnVisibility = Visibility.Collapsed;
                return;
            }

            UpgradeInfoTitle = _selectedShopUnit.Name;
            var next = _engine.GetNextUpgrade(_selectedShopUnit.UnitKey);

            if (next != null)
            {
                UpgradeInfoDesc = $"【下一级效果】\n{next.Desc}";
                UpgradeCostText = $"花费: {next.Cost} 荣誉";
                UpgradeBtnVisibility = Visibility.Visible;
            }
            else
            {
                UpgradeInfoDesc = "已升至顶级";
                UpgradeCostText = "MAX LEVEL";
                UpgradeBtnVisibility = Visibility.Collapsed;
            }
        }

        private void UpdateResources()
        {
            if (_engine.State != GameState.Playing && _engine.State != GameState.Paused) return;

            var currentGold = (int)_engine.Gold;
            var currentPop = _engine.GetUnitCount();
            var currentMaxPop = _engine.MaxPop;
            var currentWave = Math.Round(_engine.AiWaveLevel, 1);

            var goldChanged = currentGold != _lastGold;
            if (goldChanged)
            {
                GoldText = $"{currentGold}";
            }

            if (currentPop != _lastPop || currentMaxPop != _lastMaxPop)
            {
                PopText = $"{currentPop}/{currentMaxPop}";
            }

            if (!currentWave.Equals(_lastWave))
            {
                WaveText = $"{currentWave:F1}";
            }

            // [新增] Update UI State for Feedback
            if (goldChanged)
            {
                foreach (var btn in UnitButtons) btn.UpdateState(_engine.Gold);
                foreach (var btn in BuildingButtons) btn.UpdateState(_engine.Gold);
            }

            _lastGold = currentGold;
            _lastPop = currentPop;
            _lastMaxPop = currentMaxPop;
            _lastWave = currentWave;

            // Update Dynamic Unit Info (HP, Mana, CD)
            if (EntityInfoVisibility == Visibility.Visible && _engine.SelectedEntity != null)
            {
                var e = _engine.SelectedEntity;
                if (e.HP <= 0)
                {
                    RefreshActionButtons(); // Unit died, refresh selection
                }
                else
                {
                    SelectedUnitHp = e.HP;
                    SelectedUnitMaxHp = e.MaxHP;

                    // Smooth HP Logic (Damage Trail)
                    if (SelectedUnitSmoothHp < SelectedUnitHp)
                    {
                        SelectedUnitSmoothHp = SelectedUnitHp; // Heal: snap instantly
                    }
                    else if (SelectedUnitSmoothHp > SelectedUnitHp)
                    {
                        SelectedUnitSmoothHp += (SelectedUnitHp - SelectedUnitSmoothHp) * 0.1; // Decay
                    }

                    SelectedUnitHpColor = (e.HP / e.MaxHP) > 0.25 ? Brushes.LimeGreen : Brushes.Red;

                    if (e is Unit u)
                    {
                        SelectedUnitMana = u.Mana;
                        SelectedUnitMaxMana = u.Stats.MaxMana;

                        // Update Skills State
                        if (u.SkillBook.Count > 0)
                        {
                            foreach (var sb in SkillButtons)
                            {
                                var rs = u.SkillBook.FirstOrDefault(s => s.Definition.Key == sb.Key);
                                if (rs != null) { sb.CurrentCD = rs.CurrentCooldown; sb.UpdateState(u.Mana); }
                            }
                        }
                    }
                    else if (e is Building b)
                    {
                        SelectedUnitMana = 0; // Buildings usually don't have mana
                        if (b.MaxCD > 0) SelectedUnitStats = $"CD: {b.CurrentCD:F1}s";
                    }
                    NotifyOfPropertyChange("SelectedUnitMana");
                    NotifyOfPropertyChange("SelectedUnitMaxMana");
                    NotifyOfPropertyChange("SelectedUnitHpColor");
                    NotifyOfPropertyChange("SelectedUnitHp");
                    NotifyOfPropertyChange("SelectedUnitSmoothHp");
                    NotifyOfPropertyChange("SelectedUnitMaxHp");
                    NotifyOfPropertyChange("SelectedUnitStats");

                }
            }

            // Update Multi Selection HP Bars
            if (MultiSelectionVisibility == Visibility.Visible)
            {
                foreach (var item in SelectedUnitsList)
                {
                    if (item.Entity != null)
                    {
                        item.HpPct = item.Entity.MaxHP > 0 ? item.Entity.HP / item.Entity.MaxHP : 0;
                        item.HpColor = item.HpPct > 0.5 ? Brushes.LimeGreen : Brushes.Red;
                        // Trigger property change if needed, but BindableCollection items might not notify automatically unless they are ViewModels
                        // For simple polling, we might need to force refresh or use specific VM for item
                    }
                }
                SelectedUnitsList.Refresh(); // Force refresh list UI
            }
        }

        private void HandleGameOver(bool win, int reward)
        {
            ResultTitle = win ? "VICTORY" : "DEFEAT";
            ResultColor = win ? Brushes.Gold : Brushes.Red;
            ResultReward = $"荣誉奖励: +{reward}";

            WinButtonsVisibility = win ? Visibility.Visible : Visibility.Collapsed;
            LoseButtonsVisibility = win ? Visibility.Collapsed : Visibility.Visible;

            SetVisibility(result: true);
        }

        public void HandleKeyDown(System.Windows.Input.Key key)
        {
            if (_engine.State != GameState.Playing) return;

            string keyStr = key.ToString();

            // Check Building Buttons
            if (BuildTabVisibility == Visibility.Visible)
            {
                var btn = BuildingButtons.FirstOrDefault(b => b.Hotkey == keyStr);
                if (btn != null) btn.Execute();
            }

            // Check Unit Buttons
            if (UnitTabVisibility == Visibility.Visible)
            {
                var btn = UnitButtons.FirstOrDefault(b => b.Hotkey == keyStr);
                if (btn != null) btn.Execute();
            }

            // Check Skill Buttons
            if (SkillsVisibility == Visibility.Visible)
            {
                var btn = SkillButtons.FirstOrDefault(b => b.Hotkey == keyStr);
                if (btn != null)
                {
                    btn.Execute();
                    return;
                }
            }

            // Global Commands
            // (Only if not already handled by a button above, though button priority should be fine)
        }
    }
}
