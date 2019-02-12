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
    public partial class PDFView : ReactiveContentPage<VM.PDFViewModel>
    {
        public PDFView()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);
            
            //Reactive
            this.ViewModel = IoC.Get<VM.PDFViewModel>();
            if (this.ViewModel != null)
            {
                #region Animations

                this.ViewModel.WhenAnyValue(w => w.ReloadData).Subscribe(x =>
                {
                    if (x)
                    {
                        if (this.ViewModel.PDFs != null)
                        {
                            this.PDFsListView.ItemsSource = this.ViewModel.PDFs;

                            if (this.ViewModel.PDFs.Count != 0)
                            {
                                InitialInstructions.Opacity = 0;
                                this.ParentView.LowerChild(InitialInstructions);
                                this.ParentView.RaiseChild(PDFsListView);
                            }
                            else
                            {
                                InitialInstructions.Opacity = 1;
                                this.ParentView.RaiseChild(InitialInstructions);
                            }
                            
                            this.ParentView.RaiseChild(this.RefreshButton);
                        }
                    }
                });

                //Register Subscriptions
                this.ViewModel.WhenAnyValue(w => w.Animate).Subscribe(async x =>
                {
                    if (x)
                    {
                        this.ParentView.RaiseChild(this.LoaderCarrier);
                        await this.LoaderCarrier.FadeTo(0.8, 250, Easing.Linear);
                    }
                    else
                    {
                        this.ParentView.LowerChild(this.LoaderCarrier);
                        await this.LoaderCarrier.FadeTo(0, 250, Easing.Linear);
                    }
                });
                #endregion
            }
        }

        private void PDFsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var Cell = sender as ListView;
            if (Cell != null)
                Cell.SelectedItem = null;
        }
    }
}