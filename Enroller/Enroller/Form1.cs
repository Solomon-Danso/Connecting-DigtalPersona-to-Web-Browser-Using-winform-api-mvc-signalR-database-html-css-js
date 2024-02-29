using DPUruNet;
using DPXUru;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using UareUSampleCSharp;

namespace Enroller
{
    public partial class Form1 : Form
    {
        private HubConnection _connection;
        private static readonly Random random = new Random();
        private const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public Form1()
        {
            InitializeComponent();
        }


        public Dictionary<int, Fmd> Fmds
        {
            get { return fmds; }
            set { fmds = value; }
        }
        private Dictionary<int, Fmd> fmds = new Dictionary<int, Fmd>();

        public bool Reset
        {
            get { return reset; }
            set { reset = value; }
        }
        private bool reset;


        private enum Action
        {
            UpdateReaderState,
            SendBitmap,
            SendMessage
        }
        private delegate void SendMessageCallback(Action state, object payload);

        private void SendMessage(Action action, object payload)
        {


            try
            {

                if (this.pictureBox1.InvokeRequired)
                {
                    SendMessageCallback d = new SendMessageCallback(SendMessage);
                    this.Invoke(d, new object[] { action, payload });
                }
                else
                {
                    switch (action)
                    {
                        case Action.SendMessage:
                            MessageBox.Show((string)payload);
                            break;
                        case Action.SendBitmap:
                            pictureBox1.Image = (Bitmap)payload;
                            pictureBox1.Refresh();
                            break;
                    }
                }



            }
            catch (Exception)
            {
            }


        }


        private Reader reader;

        private ReaderSelection readerSelection;


        public bool StartCaptureAsync(Reader.CaptureCallback OnCaptured)
        {
            using (Tracer tracer = new Tracer("Form_Main::StartCaptureAsync"))
            {
                // Activate capture handler
                currentReader.On_Captured += new Reader.CaptureCallback(OnCaptured);

                // Call capture
                if (!CaptureFingerAsync())
                {
                    return false;
                }

                return true;
            }
        }

        public void GetStatus()
        {
            using (Tracer tracer = new Tracer("Form_Main::GetStatus"))
            {
                Constants.ResultCode result = currentReader.GetStatus();

                if ((result != Constants.ResultCode.DP_SUCCESS))
                {
                    reset = true;
                    throw new Exception("" + result);
                }

                if ((currentReader.Status.Status == Constants.ReaderStatuses.DP_STATUS_BUSY))
                {
                    Thread.Sleep(50);
                }
                else if ((currentReader.Status.Status == Constants.ReaderStatuses.DP_STATUS_NEED_CALIBRATION))
                {
                    currentReader.Calibrate();
                }
                else if ((currentReader.Status.Status != Constants.ReaderStatuses.DP_STATUS_READY))
                {
                    throw new Exception("Reader Status - " + currentReader.Status.Status);
                }
            }
        }

        public bool CaptureFingerAsync()
        {
            using (Tracer tracer = new Tracer("Form_Main::CaptureFingerAsync"))
            {
                try
                {
                    GetStatus();

                    Constants.ResultCode captureResult = currentReader.CaptureAsync(Constants.Formats.Fid.ANSI, Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT, currentReader.Capabilities.Resolutions[0]);
                    if (captureResult != Constants.ResultCode.DP_SUCCESS)
                    {
                        reset = true;
                        throw new Exception("" + captureResult);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error:  " + ex.Message);
                    return false;
                }
            }
        }

        public void CancelCaptureAndCloseReader(Reader.CaptureCallback OnCaptured)
        {
            using (Tracer tracer = new Tracer("Form_Main::CancelCaptureAndCloseReader"))
            {
                if (currentReader != null)
                {
                    currentReader.CancelCapture();

                    // Dispose of reader handle and unhook reader events.
                    currentReader.Dispose();

                    if (reset)
                    {
                        CurrentReader = null;
                    }
                }
            }
        }
        // When set by child forms, shows s/n and enables buttons.
        private Reader currentReader;
        public Reader CurrentReader
        {
            get { return currentReader; }
            set
            {
                currentReader = value;
                SendMessage(Action.UpdateReaderState, value);
            }
        }

        private ReaderCollection _readers;
        private void LoadScanners()
        {
            comboReaders.Text = string.Empty;
            comboReaders.Items.Clear();
            comboReaders.SelectedIndex = -1;

            try
            {
                _readers = ReaderCollection.GetReaders();

                foreach (Reader Reader in _readers)
                {
                    comboReaders.Items.Add(Reader.Description.Name);
                }

                if (comboReaders.Items.Count > 0)
                {
                    comboReaders.SelectedIndex = 0;
                    //btnCaps.Enabled = true;
                    //btnSelect.Enabled = true;
                }
                else
                {
                    //btnSelect.Enabled = false;
                    //btnCaps.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                //message box:
                String text = ex.Message;
                text += "\r\n\r\nPlease check if DigitalPersona service has been started";
                String caption = "Cannot access readers";
                MessageBox.Show(text, caption);
            }
        }




        private async void Form1_Load(object sender, EventArgs e)
        {


            LoadScanners();
            firstFinger = null;
            resultEnrollment = null;
            preenrollmentFmds = new List<Fmd>();
            pictureBox1.Image = null;
            if (CurrentReader != null)
            {
                CurrentReader.Dispose();
                CurrentReader = null;
            }
            CurrentReader = _readers[comboReaders.SelectedIndex];
            if (!OpenReader())
            {
                //this.Close();
            }

            if (!StartCaptureAsync(this.OnCaptured))
            {
                //this.Close();
            }



            _connection = new HubConnectionBuilder()
          .WithUrl("http://localhost:5161/chatHub") // URL of your SignalR hub
          .Build();

           

            _connection.On<List<string>>("ConnectionList", (connectionIds) =>
            {
                UpdateConnectionList(connectionIds);
            });






            try
            {
                await _connection.StartAsync(); // Start the connection
                MessageBox.Show("Successfully Connected To SignalR");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The Error: {ex.Message}");
            }







        }


        public bool OpenReader()
        {
            using (Tracer tracer = new Tracer("Form_Main::OpenReader"))
            {
                reset = false;
                Constants.ResultCode result = Constants.ResultCode.DP_DEVICE_FAILURE;

                if (currentReader == null)
                {
                    MessageBox.Show("No scanner connected");
                    Environment.Exit(0); // Close the application immediately
                }

                // Open reader
                result = currentReader.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);

                
                
                
                if (result != Constants.ResultCode.DP_SUCCESS)
                {
                    MessageBox.Show("Error:  " + result);
                    reset = true;
                    return false;
                }

                return true;
            }
        }




        private void UpdateConnectionList(List<string> connectionIds)
        {
            if (comboUsers.InvokeRequired)
            {
                comboUsers.Invoke(new Action<List<string>>(UpdateConnectionList), new object[] { connectionIds });
                return;
            }

            // Clear existing items
            comboUsers.Items.Clear();

            // Add new items
            foreach (var connectionId in connectionIds)
            {
                comboUsers.Items.Add(connectionId);
            }
        }




        public bool CheckCaptureResult(CaptureResult captureResult)
        {
            using (Tracer tracer = new Tracer("Form_Main::CheckCaptureResult"))
            {
                if (captureResult.Data == null || captureResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
                {
                    if (captureResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
                    {
                        reset = true;
                        throw new Exception(captureResult.ResultCode.ToString());
                    }

                    // Send message if quality shows fake finger
                    if ((captureResult.Quality != Constants.CaptureQuality.DP_QUALITY_CANCELED))
                    {
                        throw new Exception("Quality - " + captureResult.Quality);
                    }
                    return false;
                }

                resultConversion = FeatureExtraction.CreateFmdFromFid(captureResult.Data, Constants.Formats.Fmd.ANSI);

                return true;
            }
        }
        private const int PROBABILITY_ONE = 0x7fffffff;
        private Fmd firstFinger;
        int count = 0;
        DataResult<Fmd> resultEnrollment;
        List<Fmd> preenrollmentFmds;
        DataResult<Fmd> resultConversion;





public byte[] BitmapToByteArray(Bitmap bitmap)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
            return stream.ToArray();
        }
    }


        private async Task SendImageToApi(string token, byte[] imageData)
        {
            try
            {
                // Create HTTP client
                using (HttpClient client = new HttpClient())
                {
                    // Construct the request URL with token parameter
                    string apiUrl = "http://localhost:5286/api/Chat/JustSendTheApi?Token=" + token;

                    // Create a multipart form data content
                    MultipartFormDataContent formData = new MultipartFormDataContent();

                    // Create a stream content from the byte array
                    StreamContent imageContent = new StreamContent(new MemoryStream(imageData));

                    // Add the image file to the form data content
                    formData.Add(imageContent, "imageFile", "imageFile");

                    // Send the POST request to the API
                    HttpResponseMessage response = await client.PostAsync(apiUrl, formData);

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        //MessageBox.Show("Api Working");
                    }
                    else
                    {
                        MessageBox.Show("Api connection is not established");
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception if needed
                SendMessage(Action.SendMessage, "Error sending image to API: " + ex.Message);
            }
        }


        public static string Generate(int length)
        {
            var result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                result.Append(characters[random.Next(characters.Length)]);
            }
            return result.ToString();
        }


        public async Task Sender(string ApiServer)
        {
            try
            {
                // Check if the connection is not active
                if (_connection.State != HubConnectionState.Connected)
                {
                    MessageBox.Show("Connection is not active. Please wait for the connection to establish.");
                    return;
                }

                // Execute this code on the UI thread
                if (InvokeRequired)
                {
                    Invoke(new System.Action(async () => await Sender(ApiServer))); // Use fully qualified name for Action
                    return;
                }

                string selectedConnectionId = comboUsers.SelectedItem?.ToString(); // Get the selected connection ID from the ComboBox

                // Check if a connection ID is selected
                if (string.IsNullOrWhiteSpace(selectedConnectionId))
                {
                    MessageBox.Show("Select a connection Id");
                    return;
                }
                Console.WriteLine(selectedConnectionId);

               
                await _connection.SendAsync("SendApiLink", selectedConnectionId, ApiServer);
            }
            catch (Exception ex)
            {
                // Handle any exceptions here
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }



        public async void OnCaptured(CaptureResult captureResult)
        {
            try
            {
                // Check capture quality and throw an error if bad.
                if (!CheckCaptureResult(captureResult)) return;

                // Create bitmap
                foreach (Fid.Fiv fiv in captureResult.Data.Views)
                {
                    SendMessage(Action.SendBitmap, CreateBitmap(fiv.RawImage, fiv.Width, fiv.Height));
                   
                    Bitmap bitmap = CreateBitmap(fiv.RawImage, fiv.Width, fiv.Height);
                    byte[] bitmapBytes = BitmapToByteArray(bitmap);
                    string base64String = Convert.ToBase64String(bitmapBytes);


                    Console.WriteLine(base64String);

                    var token = Generate(10);


                    await SendImageToApi(token, bitmapBytes);

                    var apiServer = "http://localhost:5286/api/Chat/JustGetTheApi?Token=" + token;

                    await Sender(apiServer);








                }




                //Enrollment Code:
                try
                {
                    count++;
                    // Check capture quality and throw an error if bad.
                    DataResult<Fmd> resultConversion = FeatureExtraction.CreateFmdFromFid(captureResult.Data, Constants.Formats.Fmd.ANSI);

                    MessageBox.Show("A finger was captured.  \r\nCount:  " + (count));

                    if (resultConversion.ResultCode != Constants.ResultCode.DP_SUCCESS)
                    {
                        Reset = true;
                        throw new Exception(resultConversion.ResultCode.ToString());
                    }

                    preenrollmentFmds.Add(resultConversion.Data);

                    if (count >= 4)
                    {
                        resultEnrollment = DPUruNet.Enrollment.CreateEnrollmentFmd(Constants.Formats.Fmd.ANSI, preenrollmentFmds);

                        if (resultEnrollment.ResultCode == Constants.ResultCode.DP_SUCCESS)
                        {
                            preenrollmentFmds.Clear();
                            count = 0;
                            //obj_bal_ForAll.BAL_StoreCustomerFPData("tbl_Finger", txtledgerId.Text, Fmd.SerializeXml(resultEnrollment.Data));
                            MessageBox.Show("User Finger Print was successfully enrolled.");
                            return;
                        }
                        else if (resultEnrollment.ResultCode == Constants.ResultCode.DP_ENROLLMENT_INVALID_SET)
                        {
                            SendMessage(Action.SendMessage, "Enrollment was unsuccessful.  Please try again.");
                            preenrollmentFmds.Clear();
                            count = 0;
                            return;
                        }
                    }
                    MessageBox.Show("Now place the same finger on the reader.");
                }
                catch (Exception ex)
                {
                    // Send error message, then close form
                    SendMessage(Action.SendMessage, "Error:  " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                // Send error message, then close form
                SendMessage(Action.SendMessage, "Error:  " + ex.Message);
            }
        }


        public Bitmap CreateBitmap(byte[] bytes, int width, int height)
        {
            byte[] rgbBytes = new byte[bytes.Length * 3];

            for (int i = 0; i <= bytes.Length - 1; i++)
            {
                rgbBytes[(i * 3)] = bytes[i];
                rgbBytes[(i * 3) + 1] = bytes[i];
                rgbBytes[(i * 3) + 2] = bytes[i];
            }
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            for (int i = 0; i <= bmp.Height - 1; i++)
            {
                IntPtr p = new IntPtr(data.Scan0.ToInt64() + data.Stride * i);
                System.Runtime.InteropServices.Marshal.Copy(rgbBytes, i * bmp.Width * 3, p, bmp.Width * 3);
            }

            bmp.UnlockBits(data);

            return bmp;
        }

        public SqlConnection conn = new SqlConnection("Server=172.16.68.1,1433;Database=FingerPrintDb;User=sa;Password=HydotTech;TrustServerCertificate=true;");


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            CancelCaptureAndCloseReader(this.OnCaptured);
        }




        private void btnEnroll_Click(object sender, EventArgs e)
        {

            if (resultEnrollment != null && count <= 4)
            {





                try
                {
                    conn.Close();
                    conn.Open();
                     SqlCommand cmd = new SqlCommand("Insert Into " + "tblFinger" + " (LedgerId, CustomerFinger) VALUES('" + txtLedgerId.Text.ToString() + "', '" + Fmd.SerializeXml(resultEnrollment.Data) + "')", conn);

                    
                    cmd.ExecuteNonQuery();
                    conn.Close();
                    MessageBox.Show("Saved Successfully, Now enroll a different user ");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }


            }
            else
            {
                MessageBox.Show("Ensure you have enrolled 4 instances of the same fingerprint ");
            }


        }

        private void btnSignal_Click(object sender, EventArgs e)
        {

        }
    }
}
