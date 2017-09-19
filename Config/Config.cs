using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Forms;

namespace Config
{
    public class ConfigPath
    {
        [XmlElement("WIA_HORIZONTAL_SCAN_RESOLUTION_DPI")]
        public string WIA_HORIZONTAL_SCAN_RESOLUTION_DPI { get; set; }

        [XmlElement("WIA_SCAN_BRIGHTNESS_PERCENTS")]
        public string WIA_SCAN_BRIGHTNESS_PERCENTS { get; set; }

        [XmlElement("WIA_SCAN_COLOR_MODE")]
        public string WIA_SCAN_COLOR_MODE { get; set; }        

        [XmlElement("WIA_IMAGE_FORMAT")]
        public string WIA_IMAGE_FORMAT { get; set; }

        [XmlElement("DEVICE_PROPERTY_DOCUMENT_HANDLING_SELECT_ID")]
        public string DEVICE_PROPERTY_DOCUMENT_HANDLING_SELECT_ID { get; set; }

        [XmlElement("IMAGE_THRESHOLD")]
        public int IMAGE_THRESHOLD { get; set; }

        [XmlElement("ANSWER_FULLNESS")]
        public int ANSWER_FULLNESS { get; set; }
    }

    public class ConfigGet
    {
        public ConfigPath iterfaceConfig {
            get
            {
                return p;
            }
            set
            {
                p = value;
            }
        }

        private ConfigPath p;
        string ConfigfileName;
        public ConfigGet() 
        {

            ConfigfileName = Path.Combine(Path.GetDirectoryName(typeof(ConfigGet).Assembly.Location), @"Config.xml");
            if(!File.Exists(ConfigfileName))
            {
                MessageBox.Show("Config file does not exists, " + ConfigfileName + ".");
                return;
            }

            p = ReadFromXmlFile<ConfigPath>();
        }

        public T ReadFromXmlFile<T>() where T : new()
        {
            TextReader reader = null;
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                reader = new StreamReader(ConfigfileName);
                return (T)serializer.Deserialize(reader);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }

        public void WriteToXmlFile<T>() where T : new()
        {
            TextWriter writer = null;
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                writer = new StreamWriter(ConfigfileName, false);
                serializer.Serialize(writer, p);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        
    }
}
