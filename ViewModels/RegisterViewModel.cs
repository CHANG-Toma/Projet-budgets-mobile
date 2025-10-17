using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Projet_Budget_M1.Commands;

namespace Projet_Budget_M1.ViewModels
{
    public class RegisterViewModel : INotifyPropertyChanged
    {
        private string _fullName = string.Empty;
        private string _email = string.Empty;
        private string _password = string.Empty;
        private bool _isLoading;

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RegisterCommand { get; }
        public ICommand GoToLoginCommand { get; }

        public RegisterViewModel()
        {
            RegisterCommand = new RelayCommand(async () => await ExecuteRegisterAsync(), CanExecuteRegister);
            GoToLoginCommand = new RelayCommand(async () => await ExecuteGoToLoginAsync());
        }

        private async Task ExecuteRegisterAsync()
        {
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current!.Windows[0].Page!.DisplayAlert("Erreur", "Veuillez remplir tous les champs", "OK");
                return;
            }

            if (!IsValidEmail(Email))
            {
                await Application.Current!.Windows[0].Page!.DisplayAlert("Erreur", "Veuillez entrer une adresse email valide", "OK");
                return;
            }

            if (Password.Length < 6)
            {
                await Application.Current!.Windows[0].Page!.DisplayAlert("Erreur", "Le mot de passe doit contenir au moins 6 caractères", "OK");
                return;
            }

            IsLoading = true;

            try
            {
                // Simulation d'une inscription
                await Task.Delay(2000);
                
                await Application.Current!.Windows[0].Page!.DisplayAlert("Succès", $"Inscription réussie pour {FullName} ({Email})", "OK");
                
                // Ici vous pourriez naviguer vers la page de connexion ou la page principale
                // await NavigationService.NavigateToAsync<LoginViewModel>();
            }
            catch (Exception ex)
            {
                await Application.Current!.Windows[0].Page!.DisplayAlert("Erreur", $"Erreur d'inscription: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteGoToLoginAsync()
        {
            await Application.Current!.Windows[0].Page!.Navigation.PushAsync(new Views.LoginPage());
        }

        private bool CanExecuteRegister()
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(FullName) && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
