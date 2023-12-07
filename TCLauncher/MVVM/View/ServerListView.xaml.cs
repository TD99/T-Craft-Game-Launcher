﻿using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TCLauncher.Core;
using TCLauncher.Models;
using TCLauncher.MVVM.ViewModel;
using TCLauncher.MVVM.Windows;

namespace TCLauncher.MVVM.View
{
    /// <summary>
    /// Interaction logic for ServerListView.xaml
    /// </summary>
    public partial class ServerListView : UserControl
    {
        private Instance current { get; set; }

        public ServerListView()
        {
            InitializeComponent();
        }

        private void ServerItem_Clicked(object sender, MouseButtonEventArgs e)
        {
            Border border = (Border)sender;
            Instance instance = (Instance)border.DataContext;
            itemFocusBanner.Source = new BitmapImage(new Uri(instance.ThumbnailURL, UriKind.RelativeOrAbsolute));
            itemFocusName.Text = instance.DisplayName;
            itemFocusPatch.Text = $"{instance.GetCurrentPatch()?.Name}@{instance.Version}";
            itemFocusPackage.Text = "ch.tcraft." + instance.Name;
            itemFocusType.Text = instance.Type;
            itemFocusMCVersion.Text = instance.McVersion;

            string requiredRam = (instance.Requirements == null) ? "?" : instance.Requirements["ram"].ToString() ?? "?";
            string totalPhysicalMemory = SystemInfoUtils.GetTotalPhysicalMemoryInGb().ToString();
            itemFocusRam.Text = $"{requiredRam} GB / {totalPhysicalMemory} GB";

            specialFocusBtn.Content = (instance.Is_Installed) ? "Deinstallieren" : "Installieren";
            openFolderBtn.Visibility = (instance.Is_Installed) ? Visibility.Visible : Visibility.Collapsed;
            reconfigDef.Visibility = (instance.Is_Installed) ? Visibility.Visible : Visibility.Collapsed;
            editConfig.Visibility = (instance.Is_Installed) ? Visibility.Visible : Visibility.Collapsed;
            itemFocusMCWorkingDirDesc.Children.Clear();

            current = instance;

            if (instance.WorkingDirDesc != null)
            {
                foreach (string key in instance.WorkingDirDesc.Keys)
                {
                    TextBlock keyTextBlock = new TextBlock
                    {
                        Text = key,
                        Foreground = Brushes.White,
                        FontSize = 25
                    };
                    itemFocusMCWorkingDirDesc.Children.Add(keyTextBlock);

                    foreach (string description in instance.WorkingDirDesc[key])
                    {
                        TextBlock descTextBlock = new TextBlock
                        {
                            Text = description,
                            Foreground = Brushes.White,
                            FontSize = 16
                        };
                        itemFocusMCWorkingDirDesc.Children.Add(descTextBlock);
                    }
                }
            }

            itemFocus.Visibility = Visibility.Visible;
        }

        private void closeFocusBtn_Click(object sender, RoutedEventArgs e)
        {
            itemFocus.Visibility = Visibility.Collapsed;
            itemFocusBanner.Source = new BitmapImage(new Uri("/Images/nothumb.png", UriKind.RelativeOrAbsolute));
            itemFocusName.Text = "";
            itemFocusPatch.Text = "";
            itemFocusPackage.Text = "";
            itemFocusType.Text = "";
            itemFocusMCVersion.Text = "";
            specialFocusBtn.Content = "Aktion";
            itemFocusMCWorkingDirDesc.Children.Clear();

            this.DataContext = new ServerListViewModel();

            current = null;
        }

        private void forceUninstallBtn_Click(object sender, RoutedEventArgs e)
        {
            uninstallInstance(current);
        }

        private void uninstallInstance(Instance instance, bool force = false)
        {
            try
            {
                string instanceFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL", "Instances", current.Guid.ToString());
                if (!Directory.Exists(instanceFolder))
                {
                    MessageBox.Show("Es wurden keine Daten gefunden!", "Instanz löschen");
                    return;
                }

                if (force)
                {
                    Directory.Delete(instanceFolder, true);
                    instance.Is_Installed = false;
                    return;
                }

                MessageBoxResult result = MessageBox.Show("Willst du die Instanz wirklich löschen?", "Instanz löschen", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    Directory.Delete(instanceFolder, true);
                    instance.Is_Installed = false;

                    string appPath = Process.GetCurrentProcess().MainModule.FileName;
                    Process.Start(appPath, $"--uninstallSuccess {instance.DisplayName}");
                    Application.Current.Shutdown();
                }
            }
            catch
            {
                MessageBox.Show("Ein Fehler ist aufgetreten.");
            }
        }

        private async void installInstance(Instance instance)
        {
            string instanceFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL", "Instances", instance.Guid.ToString());
            string installFolder = System.IO.Path.Combine(instanceFolder, "data");
            string payloadFile = System.IO.Path.Combine(instanceFolder, "base.zip");

            try
            {
                Directory.CreateDirectory(instanceFolder);
            }
            catch
            {
                MessageBox.Show("Fehler beim Erstellen der Instanz!");
            }

            try
            {
                if (!URLExists(instance.WorkingDirZipURL))
                {
                    MessageBox.Show("Ein Fehler beim Holen der Abhängigkeiten ist aufgetreten.");
                    return;
                }

                Directory.CreateDirectory(installFolder);

                if (instance.UsePatch != true)
                {
                    using (var client = new WebClient())
                    {
                        string fileSize = await GetFileSizeAsync(instance.WorkingDirZipURL);

                        ActionWindow action = new ActionWindow($"Installieren des Pakets 'ch.tcraft.{current.Name}'\nGrösse: {fileSize}\nInfo: Dies kann einige Zeit in Anspruch nehmen!");
                        action.Show();

                        client.DownloadProgressChanged += (sender, e) =>
                        {
                            action.percent = e.ProgressPercentage;
                        };

                        action.Closed += (sender, e) =>
                        {
                            client.CancelAsync();
                        };

                        try
                        {
                            await client.DownloadFileTaskAsync(new Uri(instance.WorkingDirZipURL), payloadFile);
                        }
                        catch
                        {
                            MessageBox.Show("Download abgebrochen!");
                            uninstallInstance(current, true);
                            action.Close();
                            return;
                        }

                        action.Close();
                    }

                    ActionWindow action2 = new ActionWindow($"Konfigurieren des Pakets 'ch.tcraft.{current.Name}'");
                    action2.Show();

                    await Task.Run(() => ZipFile.ExtractToDirectory(payloadFile, installFolder));

                    action2.Close();

                    ActionWindow action3 = new ActionWindow($"Aufräumen des Pakets 'ch.tcraft.{current.Name}'");
                    action3.Show();

                    try
                    {
                        await Task.Run(() =>
                        {
                            File.Delete(payloadFile);
                        });
                    }
                    catch { }

                    action3.Close();
                }
                else
                {
                    foreach (var patch in instance.Patches.OrderBy(p => p.ID))
                    {
                        using (var client = new WebClient())
                        {
                            string fileSize = await GetFileSizeAsync(patch.URL);

                            ActionWindow action = new ActionWindow($"Installieren des Pakets 'ch.tcraft.{current.Name}@{patch.Name}:{patch.ID}'\nGrösse: {fileSize}\nInfo: Dies kann einige Zeit in Anspruch nehmen!");
                            action.Show();

                            client.DownloadProgressChanged += (sender, e) =>
                            {
                                action.percent = e.ProgressPercentage;
                            };

                            action.Closed += (sender, e) =>
                            {
                                client.CancelAsync();
                            };

                            bool err = false;

                            try
                            {
                                await client.DownloadFileTaskAsync(new Uri(patch.URL), payloadFile);
                            }
                            catch
                            {
                                MessageBox.Show("Paketfehler, die Installation ist beschädigt!");
                                err = true;
                            }

                            if (!err)
                            {
                                action.Close();

                                ActionWindow action2 = new ActionWindow($"Konfigurieren des Pakets 'ch.tcraft.{current.Name}@{patch.Name}:{patch.ID}'");
                                action2.Show();

                                await Task.Run(() => ZipFile.ExtractToDirectory(payloadFile, installFolder));

                                action2.Close();

                                ActionWindow action3 = new ActionWindow($"Aufräumen des Pakets 'ch.tcraft.{current.Name}@{patch.Name}:{patch.ID}'");
                                action3.Show();

                                try
                                {
                                    await Task.Run(() =>
                                    {
                                        File.Delete(payloadFile);
                                    });
                                }
                                catch { }

                                action3.Close();
                            }
                        }
                    }
                }

                string appPath = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(appPath, $"--installSuccess {instance.DisplayName}");
                Application.Current.Shutdown();
            }
            catch
            {
                MessageBox.Show("Ein Fehler beim Holen der Abhängigkeiten ist aufgetreten.");
            }

            reconfigure(instanceFolder, instance);

            if (Properties.Settings.Default.FirstTime)
            {
                Properties.Settings.Default.FirstTime = false;
                Properties.Settings.Default.Save();
            }
        }

        public async Task<string> GetFileSizeAsync(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        long fileSize = response.Content.Headers.ContentLength ?? 0;
                        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                        int order = 0;
                        while (fileSize >= 1024 && order < sizes.Length - 1)
                        {
                            fileSize = fileSize / 1024;
                            order++;
                        }
                        return string.Format("{0:0.##} {1}", fileSize, sizes[order]);
                    }
                }
            }
            catch { }

            return "Unbekannt";
        }

        private bool URLExists(string url)
        {
            bool result = true;

            WebRequest webRequest = WebRequest.Create(url);
            webRequest.Timeout = 1200;
            webRequest.Method = "HEAD";

            try
            {
                webRequest.GetResponse();
            }
            catch
            {
                result = false;
            }

            return result;
        }

        private void specialFocusBtn_Click(object sender, RoutedEventArgs e)
        {
            if (current.Is_Installed)
            {
                uninstallInstance(current);
            }
            else
            {
                installInstance(current);
            }

            specialFocusBtn.Content = (current.Is_Installed) ? "Deinstallieren" : "Installieren";
        }

        private void reconfigure(string instanceFolder, Instance instance)
        {
            try
            {
                instance.Is_Installed = true;

                string configFile = System.IO.Path.Combine(instanceFolder, "config.json");

                var json = JsonConvert.SerializeObject(instance);
                File.WriteAllText(configFile, json);
            }
            catch
            {
                MessageBox.Show("Ein Fehler bei der Neukonfiguration ist aufgetreten.");
            }
        }

        private void openFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            string dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL", "Instances", current.Guid.ToString(), "data");
            Process.Start("explorer.exe", dataFolder);
        }

        private async void reconfigDef_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string instanceFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL", "Instances", current.Guid.ToString());

                HttpClient _httpClient = new HttpClient();
                var response = await _httpClient.GetAsync(Properties.Settings.Default.DownloadMirror + "?guid=" + current.Guid.ToString());
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var instance = JsonConvert.DeserializeObject<ObservableCollection<Instance>>(content)[0];

                    reconfigure(instanceFolder, instance);

                    string appPath = Process.GetCurrentProcess().MainModule.FileName;
                    Process.Start(appPath, $"--updateSuccess {instance.DisplayName}");
                    Application.Current.Shutdown();

                    return;
                }
                MessageBox.Show($"Die Neukonfiguration von '{current.Name}' ist fehlgeschlagen.");
            }
            catch
            {
                MessageBox.Show($"Die Neukonfiguration von '{current.Name}' ist fehlgeschlagen.");
            }
        }

        private void editConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string instanceFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL", "Instances", current.Guid.ToString());
                string configFile = Path.Combine(instanceFolder, @"config.json");

                this.Cursor = Cursors.Wait;
                EditorWindow editorWindow = new EditorWindow(configFile, false);
                editorWindow.Show();
                this.Cursor = null;
            }
            catch
            {
                MessageBox.Show($"Die Konfiguration von '{current.Name}' ist fehlgeschlagen.");
            }
        }
    }
}