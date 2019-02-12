using System;
using System.Timers;
using System.Threading.Tasks;
using System.Windows.Input;

using Caliburn.Micro;
using Caliburn.Micro.Xamarin.Forms;
using Xamarin.Forms;

//View Models
using Cross.DataVault.ViewModels;

//Extensions
using Cross.DataVault.Relays;

namespace Cross.DataVault.ViewModels.Cards
{
    public class HomeCardViewModel : PropertyChangedBase
    {
        protected readonly INavigationService navigation;

        //Events & Dleegates
        public event Init_Loader _Init_Loader;
        public delegate void Init_Loader(object sender);

        //Dismisses the Loader
        public event DismissAnim _DismissAnim;
        public delegate void DismissAnim();

        #region Navigation
        private bool _Navigating;
        public bool Navigating
        {
            get { return _Navigating; }
            set { _Navigating = value; }
        }

        #endregion

        #region Data 
        private int _ID;
        public int ID
        {
            get { return _ID; }
            set { _ID = value; }
        }  

        private string _Title;
        public string Title
        {
            get { return _Title; }
            set { this.Set(ref _Title, value); }
        }

        #endregion

        #region Commands


        private ICommand _IGoToPage;
        public ICommand IGoToPage
        {
            get { return _IGoToPage; }
            set { this.Set(ref _IGoToPage, value); }
        }

        public bool CanGoToPage() { return true; }
        public void GoToPage()
        {
            //Navigate to the specified Card Page
            string Instructions = "Loading " + Title;

            if (_Init_Loader != null)
                _Init_Loader.Invoke(Instructions);

            Timer navigationTimer = new Timer(1000);
            navigationTimer.AutoReset = false;
            navigationTimer.Start();

            navigationTimer.Elapsed += (e, s) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Navigating = true;
                    this.NotifyOfPropertyChange(nameof(Navigating));

                    if (navigation != null)
                    {
                        switch (ID)
                        {
                            case 0: //Notes
                                navigation.NavigateToViewModelAsync<NotesViewModel>(true);

                                break;

                            case 1: //PDF
                                navigation.NavigateToViewModelAsync<PDFViewModel>(true);

                                break;

                            case 2: //Photos & Videos
                                navigation.NavigateToViewModelAsync<PhotosVideosViewModel>(true);

                                break;

                            case 3: //Documents
                                navigation.NavigateToViewModelAsync<DocumentsViewModel>(true);

                                break;
                            case 4: //Password
                                navigation.NavigateToViewModelAsync<PasswordViewModel>(true);

                                break;
                            case 5: //Music
                                navigation.NavigateToViewModelAsync<MusicViewModel>(true);

                                break;

                            case 6: //Contacts
                                navigation.NavigateToViewModelAsync<ContactsViewModel>(true);

                                break;

                            case 7: //Emails
                                navigation.NavigateToViewModelAsync<EmailViewModel>(true);

                                break;
                        }
                    }

                    //Hide the Animation
                    if (_DismissAnim != null)
                        _DismissAnim.Invoke();
                });
            };
        }


        #endregion

        public HomeCardViewModel(INavigationService _navigation)
        {
            navigation = _navigation;

            //Relays
            IGoToPage = new RelayExtension(GoToPage, CanGoToPage);
        }
    }
}
