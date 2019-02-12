using System;

//Caliburn
using Caliburn.Micro;
using Caliburn.Micro.Xamarin.Forms;

//Xamarin Forms
using Xamarin.Forms;

//Services
using Cross.DataVault.Data.Services;
using Cross.DataVault.Services.DependencyServices;
using Cross.DataVault.Services;
using ReactiveUI;

namespace Cross.DataVault.ViewModels
{
    public class SplashScreenViewModel : BaseScreen
    {
        protected readonly string _SplashResource = string.Empty;

        public SplashScreenViewModel(INavigationService _navigation, IDatabase _database, ILogging _logging, IDialogue _dialogue) : base(_navigation, _database, _logging, _dialogue)
        {
            //Depending on the Platform and OS the resource will change accordingly

            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                case Device.Android:
                    _SplashResource = "splashscreen.jpg";
                    break;

                default:
                    _SplashResource = "~/Assets/splashscreen.jpg";

                    break;
            }

            Device.StartTimer(TimeSpan.FromSeconds(4), () =>
            {
                if (_navigation != null)
                    _navigation.NavigateToViewModelAsync<LoginViewModel>(true);

                return false;
            });

            //Set Splash Screen Resource

            SplashScreen = _SplashResource;
        }

        private string _SplashScreen;
        public string SplashScreen
        {
            get
            {
                return _SplashScreen;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _SplashScreen, value);
            }
        }
    }
}
