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
using System.ServiceModel;

using ReactiveUI;

//Plugins
using Plugin.Connectivity;
using Plugin.Messaging;
using Plugin.Toasts;

namespace Cross.DataVault.ViewModels
{
    public class EmailViewModel : BaseScreen
    {
        protected readonly IPasswordManager passwordManager;
        protected readonly IToastNotificator _toastNotifier;

        public EmailViewModel(INavigationService _navigation, IDatabase _database, IDialogue _dialogue, IToastNotificator toastNotifier, ILogging _logging, IAccountManager _accountManager,
            IPasswordManager _passwordManager) : base(_navigation, _database, _logging, _dialogue)
        {
            //Services
            passwordManager = _passwordManager;
            _toastNotifier = toastNotifier;

            Title = "My Emails";

            //Relays
            IGoBack = new Relays.RelayExtension(GoBack, CanGoBack);
            IOpenFloat = new Relays.RelayExtension(OpenFloat, CanOpenFloat); //Opens up the Password Generator

            //Relays - Refresh Data
            IRefreshData = new Relays.RelayExtension(OnRefresh, CanOnRefresh);
        }

        #region Constants
        private const string _AddPassword = "_AddPassword";
        private const string _UpdatePassword = "_UpdatePassword";
        #endregion

        #region Data

        private ObservableCollection<EmailCellViewModel> _Emails;
        public ObservableCollection<EmailCellViewModel> Emails
        {
            get { return _Emails; }
            set { this.RaiseAndSetIfChanged(ref _Emails, value); }
        }

        #endregion

        #region Loader & Animation Observables
        private bool _ReloadData;
        public bool ReloadData
        {
            get { return _ReloadData; }
            set { this.RaiseAndSetIfChanged(ref _ReloadData, value); }
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
            //Reads Emails from Gmail, Outlook, and other applications
            //Start querying the 
            Instructions = "Downloading Emails";
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
                    if (dialogue != null && _AnyError)
                        dialogue.ShowAlert("mmm...Something went wrong", Message);
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
        
    }
}
