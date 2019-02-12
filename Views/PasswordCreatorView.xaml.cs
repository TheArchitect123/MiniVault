using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using ReactiveUI;
using ReactiveUI.XamForms;
using VM = Cross.DataVault.ViewModels;

namespace Cross.DataVault.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PasswordCreatorView : ReactiveContentPage<VM.PasswordCreatorViewModel>
    {
        public PasswordCreatorView()
        {
            InitializeComponent();


            NavigationPage.SetHasNavigationBar(this, false);
        }
    }
}