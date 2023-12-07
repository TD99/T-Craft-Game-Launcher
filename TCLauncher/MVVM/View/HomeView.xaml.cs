﻿using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Utils;
using TCLauncher.Core;
using TCLauncher.Models;
using TCLauncher.MVVM.Windows;

namespace TCLauncher.MVVM.View
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView
    {
        public ObservableCollection<Applet> Applets { get; set; }
        private byte StartupBehaviourLevel = Properties.Settings.Default.StartBehaviour;

        public HomeView()
        {
            InitializeComponent();
            checkInstanceListEmpty();
        }

        private async void loadWV()
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        }

        // TODO: IMPROVE CODE QUALITY
        private void checkInstanceListEmpty()
        {
            // TODO: CHECK FOR N° OF INSTANCES INSTEAD
            if (IoUtils.TclDirectory.IsEmpty(IoUtils.Tcl.InstancesPath))
            {
                profileSelect.Visibility = Visibility.Collapsed;
                profileNoneText.Visibility = Visibility.Visible;
            }
            else
            {
                profileSelect.Visibility = Visibility.Visible;
                profileNoneText.Visibility = Visibility.Collapsed;
            }
        }

        private void discoverEvent(object sender, MouseButtonEventArgs e)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    (window as MainWindow).navigateToServer();
                }
            }
        }

        private async void playBtn_Click(object sender, RoutedEventArgs e)
        {
            var tclInstancesFolder = IoUtils.Tcl.InstancesPath;
            if (!(profileSelect.SelectedItem is InstalledInstance instance))
            {
                MessageBox.Show("Bitte wähle eine Instanz aus!");
                return;
            }

            if (App.Session == null || !App.Session.CheckIsValid())
            {
                if (App.LoginHandler.AccountManager.GetAccounts().Count != 1)
                {
                    var result =
                        MessageBox.Show(
                            "Möchtest du dich mit einem Benutzerkonto anmelden? Klicke auf 'Nein' für ein Offline-Konto.",
                            "Login", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        App.MainWin.navigateToLogin();
                        return;
                    }

                    var dialog = new CustomInputDialog("Bitte gib den gewünschten Offline-Benutzernamen ein.")
                    {
                        Owner = App.MainWin
                    };

                    dialog.Show();

                    if (!await dialog.Result) return;

                    App.Session = MSession.GetOfflineSession(dialog.ResponseText);
                    App.MainWin.SetDisplayAccount(dialog.ResponseText + " (Offline)");
                }
                else
                {
                    try
                    {
                        App.Session = await App.LoginHandler.AuthenticateSilently();
                        App.MainWin.SetDisplayAccount(App.Session?.Username);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
                    }
                }

            }

            var instanceFolder = Path.Combine(tclInstancesFolder, instance.Guid.ToString(), "data");

            try
            {
                App.Launcher = new CMLauncher(new MinecraftPath(instanceFolder));

                var actionWindow = new ActionWindow("Lade Spiel...");

                App.Launcher.FileChanged += (e1) =>
                {
                    // TODO: Check for start event
                    var progress = e1.ProgressedFileCount;
                    var total = e1.TotalFileCount;
                    var percent = progress / total * 100;

                    actionWindow.percent = percent;
                    actionWindow.text = $"[{e1.FileKind}] {e1.FileName}";
                };

                App.Launcher.ProgressChanged += (sender1, e1) =>
                {
                    // TODO: Add percent logic
                };

                actionWindow.Show();

                // TODO: Variable versions
                var process = await App.Launcher.CreateProcessAsync("1.16.4", new MLaunchOption
                {
                    Session = App.Session
                });

                var processUtil = new ProcessUtil(process);
                processUtil.Exited += (sender1, e1) =>
                {
                    // TODO: Add closed logic
                };
                processUtil.StartWithEvents();
                switch (StartupBehaviourLevel)
                {
                    case 0:
                        break;
                    case 1:
                        App.MainWin.WindowState = WindowState.Minimized;
                        break;
                    case 2:
                        Application.Current.Shutdown();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SetAppletViewState(bool val = true)
        {
            if (val)
            {
                homeOverview.Visibility = Visibility.Collapsed;
                mainApplets.Visibility = Visibility.Collapsed;
                appletView.Visibility = Visibility.Visible;
            }
            else
            {
                homeOverview.Visibility = Visibility.Visible;
                mainApplets.Visibility = Visibility.Visible;
                appletView.Visibility = Visibility.Collapsed;

                webView.Source = new Uri("https://tcraft.link/tclauncher/api/plugins/applet-loader/");
            }
        }

        private async void AppletItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Border border = (Border)sender;
            Applet applet = (Applet)border.DataContext;

            if (applet.ActionURL is null) return;
            
            SetAppletViewState(true);

            await Task.Delay(2000);

            webView.Source = new System.Uri(applet.ActionURL);
        }

        private void profileSelect_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (profileSelect.SelectedItem is InstalledInstance selectedInstance)
            {
                Properties.Settings.Default.LastSelected = selectedInstance.Guid;
                Properties.Settings.Default.Save();

                //if (selectedInstance.Servers != null && selectedInstance.Servers.Any())
                //{
                //    servInfo.Visibility = Visibility.Visible;
                //    serverSelect.SelectedIndex = 0;
                //}
                //else
                //{
                //    servInfo.Visibility = Visibility.Collapsed;
                //}
            }
            RefreshApplets();
        }

        private async void RefreshApplets()
        {
            mainApplets.ItemsSource = new ObservableCollection<Applet>
            {
                new Applet(1, null, "https://tcraft.link/tclauncher/api/assets/loader.gif", null, null, null),
                new Applet(2, null, "https://tcraft.link/tclauncher/api/assets/loader.gif", null, null, null)
            };

            try
            {
                InstalledInstance selectedInstance = profileSelect.SelectedItem as InstalledInstance;
                if (selectedInstance is null) throw new Exception();
                string appletsURL = selectedInstance.AppletURL;
                if (String.IsNullOrEmpty(appletsURL)) throw new Exception();
                HttpClient httpClient = new HttpClient();
                var response = await httpClient.GetAsync(appletsURL);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    Applets = new ObservableCollection<Applet>(JsonConvert.DeserializeObject<ObservableCollection<Applet>>(content).OrderByDescending(a => a.Weight));
                }
            }
            catch {
                Applets = null;
            }

            mainApplets.ItemsSource = Applets;
        }

        private void webViewBackButton_Click(object sender, RoutedEventArgs e)
        {
            SetAppletViewState(false);
        }

        //private void serverSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (serverSelect.SelectedItem == null) return;
        //    Server server = (Server)serverSelect.SelectedItem;
        //    currentServerImg.Source = new BitmapImage(new Uri(@"https://tcraft.link/tclauncher/api/plugins/server-tool/GetAccent.php?literal&url=" + server.IP));
        //}
    }
}