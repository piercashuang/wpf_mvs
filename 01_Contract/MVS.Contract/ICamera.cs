namespace MVS.Contract {
    public interface ICamera {
        string Brand { get; }
        bool Open(string sn);
        void StartGrabbing();
        void StopGrabbing();
    }
    public enum CameraStatus { Idle, Running, Error, Offline }
}
