using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AsyncAwaitBestPractices.MVVM;
using JetBrains.Annotations;
using MahApps.Metro.Controls.Dialogs;

namespace IconPacks.Browser.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        protected readonly IDialogCoordinator dialogCoordinator = DialogCoordinator.Instance;

        public ViewModelBase()
        {
            OpenUrlCommand = new AsyncCommand<string>(OpenUrlLink, x => !string.IsNullOrWhiteSpace(x as string));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public IAsyncCommand<string> OpenUrlCommand { get; }

        protected async Task OpenUrlLink(string link)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = link ?? throw new ArgumentNullException(nameof(link)),
                    // UseShellExecute is default to false on .NET Core while true on .NET Framework.
                    // Only this value is set to true, the url link can be opened.
                    UseShellExecute = true,
                });
            }
            catch (Exception e)
            {
                await DialogCoordinator.Instance.ShowMessageAsync(this, "Error", e.Message);
            }
        }

        protected bool Set<T>(ref T field, T newValue = default(T), [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }

            field = newValue;

            OnPropertyChanged(propertyName);

            return true;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}