using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace RenomearTudo.App.Infrastructure
{
    /// <summary>
    /// ObservableCollection com operações em lote para evitar centenas ou milhares
    /// de atualizações de layout ao adicionar/reordenar arquivos.
    /// </summary>
    public sealed class BulkObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotifications;

        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) return;

            _suppressNotifications = true;
            try
            {
                foreach (var item in items)
                    Items.Add(item);
            }
            finally
            {
                _suppressNotifications = false;
            }

            RaiseReset();
        }

        public void ResetWith(IEnumerable<T> items)
        {
            _suppressNotifications = true;
            try
            {
                Items.Clear();
                if (items != null)
                {
                    foreach (var item in items)
                        Items.Add(item);
                }
            }
            finally
            {
                _suppressNotifications = false;
            }

            RaiseReset();
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotifications)
                base.OnCollectionChanged(e);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!_suppressNotifications)
                base.OnPropertyChanged(e);
        }

        private void RaiseReset()
        {
            base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            base.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
