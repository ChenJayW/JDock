using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JDock {

    public class DockItem {
        public double ItemWidth { get; set; } = 45;
        public double ItemMargin { get; set; } = 10;   // 每个图标之间的间距
    }

    public class MainViewModelDC : INotifyPropertyChanged {

        // 每个图标的宽度
        public double _windowWidth = 45;
      
        public double WindowWidth {
            get => _windowWidth; set {
                _totalWidth = value;
                OnPropertyChanged();
            }
        }
      
        private double _totalWidth;

        public ObservableCollection<ListItem> Items;

        public MainViewModelDC(ObservableCollection<ListItem> Items) {
            this.Items = Items;
            // 监听数据源的更新
            Items.CollectionChanged += OnDockItemsChanged;
        }

        public double TotalWidth {
            get => _totalWidth;
            set {
                _totalWidth = value;
                OnPropertyChanged();
            }
        }

        // 当 DockItems 更新时重新计算宽度
        private void OnDockItemsChanged(object sender, NotifyCollectionChangedEventArgs e) {
            UpdateTotalWidth();
        }

        // 计算 ItemsControl 的总宽度
        private void UpdateTotalWidth() {
            //TotalWidth = Items.Count * (ItemWidth + (ItemMargin * 2)); // 减去最后一个多余的间距
        }

        #region INotifyPropertyChanged 实现
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
