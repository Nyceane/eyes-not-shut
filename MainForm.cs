using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using voice_synthesis.cs;

namespace face_tracking.cs
{
    public partial class MainForm : Form
    {
        public enum Label
        {
            StatusLabel,
            AlertsLabel
        };

        public PXCMSession Session;
        public volatile bool Register = false;
        public volatile bool Unregister = false;        
        public volatile bool Stopped = false;

        private readonly object m_bitmapLock = new object();
        private readonly FaceTextOrganizer m_faceTextOrganizer;
        private IEnumerable<CheckBox> m_modulesCheckBoxes;
        private IEnumerable<TextBox> m_modulesTextBoxes; 
        private Bitmap m_bitmap;
        private string m_filename;
        private Tuple<PXCMImage.ImageInfo, PXCMRangeF32> m_selectedColorResolution;
        private volatile bool m_closing;
        private static ToolStripMenuItem m_deviceMenuItem;
        private static ToolStripMenuItem m_moduleMenuItem;
        private static readonly int LANDMARK_ALIGNMENT = -3;

        private System.Timers.Timer aTimer;

        static int yawn = 0;
        static int eyeclose = 0;
        static int lookaway = 0;

        private async void CheckIfDriving()
        {
            Mojio.Client.MojioClient client = new Mojio.Client.MojioClient();
            var result = await client.BeginAsync(new Guid("c363d2ec-58e2-4e24-9e80-8a359b24075c"), new Guid("34e32830-f655-4c5b-a08f-1256c4cff9f6"), new Guid("d79b8d70-216f-4ee8-a7d9-0e3f728c9386"));
            var vehicle = await client.GetAsync<Mojio.Vehicle>(new Guid("a9288126-9822-4431-a0fc-6abbee258e61"));
            if(vehicle.Data.LastGear != Mojio.Gears.P)
            {
                //Run Scanning
            }
            

            var events = from ev in client.Queryable<Mojio.Events.Event>()
                         where ev.EventType.Equals(Mojio.Events.EventType.AttentionAssistant)
                         && ev.Time > DateTime.Now.AddMinutes(-1)
                         select ev;
            if(events.Any())
            {
                //detected
            }
            else
            {
                //clear
            }
        }

        private void setFlag()
        {
            string url = "https://secret-plateau-4930.herokuapp.com/flagset";
            WebRequest request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            ((HttpWebRequest)request).UserAgent = ".NET Framework Example Client";
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            response.Close();
        }

        private void clearFlag()
        {
            string url = "https://secret-plateau-4930.herokuapp.com/flagclear";
            WebRequest request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            ((HttpWebRequest)request).UserAgent = ".NET Framework Example Client";
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            response.Close();
        }

        private void setLight()
        {
            string url = "http://api.hackthedrive.com/vehicles/WBY1Z4C55EV273078/lights/";

            string json = "{\"count\": 3}";
            var data = Encoding.ASCII.GetBytes(json);

            var request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = json.Length;
            request.Accept = "application/json";


            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                Console.WriteLine("Flashing light Sent to the car: " + responseString);
                response.Close();
            }
            catch (System.Net.WebException ex)
            {

                if (ex.Status != WebExceptionStatus.ProtocolError)
                {
                    throw;
                }

                // Else, return the response anyway
//                var responseString = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }
        }

        private void lockDoor()
        {
            string url = "http://api.hackthedrive.com/vehicles/WBY1Z4C55EV273078/lock/";

            string json = "{\"key\": \"peter2015\"}";
            var data = Encoding.ASCII.GetBytes(json);

            var request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = json.Length;
            request.Accept = "application/json";


            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                Console.WriteLine("Doorlock Sent to the car: " + responseString);
                response.Close();
            }
            catch (System.Net.WebException ex)
            {

                if (ex.Status != WebExceptionStatus.ProtocolError)
                {
                    throw;
                }

                // Else, return the response anyway
   //             var responseString = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }
        }

        private void speakHorn()
        {
            string url = "http://api.hackthedrive.com/vehicles/WBY1Z4C55EV273078/horn/";

            string json = "{\"key\": \"peter2015\", \"count\": 5}";
            var data = Encoding.ASCII.GetBytes(json);

            var request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = json.Length;
            request.Accept = "application/json";


            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                Console.WriteLine("Horn Sent to the car: " + responseString);
                response.Close();
            }
            catch (System.Net.WebException ex)
            {

                if (ex.Status != WebExceptionStatus.ProtocolError)
                {
                    throw;
                }

                // Else, return the response anyway
//                var responseString = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            }
        }

        private void speakOut()
        {
            string sts = VoiceSynthesis.Speak(null, 
                0, 
                "Warning Drowsy Driving Detected", 100, 100, 100
                );
            if (sts != null) MessageBox.Show(sts);
        }
        public MainForm(PXCMSession session)
        {
            InitializeComponent();
            InitializeCheckboxes();
            InitializeTextBoxes();

            m_faceTextOrganizer = new FaceTextOrganizer();
            m_deviceMenuItem = new ToolStripMenuItem("Device");
            m_moduleMenuItem = new ToolStripMenuItem("Module");
            Session = session;
            CreateResolutionMap();
            PopulateDeviceMenu();
            PopulateModuleMenu();
            PopulateProfileMenu();

            FormClosing += MainForm_FormClosing;
            Panel2.Paint += Panel_Paint;

            this.Text = "Eyes not Shut";

            this.RegisterUser.Hide();
            this.UnregisterUser.Text = "";

            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 7000;
            aTimer.Enabled = true;

            setLight();

        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {

            DateTime now = DateTime.UtcNow;

            SetText("");

            if (eyeclose > 25)
            {
                Console.WriteLine("Eyeclose: " + eyeclose.ToString());
                SetText("Eye Closed");
                speakOut();
                setFlag();
                setLight();
                lockDoor();
                speakHorn();
            }
            else
            {
                clearFlag();
            }

            if (yawn > 10)
            {
                string tmp = this.UnregisterUser.Text;
                if (!String.IsNullOrEmpty(this.UnregisterUser.Text))
                {
                    tmp += ", ";
                }
                tmp += "Yawning";
                SetText(tmp);
            }

            if (lookaway > 10)
            {
                string tmp = this.UnregisterUser.Text;
                if (!String.IsNullOrEmpty(this.UnregisterUser.Text))
                {
                    tmp += ", ";
                }
                tmp += "Look Away";
                SetText(tmp);
            }

            yawn = 0;
            eyeclose = 0;
            lookaway = 0;
        }

        delegate void SetTextCallback(string text);

        private void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.UnregisterUser.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.UnregisterUser.Text = text;
            }
        }

        private void InitializeTextBoxes()
        {
            m_modulesTextBoxes = new List<TextBox>
            {
                NumDetectionText,
                NumLandmarksText,
                NumPoseText,
                NumExpressionsText,
            };

            foreach (var textBox in m_modulesTextBoxes)
            {
                textBox.Text = @"4";
            }
        }
        private void InitializeCheckboxes()
        {
            m_modulesCheckBoxes = new List<CheckBox>
            {
                Detection,
                Landmarks,
                Pose,
                Expressions,
                Recognition
            };

            foreach (var checkBox in m_modulesCheckBoxes)
            {
                checkBox.Enabled = true;
                checkBox.Checked = true;
            }
        }

        public Dictionary<string, PXCMCapture.DeviceInfo> Devices { get; set; }
        public Dictionary<string, IEnumerable<Tuple<PXCMImage.ImageInfo, PXCMRangeF32>>> ColorResolutions { get; set; }
        private readonly List<Tuple<int, int>> SupportedColorResolutions = new List<Tuple<int, int>>
        {
            Tuple.Create(1920, 1080),
            Tuple.Create(1280, 720),
            Tuple.Create(960, 540),
            Tuple.Create(640, 480),
            Tuple.Create(640, 360),
        };

        public int NumDetection
        {
            get 
            {
                int val;
                try
                {
                    val = Convert.ToInt32(NumDetectionText.Text); 
                }
                catch
                {
                    val = 0;
                }
                return val; 
            }
        }

        public int NumLandmarks
        {
            get 
            {
                int val;
                try
                {
                    val = Convert.ToInt32(NumLandmarksText.Text); 
                }
                catch
                {
                    val = 0;
                }
                return val; 
            }            
        }

        public int NumPose
        {
            get 
            {
                int val;
                try
                {
                    val = Convert.ToInt32(NumPoseText.Text); 
                }
                catch
                {
                    val = 0;
                }
                return val; 
            }             
        }

        public int NumExpressions
        {
            get 
            {
                int val;
                try
                {
                    val = Convert.ToInt32(NumExpressionsText.Text); 
                }
                catch
                {
                    val = 0;
                }
                return val; 
            }
        }

        public string GetFileName()
        {
            return m_filename;
        }

        public bool IsRecognitionChecked()
        {
            return Recognition.Checked;
        }

        private void CreateResolutionMap()
        {
            ColorResolutions = new Dictionary<string, IEnumerable<Tuple<PXCMImage.ImageInfo, PXCMRangeF32>>>();
            var desc = new PXCMSession.ImplDesc
            {
                group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR,
                subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE
            };

            for (int i = 0; ; i++)
            {
                PXCMSession.ImplDesc desc1;
                if (Session.QueryImpl(desc, i, out desc1) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

                PXCMCapture capture;
                if (Session.CreateImpl(desc1, out capture) < pxcmStatus.PXCM_STATUS_NO_ERROR) continue;

                for (int j = 0; ; j++)
                {
                    PXCMCapture.DeviceInfo info;
                    if (capture.QueryDeviceInfo(j, out info) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

                    PXCMCapture.Device device = capture.CreateDevice(j);
                    if (device == null)
                    {
                        throw new Exception("PXCMCapture.Device null");
                    }
                    var deviceResolutions = new List<Tuple<PXCMImage.ImageInfo, PXCMRangeF32>>();

                    for (int k = 0; k < device.QueryStreamProfileSetNum(PXCMCapture.StreamType.STREAM_TYPE_COLOR); k++)
                    {
                        PXCMCapture.Device.StreamProfileSet profileSet;
                        device.QueryStreamProfileSet(PXCMCapture.StreamType.STREAM_TYPE_COLOR, k, out profileSet);
                        var currentRes = new Tuple<PXCMImage.ImageInfo, PXCMRangeF32>(profileSet.color.imageInfo,
                            profileSet.color.frameRate);

                        if (SupportedColorResolutions.Contains(new Tuple<int, int>(currentRes.Item1.width, currentRes.Item1.height)))
                        {
                            deviceResolutions.Add(currentRes);
                        }
                    }
                    ColorResolutions.Add(info.name, deviceResolutions);
                    device.Dispose();
                }                              
                
                capture.Dispose();
            }
        }

        public void PopulateDeviceMenu()
        {
            Devices = new Dictionary<string, PXCMCapture.DeviceInfo>();
            var desc = new PXCMSession.ImplDesc
            {
                group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR,
                subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE
            };
                        
            for (int i = 0; ; i++)
            {
                PXCMSession.ImplDesc desc1;
                if (Session.QueryImpl(desc, i, out desc1) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

                PXCMCapture capture;
                if (Session.CreateImpl(desc1, out capture) < pxcmStatus.PXCM_STATUS_NO_ERROR) continue;

                for (int j = 0; ; j++)
                {
                    PXCMCapture.DeviceInfo dinfo;
                    if (capture.QueryDeviceInfo(j, out dinfo) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
                    if (!Devices.ContainsKey(dinfo.name))
                        Devices.Add(dinfo.name, dinfo);
                    var sm1 = new ToolStripMenuItem(dinfo.name, null, Device_Item_Click);
                    m_deviceMenuItem.DropDownItems.Add(sm1);
                }

                capture.Dispose();
            }

            if (m_deviceMenuItem.DropDownItems.Count > 0)
            {
                ((ToolStripMenuItem)m_deviceMenuItem.DropDownItems[0]).Checked = true;
                PopulateColorResolutionMenu(m_deviceMenuItem.DropDownItems[0].ToString());
            }

            try
            {
                MainMenu.Items.RemoveAt(0);
            }
            catch (NotSupportedException)
            {
                m_deviceMenuItem.Dispose();
                throw;
            }
            MainMenu.Items.Insert(0, m_deviceMenuItem);
        }

        public void PopulateColorResolutionMenu(string deviceName)
        {
            bool foundDefaultResolution = false;
            var sm = new ToolStripMenuItem("Color Resolution");
            foreach (var resolution in ColorResolutions[deviceName])
            {
                string resText = PixelFormat2String(resolution.Item1.format) + " " + resolution.Item1.width + "x"
                                 + resolution.Item1.height + " " + resolution.Item2.max + " fps";
                var sm1 = new ToolStripMenuItem(resText, null);
                Tuple<PXCMImage.ImageInfo, PXCMRangeF32> selectedResolution = resolution;
                sm1.Click += (sender, eventArgs) =>
                {
                    m_selectedColorResolution = selectedResolution;
                    ColorResolution_Item_Click(sender);
                };
            
                sm.DropDownItems.Add(sm1);

                if (selectedResolution.Item1.format == PXCMImage.PixelFormat.PIXEL_FORMAT_YUY2 && 
                    selectedResolution.Item1.width == 640 && selectedResolution.Item1.height == 360 && selectedResolution.Item2.min == 30)
                {
                    foundDefaultResolution = true;
                    sm1.Checked = true;
                    sm1.PerformClick();
                }
            }

	        if (!foundDefaultResolution && sm.DropDownItems.Count > 0)
	        {
	            ((ToolStripMenuItem)sm.DropDownItems[0]).Checked = true;
	            ((ToolStripMenuItem)sm.DropDownItems[0]).PerformClick();
	        }

            try
            {
                MainMenu.Items.RemoveAt(1);
            }
            catch (NotSupportedException)
            {
                sm.Dispose();
                throw;
            }
            MainMenu.Items.Insert(1, sm);
        }

        private void PopulateModuleMenu()
        {
            var desc = new PXCMSession.ImplDesc();
            desc.cuids[0] = PXCMFaceModule.CUID;
            
            for (int i = 0; ; i++)
            {
                PXCMSession.ImplDesc desc1;
                if (Session.QueryImpl(desc, i, out desc1) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
                var mm1 = new ToolStripMenuItem(desc1.friendlyName, null, Module_Item_Click);
                m_moduleMenuItem.DropDownItems.Add(mm1);
            }
            if (m_moduleMenuItem.DropDownItems.Count > 0)
                ((ToolStripMenuItem)m_moduleMenuItem.DropDownItems[0]).Checked = true;
            try
            {
                MainMenu.Items.RemoveAt(2);
            }
            catch (NotSupportedException)
            {
                m_moduleMenuItem.Dispose();
                throw;
            }
            MainMenu.Items.Insert(2, m_moduleMenuItem);
            
        }

        private void PopulateProfileMenu()
        {
            var pm = new ToolStripMenuItem("Profile");

            foreach (var trackingMode in (PXCMFaceConfiguration.TrackingModeType[])Enum.GetValues(typeof(PXCMFaceConfiguration.TrackingModeType)))
            {
                var pm1 = new ToolStripMenuItem(FaceMode2String(trackingMode), null, Profile_Item_Click);
                pm.DropDownItems.Add(pm1);

                if (trackingMode == PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR_PLUS_DEPTH) //3d = default
                {
                    pm1.Checked = true;
                }
            }
            try
            {
                MainMenu.Items.RemoveAt(3);
            }
            catch (NotSupportedException)
            {
                pm.Dispose();
                throw;
            }
            MainMenu.Items.Insert(3, pm);
        }

        private static string FaceMode2String(PXCMFaceConfiguration.TrackingModeType mode)
        {
            switch (mode)
            {
                case PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR:
                    return "2D Tracking";
                case PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR_PLUS_DEPTH:
                    return "3D Tracking";
            }
            return "";
        }

        private static string PixelFormat2String(PXCMImage.PixelFormat format)
        {
            switch (format)
            {
                case PXCMImage.PixelFormat.PIXEL_FORMAT_YUY2:
                    return "YUY2";
                case PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32:
                    return "RGB32";
                case PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24:
                    return "RGB24";                
            }
            return "NA";
        }

        private void RadioCheck(object sender, string name)
        {
            foreach (ToolStripMenuItem m in MainMenu.Items)
            {
                if (!m.Text.Equals(name)) continue;
                foreach (ToolStripMenuItem e1 in m.DropDownItems)
                {
                    e1.Checked = (sender == e1);
                }
            }
        }

        private void ColorResolution_Item_Click(object sender)
        {
            RadioCheck(sender, "Color Resolution");
        }

        private void Device_Item_Click(object sender, EventArgs e)
        {
            PopulateColorResolutionMenu(sender.ToString());
            RadioCheck(sender, "Device");
        }

        private void Module_Item_Click(object sender, EventArgs e)
        {
            RadioCheck(sender, "Module");
            PopulateProfileMenu();
        }

        private void Profile_Item_Click(object sender, EventArgs e)
        {
            RadioCheck(sender, "Profile");
        }

        private void Start_Click(object sender, EventArgs e)
        {
            Start.Enabled = false;
            MainMenu.Enabled = false;
            Mirror.Enabled = false;
            NumDetectionText.Enabled = false;
            NumLandmarksText.Enabled = false;
            NumPoseText.Enabled = false;
            NumExpressionsText.Enabled = false;
            Stop.Enabled = true;

            foreach (CheckBox moduleCheckBox in m_modulesCheckBoxes)
            {
                moduleCheckBox.Enabled = false;
            }

            if (Recognition.Checked)
            {
                RegisterUser.Enabled = true;
                UnregisterUser.Enabled = true;
            }

            Stopped = false;
            var thread = new Thread(DoTracking);
            thread.Start();
        }

        private void DoTracking()
        {
            var ft = new FaceTracking(this);
            ft.SimplePipeline();
            Invoke(new DoTrackingCompleted(() =>
            {
                foreach (CheckBox moduleCheckBox in m_modulesCheckBoxes)
                {
                    moduleCheckBox.Enabled = true;
                }
                Start.Enabled = true;
                Stop.Enabled = false;
                MainMenu.Enabled = true;

                Mirror.Enabled = true;
                NumDetectionText.Enabled = true;
                NumLandmarksText.Enabled = true;
                NumPoseText.Enabled = true;
                NumExpressionsText.Enabled = true;

                RegisterUser.Enabled = false;
                UnregisterUser.Enabled = false;

                if (m_closing) Close();
            }));
        }

        public string GetCheckedDevice()
        {
            return (from ToolStripMenuItem m in MainMenu.Items
                where m.Text.Equals("Device")
                from ToolStripMenuItem e in m.DropDownItems
                where e.Checked
                select e.Text).FirstOrDefault();
        }

        public Tuple<PXCMImage.ImageInfo, PXCMRangeF32> GetCheckedColorResolution()
        {
            return m_selectedColorResolution;
        }

        public string GetCheckedModule()
        {
            return (from ToolStripMenuItem m in MainMenu.Items
                where m.Text.Equals("Module")
                from ToolStripMenuItem e in m.DropDownItems
                where e.Checked
                select e.Text).FirstOrDefault();
        }

        public string GetCheckedProfile()
        {
            foreach (ToolStripMenuItem m in from ToolStripMenuItem m in MainMenu.Items where m.Text.Equals("Profile") select m)
            {
                for (int i = 0; i < m.DropDownItems.Count; i++)
                {
                    if (((ToolStripMenuItem) m.DropDownItems[i]).Checked)
                        return m.DropDownItems[i].Text;
                }
            }
            return "";
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stopped = true;
            e.Cancel = Stop.Enabled;
            m_closing = true;
        }

        public void UpdateStatus(string status, Label label)
        {
            if (label == Label.StatusLabel)
                Status2.Invoke(new UpdateStatusDelegate(delegate(string s) { StatusLabel.Text = s; }),
                    new object[] {status});

            if (label == Label.AlertsLabel)
                Status2.Invoke(new UpdateStatusDelegate(delegate(string s) { AlertsLabel.Text = s; }),
                    new object[] {status});
        }

        private void Stop_Click(object sender, EventArgs e)
        {
            Stopped = true;
        }

        private void Panel_Paint(object sender, PaintEventArgs e)
        {
            lock (m_bitmapLock)
            {
                if (m_bitmap == null) return;
                if (Scale.Checked)
                {
                    e.Graphics.DrawImage(m_bitmap, Panel2.ClientRectangle);
                }
                else
                {
                    e.Graphics.DrawImageUnscaled(m_bitmap, 0, 0);
                }
            }
        }

        public void UpdatePanel()
        {
            Panel2.Invoke(new UpdatePanelDelegate(() => Panel2.Invalidate()));
        }

        public void DrawBitmap(Bitmap picture)
        {
            lock (m_bitmapLock)
            {
                if (m_bitmap != null)
                {
                    m_bitmap.Dispose();
                }
                m_bitmap = new Bitmap(picture);
            }
        }

        public void DrawGraphics(PXCMFaceData moduleOutput)
        {
            Debug.Assert(moduleOutput != null);

            for (int i = 0; i < moduleOutput.QueryNumberOfDetectedFaces(); i++)
            {
                PXCMFaceData.Face face = moduleOutput.QueryFaceByIndex(i);
                if (face == null)
                {
                    throw new Exception("DrawGraphics::PXCMFaceData.Face null");
                }
                
                lock (m_bitmapLock)
                {
                    m_faceTextOrganizer.ChangeFace(i, face, m_bitmap.Height, m_bitmap.Width);
                }

                DrawLocation(face);
                DrawLandmark(face);
                DrawPose(face);
                DrawExpressions(face);
                DrawRecognition(face);
            }
        }

        private void RegisterUser_Click(object sender, EventArgs e)
        {
            Register = true;
        }

        private void UnregisterUser_Click(object sender, EventArgs e)
        {
            Unregister = true;
        }

        #region Playback / Record

        private void Live_Click(object sender, EventArgs e)
        {
            Playback.Checked = Record.Checked = false;
            Live.Checked = true;
        }

        private void Playback_Click(object sender, EventArgs e)
        {
            Live.Checked = Record.Checked = false;
            Playback.Checked = true;
            var ofd = new OpenFileDialog
            {
                Filter = @"RSSDK clip|*.rssdk|Old format clip|*.pcsdk|All files|*.*",
                CheckFileExists = true,
                CheckPathExists = true
            };
            try
            {
                m_filename = (ofd.ShowDialog() == DialogResult.OK) ? ofd.FileName : null;                
            }
            catch (Exception)
            {
                ofd.Dispose();
                throw;
            }
            ofd.Dispose();
        }

        public bool GetPlaybackState()
        {
            return Playback.Checked;
        }

        private void Record_Click(object sender, EventArgs e)
        {
            Live.Checked = Playback.Checked = false;
            Record.Checked = true;
            var sfd = new SaveFileDialog
            {
                Filter = @"RSSDK clip|*.rssdk|All files|*.*",
                CheckPathExists = true,
                OverwritePrompt = true,
                AddExtension    = true
            };
            try
            {
                m_filename = (sfd.ShowDialog() == DialogResult.OK) ? sfd.FileName : null;
            }
            catch (Exception)
            {
                sfd.Dispose();
                throw;
            }
            sfd.Dispose();
        }

        public bool GetRecordState()
        {
            return Record.Checked;
        }

        public string GetPlaybackFile()
        {
            return Invoke(new GetFileDelegate(() =>
            {
                var ofd = new OpenFileDialog
                {
                    Filter = @"All files (*.*)|*.*",
                    CheckFileExists = true,
                    CheckPathExists = true
                };
                return ofd.ShowDialog() == DialogResult.OK ? ofd.FileName : null;
            })) as string;
        }

        public string GetRecordFile()
        {
            return Invoke(new GetFileDelegate(() =>
            {
                var sfd = new SaveFileDialog
                {
                    Filter = @"All files (*.*)|*.*",
                    CheckFileExists = true,
                    OverwritePrompt = true
                };
                if (sfd.ShowDialog() == DialogResult.OK) return sfd.FileName;
                return null;
            })) as string;
        }

        private delegate string GetFileDelegate();

        #endregion

        #region Modules Drawing

        private static readonly Assembly m_assembly = Assembly.GetExecutingAssembly();

        private readonly ResourceSet m_resources = 
            new ResourceSet(m_assembly.GetManifestResourceStream(@"face_tracking.cs.Properties.Resources.resources"));

        private readonly Dictionary<PXCMFaceData.ExpressionsData.FaceExpression, Bitmap> m_cachedExpressions =
            new Dictionary<PXCMFaceData.ExpressionsData.FaceExpression, Bitmap>();

        private readonly Dictionary<PXCMFaceData.ExpressionsData.FaceExpression, string> m_expressionDictionary =
            new Dictionary<PXCMFaceData.ExpressionsData.FaceExpression, string>
            {
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_MOUTH_OPEN, @"MouthOpen"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_SMILE, @"Smile"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_KISS, @"Kiss"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_UP, @"Eyes_Turn_Up"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_DOWN, @"Eyes_Turn_Down"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_TURN_LEFT, @"Eyes_Turn_Left"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_TURN_RIGHT, @"Eyes_Turn_Right"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_CLOSED_LEFT, @"Eyes_Closed_Left"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_CLOSED_RIGHT, @"Eyes_Closed_Right"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_BROW_LOWERER_RIGHT, @"Brow_Lowerer_Right"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_BROW_LOWERER_LEFT, @"Brow_Lowerer_Left"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_BROW_RAISER_RIGHT, @"Brow_Raiser_Right"},
                {PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_BROW_RAISER_LEFT, @"Brow_Raiser_Left"}
            };

        public void DrawLocation(PXCMFaceData.Face face)
        {
            Debug.Assert(face != null);
            if (m_bitmap == null || !Detection.Checked) return;

            PXCMFaceData.DetectionData detection = face.QueryDetection();
            if (detection == null)
                return;

            lock (m_bitmapLock)
            {
                using (Graphics graphics = Graphics.FromImage(m_bitmap))
                using (var pen = new Pen(m_faceTextOrganizer.Colour, 3.0f))
                using (var brush = new SolidBrush(m_faceTextOrganizer.Colour))
                using (var font = new Font(FontFamily.GenericMonospace, m_faceTextOrganizer.FontSize, FontStyle.Bold))
                {
                    graphics.DrawRectangle(pen, m_faceTextOrganizer.RectangleLocation);
                    String faceId = String.Format("Face ID: {0}",
                        face.QueryUserID().ToString(CultureInfo.InvariantCulture));
                    graphics.DrawString(faceId, font, brush, m_faceTextOrganizer.FaceIdLocation);
                }
            }
        }

        

        public void DrawLandmark(PXCMFaceData.Face face)
        {
            Debug.Assert(face != null);
            PXCMFaceData.LandmarksData landmarks = face.QueryLandmarks();
            if (m_bitmap == null || !Landmarks.Checked || landmarks == null) return;

            lock (m_bitmapLock)
            {
                using (Graphics graphics = Graphics.FromImage(m_bitmap))
                using (var brush = new SolidBrush(Color.White))
                using (var lowConfidenceBrush = new SolidBrush(Color.Red))
                using (var font = new Font(FontFamily.GenericMonospace, m_faceTextOrganizer.FontSize, FontStyle.Bold))
                {
                    PXCMFaceData.LandmarkPoint[] points;
                    bool res = landmarks.QueryPoints(out points);
                    Debug.Assert(res);

                    var point = new PointF();

                    foreach (PXCMFaceData.LandmarkPoint landmark in points)
                    {
                        point.X = landmark.image.x + LANDMARK_ALIGNMENT;
                        point.Y = landmark.image.y + LANDMARK_ALIGNMENT;

                        if (landmark.confidenceImage == 0)
                            graphics.DrawString("x", font, lowConfidenceBrush, point);
                        else
                            graphics.DrawString("•", font, brush, point);
                    }
                }
            }
        }

        public void DrawPose(PXCMFaceData.Face face)
        {
            Debug.Assert(face != null);
            PXCMFaceData.PoseEulerAngles poseAngles;
            PXCMFaceData.PoseData pdata = face.QueryPose();
            if (pdata == null)
            {
                return;
            }
            if (!Pose.Checked || !pdata.QueryPoseAngles(out poseAngles)) return;

            lock (m_bitmapLock)
            {
                using (Graphics graphics = Graphics.FromImage(m_bitmap))
                using (var brush = new SolidBrush(m_faceTextOrganizer.Colour))
                using (var font = new Font(FontFamily.GenericMonospace, m_faceTextOrganizer.FontSize, FontStyle.Bold))
                {
                    string yawText = String.Format("Yaw = {0}",
                        Convert.ToInt32(poseAngles.yaw).ToString(CultureInfo.InvariantCulture));
                    graphics.DrawString(yawText, font, brush, m_faceTextOrganizer.PoseLocation.X,
                        m_faceTextOrganizer.PoseLocation.Y);

                    string pitchText = String.Format("Pitch = {0}",
                        Convert.ToInt32(poseAngles.pitch).ToString(CultureInfo.InvariantCulture));
                    graphics.DrawString(pitchText, font, brush, m_faceTextOrganizer.PoseLocation.X,
                        m_faceTextOrganizer.PoseLocation.Y + m_faceTextOrganizer.FontSize);

                    string rollText = String.Format("Roll = {0}",
                        Convert.ToInt32(poseAngles.roll).ToString(CultureInfo.InvariantCulture));
                    graphics.DrawString(rollText, font, brush, m_faceTextOrganizer.PoseLocation.X,
                        m_faceTextOrganizer.PoseLocation.Y + 2 * m_faceTextOrganizer.FontSize);
                }
            }
        }

        public void DrawExpressions(PXCMFaceData.Face face)
        {
            Debug.Assert(face != null);
            if (m_bitmap == null || !Expressions.Checked) return;

            PXCMFaceData.ExpressionsData expressionsOutput = face.QueryExpressions();

            if (expressionsOutput == null) return;

            lock (m_bitmapLock)
            {
                using (Graphics graphics = Graphics.FromImage(m_bitmap))
                using (var brush = new SolidBrush(m_faceTextOrganizer.Colour))
                {
                    const int imageSizeWidth = 18;
                    const int imageSizeHeight = 18;

                    int positionX = m_faceTextOrganizer.ExpressionsLocation.X;
                    int positionXText = positionX + imageSizeWidth;
                    int positionY = m_faceTextOrganizer.ExpressionsLocation.Y;
                    int positionYText = positionY + imageSizeHeight / 4;

                    foreach (var expressionEntry in m_expressionDictionary)
                    {
                        PXCMFaceData.ExpressionsData.FaceExpression expression = expressionEntry.Key;
                        PXCMFaceData.ExpressionsData.FaceExpressionResult result;
                        bool status = expressionsOutput.QueryExpression(expression, out result);
                        if (!status) continue;

                        if (expressionEntry.Value.Equals(@"MouthOpen"))
                        {
                            if (result.intensity > 50)
                            {
                                yawn++;
                            }
                        }

                        if (expressionEntry.Value.Equals(@"Eyes_Closed_Left") || expressionEntry.Value.Equals(@"Eyes_Closed_Right"))
                        {
                            if (result.intensity > 30)
                            {
                                eyeclose++;
                            }
                        }

                        if (expressionEntry.Value.Equals(@"Eyes_Turn_Right") || expressionEntry.Value.Equals(@"Eyes_Turn_Left"))
                        {
                            if (result.intensity > 30)
                            {
                                lookaway++;
                            }
                        }

                        Bitmap cachedExpressionBitmap;
                        bool hasCachedExpressionBitmap = m_cachedExpressions.TryGetValue(expression, out cachedExpressionBitmap);
                        if (!hasCachedExpressionBitmap)
                        {
                            cachedExpressionBitmap = (Bitmap) m_resources.GetObject(expressionEntry.Value);
                            m_cachedExpressions.Add(expression, cachedExpressionBitmap);
                        }

                        using (var font = new Font(FontFamily.GenericMonospace, m_faceTextOrganizer.FontSize, FontStyle.Bold))
                        {
                            graphics.DrawImage(cachedExpressionBitmap, new Rectangle(positionX, positionY, imageSizeWidth, imageSizeHeight));
                            string expressionText = String.Format("= {0}", result.intensity);
                            graphics.DrawString(expressionText, font, brush, positionXText, positionYText);

                            positionY += imageSizeHeight;
                            positionYText += imageSizeHeight;
                        }
                    }
                }
            }
        }

        public void DrawRecognition(PXCMFaceData.Face face)
        {
            Debug.Assert(face != null);
            if (m_bitmap == null || !Recognition.Checked) return;

            PXCMFaceData.RecognitionData qrecognition = face.QueryRecognition();
            if (qrecognition == null)
            {
                throw new Exception(" PXCMFaceData.RecognitionData null");
            }
            int userId = qrecognition.QueryUserID();
            string recognitionText = userId == -1 ? "Not Registered" : String.Format("Registered ID: {0}", userId);

            lock (m_bitmapLock)
            {
                using (Graphics graphics = Graphics.FromImage(m_bitmap))
                using (var brush = new SolidBrush(m_faceTextOrganizer.Colour))
                using (var font = new Font(FontFamily.GenericMonospace, m_faceTextOrganizer.FontSize, FontStyle.Bold))
                {
                    graphics.DrawString(recognitionText, font, brush, m_faceTextOrganizer.RecognitionLocation);
                }
            }
        }

        #endregion

        private delegate void DoTrackingCompleted();

        private delegate void UpdatePanelDelegate();

        private delegate void UpdateStatusDelegate(string status);

        private void Detection_CheckedChanged(object sender, EventArgs e)
        {
            NumDetectionText.Enabled = Detection.Checked;
        }

        private void Landmarks_CheckedChanged(object sender, EventArgs e)
        {
            NumLandmarksText.Enabled = Landmarks.Checked;
        }

        private void Pose_CheckedChanged(object sender, EventArgs e)
        {
            NumPoseText.Enabled = Pose.Checked;
        }

        private void Expressions_CheckedChanged(object sender, EventArgs e)
        {
            NumExpressionsText.Enabled = Expressions.Checked;
        }

        public bool IsDetectionEnabled()
        {
            return Detection.Checked;
        }

        public bool IsLandmarksEnabled()
        {
            return Landmarks.Checked;
        }

        public bool IsPoseEnabled()
        {
            return Pose.Checked;
        }

        public bool IsExpressionsEnabled()
        {
            return Expressions.Checked;
        }

        public bool IsMirrored()
        {
            return Mirror.Checked;
        }

    }
}
