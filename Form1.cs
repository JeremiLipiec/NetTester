using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Net_Tester
{
    struct PingResult
    {
        public string target;
        public string result;
        public int time;
    }

    enum PingState
    {
        Paused,
        Pinging,
        NetworkError
    }

    public partial class Form1 : Form
    {
        PingState _state = PingState.Paused;
        PingState State {
            get => _state;
            set {
                _state = value;

                if(value == PingState.Pinging)
                    button_start.Text = "Stop";
                else if (value == PingState.Paused)
                    button_start.Text = "Start";

                label_status.Text = $"Status: {State}";

                UpdateBackColor();
            }
        }

        readonly string pingAdress = "8.8.8.8";
        readonly string dayFilePath = "dailyHistory";
        readonly string folderPath = "tests";
        readonly string extension = ".txt";
        readonly string today = "";

        PingResult[] history;
        readonly int historyLimit = 5;

        Ping test = new Ping();

        public Form1() {
            InitializeComponent();
            ShowInTaskbar = false;
            notifyIcon.Visible = true;

            history = new PingResult[historyLimit];

            today = DateTime.Now.Date.ToShortDateString();
            MainPanel.BackColor = Color.Gray;
        }

        private void button_start_Click(object sender, EventArgs e) {
            if (State == PingState.Paused)
                State = PingState.Pinging;
            else State = PingState.Paused;
        }

        void CheckPing() {
            if (State == PingState.Paused) return;
            PingReply reply;

            try {
                reply = test.Send(pingAdress);
            } catch {
                State = PingState.NetworkError;
                return;
            }

            PingResult result = new PingResult() {
                target = pingAdress
            };

            if (reply.Status == IPStatus.Success) {
                result.result = $"{reply.RoundtripTime} ms";
                result.time = (int)reply.RoundtripTime;
                State = PingState.Pinging;
            } else {
                result.result = "Net error";
                State = PingState.NetworkError;
                result.time = -1;
            }

            AddToHistory(result);
            Save();
            UpdateGUI();
        }

        void AddToHistory(PingResult result) {
            PingResult[] demo = new PingResult[history.Length];
            demo[0] = result;

            for (int i = 1; i < history.Length; i++) {
                demo[i] = history[i - 1];
            }

            for (int i = 0; i < history.Length; i++) {
                history[i] = demo[i];
            }

            UpdateBackColor();
        }

        void UpdateBackColor() {
            switch (State) {
                case PingState.Paused: {
                    MainPanel.BackColor = Color.Gray;
                    return;
                }
                case PingState.NetworkError: {
                    MainPanel.BackColor = Color.Red;
                    return;
                }
            }

            int lastPing = history[0].time;

            if (lastPing < 100) {
                MainPanel.BackColor = Color.Green;
            } else if (lastPing < 250) {
                MainPanel.BackColor = Color.Yellow;
            } else {
                MainPanel.BackColor = Color.Red;
            }
        }

        float CalculateAvgPing() {
            float result = 0;
            int fullness = 0;

            for (int i = 0; i < history.Length; i++) {
                if (history[i].time > 0) {
                    fullness++;
                    result += history[i].time;
                }
            }
            return result / fullness;
        }

        void Save() {
            try {
                if (!Directory.Exists(folderPath)) {
                    Directory.CreateDirectory(folderPath);
                }

                // to history
                StreamWriter sw = File.AppendText(folderPath + "/" + dayFilePath + today + extension);
                sw.WriteLine(history[0].time);
                sw.Close();
            } catch { } // just skip
        }

        private void timer_ping_Tick(object sender, EventArgs e) => CheckPing();

        private void notifyIcon_DoubleClick(object sender, EventArgs e) {
            WindowState = FormWindowState.Normal;
            Show();
            Activate();
        }

        void UpdateGUI() {
            label_avgPing.Text = $"Average: " + CalculateAvgPing().ToString("n0") + "ms";

            //update last
            last_text.Text = history[0].result;

            // update history
            string result = "";
            for (int i = 0; i < history.Length; i++)
                result += history[i].result + "\n";
            result_text.Text = result;
        }


        //mouse move
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void panel2_MouseMove(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void button1_Click(object sender, EventArgs e) => Application.Exit();

        private void button2_Click(object sender, EventArgs e) {
            WindowState = FormWindowState.Minimized;
        }
    }
}
