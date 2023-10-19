﻿using System.Windows.Shell;

namespace T_Craft_Game_Launcher.MVVM.Windows
{
    /// <summary>
    /// Interaction logic for ActionWindow.xaml
    /// </summary>
    public partial class ActionWindow
    {
        private TaskbarItemInfo taskbarInfo;
        private int _percent;
        private string _text;
        public int percent
        {
            get => _percent;
            set
            {
                _percent = value;
                percentText.Text = value.ToString();
                taskbarInfo.ProgressValue = value / 100.0;
            }
        }

        public string text
        {
            get => _text;
            set
            {
                _text = value;
                actionText.Text = value;
            }
        }

        public ActionWindow(string action = null)
        {
            InitializeComponent();
            text = action;
            taskbarInfo = new TaskbarItemInfo();
            taskbarInfo.ProgressState = TaskbarItemProgressState.Normal;
            taskbarInfo.ProgressValue = 0.0;
            this.TaskbarItemInfo = taskbarInfo;
        }
    }
}
