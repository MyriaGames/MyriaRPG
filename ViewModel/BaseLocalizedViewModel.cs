using System.ComponentModel;
using System.Reflection;
using System.Windows.Threading;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Model;

namespace Myria.Wpf.ViewModel
{

    public abstract class BaseLocalizedViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected BaseLocalizedViewModel()
        {
            // Initial fill
            RefreshLocalizedProperties();

            // Live updates
            Localization.LanguageChanged += OnLanguageChanged;
        }

        ~BaseLocalizedViewModel()
        {
            Localization.LanguageChanged -= OnLanguageChanged;
        }

        protected virtual void OnLanguageChanged(object? sender, EventArgs e)
        {
            // If you have a dispatcher, marshal back to UI thread (optional, safe):
            Dispatcher.CurrentDispatcher.Invoke(RefreshLocalizedProperties);
        }

        protected void RefreshLocalizedProperties()
        {
            foreach (var prop in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.PropertyType != typeof(string) || !prop.CanWrite) continue;

                var attr = prop.GetCustomAttribute<LocalizedKeyAttribute>();
                if (attr is null) continue;

                var value = Localization.T(attr.Key); // your method :contentReference[oaicite:1]{index=1}
                prop.SetValue(this, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop.Name));
            }

        }

    }

}
