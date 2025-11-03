namespace Projet_Budget_M1.Views
{
    public partial class DashboardPage : ContentPage
    {
        public DashboardPage()
        {
            InitializeComponent();
            InitializeTransactionForm();
        }

        private void InitializeTransactionForm()
        {
            // Initialiser la date à aujourd'hui
            DatePicker.Date = DateTime.Now;
            
            // Définir la catégorie par défaut
            CategoryPicker.SelectedIndex = 0; // Alimentation
            CategoryIcon.Text = GetCategoryIcon("Alimentation");
        }

        private async void OnViewBudgetDetailsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Budget", "Afficher les détails du budget", "OK");
        }

        private async void OnViewAllTransactionsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Transactions", "Afficher toutes les transactions", "OK");
        }

        private void OnAddTransactionClicked(object sender, EventArgs e)
        {
            // Réinitialiser le formulaire
            ResetTransactionForm();
            
            // Afficher l'overlay
            TransactionOverlay.IsVisible = true;
        }

        private void OnCloseTransactionFormClicked(object sender, EventArgs e)
        {
            TransactionOverlay.IsVisible = false;
        }

        private void OnOverlayTapped(object sender, EventArgs e)
        {
            // Fermer l'overlay si on clique sur le fond (mais pas sur le formulaire)
            TransactionOverlay.IsVisible = false;
        }

        private void OnFormFrameTapped(object sender, EventArgs e)
        {
            // Empêcher la propagation de l'événement pour que le formulaire ne se ferme pas
            // quand on clique dessus
        }

        private void OnDateSelected(object sender, DateChangedEventArgs e)
        {
            // La date est automatiquement mise à jour dans le DatePicker
        }

        private void OnCategorySelected(object sender, EventArgs e)
        {
            // Mettre à jour l'icône selon la catégorie sélectionnée
            if (CategoryPicker.SelectedItem != null)
            {
                string category = CategoryPicker.SelectedItem.ToString();
                CategoryIcon.Text = GetCategoryIcon(category);
            }
        }

        private string GetCategoryIcon(string category)
        {
            return category switch
            {
                "Alimentation" => "🍽️",
                "Transport" => "🚗",
                "Logement" => "🏠",
                "Santé" => "🏥",
                "Loisirs" => "🎮",
                "Autres" => "📦",
                _ => "📋"
            };
        }

        private async void OnSaveTransactionClicked(object sender, EventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(TitleEntry.Text))
            {
                await DisplayAlert("Erreur", "Le titre est obligatoire", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(AmountEntry.Text) || 
                !double.TryParse(AmountEntry.Text, out double amount) || 
                amount <= 0)
            {
                await DisplayAlert("Erreur", "Le montant doit être un nombre positif", "OK");
                return;
            }

            // Ici, vous pouvez ajouter la logique pour sauvegarder la transaction
            // Par exemple, appeler un ViewModel ou un service
            
            string category = CategoryPicker.SelectedItem?.ToString() ?? "Non définie";
            string note = NoteEditor.Text;
            DateTime date = DatePicker.Date;

            // Afficher un message de confirmation (temporaire)
            await DisplayAlert("Dépense enregistrée", 
                $"Titre: {TitleEntry.Text}\n" +
                $"Montant: {amount} €\n" +
                $"Catégorie: {category}\n" +
                $"Date: {date:dd/MM/yyyy}", 
                "OK");

            // Fermer l'overlay
            TransactionOverlay.IsVisible = false;
            
            // Réinitialiser le formulaire
            ResetTransactionForm();
        }

        private void ResetTransactionForm()
        {
            TitleEntry.Text = string.Empty;
            AmountEntry.Text = "0.00";
            NoteEditor.Text = string.Empty;
            DatePicker.Date = DateTime.Now;
            CategoryPicker.SelectedIndex = 0;
            CategoryIcon.Text = GetCategoryIcon("Alimentation");
        }

        // Navigation vers les autres pages
        private async void OnHomeClicked(object sender, EventArgs e)
        {
            // On est déjà sur la page d'accueil
            await DisplayAlert("Navigation", "Vous êtes déjà sur la page d'accueil", "OK");
        }

        private async void OnTransactionsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//TransactionsPage");
        }

        private async void OnStatisticsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//StatisticsPage");
        }

        private async void OnBudgetClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//BudgetPage");
        }
    }
}
