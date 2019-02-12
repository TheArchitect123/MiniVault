using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using ReactiveUI;
using ReactiveUI.XamForms;
using VM = Cross.DataVault.ViewModels;

namespace Cross.DataVault.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NotesCreatorView : ReactiveContentPage<VM.NotesCreatorViewModel>
    {
        public NotesCreatorView()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);

            //Platform Specific Fonts
            if(Device.RuntimePlatform == Device.Android)
            {
                this.SubjectView.FontSize = 12;
                this.MessageView.FontSize = 12;
            }
            else
            {
                this.SubjectView.FontSize = 15;
                this.MessageView.FontSize = 15;
            }
        }
    }
}