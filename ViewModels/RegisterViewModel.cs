using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.ServiceModel;

using Xamarin.Forms;

using Caliburn.Micro;
using Caliburn.Micro.Xamarin.Forms;

//Services
using Cross.DataVault.Data.Services;
using Cross.DataVault.Services;
using Cross.DataVault.Services.Managers;
using Cross.DataVault.Services.DependencyServices;
using Cross.DataVault.ServiceAccess; //Cloud Service APIs
using Cross.DataVault.ServiceAccess.Configuration;

//Helpers
using Cross.DataVault.Infrastructure.Utilities;
using Cross.DataVault.Data.Mapper;

//Extensions
using Cross.DataVault.Relays;
using Cross.DataVault.Utils;

//Core Data
using Cross.DataVault.Data;

//Plugin
using Plugin.Media;
using Plugin.Media.Abstractions;
using Plugin.Connectivity;
using Plugin.Security.Core;
using ReactiveUI;

namespace Cross.DataVault.ViewModels
{
    public class RegisterViewModel : BaseScreen
    {
        #region Data
        private string _Avatar;
        public string Avatar   // This is generated from the Media Camera plugin. This is read  from the device's photo library cache
        {
            get { return _Avatar; }
            set { this.RaiseAndSetIfChanged(ref _Avatar, value); }
        }

        private byte[] _Avatar_File;
        public byte[] Avatar_File
        {
            get { return _Avatar_File; }
            set { this.RaiseAndSetIfChanged(ref _Avatar_File, value); }
        }

        private string _Username;
        public string Username
        {
            get { return _Username; }
            set
            {

                HasError = false;
                this.RaiseAndSetIfChanged(ref _Username, value);
            }
        }

        private string _Password;
        public string Password
        {
            get { return _Password; }
            set
            {
                HasError = false;
                this.RaiseAndSetIfChanged(ref _Password, value);
            }
        }

        private string _ConfirmPassword;
        public string ConfirmPassword
        {
            get { return _ConfirmPassword; }
            set
            {
                HasError = false;
                this.RaiseAndSetIfChanged(ref _ConfirmPassword, value);
            }
        }

        //Name 
        private string _FirstName;
        public string FirstName
        {
            get { return _FirstName; }
            set { this.RaiseAndSetIfChanged(ref _FirstName, value); }
        }

        private string _LastName;
        public string LastName
        {
            get { return _LastName; }
            set { this.RaiseAndSetIfChanged(ref _LastName, value); }
        }

        //Contact Details
        private string _Mobile;
        public string Mobile
        {
            get { return _Mobile; }
            set { this.RaiseAndSetIfChanged(ref _Mobile, value); }
        }

        private string _Work;
        public string Work
        {
            get { return _Work; }
            set { this.RaiseAndSetIfChanged(ref _Work, value); }
        }
        private string _Home;
        public string Home
        {
            get { return _Home; }
            set { this.RaiseAndSetIfChanged(ref _Home, value); }
        }

        private string _ErrorMessage;
        public string ErrorMessage
        {
            get { return _ErrorMessage; }
            set { this.RaiseAndSetIfChanged(ref _ErrorMessage, value); }
        }

        private bool _HasError;
        public bool HasError
        {
            get { return _HasError; }
            set { this.RaiseAndSetIfChanged(ref _HasError, value); }
        }

        #endregion

        #region Animator
        private bool _RegAnimate;
        public bool RegAnimate
        {
            get
            {
                Console.WriteLine("Animated Get: " + _RegAnimate);
                return _RegAnimate;
            }
            set
            {
                Console.WriteLine("Animated Set: " + value);
                this.RaiseAndSetIfChanged(ref _RegAnimate, value);
            }
        }

        private string _Instructions;
        public string Instructions
        {
            get { return _Instructions; }
            set { this.RaiseAndSetIfChanged(ref _Instructions, value); }
        }

        #endregion


        #region Commands
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

        private ICommand _IRegister;
        public ICommand IRegister
        {
            get { return _IRegister; }
            set { this.RaiseAndSetIfChanged(ref _IRegister, value); }
        }

        public bool CanRegister() { return true; }
        public async void Register()
        {
            RegAnimate = true;
            bool _Error = false;

            //Diagnostics
            string Message = string.Empty;
            string StackTrace = string.Empty;

            //Register user credentials
            await Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(Username))
                        throw new ArgumentNullException("Username cannot be empty. Please try again");
                    if (string.IsNullOrWhiteSpace(Password))
                        throw new ArgumentNullException("Password cannot be empty. Please try again");
                    if (string.IsNullOrWhiteSpace(ConfirmPassword))
                        throw new ArgumentNullException("Password cannot be empty. Please try again");
                    if (!Password.Equals(ConfirmPassword))
                        throw new ArgumentNullException("Passwords do not match");

                    //Regex validation = new Regex(_EmailValidator, RegexOptions.CultureInvariant);
                    //if (!validation.IsMatch(Username))
                    //    throw new InvalidDataException("Your username does not appear to be an email address. Please try again");

                    //First & Last Name
                    if (string.IsNullOrWhiteSpace(FirstName))
                        throw new ArgumentNullException("First name cannot be empty");
                    if (string.IsNullOrWhiteSpace(LastName))
                        throw new ArgumentNullException("Last name cannot be empty");

                    //Generate Account 
                    Account obj = new Account();
                    obj.Contact_ID_Ref = Guid.NewGuid().ToString();
                    obj.FirstName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(FirstName);
                    obj.LastName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(LastName);
                    obj.SiteUser_DisplayName = $"{ obj.FirstName } { obj.LastName }";

                    obj.Sys_Creation = DateTime.Now;
                    obj.Sys_Transaction = DateTime.Now;

                    //Has the passwords on account generation and on login
                    PasswordEncoder hasher = new PasswordEncoder();
                    obj.Username = Username;
                    obj.Password = Password;

                    obj.Mobile = Mobile;
                    obj.Work = Work;
                    obj.Email = Username;
                    obj.Home = Home;
                    obj.Avatar = Avatar_File;
                    obj.Avatar_FilePath = Avatar;

                    if (accountManager != null)
                    {
                        var hashedPassword = hasher.Encode(Password, EncryptType.SHA_512);
                        if (!accountManager.AuthenticateSiteUser_ByCredentials(obj.Username, hashedPassword))
                        {
                            //Add Accounts via the server Via a background service. Update the Guid based on the Id
                            Task.Run(() =>
                            {
                                try
                                {
                                    DataVaultAccountServiceClient serviceClient = new DataVaultAccountServiceClient(ConfigurationManager.InSecurePublicBinding(), new System.ServiceModel.EndpointAddress(Constants.AccountsInSecureUrl));

                                    var response = serviceClient._Generate_AccountForUser(LocalMapper.MapAccount_ToServer(obj));
                                    if (response.Errors.Count != 0)
                                    {
                                        response.Errors.ForEach(w =>
                                        {
                                            var log = LocalMapper.Map_LogWithError(w, string.Empty, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

                                            if (this.logging != null)
                                                this.logging.AddLog(log);
                                        });
                                    }
                                    else
                                    {
                                        accountManager.Update_AccountGuidByUsername(Username, response.Contact_ID);
                                        Constants.InMemory_ContactID = response.Contact_ID;
                                    }
                                }
                                catch (Exception oEx)
                                {
                                    string sMessage = string.Empty;
                                    string sStackTrace = string.Empty;

                                    if (oEx.InnerException != null)
                                    {
                                        sMessage = oEx.InnerException.Message;
                                        sStackTrace = oEx.InnerException.StackTrace;
                                    }
                                    else
                                    {
                                        sMessage = oEx.Message;
                                        sStackTrace = oEx.StackTrace;
                                    }

                                    var mEx = new Exceptions(logging, sMessage, sStackTrace);
                                    if (mEx != null)
                                        mEx.HandleException(mEx, logging);
                                }
                            });

                            obj.Password = hashedPassword;
                            accountManager.AddAccount_ByHashedPassword(obj);
                        }
                        else
                            throw new MemberAccessException("This account already exists. Please try a different username");
                    }
                    else
                        throw new ArgumentNullException("Dependency cannot be null. Please review the source code and contact site administrator for assistance");
                }
                catch (Exception ex)
                {
                    HasError = true;
                    _Error = true;

                    if (ex.Message.Contains("Passwords do not match"))
                        ErrorMessage = "Passwords do not match";
                    else
                        ErrorMessage = "Invalid username or password";

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

                    var pEx = new Exceptions(logging, Message, StackTrace);
                    if (pEx != null)
                        pEx.HandleException(pEx, logging);
                }

            }).WaitUntilComplete(TimeSpan.FromSeconds(4), () =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    RegAnimate = false;

                    if (!_Error)
                    {
                        if (navigation != null)
                            navigation.GoBackAsync(true);
                    }
                    else
                    {
                        dialogue.ShowAlert("mmm...Something went wrong", Message);
                    }
                });
            });
        }

        //Avatar Config
        private ICommand _IAvatar;
        public ICommand IAvatar
        {
            get { return _IAvatar; }
            set { this.RaiseAndSetIfChanged(ref _IAvatar, value); }
        }

        public bool CanOpenAvatar() { return true; }
        public async void OpenAvatar()
        {
            CrossMedia.Current.Initialize();

            //Opens up the Camera or Photos & Videos library where the userr can add a photo
            if (CrossMedia.Current.IsTakePhotoSupported)
            {
                if (Xamarin.Forms.Device.RuntimePlatform == Device.Android)
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
                        Avatar = Photo.Path;

                        using (BinaryReader reader = new BinaryReader(Photo.GetStream()))
                            Avatar_File = reader.ReadBytes((int)Photo.GetStream().Length);
                    }
                }
                else
                {
                    var Photo = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions()
                    {
                        Directory = "avatar",
                        Name = "AvatarPhoto.jpg",
                        SaveToAlbum = false,
                        DefaultCamera = CameraDevice.Front
                    });

                    if (Photo != null)
                    {
                        Avatar = Photo.Path;

                        using (BinaryReader reader = new BinaryReader(Photo.GetStream()))
                            Avatar_File = reader.ReadBytes((int)Photo.GetStream().Length);
                    }
                }
            }
            else
            {
                //Open Photo library and and bind an image to the avatar
                if (CrossMedia.Current.IsPickPhotoSupported)
                {
                    var AlbumPhoto = await CrossMedia.Current.PickPhotoAsync(new PickMediaOptions());

                    if (AlbumPhoto != null)
                    {
                        Avatar = AlbumPhoto.Path;

                        using (BinaryReader reader = new BinaryReader(AlbumPhoto.GetStream()))
                            Avatar_File = reader.ReadBytes((int)AlbumPhoto.GetStream().Length);
                    }
                }
            }
        }
        #endregion

        //Resources & Drawables
        private const string _AvatarContact = "contact.png";
        private const string _WinAvatarContact = "~/Assets/contact.png";

        //constantsS
        private const string _EmailValidator = "\\w\\d\\c@"; //Used by the Regular expression engine, to determine the Email 

        protected readonly IAccountManager accountManager;

        public RegisterViewModel(INavigationService _navigation, IDatabase _database,
            ILogging _logging, IAccountManager _accountManager, IDialogue _dialogue) : base(_navigation, _database, _logging, _dialogue)
        {
            accountManager = _accountManager;

            //Commands
            IRegister = new RelayExtension(Register, CanRegister);
            IAvatar = new RelayExtension(OpenAvatar, CanOpenAvatar);
            IGoBack = new Relays.RelayExtension(GoBack, CanGoBack);

            Instructions = "Generating Account";

            //Initial Avatar Photo
            if (Device.RuntimePlatform != Device.UWP)
                Avatar = _AvatarContact;
            else
                Avatar = _WinAvatarContact;
        }
    }
}
