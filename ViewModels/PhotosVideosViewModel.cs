using System;
using System.Threading.Tasks;
using System.ServiceModel;

using System.IO;
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

//Services
using Cross.DataVault.Services.DependencyServices;
using Cross.DataVault.Data.Services;
using Cross.DataVault.Services;
using Cross.DataVault.Services.Managers;
using Cross.DataVault.ServiceAccess; //Cloud Service APIs
using Cross.DataVault.ServiceAccess.Configuration;

//Helpers
using Cross.DataVault.Infrastructure.Utilities;
using Cross.DataVault.Data.Mapper;

//Data 
using Cross.DataVault.Data;
using Cross.DataVault.Data.Mapper;

//Plugins
using Plugin.Media;
using Plugin.Media.Abstractions;
using ReactiveUI;

//Plugins
using Plugin.Connectivity;

namespace Cross.DataVault.ViewModels
{
    public class PhotosVideosViewModel : BaseScreen
    {
        //Resources
        protected readonly IAccountManager _accountManager;
        protected readonly IPhotoVideoManager _photoManager;

        //Constants
        private const string _UpdatePhotos = "_UpdatePhotos";

        public PhotosVideosViewModel(INavigationService _navigation, IDatabase _database, ILogging _logging,
            IDialogue dialogue, IAccountManager accountManager, IPhotoVideoManager photoManager) : base(_navigation, _database, _logging, dialogue)
        {
            _accountManager = accountManager;
            _photoManager = photoManager;

            Title = "My Photos";

            //Relays
            IGoBack = new Relays.RelayExtension(GoBack, CanGoBack);
            IOpenFloat = new Relays.RelayExtension(OpenFloat, CanOpenFloat); // Take Photo & Video

            //Relays - Refresh Data
            IRefreshData = new Relays.RelayExtension(OnRefresh, CanOnRefresh);

            //Initialization
            Initialize_Core();
        }

        #region Data

        private ObservableCollection<PhotosVideoCellViewModel> _Photos;
        public ObservableCollection<PhotosVideoCellViewModel> Photos
        {
            get { return _Photos == null ? _Photos = new ObservableCollection<PhotosVideoCellViewModel>() : _Photos; }
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
            Instructions = "Downloading Photos & Videos";
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
                    //Query the local media library 
                    //Query the Server's Store and Add to the Collection
                    var obj = new List<PhotoVideo>();
                    var cells = new ObservableCollection<PhotosVideoCellViewModel>();
                    
                    DataVaultWebServiceClient dataService = new DataVaultWebServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.Data_InSecureUrl));

                    var photos = dataService._GetPhotos_ByUserID(Constants.InMemory_ContactID);
                    if (photos._Photos.Count != 0)
                    {
                        var SiteUser = _accountManager.GetSiteUser_ByID<Cross.DataVault.Data.Account>(Constants.InMemory_ContactID);
                        if (SiteUser != null)
                        {
                            if (photos._Photos.Count != 0)
                            {
                                this.Photos.Clear();
                                _photoManager.Delete_PhotosByUserId(Constants.InMemory_ContactID); //Clear all notes then download them
                                
                                photos._Photos.ForEach(w =>
                                {
                                    obj.Add(LocalMapper.MapPhoto_FromServer(w));
                                });

                                if (_photoManager != null)
                                    _photoManager.AddPhoto_ByCollections(obj);
                                OnRefresh_Core();
                            }
                        }
                    }

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
                    //if (dialogue != null)
                    //    dialogue.ShowAlert("mmm...Something went wrong", Message);
                }
            }).ContinueWith((e) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Refreshing = false;
                    Animate = false;
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
            Animate = true;
            Instructions = "Adding Photo";
            bool HasError = false;

            string eMessage = string.Empty;
            string eStackTrace = string.Empty;

            string cid = Guid.NewGuid().ToString(); //Temp Client Id

            //Open Dialogue, which allows the user to choose between a photo or video
            if (dialogue != null)
            {
                try
                {
                    CrossMedia.Current.Initialize();
                    var id = Guid.NewGuid().ToString();

                    dialogue.ShowAlert_WithCameraOption("Take or Pick a Photo", "Would you like to take a photo or pick a photo?", async () =>
                    {
                        if (CrossMedia.Current.IsCameraAvailable)
                        {
                            var Photo = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions()
                            {
                                Directory = "avatar",
                                SaveToAlbum = false,
                                Name = "AvatarPhoto.jpg",
                                DefaultCamera = CameraDevice.Rear
                            });

                            if (Photo != null)
                            {
                                var curr = _accountManager.GetSiteUser_ByID<Cross.DataVault.Data.Account>(Constants.InMemory_ContactID);
                                var obj = new PhotosVideoCellViewModel(navigation, dialogue);

                                obj.Author_DisplayName = String.Format("{0} {1}", curr.FirstName, curr.LastName);
                                obj.ID = id;
                                obj.Author_ID = curr.Contact_ID_Ref;

                                //Subscriptions
                                obj._DeleteContent += RemovePhoto_FromCollection;

                                obj.Date = $"{DateTime.Now.ToString("m")}, {DateTime.Now.ToString("hh:mm tt")}";
                                obj.Time = DateTime.Now.ToString("hh:mm tt");

                                using (BinaryReader reader = new BinaryReader(Photo.GetStream()))
                                    obj.Photo = reader.ReadBytes((int)Photo.GetStream().Length);

                                //Photo Access Object
                                PhotoVideo item = new PhotoVideo();
                                item.User_ID = curr.Contact_ID_Ref;
                                item.Photo = obj.Photo;
                                item.Content_ID = id;

                                item.Author_FirstName = curr.FirstName;
                                item.Author_LastName = curr.LastName;
                                item.Author_DisplayName = String.Format("{0} {1}", curr.FirstName, curr.LastName);

                                item.Sys_Creation = DateTime.Now;

                                //Add Photo To Server
                                await Task.Run(() =>
                                {
                                    try
                                    {
                                        Device.BeginInvokeOnMainThread(() => { this.Photos.Add(obj); });
                                        _photoManager.AddPhoto(item);
                                        Constants.Photos_ID = null;

                                        DataVaultWebServiceClient dataService = new DataVaultWebServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.Data_InSecureUrl));

                                        var response = dataService._AddPhoto(LocalMapper.MapPhoto_ToServer(item)); // Add Photo to Server
                                        if (response.Errors.Count != 0)
                                            throw new Exception("Message: " + response.Errors[0]);
                                        else
                                        {
                                            Device.BeginInvokeOnMainThread(() =>
                                            {
                                                var photo = this.Photos.SingleOrDefault(w => w.ID.Equals(cid));
                                                photo.ID = response.Content_ID;
                                                ReloadData = true;
                                            });
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        HasError = true;

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
                                }).ContinueWith((e) =>
                                {
                                    Device.BeginInvokeOnMainThread(() =>
                                    {
                                        Animate = false;

                                        //Output a dialogue here
                                        //if (dialogue != null && HasError)
                                        //    dialogue.ShowAlert("mmm...Something went wrong", eMessage);
                                    });
                                });
                            }
                            else
                                Device.BeginInvokeOnMainThread(() => { Animate = false; });
                        }
                        else
                        {
                            var PickPhoto = await CrossMedia.Current.PickPhotoAsync(new PickMediaOptions() { });
                            if (PickPhoto != null)
                            {
                                var curr = _accountManager.GetSiteUser_ByID<Cross.DataVault.Data.Account>(Constants.InMemory_ContactID);
                                var obj = new PhotosVideoCellViewModel(navigation, dialogue);
                                obj.Author_DisplayName = String.Format("{0} {1}", curr.FirstName, curr.LastName);
                                obj.ID = id;
                                obj.Author_ID = curr.Contact_ID_Ref;

                                if (string.IsNullOrWhiteSpace(obj.Author_DisplayName))
                                    obj.Author_DisplayName = curr.SiteUser_DisplayName;

                                obj.Date = $"{DateTime.Now.ToString("m")}, {DateTime.Now.ToString("hh:mm tt")}";
                                obj.Time = DateTime.Now.ToString("hh:mm tt");

                                //Subscriptions
                                obj._DeleteContent += RemovePhoto_FromCollection;

                                //Photo
                                using (BinaryReader reader = new BinaryReader(PickPhoto.GetStream()))
                                    obj.Photo = reader.ReadBytes((int)PickPhoto.GetStream().Length);

                                //Photo Access Object
                                PhotoVideo item = new PhotoVideo();
                                item.User_ID = curr.Contact_ID_Ref;
                                item.Content_ID = id;

                                item.Author_FirstName = curr.FirstName;
                                item.Author_LastName = curr.LastName;
                                item.Author_DisplayName = String.Format("{0} {1}", curr.FirstName, curr.LastName);

                                item.Photo = obj.Photo;
                                item.Sys_Creation = DateTime.Now;

                                //Add Photo To Server
                                await Task.Run(() =>
                                {
                                    try
                                    {
                                        Device.BeginInvokeOnMainThread(() => { this.Photos.Add(obj); });
                                        _photoManager.AddPhoto(item);
                                        Constants.Photos_ID = null;

                                        DataVaultWebServiceClient dataService = new DataVaultWebServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.Data_InSecureUrl));

                                        var response = dataService._AddPhoto(LocalMapper.MapPhoto_ToServer(item)); // Add Photo to Server
                                        if (response.Errors.Count != 0)
                                            throw new Exception("Message: " + response.Errors[0]);
                                        else
                                        {
                                            Device.BeginInvokeOnMainThread(() =>
                                            {
                                                var photo = this.Photos.SingleOrDefault(w => w.ID.Equals(cid));
                                                photo.ID = response.Content_ID;
                                                ReloadData = true;
                                            });
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        HasError = true;
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
                                    }
                                }).ContinueWith((e) =>
                                {
                                    Device.BeginInvokeOnMainThread(() =>
                                    {
                                        Animate = false;

                                        //if (dialogue != null && HasError)
                                        //    dialogue.ShowAlert("mmm...Something went wrong", eMessage);
                                    });
                                });
                            }
                            else
                                Device.BeginInvokeOnMainThread(() => { Animate = false; });
                        }

                    }, async () =>
                    {
                        var PickPhoto = await CrossMedia.Current.PickPhotoAsync(new PickMediaOptions() { });

                        if (PickPhoto != null)
                        {
                            var curr = _accountManager.GetSiteUser_ByID<Cross.DataVault.Data.Account>(Constants.InMemory_ContactID);
                            var obj = new PhotosVideoCellViewModel(navigation, dialogue);
                            obj.Author_DisplayName = String.Format("{0} {1}", curr.FirstName, curr.LastName);
                            obj.ID = id;
                            obj.Author_ID = curr.Contact_ID_Ref;

                            if (string.IsNullOrWhiteSpace(obj.Author_DisplayName))
                                obj.Author_DisplayName = curr.SiteUser_DisplayName;

                            //Subscriptions
                            obj._DeleteContent += RemovePhoto_FromCollection;

                            obj.Date = $"{DateTime.Now.ToString("m")}, {DateTime.Now.ToString("hh:mm tt")}";
                            obj.Time = DateTime.Now.ToString("hh:mm tt");

                            //Photo
                            using (BinaryReader reader = new BinaryReader(PickPhoto.GetStream()))
                                obj.Photo = reader.ReadBytes((int)PickPhoto.GetStream().Length);

                            //Photo Access Object
                            PhotoVideo item = new PhotoVideo();
                            item.User_ID = curr.Contact_ID_Ref;
                            item.Content_ID = id;

                            item.Author_FirstName = curr.FirstName;
                            item.Author_LastName = curr.LastName;
                            item.Author_DisplayName = String.Format("{0} {1}", curr.FirstName, curr.LastName);

                            item.Photo = obj.Photo;
                            item.Sys_Creation = DateTime.Now;

                            //Add Photo To Server
                            await Task.Run(() =>
                            {
                                try
                                {
                                    Device.BeginInvokeOnMainThread(() => { this.Photos.Add(obj); });
                                    _photoManager.AddPhoto(item);
                                    Constants.Photos_ID = null;

                                    DataVaultWebServiceClient dataService = new DataVaultWebServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.Data_InSecureUrl));

                                    var response = dataService._AddPhoto(LocalMapper.MapPhoto_ToServer(item)); // Add Photo to Server
                                    if (response.Errors.Count != 0)
                                        throw new Exception("Message: " + response.Errors[0]);
                                    else
                                    {
                                        Device.BeginInvokeOnMainThread(() =>
                                        {
                                            var photo = this.Photos.SingleOrDefault(w => w.ID.Equals(cid));
                                            photo.ID = response.Content_ID;
                                            ReloadData = true;
                                        });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    HasError = true;
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
                            }).ContinueWith((e) =>
                            {
                                Device.BeginInvokeOnMainThread(() =>
                                {
                                    Animate = false;
                                    //if (dialogue != null && HasError)
                                    //    dialogue.ShowAlert("mmm...Something went wrong", eMessage);
                                });
                            });
                        }
                        else
                            Device.BeginInvokeOnMainThread(() => { Animate = false; });
                    });
                }
                catch (Exception ex)
                {
                    HasError = true;
                    string pMessage = string.Empty;
                    string pStackTrace = string.Empty;

                    if (ex.InnerException != null)
                    {
                        pMessage = ex.InnerException.Message;
                        pStackTrace = ex.InnerException.StackTrace;
                    }
                    else
                    {
                        pMessage = ex.Message;
                        pStackTrace = ex.StackTrace;
                    }

                    var mEx = new Exceptions(logging, pMessage, pStackTrace);
                    if (mEx != null)
                        mEx.HandleException(mEx, logging);

                    //Output a dialogue here
                    //if (dialogue != null && HasError)
                    //    dialogue.ShowAlert("mmm...Something went wrong", mEx.Message);
                }
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

        #region Loader & Animation Observables
        private bool _ReloadData;
        public bool ReloadData
        {
            get { return _ReloadData; }
            private set
            {
                if (value && _ReloadData)
                    _ReloadData = false;
                else
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

        private async void RemovePhoto_FromCollection(object sender)
        {
            if (sender != null)
            {
                var id = sender as string;

                //Add Notes to Server
                Animate = true;
                Instructions = "Deleting Photo";

                //Diagnostics
                string Message = string.Empty;
                string StackTrace = string.Empty;
                bool _HasError = false;

                //Remove note
                await Task.Run(() =>
                {
                    try
                    {
                        this.Photos.RemoveAt(this.Photos.IndexOf(this.Photos.SingleOrDefault(w => w.ID == id)));
                        _photoManager.Delete_PhotoByID(id);

                        DataVaultWebServiceClient dataService = new DataVaultWebServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.Data_InSecureUrl));

                        var response = dataService._DeletePhoto_ByID(id, Constants.InMemory_ContactID);
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

        public void Initialize_Core()
        {
            Animate = true;
            Instructions = "Loading Photos & Videos";

            Task.Run(() =>
            {
                var photos = _photoManager.GetPhotos_ByUserID<PhotoVideo>(Constants.InMemory_ContactID);
                var SiteUser = _accountManager.GetSiteUser_ByID<Cross.DataVault.Data.Account>(Constants.InMemory_ContactID);
                if (photos.Count != 0)
                {
                    foreach (var photo in photos)
                    {
                        var obj = new PhotosVideoCellViewModel(photo, navigation, dialogue);
                        if (SiteUser != null && !string.IsNullOrWhiteSpace(SiteUser.SiteUser_DisplayName))
                            obj.Author_DisplayName = SiteUser.SiteUser_DisplayName;

                        obj._DeleteContent += RemovePhoto_FromCollection;
                        this.Photos.Add(obj);
                    }
                }

                this.Photos.CollectionChanged += Photos_CollectionChanged;

            }).ContinueWith((e) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ReloadData = true;
                    Animate = false;
                });
            });
        }

        public void OnRefresh_Core()
        {
            Animate = true;
            Instructions = "Loading Photos & Videos";

            Task.Run(() =>
            {
                var photos = _photoManager.GetPhotos_ByUserID<PhotoVideo>(Constants.InMemory_ContactID);
                var SiteUser = _accountManager.GetSiteUser_ByID<Cross.DataVault.Data.Account>(Constants.InMemory_ContactID);
                if (photos.Count != 0)
                {
                    foreach (var photo in photos)
                    {
                        var obj = new PhotosVideoCellViewModel(photo, navigation, dialogue);
                        if (SiteUser != null && !string.IsNullOrWhiteSpace(SiteUser.SiteUser_DisplayName))
                            obj.Author_DisplayName = SiteUser.SiteUser_DisplayName;

                        obj._DeleteContent += RemovePhoto_FromCollection;
                        this.Photos.Add(obj);
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

        private void Photos_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() => { ReloadData = true; });
        }

        #endregion

    }
}
