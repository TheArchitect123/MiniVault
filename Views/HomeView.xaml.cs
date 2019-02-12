using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using ReactiveUI;
using ReactiveUI.XamForms;
using Caliburn.Micro;

using VM = Cross.DataVault.ViewModels;

//Services 
using XLabs.Platform.Device;

namespace Cross.DataVault.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomeView : ReactiveContentPage<VM.HomeViewModel>
    {
        public HomeView()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);
            this.ParentView.LowerChild(this.LoaderView);

            //Reactive
            this.ViewModel = IoC.Get<VM.HomeViewModel>();
            if (this.ViewModel != null)
            {

                #region UI
                //Avatar
                this.ViewModel.WhenAnyValue(w => w.Avatar).Subscribe(x =>
                {
                    if (x != null)
                    {
                        if (x.Length != 0)
                        {
                            //Load Avatar from byte[] Collection
                            //This requires an avatar (Image View) to be rendered in the Drawer
                            MemoryStream avatarStream = new MemoryStream(x);
                            AvatarPhoto.Source = ImageSource.FromStream(() => { return avatarStream; });
                        }
                        else
                            AvatarPhoto.Opacity = 0;
                    }
                    else
                        AvatarPhoto.Opacity = 0;
                });
                #endregion

                #region Animations
                //Register Subscriptions
                this.ViewModel.WhenAnyValue(w => w.Animate).Subscribe(async x =>
                {
                    if (x)
                    {
                        this.ParentView.RaiseChild(this.LoaderView);
                        await this.LoaderView.FadeTo(0.8, 250, Easing.Linear);
                    }
                    else
                    {
                        this.ParentView.LowerChild(this.LoaderView);
                        await this.LoaderView.FadeTo(0, 250, Easing.Linear);
                    }
                });
                #endregion
            }

            var DeviceManager = IoC.Get<IDevice>();
            if (DeviceManager != null)
            {
                //Any Device specific UI 
                this.drawer.DrawerLength = DeviceManager.Display.Xdpi * 0.75f;
                this.HomeNavView.WidthRequest = DeviceManager.Display.Xdpi * 0.75f;
            }
        }

        #region Animations
        private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var Cell = sender as ListView;
            Cell.SelectedItem = null;
        }

        #endregion
    }
}