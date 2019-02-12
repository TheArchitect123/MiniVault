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
    public partial class MusicView : ReactiveContentPage<VM.MusicViewModel>
    {
        //Constants
        private const string _MusicUpdate = "_MusicUpdate";

        public MusicView()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);

            //Reactive
            this.ViewModel = IoC.Get<VM.MusicViewModel>();
            if (this.ViewModel != null)
            {
                this.ViewModel.WhenAnyValue(x => x.ReloadData).Subscribe(sender =>
                {
                    if (sender)
                    {
                        if (this.ViewModel.Music != null)
                        {
                            this.MusicListView.ItemsSource = this.ViewModel.Music;

                            if (this.ViewModel.Music.Count != 0)
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
                        this.ParentView.RaiseChild(this.MusicLoader);
                        await this.MusicLoader.FadeTo(0.8, 250, Easing.Linear);
                    }
                    else
                    {
                        this.ParentView.LowerChild(this.MusicLoader);
                        await this.MusicLoader.FadeTo(0, 250, Easing.Linear);
                    }
                });
                #endregion
            }
        }
        
        private void MusicListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var Cell = sender as ListView;
            if (Cell != null)
                Cell.SelectedItem = null;
        }
    }
}