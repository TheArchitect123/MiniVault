using System;
using System.Threading.Tasks;
using System.Windows.Input;

using System.ServiceModel;

using Caliburn.Micro;
using Caliburn.Micro.Xamarin.Forms;
using Xamarin.Forms;

//Extensions
using Cross.DataVault.Relays;

//Helpers
using Cross.DataVault.Infrastructure.Utilities;
using Cross.DataVault.Data.Mapper;

//Helpers
using Cross.DataVault.Infrastructure.Utilities;
using Cross.DataVault.Data.Mapper;

//Services
using Cross.DataVault.Data.Services;
using Cross.DataVault.Services;
using Cross.DataVault.Services.Managers;
using Cross.DataVault.Services.DependencyServices;
using Cross.DataVault.ServiceAccess; //Cloud Service APIs

//Core Data
using Cross.DataVault.Data;
using ReactiveUI;

namespace Cross.DataVault.ViewModels
{
    public class NotesCreatorViewModel : BaseScreen
    {
        public NotesCreatorViewModel(INavigationService _navigation,
            IDatabase _database, ILogging _logging,
            INotesManager notesManager,
            IDialogue _dialogue,
            ILoader loader
            ) : base(_navigation, _database, _logging, _dialogue)
        {
            //Managers
            _notesManager = notesManager;

            //Services
            _loader = loader;

            IConfirm = new RelayExtension(Confirm, CanConfirm);
            IGoBack = new RelayExtension(GoBack, CanGoBack);

            Title = "Add a Note";

            //Bind the Editable data to the views
            if (!string.IsNullOrWhiteSpace(Constants.Note_ID))
            {
                var note = _notesManager.Get_NoteByID<Notes>(Constants.Note_ID);
                if (note != null)
                {
                    Subject = note.Subject;
                    Note = note.Description;
                }
            }
        }

        #region Services
        //Contants
        private const string _SendNote = "_SendNote";
        private const string _UpdateNote = "_UpdateNote";

        //Managers
        protected readonly INotesManager _notesManager;

        //Services
        protected readonly ILoader _loader;

        #endregion

        #region Data

        private string _Subject;
        public string Subject
        {
            get
            {
                return _Subject;
            }
            set { this.RaiseAndSetIfChanged(ref _Subject, value); }
        }

        private string _Note;
        public string Note
        {
            get
            {
                return _Note;
            }
            set { this.RaiseAndSetIfChanged(ref _Note, value); }
        }

        private string _Title;
        public string Title
        {
            get { return _Title; }
            set { this.RaiseAndSetIfChanged(ref _Title, value); }
        }

        #endregion

        #region Commands

        private ICommand _IConfirm;
        public ICommand IConfirm
        {
            get { return _IConfirm; }
            set { this.RaiseAndSetIfChanged(ref _IConfirm, value); }
        }

        public bool CanConfirm() { return true; }
        public void Confirm()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Note))
                    throw new ArgumentNullException("Note cannot be empty");

                if (!string.IsNullOrWhiteSpace(Constants.Note_ID))
                {
                    var note = _notesManager.Get_NoteByID<Notes>(Constants.Note_ID);
                    if (note != null)
                    {
                        note.Subject = Subject;
                        note.Description = Note;

                        _notesManager.UpdateNote(note);
                        MessagingCenter.Send<NotesCreatorViewModel, Notes>(this, _UpdateNote, note);
                    }
                    else
                    {
                        var obj = new Notes();
                        obj.Contact_ID_Ref = Constants.InMemory_ContactID;
                        obj.Content_ID_Ref = Guid.NewGuid().ToString();
                        obj.Description = Note;
                        obj.Subject = Subject;
                        obj.Sys_Creation = DateTime.Now;
                        obj.Sys_Transaction = DateTime.Now;

                        _notesManager.AddNote(obj);
                        MessagingCenter.Send<NotesCreatorViewModel, Notes>(this, _SendNote, obj);
                    }
                }
                else
                {
                    var obj = new Notes();
                    obj.Contact_ID_Ref = Constants.InMemory_ContactID;
                    obj.Content_ID_Ref = Guid.NewGuid().ToString();
                    obj.Description = Note;
                    obj.Subject = Subject;
                    obj.Sys_Creation = DateTime.Now;
                    obj.Sys_Transaction = DateTime.Now;

                    _notesManager.AddNote(obj);
                    MessagingCenter.Send<NotesCreatorViewModel, Notes>(this, _SendNote, obj);
                }

                //Pop to previous page
                if (navigation != null)
                    navigation.GoBackAsync(true);
            }
            catch (Exception ex)
            {
                string eMessage = string.Empty;
                string eStackTrace = string.Empty;

                if (ex.InnerException != null)
                {
                    eMessage = ex.InnerException.Message;
                    eStackTrace = ex.InnerException.StackTrace;
                }
                else
                {
                    eMessage = ex.Message;
                    eStackTrace = ex.StackTrace;
                }

                var mEx = new Exceptions(logging, eMessage, eStackTrace);
                if (mEx != null)
                    mEx.HandleException(mEx, logging);

                //Output a dialogue here
                if (dialogue != null)
                    dialogue.ShowAlert("mmm...Something went wrong", mEx.Message);
            }
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
    }
}
