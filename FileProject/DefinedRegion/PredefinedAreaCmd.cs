using PIE.Meteo.Core;
using PIE.Meteo.PIEControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIE.Meteo.FileProject
{
  public  class PredefinedAreaCmd : ImUserCommand
    {
        public bool CanExecute()
        {
            if (mService.MainPanelVM.CurSelectedDocument != null)
            {
                mMapOperation mapcontrol = mService.MainPanelVM.CurSelectedDocument as mMapOperation;
                if (mapcontrol == null) return false;
            }
            return true;
        }
        public async void Execute()
        {
            PredefinedAreas the = new PredefinedAreas();
            the.Show();
        }
    }
}
