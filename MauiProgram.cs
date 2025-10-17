using Microsoft.Extensions.Logging;
using Projet_Budget_M1.Services;
using Projet_Budget_M1.ViewModels;
using Projet_Budget_M1.Views;
using Projet_Budget_M1.Utils;

namespace Projet_Budget_M1
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Enregistrement des services
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            
            // Enregistrement des ViewModels
            builder.Services.AddTransient<MainPageViewModel>();
            
            // Configuration du ServiceProvider pour ViewModelLocator
            var app = builder.Build();
            ServiceHelper.ServiceProvider = app.Services;

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return app;
        }
    }
}
