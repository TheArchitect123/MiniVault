using System;
using System.Windows.Input;

using Cross.DataVault.Data;
using Caliburn.Micro;
using Caliburn.Micro.Xamarin.Forms;
using ReactiveUI;

//Services
using Cross.DataVault.Services.DependencyServices;

//Plugins
using Plugin.Messaging;

namespace Cross.DataVault.ViewModels.Cell.Secure
{
    public class PasswordCellViewModel : ReactiveObject
    {
        //Events & Delegates
        public event DeleteContent _DeleteContent;
        public delegate void DeleteContent(object sender);

        private string _Password;
        public string Password
        {
            get { return _Password; }
            set { this.RaiseAndSetIfChanged(ref _Password, value); }
        }

        private string _Description;
        public string Description
        {
            get { return _Description; }
            set { this.RaiseAndSetIfChanged(ref _Description, value); }
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

        public string ID { get; set; }
        public string UserID { get; set; }

        #region Commands
        public ICommand IOpenPassword => new Relays.RelayExtension(OpenPassword, CanOpenPassword);

        public bool CanOpenPassword() { return true; }
        public void OpenPassword()
        {
            //Opens and animates the Password View, where the user can edit the password or remove it
            Constants.Passwords_ID = ID;

            if (navigation != null)
                navigation.NavigateToViewModelAsync<PasswordCreatorViewModel>(true);
        }

        public ICommand IDelete => new Relays.RelayExtension(DeletePassword, CanDeletePassword);

        //Delete the selected note
        public bool CanDeletePassword() { return true; }
        public void DeletePassword()
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
                //Send link to application
                CrossMessaging.Current.EmailMessenger.SendEmail("", "My Password - Not a good idea!", Password);
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

        #endregion

        protected INavigationService navigation;
        protected IDialogue dialogue;

        public PasswordCellViewModel(Passwords obj, INavigationService _navigation, IDialogue _dialogue)
        {
            navigation = _navigation;
            dialogue = _dialogue;

            this.Description = obj.Description;
            this.Password = obj.Password;
            this.Time = obj.Sys_Creation.ToString("hh:MM tt");
            this.Date = obj.Sys_Creation.ToString("m");

            this.ID = obj.Password_ID;
            this.UserID = obj.Contact_ID_Ref;
        }
    }
}
