﻿using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Windows;
using TCLauncher.Models;
using TCLauncher.MVVM.Windows;

namespace TCLauncher.Core
{
    // TODO: Make it more universal / useful
    /// <summary>
    /// A utility class for handling message box operations.
    /// </summary>
    public static class MessageBoxUtils
    {
        /// <summary>
        /// Displays a message box with the specified text and title.
        /// </summary>
        /// <param name="text">The text to display in the message box.</param>
        /// <param name="title">The title of the message box. Default is an empty string.</param>
        public static void ShowToVoid(string text, string title = "")
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var dialog = string.IsNullOrEmpty(title)
                    ? new CustomButtonDialog(DialogButtons.Ok, text, null)
                    : new CustomButtonDialog(DialogButtons.Ok, title, text);
                dialog.ShowDialog();
            }));
        }

        /// <summary>
        /// Displays a message box with the specified text and title. (legacy)
        /// </summary>
        /// <param name="text">The text to display in the message box.</param>
        /// <param name="title">The title of the message box. Default is an empty string.</param>
        public static void ShowToVoidLegacy(string text, string title = "")
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(text, title)));
        }

        public static async Task<string> AskForString(string message, bool isOptional = false)
        {
            while (true)
            {
                var dialog = new CustomInputDialog(message + (isOptional ? " (optional)" : ""))
                {
                    Owner = App.MainWin
                };
                dialog.Show();
                if (!await dialog.Result) return null;
                var input = dialog.ResponseText;
                if (!string.IsNullOrEmpty(input) || isOptional)
                {
                    return input;
                }
                MessageBox.Show("Ungültige Eingabe. Bitte versuchen Sie es erneut.", "Fehler", MessageBoxButton.OK);
            }
        }

        public static async Task<bool?> AskForBool(string message, bool isOptional = false)
        {
            while (true)
            {
                var dialog = new CustomInputDialog(message + (isOptional ? " (optional)" : ""))
                {
                    Owner = App.MainWin
                };
                dialog.Show();
                if (!await dialog.Result) return null;
                var input = dialog.ResponseText;
                if (bool.TryParse(input, out bool result) || (isOptional && string.IsNullOrEmpty(input)))
                {
                    return result;
                }
                MessageBox.Show("Ungültige Eingabe. Bitte geben Sie 'true' oder 'false' ein.", "Fehler", MessageBoxButton.OK);
            }
        }

        public static async Task<int?> AskForInt(string message, bool isOptional = false)
        {
            while (true)
            {
                var dialog = new CustomInputDialog(message + (isOptional ? " (optional)" : ""))
                {
                    Owner = App.MainWin
                };
                dialog.Show();
                if (!await dialog.Result) return null;
                var input = dialog.ResponseText;
                if (int.TryParse(input, out int result) || (isOptional && string.IsNullOrEmpty(input)))
                {
                    return result;
                }
                MessageBox.Show("Ungültige Eingabe. Bitte geben Sie eine ganze Zahl ein.", "Fehler", MessageBoxButton.OK);
            }
        }

        public static async Task<T> AskForJson<T>(string message, bool isOptional = false)
        {
            while (true)
            {
                var dialog = new CustomInputDialog(message + (isOptional ? " (optional)" : ""))
                {
                    Owner = App.MainWin
                };
                dialog.Show();
                if (!await dialog.Result) return default(T);
                var input = dialog.ResponseText;
                try
                {
                    return JsonConvert.DeserializeObject<T>(input);
                }
                catch
                {
                    if (isOptional && string.IsNullOrEmpty(input))
                    {
                        return default(T);
                    }
                    MessageBox.Show("Ungültige Eingabe. Bitte geben Sie gültigen JSON-Text ein.", "Fehler", MessageBoxButton.OK);
                }
            }
        }
    }
}
