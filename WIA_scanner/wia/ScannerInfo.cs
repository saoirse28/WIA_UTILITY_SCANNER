using System;
using System.Linq;

namespace Wia
{
    public class ScannerInfo
    {

        public String Name { get; set; }
        public String Id { get; set; }
        public ScannerInfo(string name, string id)
        {
            this.Name = name;
            this.Id = id;
        }
        public override string ToString()
        {
            return this.Name;
        }
        public WIA.Device GetDevice()
        {
            WIA.DeviceManager manager = new WIA.DeviceManager();
            return manager.DeviceInfos.OfType<WIA.DeviceInfo>().FirstOrDefault(o => o.DeviceID == this.Id).Connect();
        }   
    }
}
