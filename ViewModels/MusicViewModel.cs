using System;
using System.Threading.Tasks;

using System.Linq;
using System.Collections.Generic;
using System.Windows.Input;

using Caliburn.Micro;
using Caliburn.Micro.Xamarin.Forms;
using Xamarin.Forms;

//Data
using Cross.DataVault.Data;
using Cross.DataVault.Data.Mapper;

//Helpers
using Cross.DataVault.Infrastructure.Utilities;
using Cross.DataVault.Data.Mapper;

//View Models
using Cross.DataVault.ViewModels.Cards;
using Cross.DataVault.ViewModels.Cell;
using Cross.DataVault.ViewModels.Cell.Secure;

//Services
using Cross.DataVault.Data.Services;
using Cross.DataVault.Services;
using Cross.DataVault.Services.Managers;
using Cross.DataVault.Services.DependencyServices;
using System.ServiceModel;

using ReactiveUI;

//Plugins
using Plugin.Connectivity;

namespace Cross.DataVault.ViewModels
{
    public class MusicViewModel : BaseScreen
    {
        public MusicViewModel(INavigationService _navigation, IDatabase _database, ILogging _logging, IAccountManager _accountManager, IDialogue _dialogue,
            IMusicManager _musicManager) : base(_navigation, _database, _logging, _dialogue)
        {
            musicManager = _musicManager;

            Title = "My Music";

            //Relays
            IGoBack = new Relays.RelayExtension(GoBack, CanGoBack);

            //Relays - Refresh Data
            IRefreshData = new Relays.RelayExtension(OnRefresh, CanOnRefresh);

            //Initialization
            Initialize_Core();
        }

        //Services
        protected readonly IMusicManager musicManager;

        //Constants
        private const string _MusicUpdate = "_MusicUpdate";


        #region Data
        private List<MusicCellViewModel> _Music;
        public List<MusicCellViewModel> Music
        {
            get { return _Music == null ? _Music = new List<MusicCellViewModel>() : _Music; }
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
            Instructions = "Downloading Music";
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
                }
                finally
                {
                    //dispose of any memory here
                    if (dialogue != null)
                        dialogue.ShowAlert("mmm...Something went wrong", Message);
                }
            }).ContinueWith((e) =>
            {
                //Hide the animator when done

                //If any errors occur render them on the dialogue service

                Device.BeginInvokeOnMainThread(() =>
                {
                    ReloadData = true;
                    Animate = false;
                    Refreshing = false;
                });
            });
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

        #region Loader & Animations
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
            set { this.RaiseAndSetIfChanged(ref _Refreshing, value); }
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

        #region Business Logic

        private void Initialize_Core()
        {
            //Generate collection of Notes
            Animate = true;
            Instructions = "Loading Music";

            Task.Run(() =>
            {
                var musics = musicManager.GetMusicCollection_ByContactID<Music>(Constants.InMemory_ContactID);
                if (musics.Count != 0)
                {
                    foreach (var music in musics)
                    {
                        MusicCellViewModel obj = new MusicCellViewModel();
                        obj.Author_DisplayName = music.AuthorName;
                        obj.Album_Title = music.AlbumTitle;
                        obj.Music_Name = music.Music_Name;
                        obj.Duration = String.Format("{0}:{1}", music.Duration.Hours, music.Duration.Minutes);
                        obj.ReleaseDate = music.ReleaseDate.ToString("m");

                        this.Music.Add(obj);
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
