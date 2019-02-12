using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Cross.DataVault.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PhotoViewerView : ContentPage
    {
        public PhotoViewerView()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);
        }

        private void PinchGestureRecognizer_PinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            if(e.Status == GestureStatus.Running)
            {

            }
        }
    }
}