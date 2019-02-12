using Caliburn.Micro;
using Caliburn.Micro.Xamarin.Forms;

using System;
using System.Windows.Input;
using System.Collections.Generic;

using System.ComponentModel;
using System.Runtime.CompilerServices;

//View Models
using Cross.DataVault.ViewModels.Cell;

//Extensions
using Cross.DataVault.Relays;

//Services
using Cross.DataVault.Data.Services;
using Cross.DataVault.Services.DependencyServices;
using Cross.DataVault.Services;

//Reactive
using ReactiveUI;

//Data 
using Cross.DataVault.Data.Interface;
using Cross.DataVault.Data;
using Cross.DataVault.Data.Mapper;

//Utilities
using Cross.DataVault.Infrastructure.Utilities;

namespace Cross.DataVault.ViewModels
{
    public abstract class BaseScreen : ReactiveObject
    {
        //Resources
        private const string _MenuSource = "menu.png";
        private const string _SearchSource = "search_white.png";

        private const string _WinMenuSource = "~/Assets/menu.png";
        private const string _WinSearchSource = "~/Assets/search_white.png";


        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { this.RaiseAndSetIfChanged(ref _isBusy, value); }
        }

        protected readonly INavigationService navigation;
        protected readonly IDatabase database;
        protected readonly ILogging logging;
        protected readonly IDialogue dialogue;

        //Unhandled Exceptions
        protected virtual void OnUnhandledException(object error)
        {
            var exception = error as Exceptions;
            if (exception != null)
            {
                if (logging != null)
                    exception.HandleException(exception, logging);
            }
        }

        //Collections

        #region Networking
        private void Current_ConnectivityChanged(object sender, Plugin.Connectivity.Abstractions.ConnectivityChangedEventArgs e)
        {
            if (!e.IsConnected)
            {
                var dialogue = IoC.Get<IDialogue>();
                if (dialogue != null)
                    dialogue.ShowAlert("Lost internet connection", "This application requires a working internet connection to sync data");
            }
        }
        #endregion

        //Device Information -- Useful APIs to expose to the View Models
        public string Date => DateTime.Now.ToString("dd MM");
        public string Time => DateTime.Now.ToString("hh:mm tt");
        public string MachineName => Environment.MachineName;

        public BaseScreen(INavigationService _navigation, IDatabase _database, ILogging _logging, IDialogue _dialogue)
        {
            navigation = _navigation;
            database = _database;
            logging = _logging;
            dialogue = _dialogue;

            //Monitor Network connection
            Plugin.Connectivity.CrossConnectivity.Current.ConnectivityChanged += Current_ConnectivityChanged;
        }
    }
}
