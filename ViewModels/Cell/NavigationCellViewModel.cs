using System;
using System.Timers;

using System.Windows.Input;

using Caliburn.Micro;
using Caliburn.Micro.Xamarin.Forms;
using Xamarin.Forms;

using Cross.DataVault.ViewModels;

namespace Cross.DataVault.ViewModels.Cell
{
    public class NavigationCellViewModel : PropertyChangedBase
    {
        #region Data
        private string _Title;
        public string Title
        {
            get { return _Title; }
            set { this.Set(ref _Title, value); }
        }

        private string _Icon;
        public string Icon
        {
            get { return _Icon; }
            set { this.Set(ref _Icon, value); }
        }

        public int _ID { get; set; }

        #endregion

        #region Commands
        public event Handler _Handler;
        public delegate void Handler(object sender, EventArgs e);

        //Generates Bug Reports for the Site User
        public event EBugReportHandler _EBugReportHandler;
        public delegate void EBugReportHandler(object sender, EventArgs e);

        //Dismisses the Loader
        public event DismissAnim _DismissAnim;
        public delegate void DismissAnim();

        //Side Drawer Commands
        private ICommand _INavigate;
        public ICommand INavigate
        {
            get { return _INavigate; }
            set { this.Set(ref _INavigate, value); }
        }
        public bool CanNavigate() { return true; }
        public void Navigate()
        {
            if (_Handler != null)
                _Handler.Invoke("Loading " + Title, EventArgs.Empty);

            Timer navigationTimer = new Timer(1000);
            navigationTimer.AutoReset = false;
            navigationTimer.Start();

            navigationTimer.Elapsed += (e, s) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (_navigation != null)
                    {
                        if (_ID == 0) //Home
                            _navigation.NavigateToViewModelAsync<HomeViewModel>(true);
                        else if (_ID == 1) //Search 
                            IoC.Get<HomeViewModel>().OpenSearch(); //Run Animation that renders the search bar
                        else if (_ID == 2) // Notes
                            _navigation.NavigateToViewModelAsync<NotesViewModel>(true);
                        else if (_ID == 3) // PDFs
                            _navigation.NavigateToViewModelAsync<PDFViewModel>(true);
                        else if (_ID == 4) // Photos & Videos
                            _navigation.NavigateToViewModelAsync<PhotosVideosViewModel>(true);
                        else if (_ID == 5) // Passwords
                            _navigation.NavigateToViewModelAsync<PasswordViewModel>(true);
                        else if (_ID == 6) // Documents
                            _navigation.NavigateToViewModelAsync<ContactsViewModel>(true);
                        else if (_ID == 7) // Music
                            _navigation.NavigateToViewModelAsync<MusicViewModel>(true);
                        else if (_ID == 8) // Email
                            _navigation.NavigateToViewModelAsync<EmailViewModel>(true);
                        else if (_ID == 9) // Bug Report
                        {
                            if (_EBugReportHandler != null)
                                _EBugReportHandler.Invoke(string.Empty, EventArgs.Empty);
                        }
                        else
                            _navigation.GoBackAsync(true);
                    }

                    //Hide the Animation
                    if (_DismissAnim != null)
                        _DismissAnim.Invoke();
                });
            };
        }

        private void Generate_BugReport_ByID(int id)
        {
        }

        #endregion

        protected readonly INavigationService _navigation;

        public NavigationCellViewModel(INavigationService navigation)
        {
            _navigation = navigation;

            INavigate = new Relays.RelayExtension(Navigate, CanNavigate);
        }
    }
}
