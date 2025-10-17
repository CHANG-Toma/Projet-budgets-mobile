using Projet_Budget_M1.Utils;
using Projet_Budget_M1.ViewModels;

namespace Projet_Budget_M1
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
            // Configuration du ViewModel pour la page principale
            var mainPage = new Views.MainPage();
            mainPage.BindingContext = ServiceHelper.GetService<MainPageViewModel>();
            
            // Remplacement de la page par défaut
            Items.Clear();
            Items.Add(new ShellContent
            {
                Content = mainPage,
                Title = "Budget App"
            });
        }
    }
}
