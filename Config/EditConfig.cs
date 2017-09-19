using System;
using System.Reflection;
using System.Windows.Forms;

namespace Config
{
    public partial class EditConfig : Form
    {
        public EditConfig()
        {
            InitializeComponent();
        }

        ConfigGet cGet;
        ConfigPath cPath;
        PropertyInfo[] itemGet;
        private void EditConfig_Load(object sender, EventArgs e)
        {
            cGet= new ConfigGet();
            PropertyInfo[] ps = cGet.GetType().GetProperties();
            cPath = (ConfigPath)ps[0].GetValue(cGet, null);
            itemGet = cPath.GetType().GetProperties();
            foreach (var i in itemGet)
            {
                dataGridView1.Rows.Add(i.Name, i.GetValue(cPath, null));
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach(DataGridViewRow i in dataGridView1.Rows)
            {
                foreach(PropertyInfo iP in itemGet)
                {
                    if (iP.Name == i.Cells["cName"].Value.ToString())
                    {
                        if (iP.PropertyType == typeof(int))
                        {
                            int tryInt;
                            if (int.TryParse(i.Cells["cValue"].Value.ToString(),out tryInt))
                            {
                                iP.SetValue(cPath, tryInt);
                            }
                            else
                            {
                                iP.SetValue(cPath, 0);
                            }
                        }
                        else
                        {
                            iP.SetValue(cPath, i.Cells["cValue"].Value.ToString());
                        }
                        
                    }
                }
            }
            cGet.WriteToXmlFile<ConfigPath>();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
