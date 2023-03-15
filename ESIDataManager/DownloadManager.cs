using CsvHelper;
using ECT.Models.ESI.Alliance;
using EsiDataManager.Models.ESI.Corporation;
using ESIDataManager.Events;
using ESIDataManager.Interfaces;
using Microsoft.Office.Interop.Excel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Refit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Popups;
using WinRT.Interop;

namespace ESIDataManager
{
    public sealed class DownloadManager
    {
        private static readonly DownloadManager _instance = new();
        private static readonly JsonSerializer _serializer = new();
        
        public event EventHandler<DownloadCompletedEventArgs> OnDownloadCompleted;
        public event EventHandler<DownloadProgressEventArgs> OnDownloadProgress;

        private static Task _downloadThread;

        private CancellationTokenSource tokenSource = new();

        public static DownloadManager Instance
        { 
            get
            {
                return _instance;
            }
        }

        public bool IsDownloading { get; private set; }

        private DownloadManager() { }

        public void StartDownload(string action, string filePath)
        {
            IsDownloading = true;

            _downloadThread = CreateDownloadTask(action, filePath);

            try
            {
                _downloadThread.Wait();
            }
            catch (Exception e)
            {
                // Cancelled
            }
        }

        public void StopDownload()
        {
            if(IsDownloading)
            {
                try
                {
                    tokenSource.Cancel();
                }
                finally
                {
                    _downloadThread.Dispose();
                    tokenSource.Dispose();
                }

                tokenSource = new CancellationTokenSource();
            }
        }

        private Task CreateDownloadTask(string action, string filePath)
        {
            return Task.Factory.StartNew(async () =>
            {
                tokenSource.Token.ThrowIfCancellationRequested();

                switch(action)
                {
                    case "Alliances":
                        await DownloadAlliances(filePath, tokenSource.Token);
                        break;
                    case "Corporations (NPC)":
                        await DownloadCorporations(filePath, tokenSource.Token);
                        break;
                    case "Dogma Attributes":
                        await DownloadDogmaAttrs(filePath, tokenSource.Token);
                        break;
                    case "Dogma Effects":
                        await DownloadDogmaEffects(filePath, tokenSource.Token);
                        break;
                    case "Dogma Modifiers":
                        await DownloadDogmaModifiers(filePath, tokenSource.Token);
                        break;
                    case "Items (types)":
                        break;
                    case "ItemAttributes":
                        break;
                    case "ItemEffects":
                        break;
                }

                IsDownloading = false;
                OnDownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs
                {
                    Success = _downloadThread.Status == TaskStatus.RanToCompletion
                });
            }, tokenSource.Token);
        }

        #region Download Methods

        private async Task DownloadAlliances(string filePath, CancellationToken cancellationToken)
        {
            var api = GetApi();
            var allianceIds = await api.GetAlliances(cancellationToken);
            OnDownloadProgress?.Invoke(this, new DownloadProgressEventArgs
            {
                TotalCount = allianceIds.Length,
                DownloadProgress = 0
            });

            var allianceDetails = new List<Alliance>();
            for (int i = 0; i < allianceIds.Length; i++)
            {
                var alliance = await api.GetAlliance(allianceIds[i], cancellationToken);

                if (alliance != null)
                {
                    allianceDetails.Add(alliance);
                }

                OnDownloadProgress?.Invoke(this, new DownloadProgressEventArgs
                {
                    TotalCount = allianceIds.Length,
                    DownloadProgress = i + 1
                });

            }

            SaveFile(allianceDetails, filePath);
        }

        private async Task DownloadCorporations(string filePath, CancellationToken cancellationToken)
        {
            var api = GetApi();
            var corpIds = await api.GetNpcCorporations(cancellationToken);
            OnDownloadProgress?.Invoke(this, new DownloadProgressEventArgs
            {
                TotalCount = corpIds.Length,
                DownloadProgress = 0
            });

            var corpDetails = new List<Corporation>();
            for (int i = 0; i < corpIds.Length; i++)
            {
                var corporation = await api.GetCorporation(corpIds[i], cancellationToken);

                if (corporation != null)
                {
                    corpDetails.Add(corporation);
                }

                OnDownloadProgress?.Invoke(this, new DownloadProgressEventArgs
                {
                    TotalCount = corpIds.Length,
                    DownloadProgress = i + 1
                });

            }

            SaveFile(corpDetails, filePath);
        }

        private async Task DownloadDogmaAttrs(string filePath, CancellationToken cancellationToken)
        {

        }

        private async Task DownloadDogmaEffects(string filePath, CancellationToken cancellationToken)
        {

        }

        private async Task DownloadDogmaModifiers(string filePath, CancellationToken cancellationToken)
        {

        }

        #endregion

        private static IEveOnlineApi GetApi()
        {
            var settings = new RefitSettings(new NewtonsoftJsonContentSerializer());
            return RestService.For<IEveOnlineApi>("https://esi.evetech.net/latest", settings);
        }

        #region IO Methods

        private void SaveFile<T>(IEnumerable<T> data, string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists && fileInfo.Length > 0)
                {
                    DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
                    {
                        ContentDialog dg = new()
                        {
                            XamlRoot = App.Current.Window.Content.XamlRoot,
                            Title = "File will be overwritten",
                            Content = "The sepecified file already exists, are you sure you want to overwrite it?",
                            PrimaryButtonText = "Yes",
                            SecondaryButtonText = "No"
                        };

                        if (await dg.ShowAsync() == ContentDialogResult.Primary)
                        {
                            SaveFileAs(data, filePath);
                        }
                    });
                }
                else
                {
                    SaveFileAs(data, filePath);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void SaveFileAs<T>(IEnumerable<T> data, string filePath)
        {
            var fileType = GetFileType(filePath);
            switch (fileType)
            {
                case FileType.CSV:
                    SaveAsCsv(data, filePath);
                    break;
                case FileType.TXT:
                    SaveAsJsonOrTxt(JsonConvert.SerializeObject(data), filePath);
                    break;
                case FileType.XLSX:
                    // Save as excel format
                    SaveAsXlsx(data, filePath);
                    break;
                case FileType.JSON:
                    SaveAsJsonOrTxt(JsonConvert.SerializeObject(data), filePath);
                    break;
            }
        }

        private void SaveAsCsv<T>(IEnumerable<T> data, string path)
        {
            using var writer = new StreamWriter(path);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(data);
        }

        private void SaveAsXlsx<T>(IEnumerable<T> data, string path)
        {
            Application excel = new Application();
            excel.Visible = false;
            excel.DisplayAlerts = false;



        }

        private void SaveAsJsonOrTxt(string data, string path)
        {
            File.WriteAllText(path, data);
        }

        private FileType GetFileType(string filePath)
        {
            try
            {
                var extension = filePath.Split(".")?.Last()?.ToLower();
                if (!string.IsNullOrEmpty(extension))
                {
                    switch (extension)
                    {
                        case "csv":
                            return FileType.CSV;
                        case "txt":
                            return FileType.TXT;
                        case "xlsx":
                            return FileType.XLSX;
                        case "json":
                            return FileType.JSON;
                    }
                }
                throw new ArgumentOutOfRangeException(nameof(filePath));
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion
    }
}
