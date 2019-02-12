using System;
using System.Windows.Input;

using Caliburn.Micro;
using Caliburn.Micro.Xamarin.Forms;

//Data
using Cross.DataVault.Data;

//Services
using Cross.DataVault.Services.DependencyServices;

//Plugins
using Plugin.Connectivity;
using Plugin.Messaging;


//Relays
using Cross.DataVault.Relays;
using ReactiveUI;

namespace Cross.DataVault.ViewModels.Cell.Secure
{
    public class NotesCellViewModel : ReactiveObject
    {
        //Events & Delegates
        public event DeleteContent _DeleteContent;
        public delegate void DeleteContent(object sender);

        //Services
        private readonly INavigationService navigation;

        private string _Subject;
        public string Subject
        {
            get { return _Subject; }
            set { this.RaiseAndSetIfChanged(ref _Subject, value); }
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
        public ICommand IOpenNote
        {
            get { return new RelayExtension(OpenNote, CanOpenNote); }
        }

        public bool CanOpenNote() { return true; }
        public void OpenNote()
        {
            Constants.Note_ID = ID;

            if (navigation != null)
                navigation.NavigateToViewModelAsync<NotesCreatorViewModel>(true);
        }

        public ICommand IDelete
        {
            get { return new RelayExtension(DeleteNote, CanDeleteNote); }
        }

        //Delete the selected note
        public bool CanDeleteNote() { return true; }
        public void DeleteNote()
        {
            //Delete the note
            if (_DeleteContent != null)
                _DeleteContent.Invoke(ID);
        }

        public ICommand IShare
        {
            get { return new RelayExtension(Share, CanShare); }
        }

        //Share the Selected note
        public bool CanShare() { return true; }
        public void Share()
        {
            //Open up share activity view
            if (CrossMessaging.Current.EmailMessenger.CanSendEmail)
            {
                //Send link to application
                CrossMessaging.Current.EmailMessenger.SendEmail("", Subject, Description);
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

        protected readonly IDialogue dialogue;

        //Initialization
        public NotesCellViewModel() { }
        public NotesCellViewModel(Notes obj, INavigationService _navigation, IDialogue _dialogue)
        {
            this.Date = obj.Sys_Creation.ToString("m");
            this.Time = obj.Sys_Creation.ToString("hh:mm tt");
            this.Description = obj.Description;
            this.Subject = obj.Subject;

            this.ID = obj.Content_ID_Ref;
            this.UserID = obj.Contact_ID_Ref;

            navigation = _navigation;
            dialogue = _dialogue;
        }
    }
}
