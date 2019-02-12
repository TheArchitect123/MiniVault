using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Caliburn.Micro;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using VM = Cross.DataVault.ViewModels;

using ReactiveUI;
using ReactiveUI.XamForms;

namespace Cross.DataVault.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginView : ReactiveContentPage<VM.LoginViewModel>
    {
        public LoginView()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);
            
            //Platform Specific Settings
            if(Device.RuntimePlatform == Device.Android)
            {
                this.Registration.FontSize = 10;
                this.RememberMe.FontSize = 12;
            }
            else
            {
                this.Registration.FontSize = 12;
                this.RememberMe.FontSize = 15;
            }

            this.ViewModel = IoC.Get<VM.LoginViewModel>();
            if (this.ViewModel != null)
            {

                //Data Binding

                //Register Subscriptions
                this.ViewModel.WhenAnyValue(w => w.Animate).Subscribe(async x =>
                {
                    if (x)
                    {
                        this.ParentView.RaiseChild(this.LoaderCarrier);
                        await this.LoaderCarrier.FadeTo(0.8, 250, Easing.Linear);
                    }
                    else
                    {
                        this.ParentView.LowerChild(this.LoaderCarrier);
                        await this.LoaderCarrier.FadeTo(0, 250, Easing.Linear);
                    }
                });
            }
        }
    }
}