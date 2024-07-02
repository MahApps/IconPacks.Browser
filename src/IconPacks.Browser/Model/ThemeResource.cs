// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using ControlzEx.Theming;
using IconPacks.Browser.ViewModels;
using JetBrains.Annotations;

namespace IconPacks.Browser.Model
{
    /// <summary>
    /// Clas for one single resource color in light and dark
    /// </summary>
    public class ThemeResource : ViewModelBase
    {
        private bool isHidden = false;

        public ThemeResource(Theme theme, LibraryTheme libraryTheme, ResourceDictionary resourceDictionary, DictionaryEntry dictionaryEntryDark, DictionaryEntry dictionaryEntryLight)
            : this(theme, libraryTheme, resourceDictionary, dictionaryEntryDark.Key.ToString(), dictionaryEntryDark.Value, dictionaryEntryLight.Value)
        {
        }

        public ThemeResource(Theme theme, LibraryTheme libraryTheme, ResourceDictionary resourceDictionary, string? key, object? valueDark, object? valueLight)
        {
            this.Theme = theme;
            this.LibraryTheme = libraryTheme;

            this.Source = (resourceDictionary.Source?.ToString() ?? "Runtime").ToLower();
            this.Source = CultureInfo.InstalledUICulture.TextInfo.ToTitleCase(this.Source)
                                     .Replace("Pack", "pack")
                                     .Replace("Application", "application")
                                     .Replace("Xaml", "xaml");

            this.Key = key;

            this.ValueLight = valueLight switch
            {
                Color color => new SolidColorBrush(color),
                Brush brush => brush,
                _ => null
            };

            this.AlphaLight = valueLight switch
            {
                Color color => color.A,
                SolidColorBrush brush => brush.Color.A,
                LinearGradientBrush brush => brush.GradientStops[0].Color.A,
                _ => 255
            };

            this.StringValueLight = valueLight?.ToString();

            this.ValueDark = valueDark switch
            {
                Color color => new SolidColorBrush(color),
                Brush brush => brush,
                _ => null
            };

            this.AlphaDark = valueDark switch
            {
                Color color => color.A,
                SolidColorBrush brush => brush.Color.A,
                LinearGradientBrush brush => brush.GradientStops[0].Color.A,
                _ => 255
            };

            this.StringValueDark = valueDark?.ToString();
        }

        /// <summary>
        /// The current selected theme. Switch in Settings of App
        /// </summary>
        public Theme Theme { get; }

        /// <summary>
        /// The current selected theme. Switch in Settings of App
        /// </summary>
        public LibraryTheme LibraryTheme { get; }

        /// <summary>
        /// Source of this theme
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// Key of this resource
        /// </summary>
        public string? Key { get; }

        /// <summary>
        /// The light color of this resource
        /// </summary>
        public Brush? ValueLight { get; }
        /// <summary>
        /// The alpha value of this light color
        /// </summary>
        public int AlphaLight { get; } = 0;
        /// <summary>
        /// The light color as hex string
        /// </summary>
        public string? StringValueLight { get; }
        /// <summary>
        /// The dark color
        /// </summary>
        public Brush? ValueDark { get; }
        /// <summary>
        /// Alpha channel of dark color
        /// </summary>
        public int AlphaDark { get; } = 0;
        /// <summary>
        /// the dark color as hex string
        /// </summary>
        public string? StringValueDark { get; }
        /// <summary>
        /// when tru, don't show in grid
        /// </summary>
        public bool IsHidden
        {
            get => isHidden; set
            {
                if (Set(ref isHidden, value))
                {

                }
            }
        }

        /// <summary>
        /// Filter like it's done in icons
        /// </summary>
        /// <param name="filterText"></param>
        /// <returns></returns>
        public bool CheckFilter(string filterText)
        {
            bool RetVal = true;
            if (!string.IsNullOrWhiteSpace(filterText))
            {
                var filterSubStrings = filterText.Split(new[] { '+', ',', ';', '&' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var filterSubString in filterSubStrings)
                {
                    var filterOrSubStrings = filterSubString.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                    var isInName = filterOrSubStrings.Any(x => Key.IndexOf(x.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0);

                    if (!isInName) RetVal = false;
                }
            }
            return RetVal;
        }

    }
}