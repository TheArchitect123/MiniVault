using System;
using ReactiveUI;
using System.Windows.Input;

//Services
using Caliburn.Micro.Xamarin.Forms;
using Cross.DataVault.Services.DependencyServices;

//Data
using Cross.DataVault.Data;

//Plugins
using Plugin.Messaging;
using Plugin.Connectivity;

namespace Cross.DataVault.ViewModels.Cell.Secure
{
    public class CoreContactsCellViewModel : ReactiveObject
    {
        //Events & Delegates
        public event DeleteContent _DeleteContent;
        public delegate void DeleteContent(object sender);

        //Events & Delegates
        public event DisplayContact _DisplayContact;
        public delegate void DisplayContact(object sender);

        #region Data

        private string _First_Name;
        public string First_Name
        {
            get { return _First_Name; }
            set { this.RaiseAndSetIfChanged(ref _First_Name, value); }
        }

        private string _Last_Name;
        public string Last_Name
        {
            get { return _Last_Name; }
            set { this.RaiseAndSetIfChanged(ref _Last_Name, value); }
        }

        private string _Display_Name;
        public string Display_Name
        {
            get { return _Display_Name; }
            set { this.RaiseAndSetIfChanged(ref _Display_Name, value); }
        }

        //Mobile, Home, or Work
        private string _Contact_Number;
        public string Contact_Number
        {
            get { return _Contact_Number; }
            set { this.RaiseAndSetIfChanged(ref _Contact_Number, value); }
        }

        private string _Email;
        public string Email
        {
            get { return _Email; }
            set { this.RaiseAndSetIfChanged(ref _Email, value); }
        }

        private string _Date;
        public string Date
        {
            get { return _Date; }
            set { this.RaiseAndSetIfChanged(ref _Date, value); }
        }

        private string _Time;
        public string Time
        {
            get { return _Time; }
            set { this.RaiseAndSetIfChanged(ref _Time, value); }
        }

        private string _ID;
        public string ID
        {
            get { return _ID; }
            set { this.RaiseAndSetIfChanged(ref _ID, value); }
        }

        #endregion

        #region Commands
        public ICommand IContactPress => new Relays.RelayExtension(ContactPress, CanContactPress);

        public bool CanContactPress() { return true; }
        public void ContactPress()
        {
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

        public ICommand IEmailPress => new Relays.RelayExtension(EmailPress, CanEmailPress);

        public bool CanEmailPress() { return true; }
        public void EmailPress()
        {
            if (!string.IsNullOrWhiteSpace(Email))
            {
                if (CrossMessaging.Current.EmailMessenger.CanSendEmail)
                    CrossMessaging.Current.EmailMessenger.SendEmail(Email, string.Empty, string.Empty);
            }
        }

        public ICommand IDelete => new Relays.RelayExtension(DeleteContact, CanDeleteContact);

        //Delete the selected note
        public bool CanDeleteContact() { return true; }
        public void DeleteContact()
        {
            //Delete the note
            if (_DeleteContent != null)
                _DeleteContent.Invoke(ID);
        }

        public ICommand IShare => new Relays.RelayExtension(Share, CanShare);

        //Share the Selected note
        public bool CanShare() { return true; }
        public void Share()
        {
            //Open up share activity view
            if (CrossMessaging.Current.EmailMessenger.CanSendEmail)
            {
                string Message = $"\n------------------------------------- \n Contact Details:\n Full Name: {Display_Name} \nEmail: {Email} \n Contact Number: {Contact_Number} \n Date:" +
                                 $" {DateTime.Now.ToString("m")}, {DateTime.Now.ToString("hh:mm tt")}";

                CrossMessaging.Current.EmailMessenger.SendEmail("", "My Contact", Message); //Share the Contact
            }
            else
            {
                if (dialogue != null)
                    dialogue.ShowAlert_WithAction("mmm...Something went wrong", "Seems that you have no email accounts set up on this device. Would you like to set one up now?", () =>
                    {
                        //Open settings up so the user can set up their email accounts
                        Plugin.Permissions.CrossPermissions.Current.OpenAppSettings();
                    });
            }
        }

        public ICommand IViewContact => new Relays.RelayExtension(ViewContact, CanViewContact);

        //Share the Selected note
        public bool CanViewContact() { return true; }
        public void ViewContact()
        {
            if (_DisplayContact != null)
                _DisplayContact.Invoke(ID);
        }


        #endregion

        protected readonly IDialogue dialogue;
        protected readonly INavigationService navigation;

        public CoreContactsCellViewModel() { }
        public CoreContactsCellViewModel(Contact obj, IDialogue _dialogue, INavigationService _navigation)
        {
            dialogue = _dialogue;
            navigation = _navigation;

            this.First_Name = obj.First_Name;
            this.Last_Name = obj.Last_Name;
            this.Display_Name = $"{obj.SiteUser_DisplayName}";

            if (!string.IsNullOrWhiteSpace(obj.Work))
                this.Contact_Number = "Work: " + obj.Work;
            if (!string.IsNullOrWhiteSpace(obj.Home))
                this.Contact_Number = "Home: " + obj.Home;
            if (!string.IsNullOrWhiteSpace(obj.Mobile))
                this.Contact_Number = "Mobile: " + obj.Mobile;

            if (string.IsNullOrWhiteSpace(this.Contact_Number))
                this.Contact_Number = "Mobile: ";

            this.ID = obj.Contact_ID;

            this.Email = "Email: " + obj.Email;
            this.Date = obj.Sys_Creation.ToString("m");
            this.Time = obj.Sys_Creation.ToString("hh:mm tt");
        }
    }
}
