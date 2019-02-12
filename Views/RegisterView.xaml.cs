using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Caliburn.Micro;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ReactiveUI;
using ReactiveUI.XamForms;

using VM = Cross.DataVault.ViewModels;

namespace Cross.DataVault.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RegisterView : ReactiveContentPage<VM.RegisterViewModel>
    {
        public RegisterView()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);

            //Fonts
            if (Device.RuntimePlatform == Device.Android)
                this.RegisterButton.FontSize = 16;

            this.ViewModel = IoC.Get<VM.RegisterViewModel>();
            if (this.ViewModel != null)
            {
                //Data Binding
                //Between the controls and the Xaml Page
                this.ViewModel.WhenAnyValue(w => w.Avatar).Subscribe(async x =>
                {
                    try
                    {
                        var AvatarSource = ImageSource.FromUri(new Uri(x));
                        if (!string.IsNullOrWhiteSpace(x) && AvatarSource != null)
                            this.AvatarView.Source = AvatarSource;
                    }
                    catch { }
                });

                //Register Subscriptions

                this.ViewModel.WhenAnyValue(w => w.RegAnimate).Subscribe(async x =>
                {
                    if (x)
                    {
                        this.ParentView.RaiseChild(this.LoaderCarrier);
                        await this.LoaderCarrier.FadeTo(0.8, 250, Easing.Linear);
                    }
                    else
                    {
                        this.ParentView.LowerChild(this.LoaderCarrier);
                        this.ParentView.RaiseChild(this.NavBar);
                        await this.LoaderCarrier.FadeTo(0, 250, Easing.Linear);
                    }
                });
            }
        }
    }
}