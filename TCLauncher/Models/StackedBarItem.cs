﻿using System.Windows.Media;

namespace TCLauncher.Models
{
    public class StackedBarItem
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public Color? Color { get; set; }
        public string Unit { get; set; }

        public StackedBarItem()
        {
        }

        public StackedBarItem(string name, double value, Color? color, string unit)
        {
            Name = name;
            Value = value;
            Color = color;
            Unit = unit;
        }
    }
}
