using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace JDock {
    /// <summary>
    /// Interaction logic for HideButton.xaml
    /// </summary>
    public partial class HideButton : Window {
        private readonly Window _mainWindow;

        /*
*  窗口大小系数
*      百分比
*/
        public float heightCoefficient = 0.004f;
        public float widthCoefficient = 0.12f;

        public HideButton(Window mainWindow) {
            InitializeComponent();

            _mainWindow = mainWindow; // 保存主窗口引用

            bottom_border.Height = (SystemParameters.PrimaryScreenHeight * heightCoefficient);
            bottom_border.Width = (SystemParameters.PrimaryScreenWidth * widthCoefficient);
            this.Width = bottom_border.Width;
            this.Height = bottom_border.Height;

        }

        private void RestoreMainWindow(object sender, MouseButtonEventArgs e) {
            // 先重置主窗口的位置和透明度
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            //_mainWindow.Top = screenHeight; // 放置在屏幕外
            _mainWindow.Opacity = 0; // 完全透明
            _mainWindow.Show();

            // 创建滑入动画（从下往上）
            var slideUpAnimation = new DoubleAnimation {
                From = screenHeight - _mainWindow.Top, // 从屏幕外滑入
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase()
            };

            // 创建透明度渐入动画
            var fadeInAnimation = new DoubleAnimation {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(500)
            };

            // 应用动画
            var mainWindowTransform = (TranslateTransform)_mainWindow.RenderTransform;
            mainWindowTransform.BeginAnimation(TranslateTransform.YProperty, slideUpAnimation);
            _mainWindow.BeginAnimation(OpacityProperty, fadeInAnimation);

            // 关闭条形按钮窗口
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            // 窗口加载时，设置在屏幕底部居中
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            this.Left = (screenWidth - this.Width) / 2; // 水平居中
            this.Top = screenHeight - this.Height;      // 位于底部


        }
    }
}
