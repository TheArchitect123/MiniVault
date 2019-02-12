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

//Helpers
using Cross.DataVault.Infrastructure.Utilities;
using Cross.DataVault.Data.Mapper;

//Data 
using Cross.DataVault.Data.Interface;
using Cross.DataVault.Data;

//Services
using Cross.DataVault.Data.Services;
using Cross.DataVault.Services;
using Cross.DataVault.Services.Managers;
using Cross.DataVault.Services.DependencyServices;
using Cross.DataVault.ServiceAccess; //Cloud Service APIs
using Cross.DataVault.ServiceAccess.Configuration;

using ReactiveUI;

//Plugins
using Plugin.Toasts;
using Plugin.Toasts.Options;
using Plugin.Connectivity;
using Plugin.Messaging;

namespace Cross.DataVault.ViewModels
{
    public class HomeViewModel : BaseScreen
    {
        public HomeViewModel(INavigationService _navigation, IDatabase _database,
            ILogging _logging, IToastNotificator toastNotifier,
            //Managers
            IAccountManager _accountManager, INotesManager _notesManager, IPhotoVideoManager _photoVideoManager, IPasswordManager _passwordManager,
            IContactManager _contactManager, IContactStore _contactStore, IDialogue _dialogue) : base(_navigation, _database, _logging, _dialogue)
        {
            //Managers
            accountManager = _accountManager;
            notesManager = _notesManager;
            passwordManager = _passwordManager;
            photoVideoManager = _photoVideoManager;
            contactManager = _contactManager;

            contactStore = _contactStore;

            //services
            _toastNotifier = toastNotifier;

            //Navigation Drawer Information
            Title = "Home";

            var SiteUser = _accountManager.GetSiteUser_ByID<Cross.DataVault.Data.Account>(Constants.InMemory_ContactID);
            if (SiteUser != null)
            {
                SiteUserEmail = SiteUser.Email;
                SiteUserName = String.Format("{0} {1}", SiteUser.FirstName, SiteUser.LastName);
                Avatar = SiteUser.Avatar;
                Initials = String.Format("{0}{1}", SiteUserName.First().ToString().ToUpper(), SiteUserName.Last().ToString().ToLower());
            }

            //Relays
            IOpenDrawer = new Relays.RelayExtension(OpenDrawer, CanOpenDrawer);
            IOpenSearch = new Relays.RelayExtension(OpenSearch, CanOpenSearch);

            //Relays - Refresh Data
            IOnRefresh = new Relays.RelayExtension(OnRefresh, CanOnRefresh);

            //Refresh Data
            OnRefresh();

            Initialize_Navigation();
            InitializeCards();
        }

        #region Services
        //Managers
        protected readonly IAccountManager accountManager;
        protected readonly INotesManager notesManager;
        protected readonly IPasswordManager passwordManager;
        protected readonly IContactManager contactManager;
        protected readonly IPhotoVideoManager photoVideoManager;


        //Dependency Services
        protected readonly IContactStore contactStore;
        protected readonly IToastNotificator _toastNotifier;

        #endregion


        #region Data
        private List<HomeCardViewModel> _Cards;
        public List<HomeCardViewModel> Cards
        {
            get { return _Cards; }
            set { this.RaiseAndSetIfChanged(ref _Cards, value); }
        }


        #endregion

        #region Loader & Animations
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

        //Opens the Animation Loading indicator, which starts the Animation Service via Reactive UI
        public void OnLoadDataShowAnim(object sender)
        {
            Animate = true;
            if (sender != null)
                Instructions = sender as string;
        }

        public void OnLoadDataHideAnim()
        {
            Animate = false;
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
        public async void OnRefresh()
        {
            //Start querying the 
            Instructions = "Downloading your Data";
            Animate = true;
            Refreshing = true;

            //Diagnostics
            string Message = string.Empty; ;
            string StackTrace = string.Empty;
            bool _AnyError = false;

            await Task.Run(() =>
            {
                //Query the user's data from the back end SSMS
                try
                {
                    DataVaultWebServiceClient dataService = new DataVaultWebServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.Data_InSecureUrl));
                    if (dataService.HasServiceAvailable())
                    {
                        #region Download Notes
                        var obj = new List<Notes>();
                        notesManager.Delete_AllNotesByContactID(Constants.InMemory_ContactID); //Clear all notes then download them

                        var notes = dataService._GetNotes_ByUserID(Constants.InMemory_ContactID);
                        if (notes._Notes.Count != 0)
                        {
                            notes._Notes.ForEach(w => obj.Add(LocalMapper.MapNote_FromServer(w)));

                            if (notesManager != null)
                                notesManager.AddNotes(obj);
                        }
                        #endregion

                        #region Passwords
                        var pObj = new List<Passwords>();
                        passwordManager.Delete_AllPasswordsByContactID(Constants.InMemory_ContactID); //Clear all passwords then download them

                        var passwords = dataService._GetPasswords_ByUserID(Constants.InMemory_ContactID)._Passwords;
                        if (passwords.Count != 0)
                        {
                            passwords.ForEach(w => pObj.Add(LocalMapper.MapPassword_FromServer(w)));

                            if (passwordManager != null)
                                passwordManager.AddPasswords(pObj);
                        }
                        #endregion

                        #region Photos

                        var photos = new List<PhotoVideo>();
                        photoVideoManager.Delete_PhotosByUserId(Constants.InMemory_ContactID); //Clear all photos then download them

                        var photos_server = dataService._GetPhotos_ByUserID(Constants.InMemory_ContactID);
                        if (photos_server._Photos.Count != 0)
                        {
                            photos_server._Photos.ForEach(w => photos.Add(LocalMapper.MapPhoto_FromServer(w)));

                            if (photoVideoManager != null)
                                photoVideoManager.AddPhoto_ByCollections(photos);
                        }
                        #endregion

                        #region Contacts
                        var curr = new List<Contact>();
                        contactManager.ClearContacts_ForUserID(Constants.InMemory_ContactID);

                        var server_contacts = dataService._GetContacts_ByUserID(Constants.InMemory_ContactID);
                        var contacts = contactStore.Get_ContactsFromStore<Contact>();
                        if (contacts != null)
                        {
                            contacts.ForEach(w =>
                            {
                                //Add Contact to the contact store for the particular account
                                w.Sys_Creation = DateTime.Now;
                                w.Sys_Transaction = DateTime.Now;

                                w.Contact_ID = contactManager.Get_NewContactID();
                                w.User_ID = Constants.InMemory_ContactID;

                                if (!curr.Contains(w))
                                    curr.Add(w);
                            });
                        }

                        if (server_contacts._Contacts != null)
                        {
                            server_contacts._Contacts.ForEach(w =>
                            {
                                if (!curr.Contains(LocalMapper.MapContact_FromServer(w)))
                                    curr.Add(LocalMapper.MapContact_FromServer(w));
                            });
                        }

                        if (contactManager != null && curr.Count != 0)
                            contactManager.AddContacts_ByDetails(curr);

                        #endregion
                    }
                    else
                        throw new InvalidOperationException("Web Service is not available. Leaving local data intact");
                }
                catch (Exception ex)
                {
                    _AnyError = true;
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
                Device.BeginInvokeOnMainThread(() =>
                {
                    Animate = false;
                    Refreshing = false;
                });
            });
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
            if (IsOpenSearch)
                IsOpenSearch = false;
            else
                IsOpenSearch = true;
        }

        #endregion

        #region Business Logic
        private void DismissDrawer(object sender, EventArgs e)
        {
            OpenDrawer();
            OnLoadDataShowAnim(sender);
        }

        private void Initialize_Navigation()
        {
            var navigationSource = new List<NavigationCellViewModel>() {

                new NavigationCellViewModel(navigation){
                     Title = "Notes",
                      Icon = "notes.png",
                          _ID = 2,

                },
                new NavigationCellViewModel(navigation){
                     Title = "PDFs",
                      Icon = "nav_pdf.png",
                          _ID = 3
                },

                new NavigationCellViewModel(navigation){
                     Title = "Photo & Video",
                      Icon = "photo_video.png",
                          _ID = 4
                },

                new NavigationCellViewModel(navigation){
                     Title = "Passwords",
                      Icon = "password.png",
                          _ID = 5
                },

                new NavigationCellViewModel(navigation){
                     Title = "Contacts",
                      Icon = "documents.png",
                          _ID = 6
                },

                new NavigationCellViewModel(navigation){
                     Title = "Music",
                      Icon = "music.png",
                          _ID = 7
                },

                 new NavigationCellViewModel(navigation){
                     Title = "Email",
                      Icon = "emails.png",
                          _ID = 8
                },

                   new NavigationCellViewModel(navigation){
                     Title = "Bug Report",
                      Icon = "bugreport.png",
                          _ID = 9
                },

                  new NavigationCellViewModel(navigation){
                     Title = "Sign Out",
                      Icon = "signout.png",
                          _ID = 10
                }
            };

            //Subscriptions
            navigationSource.ForEach(w =>
            {
                w._Handler += DismissDrawer;
                w._DismissAnim += OnLoadDataHideAnim;

                if (w._ID == 9)
                    w._EBugReportHandler += Generate_BugReport_ByID;
            });


            this.NavigationSource = navigationSource;
        }

        private void InitializeCards()
        {
            this.Cards = new List<HomeCardViewModel>();
            var cards = new List<HomeCardViewModel>()
            {
                //Notes
                new HomeCardViewModel(navigation)
                {
                     Title = "Notes",
                      ID = 0,
                },
                
                ////PDF
                new HomeCardViewModel(navigation)
                {
                     Title = "PDFs",
                      ID = 1
                },

                //Photos & Videos
                new HomeCardViewModel(navigation)
                {
                     Title = "Photos & Albums",
                      ID = 2
                },

                ////Documents
                new HomeCardViewModel(navigation)
                {
                     Title = "Documents",
                      ID = 3
                },

                //Passwords
                new HomeCardViewModel(navigation)
                {
                     Title = "Passwords",
                      ID = 4
                },

                new HomeCardViewModel(navigation)
                {
                     Title = "Music",
                      ID = 5
                },

                new HomeCardViewModel(navigation)
                {
                     Title = "Contacts",
                      ID = 6
                },
                  new HomeCardViewModel(navigation)
                {
                     Title = "Emails",
                      ID = 7
                },
            };

            cards.ForEach(w =>
            {
                w._Init_Loader += OnLoadDataShowAnim;
                w._DismissAnim += OnLoadDataHideAnim;

                this.Cards.Add(w);
            });
        }

        private async void Generate_BugReport_ByID(object sender, EventArgs e)
        {
            //Query any logs here
            Animate = true;
            Instructions = "Generating Bug Report";

            //Diagnostics
            string ErrorMessage = string.Empty;
            bool _HasError = false;

            await Task.Run(() =>
            {
                try
                {
                    var logs = this.logging.GetLogs<Log>();
                    if (logs.Count != 0)
                    {
                        var SiteUser = accountManager.GetSiteUser_ByID<Cross.DataVault.Data.Account>(Constants.InMemory_ContactID);
                        string Message = string.Empty;
                        logs.ForEach(w =>
                        {
                            if (string.IsNullOrWhiteSpace(SiteUser.SiteUser_DisplayName))
                                SiteUser.SiteUser_DisplayName = $"{SiteUser.FirstName} {SiteUser.LastName}";

                            //Generate Log Message -- format this right
                            Message += $"\n------------------------------------- \n Device: {this.MachineName} \n Contact Details:\n Full Name: {SiteUser.SiteUser_DisplayName} \nEmail: {SiteUser.Email} \n Contact Number: {SiteUser.Mobile} \n Date:" +
                        $" {w.Sys_Creation.ToString("m")}, {w.Sys_Creation.ToString("hh:mm tt")} \n Message:\n { w.Message} \nStackTrace:\n {w.StackTrace}";
                        });

                        Device.BeginInvokeOnMainThread(() => { CrossMessaging.Current.EmailMessenger.SendEmail("dan.developer789@gmail.com", "Log File: Mini Vault", Message); });
                    }
                    else
                        throw new ArgumentOutOfRangeException("There doesn't seem to be any log information to report");
                }
                catch (Exception ex)
                {
                    _HasError = true;

                    string Message = string.Empty;
                    string StackTrace = string.Empty;
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
            }).ContinueWith((ex) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Animate = false;
                    if (_HasError)
                        dialogue.ShowAlert("mmm...something went wrong", ErrorMessage);
                });
            });
        }

        #endregion
    }
}
