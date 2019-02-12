using System;
using System.Threading.Tasks;
using System.Linq;
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

//Services
using Cross.DataVault.Data.Services;
using Cross.DataVault.Services;
using Cross.DataVault.Services.Managers;
using Cross.DataVault.Services.DependencyServices;
using Cross.DataVault.ServiceAccess; //Cloud Service APIs
using Cross.DataVault.ServiceAccess.Configuration;

using System.ServiceModel;

using ReactiveUI;

//Plugins
using Plugin.Connectivity;
using Plugin.Messaging;
using Plugin.Toasts;

namespace Cross.DataVault.ViewModels
{
    public class PasswordViewModel : BaseScreen
    {
        protected readonly IPasswordManager passwordManager;
        protected readonly IToastNotificator _toastNotifier;

        public PasswordViewModel(INavigationService _navigation, IDatabase _database, IDialogue _dialogue, IToastNotificator toastNotifier, ILogging _logging, IAccountManager _accountManager,
            IPasswordManager _passwordManager) : base(_navigation, _database, _logging, _dialogue)
        {
            //Services
            passwordManager = _passwordManager;
            _toastNotifier = toastNotifier;

            Title = "My Passwords";

            //Relays
            IGoBack = new Relays.RelayExtension(GoBack, CanGoBack);
            IOpenFloat = new Relays.RelayExtension(OpenFloat, CanOpenFloat); //Opens up the Password Generator

            //Relays - Refresh Data
            IRefreshData = new Relays.RelayExtension(OnRefresh, CanOnRefresh);

            //Subscriptions
            MessagingCenter.Subscribe<PasswordCreatorViewModel, Passwords>(this, _AddPassword, (receiver, data) => { AddPasswords_ToCollection(data); });
            MessagingCenter.Subscribe<PasswordCreatorViewModel, Passwords>(this, _UpdatePassword, (receiver, data) => { UpdatePasswords_FromCollection(data); });


            //Initialization
            Initialize_Core();
        }

        #region Constants
        private const string _AddPassword = "_AddPassword";
        private const string _UpdatePassword = "_UpdatePassword";
        #endregion

        #region Data

        private ObservableCollection<PasswordCellViewModel> _Passwords;
        public ObservableCollection<PasswordCellViewModel> Passwords
        {
            get { return _Passwords == null ? _Passwords = new ObservableCollection<PasswordCellViewModel>() : _Passwords; }
        }

        #endregion

        #region Loader & Animation Observables
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
                //if (!value)
                //    OnRefresh();
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
            Instructions = "Downloading Passwords";
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
                    var obj = new List<Passwords>();

                    DataVaultWebServiceClient dataService = new DataVaultWebServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.Data_InSecureUrl));
                    var passwords = dataService._GetPasswords_ByUserID(Constants.InMemory_ContactID)._Passwords;
                    if (passwords.Count != 0)
                    {
                        this.Passwords.Clear();
                        passwordManager.Delete_AllPasswordsByContactID(Constants.InMemory_ContactID); //Clear all notes then download them
                        passwords.ForEach(w => obj.Add(LocalMapper.MapPassword_FromServer(w)));

                        if (passwordManager != null)
                        {
                            if (obj.Count != 0)
                            {
                                passwordManager.AddPasswords(obj);
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

                    _AnyError = true;
                    var mEx = new Exceptions(logging, Message, StackTrace);
                    if (mEx != null)
                        mEx.HandleException(mEx, logging);
                }
                finally
                {
                    //dispose of any memory here
                    //if (dialogue != null && _AnyError)
                    //    dialogue.ShowAlert("mmm...Something went wrong", Message);
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
            Constants.Passwords_ID = null;

            if (navigation != null)
                navigation.NavigateToViewModelAsync<PasswordCreatorViewModel>(true);
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

        private ICommand _IOpenDrawer;
        public ICommand IOpenDrawer
        {
            get { return _IOpenDrawer; }
            set { this.RaiseAndSetIfChanged(ref _IOpenDrawer, value); }
        }

        #endregion

        #region Business Logic
        private async void UpdatePasswords_FromCollection(Passwords obj)
        {
            //Add Notes to Server
            Animate = true;
            Instructions = "Updating";

            //Diagnostics
            string Message = string.Empty;
            string StackTrace = string.Empty;
            bool _HasError = false;

            await Task.Run(() =>
            {
                try
                {

                    //Update Collection
                    var Index = this.Passwords.IndexOf(this.Passwords.SingleOrDefault(i => i.ID == obj.Password_ID));
                    this.Passwords[Index].Password = obj.Password;
                    this.Passwords[Index].Description = obj.Description;

                    DataVaultWebServiceClient dataService = new DataVaultWebServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.Data_InSecureUrl));
                    var response = dataService._UpdatePassword_ByID(LocalMapper.MapPassword_ToServer(obj));
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

                    _HasError = true;
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

        private async void AddPasswords_ToCollection(Passwords obj)
        {
            //Add Notes to Server
            Animate = true;
            Instructions = "Creating Password";

            //Diagnostics
            string Message = string.Empty;
            string StackTrace = string.Empty;
            bool _HasError = false;

            string cid = obj.Password_ID; //Temp Client Id

            await Task.Run(() =>
            {
                try
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        var curr = new PasswordCellViewModel(obj, navigation, dialogue);
                        curr._DeleteContent += RemovePassword_FromCollection;
                        this.Passwords.Add(curr);
                    });

                    DataVaultWebServiceClient dataService = new DataVaultWebServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.Data_InSecureUrl));
                    var response = dataService._AddPassword(LocalMapper.MapPassword_ToServer(obj));
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
                            var password = this.Passwords.SingleOrDefault(w => w.ID.Equals(cid));
                            password.ID = response.Content_ID;
                            ReloadData = true;
                        });

                        //Update Password
                        obj.Password_ID = response.Content_ID;
                        this.passwordManager.UpdatePassword(obj);
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

                    _HasError = true;
                    var mEx = new Exceptions(logging, Message, StackTrace);
                    if (mEx != null)
                        mEx.HandleException(mEx, logging);
                }
            }).ContinueWith((e) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Animate = false;
                    //if (_HasError && dialogue != null)
                    //    dialogue.ShowAlert("mmm...Something went wrong", Message);
                });
            });
        }

        private async void RemovePassword_FromCollection(object sender)
        {
            if (sender != null)
            {
                var id = sender as string;

                //Add Notes to Server
                Animate = true;
                Instructions = "Deleting";

                //Diagnostics
                string Message = string.Empty;
                string StackTrace = string.Empty;
                bool _HasError = false;

                //Remove note
                await Task.Run(() =>
                {
                    try
                    {
                        this.Passwords.RemoveAt(this.Passwords.IndexOf(this.Passwords.SingleOrDefault(w => w.ID == id)));
                        passwordManager.Delete_PasswordById(id);

                        DataVaultWebServiceClient dataService = new DataVaultWebServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.Data_InSecureUrl));
                        var response = dataService._DeletePassword_ByID(id, Constants.InMemory_ContactID);
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

                        _HasError = true;
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
            Instructions = "Loading Passwords";

            Task.Run(() =>
            {
                var passwords = passwordManager.Get_PasswordByContactID<Passwords>(Constants.InMemory_ContactID);
                if (passwords.Count != 0)
                {
                    foreach (var password in passwords)
                    {
                        PasswordCellViewModel obj = new PasswordCellViewModel(password, navigation, dialogue);
                        obj._DeleteContent += RemovePassword_FromCollection;

                        this.Passwords.Add(obj);
                    }
                }

                this.Passwords.CollectionChanged += Passwords_CollectionChanged;

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
            Instructions = "Loading Passwords";

            Task.Run(() =>
            {
                var passwords = passwordManager.Get_PasswordByContactID<Passwords>(Constants.InMemory_ContactID);
                if (passwords.Count != 0)
                {
                    foreach (var password in passwords)
                    {
                        PasswordCellViewModel obj = new PasswordCellViewModel(password, navigation, dialogue);
                        obj._DeleteContent += RemovePassword_FromCollection;

                        this.Passwords.Add(obj);
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

        private void Passwords_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() => { ReloadData = true; });
        }

        #endregion
    }
}
