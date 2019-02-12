using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Caliburn.Micro;

using ReactiveUI;
using ReactiveUI.XamForms;
using VM = Cross.DataVault.ViewModels;

//Services
using XLabs.Platform.Device;

namespace Cross.DataVault.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PasswordView : ReactiveContentPage<VM.PasswordViewModel>
    {
        public PasswordView()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);

            //Reactive
            this.ViewModel = IoC.Get<VM.PasswordViewModel>();
            if (this.ViewModel != null)
            {
                #region UI
                this.ViewModel.WhenAnyValue(w => w.ReloadData).Subscribe(x =>
                {
                    if (x)
                    {
                        if (this.ViewModel.Passwords != null)
                        {
                            this.PasswordListView.ItemsSource = this.ViewModel.Passwords;

                            if (this.ViewModel.Passwords.Count != 0)
                            {
                                InitialInstructions.Opacity = 0;
                                this.SubParentView.LowerChild(InitialInstructions);
                                this.SubParentView.RaiseChild(PasswordListView);
                            }
                            else
                            {
                                InitialInstructions.Opacity = 1;
                                this.SubParentView.RaiseChild(InitialInstructions);
                            }

                            this.SubParentView.RaiseChild(this.FloatingButton);
                            this.SubParentView.RaiseChild(this.RefreshButton);
                        }
                    }
                });
                #endregion

                #region Animations
                //Register Subscriptions
                this.ViewModel.WhenAnyValue(w => w.Animate).Subscribe(async x =>
                {
                    if (x)
                    {
                        this.ParentView.RaiseChild(this.PasswordLoader);
                        await this.PasswordLoader.FadeTo(0.8, 250, Easing.Linear);
                    }
                    else
                    {
                        this.ParentView.LowerChild(this.PasswordLoader);
                        await this.PasswordLoader.FadeTo(0, 250, Easing.Linear);
                    }
                });
                #endregion
            }
        }

        private void PasswordListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var Cell = sender as ListView;
            if (Cell != null)
                Cell.SelectedItem = null;
        }
    }
}