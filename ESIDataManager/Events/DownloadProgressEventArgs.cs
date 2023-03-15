using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESIDataManager.Events
{
    public class DownloadProgressEventArgs : EventArgs
    {
        public int TotalCount { get; set; }
        public int DownloadProgress { get; set; }
    }
}
