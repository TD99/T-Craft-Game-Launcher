﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using T_Craft_Game_Launcher.MVVM.Model;

namespace T_Craft_Game_Launcher.MVVM.View
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        public ObservableCollection<Applet> Applets { get; set; }
        private byte StartupBehaviourLevel = Properties.Settings.Default.StartBehaviour;

        public HomeView()
        {
            InitializeComponent();
            loadAccount();
            checkInstanceListEmpty();
        }

        private async void loadWV()
        {
            await webView.EnsureCoreWebView2Async();
        }

        private void checkInstanceListEmpty()
        {
            if (Properties.Settings.Default.FirstTime)
            {
                profileSelect.Visibility = Visibility.Collapsed;
                profileNoneText.Visibility = Visibility.Visible;
            }
        }

        private void loadAccount()
        {
            try
            {
                string tclFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL");
                string udataFolder = Path.Combine(tclFolder, "UData");
                string avatarsFolder = Path.Combine(udataFolder, "Avatars");
                string skinsFolder = Path.Combine(udataFolder, "Skins");
                string accountFile = Path.Combine(udataFolder, "launcher_accounts.json");

                string jsonText = File.ReadAllText(accountFile);
                JObject json = JObject.Parse(jsonText);

                string name = (string)json["accounts"][(string)json["activeAccountLocalId"]]["minecraftProfile"]["name"];
                string id = (string)json["accounts"][(string)json["activeAccountLocalId"]]["minecraftProfile"]["id"];

                string avatarSrc = $"https://mc-heads.net/avatar/{id}";
                string avatarCacheFile = Path.Combine(avatarsFolder, $"{id}.png");

                if (!Directory.Exists(avatarsFolder))
                {
                    Directory.CreateDirectory(avatarsFolder);
                }

                if (File.Exists(avatarCacheFile))
                {
                    avatarSrc = avatarCacheFile;
                }
                else
                {
                    var client = new WebClient();
                    client.DownloadFile(avatarSrc, avatarCacheFile);
                }

                string skinSrc = $"https://mc-heads.net/body/{id}";
                string skinCacheFile = Path.Combine(skinsFolder, $"{id}.png");

                if (!Directory.Exists(skinsFolder))
                {
                    Directory.CreateDirectory(skinsFolder);
                }

                if (File.Exists(skinCacheFile))
                {
                    skinSrc = skinCacheFile;
                }
                else
                {
                    var client = new WebClient();
                    client.DownloadFile(skinSrc, skinCacheFile);
                }

                BitmapImage avatarBitmap = new BitmapImage();
                avatarBitmap.BeginInit();
                avatarBitmap.UriSource = new Uri(avatarSrc);
                avatarBitmap.EndInit();

                BitmapImage skinBitmap = new BitmapImage();
                skinBitmap.BeginInit();
                skinBitmap.UriSource = new Uri(skinSrc);
                skinBitmap.EndInit();

                Dispatcher.Invoke(() =>
                {
                    textUserName.Text = name;
                    imageUserPicture.Source = avatarBitmap;
                    imageBodyPicture.Source = skinBitmap;
                });
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    textUserName.Text = "Anonym";
                    imageUserPicture.Source = null;
                });
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

        private void playBtn_Click(object sender, RoutedEventArgs e)
        {
            InstalledInstance selectedInstance = profileSelect.SelectedItem as InstalledInstance;

            try
            {
                string tclFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL");
                string instanceFolder = Path.Combine(tclFolder, "Instances");
                string runtimeFolder = Path.Combine(tclFolder, "Runtime");
                string udataFolder = Path.Combine(tclFolder, "UData");

                string selectedInstanceFolder = Path.Combine(instanceFolder, selectedInstance.Guid.ToString());
                string instanceDataFolder = Path.Combine(selectedInstanceFolder, "data");

                if (!Directory.Exists(selectedInstanceFolder))
                {
                    MessageBox.Show($"Die Instanz '{selectedInstance.Name}' konnte nicht gefunden werden!");
                    return;
                }

                string udataLoginFile = Path.Combine(udataFolder, "launcher_accounts.json");
                string targetLoginFile = Path.Combine(instanceDataFolder, "launcher_accounts.json");

                if (File.Exists(udataLoginFile))
                {
                    File.Copy(udataLoginFile, targetLoginFile, true);
                }

                string msaCredentialsFile = Path.Combine(udataFolder, "launcher_msa_credentials.bin");
                string targetMsaCredentialsFile = Path.Combine(instanceDataFolder, "launcher_msa_credentials.bin");

                if (File.Exists(msaCredentialsFile))
                {
                    File.Copy(msaCredentialsFile, targetMsaCredentialsFile, true);
                }

                string exeFile = Path.Combine(runtimeFolder, "Minecraft.exe");
                if (!File.Exists(exeFile))
                {
                    MessageBox.Show("Der MC Launcher existiert nicht!");
                    return;
                }

                Process launcher = new Process();
                launcher.StartInfo.FileName = exeFile;
                launcher.StartInfo.Arguments = $"--workDir=\"{instanceDataFolder}\"";
                launcher.EnableRaisingEvents = true;

                launcher.Exited += (sender1, e1) =>
                {
                    try
                    {
                        if (File.Exists(targetLoginFile) && new FileInfo(targetLoginFile).Length > 0)
                        {
                            File.Copy(targetLoginFile, udataLoginFile, true);
                        }
                        if (File.Exists(targetMsaCredentialsFile) && new FileInfo(targetMsaCredentialsFile).Length > 0)
                        {
                            File.Copy(targetMsaCredentialsFile, msaCredentialsFile, true);
                        }
                    }
                    catch { }
                };

                var action = new ActionWindow("Start vorbereiten...");
                action.Owner = Application.Current.MainWindow;
                action.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                action.Show();

                launcher.Start();

                Task.Delay(1000).ContinueWith(t =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        action.Hide();
                    });
                });

                switch (StartupBehaviourLevel)
                {
                    case 0:
                        break;
                    case 1:
                        Application.Current.MainWindow.WindowState = WindowState.Minimized;
                        break;
                    case 2:
                        Application.Current.Shutdown();
                        break;
                }
            }
            catch
            {
                MessageBox.Show("Ein Startfehler ist aufgetreten!");
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
            InstalledInstance selectedInstance = profileSelect.SelectedItem as InstalledInstance;
            if (selectedInstance != null)
            {
                Properties.Settings.Default.LastSelected = selectedInstance.Guid;
                Properties.Settings.Default.Save();

                if (selectedInstance.Servers != null && selectedInstance.Servers.Count() >= 1)
                {
                    servInfo.Visibility = Visibility.Visible;
                    serverSelect.SelectedIndex = 0;
                }
                else
                {
                    servInfo.Visibility = Visibility.Collapsed;
                }
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

        private void serverSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (serverSelect.SelectedItem == null) return;
            Server server = (Server)serverSelect.SelectedItem;
            currentServerImg.Source = new BitmapImage(new Uri(@"https://tcraft.link/tclauncher/api/plugins/server-tool/GetAccent.php?literal&url=" + server.IP));
        }
    }
}
