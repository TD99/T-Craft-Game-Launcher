﻿using System.Diagnostics;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Windows;
using TCLauncher.Models;
using TCLauncher.Properties;

namespace TCLauncher.Core
{
    public static class InternetUtils
    {
        public static long PingPage(string url)
        {
            try
            {
                var pingSender = new Ping();
                var reply = pingSender.Send(RemoveProtocol(url));

                if (reply != null && reply.Status == IPStatus.Success)
                    return reply.RoundtripTime;
            }
            catch
            {
                // TODO: Don't ignore
            }

            return -1;
        }

        public static bool ReachPage(string url)
        {
            string[] protocols = { "http://", "https://" };
            url = RemoveProtocol(url, protocols);

            foreach (var protocol in protocols)
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(protocol + url);
                    request.AllowAutoRedirect = false;
                    request.Method = "HEAD";
                    request.GetResponse();
                    return true;
                }
                catch
                {
                    // ignore the exception and try the next protocol
                }
            }
            return false;
        }

        /// <summary>
        /// Removes the protocol (e.g., "http://", "https://") from the start of the specified URL.
        /// </summary>
        /// <param name="url">The URL from which to remove the protocol.</param>
        /// <param name="protocols">An optional array of protocols to remove from the URL. If not specified, the default protocols "http://" and "https://" are used.</param>
        /// <returns>The URL with the protocol removed. If the URL does not start with any of the specified protocols, the original URL is returned.</returns>
        public static string RemoveProtocol(string url, string[] protocols = null)
        {
            if (protocols == null)
            {
                protocols = new[] { "http://", "https://" };
            }

            foreach (var i in protocols)
            {
                if (url.StartsWith(i))
                {
                    return url.Substring(i.Length);
                }
            }

            return url;
        }

        /// <summary>
        /// Checks if the provided URL starts with any of the given protocols.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <param name="protocols">An array of protocols to check against. Defaults to http:// and https:// if not provided.</param>
        /// <returns>Returns true if the URL starts with any of the provided protocols, false otherwise.</returns>
        public static bool HasProtocol(string url, string[] protocols = null)
        {
            if (protocols == null)
            {
                protocols = new[] { "http://", "https://" };
            }

            foreach (var i in protocols)
            {
                if (url.StartsWith(i))
                {
                    return true;
                }
            }

            return false;
        }

        public static McServerAddress GetMcServerAddress(string ipPortPair)
        {
            var mcServerAddress = new McServerAddress();

            if (ipPortPair == null) return mcServerAddress;

            var split = ipPortPair.Split(':');

            switch (split.Length)
            {
                case 1:
                {
                    mcServerAddress.IP = split[0];
                    break;
                }
                case 2:
                {
                    mcServerAddress.IP = split[0];
                    mcServerAddress.Port = int.Parse(split[1]);
                    break;
                }
            }

            return mcServerAddress;
        }


        /// <summary>
        /// Checks if the given string is a valid URL.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <returns>True if the URL is valid, false otherwise.</returns>
        public static bool CheckIsValidUrl(string url)
        {
            // c# check
            var isValidUrl = Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                             && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            // regex check
            const string pattern = @"^(http|https)://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(/\S*)?$";
            var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            return isValidUrl /*&& regex.IsMatch(url)*/;
        }

        /// <summary>
        /// Opens the given URL in the default web browser.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        public static void OpenUrlInWebBrowser(string url)
        {
            if (CheckIsValidUrl(url))
            {
                try
                {
                    Process.Start(url);
                }
                catch (Exception ex)
                {
                    // Handle the exception if the browser was not correctly launched.
                    MessageBox.Show($"An error occurred: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show(Languages.sandbox_security_message_blocked + " (" + url + ")", Languages.tclauncher_security);
            }
        }
    }
}
