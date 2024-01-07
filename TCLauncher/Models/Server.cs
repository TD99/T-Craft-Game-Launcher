﻿namespace TCLauncher.Models
{
    public class Server
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string ThumbnailURL { get; set; }

        public Server(string name, string address, string thumbnailURL)
        {
            Name = name;
            Address = address;
            ThumbnailURL = thumbnailURL;
        }

        public Server(string name, string address)
        {
            Name = name;
            Address = address;
            ThumbnailURL = "/Images/nothumb.png";
        }

        public Server()
        {
            ThumbnailURL = "/Images/nothumb.png";
        }
    }
}
