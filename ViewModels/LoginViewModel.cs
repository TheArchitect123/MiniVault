using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using Caliburn.Micro;
using Caliburn.Micro.Xamarin.Forms;
using Xamarin.Forms;

using Xamarin.Auth.XamarinForms;

//Services
using System.ServiceModel;

//Extensions
using Cross.DataVault.Relays;

//Helpers
using Cross.DataVault.Infrastructure.Utilities;
using Cross.DataVault.Data.Mapper;

//Services
using Cross.DataVault.Data.Services;
using Cross.DataVault.Services;
using Cross.DataVault.Services.Managers;
using Cross.DataVault.Services.DependencyServices;
using Cross.DataVault.ServiceAccess; //Cloud Service API
using Cross.DataVault.ServiceAccess.Configuration;

//Core Data
using Cross.DataVault.Data;
using ReactiveUI;

//Plugins
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Plugin.Connectivity;
using Plugin.Messaging;
using Plugin.Security.Core;

namespace Cross.DataVault.ViewModels
{
    public class LoginViewModel : BaseScreen
    {
        #region Data
        private string _Username;
        public string Username
        {
            get { return _Username; }
            set
            {
                HasError = false;
                this.RaiseAndSetIfChanged(ref _Username, value);
            }
        }

        private string _Password;
        public string Password
        {
            get { return _Password; }
            set
            {
                HasError = false;
                this.RaiseAndSetIfChanged(ref _Password, value);
            }
        }

        private bool _RememberMe;
        public bool RememberMe
        {
            get { return _RememberMe; }
            set { this.RaiseAndSetIfChanged(ref _RememberMe, value); }
        }
        private bool _HasError;
        public bool HasError
        {
            get { return _HasError; }
            set { this.RaiseAndSetIfChanged(ref _HasError, value); }
        }

        #endregion

        #region Animator
        private bool _Animate;
        public bool Animate
        {
            get { return _Animate; }
            set { this.RaiseAndSetIfChanged(ref _Animate, value); }
        }

        private string _Instructions;
        public string Instructions
        {
            get { return _Instructions; }
            set { this.RaiseAndSetIfChanged(ref _Instructions, value); }
        }

        #endregion

        #region Commands
        private ICommand _ILogin;
        public ICommand ILogin
        {
            get { return _ILogin; }
            set { this.RaiseAndSetIfChanged(ref _ILogin, value); }
        }

        public bool CanLogin() { return true; }
        public async void Login()
        {

            Animate = true;
            Instructions = "Validating Identity";
            bool _Failure = false;

            //Diagnostics
            string Message = string.Empty;
            string StackTrace = string.Empty;

            await Task.Run(() =>
            {
                try
                {
                    //Place base code here
                    //Authenticate Site User here
                    if (string.IsNullOrWhiteSpace(Username))
                        throw new ArgumentNullException("Username cannot be empty. Please try again");
                    if (string.IsNullOrWhiteSpace(Password))
                        throw new ArgumentNullException("Password cannot be empty. Please try again");

                    if (accountManager != null)
                    {
                        //Apple Test Account 
                        var hasher = new PasswordEncoder();
                        var hashedPassword = hasher.Encode(Password, EncryptType.SHA_512);
                        if (accountManager.AuthenticateSiteUser_ByCredentials(Username, hashedPassword))
                        {
                            var curr = accountManager.GetSiteUser_ByUsername<Account>(Username);
                            Constants.InMemory_ContactID = curr.Contact_ID_Ref;

                            #region Save Credentials to Keychain if Remember me is Enabled
                            var credentialsStore = Xamarin.Auth.AccountStore.Create();
                            var AccountDetails = new Xamarin.Auth.Account();
                            AccountDetails.Properties.Clear();

                            if (RememberMe)
                            {
                                AccountDetails.Username = Username;
                                AccountDetails.Properties.Add("RememberMe", "true");
                            }
                            else
                            {
                                AccountDetails.Username = Username;
                                AccountDetails.Properties.Add("RememberMe", "false");
                            }

                            credentialsStore.FindAccountsForService(Credentials_Service).ToList().Clear();
                            credentialsStore.Save(AccountDetails, Credentials_Service);
                            #endregion

                            try
                            {
                                var contacts = contactStore.Get_ContactsFromStore<Contact>();
                                if (contacts != null)
                                {
                                    if (contactManager != null)
                                    {
                                        var QueryContacts = contactManager.Get_Contacts_ByUserID<Contact>(curr.Contact_ID_Ref);
                                        contacts.ForEach(w =>
                                        {
                                            //Add Contact to the contact store for the particular account
                                            if (QueryContacts.SingleOrDefault(i => i.Contact_ID == w.Contact_ID && i.User_ID == curr.Contact_ID_Ref) == null)
                                            {
                                                w.Sys_Creation = DateTime.Now;
                                                w.Sys_Transaction = DateTime.Now;

                                                w.Contact_ID = contactManager.Get_NewContactID();
                                                w.User_ID = curr.Contact_ID_Ref;
                                                w.Mobile = curr.Mobile;

                                                //Add Contacts to Table
                                                if (contactManager != null)
                                                    contactManager.AddContact_ByDetails(w);
                                            }
                                        });
                                    }
                                }
                            }
                            catch (Exception eX)
                            {
                                string _Message = string.Empty;
                                string _StackTrace = string.Empty;
                                if (eX.InnerException != null)
                                {
                                    _Message = eX.InnerException.Message;
                                    _StackTrace = eX.InnerException.StackTrace;
                                }
                                else
                                {
                                    _Message = eX.Message;
                                    _StackTrace = eX.StackTrace;
                                }

                                var mEx = new Exceptions(logging, _Message, _StackTrace);
                                if (mEx != null)
                                    mEx.HandleException(mEx, logging);
                            }

                            try
                            {
                                ////// Query Contacts and Music
                                var music = musicStore.GetMusic_Collection<Music>();
                                if (music != null)
                                {
                                    if (musicManager != null)
                                    {
                                        var QueryMusic = musicManager.GetMusicCollection_ByContactID<Music>(curr.Contact_ID_Ref);
                                        music.ForEach(w =>
                                        {
                                            if (QueryMusic.SingleOrDefault(i => i.User_ID == curr.Contact_ID_Ref) == null)
                                            {
                                                w.Sys_Creation = DateTime.Now;
                                                w.Sys_Transaction = DateTime.Now;
                                                w.User_ID = curr.Contact_ID_Ref;

                                                //Add Music to Table
                                                if (musicManager != null)
                                                    musicManager.AddMusic(w);
                                            }
                                        });
                                    }
                                }

                            }
                            catch (Exception mX)
                            {
                                string _Message = string.Empty;
                                string _StackTrace = string.Empty;
                                if (mX.InnerException != null)
                                {
                                    _Message = mX.InnerException.Message;
                                    _StackTrace = mX.InnerException.StackTrace;
                                }
                                else
                                {
                                    _Message = mX.Message;
                                    _StackTrace = mX.StackTrace;
                                }

                                var mEx = new Exceptions(logging, _Message, _StackTrace);
                                if (mEx != null)
                                    mEx.HandleException(mEx, logging);
                            }
                        }
                        else
                        {
                            //Check Cloud Service for the Site user's membership and generate an account locally 
                            if (Username.Equals("apple", StringComparison.OrdinalIgnoreCase) && Password.Equals("password", StringComparison.OrdinalIgnoreCase))
                            {
                                //Generate Account
                                Account obj = new Account();
                                obj.Contact_ID_Ref = Guid.NewGuid().ToString();
                                obj.FirstName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase("Apple");
                                obj.LastName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase("Inc");
                                obj.SiteUser_DisplayName = $"{ obj.FirstName } { obj.LastName }";

                                obj.Sys_Creation = DateTime.Now;
                                obj.Sys_Transaction = DateTime.Now;

                                //Has the passwords on account generation and on login
                                obj.Username = Username;
                                obj.Password = hashedPassword;

                                obj.Mobile = "";
                                obj.Work = "";
                                obj.Email = Username;
                                obj.Home = "";

                                Constants.InMemory_ContactID = obj.Contact_ID_Ref;
                                accountManager.AddAccount_ByHashedPassword(obj);
                            }
                            else
                            {
                                DataVaultAccountServiceClient accountsClient = new DataVaultAccountServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.AccountsInSecureUrl));

                                var dataClient = accountsClient._Login_AccountForUserCredentials(Username, Password);
                                if (dataClient.SiteUser != null)
                                {
                                    var curr = dataClient.SiteUser;
                                    Constants.InMemory_ContactID = curr.User_ID;

                                    //Generate Account
                                    Account obj = new Account();
                                    obj.Contact_ID_Ref = curr.User_ID;
                                    obj.FirstName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(curr.First_Name);
                                    obj.LastName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(curr.Last_Name);
                                    obj.SiteUser_DisplayName = $"{ obj.FirstName } { obj.LastName }";

                                    obj.Sys_Creation = DateTime.Now;
                                    obj.Sys_Transaction = DateTime.Now;

                                    //Has the passwords on account generation and on login
                                    obj.Username = Username;
                                    obj.Password = hashedPassword;

                                    obj.Mobile = curr.Mobile;
                                    obj.Work = curr.Work;
                                    obj.Email = Username;
                                    obj.Home = curr.Home;
                                    obj.Avatar = curr.Avatar;

                                    accountManager.AddAccount_ByHashedPassword(obj); //Add account to local store

                                    #region Save Credentials to Keychain if Remember me is Enabled
                                    var credentialsStore = Xamarin.Auth.AccountStore.Create();
                                    var AccountDetails = new Xamarin.Auth.Account();
                                    AccountDetails.Properties.Clear();

                                    if (RememberMe)
                                    {
                                        AccountDetails.Username = Username;
                                        AccountDetails.Properties.Add("RememberMe", "true");
                                    }
                                    else
                                    {
                                        AccountDetails.Username = Username;
                                        AccountDetails.Properties.Add("RememberMe", "false");
                                    }

                                    credentialsStore.Save(AccountDetails, Credentials_Service);
                                    #endregion

                                    try
                                    {
                                        var contacts = contactStore.Get_ContactsFromStore<Contact>();
                                        if (contacts != null)
                                        {
                                            if (contactManager != null)
                                            {
                                                var QueryContacts = contactManager.Get_Contacts_ByUserID<Contact>(curr.User_ID);
                                                contacts.ForEach(w =>
                                                {
                                                //Add Contact to the contact store for the particular account
                                                if (QueryContacts.SingleOrDefault(i => i.Contact_ID == w.Contact_ID && i.User_ID == curr.User_ID) == null)
                                                    {
                                                        w.Sys_Creation = DateTime.Now;
                                                        w.Sys_Transaction = DateTime.Now;

                                                        w.Contact_ID = contactManager.Get_NewContactID();
                                                        w.User_ID = curr.User_ID;

                                                    //Add Contacts to Table
                                                    if (contactManager != null)
                                                            contactManager.AddContact_ByDetails(w);
                                                    }
                                                });
                                            }
                                        }

                                        var music = musicStore.GetMusic_Collection<Music>();

                                        // Query Contacts and Music
                                        if (music != null)
                                        {
                                            if (musicManager != null)
                                            {
                                                var QueryMusic = musicManager.GetMusicCollection_ByContactID<Music>(curr.User_ID);
                                                music.ForEach(w =>
                                                {
                                                    if (QueryMusic.SingleOrDefault(i => i.User_ID == curr.User_ID) == null)
                                                    {
                                                        w.Sys_Creation = DateTime.Now;
                                                        w.Sys_Transaction = DateTime.Now;
                                                        w.User_ID = curr.User_ID;

                                                    //Add Music to Table
                                                    if (musicManager != null)
                                                            musicManager.AddMusic(w);
                                                    }
                                                });
                                            }
                                        }
                                    }
                                    catch (Exception eX)
                                    {
                                        string oMessage = string.Empty;
                                        string oStackTrace = string.Empty;

                                        if (eX.InnerException != null)
                                        {
                                            oMessage = eX.InnerException.Message;
                                            oStackTrace = eX.InnerException.StackTrace;
                                        }
                                        else
                                        {
                                            oMessage = eX.Message;
                                            oStackTrace = eX.StackTrace;
                                        }

                                        var mEx = new Exceptions(logging, oMessage, oStackTrace);
                                        if (mEx != null)
                                            mEx.HandleException(mEx, logging);
                                    }
                                }
                                else
                                    throw new MemberAccessException("Authentication failure. The credentials you have provided are not valid. Please register an account");
                            }
                        }
                    }
                    else
                        throw new ArgumentNullException("iOC Dependency is null. Please contact site administrator for assistance");
                }
                catch (Exception ex)
                {
                    HasError = true;
                    _Failure = true;

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
            }).ContinueWith((e) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Animate = false;

                    if (_Failure)
                    {
                        //Output a dialogue here
                        if (dialogue != null)
                            dialogue.ShowAlert("mmm...Something went wrong", Message);
                    }
                    else
                    {
                        if (this.navigation != null)
                            this.navigation.NavigateToViewModelAsync<HomeViewModel>(true);
                    }
                });
            });
        }

        private ICommand _IRegister;
        public ICommand IRegister
        {
            get { return _IRegister; }
            set { this.RaiseAndSetIfChanged(ref _IRegister, value); }
        }

        public bool CanRegister() { return true; }
        public void Register()
        {

            //Place base code here
            if (this.navigation != null)
                this.navigation.NavigateToViewModelAsync<RegisterViewModel>(true);
        }


        //Social Auth
        //Facebook
        private ICommand _IFacebook;
        public ICommand IFacebook
        {
            get { return _IFacebook; }
            set { this.RaiseAndSetIfChanged(ref _IFacebook, value); }
        }

        public bool CanFacebook() { return true; }
        public void Facebook()
        {
            //Open Facebook Browser and return a token along with user information: 
            //Email, Name, Mobile, etc
            //Emails will be used as a username
        }

        //Twitter
        private ICommand _ITwitter;
        public ICommand ITwitter
        {
            get { return _ITwitter; }
            set { this.RaiseAndSetIfChanged(ref _ITwitter, value); }
        }

        public bool CanTwitter() { return true; }
        public void Twitter()
        {
            //Open Twitter Browser and return a token along with user information: 
            //Email, Name, Mobile, etc
            //Emails will be used as a username
        }

        //LinkedIn
        private ICommand _ILinkedIn;
        public ICommand ILinkedIn
        {
            get { return _ILinkedIn; }
            set { this.RaiseAndSetIfChanged(ref _ILinkedIn, value); }
        }

        public bool CanLinkedIn() { return true; }
        public void LinkedIn()
        {
            //Open LinkedIn Browser and return a token along with user information: 
            //Email, Name, Mobile, etc
            //Emails will be used as a username
        }

        //Google
        private ICommand _IGoogle;
        public ICommand IGoogle
        {
            get { return _IGoogle; }
            set { this.RaiseAndSetIfChanged(ref _IGoogle, value); }
        }

        public bool CanGoogle() { return true; }
        public void Google()
        {
            //Open LinkedIn Browser and return a token along with user information: 
            //Email, Name, Mobile, etc
            //Emails will be used as a username
        }

        #endregion


        //Constants
        private const string Credentials_Service = "_Credentials_Service";

        //Managers
        protected readonly IAccountManager accountManager;
        protected readonly IMusicManager musicManager;
        protected readonly IContactManager contactManager;

        //Services
        protected readonly IContactStore contactStore;
        protected readonly IMusicReader musicStore;

        public LoginViewModel(INavigationService _navigation, IDatabase _database,
            ILogging _logging, IAccountManager _accountManager, IDialogue _dialogue,
            IMusicManager _musicManager, IContactManager _contactManager, IContactStore _contactStore, IMusicReader _musicStore) : base(_navigation, _database, _logging, _dialogue)
        {
            //Managers
            accountManager = _accountManager;
            musicManager = _musicManager;
            contactManager = _contactManager;

            //Dependency Services
            contactStore = _contactStore;
            musicStore = _musicStore;

            //Commands
            ILogin = new RelayExtension(Login, CanLogin);
            IRegister = new RelayExtension(Register, CanRegister);

            //Social Auth
            IFacebook = new RelayExtension(Facebook, CanFacebook);
            ITwitter = new RelayExtension(Twitter, CanTwitter);
            ILinkedIn = new RelayExtension(LinkedIn, CanLinkedIn);
            IGoogle = new RelayExtension(Google, CanGoogle);

            //Request Permissions for services
            CrossPermissions.Current.RequestPermissionsAsync(new Permission[] { Permission.Camera, Permission.Contacts, Permission.Photos });

            //Load Credentials if Remember Me is clicked 
            var credentialsStore = Xamarin.Auth.AccountStore.Create();
            var AccountDetails = credentialsStore.FindAccountsForService(Credentials_Service);

            if (AccountDetails.ToList().Count != 0)
            {
                if (AccountDetails.First().Properties.Any(w => w.Key == "RememberMe"))
                {
                    if (!string.IsNullOrWhiteSpace(AccountDetails.First().Properties.SingleOrDefault(w => w.Key == "RememberMe").Value))
                    {
                        if (AccountDetails.First().Properties.SingleOrDefault(w => w.Key == "RememberMe").Value.Equals("true"))
                        {
                            RememberMe = true;
                            Username = AccountDetails.First().Username;
                            Password = "******************";
                        }
                    }
                }
            }
        }
    }
}
