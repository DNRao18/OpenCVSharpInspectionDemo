using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using VisionInspection.Inspection;
using VisionInspection.Properties;

namespace VisionInspection
{
    /// <summary>
    /// Extract error data by analyzing the image for scanner field calibration
    /// 64-bit: Copy bin\x64\OpenCvSharpExtern.dll to bin
    /// 32-bit: Copy bin\x32\OpenCvSharpExtern.dll to bin
    /// </summary>
    public class MainWindow : Form
    {
        protected Mat img_color;

        private GridInfo gridInfo = new GridInfo();

        protected Vector2[,] resultRealMap;

        protected Vector2[,] resultPixelMap;

        private eCrossLine crossLineType;

        private System.Drawing.Point startPoint;

        private Thread thread;

        private TrackBar trbBinary;

        private Label label1;

        private Label label2;

        private Label label3;

        private NumericUpDown nudDpi;

        private NumericUpDown nudRows;

        private Label label4;

        private NumericUpDown nudCols;

        private CheckBox cbColor;

        private Label label8;

        private Label label9;

        private Label label5;

        private TextBox txtRowPitch;

        private TextBox txtColPitch;

        private TrackBar trbArea;

        private StatusStrip statusStrip1;

        private ToolStripStatusLabel lblAliasName;

        private ToolStripStatusLabel lblTime;

        private ToolStrip toolStrip1;

        private ToolStripButton toolStripButton1;

        private ToolStripButton toolStripButton2;

        private ToolStripButton toolStripButton3;

        private ToolStripSeparator toolStripSeparator9;

        private ToolStripButton btnOpen;

        private ToolStripSeparator toolStripSeparator1;

        private ToolStripStatusLabel lblCounts;

        private TextBox tbCrosshairHeight;

        private TextBox tbCrosshairWidth;

        private Label label7;

        private Label label10;

        private GroupBox gbCrosshair;

        private Button btnInspection;

        private TabControl tabControl1;

        private TabPage tabPage1;

        private Panel panel1;

        private TabPage tabPage2;

        private DataGridView dataGridView1;

        private PictureBox pictureBox1;

        private ToolStripStatusLabel lblAngle;

        private Button btnCancel;

        private Button btnOk;

        private Label lblThreshold;

        private GroupBox groupBox1;

        private RadioButton rdoCircle;

        private RadioButton rdoLine;

        private GroupBox gbCrossLine;

        private RadioButton rbHatchCrossLine;

        private RadioButton rbThinCrossLine;

        private ToolStripStatusLabel lblFileName;

        /// <summary>
        /// alias name
        /// </summary>
        public virtual string AliasName
        {
            get
            {
                return this.lblAliasName.Text;
            }
            set
            {
                this.lblAliasName.Text = value;
            }
        }
        /// <summary>
        /// Pixels per inch (Dots/Inches)
        /// </summary>
        public virtual int Dpi
        {
            get
            {
                return (int)this.nudDpi.Value;
            }
            set
            {
                if (value > 0)
                {
                    this.nudDpi.Value = value;
                }
            }
        }

        /// <summary>
        /// Measurement inspection result X, Y value (mm) array
        /// start from top left
        /// </summary>
        public virtual PointF[] ResultMeasure { get; private set; }
        /// <summary>
        /// Absolute Position X, Y Value (mm) Array
        /// </summary>
        public virtual PointF[] ResultReference { get; private set; }

        public virtual bool IsSuccess { get; private set; }

        public virtual int Rows
        {
            get
            {
                return int.Parse(this.nudRows.Value.ToString());
            }
            set
            {
                this.nudRows.Value = value;
            }
        }

        public virtual float RowPitch
        {
            get
            {
                return float.Parse(this.txtRowPitch.Text);
            }
            set
            {
                this.txtRowPitch.Text = $"{value:F6}";
            }
        }

        
        public virtual int Cols
        {
            get
            {
                return int.Parse(this.nudCols.Value.ToString());
            }
            set
            {
                this.nudCols.Value = value;
            }
        }

        public virtual float ColPitch
        {
            get
            {
                return float.Parse(this.txtColPitch.Text);
            }
            set
            {
                this.txtColPitch.Text = $"{value:F6}";
            }
        }

        public virtual float CrosshairWidth
        {
            get
            {
                return float.Parse(this.tbCrosshairWidth.Text);
            }
            set
            {
                this.tbCrosshairWidth.Text = $"{value:F3}";
            }
        }
        /// <summary>
        /// Crosshair height size (mm)
        /// </summary>
        public virtual float CrosshairHeight
        {
            get
            {
                return float.Parse(this.tbCrosshairHeight.Text);
            }
            set
            {
                this.tbCrosshairHeight.Text = $"{value:F3}";
            }
        }

        public MainWindow()
        {
            this.InitializeComponent();
            if (this.rdoCircle.Checked)
            {
                this.gbCrosshair.Enabled = false;
            }
            else if (this.rdoLine.Checked)
            {
                this.gbCrosshair.Enabled = true;
            }
            base.Load += MainWindow_Load;
            this.panel1.DragDrop += Panel1_DragDrop;
            this.panel1.DragEnter += Panel1_DragEnter;
            this.pictureBox1.MouseWheel += PictureBox1_MouseWheel;
            base.FormClosing += MainWindow_FormClosing;
            this.dataGridView1.CellPainting += DataGridView1_CellPainting;
            this.crossLineType = eCrossLine.eThinLine;
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            this.btnInspection.Focus();
        }

        private void Panel1_DragDrop(object sender, DragEventArgs e)
        {
            object data = e.Data.GetData(DataFormats.FileDrop);
            if (data != null)
            {
                string[] array = data as string[];
                switch (Path.GetExtension(array[0]).ToLower())
                {
                    case ".bmp":
                    case ".jpg":
                    case ".jpeg":
                    case ".jpe":
                    case ".tif":
                    case ".png":
                        this.ReadFile(array[0]);
                        break;
                }
            }
        }

        private void Panel1_DragEnter(object sender, DragEventArgs e)
        {
            object data = e.Data.GetData(DataFormats.FileDrop);
            if (data != null)
            {
                switch (Path.GetExtension((data as string[])[0]).ToLower())
                {
                    case ".bmp":
                    case ".jpg":
                    case ".jpeg":
                    case ".jpe":
                    case ".tif":
                    case ".png":
                        e.Effect = DragDropEffects.Copy;
                        break;
                    default:
                        e.Effect = DragDropEffects.None;
                        break;
                }
            }
        }

        private void DataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            int columnIndex = e.ColumnIndex;
            int rowIndex = e.RowIndex;
            if (rowIndex < 0 || columnIndex < 0)
            {
                return;
            }
            this.dataGridView1.SuspendLayout();
            DataGridViewRow dataGridViewRow = this.dataGridView1.Rows[rowIndex];
            string text = (string)dataGridViewRow.Cells[columnIndex].Value;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            string[] array = text.Split('\r', ',', ';', '\t');
            if (2 == array.Length)
            {
                float num = new Vector2(float.Parse(array[0]), float.Parse(array[1])).Length();
                if (num > 10f)
                {
                    dataGridViewRow.Cells[columnIndex].Style.BackColor = Color.Red;
                    dataGridViewRow.Cells[columnIndex].Style.ForeColor = Color.White;
                }
                else
                {
                    if (num > 0.05f)
                    {
                        num = 0.05f;
                    }
                    int num2 = (int)(255f - 255f * num / 0.05f);
                    Color backColor = Color.FromArgb(num2, num2, num2);
                    if (num2 > 100 && num2 < 156)
                    {
                        num2 = 200;
                    }
                    Color foreColor = Color.FromArgb(255 - num2, 255 - num2, 255 - num2);
                    dataGridViewRow.Cells[columnIndex].Style.BackColor = backColor;
                    dataGridViewRow.Cells[columnIndex].Style.ForeColor = foreColor;
                }
            }
            this.dataGridView1.ResumeLayout();
        }

        public void RefreshData()
        {
            this.dataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            int num = int.Parse(this.nudRows.Value.ToString());
            int num2 = int.Parse(this.nudCols.Value.ToString());
            if (this.ResultMeasure.Length != num * num2 || this.ResultReference.Length != num * num2)
            {
                return;
            }
            this.dataGridView1.SuspendLayout();
            this.dataGridView1.Rows.Clear();
            this.dataGridView1.Columns.Clear();
            this.dataGridView1.ColumnCount = num2;
            int num3 = 0;
            for (int i = 0; i < num; i++)
            {
                DataGridViewRow dataGridViewRow = new DataGridViewRow();
                dataGridViewRow.Height = 40;
                dataGridViewRow.CreateCells(this.dataGridView1);
                for (int j = 0; j < num2; j++)
                {
                    PointF pointF = new PointF(this.ResultMeasure[num3].X - this.ResultReference[num3].X, this.ResultMeasure[num3].Y - this.ResultReference[num3].Y);
                    dataGridViewRow.Cells[j].Value = $"{pointF.X:F6}{Environment.NewLine}{pointF.Y:F6}";
                    num3++;
                }
                this.dataGridView1.Rows.Add(dataGridViewRow);
            }
            for (int k = 0; k < num2; k++)
            {
                this.dataGridView1.Columns[k].Width = 84;
                this.dataGridView1.Columns[k].HeaderText = (k + 1).ToString();
                this.dataGridView1.Columns[k].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }
            for (int l = 0; l < num; l++)
            {
                this.dataGridView1.Rows[l].HeaderCell.Value = (l + 1).ToString();
                this.dataGridView1.Rows[l].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }
            this.dataGridView1.ResumeLayout();
        }

        private void ReadFile(string fileName)
        {
            try
            {
                if(File.Exists(fileName))
                {
                    this.img_color?.Dispose();
                    this.img_color = Cv2.ImRead(fileName);
                    this.pictureBox1.Image?.Dispose();
                    this.pictureBox1.Top = 0;
                    this.pictureBox1.Left = 0;
                    this.pictureBox1.Image = this.img_color.ToBitmap();
                    this.pictureBox1.Width = this.img_color.Width;
                    this.pictureBox1.Height = this.img_color.Height;
                    this.startPoint = System.Drawing.Point.Empty;
                    this.lblFileName.Text = fileName;
                }
                else
                {
                    MessageBox.Show("Image doesn't exists");
                }

            }
            catch (Exception ex)
            {
                this.img_color?.Dispose();
                this.img_color = null;
                MessageBox.Show(ex.Message);
            }
        }

        private void btnOpenImage_Click(object sender, EventArgs e)
        {   
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.bmp, *.jpg, *.jpeg, *.jpe, *.tif, *.png) | *.bmp; *.jpg; *.jpeg; *.jpe; *.tif; *.png";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                this.ReadFile(openFileDialog.FileName);
            }
        }

        private void btnZoomOut_Click(object sender, EventArgs e)
        {
            if (this.img_color != null && this.pictureBox1.Width >= 100 && this.pictureBox1.Height >= 100)
            {
                this.pictureBox1.Width = (int)((double)this.pictureBox1.Width * 0.8);
                this.pictureBox1.Height = (int)((double)this.pictureBox1.Height * 0.8);
            }
        }

        private void btnZoomIn_Click(object sender, EventArgs e)
        {
            if (this.img_color != null)
            {
                this.pictureBox1.Width = (int)((double)this.pictureBox1.Width * 1.2);
                this.pictureBox1.Height = (int)((double)this.pictureBox1.Height * 1.2);
            }
        }

        private void btnZoomFit_Click(object sender, EventArgs e)
        {
            if (this.img_color != null)
            {
                this.pictureBox1.Left = (this.pictureBox1.Top = 0);
                this.pictureBox1.Width = this.img_color.Width;
                this.pictureBox1.Height = this.img_color.Height;
            }
        }

        private void rdoCircle_CheckedChanged(object sender, EventArgs e)
        {
            if (this.rdoCircle.Checked)
            {
                this.gbCrosshair.Enabled = false;
            }
            else if (this.rdoLine.Checked)
            {
                this.gbCrosshair.Enabled = true;
            }
        }

        /// <summary>
        /// 2022.02.16 Background color So, modify to select data taken from RGB
        /// Color -&gt; When changing gray, there is a disadvantage that the difference in brightness between the crosshairs and the background is reduced when changing the gray image depending on the background color
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        private int getImageHue(Mat img)
        {
            Mat mat = new Mat();
            Cv2.CvtColor(img, mat, ColorConversionCodes.BGR2HSV);
            Mat[] mv = new Mat[3];
            Cv2.Split(mat, out mv);
            int[] array = new int[180];
            array.Initialize();
            for (int i = 0; i < mat.Height; i++)
            {
                for (int j = 0; j < mat.Width; j++)
                {
                    array[mv[0].At<byte>(i, j)]++;
                }
            }
            int num2 = 0;
            int num3 = 0;
            for (int k = 0; k < 180; k++)
            {
                if (array[k] > num2)
                {
                    num2 = array[k];
                    num3 = k;
                }
            }
            int result = num3;
            mat.Release();
            mv[0].Release();
            mv[1].Release();
            mv[2].Release();
            return result;
        }

        private void Inspect()
        {
            try
            {
                if (this.img_color == null)
                {
                    return;
                }
                Mat img_result = this.img_color.Clone();
                Stopwatch sw = Stopwatch.StartNew();
                base.Invoke((MethodInvoker)delegate
                {
                    this.dataGridView1.Rows.Clear();
                    this.dataGridView1.Columns.Clear();
                });
                this.ResultMeasure = null;
                Mat mat = new Mat();
                Mat[] mv = new Mat[3];
                Cv2.Split(img_result, out mv);
                int imageHue = this.getImageHue(img_result);
                mat = (((imageHue > 0 && imageHue <= 35) || imageHue >= 150) ? mv[0] : ((imageHue <= 35 || imageHue >= 90) ? mv[2] : mv[2]));
                bool flag = true;
                int binaryValue = 0;
                this.trbBinary.Invoke((MethodInvoker)delegate
                {
                    binaryValue = this.trbBinary.Value;
                });
                try
                {
                    if (this.rdoCircle.Checked)
                    {
                        flag = new InspCircle(this.Dpi, this.Rows, this.Cols, this.RowPitch, this.ColPitch).Inspection(mat, this.cbColor.Checked, binaryValue, ref this.resultRealMap, ref this.resultPixelMap);
                    }
                    else if (this.rdoLine.Checked)
                    {
                        flag = new InspCrosshair(this.Dpi, this.Rows, this.Cols, this.RowPitch, this.ColPitch, this.CrosshairWidth, this.CrosshairHeight).Inspection(mat, this.cbColor.Checked, binaryValue, this.crossLineType, ref this.resultRealMap, ref this.resultPixelMap);
                    }
                }
                catch (Exception ex2)
                {
                    Exception ex = ex2;
                    base.Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show(this, ex.Message, "예외 발생됨");
                    });
                }
                finally
                {
                    mat.Dispose();
                }
                if (!flag)
                {
                    int length = this.resultPixelMap.GetLength(0);
                    int length2 = this.resultPixelMap.GetLength(1);
                    this.ResultMeasure = new PointF[length * length2];
                    int num = 0;
                    float num2 = 25.4f / (float)this.Dpi;
                    for (int i = 0; i < length; i++)
                    {
                        for (int j = 0; j < length2; j++)
                        {
                            if (this.resultPixelMap[i, j].X != 0f || this.resultPixelMap[i, j].Y != 0f)
                            {
                                int num3 = (int)((double)this.resultPixelMap[i, j].X + 0.5);
                                int num4 = (int)((double)this.resultPixelMap[i, j].Y + 0.5);
                                if (i == length / 2 && j == length2 / 2)
                                {
                                    Cv2.Circle(img_result, new OpenCvSharp.Point(num3, num4), 48, Scalar.Red, 2, LineTypes.AntiAlias);
                                }
                                Cv2.Line(img_result, new OpenCvSharp.Point(num3 - 20, num4), new OpenCvSharp.Point(num3 + 20, num4), Scalar.Red, 1, LineTypes.AntiAlias);
                                Cv2.Line(img_result, new OpenCvSharp.Point(num3, num4 - 20), new OpenCvSharp.Point(num3, num4 + 20), Scalar.Red, 1, LineTypes.AntiAlias);
                                Cv2.PutText(img_result, $"{num + 1}", new OpenCvSharp.Point(num3 + 15, num4 + 60), HersheyFonts.HersheySimplex, 1.0, Scalar.Yellow, 2, LineTypes.AntiAlias);
                                this.ResultMeasure[num] = new PointF((float)num3 * num2, (float)num4 * num2);
                                num++;
                            }
                        }
                    }
                    base.Invoke((MethodInvoker)delegate
                    {
                        this.pictureBox1.Image?.Dispose();
                        this.pictureBox1.Image = img_result.ToBitmap();
                    });
                    img_result.Dispose();
                    this.IsSuccess = false;
                    base.Invoke((MethodInvoker)delegate
                    {
                        this.lblCounts.Text = " Fail to search ";
                        this.lblCounts.ForeColor = Color.Red;
                        this.lblTime.Text = $" {sw.ElapsedMilliseconds} ms ";
                    });
                    return;
                }
                if (this.resultPixelMap == null)
                {
                    this.IsSuccess = false;
                    base.Invoke((MethodInvoker)delegate
                    {
                        this.lblCounts.Text = " Fail to search ";
                        this.lblCounts.ForeColor = Color.Red;
                    });
                    return;
                }
                int resRow = this.resultPixelMap.GetLength(0);
                int resCol = this.resultPixelMap.GetLength(1);
                this.ResultReference = new PointF[resRow * resCol];
                float num5 = (0f - this.ColPitch) * (float)(resCol / 2);
                float num6 = this.RowPitch * (float)(resRow / 2);
                int num7 = 0;
                for (int k = 0; k < resRow; k++)
                {
                    for (int l = 0; l < resCol; l++)
                    {
                        this.ResultReference[num7] = new PointF(num5 + (float)l * this.ColPitch, num6 - (float)k * this.RowPitch);
                        num7++;
                    }
                }
                this.ResultMeasure = new PointF[resRow * resCol];
                int resCount = 0;
                float num8 = 25.4f / (float)this.Dpi;
                for (int m = 0; m < resRow; m++)
                {
                    for (int n = 0; n < resCol; n++)
                    {
                        int num9 = (int)((double)this.resultPixelMap[m, n].X + 0.5);
                        int num10 = (int)((double)this.resultPixelMap[m, n].Y + 0.5);
                        if (m == resRow / 2 && n == resCol / 2)
                        {
                            Cv2.Circle(img_result, new OpenCvSharp.Point(num9, num10), 48, Scalar.Red, 2, LineTypes.AntiAlias);
                        }
                        Cv2.Line(img_result, new OpenCvSharp.Point(num9 - 20, num10), new OpenCvSharp.Point(num9 + 20, num10), Scalar.Red, 1, LineTypes.AntiAlias);
                        Cv2.Line(img_result, new OpenCvSharp.Point(num9, num10 - 20), new OpenCvSharp.Point(num9, num10 + 20), Scalar.Red, 1, LineTypes.AntiAlias);
                        Cv2.PutText(img_result, $"{resCount + 1}", new OpenCvSharp.Point(num9 + 15, num10 + 60), HersheyFonts.HersheySimplex, 1.25, Scalar.Yellow, 2, LineTypes.AntiAlias);
                        this.ResultMeasure[resCount] = new PointF((float)num9 * num8, (float)num10 * num8);
                        resCount++;
                    }
                }
                PointF pointF = this.ResultMeasure[this.ResultMeasure.Length / 2];
                for (int num11 = 0; num11 < this.ResultMeasure.Length; num11++)
                {
                    this.ResultMeasure[num11] = new PointF(this.ResultMeasure[num11].X - pointF.X, 0f - (this.ResultMeasure[num11].Y - pointF.Y));
                }
                double horizontanAngle = 0.0;
                double num12 = 0.0;
                double meanAngle = 0.0;
                PointF pointF2 = this.ResultMeasure[this.ResultMeasure.Length / 2 - resCol / 2];
                PointF pointF3 = this.ResultMeasure[this.ResultMeasure.Length / 2 + resCol / 2];
                PointF pointF4 = this.ResultMeasure[this.ResultMeasure.Length - resCol / 2 - 1];
                PointF pointF5 = this.ResultMeasure[resCol / 2];
                double num13 = Math.Atan2(pointF3.Y - pointF2.Y, pointF3.X - pointF2.X);
                horizontanAngle = num13 * 180.0 / Math.PI;
                double num14 = Math.Atan2(pointF4.Y - pointF5.Y, pointF4.X - pointF5.X);
                num12 = num14 * 180.0 / Math.PI + 90.0;
                double num16 = (num13 + num14) / 2.0;
                meanAngle = (horizontanAngle + num12) / 2.0;
                for (int num15 = 0; num15 < this.ResultMeasure.Length; num15++)
                {
                    Vector2 vector = Vector2.Transform(new Vector2(this.ResultMeasure[num15].X, this.ResultMeasure[num15].Y), Matrix3x2.CreateRotation((float)(0.0 - num13)));
                    this.ResultMeasure[num15] = new PointF(vector.X, vector.Y);
                }
                base.Invoke((MethodInvoker)delegate
                {
                    this.lblCounts.Text = $" {resCount}  [{resRow} X {resCol}] Searched ";
                    this.lblCounts.ForeColor = Color.Black;
                    this.lblTime.Text = $" {sw.ElapsedMilliseconds} ms ";
                    if (Math.Abs(meanAngle) > 10.0)
                    {
                        this.IsSuccess = false;
                        this.lblAngle.Text = $" Angle : {horizontanAngle:F3}° ";
                        this.lblAngle.ForeColor = Color.Red;
                    }
                    else
                    {
                        this.IsSuccess = true;
                        this.lblAngle.Text = $" Angle : {horizontanAngle:F3}° ";
                        this.lblAngle.ForeColor = Color.Black;
                    }
                    this.pictureBox1.Image?.Dispose();
                    this.pictureBox1.Image = img_result.ToBitmap();
                    img_result.Dispose();
                    this.RefreshData();
                });
                mv[0].Release();
                mv[1].Release();
                mv[2].Release();
                mat.Release();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.img_color != null && e.Button == MouseButtons.Left)
            {
                this.startPoint = e.Location;
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.img_color != null && e.Button == MouseButtons.Left)
            {
                this.pictureBox1.Location = new System.Drawing.Point(this.pictureBox1.Left + e.Location.X - this.startPoint.X, this.pictureBox1.Top + e.Location.Y - this.startPoint.Y);
                this.Cursor = Cursors.Hand;
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void PictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (this.img_color == null)
            {
                return;
            }
            if (e.Delta > 0)
            {
                if (this.pictureBox1.Width < 15 * base.Width && this.pictureBox1.Height < 15 * base.Height)
                {
                    this.pictureBox1.Width = (int)((double)this.pictureBox1.Width * 1.25);
                    this.pictureBox1.Height = (int)((double)this.pictureBox1.Height * 1.25);
                    this.pictureBox1.Top = (int)((double)e.Y - 1.25 * (double)(e.Y - this.pictureBox1.Top));
                    this.pictureBox1.Left = (int)((double)e.X - 1.25 * (double)(e.X - this.pictureBox1.Left));
                }
            }
            else if (this.pictureBox1.Width > 200 && this.pictureBox1.Height > 200)
            {
                this.pictureBox1.Width = (int)((double)this.pictureBox1.Width / 1.25);
                this.pictureBox1.Height = (int)((double)this.pictureBox1.Height / 1.25);
                this.pictureBox1.Top = (int)((double)e.Y - 0.8 * (double)(e.Y - this.pictureBox1.Top));
                this.pictureBox1.Left = (int)((double)e.X - 0.8 * (double)(e.X - this.pictureBox1.Left));
            }
        }

        private void btnInspection_Click(object sender, EventArgs e)
        {
            if (this.pictureBox1.Image == null || this.img_color == null || this.img_color.Empty())
            {
                return;
            }
            if (this.thread == null || !this.thread.IsAlive)
            {
                if (this.thread != null)
                {
                    this.thread.Join(100);
                    this.thread = null;
                }
                this.gridInfo.GridCol = this.Cols;
                this.gridInfo.GridRow = this.Rows;
                this.gridInfo.PitchCol = this.ColPitch;
                this.gridInfo.PitchRow = this.RowPitch;
                this.gridInfo.Reset();
                this.pictureBox1.Image?.Dispose();
                this.pictureBox1.Image = this.img_color.ToBitmap();
                this.thread = new Thread(WorkerThread);
                this.thread.Start();
            }
        }

        protected virtual void WorkerThread()
        {
            base.Invoke((MethodInvoker)delegate
            {
                this.IsSuccess = false;
                this.btnInspection.Text = "Inspecting ...";
                this.lblCounts.Text = " Searching ... ";
                this.lblTime.Text = " ... ";
            });
            this.Inspect();
            base.Invoke((MethodInvoker)delegate
            {
                if (!base.IsDisposed)
                {
                    this.btnInspection.Text = "Inspect";
                }
            });
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.thread != null && this.thread.IsAlive)
            {
                e.Cancel = true;
            }
        }

        private void trbBinary_Scroll(object sender, EventArgs e)
        {
            base.Invoke((MethodInvoker)delegate
            {
                this.lblThreshold.Text = $"{this.trbBinary.Value}";
            });
        }

        private void rbCrossLine_CheckedChanged(object sender, EventArgs e)
        {
            if (this.rbThinCrossLine.Checked)
            {
                this.crossLineType = eCrossLine.eThinLine;
            }
            else if (this.rbHatchCrossLine.Checked)
            {
                this.crossLineType = eCrossLine.eHatchLine;
            }
        }
        /// <summary>
        /// Method required for designer support.
        /// Do not edit the contents of this method with a code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.trbBinary = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.nudDpi = new System.Windows.Forms.NumericUpDown();
            this.nudRows = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.nudCols = new System.Windows.Forms.NumericUpDown();
            this.cbColor = new System.Windows.Forms.CheckBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.txtRowPitch = new System.Windows.Forms.TextBox();
            this.txtColPitch = new System.Windows.Forms.TextBox();
            this.trbArea = new System.Windows.Forms.TrackBar();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lblAliasName = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblTime = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblCounts = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblAngle = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblFileName = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnOpen = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton3 = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            this.tbCrosshairHeight = new System.Windows.Forms.TextBox();
            this.tbCrosshairWidth = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.gbCrosshair = new System.Windows.Forms.GroupBox();
            this.btnInspection = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.panel1 = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.lblThreshold = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rdoCircle = new System.Windows.Forms.RadioButton();
            this.rdoLine = new System.Windows.Forms.RadioButton();
            this.gbCrossLine = new System.Windows.Forms.GroupBox();
            this.rbHatchCrossLine = new System.Windows.Forms.RadioButton();
            this.rbThinCrossLine = new System.Windows.Forms.RadioButton();
            ((System.ComponentModel.ISupportInitialize)(this.trbBinary)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDpi)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRows)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCols)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trbArea)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.gbCrosshair.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.gbCrossLine.SuspendLayout();
            this.SuspendLayout();
            // 
            // trbBinary
            // 
            this.trbBinary.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.trbBinary.AutoSize = false;
            this.trbBinary.LargeChange = 10;
            this.trbBinary.Location = new System.Drawing.Point(988, 265);
            this.trbBinary.Maximum = 255;
            this.trbBinary.Name = "trbBinary";
            this.trbBinary.Size = new System.Drawing.Size(105, 20);
            this.trbBinary.TabIndex = 9;
            this.trbBinary.TickFrequency = 20;
            this.trbBinary.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trbBinary.Value = 30;
            this.trbBinary.Scroll += new System.EventHandler(this.trbBinary_Scroll);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(885, 265);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 19);
            this.label1.TabIndex = 7;
            this.label1.Text = "Binary :";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(881, 235);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(89, 19);
            this.label2.TabIndex = 8;
            this.label2.Text = "Min. Area :";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(933, 59);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(47, 19);
            this.label3.TabIndex = 12;
            this.label3.Text = "DPI :";
            // 
            // nudDpi
            // 
            this.nudDpi.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nudDpi.Location = new System.Drawing.Point(982, 57);
            this.nudDpi.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudDpi.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudDpi.Name = "nudDpi";
            this.nudDpi.Size = new System.Drawing.Size(80, 26);
            this.nudDpi.TabIndex = 0;
            this.nudDpi.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nudDpi.Value = new decimal(new int[] {
            1200,
            0,
            0,
            0});
            // 
            // nudRows
            // 
            this.nudRows.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nudRows.Increment = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.nudRows.Location = new System.Drawing.Point(982, 86);
            this.nudRows.Maximum = new decimal(new int[] {
            51,
            0,
            0,
            0});
            this.nudRows.Minimum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.nudRows.Name = "nudRows";
            this.nudRows.Size = new System.Drawing.Size(80, 26);
            this.nudRows.TabIndex = 1;
            this.nudRows.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nudRows.Value = new decimal(new int[] {
            9,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(922, 88);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(58, 19);
            this.label4.TabIndex = 14;
            this.label4.Text = "Rows :";
            // 
            // nudCols
            // 
            this.nudCols.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nudCols.Increment = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.nudCols.Location = new System.Drawing.Point(982, 114);
            this.nudCols.Maximum = new decimal(new int[] {
            51,
            0,
            0,
            0});
            this.nudCols.Minimum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.nudCols.Name = "nudCols";
            this.nudCols.Size = new System.Drawing.Size(80, 26);
            this.nudCols.TabIndex = 2;
            this.nudCols.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nudCols.Value = new decimal(new int[] {
            9,
            0,
            0,
            0});
            // 
            // cbColor
            // 
            this.cbColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbColor.AutoSize = true;
            this.cbColor.Location = new System.Drawing.Point(954, 202);
            this.cbColor.Name = "cbColor";
            this.cbColor.Size = new System.Drawing.Size(133, 23);
            this.cbColor.TabIndex = 7;
            this.cbColor.Text = "Inverted Color";
            this.cbColor.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(888, 143);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(100, 19);
            this.label8.TabIndex = 23;
            this.label8.Text = "Rows Pitch :";
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(928, 116);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(51, 19);
            this.label9.TabIndex = 24;
            this.label9.Text = "Cols :";
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(894, 171);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(93, 19);
            this.label5.TabIndex = 25;
            this.label5.Text = "Cols Pitch :";
            // 
            // txtRowPitch
            // 
            this.txtRowPitch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRowPitch.Location = new System.Drawing.Point(982, 140);
            this.txtRowPitch.Name = "txtRowPitch";
            this.txtRowPitch.Size = new System.Drawing.Size(80, 26);
            this.txtRowPitch.TabIndex = 3;
            this.txtRowPitch.Text = "12.5";
            this.txtRowPitch.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtColPitch
            // 
            this.txtColPitch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtColPitch.Location = new System.Drawing.Point(982, 168);
            this.txtColPitch.Name = "txtColPitch";
            this.txtColPitch.Size = new System.Drawing.Size(80, 26);
            this.txtColPitch.TabIndex = 4;
            this.txtColPitch.Text = "12.5";
            this.txtColPitch.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // trbArea
            // 
            this.trbArea.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.trbArea.AutoSize = false;
            this.trbArea.LargeChange = 50;
            this.trbArea.Location = new System.Drawing.Point(988, 235);
            this.trbArea.Maximum = 1000;
            this.trbArea.Minimum = 1;
            this.trbArea.Name = "trbArea";
            this.trbArea.Size = new System.Drawing.Size(105, 20);
            this.trbArea.TabIndex = 8;
            this.trbArea.TickFrequency = 50;
            this.trbArea.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trbArea.Value = 200;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblAliasName,
            this.lblTime,
            this.lblCounts,
            this.lblAngle,
            this.lblFileName});
            this.statusStrip1.Location = new System.Drawing.Point(0, 732);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1098, 24);
            this.statusStrip1.TabIndex = 29;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // lblAliasName
            // 
            this.lblAliasName.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAliasName.Name = "lblAliasName";
            this.lblAliasName.Size = new System.Drawing.Size(76, 18);
            this.lblAliasName.Text = " NoName ";
            // 
            // lblTime
            // 
            this.lblTime.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTime.Name = "lblTime";
            this.lblTime.Size = new System.Drawing.Size(40, 18);
            this.lblTime.Text = "0 ms";
            // 
            // lblCounts
            // 
            this.lblCounts.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCounts.Name = "lblCounts";
            this.lblCounts.Size = new System.Drawing.Size(88, 18);
            this.lblCounts.Text = "0 Searched";
            // 
            // lblAngle
            // 
            this.lblAngle.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAngle.Name = "lblAngle";
            this.lblAngle.Size = new System.Drawing.Size(102, 18);
            this.lblAngle.Text = "Angle : 0.000°";
            // 
            // lblFileName
            // 
            this.lblFileName.Name = "lblFileName";
            this.lblFileName.Size = new System.Drawing.Size(61, 18);
            this.lblFileName.Text = "(Empty)";
            // 
            // toolStrip1
            // 
            this.toolStrip1.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolStrip1.GripMargin = new System.Windows.Forms.Padding(5, 2, 5, 2);
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnOpen,
            this.toolStripSeparator1,
            this.toolStripButton1,
            this.toolStripButton2,
            this.toolStripButton3,
            this.toolStripSeparator9});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1098, 31);
            this.toolStrip1.TabIndex = 30;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btnOpen
            // 
            this.btnOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnOpen.Image = global::VisionInspection.Properties.Resources.btnOpen_Image;
            this.btnOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(29, 28);
            this.btnOpen.Text = "Open";
            this.btnOpen.ToolTipText = "Open";
            this.btnOpen.Click += new System.EventHandler(this.btnOpenImage_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 31);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton1.Image = global::VisionInspection.Properties.Resources.toolStripButton1_Image;
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(29, 28);
            this.toolStripButton1.Text = "toolStripButton3";
            this.toolStripButton1.ToolTipText = "Zoom Out";
            this.toolStripButton1.Click += new System.EventHandler(this.btnZoomOut_Click);
            // 
            // toolStripButton2
            // 
            this.toolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton2.Image = global::VisionInspection.Properties.Resources.toolStripButton2_Image;
            this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton2.Name = "toolStripButton2";
            this.toolStripButton2.Size = new System.Drawing.Size(29, 28);
            this.toolStripButton2.Text = "toolStripButton2";
            this.toolStripButton2.ToolTipText = "Zoom In";
            this.toolStripButton2.Click += new System.EventHandler(this.btnZoomIn_Click);
            // 
            // toolStripButton3
            // 
            this.toolStripButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton3.Image = global::VisionInspection.Properties.Resources.toolStripButton3_Image;
            this.toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton3.Name = "toolStripButton3";
            this.toolStripButton3.Size = new System.Drawing.Size(29, 28);
            this.toolStripButton3.Text = "toolStripButton1";
            this.toolStripButton3.ToolTipText = "Zoom 1:1";
            this.toolStripButton3.Click += new System.EventHandler(this.btnZoomFit_Click);
            // 
            // toolStripSeparator9
            // 
            this.toolStripSeparator9.Name = "toolStripSeparator9";
            this.toolStripSeparator9.Size = new System.Drawing.Size(6, 31);
            // 
            // tbCrosshairHeight
            // 
            this.tbCrosshairHeight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tbCrosshairHeight.Location = new System.Drawing.Point(111, 53);
            this.tbCrosshairHeight.Name = "tbCrosshairHeight";
            this.tbCrosshairHeight.Size = new System.Drawing.Size(80, 26);
            this.tbCrosshairHeight.TabIndex = 32;
            this.tbCrosshairHeight.Text = "1.0";
            this.tbCrosshairHeight.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // tbCrosshairWidth
            // 
            this.tbCrosshairWidth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tbCrosshairWidth.Location = new System.Drawing.Point(111, 25);
            this.tbCrosshairWidth.Name = "tbCrosshairWidth";
            this.tbCrosshairWidth.Size = new System.Drawing.Size(80, 26);
            this.tbCrosshairWidth.TabIndex = 31;
            this.tbCrosshairWidth.Text = "1.0";
            this.tbCrosshairWidth.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(11, 55);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(65, 19);
            this.label7.TabIndex = 34;
            this.label7.Text = "Height :";
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(14, 27);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(62, 19);
            this.label10.TabIndex = 33;
            this.label10.Text = "Width :";
            // 
            // gbCrosshair
            // 
            this.gbCrosshair.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gbCrosshair.Controls.Add(this.label10);
            this.gbCrosshair.Controls.Add(this.tbCrosshairHeight);
            this.gbCrosshair.Controls.Add(this.label7);
            this.gbCrosshair.Controls.Add(this.tbCrosshairWidth);
            this.gbCrosshair.Location = new System.Drawing.Point(864, 493);
            this.gbCrosshair.Name = "gbCrosshair";
            this.gbCrosshair.Size = new System.Drawing.Size(210, 88);
            this.gbCrosshair.TabIndex = 35;
            this.gbCrosshair.TabStop = false;
            this.gbCrosshair.Text = "Cross Hair Size";
            // 
            // btnInspection
            // 
            this.btnInspection.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInspection.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnInspection.Location = new System.Drawing.Point(890, 635);
            this.btnInspection.Name = "btnInspection";
            this.btnInspection.Size = new System.Drawing.Size(196, 42);
            this.btnInspection.TabIndex = 36;
            this.btnInspection.Text = "Inspect";
            this.btnInspection.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnInspection.UseVisualStyleBackColor = true;
            this.btnInspection.Click += new System.EventHandler(this.btnInspection_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(0, 34);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(853, 698);
            this.tabControl1.TabIndex = 39;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.panel1);
            this.tabPage1.Location = new System.Drawing.Point(4, 27);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(845, 667);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = " Loaded Image ";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.AllowDrop = true;
            this.panel1.BackColor = System.Drawing.Color.DarkGray;
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(839, 661);
            this.panel1.TabIndex = 7;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(694, 384);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
            this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseMove);
            this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseUp);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.dataGridView1);
            this.tabPage2.Location = new System.Drawing.Point(4, 27);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(845, 667);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = " Measured Data ";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.BackgroundColor = System.Drawing.Color.DarkGray;
            this.dataGridView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dataGridView1.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(1);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridView1.DefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.GridColor = System.Drawing.SystemColors.Control;
            this.dataGridView1.Location = new System.Drawing.Point(3, 3);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Name = "dataGridView1";
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridView1.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dataGridView1.RowHeadersWidth = 60;
            this.dataGridView1.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dataGridView1.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dataGridView1.RowTemplate.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridView1.RowTemplate.Height = 23;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dataGridView1.ShowEditingIcon = false;
            this.dataGridView1.Size = new System.Drawing.Size(839, 661);
            this.dataGridView1.TabIndex = 2;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(990, 683);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(96, 42);
            this.btnCancel.TabIndex = 41;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(890, 683);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(96, 42);
            this.btnOk.TabIndex = 40;
            this.btnOk.Text = "&Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // lblThreshold
            // 
            this.lblThreshold.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblThreshold.BackColor = System.Drawing.Color.White;
            this.lblThreshold.Location = new System.Drawing.Point(941, 265);
            this.lblThreshold.Name = "lblThreshold";
            this.lblThreshold.Size = new System.Drawing.Size(42, 19);
            this.lblThreshold.TabIndex = 43;
            this.lblThreshold.Text = "30";
            this.lblThreshold.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.rdoCircle);
            this.groupBox1.Controls.Add(this.rdoLine);
            this.groupBox1.Controls.Add(this.gbCrossLine);
            this.groupBox1.Location = new System.Drawing.Point(859, 315);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(234, 172);
            this.groupBox1.TabIndex = 45;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Object Type";
            // 
            // rdoCircle
            // 
            this.rdoCircle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.rdoCircle.AutoSize = true;
            this.rdoCircle.Checked = true;
            this.rdoCircle.Location = new System.Drawing.Point(14, 23);
            this.rdoCircle.Name = "rdoCircle";
            this.rdoCircle.Size = new System.Drawing.Size(73, 23);
            this.rdoCircle.TabIndex = 5;
            this.rdoCircle.TabStop = true;
            this.rdoCircle.Text = "Circle";
            this.rdoCircle.UseVisualStyleBackColor = true;
            // 
            // rdoLine
            // 
            this.rdoLine.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.rdoLine.AutoSize = true;
            this.rdoLine.Location = new System.Drawing.Point(14, 48);
            this.rdoLine.Name = "rdoLine";
            this.rdoLine.Size = new System.Drawing.Size(109, 23);
            this.rdoLine.TabIndex = 6;
            this.rdoLine.Text = "Cross Line";
            this.rdoLine.UseVisualStyleBackColor = true;
            // 
            // gbCrossLine
            // 
            this.gbCrossLine.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gbCrossLine.Controls.Add(this.rbHatchCrossLine);
            this.gbCrossLine.Controls.Add(this.rbThinCrossLine);
            this.gbCrossLine.Location = new System.Drawing.Point(41, 77);
            this.gbCrossLine.Name = "gbCrossLine";
            this.gbCrossLine.Size = new System.Drawing.Size(174, 83);
            this.gbCrossLine.TabIndex = 42;
            this.gbCrossLine.TabStop = false;
            // 
            // rbHatchCrossLine
            // 
            this.rbHatchCrossLine.AutoSize = true;
            this.rbHatchCrossLine.Image = global::VisionInspection.Properties.Resources.thic_cross;
            this.rbHatchCrossLine.Location = new System.Drawing.Point(27, 54);
            this.rbHatchCrossLine.Name = "rbHatchCrossLine";
            this.rbHatchCrossLine.Size = new System.Drawing.Size(124, 23);
            this.rbHatchCrossLine.TabIndex = 0;
            this.rbHatchCrossLine.Text = "Hatch Line";
            this.rbHatchCrossLine.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbHatchCrossLine.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.rbHatchCrossLine.UseVisualStyleBackColor = true;
            this.rbHatchCrossLine.CheckedChanged += new System.EventHandler(this.rbCrossLine_CheckedChanged);
            // 
            // rbThinCrossLine
            // 
            this.rbThinCrossLine.AutoSize = true;
            this.rbThinCrossLine.Checked = true;
            this.rbThinCrossLine.Image = global::VisionInspection.Properties.Resources.thin_cross;
            this.rbThinCrossLine.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.rbThinCrossLine.Location = new System.Drawing.Point(27, 28);
            this.rbThinCrossLine.Name = "rbThinCrossLine";
            this.rbThinCrossLine.Size = new System.Drawing.Size(113, 23);
            this.rbThinCrossLine.TabIndex = 0;
            this.rbThinCrossLine.TabStop = true;
            this.rbThinCrossLine.Text = "Thin Line";
            this.rbThinCrossLine.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbThinCrossLine.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.rbThinCrossLine.UseVisualStyleBackColor = true;
            this.rbThinCrossLine.CheckedChanged += new System.EventHandler(this.rbCrossLine_CheckedChanged);
            // 
            // MainWindow
            // 
            this.AcceptButton = this.btnInspection;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1098, 756);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lblThreshold);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.gbCrosshair);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btnInspection);
            this.Controls.Add(this.trbArea);
            this.Controls.Add(this.txtColPitch);
            this.Controls.Add(this.txtRowPitch);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.cbColor);
            this.Controls.Add(this.nudCols);
            this.Controls.Add(this.nudRows);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.nudDpi);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.trbBinary);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.statusStrip1);
            this.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MainWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Image Inspector™";
            this.Load += new System.EventHandler(this.MainWindow_Load_1);
            ((System.ComponentModel.ISupportInitialize)(this.trbBinary)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDpi)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRows)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCols)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trbArea)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.gbCrosshair.ResumeLayout(false);
            this.gbCrosshair.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.gbCrossLine.ResumeLayout(false);
            this.gbCrossLine.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void MainWindow_Load_1(object sender, EventArgs e)
        {
            Bitmap bitmap = Properties.Resources.ContourExtraction;
            string tempFilePath = Path.GetTempPath() + "\\Sample.bmp";
            bitmap.Save(tempFilePath, ImageFormat.Bmp);
            if (File.Exists(tempFilePath))
            {
                ReadFile(tempFilePath);
            }
             
        }
    }
}
