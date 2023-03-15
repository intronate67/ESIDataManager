using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESIDataManager.Events
{
    public class DownloadCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
    }
}
