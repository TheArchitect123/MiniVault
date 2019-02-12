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
    public partial class ContactsView : ReactiveContentPage<VM.ContactsViewModel>
    {
        //Constants
        private const string _ContactsUpdate = "_ContactsUpdate";

        public ContactsView()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);

            //Reactive
            this.ViewModel = IoC.Get<VM.ContactsViewModel>();
            if (this.ViewModel != null)
            {
                this.ViewModel.WhenAnyValue(x => x.ReloadData).Subscribe(sender =>
                {
                    if (sender)
                    {
                        if (this.ViewModel.Contacts != null)
                        {
                            this.ContactListView.ItemsSource = this.ViewModel.Contacts;

                            if (this.ViewModel.Contacts.Count != 0)
                            {
                                InitialInstructions.Opacity = 0;
                                this.SubParentView.LowerChild(InitialInstructions);
                            }
                            else
                            {
                                InitialInstructions.Opacity = 1;
                                this.SubParentView.RaiseChild(InitialInstructions);
                            }

                            this.SubParentView.RaiseChild(this.RefreshButton);
                        }
                    }
                });

                #region Animations
                //Register Subscriptions
                this.ViewModel.WhenAnyValue(w => w.Animate).Subscribe(async x =>
                {
                    if (x)
                    {
                        this.ParentView.RaiseChild(this.ContactsLoader);
                        await this.ContactsLoader.FadeTo(0.8, 250, Easing.Linear);
                    }
                    else
                    {
                        this.ParentView.LowerChild(this.ContactsLoader);
                        await this.ContactsLoader.FadeTo(0, 250, Easing.Linear);
                    }
                });
                #endregion
            }
        }

        private void ContactListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var Cell = sender as ListView;
            if (Cell != null)
                Cell.SelectedItem = null;
        }
    }
}