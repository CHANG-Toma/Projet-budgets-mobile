using Projet_Budget_M1.Views;

namespace Projet_Budget_M1
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
            // Configuration de la page de connexion comme page principale
            Items.Clear();
            Items.Add(new ShellContent
            {
                Content = new LoginPage(),
                Title = "Connexion"
            });
        }
    }
}
