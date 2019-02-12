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
    public partial class NotesView : ReactiveContentPage<VM.NotesViewModel>
    {
        public NotesView()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);

            //Reactive
            this.ViewModel = IoC.Get<VM.NotesViewModel>();
            if (this.ViewModel != null)
            {
                this.ViewModel.WhenAnyValue(w => w.ReloadData).Subscribe(x =>
                {
                    if (x)
                    {
                        this.NotesListView.ItemsSource = this.ViewModel.Notes;

                        if (this.ViewModel.Notes.Count != 0)
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
                        this.SubParentView.RaiseChild(this.FloatingButton);
                    }
                });

                #region Animations
                //Register Subscriptions
                this.ViewModel.WhenAnyValue(w => w.Animate).Subscribe(async x =>
                {
                    if (x)
                    {
                        this.ParentView.RaiseChild(this.NotesLoader);
                        await this.NotesLoader.FadeTo(0.8, 250, Easing.Linear);
                    }
                    else
                    {
                        this.ParentView.LowerChild(this.NotesLoader);
                        await this.NotesLoader.FadeTo(0, 250, Easing.Linear);
                    }
                });
                #endregion
            }
        }

        private void NotesListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var Cell = sender as ListView;
            if (Cell != null)
                Cell.SelectedItem = null;
        }
    }
}