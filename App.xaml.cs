using Projet_Budget_M1.Views;

namespace Projet_Budget_M1
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Afficher la page de connexion au démarrage
            return new Window(new LoginPage());
        }
    }
}