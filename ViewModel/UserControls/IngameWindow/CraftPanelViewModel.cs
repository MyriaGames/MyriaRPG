using Myria.Lib.Core.Entities.Items;
using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Utils;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.UserControls.IngameWindow
{
    public class CraftPanelViewModel : BaseViewModel
    {
        protected readonly Npc _npc;
        protected readonly Character _character;
        private readonly Action _goBack;

        public string Title => Localization.T("npc.craft.title");
        public string BtnBack => Localization.T("app.general.UI.back");
        public string BtnCraft => Localization.T("npc.craft.craft");
        public string QuantityLabel => Localization.T("npc.shop.quantity");
        public string MaxLabel => Localization.T("app.general.UI.max");
        public string IngredientsLabel => Localization.T("npc.craft.ingredients");

        public ObservableCollection<RecipeVm> Recipes { get; } = new();

        private RecipeVm _selectedRecipe;
        public RecipeVm SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                _selectedRecipe = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedName));
                OnPropertyChanged(nameof(SelectedIngredients));
                UpdateMaxCraftable();
                Quantity = 1;
                StatusMessage = "";
            }
        }

        public string SelectedName => SelectedRecipe?.Name ?? "";
        public ObservableCollection<IngredientVm> SelectedIngredients => SelectedRecipe?.Ingredients ?? new();

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (value < 1) value = 1;
                if (value > MaxCraftable && MaxCraftable > 0) value = MaxCraftable;
                if (MaxCraftable == 0) value = 0;

                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanCraft));
            }
        }

        private int _maxCraftable;
        public int MaxCraftable
        {
            get => _maxCraftable;
            set
            {
                _maxCraftable = value;
                OnPropertyChanged();
            }
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool CanCraft => SelectedRecipe != null && Quantity > 0 && Quantity <= MaxCraftable;

        public ICommand BackCommand { get; }
        public ICommand CraftCommand { get; }
        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand MaxQuantityCommand { get; }

        public CraftPanelViewModel(Npc npc, Character character, Action goBack)
        {
            _npc = npc;
            _character = character;
            _goBack = goBack;

            BackCommand = new RelayCommand(_goBack);
            CraftCommand = new RelayCommand(CraftSelected);
            IncreaseQuantityCommand = new RelayCommand(() => Quantity++);
            DecreaseQuantityCommand = new RelayCommand(() => Quantity--);
            MaxQuantityCommand = new RelayCommand(() => Quantity = MaxCraftable);

            LoadRecipes();
        }

        private void LoadRecipes()
        {
            Recipes.Clear();
            StatusMessage = "";

            string jobId = _npc.MasterJobId ?? "blacksmith";
            int playerKnowledge = JobXpService.GetLevel(JobManager.GetOrAdd(_character, jobId).KnowledgeXp);

            foreach (var r in CraftingService.GetRecipes(_npc.Id)
                                             .Where(r => r.RequiredKnowledgeLevel <= playerKnowledge))
            {
                Recipes.Add(new RecipeVm
                {
                    Id       = r.OutputId,
                    Name     = Localization.T($"item.{r.OutputId}"),
                    XpReward = r.XpReward,
                    Ingredients = new ObservableCollection<IngredientVm>(
                        r.Ingredients.Select(i => new IngredientVm
                        {
                            Id     = i.ItemId,
                            Name   = $"item.{i.ItemId}",
                            Amount = i.Amount
                        }))
                });
            }

            SelectedRecipe = Recipes.FirstOrDefault();
            if (SelectedRecipe == null)
                StatusMessage = Localization.T("npc.craft.noRecipes");
        }

        protected void UpdateMaxCraftable()
        {
            if (SelectedRecipe == null)
            {
                MaxCraftable = 0;
                StatusMessage = Localization.T("npc.craft.selectRecipe");
                return;
            }

            int max = int.MaxValue;

            foreach (var ingredient in SelectedRecipe.Ingredients)
            {
                var playerItem = _character.Inventory.Items.FirstOrDefault(i => i.Id == ingredient.Id);
                int playerAmount = playerItem?.StackSize ?? 0;
                ingredient.CharacterHas = playerAmount;

                if (ingredient.Amount > 0)
                {
                    int canMake = playerAmount / ingredient.Amount;
                    if (canMake < max)
                        max = canMake;
                }
            }

            MaxCraftable = max == int.MaxValue ? 0 : max;
            OnPropertyChanged(nameof(CanCraft));

            StatusMessage = MaxCraftable == 0
                ? Localization.T("npc.craft.notEnoughMaterials")
                : "";

            if (Quantity > MaxCraftable) Quantity = MaxCraftable;
            if (Quantity == 0 && MaxCraftable > 0) Quantity = 1;
        }

        private void CraftSelected()
        {
            if (SelectedRecipe == null || Quantity <= 0)
            {
                StatusMessage = Localization.T("npc.craft.selectRecipe");
                return;
            }
            _ = ExecuteCraft();
        }

        protected virtual Task ExecuteCraft()
        {
            CraftLocal();
            return Task.CompletedTask;
        }

        private void CraftLocal()
        {
            // Delegates to CraftExecutionService.Craft — the same method the multiplayer server
            // now calls (GameHub.Craft) — instead of this ViewModel's own independent copy, which
            // never refunded ingredients if the output item couldn't be created (only handled the
            // inventory-full case).
            var outcome = CraftExecutionService.Craft(_character, _npc, SelectedRecipe.Id, Quantity);

            if (!outcome.Success)
            {
                StatusMessage = outcome.Reason switch
                {
                    "missing_ingredients" => Localization.T("npc.craft.notEnoughMaterials"),
                    "inventory_full"      => Localization.T("npc.craft.inventoryFull"),
                    _                     => Localization.T("npc.craft.fail")
                };
                return;
            }

            StatusMessage = Localization.T("npc.craft.success", outcome.Amount, SelectedRecipe.Name);
            UpdateMaxCraftable();
            Quantity = 1;
        }
    }

    public class IngredientVm : BaseViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Amount { get; set; }

        private int _characterHas;
        public int CharacterHas
        {
            get => _characterHas;
            set { _characterHas = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayString)); }
        }

        public string AmountText => Amount.ToString();
        public bool HasEnough => CharacterHas >= Amount;

        public string DisplayString => $"{Localization.T(Name)}: {Amount} ({Localization.T("app.general.UI.owned")}: {CharacterHas})";

        public override string ToString() => Name;
    }

    public class RecipeVm : BaseViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public long XpReward { get; set; }
        public ObservableCollection<IngredientVm> Ingredients { get; set; } = new();

        public override string ToString() => Name;
    }
}
