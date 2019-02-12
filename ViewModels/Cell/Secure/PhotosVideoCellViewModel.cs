using System;
using System.Windows.Input;

using Caliburn.Micro.Xamarin.Forms;
using ReactiveUI;

//Services
using Cross.DataVault.Services.DependencyServices;

//Data
using Cross.DataVault.Data;

//Plugins
using Plugin.Messaging;
using Plugin.Media;

namespace Cross.DataVault.ViewModels.Cell.Secure
{
    public class PhotosVideoCellViewModel : ReactiveObject
    {
        //Events & Delegates
        public event DeleteContent _DeleteContent;
        public delegate void DeleteContent(object sender);

        public byte[] Photo { get; set; }

        public string Author_DisplayName { get; set; }
        public string Author_ID { get; set; }

        public string Date { get; set; }
        public string Time { get; set; }

        public string ID { get; set; }

        #region Commands 
        public ICommand IOpenMedia { get { return new Relays.RelayExtension(OpenMedia, CanOpenMedia); } }

        public bool CanOpenMedia() { return true; }
        public void OpenMedia()
        {
            Constants.Photos_ID = ID;
            //Open the Photos viewer and display the photo being taken
            if (navigation != null)
                navigation.NavigateToViewModelAsync<PhotoViewerViewModel>(true);
        }

        public ICommand IDelete
        {
            get { return new Relays.RelayExtension(DeleteNote, CanDeleteNote); }
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
            get { return new Relays.RelayExtension(Share, CanShare); }
        }

        //Share the Selected note
        public bool CanShare() { return true; }
        public void Share()
        {
            //Open up share activity view
            if (CrossMessaging.Current.EmailMessenger.CanSendEmail)
            {
                //Send photo to application
                CrossMessaging.Current.EmailMessenger.SendEmail("", "Attachment", "");
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

        //Dependencies
        private readonly INavigationService navigation;
        private readonly IDialogue dialogue;

        //Initialization
        public PhotosVideoCellViewModel(INavigationService _navigation, IDialogue _dialogue) {
            navigation = _navigation;
            dialogue = _dialogue;
        }
        public PhotosVideoCellViewModel(PhotoVideo obj, INavigationService _navigation, IDialogue _dialogue)
        {
            navigation = _navigation;
            dialogue = _dialogue;

            this.Author_DisplayName = obj.Author_DisplayName;
            if (string.IsNullOrWhiteSpace(this.Author_DisplayName))
                this.Author_DisplayName = $"{obj.Author_FirstName} {obj.Author_LastName}";

            if (obj.Sys_Creation != null)
            {
                this.Date = $"{obj.Sys_Creation.ToString("m")}, {obj.Sys_Creation.ToString("hh:mm tt")}"; 
                this.Time = obj.Sys_Creation.ToString("hh:mm tt");
            }

            this.Photo = obj.Photo;
            this.ID = obj.Content_ID;
            this.Author_ID = obj.Author_ID;
        }
    }
}
