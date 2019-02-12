using System;
using System.Windows.Input;
using System.ServiceModel;

using Caliburn.Micro;
using Caliburn.Micro.Xamarin.Forms;
using Xamarin.Forms;

//Helpers
using Cross.DataVault.Infrastructure.Utilities;
using Cross.DataVault.Data.Mapper;

//Extensions
using Cross.DataVault.Relays;

//Services
using Cross.DataVault.Data.Services;
using Cross.DataVault.Services;
using Cross.DataVault.Services.Managers;
using Cross.DataVault.Services.DependencyServices;

//Core Data
using Cross.DataVault.Data;
using ReactiveUI;

namespace Cross.DataVault.ViewModels
{
    public class PasswordCreatorViewModel : BaseScreen
    {
        protected readonly IPasswordManager _passwordManager;

        public PasswordCreatorViewModel(INavigationService _navigation, IDatabase _database, ILogging _logging, IDialogue _dialogue,
            IPasswordManager _passwordManager
            ) : base(_navigation, _database, _logging, _dialogue)
        {
            this._passwordManager = _passwordManager;

            IConfirm = new RelayExtension(Confirm, CanConfirm);
            IGoBack = new RelayExtension(GoBack, CanGoBack);

            Title = "Add a Password";

            //Bind the Editable data to the views
            if (!string.IsNullOrWhiteSpace(Constants.Passwords_ID))
            {
                var password = _passwordManager.Get_PasswordByID<Passwords>(Constants.Passwords_ID);
                if (password != null)
                {
                    Password = password.Password;
                    Description = password.Description;
                }
            }
        }

        #region Constants
        private const string _AddPassword = "_AddPassword";
        private const string _UpdatePassword = "_UpdatePassword";
        #endregion


        #region Data

        private string _Password;
        public string Password
        {
            get
            {

                return _Password;
            }
            set { this.RaiseAndSetIfChanged(ref _Password, value); }
        }

        private string _Description;
        public string Description
        {
            get
            {
                return _Description;
            }
            set { this.RaiseAndSetIfChanged(ref _Description, value); }
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
            //Call Notes Dependency and add to the Collection of Notes, along to the database
            try
            {
                if (string.IsNullOrWhiteSpace(Description))
                    throw new ArgumentNullException("Description cannot be empty");
                if (string.IsNullOrWhiteSpace(Password))
                    throw new ArgumentNullException("Password cannot be empty");

                if (!string.IsNullOrWhiteSpace(Constants.Passwords_ID))
                {
                    var password = _passwordManager.Get_PasswordByID<Passwords>(Constants.Passwords_ID);
                    if (password != null)
                    {
                        password.Password = Password;
                        password.Description = Description;

                        _passwordManager.UpdatePassword(password);
                        MessagingCenter.Send<PasswordCreatorViewModel, Passwords>(this, _UpdatePassword, password);
                    }
                    else
                    {
                        var obj = new Passwords();
                        obj.Contact_ID_Ref = Constants.InMemory_ContactID;
                        obj.Password_ID = _passwordManager.Get_NewPasswordID();
                        obj.Description = Description;
                        obj.Password = Password;
                        obj.Sys_Creation = DateTime.Now;
                        obj.Sys_Transaction = DateTime.Now;

                        _passwordManager.AddPassword(obj);

                        MessagingCenter.Send<PasswordCreatorViewModel, Passwords>(this, _AddPassword, obj);
                    }
                }
                else
                {
                    var obj = new Passwords();
                    obj.Contact_ID_Ref = Constants.InMemory_ContactID;
                    obj.Password_ID = _passwordManager.Get_NewPasswordID();
                    obj.Description = Description;
                    obj.Password = Password;
                    obj.Sys_Creation = DateTime.Now;
                    obj.Sys_Transaction = DateTime.Now;

                    _passwordManager.AddPassword(obj);

                    MessagingCenter.Send<PasswordCreatorViewModel, Passwords>(this, _AddPassword, obj);
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
                if (dialogue != null && mEx != null)
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
