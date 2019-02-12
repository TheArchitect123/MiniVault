using System;
using System.Timers;
using System.Linq;
using System.Windows.Input;

using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;

using ReactiveUI;

using Caliburn.Micro;
using Caliburn.Micro.Xamarin.Forms;
using Xamarin.Forms;

//Helpers
using Cross.DataVault.Infrastructure.Utilities;
using Cross.DataVault.Data.Mapper;

//Data 
using Cross.DataVault.Data;

//View Models
using Cross.DataVault.ViewModels.Cell.Secure;
using Cross.DataVault.ViewModels.Cell;

//Services 
using Cross.DataVault.Data.Services;
using Cross.DataVault.Services;
using Cross.DataVault.Services.Managers; //Manager
using Cross.DataVault.Services.DependencyServices;
using Cross.DataVault.ServiceAccess; //Cloud Service APIs
using Cross.DataVault.ServiceAccess.Configuration;

using System.ServiceModel;

//Plugins
using Plugin.Connectivity;
using Plugin.Messaging;

namespace Cross.DataVault.ViewModels
{
    public class ContactsViewModel : BaseScreen
    {
        #region Loader
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

        #region Data

        private ObservableCollection<CoreContactsCellViewModel> _Contacts;
        public ObservableCollection<CoreContactsCellViewModel> Contacts
        {
            get { return _Contacts == null ? _Contacts = new ObservableCollection<CoreContactsCellViewModel>() : _Contacts; }
        }

        #endregion


        #region Navigation Bar 

        private string _Title;
        public string Title
        {
            get { return _Title; }
            set { this.RaiseAndSetIfChanged(ref _Title, value); }
        }

        #endregion

        #region Commands

        public void ContactPress(object number, object displayName)
        {
            var Contact_Number = number as string;
            var Display_Name = displayName as string;

            if (!string.IsNullOrWhiteSpace(Contact_Number))
            {
                if (CrossMessaging.Current.PhoneDialer.CanMakePhoneCall)
                {
                    if (!string.IsNullOrWhiteSpace(Display_Name))
                        CrossMessaging.Current.PhoneDialer.MakePhoneCall(Contact_Number, Display_Name);
                    else
                        CrossMessaging.Current.PhoneDialer.MakePhoneCall(Contact_Number, "Unknown");
                }
            }
        }

        public void EmailPress(object email)
        {
            var Email = email as string;

            if (!string.IsNullOrWhiteSpace(Email))
            {
                if (CrossMessaging.Current.EmailMessenger.CanSendEmail)
                    CrossMessaging.Current.EmailMessenger.SendEmail(Email, string.Empty, string.Empty);
            }
        }

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
            Instructions = "Downloading Contacts";
            Animate = true;
            Refreshing = true;

            //Diagnostics
            string Message = string.Empty; ;
            string StackTrace = string.Empty;
            bool _AnyError = false;

            Task.Run(() =>
            {
                //Query the user's data from the back end SSMS
                try
                {
                    var contactsCollection = new ObservableCollection<CoreContactsCellViewModel>();

                    var curr = new List<Contact>();
                    var obj = new List<CoreContactsCellViewModel>();

                    this.Contacts.Clear();
                    contactsManager.ClearContacts_ForUserID(Constants.InMemory_ContactID);

                    DataVaultWebServiceClient dataService = new DataVaultWebServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.Data_InSecureUrl));
                    var server_contacts = dataService._GetContacts_ByUserID(Constants.InMemory_ContactID);
                    var contacts = contactStore.Get_ContactsFromStore<Contact>();
                    if (contacts != null)
                    {
                        contacts.ForEach(w =>
                        {
                            //Add Contact to the contact store for the particular account
                            w.Sys_Creation = DateTime.Now;
                            w.Sys_Transaction = DateTime.Now;

                            w.Contact_ID = contactsManager.Get_NewContactID();
                            w.User_ID = Constants.InMemory_ContactID;

                            //Add to Local Collection
                            var cObj = new CoreContactsCellViewModel(w, dialogue, navigation);
                            cObj._DeleteContent += RemoveContact_FromCollection;

                            if (!curr.Contains(w))
                                curr.Add(w);

                            if (!obj.Contains(cObj))
                                obj.Add(cObj);
                        });
                    }

                    if (server_contacts._Contacts != null)
                    {
                        server_contacts._Contacts.ForEach(w =>
                        {
                            //Add Contacts to Table
                            var csObj = new CoreContactsCellViewModel(LocalMapper.MapContact_FromServer(w), dialogue, navigation);
                            csObj._DeleteContent += RemoveContact_FromCollection;

                            if (!curr.Contains(LocalMapper.MapContact_FromServer(w)))
                                curr.Add(LocalMapper.MapContact_FromServer(w));

                            if (!obj.Contains(csObj))
                                obj.Add(csObj);
                        });
                    }

                    if (contactsManager != null && curr.Count != 0)
                        contactsManager.AddContacts_ByDetails(curr);

                    Initialize_Core();
                    //      MessagingCenter.Send<ContactsViewModel>(this, _ContactsUpdate);
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
                    Animate = false; Refreshing = false;

                    if (dialogue != null && _AnyError)
                        dialogue.ShowAlert("mmm...Something went wrong", Message);
                });
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

        #endregion

        #region Business Logic

        private async void RemoveContact_FromCollection(object sender)
        {
            if (sender != null)
            {
                var id = sender as string;

                //Add Notes to Server
                Animate = true;
                Instructions = "Deleting Contact";
                bool HasError = false;

                string Message = string.Empty;
                string StackTrace = string.Empty;

                //Remove note
                await Task.Run(() =>
                {
                    try
                    {
                        //var response = dataService._DeleteContact_ByID(id.Value);
                        //if (response.Errors.Count != 0)
                        //{
                        //    response.Errors.ForEach(w =>
                        //    {
                        //        //Add to log table for diagnostics

                        //        if (this.logging != null)
                        //        {
                        //            var log = LocalMapper.Map_LogWithMessage(w, (this.logging.GetLogID_ByMax() + 1), (this.logging.GetProcessID_ByMax() + 1));
                        //            this.logging.AddLog(log);
                        //        }
                        //    });

                        //    if (_dialogue != null)
                        //        _dialogue.ShowAlert("mmm...Something went wrong", response.Errors[0]);
                        //}
                        //else
                        //{
                        //    if (this.Contacts == null)
                        //        throw new ArgumentNullException("Contacts Collection cannot be null");

                        //    this.Contacts.RemoveAt(this.Contacts.IndexOf(this.Contacts.SingleOrDefault(w => w.ID == id)));
                        //    contactsManager.RemoveContact_ByID(id.Value);
                        //}

                        if (this.Contacts == null)
                            throw new ArgumentNullException("Contacts Collection cannot be null");

                        this.Contacts.RemoveAt(this.Contacts.IndexOf(this.Contacts.SingleOrDefault(w => w.ID == id)));
                        contactsManager.RemoveContact_ByID(id);
                    }
                    catch (Exception ex)
                    {
                        HasError = true;

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
                        if (dialogue != null && HasError)
                            dialogue.ShowAlert("mmm...Something went wrong", Message);
                    });
                });
            }
        }

        private async void DisplayContact_FromCollection(object sender)
        {
            if (sender != null)
            {
                var id = sender as string;

                //Add Notes to Server
                Animate = true;
                Instructions = "Loading Contact";

                //Load And Assign data to 
                Constants.Contact_ID = id;

                //Remove note
                Timer displayTimer = new Timer(2);
                displayTimer.AutoReset = false;
                displayTimer.Start();

                displayTimer.Elapsed += (e, s) =>
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        Animate = false;
                        if (navigation != null)
                            navigation.NavigateToViewModelAsync<ContactsDetailViewModel>(true);
                    });
                };
            }
        }
        private void Initialize_Core()
        {
            //Generate collection of Notes
            Animate = true;
            Instructions = "Loading Contacts";

            Task.Run(() =>
            {
                var contacts = contactsManager.Get_Contacts_ByUserID<Contact>(Constants.InMemory_ContactID);
                if (contacts.Count != 0)
                {
                    foreach (var contact in contacts)
                    {
                        contact.Contact_ID = contactsManager.Get_NewContactID();
                        CoreContactsCellViewModel obj = new CoreContactsCellViewModel(contact, dialogue, navigation);

                        obj._DeleteContent += RemoveContact_FromCollection;
                        obj._DisplayContact += DisplayContact_FromCollection;

                        //Subscriptions
                        this.Contacts.Add(obj);
                    }
                }

                this.Contacts.CollectionChanged += Contacts_CollectionChanged;

            }).ContinueWith((e) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Animate = false;
                    ReloadData = true;
                });
            });
        }

        private void Contacts_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() => { ReloadData = true; });
        }

        private void AddContact(Contact obj)
        {
            if (this.Contacts != null)
            {
                obj.Contact_ID = contactsManager.Get_NewContactID();
                this.Contacts.Add(new CoreContactsCellViewModel(obj, dialogue, navigation));
            }
        }

        #endregion

        protected readonly IContactManager contactsManager;
        protected readonly IAccountManager accountManager;
        protected readonly IContactStore contactStore;

        //Constants
        private const string _ContactsUpdate = "_ContactsUpdate";


        //Initializes loads all contacts from the device and adds them to the collection
        //Allows the ability to add contacts
        public ContactsViewModel(INavigationService _navigation, IDatabase _database, ILogging _logger, IDialogue _dialogue,
            IContactManager _contactManager, IAccountManager _accountManager, IContactStore _contactStore) : base(_navigation, _database, _logger, _dialogue)
        {
            contactsManager = _contactManager;
            accountManager = _accountManager;
            contactStore = _contactStore;

            //Drawer Details
            Title = "My Contacts";

            //Relays
            IGoBack = new Relays.RelayExtension(GoBack, CanGoBack);

            //Relays - Refresh Data
            IOnRefresh = new Relays.RelayExtension(OnRefresh, CanOnRefresh);

            //Initialize Data & Navigation
            Initialize_Core();
        }
    }
}
