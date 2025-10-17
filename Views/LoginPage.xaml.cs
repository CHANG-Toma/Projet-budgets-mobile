using Projet_Budget_M1.ViewModels;

namespace Projet_Budget_M1.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            var email = EmailEntry.Text;
            var password = PasswordEntry.Text;

            // Validation basique - seulement email requis
            if (string.IsNullOrWhiteSpace(email))
            {
                await DisplayAlert("Erreur", "Veuillez entrer votre email", "OK");
                return;
            }

            if (!IsValidEmail(email))
            {
                await DisplayAlert("Erreur", "Veuillez entrer une adresse email valide", "OK");
                return;
            }

            // Simulation de la connexion
            try
            {
                await Task.Delay(1000); // Simule un appel API - 1s
                
                // Navigation vers le dashboard après connexion réussie
                Application.Current!.Windows[0].Page = new AppShell();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur de connexion: {ex.Message}", "OK");
            }
        }

        private async void OnSignUpTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage());
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
