using System;
using System.Threading.Tasks;
using System.Linq;
using System.ServiceModel;

using System.Collections.ObjectModel;
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

//Data
using Cross.DataVault.Data;
using Cross.DataVault.Data.Mapper;

//Services
using Cross.DataVault.Data.Services;
using Cross.DataVault.Services;
using Cross.DataVault.Services.Managers;
using Cross.DataVault.Services.DependencyServices;
using Cross.DataVault.ServiceAccess; //Cloud Service APIs
using Cross.DataVault.ServiceAccess.Configuration;

using ReactiveUI;
using ReactiveUI.XamForms;

//Plugins 
using Plugin.Connectivity;

namespace Cross.DataVault.ViewModels
{
    public class NotesViewModel : BaseScreen
    {
        public NotesViewModel(INavigationService _navigation, IDatabase _database, ILogging _logging, IDialogue _dialogue,
            INotesManager notesManager, IAccountManager _accountManager, IEventAggregator eventAggregator) : base(_navigation, _database, _logging, _dialogue)
        {
            _notesManager = notesManager;
            _eventAggregator = eventAggregator;

            Title = "My Notes";

            //Relays
            IGoBack = new Relays.RelayExtension(GoBack, CanGoBack);
            IOpenFloat = new Relays.RelayExtension(OpenFloat, CanOpenFloat);
            IRefreshData = new Relays.RelayExtension(OnRefresh, CanOnRefresh);

            //Subscriptions
            MessagingCenter.Subscribe<NotesCreatorViewModel, Notes>(this, _SendNote, (receiver, data) => { AddNotes_ToCollection(data); });
            MessagingCenter.Subscribe<NotesCreatorViewModel, Notes>(this, _UpdateNote, (receiver, data) => { UpdateNote_ToCollection(data); });

            //Perform Initialization here
            Initialize_Core();
        }

        private void Notes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() => { ReloadData = true; });
        }

        #region Constants
        //Contants
        private const string _SendNote = "_SendNote";
        private const string _UpdateNote = "_UpdateNote";
        #endregion

        #region Dependencies
        protected readonly INotesManager _notesManager;
        protected IEventAggregator _eventAggregator;

        #endregion

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
            set
            {
                this.RaiseAndSetIfChanged(ref _Refreshing, value);
            }
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

        private ObservableCollection<NotesCellViewModel> _Notes;
        public ObservableCollection<NotesCellViewModel> Notes
        {
            get { return _Notes == null ? _Notes = new ObservableCollection<NotesCellViewModel>() : _Notes; }
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
        private ICommand _IRefreshData;
        public ICommand IRefreshData
        {
            get { return _IRefreshData; }
            set { this.RaiseAndSetIfChanged(ref _IRefreshData, value); }
        }

        public bool CanOnRefresh() { return true; }
        public void OnRefresh()
        {
            //Start querying the 
            Instructions = "Downloading Notes";
            Animate = true;
            Refreshing = true;

            //Diagnostics
            string Message = string.Empty;
            string StackTrace = string.Empty;
            bool _AnyError = false;

            Task.Run(() =>
            {
                //Query the user's data from the back end SSMS
                try
                {
                    if (_notesManager != null)
                    {
                        var obj = new List<Notes>();

                        DataVaultWebServiceClient dataService = new DataVaultWebServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.Data_InSecureUrl));
                        var notes = dataService._GetNotes_ByUserID(Constants.InMemory_ContactID);
                        if (notes._Notes.Count != 0)
                        {
                            if (_notesManager != null)
                            {
                                this.Notes.Clear();
                                _notesManager.Delete_AllNotesByContactID(Constants.InMemory_ContactID); //Clear all notes then download them

                                notes._Notes.ForEach(w => obj.Add(LocalMapper.MapNote_FromServer(w)));
                                _notesManager.AddNotes(obj);
                                OnRefresh_Core();
                            }
                        }
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
                Device.BeginInvokeOnMainThread(() =>
                {
                    Animate = false;
                    Refreshing = false;

                    //if (dialogue != null && _AnyError)
                    //    dialogue.ShowAlert("mmm...Something went wrong", Message);
                });
            });
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
            ReloadData = false;
            Constants.Note_ID = null;

            if (navigation != null)
                navigation.NavigateToViewModelAsync<NotesCreatorViewModel>(true);
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
        private async void UpdateNote_ToCollection(Notes obj)
        {
            //Add Notes to Server
            Animate = true;
            Instructions = "Updating Note";

            //Diagnostics
            string Message = string.Empty;
            string StackTrace = string.Empty;
            bool _HasError = false;

            await Task.Run(() =>
            {
                try
                {
                    var Index = this.Notes.IndexOf(this.Notes.SingleOrDefault(i => i.ID == obj.Content_ID_Ref));
                    this.Notes[Index].Subject = obj.Subject;
                    this.Notes[Index].Description = obj.Description;

                    DataVaultWebServiceClient dataService = new DataVaultWebServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.Data_InSecureUrl));
                    var response = dataService._UpdateNote_ByID(LocalMapper.MapNote_ToServer(obj));
                    if (response.Errors.Count != 0)
                    {
                        response.Errors.ForEach(w =>
                        {
                            //Add to log table for diagnostics
                            if (this.logging != null)
                            {
                                var log = LocalMapper.Map_LogWithMessage(w, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
                                this.logging.AddLog(log);
                            }
                        });

                        _HasError = true;
                    }
                }
                catch (Exception ex)
                {
                    _HasError = true;

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
                    //if (dialogue != null & _HasError)
                    //    dialogue.ShowAlert("mmm...Something went wrong", Message);
                });
            });
        }

        private async void AddNotes_ToCollection(Notes obj)
        {
            //Add Notes to Server
            Animate = true;
            Instructions = "Adding Note";

            //Diagnostics
            string Message = string.Empty;
            string StackTrace = string.Empty;
            bool _HasError = false;

            string cid = obj.Content_ID_Ref; //Temp Client Id

            await Task.Run(() =>
            {
                try
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        var curr = new NotesCellViewModel(obj, navigation, dialogue);
                        curr._DeleteContent += RemoveNote_FromCollection;
                        this.Notes.Add(curr);
                    });

                    DataVaultWebServiceClient dataService = new DataVaultWebServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.Data_InSecureUrl));
                    var response = dataService._AddNote(LocalMapper.MapNote_ToServer(obj));
                    if (response.Errors.Count != 0)
                    {
                        response.Errors.ForEach(w =>
                        {
                            //Add to log table for diagnostics
                            if (this.logging != null)
                            {
                                var log = LocalMapper.Map_LogWithMessage(w, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
                                this.logging.AddLog(log);
                            }
                        });

                        _HasError = true;
                    }
                    else
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            var note = this.Notes.SingleOrDefault(w => w.ID.Equals(cid));
                            note.ID = response.Content_ID;
                            ReloadData = true;
                        });

                        //Update local ID with the Server ID
                        obj.Content_ID_Ref = response.Content_ID;
                        _notesManager.UpdateNote(obj);
                    }
                }
                catch (Exception ex)
                {
                    _HasError = true;

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
                    //if ()
                    //    dialogue.ShowAlert("mmm...Something went wrong", Message);
                });
            });
        }

        private async void RemoveNote_FromCollection(object sender)
        {
            if (sender != null)
            {
                var id = sender as string;

                //Add Notes to Server
                Animate = true;
                Instructions = "Deleting Note";

                //Diagnostics
                string Message = string.Empty;
                string StackTrace = string.Empty;
                bool _HasError = false;

                //Remove note
                await Task.Run(() =>
                {
                    try
                    {
                        this.Notes.RemoveAt(this.Notes.IndexOf(this.Notes.SingleOrDefault(w => w.ID == id)));
                        _notesManager.Delete_NoteById(id);

                        DataVaultWebServiceClient dataService = new DataVaultWebServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.Data_InSecureUrl));

                        var response = dataService._DeleteNote_ByID(id, Constants.InMemory_ContactID);
                        if (response.Errors.Count != 0)
                        {
                            response.Errors.ForEach(w =>
                            {
                                //Add to log table for diagnostics

                                if (this.logging != null)
                                {
                                    var log = LocalMapper.Map_LogWithMessage(w, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
                                    this.logging.AddLog(log);
                                }
                            });

                            _HasError = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _HasError = true;

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
                        //if (_HasError)
                        //    dialogue.ShowAlert("mmm...Something went wrong", Message);
                    });
                });
            }
        }


        private void Initialize_Core()
        {
            //Generate collection of Notes
            Animate = true;
            Instructions = "Loading your notes";

            Task.Run(() =>
            {
                this.Notes.Clear();

                var notes = _notesManager.Get_NotesByContactID<Notes>(Constants.InMemory_ContactID);
                if (notes.Count != 0)
                {
                    foreach (var note in notes)
                    {
                        var obj = new NotesCellViewModel(note, navigation, dialogue);

                        //Subscriptions
                        obj._DeleteContent += RemoveNote_FromCollection;

                        this.Notes.Add(obj);
                    }
                }

                this.Notes.CollectionChanged += Notes_CollectionChanged;
            }).ContinueWith((e) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ReloadData = true;
                    Animate = false;
                });
            });
        }

        private void OnRefresh_Core()
        {
            //Generate collection of Notes
            Animate = true;
            Instructions = "Loading your notes";

            Task.Run(() =>
            {
                this.Notes.Clear();

                var notes = _notesManager.Get_NotesByContactID<Notes>(Constants.InMemory_ContactID);
                if (notes.Count != 0)
                {
                    foreach (var note in notes)
                    {
                        var obj = new NotesCellViewModel(note, navigation, dialogue);

                        //Subscriptions
                        obj._DeleteContent += RemoveNote_FromCollection;

                        this.Notes.Add(obj);
                    }
                }
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
