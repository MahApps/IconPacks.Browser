// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ControlzEx.Theming;
using IconPacks.Browser.Model;
using IconPacks.Browser.ViewModels;
using JetBrains.Annotations;

namespace IconPacks.Browser.ViewModels
{
    /// <summary>
    /// Viewmodel for Color Resources
    /// </summary>
    public class ThemeResourcesViewModel : ViewModelBase
    {
        private DispatcherTimer timer = new DispatcherTimer();
        private string header = "Colors";
        private string _filterTextColors;
        private string statusColorFilter;
        private int filterAlphaValue = 0;

        /// <summary>
        /// this are the resources of this theme
        /// </summary>
        public ObservableCollection<ThemeResource> ThemeResources { get; } = new ObservableCollection<ThemeResource>();

        public ThemeResourcesViewModel()
        {
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += Timer_Tick;
        }

        /// <summary>
        /// Disables the popup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            PopupIsOpen = false;
            this.OnPropertyChanged(nameof(PopupIsOpen));
            timer.Stop();
        }

        /// <summary>
        /// Theme changed==Updat colors
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void ThemeManager_ThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            UpdateThemeResources();
        }

        /// <summary>
        /// Show the current Theme
        /// </summary>
        public string Header
        {
            get => header; set
            {
                header = value;
                this.OnPropertyChanged(nameof(Header));
            }
        }

        /// <summary>
        /// Popup for doublick infomation
        /// </summary>
        public bool PopupIsOpen { get; set; }
        public object PopupPlacementTarget { get; set; }
        public string PopupText { get; set; }

        /// <summary>
        /// Only colors with alpha channel greater this value will be shown
        /// </summary>
        public int FilterAlphaValue
        {
            get => filterAlphaValue; set
            {
                if (Set(ref filterAlphaValue, value))
                {
                    Filter();
                }
            }
        }

        /// <summary>
        /// Search Textbox like it was for icons
        /// </summary>
        public string FilterTextColors
        {
            get => _filterTextColors;
            set
            {
                if (Set(ref _filterTextColors, value))
                {
                    Filter();
                }
            }
        }

        /// <summary>
        /// For Statusbar, count of colors visible
        /// </summary>
        public string StatusColorFilter
        {
            get => statusColorFilter; set
            {
                Set(ref statusColorFilter, value);
            }
        }

        /// <summary>
        /// Filter the resources, IsHidden=true means not visible
        /// </summary>
        private void Filter()
        {
            var colorCount = 0;
            foreach (var item in ThemeResources)
            {
                item.IsHidden = !item.CheckFilter(_filterTextColors) || item.AlphaLight< filterAlphaValue || item.AlphaDark< filterAlphaValue;
                if (!item.IsHidden) colorCount += 1;
            }
            var filtered = (colorCount != ThemeResources.Count) ? " (filtered)" : "";
            StatusColorFilter = $"{Header} with {colorCount} Colors{filtered}";
        }

        /// <summary>
        /// Forwarded from code behind. Sorry. MVVM to complicated for me :)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DataGridCell_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender != null && sender is DataGridCell cell && cell.DataContext != null && cell.DataContext is ThemeResource tr)
            {
                PopupText = "";
                if (cell.Column.DisplayIndex == 0)
                {
                    System.Windows.Clipboard.SetText(tr.Key);
                    PopupText = $"Resourcename \"{tr.Key}\" copied to clipboard";
                }
                else if (cell.Column.DisplayIndex == 1 || cell.Column.DisplayIndex == 2)
                {
                    System.Windows.Clipboard.SetText(tr.StringValueLight);
                    PopupText = $"hex-value for light theme \"{tr.StringValueLight}\" copied to clipboard";
                }
                else if (cell.Column.DisplayIndex == 3 || cell.Column.DisplayIndex == 4)
                {
                    System.Windows.Clipboard.SetText(tr.StringValueDark);
                    PopupText = $"hex-value for dark theme \"{tr.StringValueDark}\" copied to clipboard";
                }


                if (!string.IsNullOrEmpty(PopupText))
                {
                    PopupPlacementTarget = sender;
                    this.OnPropertyChanged(nameof(PopupPlacementTarget));
                    this.OnPropertyChanged(nameof(PopupText));

                    PopupIsOpen = true;
                    this.OnPropertyChanged(nameof(PopupIsOpen));

                    timer.Start();
                }

            }
        }

        /// <summary>
        /// When Theme Changed, update the colos
        /// </summary>
        public void UpdateThemeResources()
        {
            this.ThemeResources.Clear();

            if (Application.Current.MainWindow != null)
            {
                var themeOriginal = ThemeManager.Current.DetectTheme(Application.Current.MainWindow);
                string Accent = "";
                foreach (var t in ThemeManager.Current.Themes)
                {
                    if (t.PrimaryAccentColor.Equals(themeOriginal.PrimaryAccentColor))
                    {
                        Accent = t.ColorScheme;
                        break;
                    }
                }

                Header = $"App color: {Accent}";


                if (themeOriginal is not null)
                {
                    var themeDark = ThemeManager.Current.GetTheme("Dark", Accent);
                    var themeLight = ThemeManager.Current.GetTheme("Light", Accent);

                    if (themeDark != null && themeLight != null)
                    {
                        var libraryThemeDark = themeDark.LibraryThemes.FirstOrDefault(x => x.Origin == "MahApps.Metro");
                        var resourceDictionaryDark = libraryThemeDark?.Resources.MergedDictionaries.FirstOrDefault();

                        var libraryThemeLight = themeLight.LibraryThemes.FirstOrDefault(x => x.Origin == "MahApps.Metro");
                        var resourceDictionaryLight = libraryThemeLight?.Resources.MergedDictionaries.FirstOrDefault();

                        if (resourceDictionaryDark != null && resourceDictionaryLight != null)
                        {
                            var colorCount = 0;
                            foreach (var dictionaryEntryDark in resourceDictionaryDark.OfType<DictionaryEntry>())
                            {
                                var dictionaryEntryLight = dictionaryEntryDark.Value;

                                if (resourceDictionaryLight.Contains(dictionaryEntryDark.Key)) dictionaryEntryLight = resourceDictionaryLight[dictionaryEntryDark.Key];
                                var tr = new ThemeResource(themeDark, libraryThemeDark!, resourceDictionaryDark, dictionaryEntryDark.Key.ToString(), dictionaryEntryDark.Value, dictionaryEntryLight);
                                //tr.isHidden = OnlyFullAlpha && (!tr.FullAlpha || !tr.FullAlphaDark);

                                if (!tr.IsHidden) colorCount += 1;
                                this.ThemeResources.Add(tr);
                            }
                            FilterTextColors = "";
                        }
                    }

                }

            }
        }
    }
}