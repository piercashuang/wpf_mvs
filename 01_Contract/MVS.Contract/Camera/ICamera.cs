namespace MVS.Contract.Camera {
    public interface ICamera {
        string Brand { get; }
        bool Open(string sn);
        void StartGrabbing();
        void StopGrabbing();
    }
   
}
