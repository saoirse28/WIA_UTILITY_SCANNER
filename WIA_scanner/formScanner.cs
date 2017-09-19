using AForge;
using AForge.Imaging.Filters;
using FyfeSoftware.Sketchy.Core;
using FyfeSoftware.Sketchy.Core.Shapes;
using FyfeSoftware.Sketchy.Design;
using Wia;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace wia_scanner
{
    public partial class formScanner : Form
    {
        public ScanEngine m_scanEngine = new ScanEngine();
        private BackgroundWorker bw = new BackgroundWorker();
        private Queue<string> m_processQueue = new Queue<string>();
        private object m_syncObject = new object();

        List<AForge.Point> controlPoints = new List<AForge.Point>();
        public Bitmap cropBmp, originalBmp;
        public DesignerCanvas m_canvas;
        private float[] zooms = {
                                    0.1f,
                                    0.2f,
                                    0.4f,
                                    0.5f,
                                    0.75f,
                                    1.0f,
                                    1.25f,
                                    1.5f,
                                    1.75f,
                                    2.0f,
                                    3.0f
                                };


        private enum EBUTTONSTATE
        {
            CROPSTART,
            CROPEND,
            TRANSFORMSTART,
            TRANSFORMEND
        }

        private void SetButtons(EBUTTONSTATE a_ebuttonstate)
        {
            switch (a_ebuttonstate)
            {
                default:
                case EBUTTONSTATE.CROPSTART:
                    toolStrip1.Items["toolStripButtonReset"].Enabled = false;
                    toolStrip1.Items["toolStripButtonRotateLeft"].Enabled = false;
                    toolStrip1.Items["toolStripButtonRotateRight"].Enabled = false;
                    toolStrip1.Items["toolStripButtonCrop"].Enabled = false;
                    toolStrip1.Items["toolStripButtonTransform"].Enabled = false;
                    toolStrip1.Items["toolStripButtonBrightness"].Enabled = false;

                    toolStrip1.Items["toolStripButtonAcceptCrop"].Visible = true;
                    toolStrip1.Items["toolStripButtonCancelCrop"].Visible = true;
                    toolStrip1.Items["toolStripButtonAcceptTransform"].Visible = false;
                    toolStrip1.Items["toolStripButtonCancelTransform"].Visible = false;
                    button1.Enabled = false;
                    button2.Enabled = false;
                    break;
                case EBUTTONSTATE.CROPEND:
                    toolStrip1.Items["toolStripButtonReset"].Enabled = true;
                    toolStrip1.Items["toolStripButtonRotateLeft"].Enabled = true;
                    toolStrip1.Items["toolStripButtonRotateRight"].Enabled = true;
                    toolStrip1.Items["toolStripButtonCrop"].Enabled = true;
                    toolStrip1.Items["toolStripButtonTransform"].Enabled = true;
                    toolStrip1.Items["toolStripButtonBrightness"].Enabled = true;

                    toolStrip1.Items["toolStripButtonAcceptCrop"].Visible = false;
                    toolStrip1.Items["toolStripButtonCancelCrop"].Visible = false;
                    toolStrip1.Items["toolStripButtonAcceptTransform"].Visible = false;
                    toolStrip1.Items["toolStripButtonCancelTransform"].Visible = false;
                    button1.Enabled = true;
                    button2.Enabled = true;
                    break;
                case EBUTTONSTATE.TRANSFORMSTART:
                    toolStrip1.Items["toolStripButtonReset"].Enabled = false;
                    toolStrip1.Items["toolStripButtonRotateLeft"].Enabled = false;
                    toolStrip1.Items["toolStripButtonRotateRight"].Enabled = false;
                    toolStrip1.Items["toolStripButtonCrop"].Enabled = false;
                    toolStrip1.Items["toolStripButtonTransform"].Enabled = false;
                    toolStrip1.Items["toolStripButtonBrightness"].Enabled = false;

                    toolStrip1.Items["toolStripButtonAcceptCrop"].Visible = false;
                    toolStrip1.Items["toolStripButtonCancelCrop"].Visible = false;
                    toolStrip1.Items["toolStripButtonAcceptTransform"].Visible = true;
                    toolStrip1.Items["toolStripButtonCancelTransform"].Visible = true;
                    button1.Enabled = false;
                    button2.Enabled = false;
                    break;
                case EBUTTONSTATE.TRANSFORMEND:
                    toolStrip1.Items["toolStripButtonReset"].Enabled = true;
                    toolStrip1.Items["toolStripButtonRotateLeft"].Enabled = true;
                    toolStrip1.Items["toolStripButtonRotateRight"].Enabled = true;
                    toolStrip1.Items["toolStripButtonCrop"].Enabled = true;
                    toolStrip1.Items["toolStripButtonBrightness"].Enabled = true;

                    toolStrip1.Items["toolStripButtonAcceptCrop"].Visible = false;
                    toolStrip1.Items["toolStripButtonCancelCrop"].Visible = false;
                    toolStrip1.Items["toolStripButtonTransform"].Enabled = true;
                    toolStrip1.Items["toolStripButtonAcceptTransform"].Visible = false;
                    toolStrip1.Items["toolStripButtonCancelTransform"].Visible = false;
                    button1.Enabled = true;
                    button2.Enabled = true;
                    break;
            }
        }

        public formScanner()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void toolStripButtonReset_Click(object sender, EventArgs e)
        {
            try
            {
                this.m_canvas.Clear();
                cropBmp = new Bitmap(originalBmp);
                this.m_canvas.Add(new BackgroundImageShape() { Image = cropBmp }, "Image");
                SetButtons(EBUTTONSTATE.CROPEND);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void toolStripButtonRotateLeft_Click(object sender, EventArgs e)
        {
            try
            {
                m_canvas.Clear();
                cropBmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                this.m_canvas.Add(new BackgroundImageShape() { Image = cropBmp }, "Image");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void toolStripButtonRotateRight_Click(object sender, EventArgs e)
        {
            try
            {
                m_canvas.Clear();
                cropBmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                this.m_canvas.Add(new BackgroundImageShape() { Image = cropBmp }, "Image");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void toolStripButtonCrop_Click(object sender, EventArgs e)
        {
            try
            {
                IShape itm = m_canvas.FindShape("Crop");

                if (itm == null)
                {
                    this.m_canvas.Add(new CropStencil()
                    {
                        Size = new SizeF(60, 60),
                        Position = new PointF(this.skHost1.HorizontalScroll.Value, this.skHost1.VerticalScroll.Value)
                    }, "Crop");
                    SetButtons(EBUTTONSTATE.CROPSTART);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void toolStripButtonTransform_Click(object sender, EventArgs e)
        {
            try
            {
                if (m_canvas.FindShape("TL") != null)
                {
                    return;
                }

                this.m_canvas.Add(new CornerAnchorShape(new PointF(0, 0), "TL"), "TL");
                this.m_canvas.Add(new CornerAnchorShape(new PointF(cropBmp.Width, 0), "TR"), "TR");
                this.m_canvas.Add(new CornerAnchorShape(new PointF(0, cropBmp.Height), "BL"), "BL");
                this.m_canvas.Add(new CornerAnchorShape(new PointF(cropBmp.Width, cropBmp.Height), "BR"), "BR");

                // Join the canvas stuff
                this.m_canvas.Add(new ConnectionLineShape()
                {
                    Source = this.m_canvas.FindShape("TL"),
                    Target = this.m_canvas.FindShape("TR"),
                    OutlineWidth = 3,
                    OutlineColor = Color.OrangeRed,
                    OutlineStyle = System.Drawing.Drawing2D.DashStyle.Dot
                }, "TLTR");
                this.m_canvas.Add(new ConnectionLineShape()
                {
                    Source = this.m_canvas.FindShape("TR"),
                    Target = this.m_canvas.FindShape("BR"),
                    OutlineWidth = 3,
                    OutlineColor = Color.OrangeRed,
                    OutlineStyle = System.Drawing.Drawing2D.DashStyle.Dot
                }, "TRBR");
                this.m_canvas.Add(new ConnectionLineShape()
                {
                    Source = this.m_canvas.FindShape("BL"),
                    Target = this.m_canvas.FindShape("BR"),
                    OutlineWidth = 3,
                    OutlineColor = Color.OrangeRed,
                    OutlineStyle = System.Drawing.Drawing2D.DashStyle.Dot
                }, "BLBR");
                this.m_canvas.Add(new ConnectionLineShape()
                {
                    Source = this.m_canvas.FindShape("TL"),
                    Target = this.m_canvas.FindShape("BL"),
                    OutlineWidth = 3,
                    OutlineColor = Color.OrangeRed,
                    OutlineStyle = System.Drawing.Drawing2D.DashStyle.Dot
                }, "TLBL");

                SetButtons(EBUTTONSTATE.TRANSFORMSTART);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void toolStripButtonBrightness_Click(object sender, EventArgs e)
        {
            try
            {
                formAdjustLight frm = new formAdjustLight();
                frm.bmp = cropBmp;
                DialogResult result = frm.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    cropBmp = new Bitmap(frm.imgLight);
                }
                this.m_canvas.Clear();
                this.m_canvas.Add(new BackgroundImageShape() { Image = cropBmp }, "Image");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
            
        private void toolStripButtonAcceptCrop_Click(object sender, EventArgs e)
        {
            IShape itm = m_canvas.FindShape("Crop");

            if (itm != null)
            {
                PointF position = itm.Position;
                SizeF size = itm.Size;
                cropBmp = new Crop(new Rectangle((int)position.X, (int)position.Y, (int)size.Width, (int)size.Height)).Apply(cropBmp);
                this.m_canvas.Clear();
                this.m_canvas.Add(new BackgroundImageShape() { Image = cropBmp }, "Image");
                SetButtons(EBUTTONSTATE.CROPEND);
            }
        }

        private void toolStripButtonCancelCrop_Click(object sender, EventArgs e)
        {
            this.m_canvas.Clear();
            this.m_canvas.Add(new BackgroundImageShape() { Image = cropBmp }, "Image");
            SetButtons(EBUTTONSTATE.CROPEND);
        }

        private void toolStripButtonAcceptTransform_Click(object sender, EventArgs e)
        {
            IShape itm = m_canvas.FindShape("TL");

            if (itm == null)
            {
                return;
            }

            IntPoint TL = new IntPoint((int)this.m_canvas.FindShape("TL").Position.X + 15,
                (int)this.m_canvas.FindShape("TL").Position.Y + 15);
            IntPoint TR = new IntPoint((int)this.m_canvas.FindShape("TR").Position.X + 15,
                (int)this.m_canvas.FindShape("TR").Position.Y + 15);
            IntPoint BR = new IntPoint((int)this.m_canvas.FindShape("BR").Position.X + 15,
                (int)this.m_canvas.FindShape("BR").Position.Y + 15);
            IntPoint BL = new IntPoint((int)this.m_canvas.FindShape("BL").Position.X + 15,
                (int)this.m_canvas.FindShape("BL").Position.Y + 15);

            // define quadrilateral's corners
            List<IntPoint> corners = new List<IntPoint>();
            corners.Add(TL);
            corners.Add(TR);
            corners.Add(BR);
            corners.Add(BL);

            double width = Math.Sqrt(Math.Pow(TR.X - TL.X, 2) + Math.Pow(TR.Y - TL.Y, 2));
            double height = Math.Sqrt(Math.Pow(BR.X - TR.X, 2) + Math.Pow(BR.Y - TR.Y, 2));

            double w = cropBmp.Width;
            double h = cropBmp.Height;

            // create filter
            QuadrilateralTransformation filter =
                new QuadrilateralTransformation(corners, (int)width, (int)height);
            // apply the filter

            cropBmp = new Bitmap(filter.Apply(cropBmp));
            this.m_canvas.Clear();
            this.m_canvas.Add(new BackgroundImageShape() { Image = cropBmp }, "Image");
            SetButtons(EBUTTONSTATE.TRANSFORMEND);
        }

        private void toolStripButtonCancelTransform_Click(object sender, EventArgs e)
        {
            this.m_canvas.Clear();
            this.m_canvas.Add(new BackgroundImageShape() { Image = cropBmp }, "Image");
            SetButtons(EBUTTONSTATE.TRANSFORMEND);
        }

        private void toolStripButtonScanner_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] imageBytes = m_scanEngine.ScanSingle();

                using (MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
                {
                    originalBmp = (Bitmap)System.Drawing.Image.FromStream(ms);
                    cropBmp = new Bitmap(originalBmp);
                    this.m_canvas.Add(new BackgroundImageShape() { Image = cropBmp }, "Image");
                }
                tbZoom.Enabled = true;
                button2.Enabled = true;
            }
            catch(Exception ex)
            {
                if (ex.HResult == -2145320957)
                {
                    MessageBox.Show("Place paper on the scanner.");
                }

                tbZoom.Enabled = false;
                button2.Enabled = false;

                Console.WriteLine(ex.Message);
            }

        }

        private void tbZoom_Scroll(object sender, EventArgs e)
        {

            if (m_canvas != null)
            {
                lblZm.Text = String.Format("{0:##}%", zooms[tbZoom.Value] * 100);
                this.m_canvas.Zoom = zooms[tbZoom.Value];
            }
            
        }

        private void lblZm_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void formScanner_Load(object sender, EventArgs e)
        {
            m_canvas = new DesignerCanvas();
            this.skHost1.Canvas = this.m_canvas;
            tbZoom.Enabled = false;
        }

        private void toolStripButtonSetting_Click(object sender, EventArgs e)
        {
            Config.EditConfig EditConfig = new Config.EditConfig();
            EditConfig.Show();
        }

        private void toolStripButtonAbout_Click(object sender, EventArgs e)
        {
            formAbout form = new formAbout();
            var result = form.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog savefile = new SaveFileDialog();
            savefile.FileName = "RetractSoultion.JPG";
            savefile.Filter = "All files (*.*)|*.*";

            if (savefile.ShowDialog() == DialogResult.OK)
            {
                cropBmp.Save(savefile.FileName);
            }

            DialogResult = DialogResult.OK;
            Close();
        }

    }
}
