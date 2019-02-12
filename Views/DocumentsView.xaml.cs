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
    public partial class DocumentsView : ReactiveContentPage<VM.DocumentsViewModel>
    {
        //Constants
        private const string _DocumentsUpdate = "_DocumentsUpdate";

        public DocumentsView()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);
            
            //Reactive
            this.ViewModel = IoC.Get<VM.DocumentsViewModel>();
            if (this.ViewModel != null)
            {

                #region UI
                //Avatar
                this.ViewModel.WhenAnyValue(x => x.ReloadData).Subscribe(sender =>
                {
                    if (sender)
                    {
                        if (this.ViewModel.Documents != null)
                        {
                            this.DocumentsListView.ItemsSource = this.ViewModel.Documents;

                            if (this.ViewModel.Documents.Count != 0)
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
                #endregion

                #region Animations
                //Register Subscriptions
                this.ViewModel.WhenAnyValue(w => w.Animate).Subscribe(async x =>
                {
                    if (x)
                    {
                        this.ParentView.RaiseChild(this.DocumentsLoader);
                        await this.DocumentsLoader.FadeTo(0.8, 250, Easing.Linear);
                    }
                    else
                    {
                        this.ParentView.LowerChild(this.DocumentsLoader);
                        await this.DocumentsLoader.FadeTo(0, 250, Easing.Linear);
                    }
                });
                #endregion
            }
        }

        private void DocumentsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var Cell = sender as ListView;
            if (Cell != null)
                Cell.SelectedItem = null;
        }
    }
}