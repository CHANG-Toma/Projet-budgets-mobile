using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Projet_Budget_M1.Models;
using Projet_Budget_M1.Services;

namespace Projet_Budget_M1.ViewModels
{
    public class BudgetViewModel : INotifyPropertyChanged
    {
        private string _currentUserEmail = string.Empty;
        private DateTime _currentMonth;
        private string _monthLabel = string.Empty;
        private double _totalBudget;
        private double _totalSpent;
        private double _remaining;
        private double _progressPercentage;
        private double _progressBarWidth;
        private Color _progressBarColor = Color.FromArgb("#27AE60");
        private bool _isLoading;
        private double _salary;
        private double _savings;

        public ObservableCollection<BudgetCategorieViewModel> Categories { get; } = new();

        // Propriétés bindables
        public string MonthLabel
        {
            get => _monthLabel;
            set { _monthLabel = value; OnPropertyChanged(); }
        }

        public double TotalBudget
        {
            get => _totalBudget;
            set { _totalBudget = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalBudgetText)); }
        }

        public double TotalSpent
        {
            get => _totalSpent;
            set { _totalSpent = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalSpentText)); }
        }

        public double Remaining
        {
            get => _remaining;
            set { _remaining = value; OnPropertyChanged(); OnPropertyChanged(nameof(RemainingText)); OnPropertyChanged(nameof(RemainingColor)); }
        }

        public double ProgressPercentage
        {
            get => _progressPercentage;
            set { _progressPercentage = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressPercentageText)); }
        }

        public double ProgressBarWidth
        {
            get => _progressBarWidth;
            set { _progressBarWidth = value; OnPropertyChanged(); }
        }

        public Color ProgressBarColor
        {
            get => _progressBarColor;
            set { _progressBarColor = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public double Salary
        {
            get => _salary;
            set { _salary = value; OnPropertyChanged(); OnPropertyChanged(nameof(SalaryText)); OnPropertyChanged(nameof(Savings)); OnPropertyChanged(nameof(SavingsText)); }
        }

        public double Savings
        {
            get => _savings;
            set { _savings = value; OnPropertyChanged(); OnPropertyChanged(nameof(SavingsText)); OnPropertyChanged(nameof(SavingsColor)); }
        }

        // Propriétés formatées pour l'affichage
        public string TotalBudgetText => $"{TotalBudget:F2} €";
        public string TotalSpentText => $"{TotalSpent:F2} €";
        public string RemainingText => Remaining >= 0 
            ? $"{Math.Abs(Remaining):F2} €" 
            : $"⚠️ {Math.Abs(Remaining):F2} € en négatif";
        public string ProgressPercentageText => $"{ProgressPercentage:F0}%";
        public Color RemainingColor => Remaining >= 0 ? Color.FromArgb("#27AE60") : Color.FromArgb("#E74C3C");
        public string SalaryText => Salary > 0 ? $"{Salary:F2} €" : "Non défini";
        public string SavingsText => Savings >= 0 
            ? $"{Savings:F2} €" 
            : $"⚠️ {Math.Abs(Savings):F2} € en négatif";
        public Color SavingsColor => Savings >= 0 ? Color.FromArgb("#27AE60") : Color.FromArgb("#E74C3C");

        // Commandes
        public ICommand LoadDataCommand { get; }
        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand AddCategoryCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand EditSalaryCommand { get; }

        public BudgetViewModel()
        {
            _currentUserEmail = Preferences.Default.Get("userEmail", "");
            _currentMonth = DateTime.Now;

            LoadDataCommand = new Command(async () => await LoadBudgetDataAsync());
            PreviousMonthCommand = new Command(async () => await ChangeToPreviousMonth());
            NextMonthCommand = new Command(async () => await ChangeToNextMonth());
            AddCategoryCommand = new Command<string>(async (categoryName) => await AddCategoryAsync(categoryName));
            RefreshCommand = new Command(async () => await LoadBudgetDataAsync());
            EditSalaryCommand = new Command(async () => await EditSalaryAsync());
        }

        public async Task InitializeAsync()
        {
            await LoadBudgetDataAsync();
        }

        private async Task LoadBudgetDataAsync()
        {
            if (string.IsNullOrWhiteSpace(_currentUserEmail)) return;

            IsLoading = true;
            try
            {
                // S'assurer que les catégories par défaut existent (premier accès uniquement)
                // Les catégories par défaut sont affichées automatiquement, pas besoin de les créer en BD

                // Mettre à jour le label du mois
                MonthLabel = _currentMonth.ToString("MMMM yyyy");

                // Charger le total des revenus (salaire + revenus supplémentaires)
                Salary = await DbService.GetTotalRevenusAsync(_currentUserEmail, _currentMonth);

                // Charger les budgets par catégorie
                var budgets = await DbService.GetCategoryBudgetsAsync(_currentUserEmail, _currentMonth);
                System.Diagnostics.Debug.WriteLine($"LoadBudgetDataAsync: {budgets.Count} budgets chargés");

                // Calculer les totaux (seulement pour les budgets définis > 0)
                TotalBudget = budgets.Where(b => b.LimiteCategorie > 0).Sum(b => b.LimiteCategorie);
                TotalSpent = budgets.Sum(b => Math.Abs(b.Depense));
                Remaining = TotalBudget - TotalSpent;
                
                // Calculer l'épargne = salaire - total des budgets alloués
                // L'épargne peut être négative si les budgets dépassent le salaire
                Savings = Salary > 0 ? Salary - TotalBudget : 0;
                
                ProgressPercentage = TotalBudget > 0 ? (TotalSpent / TotalBudget) * 100 : 0;

                // Mettre à jour la barre de progression (max 300 pixels)
                ProgressBarWidth = TotalBudget > 0 ? Math.Min((TotalSpent / TotalBudget) * 300, 300) : 0;

                // Changer la couleur selon le pourcentage
                if (ProgressPercentage >= 100)
                    ProgressBarColor = Color.FromArgb("#E74C3C"); // Rouge
                else if (ProgressPercentage >= 80)
                    ProgressBarColor = Color.FromArgb("#F39C12"); // Orange
                else
                    ProgressBarColor = Color.FromArgb("#27AE60"); // Vert

                // Trier les catégories : celles sans budget en haut, celles avec budget en bas
                var sortedBudgets = budgets.OrderBy(b => b.LimiteCategorie > 0 ? 1 : 0)
                                          .ThenBy(b => b.NomCategorie)
                                          .ToList();

                // Charger les catégories dans l'ObservableCollection
                Categories.Clear();
                foreach (var budget in sortedBudgets)
                {
                    var categoryVM = new BudgetCategorieViewModel(budget, this);
                    Categories.Add(categoryVM);

                    // Vérifier les alertes
                    await CheckAndShowAlert(categoryVM);
                }
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Erreur LoadBudgetDataAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                // Afficher aussi dans la console
                Console.WriteLine($"❌ ERREUR LoadBudgetDataAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CheckAndShowAlert(BudgetCategorieViewModel category)
        {
            // Alerte à 80%
            if (category.Pourcentage >= 80 && category.Pourcentage < 100 && !category.AlertShown80)
            {
                category.AlertShown80 = true;
                await Application.Current.MainPage.DisplayAlert(
                    "⚠️ Alerte Budget",
                    $"La catégorie '{category.NomCategorie}' a atteint {category.Pourcentage:F0}% de son budget !",
                    "OK"
                );
            }
            // Alerte dépassement
            else if (category.Pourcentage >= 100 && !category.AlertShown100)
            {
                category.AlertShown100 = true;
                await Application.Current.MainPage.DisplayAlert(
                    "🚨 Dépassement Budget",
                    $"La catégorie '{category.NomCategorie}' a dépassé son budget de {Math.Abs(category.Restant):F2} € !",
                    "OK"
                );
            }
        }

        private async Task ChangeToPreviousMonth()
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            await LoadBudgetDataAsync();
        }

        private async Task ChangeToNextMonth()
        {
            _currentMonth = _currentMonth.AddMonths(1);
            await LoadBudgetDataAsync();
        }

        private async Task AddCategoryAsync(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName)) return;

            try
            {
                await DbService.AddCategoryAsync(_currentUserEmail, categoryName.Trim());
                await LoadBudgetDataAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Erreur",
                    $"Impossible de créer la catégorie: {ex.Message}",
                    "OK"
                );
            }
        }

        public async Task UpdateCategoryBudgetAsync(string nomCategorie, double newLimit)
        {
            try
            {
                await DbService.UpdateCategoryBudgetAsync(_currentUserEmail, nomCategorie, newLimit, _currentMonth);
                await LoadBudgetDataAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Erreur",
                    $"Impossible de mettre à jour le budget: {ex.Message}",
                    "OK"
                );
            }
        }

        private async Task EditSalaryAsync()
        {
            // Afficher les revenus existants et permettre d'en ajouter
            var revenus = await DbService.GetRevenusAsync(_currentUserEmail, _currentMonth);
            var revenusText = revenus.Count > 0 
                ? string.Join("\n", revenus.Select(r => $"- {r.Libelle}: {r.Montant:F2} €"))
                : "Aucun revenu enregistré";
            
            var action = await Application.Current.MainPage.DisplayActionSheet(
                $"Revenus du mois ({revenusText})\n\nQue souhaitez-vous faire ?",
                "Annuler",
                null,
                "Ajouter un salaire",
                "Ajouter un revenu supplémentaire"
            );

            if (action == "Annuler" || string.IsNullOrEmpty(action)) return;

            var libelle = action.Contains("salaire") ? "Salaire" : "Revenu supplémentaire";
            var placeholder = "0";
            
            var result = await Application.Current.MainPage.DisplayPromptAsync(
                $"💰 {libelle}",
                $"Entrez le montant du {libelle.ToLower()}:",
                "Enregistrer",
                "Annuler",
                placeholder: placeholder,
                keyboard: Keyboard.Numeric
            );

            if (!string.IsNullOrWhiteSpace(result) && double.TryParse(result.Replace(",", "."), out var montant))
            {
                try
                {
                    await DbService.AddRevenuAsync(_currentUserEmail, montant, _currentMonth, libelle);
                    // Recharger pour mettre à jour tous les calculs
                    await LoadBudgetDataAsync();
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Erreur",
                        $"Impossible d'ajouter le revenu: {ex.Message}",
                        "OK"
                    );
                }
            }
        }

        public async Task DeleteCategoryAsync(int idCategorie, string nomCategorie)
        {
            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Supprimer la catégorie",
                $"Êtes-vous sûr de vouloir supprimer la catégorie '{nomCategorie}' ?",
                "Supprimer",
                "Annuler"
            );

            if (confirm)
            {
                try
                {
                    await DbService.DeleteCategoryAsync(_currentUserEmail, idCategorie);
                    await LoadBudgetDataAsync();
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Erreur",
                        $"Impossible de supprimer la catégorie: {ex.Message}",
                        "OK"
                    );
                }
            }
        }

        public async Task UpdateCategoryNameAsync(int idCategorie, string currentName)
        {
            var result = await Application.Current.MainPage.DisplayPromptAsync(
                "Modifier le nom",
                "Entrez le nouveau nom de la catégorie:",
                "Enregistrer",
                "Annuler",
                initialValue: currentName
            );

            if (!string.IsNullOrWhiteSpace(result))
            {
                try
                {
                    await DbService.UpdateCategoryNameAsync(_currentUserEmail, idCategorie, result.Trim());
                    await LoadBudgetDataAsync();
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Erreur",
                        $"Impossible de modifier le nom: {ex.Message}",
                        "OK"
                    );
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// ViewModel pour une catégorie de budget
    /// </summary>
    public class BudgetCategorieViewModel : INotifyPropertyChanged
    {
        private readonly BudgetViewModel _parentViewModel;
        private readonly BudgetCategorie _model;

        public int IdCategorie => _model.IdCategorie;
        public string NomCategorie => _model.NomCategorie;
        public double LimiteCategorie => _model.LimiteCategorie;
        public double Depense => Math.Abs(_model.Depense);
        public double Restant => _model.Restant;
        public double Pourcentage => _model.Pourcentage;
        public string Icone => _model.Icone;
        public string Couleur => _model.Couleur;

        // Propriétés formatées (Dépensé / Budget max)
        public string MontantsText => LimiteCategorie > 0 
            ? $"{Depense:F2} € / {LimiteCategorie:F2} €" 
            : $"{Depense:F2} € / Non défini";
        public string PourcentageText => LimiteCategorie > 0 ? $"{Pourcentage:F0}%" : "-";
        public string RestantText => LimiteCategorie > 0 
            ? (Restant >= 0 
                ? $"{Math.Abs(Restant):F2} € restant" 
                : $"⚠️ {Math.Abs(Restant):F2} € en négatif")
            : "Budget non défini";
        // La couleur de la barre : rouge si > 100%, sinon la couleur de la catégorie
        public Color CouleurBarre
        {
            get
            {
                if (LimiteCategorie > 0 && Pourcentage >= 100)
                    return Color.FromArgb("#E74C3C"); // Rouge si dépassement
                return Color.FromArgb(Couleur);
            }
        }
        // La barre de progression : 250 pixels max, même si > 100%
        // Si dépassement, la barre doit être pleine (250 pixels) et rouge
        public double BarreWidth
        {
            get
            {
                if (LimiteCategorie <= 0) return 0;
                var ratio = Depense / LimiteCategorie;
                // Si > 100%, la barre est pleine (250 pixels)
                return ratio >= 1.0 ? 250 : ratio * 250;
            }
        }
        public Color RestantColor => LimiteCategorie > 0 
            ? (Restant >= 0 ? Color.FromArgb("#27AE60") : Color.FromArgb("#E74C3C"))
            : Color.FromArgb("#7F8C8D");

        // Flags pour les alertes (pour ne les afficher qu'une fois)
        public bool AlertShown80 { get; set; }
        public bool AlertShown100 { get; set; }

        // Commandes pour éditer le budget, supprimer et modifier la catégorie
        public ICommand EditBudgetCommand { get; }
        public ICommand DeleteCategoryCommand { get; }
        public ICommand EditCategoryNameCommand { get; }

        public BudgetCategorieViewModel(BudgetCategorie model, BudgetViewModel parentViewModel)
        {
            _model = model;
            _parentViewModel = parentViewModel;
            EditBudgetCommand = new Command(async () => await OnEditBudget());
            DeleteCategoryCommand = new Command(async () => await OnDeleteCategory());
            EditCategoryNameCommand = new Command(async () => await OnEditCategoryName());
        }

        private async Task OnEditBudget()
        {
            var placeholder = LimiteCategorie > 0 ? LimiteCategorie.ToString("F2") : "0";
            var page = Application.Current?.Windows.FirstOrDefault()?.Page ?? Application.Current?.MainPage;
            if (page is null) return;

            var result = await page.DisplayPromptAsync(
                $"Budget - {NomCategorie}",
                "Entrez le budget mensuel pour cette catégorie:",
                "Enregistrer",
                "Annuler",
                placeholder: placeholder,
                keyboard: Keyboard.Numeric
            );

            if (string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            var normalized = result.Trim().Replace(" ", "").Replace(",", ".");
            if (!double.TryParse(
                    normalized,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var newLimit))
            {
                await page.DisplayAlert("Saisie invalide", "Entre un montant valide (ex: 250 ou 250,50).", "OK");
                return;
            }

            if (newLimit < 0)
            {
                await page.DisplayAlert("Saisie invalide", "Le budget ne peut pas etre negatif.", "OK");
                return;
            }

            await _parentViewModel.UpdateCategoryBudgetAsync(NomCategorie, newLimit);
            await page.DisplayAlert("Succes", $"Budget enregistre pour '{NomCategorie}'.", "OK");
        }

        private async Task OnDeleteCategory()
        {
            await _parentViewModel.DeleteCategoryAsync(IdCategorie, NomCategorie);
        }

        private async Task OnEditCategoryName()
        {
            await _parentViewModel.UpdateCategoryNameAsync(IdCategorie, NomCategorie);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

