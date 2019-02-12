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
    public partial class PhotosVideosView : ReactiveContentPage<VM.PhotosVideosViewModel>
    {
        private const string _UpdatePhotos = "_UpdatePhotos";

        public PhotosVideosView()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);

            //Reactive
            this.ViewModel = IoC.Get<VM.PhotosVideosViewModel>();
            if (this.ViewModel != null)
            {
                this.ViewModel.WhenAnyValue(w => w.ReloadData).Subscribe(x =>
                {
                    if (x)
                    {
                        if (this.ViewModel.Photos != null)
                        {
                            this.PhotosListView.ItemsSource = this.ViewModel.Photos;

                            if (this.ViewModel.Photos.Count != 0)
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
                    }
                });

                #region Animations
                //Register Subscriptions
                this.ViewModel.WhenAnyValue(w => w.Animate).Subscribe(async x =>
                {
                    if (x)
                    {
                        this.ParentView.RaiseChild(this.PhotosLoader);
                        await this.PhotosLoader.FadeTo(0.8, 250, Easing.Linear);
                    }
                    else
                    {
                        this.ParentView.LowerChild(this.PhotosLoader);
                        await this.PhotosLoader.FadeTo(0, 250, Easing.Linear);
                    }
                });
                #endregion
            }
        }

        private void PhotosListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var Cell = sender as ListView;
            if (Cell != null)
                Cell.SelectedItem = null;
        }
    }
}