using System;
using System.Collections.Generic;
using System.Windows.Input;

using Caliburn.Micro;
using Caliburn.Micro.Xamarin.Forms;

//View Models
using Cross.DataVault.ViewModels.Cards;
using Cross.DataVault.ViewModels.Cell;

//Services
using Cross.DataVault.Data.Services;
using Cross.DataVault.Services.DependencyServices;
using Cross.DataVault.Services;

namespace Cross.DataVault.ViewModels
{
    public class SearchViewModel : BaseScreen
    {
        public SearchViewModel(INavigationService _navigation, IDatabase _database, ILogging _logging, IDialogue _dialogue) : base(_navigation, _database, _logging, _dialogue)
        {

        }
    }
}
