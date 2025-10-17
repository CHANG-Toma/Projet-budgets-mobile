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

            // Validation basique
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Erreur", "Veuillez remplir tous les champs", "OK");
                return;
            }

            if (!IsValidEmail(email))
            {
                await DisplayAlert("Erreur", "Veuillez entrer une adresse email valide", "OK");
                return;
            }

            // Simulation de la connexion
            await DisplayAlert("Connexion", $"Connexion en cours pour {email}", "OK");
            
            // permet denaviguer vers la page principale
            //await Navigation.PushAsync(new MainPage());
        }

        private async void OnSignUpTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Inscription", "Fonctionnalité d'inscription à venir", "OK");
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
