using System;
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
using System.ServiceModel;

//Plugins
using Plugin.Connectivity;
using Plugin.Messaging;

namespace Cross.DataVault.ViewModels
{
    public class ContactsDetailViewModel : BaseScreen
    {
        protected readonly IContactManager contactsManager;
        protected readonly IAccountManager accountManager;
        protected readonly IContactStore contactStore;

        //Constants
        private const string _ContactsUpdate = "_ContactsUpdate";

        #region Loader
        public bool ReloadData { get; set; }

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

        private string _Display_Name;
        public string Display_Name
        {
            get { return _Display_Name; }
            set { this.RaiseAndSetIfChanged(ref _Display_Name, value); }
        }

        private string _Mobile;
        public string Mobile
        {
            get { return _Mobile; }
            set { this.RaiseAndSetIfChanged(ref _Mobile, value); }
        }

        private string _Email;
        public string Email
        {
            get { return _Email; }
            set { this.RaiseAndSetIfChanged(ref _Email, value); }
        }

        private CoreContactsCellViewModel _Contact;
        public CoreContactsCellViewModel Contact
        {
            get { return _Contact; }
            set { this.RaiseAndSetIfChanged(ref _Contact, value); }
        }

        #endregion


        #region Navigation Bar 

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

        private string _Title;
        public string Title
        {
            get { return _Title; }
            set { this.RaiseAndSetIfChanged(ref _Title, value); }
        }

        #endregion

        #region Business Logic
        //Sendm Emails
        public ICommand _IEmail;
        public ICommand IEmail
        {
            get => _IEmail;
            set => this.RaiseAndSetIfChanged(ref _IEmail, value);
        }

        private void OpenEmail()
        {
            if (CrossMessaging.Current.EmailMessenger.CanSendEmail)
                CrossMessaging.Current.EmailMessenger.SendEmail(Email);
            else
            {
                if (Device.RuntimePlatform == Device.iOS)
                    dialogue.ShowAlert("mmm...Something went wrong", "Seems that there are no accounts set up on this device");
                else
                    dialogue.ShowAlert("mmm...Something went wrong", "Seems that there are no applications that can send emails on this device");
            }
        }

        private bool CanOpenEmail() => true;

        //Call Number
        public ICommand _IMobile;
        public ICommand IMobile
        {
            get => _IMobile;
            set => this.RaiseAndSetIfChanged(ref _IMobile, value);
        }

        private void OpenMobile()
        {
            if (CrossMessaging.Current.PhoneDialer.CanMakePhoneCall)
                CrossMessaging.Current.PhoneDialer.MakePhoneCall(Mobile, Display_Name);
            else
                dialogue.ShowAlert("mmm...Something went wrong", "Seems that I cannot call this number. Do you have a running dialer on this device?");
        }

        private bool CanOpenMobile() => true;
        #endregion



        //Initializes loads all contacts from the device and adds them to the collection
        //Allows the ability to add contacts
        public ContactsDetailViewModel(INavigationService _navigation, IDatabase _database, ILogging _logger, IDialogue _dialogue,
            IContactManager _contactManager, IAccountManager _accountManager, IContactStore _contactStore) : base(_navigation, _database, _logger, _dialogue)
        {
            contactsManager = _contactManager;
            accountManager = _accountManager;
            contactStore = _contactStore;

            //Drawer Details
            Title = "Contact Details";

            //Relays
            IGoBack = new Relays.RelayExtension(GoBack, CanGoBack);
            IMobile = new Relays.RelayExtension(OpenMobile, CanOpenMobile);
            IEmail = new Relays.RelayExtension(OpenEmail, CanOpenEmail);


            var oContact = database.Get<Contact>($"SELECT * FROM Contact WHERE Contact_ID ='{Constants.Contact_ID}'", new object[] { }).SingleOrDefault();
            if (oContact != null)
            {
                Contact = new CoreContactsCellViewModel(oContact, dialogue, navigation);

                //Data Binding
                Mobile = Contact.Contact_Number;
                Email = Contact.Email;
                Display_Name = Contact.Display_Name;
            }
        }
    }
}
