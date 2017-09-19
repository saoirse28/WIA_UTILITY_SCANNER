using Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using WIA;

namespace Wia
{
    public class ScanEngine
    {
        public bool SingleOnly { get; set; }
        public event EventHandler<ScanCompletedEventArgs> ScanCompleted;

        ConfigGet iConfig = new ConfigGet();
        const string WIA_SCAN_COLOR_MODE = "6146";
        const string WIA_HORIZONTAL_SCAN_RESOLUTION_DPI = "6147";
        const string WIA_VERTICAL_SCAN_RESOLUTION_DPI = "6148";
        const string WIA_HORIZONTAL_SCAN_START_PIXEL = "6149";
        const string WIA_VERTICAL_SCAN_START_PIXEL = "6150";
        const string WIA_HORIZONTAL_SCAN_SIZE_PIXELS = "6151";
        const string WIA_VERTICAL_SCAN_SIZE_PIXELS = "6152";
        const string WIA_SCAN_BRIGHTNESS_PERCENTS = "6154";
        const string WIA_SCAN_CONTRAST_PERCENTS = "6155";
        const string WIA_IMAGE_FORMAT = "4106";
        const string DEVICE_PROPERTY_DOCUMENT_HANDLING_CAPABILITIES_ID = "3086";
        const string DEVICE_PROPERTY_DOCUMENT_HANDLING_STATUS_ID = "3087";
        const string DEVICE_PROPERTY_DOCUMENT_HANDLING_SELECT_ID = "3088";
        const string DEVICE_PROPERTY_PAGES_ID = "3096";

        public void ScanAsync(ScannerInfo source)
        {
            if (source == null)
                return;

            Device wiaDevice = source.GetDevice();
            DeviceManager wiaManager = new DeviceManager();

            bool hasMorePages = true;
            while (hasMorePages)
            {
                try
                {
                    Item wiaItem = wiaDevice.Items[1];
                    SetWIAProperty(wiaItem.Properties, WIA_HORIZONTAL_SCAN_RESOLUTION_DPI, iConfig.iterfaceConfig.WIA_HORIZONTAL_SCAN_RESOLUTION_DPI);
                    SetWIAProperty(wiaItem.Properties, WIA_VERTICAL_SCAN_RESOLUTION_DPI, iConfig.iterfaceConfig.WIA_HORIZONTAL_SCAN_RESOLUTION_DPI);
                    SetWIAProperty(wiaItem.Properties, WIA_SCAN_BRIGHTNESS_PERCENTS, iConfig.iterfaceConfig.WIA_SCAN_BRIGHTNESS_PERCENTS);
                    SetWIAProperty(wiaItem.Properties, WIA_SCAN_COLOR_MODE, iConfig.iterfaceConfig.WIA_SCAN_COLOR_MODE);
                    SetWIAProperty(wiaItem.Properties, WIA_IMAGE_FORMAT, iConfig.iterfaceConfig.WIA_IMAGE_FORMAT);

                    Property property = FindProperty(wiaDevice.Properties, DEVICE_PROPERTY_DOCUMENT_HANDLING_SELECT_ID);
                    if (property != null)
                        property.set_Value(iConfig.iterfaceConfig.DEVICE_PROPERTY_DOCUMENT_HANDLING_SELECT_ID);

                    var imageFile = wiaItem.Transfer() as ImageFile;

                    this.ScanCompleted?.Invoke(this, new ScanCompletedEventArgs(imageFile.FileData.get_BinaryData()));

                }
                catch (Exception)
                {
                    break;
                }
                finally
                {
                    //determine if there are any more pages waiting
                    Property documentHandlingSelect = null;
                    Property documentHandlingStatus = null;

                    foreach (Property prop in wiaDevice.Properties)
                    {
                        if (prop.PropertyID == WIA_PROPERTIES.WIA_DPS_DOCUMENT_HANDLING_SELECT)
                            documentHandlingSelect = prop;
                        if (prop.PropertyID == WIA_PROPERTIES.WIA_DPS_DOCUMENT_HANDLING_STATUS)
                            documentHandlingStatus = prop;
                    }
                    hasMorePages = false; //assume there are no more pages
                    if (documentHandlingSelect != null)
                    //may not exist on flatbed scanner but required for feeder
                    {
                        //check for document feeder
                        if ((Convert.ToUInt32(documentHandlingSelect.get_Value()) & WIA_DPS_DOCUMENT_HANDLING_SELECT.FEEDER) != 0)
                        {
                            hasMorePages = ((Convert.ToUInt32(documentHandlingStatus.get_Value()) & WIA_DPS_DOCUMENT_HANDLING_STATUS.FEED_READY) != 0);
                        }
                    }

                    if (hasMorePages && this.SingleOnly)
                        hasMorePages = false;
                }
            }
        }

        public byte[] ScanSingle()
        {
            Device wiaDevice = GetFirstWiaDevice();
            DeviceManager wiaManager = new DeviceManager();

            try
            {
                Item wiaItem = wiaDevice.Items[1];
                SetWIAProperty(wiaItem.Properties, WIA_HORIZONTAL_SCAN_RESOLUTION_DPI, iConfig.iterfaceConfig.WIA_HORIZONTAL_SCAN_RESOLUTION_DPI);
                SetWIAProperty(wiaItem.Properties, WIA_VERTICAL_SCAN_RESOLUTION_DPI, iConfig.iterfaceConfig.WIA_HORIZONTAL_SCAN_RESOLUTION_DPI);
                SetWIAProperty(wiaItem.Properties, WIA_SCAN_BRIGHTNESS_PERCENTS, iConfig.iterfaceConfig.WIA_SCAN_BRIGHTNESS_PERCENTS);
                SetWIAProperty(wiaItem.Properties, WIA_SCAN_COLOR_MODE, iConfig.iterfaceConfig.WIA_SCAN_COLOR_MODE);
                SetWIAProperty(wiaItem.Properties, WIA_IMAGE_FORMAT, iConfig.iterfaceConfig.WIA_IMAGE_FORMAT);
                
                Property property = FindProperty(wiaDevice.Properties, DEVICE_PROPERTY_DOCUMENT_HANDLING_SELECT_ID);
                if (property != null)
                    property.set_Value(iConfig.iterfaceConfig.DEVICE_PROPERTY_DOCUMENT_HANDLING_SELECT_ID);

                var imageFile = wiaItem.Transfer() as ImageFile;
                return imageFile.FileData.get_BinaryData();

            }
            catch
            {
                throw;
            }
        }

        private static void SetWIAProperty(IProperties properties, object propName, object propValue)
        {
            Property prop = properties.get_Item(ref propName);
            prop.set_Value(ref propValue);
        }

        public static Property FindProperty(Properties properties, string propertyId)
        {
            foreach (Property property in properties)
            {
                int intTryParse = 0;
                if (int.TryParse(propertyId, out intTryParse))
                {
                    if (property.PropertyID == intTryParse)
                    {
                        return property;
                    }
                }
            }
            return null;
        }

        public List<ScannerInfo> GetWiaDevices()
        {

            WIA.DeviceManager mgr = new WIA.DeviceManager();
            List<ScannerInfo> retVal = new List<ScannerInfo>();

            foreach (WIA.DeviceInfo info in mgr.DeviceInfos)
            {

                if (info.Type == WIA.WiaDeviceType.ScannerDeviceType)
                {
                    foreach (WIA.Property p in info.Properties)
                    {

                        if (p.Name == "Name")
                        {
                            retVal.Add(new ScannerInfo(((WIA.IProperty)p).get_Value().ToString(), info.DeviceID));
                        }
                    }
                }
            }
            return retVal;
        }

        public WIA.Device GetFirstWiaDevice()
        {

            WIA.DeviceManager mgr = new WIA.DeviceManager();
            WIA.Device retVal = null;

            foreach (WIA.DeviceInfo info in mgr.DeviceInfos)
            {

                if (info.Type == WIA.WiaDeviceType.ScannerDeviceType)
                {
                    return mgr.DeviceInfos.OfType<DeviceInfo>().FirstOrDefault(o => o.DeviceID == info.DeviceID).Connect();
                }
            }
            return retVal;
        }
    }
}
