using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using MVS.Contract.Camera;

namespace MVS_WPF.ViewModels
{
    public class SidebarViewModel : INotifyPropertyChanged
    {
        // ================= 属性 =================
        private ObservableCollection<string> _cameraList;
        public ObservableCollection<string> CameraList
        {
            get { return _cameraList; }
            set
            {
                _cameraList = value;
                OnPropertyChanged();
            }
        }

        private ICamera _connectedCamera = null;

        private BitmapImage _displayImage;
        public BitmapImage DisplayImage
        {
            get { return _displayImage; }
            set
            {
                _displayImage = value;
                OnPropertyChanged();
            }
        }

        // ================= 命令 =================
        public ICommand ScanCamerasCommand { get; }
        public ICommand ConnectCameraCommand { get; }
        public ICommand DisconnectCameraCommand { get; }

        // ================= 构造函数 =================
        public SidebarViewModel()
        {
            CameraList = new ObservableCollection<string>();

            ScanCamerasCommand = new RelayCommand(ExecuteScanCameras);
            ConnectCameraCommand = new RelayCommand(ExecuteConnectCamera);
            DisconnectCameraCommand = new RelayCommand(ExecuteDisconnectCamera);
        }

        // ================= 方法 =================
        private async void ExecuteScanCameras(object parameter)
        {
            CameraList.Clear();
            CameraList.Add("扫描中，请稍候...");

            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    MVS.Infrastructure.CameraManager.Instance.ScanAllCameras();
                });

                var discoveredInfos = MVS.Infrastructure.CameraManager.Instance.GetAllDiscoveredCameraInfos();
                CameraList.Clear();

                if (discoveredInfos != null && System.Linq.Enumerable.Any(discoveredInfos))
                {
                    foreach (var info in discoveredInfos)
                    {
                        CameraList.Add(info.SerialNumber);
                    }
                }
                else
                {
                    CameraList.Add("未发现任何相机");
                }
            }
            catch (Exception ex)
            {
                CameraList.Clear();
                CameraList.Add("扫描异常...");
                System.Diagnostics.Debug.WriteLine($"相机扫描失败: {ex.Message}");
            }
        }

        private void ExecuteConnectCamera(object parameter)
        {
            string cameraSn = parameter as string;
            if (string.IsNullOrEmpty(cameraSn)) return;

            // 先断开之前的连接
            ExecuteDisconnectCamera(null);

            System.Diagnostics.Debug.WriteLine($"[Sidebar] 正在尝试连接相机: {cameraSn}");

            // 1. 获取相机实例
            _connectedCamera = MVS.Infrastructure.CameraManager.Instance.CreateAndConnectCamera(cameraSn);

            if (_connectedCamera != null)
            {
                // 2. 尝试打开相机，并接收返回的状态对象
                MVS.Contract.MvsStatus openStatus = _connectedCamera.Open();

                // 【核心修改】：使用 .IsOk 进行判断
                if (openStatus.IsOk)
                {
                    _connectedCamera.ImageGrabbed += OnCameraImageGrabbed;

                    // 3. 尝试取流
                    var grabStatus = _connectedCamera.StartGrabbing();
                    if (grabStatus.IsOk)
                    {
                        MessageBox.Show($"相机 {cameraSn} 连接并取流成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // 取流失败，显示具体错误信息
                        MessageBox.Show($"取流失败: {grabStatus.MessageCn}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // 打开失败，显示自定义状态类里的中文错误提示
                    MessageBox.Show($"相机打开失败: {openStatus.MessageCn}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("未能在系统中创建相机实例！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDisconnectCamera(object parameter)
        {
            if (_connectedCamera != null)
            {
                _connectedCamera.ImageGrabbed -= OnCameraImageGrabbed;
                _connectedCamera.StopGrabbing();
                _connectedCamera.Close();
                _connectedCamera = null;

                DisplayImage = null;
                System.Diagnostics.Debug.WriteLine("[Sidebar] 相机已断开");
            }
        }

        private void OnCameraImageGrabbed(object sender, Bitmap bmp)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    DisplayImage = BitmapToBitmapImage(bmp);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UI 刷新图像报错: {ex.Message}");
                }
            });
        }

        private BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }

        // ================= 接口实现 =================
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}