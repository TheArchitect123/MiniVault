using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Caliburn.Micro;
using XLabs.Platform.Device;

namespace Cross.DataVault.Views.Cell.Secure
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PasswordCellView : ViewCell
    {
        public PasswordCellView()
        {
            InitializeComponent();
        }
    }
}