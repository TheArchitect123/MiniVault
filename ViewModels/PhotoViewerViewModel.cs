using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Input;

using ReactiveUI;
using Caliburn.Micro;
using Caliburn.Micro.Xamarin.Forms;

using Cross.DataVault.Services.Managers;

//View Models
using Cross.DataVault.ViewModels.Cards;
using Cross.DataVault.ViewModels.Cell;

//Helpers
using Cross.DataVault.Infrastructure.Utilities;
using Cross.DataVault.Data.Mapper;

//Data 
using Cross.DataVault.Data;
using Cross.DataVault.Data.Services;
using Cross.DataVault.Services.DependencyServices;

//Services
using Cross.DataVault.Services;

namespace Cross.DataVault.ViewModels
{
    public class PhotoViewerViewModel : BaseScreen
    {
        private string _Photo;
        public string Photo
        {
            get { return _Photo; }
            set { this.RaiseAndSetIfChanged(ref _Photo, value); }
        }

        #region Navigation Bar 

        private string _Title;
        public string Title
        {
            get { return _Title; }
            set { this.RaiseAndSetIfChanged(ref _Title, value); }
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

        #endregion


        //Displays the photo that the user has chosen 
        public PhotoViewerViewModel(INavigationService _navigation, IDatabase _database, ILogging _logging, IPhotoVideoManager _photoManager, IDialogue _dialogue) 
            : base(_navigation, _database, _logging, _dialogue)
        {
            this.Title = "My Photo";

            IGoBack = new Relays.RelayExtension(GoBack, CanGoBack);

            //Test Data
            //Store on the File System and read the directory
            if (!string.IsNullOrWhiteSpace(Constants.Photos_ID))
            {
                var photo = _photoManager.GetPhoto_ByPhotoID<PhotoVideo>(Constants.Photos_ID);
                if (photo != null)
                {
                    var path = Path.Combine(_database._EnvironmentPath, "photo.png");
                    if (!File.Exists(path))
                    {
                        File.Delete(path);
                        File.WriteAllBytes(path, photo.Photo);
                    }
                    else
                        File.WriteAllBytes(path, photo.Photo);

                    Photo = path;
                }
            }
        }
    }
}
