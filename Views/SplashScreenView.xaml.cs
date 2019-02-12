using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Cross.DataVault.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SplashScreenView : ContentPage
    {
        public SplashScreenView()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);
        }
    }
}