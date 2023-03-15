// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using WinRT;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ESIDataManager
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private WindowsSystemDispatcherQueueHelper m_wsdqHelper;
        private DesktopAcrylicController m_backdropController;
        private SystemBackdropConfiguration m_configurationSource;

        public MainWindow()
        {
            this.InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            TrySetSystemBackdrop();

            DownloadManager.Instance.OnDownloadCompleted += Instance_OnDownloadCompleted;
            DownloadManager.Instance.OnDownloadProgress += Instance_OnDownloadProgress;
        }

        

        private bool TrySetSystemBackdrop()
        {
            if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
            {
                m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                // Create the policy object.
                m_configurationSource = new SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_backdropController = new Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController();
                
            // Enable the system backdrop.
            // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
            m_backdropController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_backdropController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // succeeded
            }

            return false; // Desktop Acrylic is not supported on this system
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed
            // so it doesn't try to use this closed window.
            if (m_backdropController != null)
            {
                m_backdropController.Dispose();
                m_backdropController = null;
            }
            this.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default; break;
            }
        }

        private void treeViewDownloadOptions_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if(args.InvokedItem is TreeViewNode node)
            {
                btnDownload.IsEnabled = !node.HasChildren;
            }
        }

        private async void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new Windows.Storage.Pickers.FileOpenPicker();
            openPicker.FileTypeFilter.Add(".csv");
            openPicker.FileTypeFilter.Add(".xlsx");
            openPicker.FileTypeFilter.Add(".json");
            openPicker.FileTypeFilter.Add(".txt");

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // Initialize the file picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);
            // Open the picker for the user to pick a file
            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                txtBoxFilePath.TextChanged -= txtBoxFilePath_TextChanged;
                txtBoxFilePath.Text = file.Path;
                txtBoxFilePath.TextChanged += txtBoxFilePath_TextChanged;

                // Update selection if file type mismatch
                if (rdiGroupFormat.SelectedIndex == -1
                    || !file.FileType.Equals((rdiGroupFormat.SelectedItem as RadioButton).Tag))
                {
                    switch (file.FileType.ToLower())
                    {
                        case ".csv":
                            rdiGroupFormat.SelectedItem = rdiCsv;
                            break;
                        case ".txt":
                            rdiGroupFormat.SelectedItem = rdiTxt;
                            break;
                        case ".xlsx":
                            rdiGroupFormat.SelectedItem = rdiXlsx;
                            break;
                        case ".json":
                            rdiGroupFormat.SelectedItem = rdiJson;
                            break;
                    }
                }
            }
            else
            {
                ContentDialog dg = new()
                {
                    XamlRoot = this.Content.XamlRoot,
                    Content = "File was null",
                    Title = "No file",
                    CloseButtonText = "Ok"
                };
                _ = await dg.ShowAsync();
            }
        }

        private async void txtBoxFilePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var fileType = txtBoxFilePath.Text.Split(".")?.Last()?.ToLower();
                if(rdiGroupFormat.SelectedIndex == -1 || (!string.IsNullOrEmpty(fileType)
                    && !fileType.Equals((rdiGroupFormat.SelectedItem as RadioButton).Tag)))
                {
                    switch (fileType.ToLower())
                    {
                        case "csv":
                            rdiGroupFormat.SelectedItem = rdiCsv;
                            break;
                        case "txt":
                            rdiGroupFormat.SelectedItem = rdiTxt;
                            break;
                        case "xlsx":
                            rdiGroupFormat.SelectedItem = rdiXlsx;
                            break;
                        case "json":
                            rdiGroupFormat.SelectedItem = rdiJson;
                            break;
                    }

                }
            }
            catch(ArgumentNullException)
            {
                ContentDialog dg = new()
                {
                    XamlRoot = this.Content.XamlRoot,
                    Content = "File path was empty",
                    Title = "Empty path",
                    CloseButtonText = "Ok"
                };

                _ = await dg.ShowAsync();
            }
            catch (InvalidOperationException)
            {
                ContentDialog dg = new()
                {
                    XamlRoot = this.Content.XamlRoot,
                    Content = "File path was formatted incorrectly, please try again.",
                    Title = "Invalid format",
                    CloseButtonText = "Ok"
                };
                _ = await dg.ShowAsync();
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            DownloadManager.Instance.StopDownload();

            // Disable stop button until another download is startedd
            btnStop.IsEnabled = false;

            // Clear out progress, if any
            progressBar.Value = 0;
            lblProgress.Text = "0%";
            lblCountProgress.Text = "0 / 0";
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            // First ensure filepath contains valid extension (csv, txt, json, or xlsx)
            var fileType = txtBoxFilePath.Text.Split(".")?.Last()?.ToLower();
            if (string.IsNullOrEmpty(txtBoxFilePath.Text))
            {
                ContentDialog dg = new()
                {
                    XamlRoot = this.Content.XamlRoot,
                    Content = "The specified path was empty, please enter a valid path and try again",
                    Title = "Empty path",
                    CloseButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Close
                };
                _ = await dg.ShowAsync();
                return;
            }
            else if (string.IsNullOrEmpty(fileType))
            {
                ContentDialog dg = new()
                {
                    XamlRoot = this.Content.XamlRoot,
                    Content = "Could not get file type (extension) from path, please use add a supported extension to the path and try again.",
                    Title = "Invalid File Path (No extension)",
                    DefaultButton = ContentDialogButton.Close,
                    CloseButtonText = "Ok"
                };
                _ = await dg.ShowAsync();
                return;
            }
            else
            {
                switch (fileType.ToLower())
                {
                    case "csv":
                    case "txt":
                    case "xlsx":
                    case "json":
                        break;
                    default:
                        ContentDialog dg = new()
                        {
                            XamlRoot = this.Content.XamlRoot,
                            Content = "The current extension is unsupport, please use a different type and type again.",
                            Title = "Unsupported File Extension",
                            DefaultButton = ContentDialogButton.Close,
                            CloseButtonText = "Ok"
                        };
                        _ = await dg.ShowAsync();
                        return;
                }
            }

            // Disable download until (finished or cancelled) and a valid item is still selected.
            btnDownload.IsEnabled = false;

            // Enable stop btn until download is finished
            btnStop.IsEnabled = true;

            if (treeViewDownloadOptions.SelectedItem is TreeViewNode node)
            {
                var option = node.Content as string;

                DownloadManager.Instance.StartDownload(option, txtBoxFilePath.Text);
            }
        }

        private void Instance_OnDownloadCompleted(object sender, Events.DownloadCompletedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                btnStop.IsEnabled = false;
                if (e.Success)
                {
                    progressBar.Background = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    progressBar.Background = new SolidColorBrush(Colors.Red);
                }
            });
        }

        private void Instance_OnDownloadProgress(object sender, Events.DownloadProgressEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                lblCountProgress.Text = $"{e.DownloadProgress} / {e.TotalCount}";
                var progressPercentage = ((double)e.DownloadProgress / e.TotalCount) * 100;
                lblProgress.Text = $"{progressPercentage:#.##}%";
                progressBar.Value = progressPercentage;
            });
        }
    }

    class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        object m_dispatcherQueueController = null;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
            }
        }
    }
}
