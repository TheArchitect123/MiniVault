using System;
using ReactiveUI;

namespace Cross.DataVault.ViewModels.Cell.Secure
{
    public class MusicCellViewModel : ReactiveObject
    {
        private string _Author_DisplayName;
        public string Author_DisplayName
        {
            get { return _Author_DisplayName; }
            set { this.RaiseAndSetIfChanged(ref _Author_DisplayName, value); }
        }

        private string _Music_Name;
        public string Music_Name
        {
            get { return _Music_Name; }
            set { this.RaiseAndSetIfChanged(ref _Music_Name, value); }
        }

        private string _Album_Title;
        public string Album_Title
        {
            get { return _Album_Title; }
            set { this.RaiseAndSetIfChanged(ref _Album_Title, value); }
        }

        private string _Duration;
        public string Duration
        {
            get { return _Duration; }
            set { this.RaiseAndSetIfChanged(ref _Duration, value); }
        }

        private string _ReleaseDate;
        public string ReleaseDate
        {
            get { return _ReleaseDate; }
            set { this.RaiseAndSetIfChanged(ref _ReleaseDate, value);}
        }

        private int _ID;
        public int ID
        {
            get { return _ID; }
            set { this.RaiseAndSetIfChanged(ref _ID, value); }
        }
    }
}
