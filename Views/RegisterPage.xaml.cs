namespace Projet_Budget_M1.Views
{
    public partial class RegisterPage : ContentPage
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            var fullName = FullNameEntry.Text;
            var email = EmailEntry.Text;
            var password = PasswordEntry.Text;

            // Validation basique - seulement nom et email requis
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            {
                await DisplayAlert("Erreur", "Veuillez remplir le nom et l'email", "OK");
                return;
            }

            if (!IsValidEmail(email))
            {
                await DisplayAlert("Erreur", "Veuillez entrer une adresse email valide", "OK");
                return;
            }

            // Simulation de l'inscription
            try
            {
                await Task.Delay(1000); // Simule un appel API - 1s
                await DisplayAlert("Succès", $"Inscription réussie pour {fullName} ({email})", "OK");
                
                // Navigation vers le dashboard après inscription réussie
                Application.Current!.Windows[0].Page = new AppShell();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur d'inscription: {ex.Message}", "OK");
            }
        }

        private async void OnLoginTapped(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
