using Projet_Budget_M1.Views;

namespace Projet_Budget_M1
{
    public partial class App : Application
    {
        // Initialisation de l'application MAUI
        public App()
        {
            InitializeComponent();
        }

        // Fonction pour créer la fenêtre de l'application
        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Afficher la page de connexion au démarrage dans une NavigationPage
            return new Window(new NavigationPage(new LoginPage()));
        }
    }
}