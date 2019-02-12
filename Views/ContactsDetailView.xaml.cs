using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using ReactiveUI.XamForms;

//View Models
using VM = Cross.DataVault.ViewModels;


namespace Cross.DataVault.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ContactsDetailView : ReactiveContentPage<VM.ContactsDetailViewModel>
    {
        public ContactsDetailView()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);
        }
    }
}