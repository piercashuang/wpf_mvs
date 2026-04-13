using System.ComponentModel;
using System.Runtime.CompilerServices;
using MVS.Infrastructure;
using MVS.Contract.Camera;
using MVS.Contract; // 确保引入状态命名空间

namespace MVS_WPF.ViewModels
{
    public class CameraPropertiesViewModel : INotifyPropertyChanged
    {
        private string _currentCameraSn;
        private bool _isSyncing = false;

        private double _exposureTime = 2000.0;
        public double ExposureTime
        {
            get => _exposureTime;
            set
            {
                _exposureTime = value;
                OnPropertyChanged();
                if (!_isSyncing && !string.IsNullOrEmpty(_currentCameraSn))
                {
                    var camera = CameraManager.Instance.GetActiveCamera(_currentCameraSn);
                    camera?.SetEnumValueByString("ExposureAuto", "Off"); // 改参数前关自动
                    camera?.SetFloatValue("ExposureTime", (float)value);
                }
            }
        }

        private double _gain = 0.0;
        public double Gain
        {
            get => _gain;
            set
            {
                _gain = value;
                OnPropertyChanged();
                if (!_isSyncing && !string.IsNullOrEmpty(_currentCameraSn))
                {
                    var camera = CameraManager.Instance.GetActiveCamera(_currentCameraSn);
                    camera?.SetFloatValue("Gain", (float)value);
                }
            }
        }

        private int _width = 1920;
        public int Width
        {
            get => _width;
            set
            {
                _width = value;
                OnPropertyChanged();
                if (!_isSyncing && !string.IsNullOrEmpty(_currentCameraSn))
                    CameraManager.Instance.GetActiveCamera(_currentCameraSn)?.SetIntValue("Width", value);
            }
        }

        private int _height = 1080;
        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                OnPropertyChanged();
                if (!_isSyncing && !string.IsNullOrEmpty(_currentCameraSn))
                    CameraManager.Instance.GetActiveCamera(_currentCameraSn)?.SetIntValue("Height", value);
            }
        }

        private bool _isAutoExposure = false;
        public bool IsAutoExposure
        {
            get => _isAutoExposure;
            set
            {
                _isAutoExposure = value;
                OnPropertyChanged();
                if (!_isSyncing && !string.IsNullOrEmpty(_currentCameraSn))
                {
                    var camera = CameraManager.Instance.GetActiveCamera(_currentCameraSn);
                    // 【修正点】：使用之前在 ICamera 增加的 ByString 方法
                    camera?.SetEnumValueByString("ExposureAuto", value ? "Continuous" : "Off");
                }
            }
        }

        public void LoadParametersFromCamera(string cameraSn)
        {
            _currentCameraSn = cameraSn;
            var camera = CameraManager.Instance.GetActiveCamera(cameraSn);
            if (camera == null) return;

            _isSyncing = true;
            try
            {
                // 【核心修正】：判断 status.IsOk
                var stExp = camera.GetFloatValue("ExposureTime", out float exp);
                if (stExp.IsOk) this.ExposureTime = exp;

                var stGain = camera.GetFloatValue("Gain", out float gain);
                if (stGain.IsOk) this.Gain = gain;

                var stW = camera.GetIntValue("Width", out long w);
                if (stW.IsOk) this.Width = (int)w;

                var stH = camera.GetIntValue("Height", out long h);
                if (stH.IsOk) this.Height = (int)h;
            }
            finally
            {
                _isSyncing = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}