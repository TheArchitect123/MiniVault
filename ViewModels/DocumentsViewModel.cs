using System;
using System.Threading.Tasks;
using System.Linq;

using System.Collections.Generic;
using System.Windows.Input;

using Caliburn.Micro;
using Caliburn.Micro.Xamarin.Forms;
using Xamarin.Forms;

//View Models
using Cross.DataVault.ViewModels.Cards;
using Cross.DataVault.ViewModels.Cell;
using Cross.DataVault.ViewModels.Cell.Secure;

//Helpers
using Cross.DataVault.Infrastructure.Utilities;
using Cross.DataVault.Data.Mapper;

//Services
using Cross.DataVault.Services.DependencyServices;
using Cross.DataVault.Services.Managers;

//Data
using Cross.DataVault.Data;

using Cross.DataVault.Data.Services;
using Cross.DataVault.Services;
using ReactiveUI;

//Plugins
using Plugin.Connectivity;

namespace Cross.DataVault.ViewModels
{
    public class DocumentsViewModel : BaseScreen
    {
        protected readonly IDocumentManager _documentManager;

        public DocumentsViewModel(INavigationService _navigation, IDatabase _database, ILogging _logging, IDialogue _dialogue,
               IDialogue dialogue, IAccountManager _accountManager, IDocumentManager documentManager) : base(_navigation, _database, _logging, _dialogue)
        {
            _dialogue = dialogue;
            _documentManager = documentManager;

            var SiteUser = _accountManager.GetSiteUser_ByID<Cross.DataVault.Data.Account>(Constants.InMemory_ContactID);
            if (SiteUser != null)
            {
                SiteUserEmail = SiteUser.Email;
                SiteUserName = String.Format("{0} {1}", SiteUser.FirstName, SiteUser.LastName);
                Avatar = SiteUser.Avatar;
            }
            else
                throw new ArgumentNullException("Cannot find Site user Information. Please contact site administrator for assistance");

            Initials = String.Format("{0}{1}", SiteUserName.First().ToString().ToUpper(), SiteUserName.Last().ToString().ToLower());
            Title = "My Documents";

            //Relays
            IOpenDrawer = new Relays.RelayExtension(OpenDrawer, CanOpenDrawer);
            IOpenSearch = new Relays.RelayExtension(OpenSearch, CanOpenSearch);
            IGoBack = new Relays.RelayExtension(GoBack, CanGoBack);
            IOpenFloat = new Relays.RelayExtension(OpenFloat, CanOpenFloat);

            //Relays - Refresh Data
            IOnRefresh = new Relays.RelayExtension(OnRefresh, CanOnRefresh);

            //Initialization
            Initialize_Core();
        }

        #region Data
        private bool _ReloadData;
        public bool ReloadData
        {
            get { return _ReloadData; }
            private set
            {
                if (value && _ReloadData)
                    _ReloadData = false;

                this.RaiseAndSetIfChanged(ref _ReloadData, value);
            }
        }

        private List<DocumentsCellViewModel> _Documents;
        public List<DocumentsCellViewModel> Documents
        {
            get { return _Documents == null ? _Documents = new List<DocumentsCellViewModel>() : _Documents; }
        }

        #endregion

        #region Navigation Bar 
        //Search 
        private string _SearchQuery;
        public string SearchQuery
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_SearchQuery))
                    return "Search...";
                else
                    return _SearchQuery;
            }
            set { this.RaiseAndSetIfChanged(ref _SearchQuery, value); }
        }

        //Navigation Source
        private List<NavigationCellViewModel> _NavigationSource;
        public List<NavigationCellViewModel> NavigationSource
        {
            get { return _NavigationSource; }
            set { this.RaiseAndSetIfChanged(ref _NavigationSource, value); }
        }

        private bool _IsOpenSearch;
        public bool IsOpenSearch
        {
            get { return _IsOpenSearch; }
            set { this.RaiseAndSetIfChanged(ref _IsOpenSearch, value); }
        }

        private bool _IsOpenDrawer;
        public bool IsOpenDrawer
        {
            get { return _IsOpenDrawer; }
            set { this.RaiseAndSetIfChanged(ref _IsOpenDrawer, value); }
        }

        private string _Title;
        public string Title
        {
            get { return _Title; }
            set { this.RaiseAndSetIfChanged(ref _Title, value); }
        }

        private string _Initials;
        public string Initials
        {
            get { return _Initials; }
            set { this.RaiseAndSetIfChanged(ref _Initials, value); }
        }

        private string _SiteUserName;
        public string SiteUserName
        {
            get { return _SiteUserName; }
            set { this.RaiseAndSetIfChanged(ref _SiteUserName, value); }
        }

        private string _SiteUserEmail;
        public string SiteUserEmail
        {
            get { return _SiteUserEmail; }
            set { this.RaiseAndSetIfChanged(ref _SiteUserEmail, value); }
        }

        private byte[] _Avatar;
        public byte[] Avatar
        {
            get { return _Avatar; }
            set { this.RaiseAndSetIfChanged(ref _Avatar, value); }
        }
        #endregion

        #region Commands
        private ICommand _IOnRefresh;
        public ICommand IOnRefresh
        {
            get { return _IOnRefresh; }
            set { this.RaiseAndSetIfChanged(ref _IOnRefresh, value); }
        }

        public bool CanOnRefresh() { return true; }
        public void OnRefresh()
        {
            //Start querying the 
            Instructions = "Downloading Documents";
            Animate = true;

            //Diagnostics
            string Message = string.Empty; ;
            string StackTrace = string.Empty;
            bool _AnyError = false;

            Task.Run(() =>
            {
                //Query the user's data from the back end SSMS
                try
                {
                    Device.StartTimer(TimeSpan.FromSeconds(4), () =>
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            Refreshing = false;
                            Animate = false;
                        });
                        return false;
                    });

                    if (CrossConnectivity.Current.IsConnected)
                    {

                    }
                    else
                    {
                        if (dialogue != null)
                            dialogue.ShowAlert("mmm...Something went wrong", "Downloading your data requires an active internet connection");
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        Message = ex.InnerException.Message;
                        StackTrace = ex.InnerException.StackTrace;
                    }
                    else
                    {
                        Message = ex.Message;
                        StackTrace = ex.StackTrace;
                    }

                    var mEx = new Exceptions(logging, Message, StackTrace);
                    if (mEx != null)
                        mEx.HandleException(mEx, logging);
                }
                finally
                {
                    //dispose of any memory here

                }
            }).ContinueWith((e) =>
            {
                //Hide the animator when done

                //If any errors occur render them on the dialogue service

                Device.BeginInvokeOnMainThread(() => { Animate = true; });
            });
        }

        private ICommand _IGoBack;
        public ICommand IGoBack
        {
            get { return _IGoBack; }
            set { this.RaiseAndSetIfChanged(ref _IGoBack, value); }
        }

        public bool CanGoBack() { return true; }
        public void GoBack()
        {
            if (navigation != null)
                navigation.GoBackAsync(true);
        }


        private ICommand _IOpenDrawer;
        public ICommand IOpenDrawer
        {
            get { return _IOpenDrawer; }
            set { this.RaiseAndSetIfChanged(ref _IOpenDrawer, value); }
        }

        public bool CanOpenDrawer() { return true; }
        public void OpenDrawer()
        {
            //Place base code here
            if (IsOpenDrawer)
            {
                IsOpenDrawer = false;
                return;
            }
            else
            {
                IsOpenDrawer = true;
                return;
            }
        }

        private ICommand _IOpenFloat;
        public ICommand IOpenFloat
        {
            get { return _IOpenFloat; }
            set { this.RaiseAndSetIfChanged(ref _IOpenFloat, value); }
        }

        public bool CanOpenFloat() { return true; }
        public void OpenFloat()
        {
            if (dialogue != null)
                dialogue.ShowAlert("mmm...Something went wrong", "This feature is not yet available. The next version will allow you to add documents to your vault");
        }

        private ICommand _IOpenSearch;
        public ICommand IOpenSearch
        {
            get { return _IOpenSearch; }
            set { this.RaiseAndSetIfChanged(ref _IOpenSearch, value); }
        }

        public bool CanOpenSearch() { return true; }
        public void OpenSearch()
        {
            //Place base code here

            if (navigation != null)
                navigation.NavigateToViewModelAsync<SearchViewModel>(true);
        }

        #endregion

        #region Loader
        private bool _Refreshing;
        public bool Refreshing
        {
            get { return _Refreshing; }
            set { this.RaiseAndSetIfChanged(ref _Refreshing, value); }
        }

        private bool _Animate;
        public bool Animate
        {
            get { return _Animate; }
            set
            {
                this.RaiseAndSetIfChanged(ref _Animate, value);
            }
        }

        private string _Instructions;
        public string Instructions
        {
            get { return _Instructions; }
            set { this.RaiseAndSetIfChanged(ref _Instructions, value); }
        }

        #endregion

        #region Business Logic
        protected virtual void Initialize_Core()
        {
            //Generate collection of Notes
            Animate = true;
            Instructions = "Loading Documents";

            Task.Run(() =>
            {

                //Query any documents here and add them to the collection 

            }).ContinueWith((e) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ReloadData = true;
                    Animate = false;
                });
            });
        }

        #endregion
    }
}
