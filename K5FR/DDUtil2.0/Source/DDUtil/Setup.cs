#define WIN64
#define WIN32
//==
// Setup.cs
//==
// Copyright (C) 2007-2011  Steve Nance - K5FR
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 3
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// You may contact the author via email at: snance@charter.net
//==

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Media;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml.XPath;
using DataDecoder.Properties;
using Logger;
using NDde.Client;
using FT_HANDLE = System.UInt32;
using Microsoft.Win32;              // For RegKey
using MRG.Controls.UI;
using RIOX;
using DDUtilState;

namespace DataDecoder
{
    public partial class Setup : Form
    {
        #region Enums

        public enum RotorMod
        { AlphaSpid, GreenHeron, Hygain, M2R2800PA, M2R2800PX, Prosistel, Yaesu };

        public enum SerialError
        { Frame, Overrun, RXOver, RXParity, TXFull };

        public enum PortMode
        { None, Kenwood, YaesuTypeI, YaesuTypeII, Icom, SO2Rxlat };

        public enum Parity
        { Even, Mark, None, Odd, Space };

        public enum StopBits
        { None, One, OnePointFive, Two };

        public enum Ports
        { CAT, RCP1, RCP2, RCP3, RCP4, RCP5 };

        private enum TempFormat
        { Celsius = 0, Fahrenheit, }

        #endregion Enums

        #region Variables

        private SplashScreen mSplashScreen;
        ASCIIEncoding AE = new ASCIIEncoding();
        public CATSerialPorts.CATSerialPort sp;
        DataSet ds;
        DataSet dsm = new DataSet();
        Mini mini;
        winRotor rtr;
        A9500 a95;
        SoundPlayer wav = new SoundPlayer();
        WN2Matrix wn;
        Hashtable flist = new Hashtable();
        Settings set = Settings.Default;
        PortMode portmode;
        RotorMod rotormod;// = new RotorMod();
        private TempFormat temp_format = TempFormat.Celsius;
        private bool closing = false;
        bool enableErrorLog = false;
        bool formLoaded = false;
        int keyValue = 0;
        int LPTnum = 0;         // decimal number of selected LPT port
        int iSleep = 0;         // Thread.Sleep var
        int StepCtr = 0;        // reps counter
        int reps = 8;           // how many times to test the SteppIR port
        double pollInt = 0;     // CAT port interval timer uses txtInv text box
        double temp = 0;
        string fileName = Application.StartupPath + "\\BandData.xml";
        string MacFileName = Application.StartupPath + "\\MacroData.xml";
        string LastFreq = "";
        string LastMode = "";
        string OutBuffer = "";
        string str = "";
        string vfo = "";
        string band = " ";
        string newVerAvail = "";
        public static string _os = "";
        public static string ver = Application.ProductVersion;
        public static int errCtr = 0;
        public static string psdrVer = "";
       // System.Timers.Timer A95Timer;
        System.Timers.Timer AlphaTimer;
        System.Timers.Timer AtTimer;
        System.Timers.Timer blinkTimer;
        System.Timers.Timer comTimer;
        System.Timers.Timer lpTimer;
        System.Timers.Timer pollTimer;
        System.Timers.Timer propTimer;
        System.Timers.Timer RepsTimer;
        System.Timers.Timer SPETimer;
        System.Timers.Timer StepTimer;
        System.Timers.Timer WatchDog;
        #endregion Variables

        #region Properties

        public bool WN1Enab
        {
            get { return rbWN1.Checked; }
            set { rbWN1.Checked = value; }
        }
        public bool WN2Enab
        {
            get { return rbWN2.Checked; }
            set { rbWN2.Checked = value; }
        }
        public bool WN3Enab
        {
            get { return rbWN3.Checked; }
            set { rbWN3.Checked = value; }
        }
        public bool WN4Enab
        {
            get { return rbWN4.Checked; }
            set { rbWN4.Checked = value; }
        }

        public bool AudioOn
        {
            get { return chkAudio.Checked; }
            set { chkAudio.Checked = value; }
        }
        public bool RotorEnab
        {
            get { return chkRotorEnab.Checked; }
            set { chkRotorEnab.Checked = value; }
        }
        public bool StepEnab
        {
            get { return chkStep.Checked; }
            set { chkStep.Checked = value; }
        }
        public bool StepFwd
        {
            get { return rbFwd.Checked; }
            set { rbFwd.Checked = value; }
        }
        public bool StepRev
        {
            get { return rb180.Checked; }
            set { rb180.Checked = value; }
        }
        public bool StepBI
        {
            get { return rbBiDir.Checked; }
            set { rbBiDir.Checked = value; }
        }
        public bool Step34
        {
            get { return rb34.Checked; }
            set { rb34.Checked = value; }
        }
        public bool DisableToolTips
        {
            set { toolTip1.Active = value; }
        }
        /// <summary>
        /// The default radio serial port
        /// </summary>
        private string default_ComRadio = "";
        public string DefaultComRadio
        {
            get { return default_ComRadio; }
            set { default_ComRadio = value; }
        }
        /// <summary>
        /// The default accessory serial port
        /// </summary>
        private string default_ComAcc = "";
        public string DefaultComAcc
        {
            get { return default_ComAcc; }
            set { default_ComAcc = value; }
        }
        /// <summary>
        /// The default Logger (RCP1) port
        /// </summary>
        private string default_LogPort = "";
        public string DefaultLogPort
        {
            get { return default_LogPort; }
            set { default_LogPort = value; }
        }
        /// <summary>
        /// The default LP100 port
        /// </summary>
        private string default_LPport = "";
        public string DefaultLPport
        {
            get { return default_LPport; }
            set { default_LPport = value; }
        }
        /// <summary>
        /// The default SteppIR port
        /// </summary>
        private string default_StepPort = "";
        public string DefaultStepPort
        {
            get { return default_StepPort; }
            set { default_StepPort = value; }
        }
        /// <summary>
        /// The default RCP2 port
        /// </summary>
        private string default_rcp2 = "";
        public string DefaultRCP2port
        {
            get { return default_rcp2; }
            set { default_rcp2 = value; }
        }
        /// <summary>
        /// The default RCP3 port
        /// </summary>
        private string default_RCP3 = "";
        public string DefaultRCP3port
        {
            get { return default_RCP3; }
            set { default_RCP3 = value; }
        }
        /// <summary>
        /// The default RCP4 port
        /// </summary>
        private string default_RCP4 = "";
        public string DefaultRCP4port
        {
            get { return default_RCP4; }
            set { default_RCP4 = value; }
        }
        /// <summary>
        /// The default PW1 port
        /// </summary>
        private string default_PW1 = "";
        public string DefaultPW1port
        {
            get { return default_PW1; }
            set { default_PW1 = value; }
        }
        /// <summary>
        /// The default parallel port
        /// </summary>
        private string default_lpt = "";
        public string DefaultLPT
        {
            get { return default_lpt; }
            set { default_lpt = value; }
        }
        #endregion Properties

        #region Initialization

        public Setup(SplashScreen splash)
        {
            // get the platform (32/64)
            OperatingSystem os = Environment.OSVersion;
            if (Directory.Exists(Environment.GetEnvironmentVariable("ProgramFiles(x86)")))
                _os = "64";
            else
                _os = "32";

            mSplashScreen = splash;
            mSplashScreen.SetProgress("Initializing Components", 0.0);
            // if the app is already running don't start another one.
            if (IsAppAlreadyRunning()) Environment.Exit(0);
            InitializeComponent();
            mini = new Mini(this);
            rtr = new winRotor(this);
            a95 = new A9500(this);
            wn = new WN2Matrix(this);
            mSplashScreen.SetProgress("Restoring Personal Settings", 0.2);

            // Restore window size and position 
            // this is the default
            this.WindowState = FormWindowState.Normal;
            this.StartPosition = FormStartPosition.WindowsDefaultBounds;

            // check if the saved bounds are nonzero and visible on any screen
            if (Settings.Default.WindowPosition != Rectangle.Empty &&
                IsVisibleOnAnyScreen(Settings.Default.WindowPosition))
            {
                // first set the bounds
                this.StartPosition = FormStartPosition.Manual;
                this.DesktopBounds = Settings.Default.WindowPosition;

                // afterwards set the window state to the saved value (which could be Maximized)
                this.WindowState = Settings.Default.WindowState;
            }
            else
            {
                // this resets the upper left corner of the window to windows standards
                this.StartPosition = FormStartPosition.WindowsDefaultLocation;

                // we can still apply the saved size
                if (Settings.Default.WindowPosition != Rectangle.Empty)
                {
                    this.Size = Settings.Default.WindowPosition.Size;
                }
            }
            windowInitialized = true;

            chkDevice.Checked = set.DevEnab;
            chkDev0.Checked = set.Dev0Enab;
            txtRCP.Text = set.RCPtext;
            txtSleep.Text = set.Sleep;
            iSleep = Convert.ToInt32(set.Sleep);
            rbFwd.Checked = true;
            mini.rbFwd.Checked = true;
            chkTips.Checked = set.ToolTips;
            chkMode.Checked = set.slaveMode;
            chkOnTop.Checked = set.MainOnTop;

            chkDog.Checked = set.DogEnab;
            chkDog_CheckedChanged(null, null);

            mSplashScreen.SetProgress("Setting up Timers", 0.4);

            txtTimer.Text = set.DogTime;
            txtCall.Text = set.Call;
            PortAccess.Output(LPTnum, 0);
            chkAudio.Checked = set.AudioOn;
            if (chkAudio.Checked) { Notification.useAudio = true; }
            else { Notification.useAudio = false; }

            //setup radio port timer
            pollTimer = new System.Timers.Timer();
            pollTimer.Elapsed += new System.Timers.ElapsedEventHandler(pollTimer_Elapsed);
            str = set.pollIntvl;
            txtInv.Text = str;
            pollInt = Convert.ToDouble(set.pollIntvl);
            if (pollInt == 0) pollInt = 1000.0;
            pollTimer.Interval = pollInt;  // 1000 = 1 second
            pollTimer.Enabled = false;

            // setup ACOM Blink Timer for a 1 second interrupt
            blinkTimer = new System.Timers.Timer();
            blinkTimer.Elapsed += new System.Timers.ElapsedEventHandler(blinkTimer_Elapsed);
            blinkTimer.Interval = 1000;      // 1000 = 1 seconds
            blinkTimer.Enabled = false;

            // setup ACOM Auto Tune Timer for a 5 second interrupt
            AtTimer = new System.Timers.Timer();
            AtTimer.Elapsed += new System.Timers.ElapsedEventHandler(AtTimer_Elapsed);
            AtTimer.Interval = 5000;      // 1000 = 1 seconds
            AtTimer.Enabled = false;

            // setup LP100 Port Timer
            lpTimer = new System.Timers.Timer();
            lpTimer.Elapsed += new System.Timers.ElapsedEventHandler(lpTimer_Elapsed);
            lpTimer.Interval = Convert.ToDouble(set.LPint);
            lpTimer.Enabled = false;

            // setup Rotor Reps Timer
            RepsTimer = new System.Timers.Timer();
            RepsTimer.Elapsed += new System.Timers.ElapsedEventHandler(RtrReps_Elapsed);
            RepsTimer.Interval = 1000;
            RepsTimer.Enabled = false;

            /* setup pw1 test timer; to be removed after testing complete */
            pw1Timer = new System.Timers.Timer();
            pw1Timer.Elapsed += new System.Timers.ElapsedEventHandler(pw1Timer_Elapsed);
            pw1Timer.Interval = 2500;  // 1000 = 1 second
            pw1Timer.Enabled = false;

            // setup SteppIR Data Timer
            StepTimer = new System.Timers.Timer();
            StepTimer.Elapsed += new System.Timers.ElapsedEventHandler(StepTimer_Elapsed);
            StepTimer.Interval = Convert.ToDouble(set.StepInv);
            StepTimer.Enabled = false;

            // setup SteppIR Data Timer
            WatchDog = new System.Timers.Timer();
            WatchDog.Elapsed += new System.Timers.ElapsedEventHandler(WatchDog_Elapsed);
            WatchDog.Interval = Convert.ToDouble(set.DogTime) * 60000;
            WatchDog.Enabled = false;

            /* setup SPE test timer; to be removed after testing complete */
            SPETimer = new System.Timers.Timer();
            SPETimer.Elapsed += new System.Timers.ElapsedEventHandler(SPETimer_Elapsed);
            SPETimer.Interval = 500;  // 1000 = 1 second
            SPETimer.Enabled = false;

            ///* setup A95 test timer; to be removed after testing complete */
            //A95Timer = new System.Timers.Timer();
            //A95Timer.Elapsed += new System.Timers.ElapsedEventHandler(A95Timer_Elapsed);
            //A95Timer.Interval = 1000;  // 1000 = 1 second
            //A95Timer.Enabled = false;
            // setup Alpha Amp 
            AlphaTimer = new System.Timers.Timer();
            AlphaTimer.Elapsed += new System.Timers.ElapsedEventHandler(AlphaTimer_Elapsed);
            AlphaTimer.Interval = Convert.ToDouble(set.AlphaInt);
            AlphaTimer.Enabled = false;
            txtAlphaInt.Text = set.AlphaInt;
            cboAlphaBaud.SelectedIndex = set.AlphaBaud;

            // setup Propadex Timer
            propTimer = new System.Timers.Timer();
            propTimer.Elapsed += new System.Timers.ElapsedEventHandler(propTimer_Elapsed);
            propTimer.Interval = 600000;
            propTimer.Enabled = true;

            // setup CatCom Timer
            comTimer = new System.Timers.Timer();
            comTimer.Elapsed += new System.Timers.ElapsedEventHandler(comTimer_Elapsed);
            comTimer.Interval = 10000;
            comTimer.Enabled = false;

            mSplashScreen.SetProgress("Initializing Ports", 0.6);
            CreateSerialPort();
            GetPortNames();

            InitRotor();

            txtRadNum.Text = set.CIVaddr;
            cboRadData.SelectedIndex = set.RadData;

            // set Radio Serial port to the last one used
            try { cboCAT.SelectedIndex = set.RadioPort; }
            catch { cboCAT.SelectedIndex = 0; BadPort("Radio CAT"); }
            txtInv.Text = Convert.ToString(pollInt);

            //txtProfLoc.Text = set.ProfLoc;
            //mini.PFfile = set.ProfLoc;

            // set Acc Serial (passive listener) port to the last one used
            try { cboSerAcc.SelectedIndex = set.AccPort; }
            catch { cboSerAcc.SelectedIndex = 0; BadPort("Passive Listener"); }

            // set Follow Radio port
            try { cboRadio.SelectedIndex = set.followRadio; }
            catch { cboRadio.SelectedIndex = 0; BadPort("Follow Radio"); }

            chkFollow.Checked = set.followChk;
            if (chkFollow.Checked == false)
            { cboRadio.Enabled = false; chkMode.Enabled = false; }
            else
            { cboRadio.Enabled = true; chkMode.Enabled = true; }

            // set Logger Serial (RCP1) port to the last one used
            try { cboLogPort.SelectedIndex = set.LogPort; }
            catch { cboLogPort.SelectedIndex = 0; BadPort("RCP1 CAT"); }

            // set LP100 Serial port to the last one used
            try { cboLPport.SelectedIndex = set.LPportNum; }
            catch { cboLPport.SelectedIndex = 0; BadPort("LP-100"); }
            txtLPint.Text = Convert.ToString(set.LPint);
            chkLPenab.Checked = set.LPenab;
            switch (set.rb100)  // which model?
            {
                case 1: rb100.Checked = true; break;
                case 2: rb100A.Checked = true; break;
                default: break;
            }

            // set SteppIR Data Serial port to the last one used
            try { cboStep.SelectedIndex = set.StepPortNum; }
            catch { cboStep.SelectedIndex = 0; BadPort("SteppIR Data"); }
            chkStep.Checked = set.StepEnab;
            chkTI.Checked = set.chkTI;
            txtStepInv.Text = set.StepInv;
            cboStepCom.SelectedIndex = set.StepCom;

            // Set RCP2 serial port to last one used
            try { cboRCP2.SelectedIndex = set.cboRCP2; }
            catch { cboRCP2.SelectedIndex = 0; BadPort("RCP2 CAT"); }

            // Set RCP3 serial port to last one used
            try { cboRCP3.SelectedIndex = set.cboRCP3; }
            catch { cboRCP3.SelectedIndex = 0; BadPort("RCP3 CAT"); }

            // Set RCP4 serial port to last one used
            try { cboRCP4.SelectedIndex = set.cboRCP4; }
            catch { cboRCP4.SelectedIndex = 0; BadPort("RCP4 CAT"); }

            // Set RCP5 serial port to last one used
            try { cboRCP5.SelectedIndex = set.cboRCP5; }
            catch { cboRCP5.SelectedIndex = 0; BadPort("RCP5 CAT"); }

            // Set N1MM serial port to last one used
            try { cboN1MM.SelectedIndex = set.cboN1MM; }
            catch { cboN1MM.SelectedIndex = 0; BadPort("N1MM CAT"); }

            // Set PW1 serial port to last port used
            try { cboPW1.SelectedIndex = set.cboPW1; }
            catch { cboPW1.SelectedIndex = 0; BadPort("PW1 CAT"); }
            chkPW1.Checked = set.chkPW1;
            chkDisBcast.Checked = set.chkPW1db;
            txtPW1ta.Text = set.txtPW1ta;
            // Set RCP1 Rotor port to last one used
            try { cboRCP1Rotor.SelectedIndex = set.RCP1RotorPort; }
            catch { cboRCP1Rotor.SelectedIndex = 0; BadPort("RCP1 Rotor"); }

            // Set RCP2 Rotor port to last one used
            try { cboRCP2Rotor.SelectedIndex = set.RCP2RotorPort; }
            catch { cboRCP2Rotor.SelectedIndex = 0; BadPort("RCP2 Rotor"); }

            // Set RCP3 Rotor port to last one used
            try { cboRCP3Rotor.SelectedIndex = set.RCP3RotorPort; }
            catch { cboRCP3Rotor.SelectedIndex = 0; BadPort("RCP3 Rotor"); }

            // Set RCP4 Rotor port to last one used
            try { cboRCP4Rotor.SelectedIndex = set.RCP4RotorPort; }
            catch { cboRCP4Rotor.SelectedIndex = 0; BadPort("RCP4 Rotor"); }

            // Set RCP5 Rotor port to last one used
            try { cboRCP5Rotor.SelectedIndex = set.RCP5RotorPort; }
            catch { cboRCP5Rotor.SelectedIndex = 0; BadPort("RCP5 Rotor"); }

            mSplashScreen.SetProgress("Initializing Features", 0.8);

            // set LPT port to last one used
            str = set.lptPort;
            try
            {
                switch (str)
                {
                    case "FW":
                        rbFW.Checked = true; LPTnum = Convert.ToInt32(set.lptNum);
                        txtPort.Text = set.lptNum; DefaultLPT = str;
                        break;
                    case "LPT1":
                        rb1.Checked = true; LPTnum = 888; DefaultLPT = str;
                        break;
                    case "LPT2":
                        rb2.Checked = true; LPTnum = 632; DefaultLPT = str;
                        break;
                    case "LPT3":
                        rb3.Checked = true; LPTnum = 636; DefaultLPT = str;
                        break;
                    case "LPT4":
                        rb4.Checked = true; LPTnum = 620; DefaultLPT = str;
                        break;
                    case "None":
                        rbNone.Checked = true; LPTnum = 0; DefaultLPT = str;
                        break;
                    case "Other":
                        rbOther.Checked = true; LPTnum = Convert.ToInt32(set.lptNum);
                        txtPort.Text = set.lptNum; DefaultLPT = str;
                        break;
                    default:
                        rbNone.Checked = true; LPTnum = 0; DefaultLPT = "NONE";
                        chkDevice.Checked = false;
                        break;
                }
                btnPortNum.Visible = false;
                lblPortBtn.Visible = false;
                PortAccess.Output(LPTnum, 0);
                //throw new Exception("Put something here...");
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                    "Unable to write to log");
            }


            fileName = set.DataFile;       // Get saved band data file name
            txtFile0.Text = fileName;      // Put band data name to file name box
            GetBandData(fileName);         // Load band data file to grid
            MacFileName = set.MacDataFile; // Get saved macro file name
            txtMacFile.Text = MacFileName; // Put macro file name to file name box
            GetMacData(MacFileName);       // Load macro file to grid
            txtDev0.Text = set.BCD;
            cboAlpha.SelectedIndex = set.AlphaPort;
            chkAlpha.Checked = set.AlphaEnab;
            SetupRepeater();
            numSplit.Value = set.SplitNum;
            chkSlaveDTR.Checked = set.slaveDTR;
            chkSlaveRTS.Checked = set.slaveRTS;
            chkPwDTR.Checked = set.pwDTR;
            chkPwRTS.Checked = set.pwRTS;
            chkAmp160.Checked = set.Amp160;
            chkAmp80.Checked = set.Amp80;
            chkAmp40.Checked = set.Amp40;
            chkAmp30.Checked = set.Amp30;
            chkAmp20.Checked = set.Amp20;
            chkAmp17.Checked = set.Amp17;
            chkAmp15.Checked = set.Amp15;
            chkAmp12.Checked = set.Amp12;
            chkAmp10.Checked = set.Amp10;
            chkAmp6.Checked = set.Amp6;
            chkNoBdChg.Checked = set.NoBandChg;
            chkSWR.Checked = set.SWRenab;
            numSWR.Value = set.SWRnum;
            chkTenths.Checked = set.Tenths;
            cboPwrPort.SelectedIndex = set.PwrPort;
            txtVspMgr.Text = set.vspMgrFile;
            txtPSDR.Text = set.PwrSDRfile;
            chkPSDR.Checked = set.StartPSDR;
            chkAutoExpert.Checked = set.chkAutoExpert;
            txtAM.Text = set.txtAM;
            txtSSB.Text = set.txtSSB;
            txtDigi.Text = set.txtDigi;
            cboMacSwPort.SelectedIndex = set.cboMacSwPort;
            txtMacSwOn.Text = set.txtMacSwOn;
            txtMacSwOff.Text = set.txtMacSwOff;
            chkMacSwEnab.Checked = set.chkMacSwEnab;
            txtMacSwMsg.Text = set.txtMacSwMsg;

            stsTX = set.stsTX;
            stsOper = set.stsOper;
            if (stsOper)
            {
                btnByp.BackColor = Color.Lime; btnByp.Text = "OPER";
                mini.btnByp.BackColor = Color.Lime; //mini.btnByp.Text = "OPER";
                txtAlcInd.BackColor = Color.Lime;
            }
            else
            {
                btnByp.BackColor = Color.Yellow; btnByp.Text = "STBY";
                mini.btnByp.BackColor = Color.Yellow; //mini.btnByp.Text = "STBY";
                WriteToPort("ZZOF000;", 50);  // turn TX off

                txtAlcInd.BackColor = Color.Yellow;
            }

            switch (set.theAmp)
            {
                case 0: ACOM2K_Click(null, null); break;
                case 1: Alpha87_Click(null, null); break;
            }
            if (set.Temp == 0) temp_format = TempFormat.Celsius;
            else temp_format = TempFormat.Fahrenheit;

            //FWSetup();  // setup the FW ports
            X2SetUp();  // setup the X2 Matrix
            WN2SetUp(); // setup the WN2
            PMSetup();  // setup the Power Master
            cboSPEport.SelectedIndex = set.SPEport;
            chkSPEenab.Checked = set.SPEenab;
            cboA95port.SelectedIndex = set.A95port;
            chkA95enab.Checked = set.A95enab;
            chkAutoDrv.Checked = set.chkAutoDrv;
            autoDriveToolStripMenuItem1.Checked = set.chkAutoDrv;

            if (set.rbAmp == 1) rbAmp1.Checked = true;
            if (set.rbAmp == 2) rbAmp2.Checked = true;
            chk1KeyLine.Checked = set.chk1KeyLine;
            SOinit();   // setup SO2R
            AuxBcdInit();
            KnobInit();
            btnReCall_Click(null, null);

            if (chkLPenab.Checked) lpTimer.Enabled = true;
            else lpTimer.Enabled = false;

            //pollTimer.Enabled = true;
            chkModeChg.Checked = set.ModeChg;
            chkCwTx.Checked = set.chkCwTx;
            chkShortCut.Checked = set.chkShortCut;
            chkStartMacro.Checked = set.chkStartMacro;
            txtStartMacroNum.Text = set.txtStartMacroNum;
            StateInit();
            cboAmpFollow.SelectedIndex = set.cboAmpFollow;
            // setup error log parameters
            string path = Application.ExecutablePath;
            path = path.Substring(0, path.LastIndexOf("\\") + 1);
            path = path + "ErrorLog.txt";
            ErrorLog.LogFilePath = path;
            enableErrorLog = set.ErrorLog;
            if (enableErrorLog)
            {
                enableErrorLoggingToolStripMenuItem.Checked = true;
                errorLogToolStripMenuItem.Checked = true;
            }
            else
            {
                enableErrorLoggingToolStripMenuItem.Checked = false;
                errorLogToolStripMenuItem.Checked = false;
            }

            mSplashScreen.SetProgress("Loading Main Form", 1.0);
            COBC = lastBand;

            // execute startup macro if selected.
            if (chkStartMacro.Checked)
            StartMacro(Convert.ToInt32(txtStartMacroNum.Text)); 

        }// Setup
        #endregion Initialization

        #region Data Grid Events

        // A cell value changed in the BCD data grid 
        private void dg1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            string dgData = dg1.CurrentRow.Cells["freq"].Value.ToString();
            dgData = dgData.Substring(0, dgData.Length - 2);
            if (dgData != null)
            {
                if (flist.ContainsKey(dgData))
                {
                    flist.Remove(dgData);
                    flist.Add(dgData, dg1.CurrentRow.Cells["data"].Value);
                }
            }
        }

        #endregion Data Grid Events

        #region Delegates

        // Write to Split button
        delegate void SetSplitCallback(string text);
        public void SetSplit(string text)
        {
            if (this.btnSplit.InvokeRequired)
            {
                SetSplitCallback d = new SetSplitCallback(SetSplit);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "On")
                    btnSplit.BackColor = Color.Yellow;
                else
                    btnSplit.BackColor = Color.PaleTurquoise;
            }
        }
        // Write to AzOffset button
        delegate void SetAzOffsetCallback(string text);
        public void SetAzOffset(string text)
        {
            if (this.txtAzOffset.InvokeRequired)
            {
                SetAzOffsetCallback d = new SetAzOffsetCallback(SetAzOffset);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtAzOffset.Text = text;
            }
        }
        // Set SteppIR Freq
        delegate void SetStepFreqCallback(string text);
        private void SetStepFreq(string text)
        {
            if (this.lblStepFreq.InvokeRequired)
            {
                SetStepFreqCallback d = new SetStepFreqCallback(SetStepFreq);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.lblStepFreq.Text = text;
                mini.lblStepFreq.Text = text;
                rtr.lblStepFreq.Text = text;
            }
        }
        // Set rbWN1
        delegate void SetWN1Callback(bool bCmd);
        private void SetWN1(bool bCmd)
        {
            if (this.rbWN1.InvokeRequired)
            {
                SetWN1Callback d = new SetWN1Callback(SetWN1);
                this.Invoke(d, new object[] { bCmd });
            }
            else
                this.rbWN1.Checked = bCmd;
        }
        // Set rbWN2
        delegate void SetWN2Callback(bool bCmd);
        private void SetWN2(bool bCmd)
        {
            if (this.rbWN2.InvokeRequired)
            {
                SetWN2Callback d = new SetWN2Callback(SetWN2);
                this.Invoke(d, new object[] { bCmd });
            }
            else
                this.rbWN2.Checked = bCmd;
        }
        // Set rbWN3
        delegate void SetWN3Callback(bool bCmd);
        private void SetWN3(bool bCmd)
        {
            if (this.rbWN3.InvokeRequired)
            {
                SetWN3Callback d = new SetWN3Callback(SetWN3);
                this.Invoke(d, new object[] { bCmd });
            }
            else
                this.rbWN3.Checked = bCmd;
        }
        // Set rbWN4
        delegate void SetWN4Callback(bool bCmd);
        private void SetWN4(bool bCmd)
        {
            if (this.rbWN4.InvokeRequired)
            {
                SetWN4Callback d = new SetWN4Callback(SetWN4);
                this.Invoke(d, new object[] { bCmd });
            }
            else
                this.rbWN4.Checked = bCmd;
        }
        // Update the Rotor SP window
        delegate void SetRotorCallback(string text);
        private void SetRotor(string text)
        {
            if (!closing)
            {
                try
                {
                    if (this.lblSP.InvokeRequired)
                    {
                        SetRotorCallback d = new SetRotorCallback(SetRotor);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                    {
                        this.lblSP.Text = text;
                        mini.txtSP.Text = text;
                        rtr.lblSP.Text = text;
                    }
                }
                catch { }

            }
        }
        // Show/Hide "Antenna Moving" caption
        delegate void AntCallback(bool bCmd);
        private void ShowAnt(bool bCmd)
        {
            if (this.lblAnt.InvokeRequired)
            {
                AntCallback d = new AntCallback(ShowAnt);
                this.Invoke(d, new object[] { bCmd });
            }
            else
                this.lblAnt.Visible = bCmd;
        }
        // Set Title Bar caption
        delegate void SetTitleCallback(string text);
        private void SetTitle(string text)
        {
            if (!closing)
            {
                if (this.txtPort.InvokeRequired)
                {
                    SetTitleCallback d = new SetTitleCallback(SetTitle);
                    this.Invoke(d, new object[] { text });
                }
                else
                {
                    this.Text = text + "   " + newVerAvail;
                    mini.Text = text + "   " + newVerAvail;
                    if (!lblAnt.Visible)
                    {
                        if (mem[idxMem, 11] != "" && mem[idxMem, 11] != null)
                        {
                            this.StatusBar.Text = text + "                    " +
                                    mem[idxMem, 11];
                            mini.Text = text + "          " + mem[idxMem, 11];
                        }
                        else
                            this.StatusBar.Text = text;
                    }
                    else
                    {
                        this.StatusBar.Text = text +
                        "               * * * Transmit Inhibited! * * *";
                        mini.Text = text +
                        "     *** Transmit Inhibited! ***";
                    }
                }
            }
        }
        // Write IC-PW1 ra address
        delegate void SetTextCallback(string text);
        private void raSetText(string text)
        {
            if (!closing)
            {
                try
                {
                    if (this.txtPW1ra.InvokeRequired)
                    {
                        SetTextCallback d = new SetTextCallback(raSetText);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                        this.txtPW1ra.Text = text;
                }
                catch { }
            }
        }
        // Write Volts reading to txt box
        delegate void SetVoltsCallback(string text);
        private void SetVolts(string text)
        {
            if (!closing)
            {
                try
                {
                    if (this.txtVolts.InvokeRequired)
                    {
                        SetVoltsCallback d = new SetVoltsCallback(SetVolts);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                    {
                        double volts = Convert.ToDouble(text);
                        txtVolts.Text = text; mini.txtVolts.Text = text;
                        if (volts < 11.5 || volts > 14.5)
                        {
                            txtVolts.BackColor = Color.Red;
                            mini.txtVolts.BackColor = Color.Red;
                        }
                        else if (volts >= 11.5 && volts <= 12.0)
                        {
                            txtVolts.BackColor = Color.Orange;
                            mini.txtVolts.BackColor = Color.Orange;
                        }
                        else if (volts > 12.0 && volts <= 12.8)
                        {
                            txtVolts.BackColor = Color.Yellow;
                            mini.txtVolts.BackColor = Color.Yellow;
                        }
                        else
                        {
                            txtVolts.BackColor = Color.LightGreen;
                            mini.txtVolts.BackColor = Color.LightGreen;
                        }
                    }
                }
                catch { }
            }
        }
        // Write Temp reading to txt box
        delegate void SetTempCallback(string text);
        private void SetTemp(string text)
        {
            if (!closing)
            {
                try
                {
                    if (this.txtTemp.InvokeRequired)
                    {
                        SetTempCallback d = new SetTempCallback(SetTemp);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                    { txtTemp.Text = text; mini.txtTemp.Text = text; }
                }
                catch { }
            }
        }
        // Write txtTemp Back Color
        delegate void SetTempColorCallback(Color text);
        private void SetTempColor(Color text)
        {
            if (!closing)
            {
                try
                {
                    if (this.txtTemp.InvokeRequired)
                    {
                        SetTempColorCallback d = new SetTempColorCallback(SetTempColor);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                    { txtTemp.BackColor = text; mini.txtTemp.BackColor = text; }
                }
                catch { }
            }
        }
        // Write LP-100 fwd Power reading to txt box
        delegate void SetFwdCallback(string text);
        private void SetFwd(string text)
        {
            if (!closing)
            {
                try
                {
                    if (this.txtFwd.InvokeRequired)
                    {
                        SetFwdCallback d = new SetFwdCallback(SetFwd);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                        txtFwd.Text = text.TrimStart('0');
                }
                catch { }

            }
        }
        // Write LP-100 SWR reading to txt box
        delegate void SetSwrCallback(string text);
        private void SetSwr(string text)
        {
            if (!closing)
            {
                try
                {
                    if (this.txtSWR.InvokeRequired)
                    {
                        SetSwrCallback d = new SetSwrCallback(SetSwr);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                    {
                        txtSWR.Text = text;
                        if (chkSWR.Checked && Convert.ToDecimal(text) > numSWR.Value)
                        {
                            WriteToPort("ZZTI1;", iSleep);
                            lblHighSWR.Visible = true;
                            // Display a message that the SWR alarm value was exceeded.
                            MessageBox.Show(new Form() { TopMost = true },
                                "The SWR alarm value setting has been exceeded.\r\r" +
                                "PowerSDR's transmit ability is now disabled.\r\r" +
                                "Press OK to dismiss this message and to Reset the SWR Alarm",
                                "High SWR  [" + text + "]", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            lblHighSWR.Visible = false;
                            WriteToPort("ZZTI0;", iSleep);
                        }
                        double clr = Convert.ToDouble(text);

                        if (clr > 2 && clr < 3)
                            txtSWR.BackColor = Color.Yellow;
                        else if (clr > 3)
                            txtSWR.BackColor = Color.Pink;
                        else txtSWR.BackColor = Color.Empty;
                    }
                }
                catch //(Exception e)
                { }//{ MessageBox.Show(e.ToString()); }
            }
        }
        // Write LP-100 Alarm reading
        delegate void SetAlarmCallback(string text);
        private void SetAlarm(string text)
        {
            if (!closing)
            {
                try
                {
                    if (this.lblAlarm.InvokeRequired)
                    {
                        SetAlarmCallback d = new SetAlarmCallback(SetAlarm);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                        lblAlarm.Text = text;
                }
                catch { }
            }
        }
        // Write LP-100 Fwd reading
        delegate void SetLPPeakCallback(string text);
        private void SetLPPeak(string text)
        {
            if (!closing)
            {
                try
                {
                    if (this.lblFast.InvokeRequired)
                    {
                        SetLPPeakCallback d = new SetLPPeakCallback(SetLPPeak);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                        lblFast.Text = text;
                }
                catch { }
            }
        }
        // Write LP-100 Power mode
        delegate void SetPowerCallback(string text);
        private void SetPower(string text)
        {
            if (!closing)
            {
                try
                {
                    if (this.lblPower.InvokeRequired)
                    {
                        SetPowerCallback d = new SetPowerCallback(SetPower);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                        lblPower.Text = text;
                }
                catch { }
            }
        }
        // Write BCD Digit
        delegate void SetDigitCallback(string text);
        private void SetDigit(string text)
        {
            if (!closing)
            {
                try
                {
                    if (this.lblBCD.InvokeRequired)
                    {
                        SetDigitCallback d = new SetDigitCallback(SetDigit);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                        this.lblBCD.Text = text; mini.lblBCD.Text = text;
                }
                catch { }
            }
        }

        #endregion Delegates
                       
        #region Form Events

        // the amplifier follow me cbo has changed.
        private void cboAmpFollow_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.cboAmpFollow = cboAmpFollow.SelectedIndex;
        }
        // the enable startup macro check box has changed
        private void chkStartMacro_CheckedChanged(object sender, EventArgs e)
        {
            if (chkStartMacro.Checked) set.chkStartMacro = true;
            else set.chkStartMacro = false;
            set.Save();
        }
        // the txtStartMacroNum text box has changed
        private void txtStartMacroNum_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (Convert.ToInt32(txtStartMacroNum.Text) > 0 &&
                    Convert.ToInt32(txtStartMacroNum.Text) < 100)
                {
                    set.txtStartMacroNum = txtStartMacroNum.Text;
                    set.Save();
                }
                else
                    throw new Exception();
            }
            catch
            {
                MessageBox.Show(new Form() { TopMost = true },
                    "The macro number value can only be a decimal number from 1-99. \r\r" +
                    "Please select a number in this range.", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                txtStartMacroNum.Text = "1";
                set.txtStartMacroNum = "1";
            }
        }
        // the enable short-cuts check box has changed
        private void chkShortCut_CheckedChanged(object sender, EventArgs e)
        {
            if (chkShortCut.Checked)
            { set.chkShortCut = true; }
            else
            { set.chkShortCut = false; }
            set.Save();
        }
        // the chkCwTx check box has changed
        private void chkCwTx_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCwTx.Checked)
                set.chkCwTx = true;
            else
                set.chkCwTx = false;
            set.Save();
        }
        // the PSDR file location has changed
        private void txtVspMgr_TextChanged(object sender, EventArgs e)
        {
            set.vspMgrFile = txtVspMgr.Text; set.Save();
        }
        //the PSDR file location textbox was double-clicked
        private void txtVspMgr_DoubleClick(object sender, EventArgs e)
        {

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = "C:\\vspMgr\\";
            openFileDialog1.Filter = "exe files|*.exe";
            openFileDialog1.Title = "Select VSP Manager File Location";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtVspMgr.Text = openFileDialog1.FileName;
            }
            openFileDialog1.Dispose();
        }
        // The Azimuth Offset has changed
        private void txtAzOffset_TextChanged(object sender, EventArgs e)
        {
            try
            {
                int off = Convert.ToInt32(txtAzOffset.Text);
                if (off > 180 || off < -180)
                {
                    txtAzOffset.Text = "0";
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "The Offset value is out of range.\r\r" +
                        "Please Re-Enter the value to be between +/- 180 degrees",
                        "Input Error");
                    return;
                }
                set.AzOffset = Convert.ToInt32(txtAzOffset.Text);
                set.Save();
            }
            catch { }
        }
        // the SWR enable check box has been changed
        private void chkSWR_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSWR.Checked) set.SWRenab = true;
            else set.SWRenab = false;
            set.Save();
        }
        // the SWR number value has changed
        private void numSWR_ValueChanged(object sender, EventArgs e)
        {
            set.SWRnum = numSWR.Value;
            set.Save();
        }
        //
        //private void btnSWR_Click(object sender, EventArgs e)
        //{
        //    WriteToPort("ZZTI0;", iSleep);
        //    lblHighSWR.Visible = false;
        //}
        // the Amp160 check box has changed
        private void chkAmp160_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAmp160.Checked) { set.Amp160 = true; }
            else { set.Amp160 = false; }
            set.Save();
        }
        // the Amp80 check box has changed
        private void chkAmp80_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAmp80.Checked) { set.Amp80 = true; }
            else { set.Amp80 = false; }
            set.Save();
        }
        // the Amp40 check box has changed
        private void chkAmp40_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAmp40.Checked) { set.Amp40 = true; }
            else { set.Amp40 = false; }
            set.Save();
        }
        // the Amp30 check box has changed
        private void chkAmp30_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAmp30.Checked) { set.Amp30 = true; }
            else { set.Amp30 = false; }
            set.Save();
        }
        // the Amp20 check box has changed
        private void chkAmp20_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAmp20.Checked) { set.Amp20 = true; }
            else { set.Amp20 = false; }
            set.Save();
        }
        // the Amp17 check box has changed
        private void chkAmp17_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAmp17.Checked) { set.Amp17 = true; }
            else { set.Amp17 = false; }
            set.Save();
        }
        // the Amp15 check box has changed
        private void chkAmp15_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAmp15.Checked) { set.Amp15 = true; }
            else { set.Amp15 = false; }
            set.Save();
        }
        // the Amp12 check box has changed
        private void chkAmp12_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAmp12.Checked) { set.Amp12 = true; }
            else { set.Amp12 = false; }
            set.Save();
        }
        // the Amp10 check box has changed
        private void chkAmp10_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAmp10.Checked) { set.Amp10 = true; }
            else { set.Amp10 = false; }
            set.Save();
        }
        // the Amp6 check box has changed
        private void chkAmp6_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAmp6.Checked) { set.Amp6 = true; }
            else { set.Amp6 = false; }
            set.Save();
        }
        //the FZ button was pressed
        private void btnFZ_Click(object sender, EventArgs e)
        {
            ShowMini();
        }
        // The NoBandChg check box has changed
        private void chkNoBdChg_CheckedChanged(object sender, EventArgs e)
        {
            if (chkNoBdChg.Checked)
            { set.NoBandChg = true; }
            else { set.NoBandChg = false; }
            set.Save();
        }
        // Slave radio mode has changed.
        private void chkModeChg_CheckedChanged(object sender, EventArgs e)
        {
            if (chkModeChg.Checked) { set.ModeChg = true; }
            else { set.ModeChg = false; }
            set.Save();
        }
        // BCD Overide check box has been changed
        private void chkOvride_CheckedChanged(object sender, EventArgs e)
        {
            if (chkOvride.Checked)
            {
                chkDev0.Checked = false;
                lblBCD.ForeColor = Color.Blue;
                mini.lblBCD.ForeColor = Color.Blue;
            }
            else
            {
                chkDev0.Checked = true;
                lblBCD.ForeColor = Color.Red;
                mini.lblBCD.ForeColor = Color.Red;
            }
            //set.Save();
        }
        // one of the Manual Override radio buttons has changed
        private void grpBCDover_CheckChanged(object sender, EventArgs e)
        {
            if (rbOvr1.Checked)
            {
                if (rbFW.Checked) { WriteFW(mAdr1, cmd0, 1, false); }
                else { OutParallelPort(LPTnum, 1); this.SetDigit("1"); }
            }
            if (rbOvr2.Checked)
            {
                if (rbFW.Checked) { WriteFW(mAdr1, cmd0, 2, false); }
                else { OutParallelPort(LPTnum, 2); this.SetDigit("2"); }
            }
            if (rbOvr3.Checked)
            {
                if (rbFW.Checked) { WriteFW(mAdr1, cmd0, 3, false); }
                else { OutParallelPort(LPTnum, 3); this.SetDigit("3"); }
            }
            if (rbOvr4.Checked)
            {
                if (rbFW.Checked) { WriteFW(mAdr1, cmd0, 4, false); }
                else { OutParallelPort(LPTnum, 4); this.SetDigit("4"); }
            }
            if (rbOvr5.Checked)
            {
                if (rbFW.Checked) { WriteFW(mAdr1, cmd0, 5, false); }
                else { OutParallelPort(LPTnum, 5); this.SetDigit("5"); }
            }
            if (rbOvr6.Checked)
            {
                if (rbFW.Checked) { WriteFW(mAdr1, cmd0, 6, false); }
                else { OutParallelPort(LPTnum, 6); this.SetDigit("6"); }
            }
            if (rbOvr7.Checked)
            {
                if (rbFW.Checked) { WriteFW(mAdr1, cmd0, 7, false); }
                else { OutParallelPort(LPTnum, 7); this.SetDigit("7"); }
            }
            if (rbOvr8.Checked)
            {
                if (rbFW.Checked) { WriteFW(mAdr1, cmd0, 8, false); }
                else { OutParallelPort(LPTnum, 8); this.SetDigit("8"); }
            }
            if (rbOvr9.Checked)
            {
                if (rbFW.Checked) { WriteFW(mAdr1, cmd0, 9, false); }
                else { OutParallelPort(LPTnum, 9); this.SetDigit("9"); }
            }
            if (rbOvr10.Checked)
            {
                if (rbFW.Checked) { WriteFW(mAdr1, cmd0, 10, false); }
                else { OutParallelPort(LPTnum, 10); this.SetDigit("10"); }
            }
            if (rbOvr11.Checked)
            {
                if (rbFW.Checked) { WriteFW(mAdr1, cmd0, 11, false); }
                else { OutParallelPort(LPTnum, 11); this.SetDigit("11"); }
            }
            if (rbOvr12.Checked)
            {
                if (rbFW.Checked) { WriteFW(mAdr1, cmd0, 12, false); }
                else { OutParallelPort(LPTnum, 12); this.SetDigit("12"); }
            }
        }
        // The slaveDTR checkbox has changed
        private void chkSlaveDTR_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSlaveDTR.Checked)
            { AccPort.DtrEnable = true; set.slaveDTR = true; }
            else
            { AccPort.DtrEnable = false; set.slaveDTR = false; }
            set.Save();
        }
        // The slaveRTS checkbox has changed
        private void chkSlaveRTS_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSlaveRTS.Checked)
            { AccPort.RtsEnable = true; set.slaveRTS = true; }
            else
            { AccPort.RtsEnable = false; set.slaveRTS = false; }
            set.Save();
        }
        // The pwDTR checkbox has changed
        private void chkPwDTR_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPwDTR.Checked)
            { PW1port.DtrEnable = true; set.pwDTR = true; }
            else
            { PW1port.DtrEnable = false; set.pwDTR = false; }
            set.Save();
        }
        // The pwRTS checkbox has changed
        private void chkPwRTS_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPwRTS.Checked)
            { PW1port.RtsEnable = true; set.pwRTS = true; }
            else
            { PW1port.RtsEnable = false; set.pwRTS = false; }
            set.Save();
        }
        // The Split button was pressed
        bool split = false;
        public void btnSplit_Click(object sender, EventArgs e)
        {
            try
            {
                if (!split)
                {
                    split = true;
                    WriteToPort("ZZVS0;", iSleep); // vfo A>B
                    int newfreq = Convert.ToInt32(lastFreq);
                    newfreq += ((int)numSplit.Value * 1000);
                    WriteToPort("ZZFB" + newfreq.ToString().PadLeft(11, '0') + ";", iSleep);
                    WriteToPort("ZZSP1;", iSleep); // turn split on
                    btnSplit.BackColor = Color.Yellow;
                }
                else
                {
                    WriteToPort("ZZSP0;", iSleep); // turn split off
                    btnSplit.BackColor = Color.PaleTurquoise;
                    split = false;
                }
            }
            catch { }
        }
        // The Split up/down number box has changed
        private void numSplit_ValueChanged(object sender, EventArgs e)
        {
            set.SplitNum = numSplit.Value;
            set.Save();
        }
        // The On-Top Check box was changed
        private void chkOnTop_CheckedChanged(object sender, EventArgs e)
        {
            if (chkOnTop.Checked)
            { this.TopMost = true; set.MainOnTop = true; }
            else
            { this.TopMost = false; set.MainOnTop = false; }
            set.Save();
        }
        // The AudoOn check box has been changed.
        private void chkAudio_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAudio.Checked) { set.AudioOn = true; Notification.useAudio = true; }
            else { set.AudioOn = false; Notification.useAudio = false; }
            set.Save();
        }
        // The station call sign has changed
        private void txtCall_TextChanged(object sender, EventArgs e)
        {
            set.Call = txtCall.Text;
            set.Save();
        }
        // The station latitude has changes
        private void txtLat_TextChanged(object sender, EventArgs e)
        {
            set.Latitude = txtLat.Text;
            set.Save();
        }
        // The station longitude has changes
        private void txtLong_TextChanged(object sender, EventArgs e)
        {
            set.Longitude = txtLong.Text;
            set.Save();
        }
        // The station grid has changes
        private void txtGrid_TextChanged(object sender, EventArgs e)
        {
            set.Grid = txtGrid.Text;
            set.Save();
        }
        // Tempature text box has been clicked
        private void txtTemp_Click(object sender, EventArgs e)
        {
            switch (temp_format)
            {
                case TempFormat.Celsius:
                    temp_format = TempFormat.Fahrenheit;
                    set.Temp = 1; set.Save(); break;
                case TempFormat.Fahrenheit:
                    temp_format = TempFormat.Celsius;
                    set.Temp = 0; set.Save(); break;
            }
            WriteTemp();
        }
        // BCD device name has been changed, change the device tab and save to settings
        private void txtDev0_TextChanged(object sender, EventArgs e)
        {
            set.BCD = txtDev0.Text;
            set.Save();   // save new Device name to system settings
        }
        // The Select Band Decoder Data File button was pressed
        private void btnFile0_Click(object sender, EventArgs e)
        {
            // get the App Data Path
            if (app_data_path == "")
            {
                //Assembly assembly = Assembly.GetExecutingAssembly();
                //FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                //string version = fvi.FileVersion.Substring(0, fvi.FileVersion.LastIndexOf("."));
                //AppDataPath = assembly.Location;
                AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                + "\\DDUtil\\";
            }
            string bDataDir = app_data_path + "BandData\\";
            if (!Directory.Exists(bDataDir))
                Directory.CreateDirectory(bDataDir);

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = bDataDir;
            openFileDialog1.Filter = "xml files|*.xml";
            openFileDialog1.Title = "Select a XML File";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtFile0.Text = openFileDialog1.FileName;
                fileName = txtFile0.Text;
                set.DataFile = fileName;
                set.Save();
                GetBandData(fileName);
            }
            openFileDialog1.Dispose();
        }
        // Re-Load BCD Device Data File
        private void btnReLoad0_Click(object sender, EventArgs e)
        {
            GetBandData(fileName);
        }
        //Save current decoder file after creation/modification
        private void btnSave0_Click(object sender, EventArgs e)
        {
            try
            {
                // Write out the Band Data from the grid to the XML file
                //dg1.DataSource = ds;
                //dg1.DataMember = ("band");
                if (txtFile0.Text == null || txtFile0.Text == "")
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "Please enter a name for the file", "File Name Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                DataSet ds = (DataSet)dg1.DataSource;
                fileName = txtFile0.Text;
                File.Delete(fileName);
                ds.WriteXml(fileName);
                set.DataFile = fileName;
                set.Save();
                GetBandData(fileName);
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                    "Unable to write to log");
            }
        }
        // call the help file
        private void btnHelp_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("HelpDecoder.htm");
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                    "Unable to write to log");
            }
        }
        // Create a random nnumber between min and max
        public int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }
        // Form Load
        private void Setup_Load(object sender, EventArgs e)
        {
            try
            {
                closing = false;
                tabControl.SelectedIndex = set.TabOpen;
                cboPrefix.SelectedIndex = RandomNumber(0, 300);
                // Kill the splash screen
                if (mSplashScreen != null) mSplashScreen.Hide();
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                    "Unable to write to log");
            }
            // go see if there is a later DDUtil version available & tell the user
            try
            {
                string remoteUri = "http://k5fr.com/binary/";
                string fileName = "version2.txt", myStringWebResource = null;
                string fileName1 = "TkVersion.txt", myStringWebResource1 = null;
                string fileName2 = "USB Tuning Knob.hex", myStringWebResource2 = null;
                // Create a new WebClient instance.
                WebClient myWebClient = new WebClient();
                // Concatenate the domain with the Web resource filename.
                myStringWebResource = remoteUri + fileName;
                myStringWebResource1 = remoteUri + fileName1;
                myStringWebResource2 = remoteUri + fileName2;
                // Download the Web resource and save it into the current filesystem folder.
                myWebClient.DownloadFile(myStringWebResource, fileName);
                myWebClient.DownloadFile(myStringWebResource1, fileName1);
                myWebClient.DownloadFile(myStringWebResource2, fileName2);
                TextReader tr = new StreamReader(fileName);
                string tempStr = tr.ReadLine();
                tr.Close();
                string newVer = tempStr.Substring(0, 1) + tempStr.Substring(2, 1) +
                    tempStr.Substring(4, 1) + tempStr.Substring(6, 2);
                string oldVer = ver.Substring(0, 1) + ver.Substring(2, 1) +
                    ver.Substring(4, 1) + ver.Substring(6, 2);
                // display alert message if new version is avail
                if (Convert.ToInt32(newVer) > Convert.ToInt32(oldVer))
                {
                    MsgBoxCheck.MessageBox dlg = new MsgBoxCheck.MessageBox();
                    DialogResult dr = dlg.Show(
                        @"Software\DDUtil\MsgBoxCheck\UpdateAlert", "DontShowAgain", //DialogResult.No, 
                        "Don't ask me this again",
                        "There is a new version of DDUtil available.\r\r" +
                        "If you would you like to update to the new version\r\r" +
                        "press Yes to Continue and download or No to Cancel.\r\r" +
                        "Selecting the 'Don't ask me this again' check box for\r" +
                        "either answer will surpress this message permanently.",
                        "Update Alert!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                    if (dr == DialogResult.Yes)
                    {
                        updateToLatestVersionToolStripMenuItem_Click(null, null);
                    }
                    newVerAvail = "UpDate";
                }
                else
                    newVerAvail = "";
            }
            catch  // this is a silent notification so ignore any failure.
            {}
            formLoaded = true;
        }
        // program is about to close
        int IdxClose = 0;
        private void Setup_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (bMacChg)    // see if macro was changed and not saved.
            {
                DialogResult result;
                result = MessageBox.Show(new Form() { TopMost = true },
                    "Are you sure you want to exit?\n\n" +
                    "Macro data has been changed and not saved!\n\n" +
                    "If you want to save your work press YES\n" +
                    "If you want to Stop the Form Close press Cancel",
                    "Macro Data UnSaved", MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Cancel)
                { e.Cancel = true; return; }
                else if (result == DialogResult.Yes)
                { btnMacSave_Click(null, null); }
            }
            // see if SO2R data was changed and not saved.
            if (bSo2rChg && chkSoEnab.Checked && !bClosePass1) 
            {
                DialogResult result;
                result = MessageBox.Show(new Form() { TopMost = true },
                    "Are you sure you want to exit?\n\n" +
                    "SO2R data has been changed and has not saved!\n\n" +
                    "If you want to save your work press YES\n" +
                    "If you want to Stop the Form Close press Cancel",
                    "SO2R Data UnSaved", MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button1);
                if (result == DialogResult.Cancel)
                { e.Cancel = true; bClosePass1 = false; return; }
                else if (result == DialogResult.Yes)
                { btnSaveSO2R_Click(null, null); }
                bClosePass1 = true;
            }
            try
            {
                if (chkA95enab.Checked && loopThd.IsAlive) 
                    loopThd.Abort();
                _server.Close();
                closing = true;
                AtTimer.Stop();
                blinkTimer.Stop();
                lpTimer.Stop();
                pollTimer.Stop();
                StepTimer.Stop();
                SPETimer.Stop();
                WatchDog.Stop();
                WN2Timer.Stop();
                AlphaTimer.Stop();
                mini.Close();
                this.Text = "DDUtil Shutting Down";
                if (sp.isOpen)
                {
                    e.Cancel = true; IdxClose = 1;
                    Thread ClosePort = new Thread(new ThreadStart(CloseSerialOnExit));
                    ClosePort.Start();
                }
                if (LPport.IsOpen)
                {
                    e.Cancel = true; IdxClose = 2;
                    Thread ClosePort = new Thread(new ThreadStart(CloseSerialOnExit));
                    ClosePort.Start();
                }
                if (AccPort.IsOpen)
                {
                    e.Cancel = true; IdxClose = 3;
                    Thread ClosePort = new Thread(new ThreadStart(CloseSerialOnExit));
                    ClosePort.Start();
                }
                if (AlphaPort.IsOpen)
                {
                    e.Cancel = true; IdxClose = 4;
                    Thread ClosePort = new Thread(new ThreadStart(CloseSerialOnExit));
                    ClosePort.Start();
                }
                if (PMport.IsOpen)
                {
                    e.Cancel = true; IdxClose = 5;
                    Thread ClosePort = new Thread(new ThreadStart(CloseSerialOnExit));
                    ClosePort.Start();
                }
                if (LogPort.IsOpen)
                {
                    e.Cancel = true; IdxClose = 6;
                    Thread ClosePort = new Thread(new ThreadStart(CloseSerialOnExit));
                    ClosePort.Start();
                }
                if (SPEport.IsOpen)
                {
                    e.Cancel = true; IdxClose = 7;
                    Thread ClosePort = new Thread(new ThreadStart(CloseSerialOnExit));
                    ClosePort.Start();
                }
                if (A95port.IsOpen)
                {
                    e.Cancel = true; IdxClose = 8;
                    Thread ClosePort = new Thread(new ThreadStart(CloseSerialOnExit));
                    ClosePort.Start();
                }
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                    "Unable to write to log");
            }
        }

        private void CloseSerialOnExit()
        {
            try
            {
                switch (IdxClose)
                {
                    case 1: sp.Close(); break;
                    case 2: LPport.Close(); break;
                    case 3: AccPort.Close(); break;
                    case 4: AlphaPort.Close(); break;
                    case 5: PMport.Close(); break;
                    case 6: LogPort.Close(); break;
                    case 7: SPEport.Close(); break;
                    case 8: A95port.Close(); break;
                }
            }
            catch { }
            //now close back in the main thread
            try 
            { this.Invoke(new EventHandler(NowClose)); }
            catch
            { }
        }
        private void NowClose(object sender, EventArgs e)
        {
            this.Close(); //now close the form
        }

        // program is closed, lets cleanup
        private void Setup_FormClosing(object sender, FormClosedEventArgs e)
        {
            try
            {
                set.TabOpen = tabControl.SelectedIndex;
                set.Save();
                PortAccess.Output(LPTnum, 0);
                if (chkSoEnab.Checked)
                {
                    PortAccess.Output(solpt1, 0);
                    PortAccess.Output(solpt2, 0);
                }
                if (chkKnobEnab.Checked) KnobPort.Write("I000;");

                // close other open ports
                if (sp.isOpen) sp.Close();
                if (StepData.IsOpen) StepData.Close();
                if (PW1port.IsOpen) PW1port.Close();
                if (A95port.IsOpen) A95port.Close();
                if (RCP2port.IsOpen) RCP2port.Close();
                if (RCP3port.IsOpen) RCP3port.Close();
                if (RCP4port.IsOpen) RCP4port.Close();
                if (RCP5port.IsOpen) RCP5port.Close();
                if (RCP1Rotor.IsOpen) RCP1Rotor.Close();
                if (RCP2Rotor.IsOpen) RCP2Rotor.Close();
                if (RCP3Rotor.IsOpen) RCP3Rotor.Close();
                if (RCP4Rotor.IsOpen) RCP4Rotor.Close();
                if (RCP5Rotor.IsOpen) RCP5Rotor.Close();
                if (RotorPort.IsOpen) RotorPort.Close();
                if (SwitchPort.IsOpen) SwitchPort.Close();
                if (MacroPort.IsOpen) MacroPort.Close();
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                    "Unable to write to log");
            }
        }
        // The LPT port has changed
        private void grpLPT_CheckedChanged(object sender, EventArgs e)
        {
            txtPort.Enabled = true;
            if (sender == rbNone)
            {
                OutParallelPort(LPTnum, 0); LPTnum = 0; set.lptPort = "NONE";
                chkDevice.Checked = false; txtPort.Text = LPTnum.ToString();
            }
            else if (sender == rbOther)
            { txtPort.Text = ""; set.lptPort = "Other"; }
            else if (sender == rb1)
            { LPTnum = 888; set.lptNum = LPTnum.ToString(); set.lptPort = "LPT1"; txtPort.Text = LPTnum.ToString(); }
            else if (sender == rb2)
            { LPTnum = 632; set.lptNum = LPTnum.ToString(); set.lptPort = "LPT2"; txtPort.Text = LPTnum.ToString(); }
            else if (sender == rb3)
            { LPTnum = 636; set.lptNum = LPTnum.ToString(); set.lptPort = "LPT3"; txtPort.Text = LPTnum.ToString(); }
            else if (sender == rb4)
            { LPTnum = 620; set.lptNum = LPTnum.ToString(); set.lptPort = "LPT4"; txtPort.Text = LPTnum.ToString(); }
            else if (sender == rbFW)
            { txtPort.Enabled = false; set.lptPort = "FW"; set.lptNum = ""; txtPort.Text = ""; }
            //LPTnum = 0; set.lptPort = "FW"; 
            //chkPortB.Checked = false;  

            else
            {
                OutParallelPort(LPTnum, 0); LPTnum = 0; set.lptPort = "NONE"; rbNone.Checked = true;
                chkDevice.Checked = false; txtPort.Text = LPTnum.ToString();
            }
            set.Save();
        }
        // LPT Port number changed
        private void txtPort_TextChanged(object sender, EventArgs e)
        {
            if (rbOther.Checked == true)
            { lblPortBtn.Visible = true; btnPortNum.Visible = true; }
            else
            { btnPortNum.Visible = false; ; lblPortBtn.Visible = false; }
        }
        // The Save LPTPort Number button was pressed
        private void btnPortNum_Click(object sender, EventArgs e)
        {
            LPTnum = Convert.ToInt32(txtPort.Text);
            set.lptNum = LPTnum.ToString();
            set.Save();
            btnPortNum.Visible = false;
            lblPortBtn.Visible = false;
        }
        // CAT port timer interval changed
        private void txtInv_TextChanged(object sender, EventArgs e)
        {
            try
            {
                pollTimer.Enabled = false;
                pollInt = Convert.ToDouble(txtInv.Text);
                set.pollIntvl = txtInv.Text;
                set.Save();     // save new polling interval to system settings
                pollTimer.Interval = pollInt;
                pollTimer.Enabled = true;
            }
            catch {}
        }
        // cboDevice selection changed
        //private void cboDevice_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    set.BCD = cboDevice.Text;
        //    set.Save();     // save new LPT device to system settings
        //}
        // Device 0 enabled condition changed
        private void chkDev0_CheckedChanged(object sender, EventArgs e)
        {
            if (chkDev0.Checked == true)
            {
                chkOvride.Checked = false;
                set.Dev0Enab = true;
            }
            else
            {
                set.Dev0Enab = false;
            }
            set.Save();
        }
        // LPT Device enabled condition changed
        private void chkDevice_CheckedChanged(object sender, EventArgs e)
        {
            if (chkDevice.Checked == true)
            {
                set.DevEnab = true;
            }
            else
            {
                set.DevEnab = false;
            }
            set.Save();
        }
        // CI-V Hex Address has changed
        private void txtRadNum_TextChanged(object sender, EventArgs e)
        {
            set.CIVaddr = txtRadNum.Text;
            set.Save();
        }
        // New RCP1 port was selected
        private void cboLogPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (LogPort.IsOpen) LogPort.Close();
            if (cboLogPort.SelectedIndex > 0)
            {
                LogPort.PortName = cboLogPort.SelectedItem.ToString();
                try
                {
                    LogPort.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "The RCP1 serial port " + LogPort.PortName +
                        " cannot be opened!", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboLogPort.SelectedIndex = 0;
                    set.LogPort = 0;
                    set.Save();
                    return;
                }
            }
            // save new port setting
            set.LogPort = cboLogPort.SelectedIndex;
            set.Save();
        }
        // RCP1 Rotor Port has changed
        private void cboRCP1Rotor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RCP1Rotor.IsOpen) RCP1Rotor.Close();
            if (cboRCP1Rotor.SelectedIndex > 0)
            {
                RCP1Rotor.PortName = cboRCP1Rotor.SelectedItem.ToString();
                try
                {
                    RCP1Rotor.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "The RCP1 Rotor port " + RCP1Rotor.PortName +
                        " cannot be opened!", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboRCP1Rotor.SelectedIndex = 0;
                    set.RCP1RotorPort = 0;
                    set.Save();
                    return;
                }
            }
            // save new port setting
            set.RCP1RotorPort = cboRCP1Rotor.SelectedIndex;
            set.Save();
        }
        // RCP2 Rotor Port has changed
        private void cboRCP2Rotor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RCP2Rotor.IsOpen) RCP2Rotor.Close();
            if (cboRCP2Rotor.SelectedIndex > 0)
            {
                RCP2Rotor.PortName = cboRCP2Rotor.SelectedItem.ToString();
                try
                {
                    RCP2Rotor.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "The RCP2 Rotor port " + RCP2Rotor.PortName +
                        " cannot be opened!", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboRCP2Rotor.SelectedIndex = 0;
                    set.RCP2RotorPort = 0;
                    set.Save(); 
                    return;
                }
            }
            // save new port setting
            set.RCP2RotorPort = cboRCP2Rotor.SelectedIndex;
            set.Save();
        }
        // RCP3 Rotor Port has changed
        private void cboRCP3Rotor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RCP3Rotor.IsOpen) RCP3Rotor.Close();
            if (cboRCP3Rotor.SelectedIndex > 0)
            {
                RCP3Rotor.PortName = cboRCP3Rotor.SelectedItem.ToString();
                try
                {
                    RCP3Rotor.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "The RCP3 Rotor port " + RCP3Rotor.PortName +
                       " cannot be opened!", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboRCP3Rotor.SelectedIndex = 0;
                    set.RCP3RotorPort = 0;
                    set.Save(); 
                    return;
                }
            }
            // save new port setting
            set.RCP3RotorPort = cboRCP3Rotor.SelectedIndex;
            set.Save();
        }
        // RCP4 Rotor Port has changed
        private void cboRCP4Rotor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RCP4Rotor.IsOpen) RCP4Rotor.Close();
            if (cboRCP4Rotor.SelectedIndex > 0)
            {
                RCP4Rotor.PortName = cboRCP4Rotor.SelectedItem.ToString();
                try
                {
                    RCP4Rotor.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "The RCP4 Rotor port " + RCP4Rotor.PortName +
                        " cannot be opened!", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboRCP4Rotor.SelectedIndex = 0;
                    set.RCP4RotorPort = 0;
                    set.Save(); 
                    return;
                }
            }
            // save new port setting
            set.RCP4RotorPort = cboRCP4Rotor.SelectedIndex;
            set.Save();
        }
        // RCP5 Rotor Port has changed
        private void cboRCP5Rotor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RCP5Rotor.IsOpen) RCP5Rotor.Close();
            if (cboRCP5Rotor.SelectedIndex > 0)
            {
                RCP5Rotor.PortName = cboRCP5Rotor.SelectedItem.ToString();
                try
                {
                    RCP5Rotor.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true },
                        "The RCP5 Rotor port " + RCP5Rotor.PortName +
                        " cannot be opened!", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboRCP5Rotor.SelectedIndex = 0;
                    set.RCP5RotorPort = 0;
                    set.Save();
                    return;
                }
            }
            // save new port setting
            set.RCP5RotorPort = cboRCP5Rotor.SelectedIndex;
            set.Save();
        }
        // new PL port was selected
        private void cboSerAcc_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AccPort.IsOpen) AccPort.Close();
            if (cboSerAcc.SelectedIndex > 0)
            {
                AccPort.PortName = cboSerAcc.SelectedItem.ToString();
                try
                {
                    AccPort.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "The Passive Listener serial port " + AccPort.PortName +
                        " cannot be opened!", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboSerAcc.SelectedIndex = 0;
                    set.AccPort = 0;
                    set.Save();
                    return;
                }
            }
            else
            {
                cboSerAcc.SelectedIndex = 0; 
                chkFollow.Checked = false; cboRadio.SelectedIndex = 0;
                cboRadio.Enabled = false; chkMode.Enabled = false;
                set.followChk = false; 
                set.Save();
            }

            // save new port setting
            set.AccPort = cboSerAcc.SelectedIndex;
            set.Save();
        }
        // new radio port was selected
        private void cboCAT_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sp.isOpen) sp.Close();
            try
            {
                if (cboCAT.SelectedIndex > 0)
                {
                    sp.Name = cboCAT.SelectedItem.ToString();
                    sp.Open();
                }
            }
            catch
            {
                MessageBox.Show(new Form() { TopMost = true }, 
                    "The Radio serial port " + sp.Name +
                    " cannot be opened!\n", "Port Error",
                   MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboCAT.SelectedIndex = 0; ;
                set.RadioPort = 0;
                set.Save();
                return;
            }
            // save new port setting
            set.RadioPort = cboCAT.SelectedIndex;
            set.Save();
        }
        // Follow radio check box has changed
        private void chkFollow_CheckedChanged(object sender, EventArgs e)
        {
            if (cboSerAcc.SelectedIndex > 0)
            {
                if (chkFollow.Checked == true)
                {
                    cboRadio.Enabled = true;
                    chkMode.Enabled = true;
                    set.followChk = true;
                    set.Save();
                }
                else
                {
                    chkFollow.Checked = false; cboRadio.SelectedIndex = 0;
                    cboRadio.Enabled = false; chkMode.Enabled = false;
                    set.followChk = false; set.Save();
                }
            }
            else
            {
                chkFollow.Checked = false; cboRadio.SelectedIndex = 0;
                cboRadio.Enabled = false; chkMode.Enabled = false;
                set.followChk = false; set.Save();

                Notification alert = new Notification();
                Notification.notiIntvl = 7000;
                Notification.notiMsg =
                    "You must assign a port number before the\r\r" +
                    "Passive Listener device(s) will be usable.\r\r" +
                    "Please assign a valid port and try again.\r";
                alert.Show();
            }
        }
        // Follow Radio type has changed
        private void cboRadio_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.followRadio = (int)cboRadio.SelectedIndex;
            set.Save();
            switch (cboRadio.SelectedIndex)
            {
                case 0: // None
                    portmode = PortMode.None;
                    txtRadNum.Enabled = false;
                    chkFollow.Checked = false;
                    break;
                case 1: // Kenwood
                    portmode = PortMode.Kenwood;
                    txtRadNum.Enabled = false;
                    break;
                case 2: // Yaesu Type I
                    portmode = PortMode.YaesuTypeI;
                    txtRadNum.Enabled = false;
                    break;
                case 3: // Yaesu Type II
                    portmode = PortMode.YaesuTypeII;
                    txtRadNum.Enabled = false;
                    break;
                case 4: // Icom
                    portmode = PortMode.Icom;
                    txtRadNum.Enabled = true;
                    break;
                case 5: // SO2Rxlat
                    portmode = PortMode.SO2Rxlat;
                    txtRadNum.Enabled = false;
                    break;
                default:
                    break;
            }
        }
        // Radio Data combo box has changed
        private void cboRadData_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cboRadData.SelectedIndex)
            {
                case 0: // 9600 8N1
                    AccPort.BaudRate = 9600;
                    AccPort.DataBits = 8;
                    AccPort.Parity = System.IO.Ports.Parity.None;
                    AccPort.StopBits = System.IO.Ports.StopBits.One;
                    break;
                case 1: // 9600 8N2
                    AccPort.BaudRate = 9600;
                    AccPort.DataBits = 8;
                    AccPort.Parity = System.IO.Ports.Parity.None;
                    AccPort.StopBits = System.IO.Ports.StopBits.Two;
                    break;
                case 2: // 4800 8N1
                    AccPort.BaudRate = 4800;
                    AccPort.DataBits = 8;
                    AccPort.Parity = System.IO.Ports.Parity.None;
                    AccPort.StopBits = System.IO.Ports.StopBits.One;
                    break;
                case 3: // 4800 8N2
                    AccPort.BaudRate = 4800;
                    AccPort.DataBits = 8;
                    AccPort.Parity = System.IO.Ports.Parity.None;
                    AccPort.StopBits = System.IO.Ports.StopBits.Two;
                    break;
                case 4: // 2400 8N1
                    AccPort.BaudRate = 2400;
                    AccPort.DataBits = 8;
                    AccPort.Parity = System.IO.Ports.Parity.None;
                    AccPort.StopBits = System.IO.Ports.StopBits.One;
                    break;
                case 5: // 2400 8N2
                    AccPort.BaudRate = 2400;
                    AccPort.DataBits = 8;
                    AccPort.Parity = System.IO.Ports.Parity.None;
                    AccPort.StopBits = System.IO.Ports.StopBits.Two;
                    break;
                case 6: // 1200 8N1
                    AccPort.BaudRate = 1200;
                    AccPort.DataBits = 8;
                    AccPort.Parity = System.IO.Ports.Parity.None;
                    AccPort.StopBits = System.IO.Ports.StopBits.One;
                    break;
                case 7: // 1200 8N2
                    AccPort.BaudRate = 1200;
                    AccPort.DataBits = 8;
                    AccPort.Parity = System.IO.Ports.Parity.None;
                    AccPort.StopBits = System.IO.Ports.StopBits.Two;
                    break;
                case 8: // 19200 8N1
                    AccPort.BaudRate = 19200;
                    AccPort.DataBits = 8;
                    AccPort.Parity = System.IO.Ports.Parity.None;
                    AccPort.StopBits = System.IO.Ports.StopBits.One;
                    break;
                default:
                    break;
            }
            set.RadData = (int)cboRadData.SelectedIndex;
            set.Save();
        }
        // RCP2 port number changed
        private void cboRCP2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RCP2port.IsOpen) RCP2port.Close();
            if (cboRCP2.SelectedIndex > 0)
            {
                RCP2port.PortName = cboRCP2.SelectedItem.ToString();
                try
                {
                    RCP2port.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "The RCP2 serial port " + RCP2port.PortName +
                        " cannot be opened!\n", "Port Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboRCP2.SelectedIndex = 0;
                    set.cboRCP2 = 0;
                    set.Save();
                    return;
                }
            }
            else
            {
                cboRCP2.SelectedIndex = 0; 
                set.Save();
            }
            // save new port setting
            set.cboRCP2 = cboRCP2.SelectedIndex;
            set.Save();
        }
        // RCP3 port number changed
        private void cboRCP3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RCP3port.IsOpen) RCP3port.Close();
            if (cboRCP3.SelectedIndex > 0)
            {
                RCP3port.PortName = cboRCP3.SelectedItem.ToString();
                try
                {
                    RCP3port.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "The RCP3 serial port " + RCP3port.PortName +
                        " cannot be opened!\n", "Port Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboRCP3.SelectedIndex = 0;
                    set.Save();
                    return;
                }
            }
            else
            {
                cboRCP3.SelectedIndex = 0; 
                set.cboRCP3 = 0;
                set.Save();
            }
            // save new port setting
            set.cboRCP3 = cboRCP3.SelectedIndex;
            set.Save();
        }
        // RCP4 port number changed
        private void cboRCP4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RCP4port.IsOpen) RCP4port.Close();
            if (cboRCP4.SelectedIndex > 0)
            {
                RCP4port.PortName = cboRCP4.SelectedItem.ToString();
                try
                {
                    RCP4port.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "The RCP4 serial port " + RCP4port.PortName +
                        " cannot be opened!\n", "Port Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboRCP4.SelectedIndex = 0;
                    set.cboRCP4 = 0;
                    set.Save();
                    return;
                }
            }
            else
            {
                cboRCP4.SelectedIndex = 0;
            }
            // save new port setting
            set.cboRCP4 = cboRCP4.SelectedIndex;
            set.Save();
        }
        // RCP5 port number changed
        private void cboRCP5_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RCP5port.IsOpen) RCP5port.Close();
            if (cboRCP5.SelectedIndex > 0)
            {
                RCP5port.PortName = cboRCP5.SelectedItem.ToString();
                try
                {
                    RCP5port.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true },
                        "The RCP5 serial port " + RCP5port.PortName +
                        " cannot be opened!\n", "Port Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboRCP5.SelectedIndex = 0;
                    set.cboRCP5 = 0;
                    set.Save();
                    return;
                }
            }
            else
            {
                cboRCP5.SelectedIndex = 0;
            }
            // save new port setting
            set.cboRCP5 = cboRCP5.SelectedIndex;
            set.Save();
        }
        // N1MM port number changed
        private void cboN1MM_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (N1MMport.IsOpen) N1MMport.Close();
            if (cboN1MM.SelectedIndex > 0)
            {
                N1MMport.PortName = cboN1MM.SelectedItem.ToString();
                try
                {
                    N1MMport.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true },
                        "The RCP5 serial port " + N1MMport.PortName +
                        " cannot be opened!\n", "Port Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboN1MM.SelectedIndex = 0;
                    set.cboN1MM = 0;
                    set.Save();
                    return;
                }
            }
            else
            {
                cboN1MM.SelectedIndex = 0;
            }
            // save new port setting
            set.cboN1MM = cboN1MM.SelectedIndex;
            set.Save();
        }
        // PW1 Enable RadioButton has changed
        private void chkPW1_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPW1.Checked)
            {
                if (cboPW1.SelectedIndex > 0)
                {
                    set.chkPW1 = true;
                }
                else
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "No port has been selected for PW1.\n\n" +
                        "Please select a valid port number and try again.", "Port Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    chkPW1.Checked = false;
                    set.chkPW1 = false;
                    cboPW1.SelectedIndex = 0;
                }
            }
            else
            {
                set.chkPW1 = false;
            }
            set.Save();

        }
        // PW1 Port Number has changed
        private void cboPW1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PW1port.IsOpen) PW1port.Close();
            if (cboPW1.SelectedIndex > 0)
            {
                PW1port.PortName = cboPW1.SelectedItem.ToString();
                try
                {
                    PW1port.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "The IC-PW1 serial port " + PW1port.PortName +
                        " cannot be opened!\n", "Port Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    chkPW1.Checked = false;
                    cboPW1.SelectedIndex = 0;
                    set.cboPW1 = 0;
                    set.Save();
                    return;
                }
            }
            else
            {
                cboPW1.SelectedIndex = 0;
                chkPW1.Checked = false;
            }
            // save new port setting
            set.cboPW1 = cboPW1.SelectedIndex;
            set.Save();
        }
        // PW1ra CI-V address textbox has changed
        private void txtPW1ra_TextChanged(object sender, EventArgs e)
        {
            set.txtPW1ra = txtPW1ra.Text;
            set.Save();
        }
        // PW1ta CI-V address textbox has changed
        private void txtPW1ta_TextChanged(object sender, EventArgs e)
        {
            set.txtPW1ta = txtPW1ta.Text;
            set.Save();
        }
        // The PW1 Disable Broadcast check box has changed.
        private void chkDisBcast_CheckedChanged(object sender, EventArgs e)
        {
            if (chkDisBcast.Checked) { set.chkPW1db = true; }
            else { set.chkPW1db = false; }
            set.Save();
        }
        // The Thread.Sleep interval has changed
        private void txtSleep_TextChanged(object sender, EventArgs e)
        {
            try
            {
                set.Sleep = txtSleep.Text;
                set.Save();
                iSleep = Convert.ToInt32(txtSleep.Text);
            }
            catch { }
        }
        // Hide/UnHide Sleep text box
        private void tabOther_DoubleClick(object sender, EventArgs e)
        {
            if (txtSleep.Visible)
            { txtSleep.Visible = false; lblSleep.Visible = false; }
            else
            { txtSleep.Visible = true; lblSleep.Visible = true; }
        }
        // The Program Memo text box has changed.
        private void txtRCP_TextChanged(object sender, EventArgs e)
        {
            try
            { set.RCPtext = txtRCP.Text; set.Save(); }
            catch { }
        }
        // The tool tips check box has changed
        private void chkTips_CheckedChanged(object sender, EventArgs e)
        {
            if (chkTips.Checked)
            { toolTip1.Active = true; set.ToolTips = true; }
            else
            { toolTip1.Active = false; set.ToolTips = false; }
            set.Save();
        }
        // Slave mode RadioButton has changed
        private void chkMode_CheckedChanged(object sender, EventArgs e)
        {
            if (chkMode.Checked) { set.slaveMode = true; }
            else { set.slaveMode = false; }
            set.Save();
        }

        #endregion Form Events

        #region Helper Methods
       
        public void StartMacro(int macro)
        {
            try
            {
                if (StepCtr == 0)// && (xOn == "0" || xOn == ""))
                {   // 
                    if (dgm.Rows[macro - 1].Cells[1].Value.ToString() == "")
                    {
                        throw new Exception();
                    }
                    string cmds = dgm.Rows[macro - 1].Cells[1].Value.ToString();
                    ParseBuffer(cmds);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (NullReferenceException)
            {
                MessageBox.Show(new Form() { TopMost = true },
                    "There are no commands setup for this Macro " + macro.ToString());
            }
        }
        
        // display tip of the day dialog
        private void Setup_Shown(object sender, EventArgs e)
        {
            if (TipOfTheDayDialog.IsShowTipsOnStartUp())
            {
                TipOfTheDayDialog dlg = new TipOfTheDayDialog();
                dlg.ShowDialog();
            }

        }
        // Converts string to hex string
        public string ConvertToHex(string asciiString)
        {
            string hex = "";
            foreach (char c in asciiString)
            {
                int tmp = c;
                hex += String.Format("{0:x2}", (uint)System.Convert.ToUInt32(tmp.ToString()));
            }
            return hex;
        }
        // takes hex byte and converts to ascii for 0-F digits
        public static string ByteToAscii(byte[] hex)
        {
            int num = hex[0];
            int rt = num & 0x0F;    // keep rt digit
            rt += 0x30;
            int lt = num >> 4;      // move left digit 4 pos
            lt += 0x30;
            return lt.ToString() + rt.ToString();
        }

        static char[] hexDigits = {
        '0', '1', '2', '3', '4', '5', '6', '7',
        '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};

        public static string ToHexString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                int b = bytes[i];
                chars[i * 2] = hexDigits[b >> 4];
                chars[i * 2 + 1] = hexDigits[b & 0x0F];
            }
            return new string(chars);
        }
        void BadPort(string port)
        {
            Notification alert = new Notification();
            Notification.notiIntvl = 7000;
            Notification.notiMsg =
            "There was a problem initializing the last used port for " + port + ".\r\r" +
            "This port will be set to empty.\r";
            alert.Show();
        }

        // Show the Mini window
        private void ShowMini()
        {
            mini.Show(); WindowState = FormWindowState.Minimized;
        }
        // output data to the parallel port
        private void OutParallelPort(int port, int num)
        {
            if (!closing)
                PortAccess.Output(port, num);
        }
        // Load Data File
        private void GetBandData(string fileName)
        {
            try
            {
                // Read in the Band Data from the XML file and display in datagrid
                ds = new DataSet();
                ds.ReadXml(fileName);
                dg1.DataSource = ds;
                dg1.DataMember = "band";
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                    "Unable to write to log");
            }
            // Read in the Band data from the XML file into the hash table
            // Uses format 14080 = 140, 3575 = 35. This placs a different value pair every 100 khz
            try
            {
                flist.Clear();
                string freq = "";
                string data = "";
                FileStream bdata = new FileStream(fileName, FileMode.Open);
                XPathDocument myDoc = new XPathDocument(bdata);
                XPathNavigator docNav = myDoc.CreateNavigator();
                foreach (XPathNavigator node in docNav.Select("//banddata/bandmulti/band/*"))
                {
                    switch (node.Name)
                    {
                        case "freq":
                            freq = node.Value.ToString();
                            freq = freq.Substring(0, freq.Length - 2);
                            break;
                        case "data":
                            data = node.Value.ToString();
                            flist.Add(freq, data);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                    "Unable to write to log");
            }
        }
        // Lookup freq data in hash table & output to LPT port
        private void LookUp(string freq)
        {
            if (flist.ContainsKey(freq))// && chkDevice.Checked)
            {
                keyValue = Convert.ToInt16(flist[freq]);
                if (chkDSInvert.Checked) keyValue = keyValue ^ 255;
                if (rbFW.Checked) // write data to the FlexWire port
                { WriteFW(mAdr1, cmd0, keyValue, false); }
                else 
                {   // write data to the parallel port selected
                    if (chkAuxEnab.Checked) // Aux BCD control
                    {
                        int auxByteL = 0;   // aux BCD LSB's data
                        int auxByteH = 0;   // aux BCD MSB's data
                        if (chkAuxBit1.Checked) auxByteL = 1;       // 4 LSB's
                        if (chkAuxBit2.Checked) auxByteL += 2;
                        if (chkAuxBit3.Checked) auxByteL += 4;
                        if (chkAuxBit4.Checked) auxByteL += 8;
                        if (chkAuxBit5.Checked) auxByteH = 16;      // 4 MSB's
                        if (chkAuxBit6.Checked) auxByteH += 32;
                        if (chkAuxBit7.Checked) auxByteH += 64;
                        if (chkAuxBit8.Checked) auxByteH += 128;

                        if (chkFollowFreq.Checked)
                        {   // if checked, 4 LSB's follow BCD file freq
                            // 4 MSB's follow aux data
                            keyValue = keyValue & 0x0f;     // save freq LSBs
                            keyValue = keyValue + auxByteH; // Add aux data MSBs
                        }
                        else
                        {   // output aux data only
                            keyValue = auxByteL + auxByteH;
                            if (chkDSInvert.Checked) 
                                keyValue = keyValue ^ 255;
                        }
                    }
                    OutParallelPort(LPTnum, keyValue);
                }
            }

        }
        // See if a copy of DDUtil is already running
        public static int myID;
        public static bool IsAppAlreadyRunning()
        {
            bool IsRunning = false;
            Process currentProcess = Process.GetCurrentProcess();
            myID = currentProcess.Id;
            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes)
            {
                if (currentProcess.Id != process.Id)
                {
                    if (currentProcess.ProcessName.Substring(0, 11) == process.ProcessName)
                    {
                        IsRunning = true;
                        break;
                    }
                }
            }
            return IsRunning;
        }
        // See if Flex Profiler is running
        public static bool IsFPRunning()
        {
            bool IsRunning = false;
            string proc = "FlexProfiler";
            Process[] processes = Process.GetProcessesByName(proc);
            if (processes.Length > 0)
            {
                IsRunning = true;
            }
            else
            {
                IsRunning = false;
            }
            return IsRunning;
        }
        // Display the PA Temp
        private void WriteTemp()
        {
            if (temp >= 70 && temp < 80)
            {
                SetTempColor(Color.Yellow);
            }
            else if (temp >= 80 && temp < 90)
            {
                SetTempColor(Color.Orange);
            }
            else if (temp >= 90)
            {
                SetTempColor(Color.Red);
            }
            else
            {
                SetTempColor(Color.LightGreen);
            }
            switch (temp_format)
            {
                case TempFormat.Celsius:
                    SetTemp(temp.ToString("f1") + "� C");
                    break;
                case TempFormat.Fahrenheit:
                    SetTemp(((temp * 1.8) + 32).ToString("f1") + "� F");
                    break;
            }
        }

        #endregion Helper Methods

        #region ACOM

        #region # Vars #

        int[] auto = new int[9];// band array for auto tune routine
        public bool bAmp = false;      // Amp is up
        public bool bAnt = true;      // ACU is up
        bool bBlink = false;    // message window 

        int ctr = 0;            // Auto tune reps counter
        int bctr = 0;           // reps counter for Mw command
        double p1 = 0;
        double p = 0;
        int fp = 0;             // final value returned from "P" command
        bool ADenab = false;
        string sFreq = "";

        #endregion Vars

        #region # Tables #

        string[,] ErrorDef =
        {
            {"",""},
            {"COVER INTERLOCK, OPEN",""},
            {"+5V TOO HIGH:    V","xx/40"},
            {"+12V TOO HIGH:    V","xx*.06"},
            {"-12V TOO HIGH:    V","(170-xx/4)/10"},
            {"LINE VOLTAGE TOO HIGH:    V","xx*1.25"},
            {"",""},
            {"SCREEN VOLTAGE ON BEFORE TIMEOUT UP",""},
            {"IG2:      mA SHOULD BE ZERO","xx"},
            {"IG1:      mA SHOULD BE ZERO","xx/10"},
            {"Ip:      mA SHOULD BE ZERO","xx*10"},
            {"",""},
            {"",""},
            {"+48V TOO HIGH:       V","xx*.024"},
            {"ANTENNA RELAY CLOSED SHOULD BE OPEN",""},
            {"INPUT RELAY CLOSED SHOULD BE OPEN",""},
            {"QSK BREAK-IN FAULT, CPU CONFLICT",""},
            {"RF PLATE:        V, SHOULD BE ZERO","xx*12"},
            {"RF GRID:        W, SHOULD BE ZERO","xx*xx/512"},
            {"HEATERS ON BEFORE TIMEOUT STARTED",""},
            {"",""},
            {"REAR TUBE TEMP TOO HIGH:     �C","xx*2-273"},
            {"FRONT TUBE TEMP TOO HIGH:     �C","xx*2-273"},
            {"HV TOO LOW AT POWER ON :       V","xx*16"},
            {"BIAS TOO LOW AT POWER ON :       V","xx/2"},
            {"",""},
            {"G2: CURRENT TOO HIGH:      mA","xx"},
            {"G1: EXCESSIVE GRID CURRENT:     mA","xx/10"},
            {"",""},
            {"",""},
            {"Ip PRESENT:       mA SHOULD BE ZERO","xx*10"},
            {"HEATER VOLTAGE TOO HIGH:      V","xx/16"},
            {"",""},
            {"HEATER CURRENT TOO HIGH:       A","xx/16"},
            {"",""},
            {"BIAS VOLTAGE TOO LOW:        V","xx/2"},
            {"G2 CONTROL CIRCUIT MALFUNCTION",""},
            {"AUTO PROTECT DRIVE POWER REMOVED",""},
            {"REMOVE AND REDUCE DRIVE POWER",""},
            {"HIGH  VOLTAGE TOO LOW:       kV","xx*.016"},
            {"G2 VOLTAGE TOO LOW:        V","xx*2"},
            {"HIGH  VOLTAGE TOO HIGH:        kV","xx*.016"},
            {"HEATER VOLTAGE TOO LOW:        V","xx/16"},
            {"HEATER CURRENT TOO LOW:        A","xx/16"},
            {"",""},
            {"LOW AIRFLOW",""},
            {"ANTENNA RELAY OPEN SHOULD BE CLOSED",""},
            {"DRIVE POWER TOO HIGH:       W","xx*xx/512"},
            {"REFLECTED POWER TOO HIGH:       W","xx*xx/100"},
            {"EXCESS PLATE CURRENT:        mA","xx*10"},
            {"EXCESSIVE G2 CURRENT:        mA","xx"},
            {"",""},
            {"",""},
            {"",""},
            {"ARC FAULT",""},
            {"DRIVE FREQUENCY OUT OF RANGE",""},
            {"",""},
            {"",""},
            {"BIAS CONTROL MALFUNCTION",""},
            {"BAND SWITCH SYSTEM MALFUNCTION",""},
            {"DETECTED RF POWER AT WRONG TIME",""},
            {"ADC - OVERFLOW",""},
            {"MICROPROCESSOR CONFLICT",""},
            {"FREQUENCY VIOLATION",""},
            {"",""},
            {"LOW GAIN",""},
            {"+5V TOO LOW:    V","xx/40"},
            {"+12V TOO LOW:     V","xx*.06"},
            {"-12V TOO LOW:     V","(170-xx/4)/10"},
            {"LINE VOLTAGE TOO LOW:     V","xx*1.25"},
            {"+48V TOO LOW:    V","xx*.24"},
            {"G2 VOLTAGE TOO HIGH:        V","xx*2"},
            {"",""},
            {"TUNE CAPACITOR SYSTEM MALFUNCTION",""},
            {"LOAD CAPACITOR SYSTEM MALFUNCTION",""},
            {"MOTOR VOLTAGE TOO LOW:         V","xx*.12"},
            {"MOTOR VOLTAGE TOO HIGH:         V","xx*.12"},
            {"+24V TOO HIGH:       V","xx*.12"},
            {"+24V TOO LOW:       V","xx*.12"},
            {"",""},
            {"ACU (Ant Ctrl Unit) Malfunction",""}
        };
        // Frequency segment low/high definations
        int[,] TableDef = 
        {
            {1750,1775}, {1775,1800}, {1800,1825}, {1825,1850}, {1850,1875}, {1875,1900}, 
            {1900,1925}, {1925,1950}, {1950,1975}, {1975,2000}, {2000,2025}, {2025,2050}, 
            {2050,2075}, {2075,2100}, {2100,2125}, {2125,2150}, {2150,2200}, {2200,2250}, 
            {2250,2300}, {2300,2350}, {2350,2400}, {2400,2450}, {2450,2500}, {2500,2550}, 
            {2550,2600}, {2600,2650}, {2650,2700}, {2700,2750}, {2750,2800}, {2800,2850}, 
            {2850,2900}, {2900,2950}, {2950,3000}, {3000,3050}, {3050,3100}, {3100,3150}, 
            {3150,3200}, {3200,3250}, {3250,3300}, {3300,3350}, {3350,3400}, {3400,3450}, 
            {3450,3500}, {3500,3550}, {3550,3600}, {3600,3650}, {3650,3700}, {3700,3750}, 
            {3750,3800}, {3800,3850}, {3850,3900}, {3900,3950}, {3950,4000}, {4000,4050}, 
            {4050,4100}, {4100,4150}, {4150,4200}, {4200,4250}, {4250,4300}, {4300,4350}, 
            {4350,4400}, {4400,4450}, {4450,4500}, {4500,4550}, {4550,4600}, {4600,4650}, 
            {4650,4700}, {4700,4750}, {4750,4800}, {4800,4850}, {4850,4900}, {4900,4950}, 
            {4950,5000}, {5000,5100}, {5100,5200}, {5200,5300}, {5300,5400}, {5400,5500}, 
            {5500,5600}, {5600,5700}, {5700,5800}, {5800,5900}, {5900,6000}, {6000,6100}, 
            {6100,6200}, {6200,6300}, {6300,6400}, {6400,6500}, {6500,6600}, {6600,6700}, 
            {6700,6800}, {6800,6900}, {6900,7000}, {7000,7100}, {7100,7200}, {7200,7300}, 
            {7300,7400}, {7400,7500}, {7500,7600}, {7600,7700}, {7700,7800}, {7800,7900}, 
            {7900,8000}, {8000,8100}, {8100,8200}, {8200,8300}, {8300,8400}, {8400,8500}, 
            {8500,8600}, {8600,8700}, {8700,8800}, {8800,8900}, {8900,9000}, {9000,9100}, 
            {9100,9200}, {9200,9300}, {9300,9400}, {9400,9500}, {9500,9600}, {9600,9700}, 
            {9700,9800}, {9800,9900}, {9900,10000}, {10000,10100}, {10100,10200}, {10200,10300}, 
            {10300,10400}, {10400,10500}, {10500,10600}, {10600,10700}, {10700,10800}, {10800,10900}, 
            {10900,11000}, {11000,11100}, {11100,11200}, {11200,11300}, {11300,11400}, {11400,11500}, 
            {11500,11600}, {11600,11700}, {11700,11800}, {11800,11900}, {11900,12000}, {12000,12100}, 
            {12100,12200}, {12200,12300}, {12300,12400}, {12400,12500}, {12500,12600}, {12600,12700}, 
            {12700,12800}, {12800,12900}, {12900,13000}, {13000,13100}, {13100,13200}, {13200,13300}, 
            {13300,13400}, {13400,13500}, {13500,13600}, {13600,13700}, {13700,13800}, {13800,13900}, 
            {13900,14000}, {14000,14100}, {14100,14200}, {14200,14300}, {14300,14400}, {14400,14500}, 
            {14500,14600}, {14600,14700}, {14700,14800}, {14800,14900}, {14900,15000}, {15000,15150}, 
            {15150,15300}, {15300,15450}, {15450,15600}, {15600,15750}, {15750,15900}, {15900,16050}, 
            {16050,16200}, {16200,16350}, {16350,16500}, {16500,16650}, {16650,16800}, {16800,16950}, 
            {16950,17100}, {17100,17250}, {17250,17400}, {17400,17550}, {17550,17700}, {17700,17850}, 
            {17850,18000}, {18000,18150}, {18150,18300}, {18300,18450}, {18450,18600}, {18600,18750}, 
            {18750,18900}, {18900,19050}, {19050,19200}, {19200,19350}, {19350,19500}, {19500,19650}, 
            {19650,19800}, {19800,19950}, {19950,20100}, {20100,20250}, {20250,20400}, {20400,20550}, 
            {20550,20700}, {20700,20850}, {20850,21000}, {21000,21150}, {21150,21300}, {21300,21450},
            {21450,21600}, {21600,21750}, {21750,21900}, {21900,22100}, {22100,22300}, {22300,22500}, 
            {22500,22750}, {22750,23000}, {23000,23250}, {23250,23500}, {23500,23750}, {23750,24000}, 
            {24000,24250}, {24250,24500}, {24500,24750}, {24750,25000}, {25000,25250}, {25250,25500}, 
            {25500,25750}, {25750,26000}, {26000,26250}, {26250,26500}, {26500,26800}, {26800,27100}, 
            {27100,27400}, {27400,27700}, {27700,28000}, {28000,28300}, {28300,28600}, {28600,28900}, 
            {28900,29200}, {29200,29500}, {29500,29800}, {29800,30000}
        };
        #endregion Tables

        #region # Delegates #

        // Write to Temp window
        delegate void SetAtempCallback(string text);
        public void SetAtemp(string text)
        {
            if (this.txtAlphaInt.InvokeRequired)
            {
                SetAtempCallback d = new SetAtempCallback(SetAtemp);
                this.Invoke(d, new object[] { text });
            }
            else txtAlphaInt.Text = text;
        }
        // Enable/Disable Tune window
        delegate void EnabTuneCallback(bool b);
        public void EnabTune(bool b)
        {
            if (this.txtTune.InvokeRequired)
            {
                EnabTuneCallback d = new EnabTuneCallback(EnabTune);
                this.Invoke(d, new object[] { b });
            }
            else txtTune.Enabled = b;
        }
        // Enable/Disable band group
        delegate void EnabBandCallback(bool b);
        public void EnabBand(bool b)
        {
            if (this.grpAmpBand.InvokeRequired)
            {
                EnabBandCallback d = new EnabBandCallback(EnabBand);
                this.Invoke(d, new object[] { b });
            }
            else grpAmpBand.Enabled = b;
        }

        #endregion Delegates

        #region # Events #

        // the ACOM has been selected
        private void ACOM2K_Click(object sender, EventArgs e)
        {
            reg = @".*?[\r|\0]"; //set regex mask
            aCOM2000AToolStripMenuItem.Checked = true;
            alpToolStripMenuItem.Checked = false;
            grpAmp.Text = "ACOM 2000A";
            mini.grpAmp.Text = "ACOM 2000A";
            cboAlphaBaud.SelectedIndex = set.AlphaBaud;
            //chkAutoDrv.Checked = set.chkAutoDrv;
            //autoDriveToolStripMenuItem1.Checked = set.chkAutoDrv;
            numDrive.Value = set.tune;
            cboAlphaBaud.SelectedIndex = 0;
            AlphaPort.DtrEnable = false;
            AlphaPort.RtsEnable = false;
            lblAmpInt.Text = "Air Temp"; txtAlphaInt.Text = "0";
            lblTune.Text = "  Ant"; lblLoad.Text = "User";
            Amp = 0;
            bAmp = false;
            set.theAmp = 0; set.Save();
            AlphaTimer.Interval = 60000; // PC hear byte update timer
            AlphaTimer.Enabled = true;
            txtTune.ReadOnly = false;
            grpAmpBand.Visible = true;
            SetpaTune(set.AlphaAnt.ToString()); //EnabTune(false);
            btnHF.Visible = false; btnSF.Visible = false;
            lblHF.Visible = false; lblSF.Visible = false;
            mini.btnHF.Visible = false; mini.btnSF.Visible = false;
            mini.lblHF.Visible = false; mini.lblSF.Visible = false;
            mini.txtTune.Visible = true; mini.lblTune.Visible = true;
            this.toolTip1.SetToolTip(this.btnTune,
                "Press to start AutoTune procedure.");
            this.toolTip1.SetToolTip(this.btnHV, "");
            this.toolTip1.SetToolTip(this.btnHF, "");
            this.toolTip1.SetToolTip(this.btnSF, "");
            this.toolTip1.SetToolTip(this.txtTune,
                "Enter Antenna # or Double-Click to increment.");
            this.toolTip1.SetToolTip(this.txtLoad,
                "Shows whether Default or User PA Settings are applied");
            this.toolTip1.SetToolTip(this.txtMsg,
                "Double-Click to disable blinking");
            this.toolTip1.SetToolTip(this.txtAlphaInt, "");
            // restore bands settings
            chkB160.Checked = set.chkB160;
            chkB80.Checked = set.chkB80;
            chkB40.Checked = set.chkB40;
            chkB30.Checked = set.chkB30;
            chkB20.Checked = set.chkB20;
            chkB17.Checked = set.chkB17;
            chkB15.Checked = set.chkB15;
            chkB12.Checked = set.chkB12;
            chkB10.Checked = set.chkB10;
        }
        // a band check box has changed
        private void chkB160_CheckedChanged(object sender, EventArgs e)
        {
            if (chkB160.Checked) set.chkB160 = true;
            else
                set.chkB160 = false;
            set.Save();
        }
        // a band check box has changed
        private void chkB80_CheckedChanged(object sender, EventArgs e)
        {
            if (chkB80.Checked) set.chkB80 = true;
            else
                set.chkB80 = false;
            set.Save();
        }
        // a band check box has changed
        private void chkB40_CheckedChanged(object sender, EventArgs e)
        {
            if (chkB40.Checked) set.chkB40 = true;
            else
                set.chkB40 = false;
            set.Save();
        }
        // a band check box has changed
        private void chkB30_CheckedChanged(object sender, EventArgs e)
        {
            if (chkB30.Checked) set.chkB30 = true;
            else
                set.chkB30 = false;
            set.Save();
        }
        // a band check box has changed
        private void chkB20_CheckedChanged(object sender, EventArgs e)
        {
            if (chkB20.Checked) set.chkB20 = true;
            else
                set.chkB20 = false;
            set.Save();
        }
        // a band check box has changed
        private void chkB17_CheckedChanged(object sender, EventArgs e)
        {
            if (chkB17.Checked) set.chkB17 = true;
            else
                set.chkB17 = false;
            set.Save();
        }
        // a band check box has changed
        private void chkB15_CheckedChanged(object sender, EventArgs e)
        {
            if (chkB15.Checked) set.chkB15 = true;
            else
                set.chkB15 = false;
            set.Save();
        }
        // a band check box has changed
        private void chkB12_CheckedChanged(object sender, EventArgs e)
        {
            if (chkB12.Checked) set.chkB12 = true;
            else
                set.chkB12 = false;
            set.Save();
        }
        // a band check box has changed
        private void chkB10_CheckedChanged(object sender, EventArgs e)
        {
            if (chkB10.Checked) set.chkB10 = true;
            else
                set.chkB10 = false;
            set.Save();
        }
        // the antenna value has changed
        public void txtTune_TextChanged(object sender, EventArgs e)
        {
            if (Amp == 0 && bAnt)
            {
                string ant = txtTune.Text;
                if (AlphaPort.IsOpen) AlphaPort.Write("AqW3" + ant + "\0");
                for (int k = 0; k < 5; k++)
                {
                    Thread.Sleep(100);
                    Application.DoEvents();
                }
                set.AlphaAnt = Convert.ToInt32(txtTune.Text);
                set.Save();
            }
        }
        // The ant has changed send a command to the amp
        public void txtTune_DoubleClick(object sender, EventArgs e)
        {
            if (Amp == 0 && bAnt)
            {
                int ant = Convert.ToInt32(txtTune.Text);
                if (ant < 9) ant += 1;
                else ant = 0;
                SetpaTune(ant.ToString());
                //if (AlphaPort.IsOpen) AlphaPort.Write("AqW3" + ant + "\0");
                //for (int k = 0; k < 5; k++)
                //{
                //    Thread.Sleep(100);
                //    Application.DoEvents();
                //}

            }
        }
        // The User window has been dbl-clicked
        private void txtLoad_DoubleClick(object sender, EventArgs e)
        {
            if (Amp == 0)
            {
                //if (AlphaPort.IsOpen) AlphaPort.Write("AqJ1\0");
                //Thread.Sleep(200);
            }
        }
        // the message window was dbl-clicked
        private void txtMsg_DoubleClick(object sender, EventArgs e)
        {
            blinkTimer.Enabled = false;
            txtMsg.BackColor = Color.LightYellow;
        }
        // the check all bands button was pressed
        private void btnBandAll_Click(object sender, EventArgs e)
        {
            chkB160.Checked = true; chkB80.Checked = true; chkB40.Checked = true;
            chkB30.Checked = true; chkB20.Checked = true; chkB17.Checked = true;
            chkB15.Checked = true; chkB12.Checked = true; chkB10.Checked = true;
        }
        // the un-check all bands button was pressed
        private void btnBandClear_Click(object sender, EventArgs e)
        {
            chkB160.Checked = false; chkB80.Checked = false; chkB40.Checked = false;
            chkB30.Checked = false; chkB20.Checked = false; chkB17.Checked = false;
            chkB15.Checked = false; chkB12.Checked = false; chkB10.Checked = false;
        }
        // the Band group info button was pressed
        private void btnInfo_Click(object sender, EventArgs e)
        {
            Process.Start("http://k5fr.com/ddutilwiki/index.php?title=How_To#Setup_a_ACOM2000A_Linear_Amplifier");
        }
        // The save drive button was pressed.
        public void btnDrive_Click(object sender, EventArgs e)
        {
            if (rbAmp1.Checked)
            {
                switch (band)
                {
                    case "160": set.pwr1 = State["_ZZPC"].ToString(); break;
                    case "080": set.pwr2 = State["_ZZPC"].ToString(); break;
                    case "040": set.pwr3 = State["_ZZPC"].ToString(); break;
                    case "030": set.pwr4 = State["_ZZPC"].ToString(); break;
                    case "020": set.pwr5 = State["_ZZPC"].ToString(); break;
                    case "017": set.pwr6 = State["_ZZPC"].ToString(); break;
                    case "015": set.pwr7 = State["_ZZPC"].ToString(); break;
                    case "012": set.pwr8 = State["_ZZPC"].ToString(); break;
                    case "010": set.pwr9 = State["_ZZPC"].ToString(); break;
                    case "006": set.pwr10 = State["_ZZPC"].ToString(); break;
                }
            }
            else if (rbAmp2.Checked)
            {
                switch (band)
                {
                    case "160": set.pwr1_2 = State["_ZZPC"].ToString(); break;
                    case "080": set.pwr2_2 = State["_ZZPC"].ToString(); break;
                    case "040": set.pwr3_2 = State["_ZZPC"].ToString(); break;
                    case "030": set.pwr4_2 = State["_ZZPC"].ToString(); break;
                    case "020": set.pwr5_2 = State["_ZZPC"].ToString(); break;
                    case "017": set.pwr6_2 = State["_ZZPC"].ToString(); break;
                    case "015": set.pwr7_2 = State["_ZZPC"].ToString(); break;
                    case "012": set.pwr8_2 = State["_ZZPC"].ToString(); break;
                    case "010": set.pwr9_2 = State["_ZZPC"].ToString(); break;
                    case "006": set.pwr10_2 = State["_ZZPC"].ToString(); break;
                }
            }
            set.Save();
            //WriteToPort("ZZPC;", iSleep);
        }
        // the drive level has changed
        private void numDrive_ValueChanged(object sender, EventArgs e)
        {
            set.tune = numDrive.Value;
            set.Save();
        }
        // the AT timer has elasped, amp not responding
        void AtTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            WriteToPort("ZZTU0;", iSleep); //turn off tune
            tune = "Off";
            SetTune("abort");
            SetMsg("Auto Tune Aborted, Toggle Enable to reset.");
            AtTimer.Stop();
        }

        #endregion Events

        #region # Methods #

        // Find the proper frequency segment from TableDef
        int GetSeg(int tfreq)
        {
            for (int i = 0; i < TableDef.Length; i++)
            {
                if (tfreq >= TableDef[i, 0] && tfreq < TableDef[i, 1])
                    return i;
            }
            return -1;
        }
        // Convert 2 Ascii bytes into decimal string 
        int AsciiDec(string msg)
        {
            string sseg = "";
            // re-combine ascii segment into hex string
            if (msg.Substring(0, 1) == ":") sseg = "A";
            else if (msg.Substring(0, 1) == ";") sseg = "B";
            else if (msg.Substring(0, 1) == "<") sseg = "C";
            else if (msg.Substring(0, 1) == "=") sseg = "D";
            else if (msg.Substring(0, 1) == ">") sseg = "E";
            else if (msg.Substring(0, 1) == "?") sseg = "F";
            else sseg = msg.Substring(0, 1);

            if (msg.Substring(1, 1) == ":") sseg += "A";
            else if (msg.Substring(1, 1) == ";") sseg += "B";
            else if (msg.Substring(1, 1) == "<") sseg += "C";
            else if (msg.Substring(1, 1) == "=") sseg += "D";
            else if (msg.Substring(1, 1) == ">") sseg += "E";
            else if (msg.Substring(1, 1) == "?") sseg += "F";
            else sseg += msg.Substring(1, 1);
            // convert hex string into decimal
            int iseg = int.Parse(sseg, NumberStyles.HexNumber);
            return iseg;
        }
        // Process command messages from the amp.
        void DataAcom(string msg)
        {
            try
            {
                msg = msg.Replace("*", ""); // '*' indicates a 8x, 90 msg, ignore it.
                if (msg.Length > 3)
                {
                    if (msg.Substring(1, 1) == "q") { return; } // my echo
                    if (msg.Substring(0, 2) == "QA")
                    {   // if message from amp to selector, repeat back to the network
                        int len = msg.Length;
                        byte[] bytes = new byte[len];
                        for (int i = 0; i < len; i++)
                        {
                            string sHex = "";
                            string sTemp = msg.Substring(i, 1);
                            if (sTemp == "?")
                            { sHex += "FF"; }
                            else
                            {
                                sHex = Convert.ToInt32(Convert.ToChar(sTemp)).ToString("X2");
                            }
                            bytes[i] = byte.Parse(sHex, NumberStyles.HexNumber); ;
                            //                            Console.Write("{0:x2} ", bytes[i]);
                        }
                        //                        Console.WriteLine();
                        if (AlphaPort.IsOpen) { AlphaPort.Write(bytes, 0, len); }
                        return;
                    }

                    if (msg.Contains("A0")) // Power Off executed
                    {
                        SetMsg("Amp is shutting down!");
                        SetPwr("Off"); btnPwr.BackColor = Color.Empty;
                        ac = "Off"; mini.btnPwr.BackColor = Color.Empty;
                        return;
                    }
                    if (msg.Contains("A1")) // Amp Filiments On, warming up
                    {
                        SetMsg("Amp is warming, please wait!");
                        SetPwr("On"); //btnPwr.BackColor = Color.Lime;
                        ac = "On"; //mini.btnPwr.BackColor = Color.Lime;
                        AlphaPort.DtrEnable = false; AlphaPort.RtsEnable = false;
                        SetOper("Wait"); //btnOper.BackColor = Color.Pink;
                        state = "Wait"; //mini.btnOper.BackColor = Color.Pink;
                        SetHV("Off"); btnHV.BackColor = Color.Empty;
                        mode = "Off"; mini.btnHV.BackColor = Color.Empty;
                        return;
                    }
                    if (msg.Contains("A2")) // Amp Warm up done, HV on
                    {
                        AlphaPort.DtrEnable = false; AlphaPort.RtsEnable = false;
                        SetMsg("Amp is ready!");
                        SetPwr("On"); //btnPwr.BackColor = Color.Lime;
                        ac = "On"; //mini.btnPwr.BackColor = Color.Lime;
                        SetOper("Stby"); //btnOper.BackColor = Color.Yellow;
                        state = "Stby"; //mini.btnOper.BackColor = Color.Yellow;
                        SetTune("Rdy");
                        btnTune.BackColor = Color.Azure;
                        mini.btnTune.BackColor = Color.Azure;
                        SetHV("On"); //btnHV.BackColor = Color.Lime;
                        mode = "High"; //mini.btnHV.BackColor = Color.Lime;
                        return;
                    }
                    if (msg.Contains("A3")) // Amp is Off, but cooling
                    {
                        bAmp = false;
                        SetMsg("Amp is off & cooling down!");
                        SetPwr("Off"); //btnPwr.BackColor = Color.Yellow;
                        ac = "Off"; //mini.btnPwr.BackColor = Color.Yellow;
                        SetOper("Off"); btnOper.BackColor = Color.Empty;
                        state = "Cool"; mini.btnOper.BackColor = Color.Empty;
                        SetTune("Off");
                        btnTune.BackColor = Color.Empty;
                        mini.btnTune.BackColor = Color.Empty;
                        SetHV("Off"); btnHV.BackColor = Color.Empty;
                        mode = "Off"; mini.btnHV.BackColor = Color.Empty;
                        return;
                    }
                    if (msg.Contains("A4")) // Amp is Off
                    {
                        bAmp = false;
                        SetMsg("Amp power down complete!");
                        SetPwr("Off"); btnPwr.BackColor = Color.Empty;
                        ac = "Off"; mini.btnPwr.BackColor = Color.Empty;
                        SetOper("Off"); btnOper.BackColor = Color.Empty;
                        state = "Off"; mini.btnOper.BackColor = Color.Empty;
                        SetTune("Off");
                        btnTune.BackColor = Color.Empty;
                        mini.btnTune.BackColor = Color.Empty;
                        SetHV("Off"); btnHV.BackColor = Color.Empty;
                        mode = "Off"; mini.btnHV.BackColor = Color.Empty;
                        return;
                    }
                    if (msg.Contains("AE")) // Error occurred
                    {
                        MathParser mp = new MathParser();
                        int param = 0;
                        int pos = msg.LastIndexOf("AE") + 2;
                        int err = int.Parse(msg.Substring(pos, 2), NumberStyles.HexNumber);
                        string errDlg = ErrorDef[err, 0]; //get msg from table
                        if (msg.Length > 6)
                        {   // if msg has parameter process it
                            param = int.Parse(msg.Substring(5, 2), NumberStyles.HexNumber);
                            string calc = ErrorDef[err, 1];
                            string c1 = Regex.Replace(calc, "(xx)", param.ToString());
                            string result = mp.Calculate(c1).ToString();
                            string regx = @"(.*?:)\s+(\w+)";
                            string mask = "$1 " + result + " $2";
                            errDlg = Regex.Replace(errDlg, regx, mask);
                        }
                        SetMsg("Error: " + msg.Substring(pos - 1, 3).ToString() + ": " + errDlg);
                        txtMsg.BackColor = Color.Yellow;
                        blinkTimer.Enabled = true;
                        bBlink = true;
                        return;
                    }
                    if (msg.Contains("AI")) // Tube Temp
                    {
                        int p = msg.LastIndexOf("I") + 1;
                        int temp = Convert.ToInt32(msg.Substring(p, 1)) * 10;
                        SetAtemp(temp.ToString() + " C");
                        return;
                    }
                    if (msg.Contains("AL")) // Amp self broadcast
                    {
                        if (!bAmp) // if 1st pass do initialization
                        {
                            bAmp = true;
                            AlphaPort.DtrEnable = false; AlphaPort.RtsEnable = false;
                            if (AlphaPort.IsOpen) AlphaPort.Write("AqMww\0"); // get screen volts ? Oper/Stby
                            Thread.Sleep(200);
                            bctr = 0;
                            if (AlphaPort.IsOpen) AlphaPort.Write("QqQ\0"); // get ACU status
                            Thread.Sleep(200);
                            lastFreq = ""; // force freq update
                            SetPwr("On"); 
                            ac = "On"; 
                            SetTune("Rdy");
                            btnTune.BackColor = Color.Azure;
                            mini.btnTune.BackColor = Color.Azure;
                            SetHV("On"); 
                            mode = "On"; 
                        }
                        return;
                    }
                    if (msg.Contains("AO")) // amp in Operate mode
                    {
                        SetOper("Oper"); state = "Oper";
                        if (chkAutoDrv.Checked)
                        {
                            if (rbAmp1.Checked)
                            {
                                switch (band)
                                {
                                    case "160": WriteToPort("ZZPC" + set.pwr1 + ";", iSleep); break;
                                    case "080": WriteToPort("ZZPC" + set.pwr2 + ";", iSleep); break;
                                    case "040": WriteToPort("ZZPC" + set.pwr3 + ";", iSleep); break;
                                    case "030": WriteToPort("ZZPC" + set.pwr4 + ";", iSleep); break;
                                    case "020": WriteToPort("ZZPC" + set.pwr5 + ";", iSleep); break;
                                    case "017": WriteToPort("ZZPC" + set.pwr6 + ";", iSleep); break;
                                    case "015": WriteToPort("ZZPC" + set.pwr7 + ";", iSleep); break;
                                    case "012": WriteToPort("ZZPC" + set.pwr8 + ";", iSleep); break;
                                    case "010": WriteToPort("ZZPC" + set.pwr9 + ";", iSleep); break;
                                }
                            }
                            if (rbAmp2.Checked)
                            {
                                switch (band)
                                {
                                    case "160": WriteToPort("ZZPC" + set.pwr1_2 + ";", iSleep); break;
                                    case "080": WriteToPort("ZZPC" + set.pwr2_2 + ";", iSleep); break;
                                    case "040": WriteToPort("ZZPC" + set.pwr3_2 + ";", iSleep); break;
                                    case "030": WriteToPort("ZZPC" + set.pwr4_2 + ";", iSleep); break;
                                    case "020": WriteToPort("ZZPC" + set.pwr5_2 + ";", iSleep); break;
                                    case "017": WriteToPort("ZZPC" + set.pwr6_2 + ";", iSleep); break;
                                    case "015": WriteToPort("ZZPC" + set.pwr7_2 + ";", iSleep); break;
                                    case "012": WriteToPort("ZZPC" + set.pwr8_2 + ";", iSleep); break;
                                    case "010": WriteToPort("ZZPC" + set.pwr9_2 + ";", iSleep); break;
                                }
                            }
                            return;
                        }
                    }
                    if (msg.Contains("AP")) // auto tune reply
                    {   // convert P digit to numeric so we can test it
                        int pos = msg.LastIndexOf("P") + 1;
                        p = Convert.ToInt32(msg.Substring(pos, 1));
                        fp = Convert.ToInt32(p);
                        if (p > 4 && p < 9)
                        {
                            if (ctr == 0) //1st pass
                            {
                                p1 = p; // save p num
                                for (int i = 0; i < 10; i++)
                                {   // delay 1 sec
                                    Thread.Sleep(100);
                                    Application.DoEvents();
                                }
                                if (AlphaPort.IsOpen) AlphaPort.Write("AqP\0");
                                // re-send 'P' command
                                Thread.Sleep(200);
                                ctr += 1;
                                return;
                            }
                            else if (ctr >= 1 && (p >= p1 - 1 && p <= p1 + 1))
                            {   // >= 2nd pass and drive is stable +/- 1
                                if (AlphaPort.IsOpen)
                                { AlphaPort.Write("AqQ\0"); } // start auto tune
                                Thread.Sleep(200);
                                return;
                            }
                            else
                            {
                                if (ctr <= 5)
                                {   // wait another sec and send 'P' command
                                    for (int i = 0; i < 10; i++)
                                    {
                                        Thread.Sleep(100);
                                        Application.DoEvents();
                                    }
                                    if (AlphaPort.IsOpen) AlphaPort.Write("AqP\0");
                                    Thread.Sleep(200);
                                    ctr += 1;
                                    return;
                                }
                                else
                                {   // had enough split
                                    WriteToPort("ZZTU0;", iSleep); //turn off tune
                                    tune = "Off";
                                    return;
                                }
                            }
                        }
                        else if (ctr < 6 && p < 5) // if to wait another 1/2 second
                        {
                            Thread.Sleep(500);
                            if (AlphaPort.IsOpen) AlphaPort.Write("AqP\0");
                            Thread.Sleep(200);
                            ctr += 1;
                            return;
                        }
                        else  // kill the sequence after 5 loops and P < 5
                        {
                            WriteToPort("ZZTU0;", iSleep); //turn off tune
                            tune = "Off";
                            SetMsg("Auto Tune failed, P = " + p);
                            //if (AlphaPort.IsOpen) AlphaPort.Write("AqJ1\0");
                            //Thread.Sleep(200);
                            return;
                        }
                    }
                    if (msg.Contains("AQ")) // Autotune result message
                    {
                        WriteToPort("ZZTU0;", iSleep); //turn off tune
                        AtTimer.Stop();
                        tune = "Off";
                        int pos = msg.LastIndexOf("Q") + 1;
                        switch (msg.Substring(pos, 1))
                        {
                            case "0": SetTune("Fail");
                                SetMsg("Auto Tune Failure! (P" + p + ") " + txtFreq.Text + " MHz");
                                break;
                            case "1": SetTune("OK");
                                SetMsg("Auto Tune Succeeded (P" + p + ") " + txtFreq.Text + " MHz");
                                break;
                            case "2": SetTune("Error");
                                SetMsg("Auto Tune Error! Drive Unstable (P" + p + ") " + txtFreq.Text + " MHz");
                                break;
                            case "3": SetTune("Rdy");
                                btnTune.BackColor = Color.Azure;
                                mini.btnTune.BackColor = Color.Azure;
                                if (!bAnt)
                                    ChkMoBands(); // go see if there are other bands to tune
                                else
                                {
                                    if (AlphaPort.IsOpen) AlphaPort.Write("AqS\0");
                                    Thread.Sleep(1000);
                                    //if (AlphaPort.IsOpen) AlphaPort.Write("AqJ1\0");
                                    //Thread.Sleep(200);
                                    if (AlphaPort.IsOpen) AlphaPort.Write("AqO\0");
                                    Thread.Sleep(1000);
                                }
                                break;
                        }
                        return;
                    }
                    if (msg.Contains("AS")) // amp in Stand By mode
                    {
                        SetOper("Stby"); state = "Stby"; 
                        return;
                    }
                    if (msg.Contains("AW1")) // Seg/Ant/User/Band/
                    {   // 71 41 57 31 3A 33 31 31 33 0D (qAW1A3113
                        int pos = msg.LastIndexOf("W1") + 2;
                        //Get the freq segment & display on console
                        int iseg = AsciiDec(msg.Substring(pos, 2));
                        SetSeg(iseg.ToString());

                        // get ant number & display on console
                        if (bAnt) SetpaTune(msg.Substring(pos + 2, 1).ToString());

                        // get user info & display on console
                        if (msg.Substring(pos + 3, 1) == "0") SetLoad("Def");
                        else if (msg.Substring(pos + 3, 1) == "1") SetLoad("Use");
                        else SetLoad("Err");

                        //get band data & display on console
                        switch (msg.Substring(pos + 4, 1))
                        {
                            case "1": SetBand("160"); break;
                            case "2": SetBand("80"); break;
                            case "3": SetBand("40"); break;
                            case "4": SetBand("30"); break;
                            case "5": SetBand("20"); break;
                            case "6": SetBand("17"); break;
                            case "7": SetBand("15"); break;
                            case "8": SetBand("12"); break;
                            case "9": SetBand("10"); break;
                        }
                        return;
                    }
                    if (msg.Contains("Aw")) // Amp measurement reply (screen volts)
                    {
                        int p = msg.LastIndexOf("w") + 1;
                        int sv = AsciiDec(msg.Substring(p, 2));
                        if (sv > 70)
                        {
                            SetOper("Oper"); state = "Oper"; 
                        }
                        else
                        {
                            SetOper("Stby"); state = "Stby";
                        }
                        bctr++;
                        if (bctr > 3)   // let the measuring continue for 3 reps
                        {   // then turn off measuring
                            if (AlphaPort.IsOpen)
                                if (AlphaPort.IsOpen) { AlphaPort.Write("AqMM\0"); }
                            Thread.Sleep(200);
                        }
                        return;
                    }
                    if (msg.Contains("qQZ")) // ACU is ready
                    {
                        bAnt = true;
                        EnabTune(true);
                        EnabBand(false);
                        SetMsg("Ant Ctrl Unit Ready");
                        return;
                    }
                }// end if msg.len >3
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                    "Unable to write to log");
            }

        }
        // start Auto Tune sequence
        void DoAuto()
        {
            DialogResult result;
            result = MessageBox.Show(
            "You are about to initiate an Auto Tune calibration routine for your\r" +
            "ACOM2000A amplifier. If this isn't what you intended, exit by pressing Cancel.\r\r" +
            "Before going any further please make sure you understand what this command\r" +
            "is for and how it works. For a description of this procedure press the Info\r" +
            "button in the Band group.\r\r" +
            "This procedure puts the radio into transmit mode so be observant\r" +
            "and prepared to act in case of a hang-up or unusual operation.\r\r" +
            "Pressing the Tune button on the PowerSDR Console will un-key the radio.\r\r" +
            "Press the OK button to start the calibration procedure.\r\r" +
            "When the procedure is finished the radio will return to normal operation.",
            "Auto Tune Procedure",
            MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);

            if (result != DialogResult.OK) { return; }
            bool oneBand = false;
            // read the bands matrix and setup array
            if (chkB160.Checked)
            { auto[0] = 1; oneBand = true; }
            else auto[0] = 0;
            if (chkB80.Checked)
            { auto[1] = 1; oneBand = true; }
            else auto[1] = 0;
            if (chkB40.Checked)
            { auto[2] = 1; oneBand = true; }
            else auto[2] = 0;
            if (chkB30.Checked)
            { auto[3] = 1; oneBand = true; }
            else auto[3] = 0;
            if (chkB20.Checked)
            { auto[4] = 1; oneBand = true; }
            else auto[4] = 0;
            if (chkB17.Checked)
            { auto[5] = 1; oneBand = true; }
            else auto[5] = 0;
            if (chkB15.Checked)
            { auto[6] = 1; oneBand = true; }
            else auto[6] = 0;
            if (chkB12.Checked)
            { auto[7] = 1; oneBand = true; }
            else auto[7] = 0;
            if (chkB10.Checked)
            { auto[8] = 1; oneBand = true; }
            else auto[8] = 0;

            if (!oneBand)
            {
                MessageBox.Show(new Form() { TopMost = true }, 
                    "To run the Auto Tune procedure, at\r" +
                    "least one (1) band must be checked", "Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            ADenab = chkAutoDrv.Checked; // save Enable Auto Drive check box
            EnabAD(false);
            //if (AlphaPort.IsOpen) AlphaPort.Write("AqJ0\0");
            //Thread.Sleep(200);
            if (AlphaPort.IsOpen) AlphaPort.Write("AqO\0"); // make sure amp is in operate
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(100);
                Application.DoEvents();
            }
            sFreq = lastFreq;
            WriteToPort("ZZSP0;", iSleep);      // make sure split is off
            WriteToPort("ZZTO0" + numDrive.Value.ToString() + ";", iSleep);    // set PSDR tune level
            if (bAnt) // if antenna selector present, only tune current band
            {
                SetTune("wait");
                btnTune.BackColor = Color.Pink;
                AtTimer.Enabled = true;         //start fail safe timer
                WriteToPort("ZZTU1;", 500);     // key radio, wait for 1/2 sec
                if (AlphaPort.IsOpen) AlphaPort.Write("AqP\0");   // get drive setting from amp
                Thread.Sleep(200);
            }
            else ChkMoBands();                  // go check what bands are set
        }
        // see if any bands are set to auto tune
        void ChkMoBands()
        {
            for (int i = 0; i < 9; i++)
            {   // iterate the auto array for bands that are set 0=no, 1=yes
                if (auto[i] == 1)
                {
                    SetTune("wait");
                    btnTune.BackColor = Color.Pink;
                    switch (i)
                    {
                        case 0: if (band != "160") { WriteToPort("ZZBS160;", iSleep); } break;
                        case 1: if (band != "080") { WriteToPort("ZZBS080;", iSleep); } break;
                        case 2: if (band != "040") { WriteToPort("ZZBS040;", iSleep); } break;
                        case 3: if (band != "030") { WriteToPort("ZZBS030;", iSleep); } break;
                        case 4: if (band != "020") { WriteToPort("ZZBS020;", iSleep); } break;
                        case 5: if (band != "017") { WriteToPort("ZZBS017;", iSleep); } break;
                        case 6: if (band != "015") { WriteToPort("ZZBS015;", iSleep); } break;
                        case 7: if (band != "012") { WriteToPort("ZZBS012;", iSleep); } break;
                        case 8: if (band != "010") { WriteToPort("ZZBS010;", iSleep); } break;
                    }
                    auto[i] = 0; // clear this band so it won't be run again
                    ctr = 0;     // start the loop counter at zero
                    for (int k = 0; k < 30; k++)
                    {   // Delay for k * 100 ms.
                        Thread.Sleep(100);
                        Application.DoEvents();
                    }
                    AtTimer.Enabled = true; //start fail safe timer
                    WriteToPort("ZZTU1;", 500); // key radio, wait for 1/2 sec
                    if (AlphaPort.IsOpen) AlphaPort.Write("AqP\0");   // get drive setting from amp
                    Thread.Sleep(200);
                    return;                     // stop the loop & wait for 'P' setting
                }
            }
            if (AlphaPort.IsOpen) AlphaPort.Write("AqS\0");
            Thread.Sleep(1000);
            //if (AlphaPort.IsOpen) AlphaPort.Write("AqJ1\0");
            //Thread.Sleep(200);
            if (AlphaPort.IsOpen) AlphaPort.Write("AqO\0");
            Thread.Sleep(1000);
            WriteToPort("ZZFA" + sFreq + ";", iSleep);    // reset the radio freq
            EnabAD(ADenab); // restore Enable Auto Drive check box from var
        }

        #endregion # Methods #

        #endregion ACOM

        #region Alpha 87A

        #region # Delegates #

        // Write to Pwr button
        delegate void SetPwrCallback(string text);
        public void SetPwr(string text)
        {
            if (this.btnPwr.InvokeRequired)
            {
                SetPwrCallback d = new SetPwrCallback(SetPwr);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                btnPwr.Text = text; mini.btnPwr.Text = text;
                if (text == "Off")
                {
                    btnPwr.BackColor = Color.Empty;
                    mini.btnPwr.BackColor = Color.Empty;
                }
                else if (text == "On")
                {
                    btnPwr.BackColor = Color.Lime;
                    mini.btnPwr.BackColor = Color.Lime;
                }
            }
        }
        // Write to Oper button
        delegate void SetOperCallback(string text);
        public void SetOper(string text)
        {
            if (this.btnOper.InvokeRequired)
            {
                SetOperCallback d = new SetOperCallback(SetOper);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                btnOper.Text = text; mini.btnOper.Text = text;
                if (text == "Off")
                {
                    btnOper.BackColor = Color.Empty;
                    mini.btnOper.BackColor = Color.Empty;
                }
                else if (text == "Stby")
                {
                    btnOper.BackColor = Color.Yellow;
                    mini.btnOper.BackColor = Color.Yellow;
                }
                else if (text == "Wait")
                {
                    btnOper.BackColor = Color.Pink;
                    mini.btnOper.BackColor = Color.Pink;
                }
                else if (text == "Oper")
                {
                    btnOper.BackColor = Color.Lime;
                    mini.btnOper.BackColor = Color.Lime;
                }
            }
        }
        // Write to Tune button
        delegate void SetTuneCallback(string text);
        public void SetTune(string text)
        {
            if (this.btnTune.InvokeRequired)
            {
                SetTuneCallback d = new SetTuneCallback(SetTune);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                btnTune.Text = text; mini.btnTune.Text = text;
                if (text == "Off")
                {
                    btnTune.BackColor = Color.Empty;
                    mini.btnTune.BackColor = Color.Empty;
                }
                else if (text == "Man")
                {
                    btnTune.BackColor = Color.Yellow;
                    mini.btnTune.BackColor = Color.Yellow;
                }
                else if (text == "Auto")
                {
                    btnTune.BackColor = Color.Lime;
                    mini.btnTune.BackColor = Color.Lime;
                }
            }
        }
        // Write to HV button
        delegate void SetHVCallback(string text);
        public void SetHV(string text)
        {
            if (this.btnHV.InvokeRequired)
            {
                SetHVCallback d = new SetHVCallback(SetHV);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                btnHV.Text = text; mini.btnHV.Text = text;
                if (text == "Off")
                {
                    btnHV.BackColor = Color.Empty;
                    mini.btnHV.BackColor = Color.Empty;
                }
                else if (text == "Low")
                {
                    btnHV.BackColor = Color.Yellow;
                    mini.btnHV.BackColor = Color.Yellow;
                }
                else if (text == "High" || text == "On")
                {
                    btnHV.BackColor = Color.Lime;
                    mini.btnHV.BackColor = Color.Lime;
                }
            }
        }
        // Write to Band window
        delegate void SetBandCallback(string text);
        public void SetBand(string text)
        {
            if (this.txtBand.InvokeRequired)
            {
                SetBandCallback d = new SetBandCallback(SetBand);
                this.Invoke(d, new object[] { text });
            }
            else txtBand.Text = text;
        }
        // Write to Seg window
        delegate void SetSegCallback(string text);
        public void SetSeg(string text)
        {
            if (this.txtSeg.InvokeRequired)
            {
                SetSegCallback d = new SetSegCallback(SetSeg);
                this.Invoke(d, new object[] { text });
            }
            else txtSeg.Text = text;
        }
        // Write to Msg window
        delegate void SetMsgCallback(string text);
        public void SetMsg(string text)
        {
            if (this.txtMsg.InvokeRequired)
            {
                SetMsgCallback d = new SetMsgCallback(SetMsg);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtMsg.Text += "\r\n" + text;
                txtMsg.SelectionStart = txtMsg.Text.Length;
                txtMsg.ScrollToCaret();
                mini.txtMsg.Text += "\r\n" + text;
                mini.txtMsg.SelectionStart = mini.txtMsg.Text.Length;
                mini.txtMsg.ScrollToCaret();
            }
        }
        // Write to Freq window
        delegate void SetFreqCallback(string text);
        public void SetFreq(string text)
        {
            if (this.txtFreq.InvokeRequired)
            {
                SetFreqCallback d = new SetFreqCallback(SetFreq);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtFreq.Text = text;
                mini.txtMsg.Text += "\r\n" + text;
                mini.txtMsg.SelectionStart = mini.txtMsg.Text.Length;
                mini.txtMsg.ScrollToCaret();
            }
        }
        // Write to Tune window
        delegate void SetpaTuneCallback(string text);
        public void SetpaTune(string text)
        {
            if (this.txtTune.InvokeRequired)
            {
                SetpaTuneCallback d = new SetpaTuneCallback(SetpaTune);
                this.Invoke(d, new object[] { text });
            }
            else
            { txtTune.Text = text; mini.txtTune.Text = text; }
        }
        // Write to Load window
        delegate void SetLoadCallback(string text);
        public void SetLoad(string text)
        {
            if (this.txtLoad.InvokeRequired)
            {
                SetLoadCallback d = new SetLoadCallback(SetLoad);
                this.Invoke(d, new object[] { text });
            }
            else
            { txtLoad.Text = text; }
        }

        #endregion Delegates

        #region # Events #

        // the 87A has been selected
        private void Alpha87_Click(object sender, EventArgs e)
        {
            reg = @".*?\r\n"; //set regex mask
            alpToolStripMenuItem.Checked = true;
            aCOM2000AToolStripMenuItem.Checked = false;
            grpAmp.Text = "Alpha 87A";
            mini.grpAmp.Text = "Alpha 87A";
            cboAlphaBaud.SelectedIndex = set.AlphaBaud;
            AlphaPort.DtrEnable = true;
            AlphaPort.RtsEnable = true;
            lblAmpInt.Text = "Interval";
            lblTune.Text = "Tune"; lblLoad.Text = "Load";
            Amp = 1;
            //chkAutoDrv.Checked = set.chkAutoDrv;
            //autoDriveToolStripMenuItem1.Checked = set.chkAutoDrv;
            set.theAmp = 1; set.Save();
            txtAlphaInt.Text = "60000";
            AlphaTimer.Enabled = true;
            grpAmpBand.Visible = false;
            btnHF.Visible = true; btnSF.Visible = true;
            lblHF.Visible = true; lblSF.Visible = true;
            mini.btnHF.Visible = true; mini.btnSF.Visible = true;
            mini.lblHF.Visible = true; mini.lblSF.Visible = true;
            mini.txtTune.Visible = false; mini.lblTune.Visible = false;

            this.toolTip1.SetToolTip(this.btnTune, "Press to toggle AutoTune function On/Off.");
            this.toolTip1.SetToolTip(this.btnHV, "Press to toggle plate voltage High/Low  .");
            this.toolTip1.SetToolTip(this.btnHF, "Press to display Hard Fault log (Double-Click button label to clear).");
            this.toolTip1.SetToolTip(this.btnSF, "Press to display Soft Fault log (Double-Click button label to clear).");
            this.toolTip1.SetToolTip(this.txtTune, "Amplifier Tune setting");
            this.toolTip1.SetToolTip(this.txtLoad, "Amplifier Load setting");
            this.toolTip1.SetToolTip(this.txtAlphaInt, "Interval to poll amplifier for status (1000 = 1 sec.)");

        }
        // the Alpha enabled RadioButton has changed
        private void chkAlpha_CheckedChanged(object sender, EventArgs e)
        {
            if (cboAlpha.SelectedIndex > 0)
            {
                if (chkAlpha.Checked)
                {
                    btnPwr.Enabled = true; btnOper.Enabled = true;
                    mini.btnPwr.Enabled = true; mini.btnOper.Enabled = true;
                    btnTune.Enabled = true; btnHV.Enabled = true;
                    mini.btnTune.Enabled = true; mini.btnHV.Enabled = true;
                    btnSF.Enabled = true; btnHF.Enabled = true;
                    mini.btnSF.Enabled = true; mini.btnHF.Enabled = true;
                    txtMsg.Enabled = true; mini.txtMsg.Enabled = true;
                    txtBand.Enabled = true; txtSeg.Enabled = true; txtFreq.Enabled = true;
                    txtTune.Enabled = true; txtLoad.Enabled = true;
                    txtAlphaInt.Enabled = true; set.AlphaEnab = true; set.Save();
                    AlphaTimer.Enabled = true;
                    grpAmpBand.Enabled = true;
                    if (set.theAmp == 0) ACOM2K_Click(null, null);
                    if (set.theAmp == 1)
                    {
                        if (AlphaPort.IsOpen) 
                        {
                            AlphaPort.Write("AC\r");
                            AlphaPort.Write("STAT\r");
                            AlphaPort.Write("GPIO OFF\r");
                            AlphaPort.Write("EXT ON\r");
                            AlphaPort.Write("RM ON\r");
                            AlphaPort.Write("AUTOTUNE\r");
                        }
                    }
                    if (set.theAmp == 2)
                    { }
                }
                else
                {
                    SetPwr("Off"); SetTune("Off"); SetOper("Off"); SetHV("Off");
                    ac = "Off"; tune = "Off"; state = "Off"; mode = "Off";
                    btnPwr.Enabled = false; btnOper.Enabled = false;
                    mini.btnPwr.Enabled = false; mini.btnOper.Enabled = false;
                    btnTune.Enabled = false; btnHV.Enabled = false;
                    mini.btnTune.Enabled = false; mini.btnHV.Enabled = false;
                    btnSF.Enabled = false; btnHF.Enabled = false;
                    mini.btnSF.Enabled = false; mini.btnHF.Enabled = false;
                    txtMsg.Enabled = false; mini.txtMsg.Enabled = false;
                    txtBand.Enabled = false; txtSeg.Enabled = false; txtFreq.Enabled = false;
                    txtTune.Enabled = false; txtLoad.Enabled = false;
                    set.AlphaEnab = false; set.Save();txtAlphaInt.Enabled = false;
                    AlphaTimer.Enabled = false; 
                    grpAmpBand.Enabled = false;
                }
            }
            else
            {
                btnPwr.Enabled = false; btnOper.Enabled = false;
                mini.btnPwr.Enabled = false; mini.btnOper.Enabled = false;
                btnTune.Enabled = false; btnHV.Enabled = false;
                mini.btnTune.Enabled = false; mini.btnHV.Enabled = false;
                btnSF.Enabled = false; btnHF.Enabled = false;
                mini.btnSF.Enabled = false; mini.btnHF.Enabled = false;
                txtMsg.Enabled = false; mini.txtMsg.Enabled = false;
                txtBand.Enabled = false; txtSeg.Enabled = false;
                txtTune.Enabled = false; txtLoad.Enabled = false;
                txtAlphaInt.Enabled = false; AlphaTimer.Enabled = false;
                txtFreq.Enabled = false; set.AlphaEnab = false; set.Save();
                chkAlpha.Checked = false;
                grpAmpBand.Enabled = false;

                Notification alert = new Notification();
                Notification.notiIntvl = 7000;
                Notification.notiMsg =
                    "You must assign a port number before the\r\r" +
                    "Amplifier can be enabled.\r\r" +
                    "Please assign a valid port and try again.\r";
                alert.Show();
            }
        }
        // The baud rate selection has changed
        private void cboAlphaBaud_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboAlphaBaud.SelectedItem.ToString() == "1200")
            {
                AlphaPort.BaudRate = 1200; AlphaPort.DataBits = 8;
                AlphaPort.Parity = System.IO.Ports.Parity.None;
                AlphaPort.StopBits = System.IO.Ports.StopBits.One;
            }
            if (cboAlphaBaud.SelectedItem.ToString() == "4800")
            {
                AlphaPort.BaudRate = 4800; AlphaPort.DataBits = 8;
                AlphaPort.Parity = System.IO.Ports.Parity.None;
                AlphaPort.StopBits = System.IO.Ports.StopBits.One;
            }
            if (cboAlphaBaud.SelectedItem.ToString() == "9600")
            {
                AlphaPort.BaudRate = 9600; AlphaPort.DataBits = 8;
                AlphaPort.Parity = System.IO.Ports.Parity.None;
                AlphaPort.StopBits = System.IO.Ports.StopBits.One;
            }
            set.AlphaBaud = cboAlphaBaud.SelectedIndex; set.Save();
        }
        // the serial port selection has changed
        private void cboAlpha_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AlphaPort.IsOpen) AlphaPort.Close();
            if (cboAlpha.SelectedIndex > 0)
            {
                AlphaPort.PortName = cboAlpha.SelectedItem.ToString();
                try
                {
                    AlphaPort.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "The Amplifier serial port " + AlphaPort.PortName +
                        " cannot be opened!\n", "Port Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboAlpha.SelectedIndex = 0;
                    chkAlpha.Checked = false;
                    set.AlphaPort = 0;
                    set.Save();
                    return;
                }
            }
            else
            {
                cboAlpha.SelectedIndex = 0;
                chkAlpha.Checked = false;
            }
            set.AlphaPort = cboAlpha.SelectedIndex;
            set.Save();
        }
        public string sf = "";          // Soft fault message
        public string hf = "";          // Hard fault message
        public string ac = "Off";       // Amp power (On/Off)
        public string mode = "Off";     // HV mode
        public string state = "Off";    // Amp State (On/Off)
        public string tune = "Off";     // State of autotune 
        string sAlpha = "";             // Port buffer message
        public int Amp = 0;
        string reg = "";
        // a Message(s) from Amplifier HAS BEEN received
        private void AlphaPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (chkAlpha.Checked)       // port must be enabled to receive data
            {
                try
                {
                    string sCmd = "";
                    SerialPort port = (SerialPort)sender;
                    byte[] data = new byte[port.BytesToRead];
                    port.Read(data, 0, data.Length);
                    for (int i = 0; i < data.Length; i++)
                    {   // if 8x or 90 msg received from ACOM chg to '*'
                        // see DataAcom() routine for processing
                        if (data[i] > 127 && data[i] < 255) data[i] = 0x2A;
                    }
                    sAlpha += AE.GetString(data, 0, data.Length);
                    Regex rex = new Regex(reg);
                    for (Match m = rex.Match(sAlpha); m.Success; m = m.NextMatch())
                    {   //loop thru the buffer and find matches
                        sCmd = m.Value; //.Substring(0, m.Value.Length);
                        sAlpha = sAlpha.Replace(m.Value, ""); //remove match from buffer
                        // route the buffer contents to the right amp routine.
                        if (Amp == 0)
                        {
                            DataAcom(sCmd);
                        }
                        else
                        {
                            sCmd = sCmd.Substring(0, sCmd.Length - 2);
                            //if (sCmd.Contains("FACTORY MODE = ON"))
                            //    AlphaPort.Write("EXT OFF\r");

                            if (sCmd.Contains("AMPLIFIER IS OFF"))
                            {
                                SetPwr("Off"); SetTune("Off"); SetOper("Off"); SetHV("Off");
                                ac = "Off"; tune = "Off"; state = "Off"; mode = "Off";
                            }
                            if (sCmd.Contains("AMPLIFIER IS ON"))
                            {
                                SetPwr("On"); ac = "On"; 
                            }
                            if (sCmd.Contains("AUTOTUNE ENABLED"))
                            {
                                SetTune("Auto"); tune = "On"; 
                            }
                            if (sCmd.Contains("AUTOTUNE DISABLED"))
                            {
                                SetTune("Man"); tune = "Off"; 
                            }
                            if (sCmd.Contains("STATE = WARMUP"))
                            {
                                SetOper("Wait"); state = "Wait"; 
                            }
                            if (sCmd.Contains("STATE = STANDBY"))
                            {
                                SetOper("Stby"); state = "Stby"; 
                            }
                            if (sCmd.Contains("STATE = OPERATE"))
                            {
                                SetOper("Oper"); state = "Oper"; 
                            }
                            if (sCmd.Contains("MODE = HIGH"))
                            {
                                SetHV("High"); mode = "High"; 
                            }
                            if (sCmd.Contains("MODE = LOW") && state !="Off")
                            {
                                SetHV("Low"); mode = "Low"; 
                            }

                            string sb = ""; string sbx = "";
                            if (sCmd.Contains("BAND"))
                            {
                                sb = sCmd.Substring(7, 1);
                                SetSeg(sCmd.Substring(22, 1));
                            }
                            switch (sb)
                            {
                                case "1": SetBand("160"); break;
                                case "2": SetBand("80"); break;
                                case "3": SetBand("40"); break;
                                case "4": SetBand("30"); break;
                                case "5": SetBand("20"); break;
                                case "6": SetBand("17"); break;
                                case "7": SetBand("15"); break;
                                case "8": SetBand("12"); break;
                                case "9": SetBand("10"); break;
                            }
                            string regex = @"FREQUENCY\s=\s(\d+)(\d\d\d)";
                            string mask = "$1.$2";

                            if (sCmd.Contains("FREQUENCY"))
                            {
                                sb = Regex.Replace(sCmd, regex, mask);
                                SetFreq(sb);
                            }
                            if (sCmd.Contains("TUNE"))
                            {
                                sb = Regex.Match(sCmd, @"TUNE\s=\s\d+").ToString();
                                sbx = Regex.Match(sb, @"\d+").ToString();
                                SetpaTune(sbx);
                            }
                            if (sCmd.Contains("LOAD"))
                            {
                                sb = Regex.Match(sCmd, @"LOAD\s=\s\d+").ToString();
                                sbx = Regex.Match(sb, @"\d+").ToString();
                                SetLoad(sbx);
                            }
                            if (sCmd.Contains("SFAULT"))
                            {
                                AlphaPort.Write("STAT\r");
                                SetMsg(sCmd);
                                sf = sCmd; btnSF.BackColor = Color.Yellow;
                                mini.btnSF.BackColor = Color.Yellow;
                            }
                            if (sCmd.Contains("HFAULT"))
                            {
                                AlphaPort.Write("STAT\r");
                                SetMsg(sCmd);
                                hf = sCmd; btnHF.BackColor = Color.Red;
                                mini.btnHF.BackColor = Color.Red;
                            }
                        }//else
                    }//for
                }//try

                catch (Exception ex)
                {
                    bool bReturnLog = false;
                    bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                    if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                        "Unable to write to log");
                }

            }//if (chkAlpha.Checked)
        }//AlphaPort_DataReceived
        //the power button was pressed
        public void btnPwr_Click(object sender, EventArgs e)
        {
            if (Amp == 0) //ACOM
            {
                if (ac == "Off") { AlphaPort.DtrEnable = true; AlphaPort.RtsEnable = true; }
                if (ac == "On") AlphaPort.Write("Aq0\0");
            }
            if (Amp == 1) //Alpha
            {
                if (ac == "Off")
                {
                    AlphaPort.Write("AC ON\r");
                }
                if (ac == "On") AlphaPort.Write("AC OFF\r");
            }
        }
        // The Operate / Stand By button was pressed
        public void btnOper_Click(object sender, EventArgs e)
        {
            if (Amp == 0)
            {
                if (ac == "On" && state != "Oper") AlphaPort.Write("AqO\0");
                else AlphaPort.Write("AqS\0");
            }
            if (Amp == 1)
            {
                if (ac == "On" && state != "Oper")
                {
                    AlphaPort.Write("OPER OFF\r");
                    AlphaPort.Write("OPER ON\r");
                }
                else AlphaPort.Write("OPER OFF\r");
            }
        }
        // The Auto tune button was pressed.
        public void btnTune_Click(object sender, EventArgs e)
        {
            if (Amp == 0)// && bAmp)
            {
                DoAuto();
            }
            if (Amp == 1)
            {
                if (ac == "On" && tune == "Off") AlphaPort.Write("AUTOTUNE ON\r");
                else AlphaPort.Write("AUTOTUNE OFF\r");
            }
        }
        // The High Voltage button was pressed
        public void btnHV_Click(object sender, EventArgs e)
        {
            if (Amp == 1)
            {
                if (ac == "On" && mode == "Low") AlphaPort.Write("MODE HIGH\r");
                else AlphaPort.Write("MODE LOW\r");
            }
        }
        /*** The Alpha timer has elapsed ***/
        void AlphaTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Amp == 0 && bAmp)
            {
                byte[] b = { 0xFF, 0x71, 0x4C, 0x00 };
                if (AlphaPort.IsOpen) AlphaPort.Write(b, 0, 4);
            }
            if (Amp == 1)
            {
                //try
                //{
                //    if (chkAlpha.Enabled)
                //    {
                //        AlphaPort.Write("STAT\r"); // Request ststus from amp
                //    }
                //    else
                //    { AlphaTimer.Enabled = false; }
                //}
                //catch { }
            }
        }
        // The Alpha Timer interval has changed
        private void txtAlphaInt_TextChanged(object sender, EventArgs e)
        {
            try
            {
                AlphaTimer.Interval = Convert.ToDouble(txtAlphaInt.Text);
                set.AlphaInt = txtAlphaInt.Text;
                set.Save();
            }
            catch { }
        }
        // The Soft Fault button has been pressed
        public void btnSF_Click(object sender, EventArgs e)
        {
            if (Amp == 1)
            {
                AlphaPort.Write("SF\r");
            }
        }
        // The Hard Fault button has been pressed
        public void btnHF_Click(object sender, EventArgs e)
        {
            if (Amp == 1)
            {
                AlphaPort.Write("HF\r");
            }
        }
        // The SF button label was double clicked
        public void lblSF_DoubleClick(object sender, EventArgs e)
        {
            if (Amp == 1)
            {
                //SetMsg(""); 
                btnSF.BackColor = Color.Empty;
                mini.btnSF.BackColor = Color.Empty;
            }
        }
        // The HF button label was double clicked
        public void lblHF_DoubleClick(object sender, EventArgs e)
        {
            if (Amp == 1)
            {
                //SetMsg(""); 
                btnHF.BackColor = Color.Empty;
                mini.btnHF.BackColor = Color.Empty;
            }
        }

        #endregion Events

        #endregion Alpha 87A

        #region Alpha 9500

        #region # Vars #

        public static Queue<string> A95Q = new Queue<string>();   // message queue
        bool A95led = false;                        // CAT activity indicator
        //bool A95on = false;                         // DDU thinks the amp is on
        string A95ant = "";                         // current antenna being used
        string A95band = "";                        // current vfo band
        string A95mem = "";                         // current memory being used
        string A95mode = "";                        // current mode being used
        string A95auto = "";                        // current state of the mem auto mode
        string wTime = "";                          // warmup time remaining
        private Thread loopThd;
        bool _go;
        SerialPort A95port = new SerialPort();
        public string pBuf = "";
        string bandNum = "";                        // numeric equiv of band (0-9)

        #endregion Vars

        #region # Tables #

        // error pointer for $APA06 routine
        int[] errPtr = new int[]{
            33, 36, 39, 42, 45, 48, 51, 54, 57, 60,
            63, 66, 69, 72, 75, 78, 81, 84, 87, 90};
        // Error messages
        string[] A95err = new string[] {
            "",                                        //00
            "Gain Fault",                              //01
            "Tap Not Found",                           //02
            "Soft IP Trip",                            //03
            "Hard IP Trip",                            //04
            "Vp Under Voltage",                        //05
            "Output Relay Closure Fail",               //06
            "Output Relay May Be Stuck",               //07
            "Bandswitch Set Failure",                  //08
            "Tune Cap Zero Failure",                   //09
            "Load Cap Zero Failure",                   //10
            "Over Temperature",                        //11
            "SWR Too High",                            //12
            "Clear Over-Temperature",                  //13
            "Plate Voltage Too High",                  //14
            "Grid Current Too High",                   //15
            "Auto-Tune Failure",                       //16
            "Plate Current Too High With Amp Unkeyed", //17
            "Input Power Too High"                     //18
            };

        // Segment Tables
        int[,] Seg160 = { { 1800, 1840 }, { 1841, 1880 }, { 1881, 1920 }, { 1921, 1960 }, { 1961, 2000 } };

        int[,] Seg80 = { { 3500, 3600 }, { 3601, 3700 }, { 3701, 3800 }, { 3801, 3900 }, { 3901, 4000 } };

        int[,] Seg40 = { { 7000, 7100 }, { 7101, 7200 }, { 7201, 7300 }, { 7301, 7400 }, { 7401, 7500 } };

        int[,] Seg30 = { { 10000, 10100 }, { 10101, 10200 }, { 10201, 10300 }, { 10301, 10400 }, { 10401, 10500 } };

        int[,] Seg20 = { { 14000, 14100 }, { 14101, 14200 }, { 14201, 14300 }, { 14301, 14400 }, { 14401, 14500 } };

        int[,] Seg17 = { { 18000, 18100 }, { 18101, 18200 }, { 18201, 18300 }, { 18301, 18400 }, { 18401, 18500 } };

        int[,] Seg15 = { { 21000, 21100 }, { 21101, 21200 }, { 21201, 21300 }, { 21301, 21400 }, { 21401, 21500 } };

        int[,] Seg12 = { { 24500, 24600 }, { 24601, 24700 }, { 24701, 24800 }, { 24801, 24900 }, { 24901, 25000 } };

        int[,] Seg10 = { { 28000, 28400 }, { 28401, 28800 }, { 28801, 29200 }, { 29201, 29600 }, { 29601, 30000 } };

        #endregion # Tables #

        #region # Delegates #

        // Write to the memory window
        delegate void SetA95memCallback(string text);
        public void SetA95mem(string text)
        {
            if (this.txtA95mem.InvokeRequired)
            {
                SetA95memCallback d = new SetA95memCallback(SetA95mem);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "1") txtA95mem.Text = "Def";
                else if (text == "2") txtA95mem.Text = "User1";
                else if (text == "3") txtA95mem.Text = "User2";
                else txtA95mem.Text = "";
            }
        }
        // Write to Plate Current (Ip) window
        delegate void SetA95ipCallback(string text);
        public void SetA95ip(string text)
        {
            if (this.txtA95ip.InvokeRequired)
            {
                SetA95ipCallback d = new SetA95ipCallback(SetA95ip);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtA95ip.Text = text;       //.TrimStart('0');
                a95.txtA95ip.Text = text;   //.TrimStart('0');
            }
        }
        // Write to Grid Current (Ig) window
        delegate void SetA95igCallback(string text);
        public void SetA95ig(string text)
        {
            if (this.txtA95ig.InvokeRequired)
            {
                SetA95igCallback d = new SetA95igCallback(SetA95ig);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtA95ig.Text = text;       //.TrimStart('0');
                a95.txtA95ig.Text = text;   //.TrimStart('0');
            }
        }
        // change LED color
        delegate void SetA95ledCallback(bool text);
        public void SetA95led(bool text)
        {
            //     if (this.txtLed.InvokeRequired)
            if (this.rbA95led.InvokeRequired)
            {
                SetA95ledCallback d = new SetA95ledCallback(SetA95led);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text) rbA95led.Checked = true; else rbA95led.Checked = false;
                if (!text) rbA95led.Checked = false;
            }
        }
        // Write to Temp window
        delegate void SetA95tempCallback(string text);
        public void SetA95Temp(string text)
        {
            if (this.txtA95temp.InvokeRequired)
            {
                SetA95tempCallback d = new SetA95tempCallback(SetA95Temp);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                try
                {
                    txtA95temp.Text = text.TrimStart('0');
                    a95.txtA95temp.Text = text.TrimStart('0');
                    if (text == "")
                    {
                        txtA95temp.Text = text;
                        a95.txtA95temp.Text = text;
                        txtA95temp.BackColor = Color.White;
                        a95.txtA95temp.BackColor = Color.White;
                        return;
                    }
                    int temp = Convert.ToInt32(text.Substring(0, text.Length - 2));

                    if (temp > 0 && temp < 45)
                    {
                        txtA95temp.BackColor = Color.LightGreen;
                        a95.txtA95temp.BackColor = Color.LightGreen;
                    }
                    else if (temp > 45)
                    {
                        txtA95temp.BackColor = Color.Red;
                        a95.txtA95temp.BackColor = Color.Red;
                    }
                    else
                    {
                        txtA95temp.BackColor = Color.White;
                        a95.txtA95temp.BackColor = Color.White;
                    }

                }
                catch { }
            }
        }
        // Write to Antenna window
        delegate void SetA95antCallback(string text);
        public void SetA95ant(string text)
        {
            if (this.txtA95ant.InvokeRequired)
            {
                SetA95antCallback d = new SetA95antCallback(SetA95ant);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "1")
                { txtA95ant.Text = "1"; a95.txtA95ant.Text = "1"; }
                else if (text == "2")
                { txtA95ant.Text = "2"; a95.txtA95ant.Text = "2"; }
                else if (text == "4")
                { txtA95ant.Text = "3"; a95.txtA95ant.Text = "3"; }
                else if (text == "8")
                { txtA95ant.Text = "4"; a95.txtA95ant.Text = "4"; }
            }
        }
        // Write to Mains On button
        delegate void SetA95onCallback(string text);
        public void SetA95on(string text)
        {
            if (this.btnA95on.InvokeRequired)
            {
                SetA95onCallback d = new SetA95onCallback(SetA95on);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "True")
                {
                    btnA95on.BackColor = Color.LightGreen;
                    a95.btnA95on.BackColor = Color.LightGreen;
                }
                else
                {
                    btnA95on.BackColor = Color.Empty;
                    a95.btnA95on.BackColor = Color.Empty;
                }
            }
        }
        // Write to Auto button
        delegate void SetA95autoCallback(string text);
        public void SetA95auto(string text)
        {
            if (this.btnA95auto.InvokeRequired)
            {
                SetA95autoCallback d = new SetA95autoCallback(SetA95auto);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "0" || text == "Off") btnA95auto.BackColor = Color.Empty;
                else
                    btnA95auto.BackColor = Color.LightGreen;
            }
        }
        //Write to Mains aux button
        delegate void SetA95auxCallback(string text);
        public void SetA95aux(string text)
        {
            if (this.btnA95aux.InvokeRequired)
            {
                SetA95auxCallback d = new SetA95auxCallback(SetA95aux);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "Off")
                {
                    btnA95aux.BackColor = Color.Empty;
                    a95.btnA95aux.BackColor = Color.Empty;
                }
            }
        }
        // Write to Oper/Stby (Mode) button
        delegate void SetA95modeCallback(string text);
        public void SetA95mode(string text)
        {
            if (this.btnA95mode.InvokeRequired)
            {
                SetA95modeCallback d = new SetA95modeCallback(SetA95mode);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                switch (text)
                {
                    case "0":   // Off
                        btnA95on.BackColor = Color.Empty;
                        btnA95aux.BackColor = Color.Empty;
                        btnA95mode.BackColor = Color.Empty;
                        btnA95mode.Text = "OFF";
                        btnA95fault.BackColor = Color.Empty;
                        btnA95auto.BackColor = Color.Empty;
                        a95.btnA95on.BackColor = Color.Empty;
                        a95.btnA95aux.BackColor = Color.Empty;
                        a95.btnA95mode.BackColor = Color.Empty;
                        a95.btnA95mode.Text = "OFF";
                        break;
                    case "1":   // Aux is On
                        btnA95aux.BackColor = Color.LightGreen;
                        a95.btnA95aux.BackColor = Color.LightGreen;
                        break;
                    case "2":   // amp is warming
                        btnA95on.BackColor = Color.LightGreen;
                        btnA95aux.BackColor = Color.LightGreen;
                        btnA95mode.BackColor = Color.LightPink;
                        btnA95mode.Text = wTime;
                        a95.btnA95on.BackColor = Color.LightGreen;
                        a95.btnA95aux.BackColor = Color.LightGreen;
                        a95.btnA95mode.BackColor = Color.LightPink;
                        a95.btnA95mode.Text = wTime;
                        break;
                    case "3":   // amp is warmed and in standby
                        btnA95on.BackColor = Color.LightGreen;
                        btnA95aux.BackColor = Color.LightGreen;
                        btnA95mode.BackColor = Color.Yellow;
                        btnA95mode.Text = "STBY";
                        a95.btnA95on.BackColor = Color.LightGreen;
                        a95.btnA95aux.BackColor = Color.LightGreen;
                        a95.btnA95mode.BackColor = Color.Yellow;
                        a95.btnA95mode.Text = "STBY";
                        break;
                    case "4":   // amp is in operate mode
                        btnA95on.BackColor = Color.LightGreen;
                        btnA95aux.BackColor = Color.LightGreen;
                        btnA95mode.BackColor = Color.LightGreen;
                        btnA95mode.Text = "OPR";
                        a95.btnA95on.BackColor = Color.LightGreen;
                        a95.btnA95aux.BackColor = Color.LightGreen;
                        a95.btnA95mode.BackColor = Color.LightGreen;
                        a95.btnA95mode.Text = "OPR";
                        break;
                    case "5":
                        break;
                    case "6":
                        break;
                }
            }
        }
        // Write to Msg window
        delegate void SetA95msgCallback(string text);
        public void SetA95msg(string text)
        {
            if (this.txtA95msg.InvokeRequired)
            {
                SetA95msgCallback d = new SetA95msgCallback(SetA95msg);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtA95msg.Text += "\r\n" + text;
                txtA95msg.SelectionStart = txtA95msg.Text.Length;
                txtA95msg.ScrollToCaret();
            }
        }
        // Clear Msg window
        delegate void A95msgClrCallback(string text);
        public void A95msgClr(string text)
        {
            if (this.txtA95msg.InvokeRequired)
            {
                A95msgClrCallback d = new A95msgClrCallback(A95msgClr);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtA95msg.Clear();
            }
        }
        // Write to Display button
        delegate void SetA95faultCallback(string text);
        public void SetA95fault(string text)
        {
            if (this.btnA95fault.InvokeRequired)
            {
                SetA95faultCallback d = new SetA95faultCallback(SetA95fault);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "True") btnA95fault.BackColor = Color.Pink;
                else btnA95fault.BackColor = Color.Empty;
            }
        }

        #endregion Delegates

        #region # Events #

        //void A95Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        //{

        //    //A95Q.Enqueue("00,02");
        //    ////sndMsg();
        //    //A95Q.Enqueue("00,03");
        //    ////sndMsg();
        //    //A95Q.Enqueue("00,05");
        //    //sendMsg();
        //}
        // the On button was pressed
        public void btnA95on_Click(object sender, EventArgs e)
        {
            A95Q.Enqueue("01,41");
        }
        // the Aux button was pressed
        public void btnA95aux_Click(object sender, EventArgs e)
        {
            A95Q.Enqueue("01,42");
        }
        // the Mode button was pressed
        public void btnA95mode_Click(object sender, EventArgs e)
        {
            if (Convert.ToInt32(A95mode) > 3)
                A95Q.Enqueue("01,40"); // if in oper select stby
            else if (Convert.ToInt32(A95mode) == 3)
                A95Q.Enqueue("01,39"); // if in stby select oper
        }
        // the Antenna button was pressed
        public void btnA95ant_Click(object sender, EventArgs e)
        {
            switch (A95ant) // rotate thru the 4 antennas
            {
                case "1": A95Q.Enqueue("01,26"); // select ant 2
                    break;
                case "2": A95Q.Enqueue("01,27"); // select ant 3
                    break;
                case "4": A95Q.Enqueue("01,28"); // select ant 4
                    break;
                case "8": A95Q.Enqueue("01,25"); // select ant 1
                    break;
            }
        }
        // the Memory button was pressed
        private void btnA95mem_Click(object sender, EventArgs e)
        {
            switch (A95mem) // rotate thru the 4 memories
            {
                case "1": A95Q.Enqueue("01,18"); // select mem 2
                    break;
                case "2": A95Q.Enqueue("01,19"); // select mem 3
                    break;
                case "3": A95Q.Enqueue("01,17"); // select mem 1
                    break;
                //case "8": A95Q.Enqueue("01,17"); // select mem 1
                //    break;
            }
        }
        // The Auto Memory burron was pressed.
        private void btnA95auto_Click(object sender, EventArgs e)
        {
            A95Q.Enqueue("01,20");
        }
        // the Fault button was pressed
        private void btnA95fault_Click(object sender, EventArgs e)
        {
            A95Q.Enqueue("00,06");
        }
        // the port has changed
        private void cboA95port_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (A95port.IsOpen) A95port.Close();
            if (cboA95port.SelectedIndex > 0)
            {
                A95port.PortName = cboA95port.SelectedItem.ToString();
                try
                {
                    A95port.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true },
                        "The Amplifier serial port " + A95port.PortName +
                       " cannot be opened!\n", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    chkA95enab.Checked = false;
                    set.A95enab = false;
                    cboA95port.SelectedIndex = 0;
                    set.A95port = 0;
                    set.Save();
                    return;
                }
            }
            else
            {
                cboA95port.SelectedIndex = 0;
                chkA95enab.Checked = false;
            }
            set.A95port = cboA95port.SelectedIndex;
            set.Save();
        }
        // the enabled check box has changed
        private void chkA95enab_CheckedChanged(object sender, EventArgs e)
        {
            if (chkA95enab.Checked)
            {
                if (cboA95port.SelectedIndex > 0)
                {
                    set.A95enab = true;
                    a95.grpA95.Enabled = true;
                    InitA95();
                }
                else
                {
                    MessageBox.Show(new Form() { TopMost = true },
                        "No port has been selected for the Alpha 9500 amplifier.\n\n" +
                    "Please select a valid port number and try again.", "Port Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    chkA95enab.Checked = false; set.A95enab = false;
                    cboA95port.SelectedIndex = 0;
                    set.A95port = cboA95port.SelectedIndex;
                }
            }
            else
            {
                _go = false;
                loopThd.Abort();
                if (A95port.IsOpen) A95port.Close();
                //A95on = false;
                SetA95on("False");
                SetA95mode("Off");
                SetA95auto("Off");
                SetA95aux("Off");
                SetA95Temp("");
                SetA95ant("");
                SetA95mem("");
                SetA95ip("");
                SetA95ig("");
                A95msgClr("");
                SetA95led(false);
                set.A95enab = false;
                a95.grpA95.Enabled = false;
                //txtAvg.Enabled = false; 
                //txtSWR.Enabled = false;
            }
            set.Save();
        }
        // the message window was double-clicked
        private void txtA95msg_DoubleClick(object sender, EventArgs e)
        {
            txtA95msg.Clear();
            SetA95fault("False");
        }
        // the memory window was double-clicked
        private void txtA95mem_DoubleClick(object sender, EventArgs e)
        {
            if (txtA95mem.Text == "User1" || txtA95mem.Text == "User2")
            {
                A95Q.Enqueue("01,15");
                SetA95msg("Memory Saved");
            }
        }

        #endregion Events

        #region Methods

        // Find the proper frequency segment from SegTables
        int GetA95Seg(int afreq)
        {
            switch (A95band)
            {
                case "160":
                    bandNum = "1";
                    for (int i = 0; i < 5; i++)
                    { if (afreq >= Seg160[i, 0] && afreq <= Seg160[i, 1]) return i; }
                    return -1;
                case "080":
                    bandNum = "2";
                    for (int i = 0; i < 5; i++)
                    { if (afreq >= Seg80[i, 0] && afreq <= Seg80[i, 1]) return i; }
                    return -1;
                case "040":
                    bandNum = "3";
                    for (int i = 0; i < 5; i++)
                    { if (afreq >= Seg40[i, 0] && afreq <= Seg40[i, 1]) return i; }
                    return -1;
                case "030":
                    bandNum = "4";
                    for (int i = 0; i < 5; i++)
                    { if (afreq >= Seg30[i, 0] && afreq <= Seg30[i, 1]) return i; }
                    return -1;
                case "020":
                    bandNum = "5";
                    for (int i = 0; i < 5; i++)
                    { if (afreq >= Seg20[i, 0] && afreq <= Seg20[i, 1]) return i; }
                    return -1;
                case "017":
                    bandNum = "6";
                    for (int i = 0; i < 5; i++)
                    { if (afreq >= Seg17[i, 0] && afreq <= Seg17[i, 1]) return i; }
                    return -1;
                case "015":
                    bandNum = "7";
                    for (int i = 0; i < 5; i++)
                    { if (afreq >= Seg15[i, 0] && afreq <= Seg15[i, 1]) return i; }
                    return -1;
                case "012":
                    bandNum = "8";
                    for (int i = 0; i < 5; i++)
                    { if (afreq >= Seg12[i, 0] && afreq <= Seg12[i, 1]) return i; }
                    return -1;
                case "010":
                    bandNum = "9";
                    for (int i = 0; i < 5; i++)
                    { if (afreq >= Seg10[i, 0] && afreq <= Seg10[i, 1]) return i; }
                    return -1;
            }
            return -1;
        }
        //  Initialize A95 settings and controls
        private void InitA95()
        {
            try
            {
                A95port.BaudRate = 115200;
                A95port.Parity = 0;
                A95port.DataBits = 8;
                A95port.StopBits = (System.IO.Ports.StopBits)Enum.Parse(typeof(StopBits), "One");
                A95port.Handshake = (System.IO.Ports.Handshake)Enum.Parse(typeof(Handshake), "None");
                A95port.ReadTimeout = 500;
                A95port.WriteTimeout = 500;
                //A95Timer.Enabled = false;
                A95msgClr("");
                A95Q.Clear();
                chkSWR.Checked = false;
                // see if digital watt meter is being used.
                if (chkA95enab.Checked && !chkLPenab.Checked &&
                    !chkWNEnab.Checked && !chkPM.Checked)
                {
                    txtFwd.Visible = true; lblFwd.Visible = true;
                    txtFwd.Enabled = true;
                    txtAvg.Enabled = true; txtSWR.Enabled = true;
                    txtFwd.Text =  txtAvg.Text = txtSWR.Text = "0.0";
                    lblAvg.Text = "Fwd";
                    wattmtr = true;
                }
                else wattmtr = false;

                // test to see if amp is running
                if (!A95port.IsOpen) A95port.Open();
                A95Q.Enqueue("07,00");  // turn talk mode off
                _go = true;
                loopThd = new Thread(Loop);
                loopThd.Start();
            }
            catch (Exception e)
            { MessageBox.Show(e.Message); }
        }
        // thread for A95 comm service loop
        public void Loop()
        {
            int Ticks = 0;
            while (_go)
            {
                try
                {
                    Thread.Sleep(5);
                    if (Ticks < 200)
                        Ticks += 1;
                    else
                        Ticks = 0;
                    A95AmpData();
                    if (Ticks % 5 == 0)
                        sendMsg();
                    if (Ticks % 100 == 0)
                    {
                        A95Q.Enqueue("00,02");
                        A95Q.Enqueue("00,03");
                        A95Q.Enqueue("00,05");
                        A95led = !A95led;
                    }
                    //if (Ticks % 199 == 0)
                    //{
                    //}
                }
                catch (TimeoutException) { }
                SetA95led(A95led);
            }
        }
        // Check message queue
        public void sendMsg()
        {
            if (A95Q.Count >= 1)
            {
                sendCmd(A95Q.Dequeue());
            }
        }
        public void sendCmd(string msg)
        {
            if (A95port.IsOpen)
            {
                if (waitForHash(20))
                    A95port.Write(msg + "\r");
                else
                    Console.WriteLine("Failed to hear echo from amp for: " + msg);
            }
        }
        public bool waitForHash(int nTimes)
        {
            string ch = "";
            int loops = 0;
            int tries = 0;
            while (ch != "#" && (tries < nTimes))
            {
                //A95port.DiscardInBuffer();
                A95port.Write("#");
                ch = "";
                loops = 0;
                Thread.Sleep(10);

                while (ch != "#" && A95port.IsOpen && loops < 10000)
                {
                    ch = A95port.ReadExisting();
                    loops += 1;
                }

                if (ch != "#")
                    tries += 1;
                Thread.Sleep(1);
            }
            if (ch == "#")
                return true;
            else
                return false;
        }
        // see if the amp has sent any data
        public void A95AmpData()
        {
            try
            {
                string sCmd = "";
                byte[] data = new byte[A95port.BytesToRead];
                if (data.Length == 0)
                    return;
                A95port.Read(data, 0, data.Length);
                pBuf += AE.GetString(data, 0, data.Length);
                Regex rex = new Regex(@"(\$APA.*?\r\n)");	//accept any string ending in CRLF	
                for (Match m = rex.Match(pBuf); m.Success; m = m.NextMatch())
                {   //loop thru the buffer and find matches
                    sCmd = m.Value.Substring(0, m.Value.Length);
                    pBuf = pBuf.Replace(m.Value, ""); //remove the match from the buffer
                    string cmd = sCmd.Substring(5, 1);
                    string param = sCmd.Substring(7, sCmd.Length - 7);
                    string regx = "";
                    switch (cmd)
                    {
                        case "2":
                            regx = @"(?<fwd>\d+),(?<ref>\d+),(?<pin>\d+),(?<vp>\d+)," +
                                   @"(?<ip>\d+),(?<gain>\d+),(?<vg>\d+),(?<ig>\d+)," +
                                   @"(?<bnd>\d+),(?<state>\d+),(?<fault>\d+),(?<key>\d+),(?<pep>\d+)";
                            Match APA02 = Regex.Match(param, regx);
                            if (wattmtr)
                            {
                                SetFwd(APA02.Groups["pep"].Value.Substring(0, 4));
                                SetAvg(APA02.Groups["fwd"].Value.Substring(0,4));
                                string refSwr = APA02.Groups["ref"].ToString();
                                SetSwr((refSwr.Substring(0, 2) + "." +
                                    refSwr.Substring(2, 1)).TrimStart('0'));
                            }
                            // Plate current
                            SetA95ip(APA02.Groups["ip"].Value);

                            // Grid current
                            SetA95ig(APA02.Groups["ig"].Value);
                            break;
                        case "3":
                            // PA Temp
                            SetA95Temp(param.Substring(32, 5));
                            break;
                        case "5":
                            // memory status
                            A95mem = param.Substring(3, 1);
                            SetA95mem(A95mem);
                            //antenna selection
                            A95ant = param.Substring(4, 1);
                            SetA95ant(A95ant);
                            // mode (oper/stby)
                            A95mode = param.Substring(10, 1);
                            SetA95mode(A95mode);

                            // Warmup time remaining
                            wTime = int.Parse(param.Substring(12, 2),
                                NumberStyles.HexNumber).ToString().TrimStart('0');

                            // New Error status
                            //param = "11,18,1B,34,00,2A,01,10,0";
                            string errDlg = "";
                            int flt = int.Parse(param.Substring(21, 2), NumberStyles.HexNumber);
                            if (flt > 0)            // new error happened
                            {
                                errDlg = A95err[flt];  //get msg from table
                                SetA95msg("Fault: " + errDlg);
                                SetA95fault("True");
                            }
                            // Auto Tune status
                            A95auto = param.Substring(24, 1);
                            SetA95auto(A95auto);
                            break;
                        case "6":   // Error log dump
                            //param = "00,00,00,00,00,00,00,00,00,00,6F," +
                            //    "01,02,03,04,05,06,07,08,09,0A,0B,0C,0D,0E,0F,10,11,12*4203";
                            errDlg = "";
                            int err = 0;
                            int ptr = int.Parse(param.Substring(30, 2), NumberStyles.HexNumber);
                            int ptr1 = ptr - 110;
                            int ptr2 = errPtr[ptr1 - 1];
                            SetA95msg("Error Log Dump:");
                            for (int i = ptr2; i < 87; i += 3)
                            {
                                err = int.Parse(param.Substring(i, 2), NumberStyles.HexNumber);
                                if (err > 0 && err < 19)
                                {
                                    errDlg = A95err[err]; //get msg from table
                                    SetA95msg("Error: " + errDlg);
                                }
                            }
                            if (ptr1 > 0)
                            {
                                for (int i = 33; i < ((ptr1 * 3) + 30); i += 3)
                                {
                                    err = int.Parse(param.Substring(i, 2), NumberStyles.HexNumber);
                                    errDlg = A95err[err]; //get msg from table
                                    if (err > 0 && err < 19)
                                        SetA95msg("Error: " + errDlg);
                                }
                            }
                            break;
                    }
                }
            }
            catch //(Exception ex)
            {
                //bool bReturnLog = false;
                //bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                //if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                //    "Unable to write to log");
            }

        }

        #endregion Methods

        #endregion Alpha 9500

        #region Auto Drive

        #region # Declarations #

        #endregion # Declarations #

        #region # Delegates #

        // Enable/Disable Auto Drive Enable checkbox
        delegate void EnabADCallback(bool b);
        public void EnabAD(bool b)
        {
            if (this.chkAutoDrv.InvokeRequired)
            {
                EnabADCallback d = new EnabADCallback(EnabAD);
                this.Invoke(d, new object[] { b });
            }
            else chkAutoDrv.Checked = b;
        }

        #endregion Delegates

        #region # Events #

        // the Auto Drive checkbox has changed.
        private void chkAutoDrv_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAutoDrv.Checked)
            {
                set.chkAutoDrv = true;
                autoDriveToolStripMenuItem1.Checked = true;
            }
            else
            {
                set.chkAutoDrv = false;
                autoDriveToolStripMenuItem1.Checked = false;
            }
            set.Save();
        }
        // the auto drive expert check box has changed
        private void chkAutoExpert_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAutoExpert.Checked)
            {
                enableExpertModeToolStripMenuItem.Checked = true;
                set.chkAutoExpert = true;
                txtAM.Enabled = true;
                txtSSB.Enabled = true;
                txtDigi.Enabled = true;
            }
            else
            {
                enableExpertModeToolStripMenuItem.Checked = false;
                set.chkAutoExpert = false;
                txtAM.Enabled = false;
                txtSSB.Enabled = false;
                txtDigi.Enabled = false;
            }
            set.Save();
            modeFactor = 1;
            LastMode = "";
            lastBand = "";
            DoModeChg();
        }
        // The Amp 1 rb was pressed
        private void rbAmp1_CheckedChanged(object sender, EventArgs e)
        {
            if (rbAmp1.Checked)
            {
                if (stsOper)
                    WriteToPort("ZZOF100;", iSleep);
                lastBand = "";
                DoBandChg(band);
                set.rbAmp = 1;
                set.Save();
            }
        }
        // The Amp 2 rb was pressed
        private void rbAmp2_CheckedChanged(object sender, EventArgs e)
        {
            if (rbAmp2.Checked)
            {
                if (stsOper)
                    if (chk1KeyLine.Checked)
                        WriteToPort("ZZOF100;", 50);  // turn PTT (TX1) on
                    else
                        WriteToPort("ZZOF010;", 50);  // turn PTT (TX2) on                
                lastBand = "";
                DoBandChg(band);
                set.rbAmp = 2;
                set.Save();
            }
        }
        // the txtAM text box has changed
        private void txtAM_TextChanged(object sender, EventArgs e)
        {
            set.txtAM = txtAM.Text; set.Save();
        }
        // the txtSSB text box has changed
        private void txtSSB_TextChanged(object sender, EventArgs e)
        {
            set.txtSSB = txtSSB.Text; set.Save();
        }
        // the txtDigi text box has changed
        private void txtDigi_TextChanged(object sender, EventArgs e)
        {
            set.txtDigi = txtDigi.Text; set.Save();
        }

        #endregion # Events #

        #region # Methods #

        #endregion # Methods #

        #endregion Auto Drive

        #region Aux BCD
        // see the LookUp() method for the bit handeling code
        // which is tied to the polling rate thru spCatRx

        #region # Events #

        // the Auxilary BCD Enable check box has changed
        private void chkAuxEnab_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAuxEnab.Checked)
            {
                chkFollowFreq.Enabled = true;
                chkAuxMacro.Enabled = true;
                chkAuxBit1.Enabled = true; chkAuxBit2.Enabled = true;
                chkAuxBit3.Enabled = true; chkAuxBit4.Enabled = true;
                chkAuxBit5.Enabled = true; chkAuxBit6.Enabled = true;
                chkAuxBit7.Enabled = true; chkAuxBit8.Enabled = true;
                lblBCD.ForeColor = Color.Green;
                mini.lblBCD.ForeColor = Color.Green;
                set.chkAuxEnab = true;
            }
            else
            {
                lblBCD.ForeColor = Color.Red;
                mini.lblBCD.ForeColor = Color.Red;
                set.chkAuxEnab = false;
                chkFollowFreq.Checked = false;
                chkAuxMacro.Checked = false;
                chkFollowFreq.Enabled = false;
                chkAuxMacro.Enabled = false;
                chkAuxBit1.Enabled = false; chkAuxBit2.Enabled = false;
                chkAuxBit3.Enabled = false; chkAuxBit4.Enabled = false;
                chkAuxBit5.Enabled = false; chkAuxBit6.Enabled = false;
                chkAuxBit7.Enabled = false; chkAuxBit8.Enabled = false;
            }
            chkDev0.Checked = true;
            set.Save();
        }
        // the follow freq check box has changed.
        private void chkFollowFreq_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAuxEnab.Checked)
            {
                if (chkFollowFreq.Checked)
                {
                    chkAuxBit1.Enabled = false; chkAuxBit2.Enabled = false;
                    chkAuxBit3.Enabled = false; chkAuxBit4.Enabled = false;
                    set.chkFollowFreq = true;
                }
                else
                {
                    chkAuxBit1.Enabled = true; chkAuxBit2.Enabled = true;
                    chkAuxBit3.Enabled = true; chkAuxBit4.Enabled = true;
                    set.chkFollowFreq = false;
                }
            }
            else
            {
                chkFollowFreq.Checked = false;
                chkAuxBit1.Enabled = true; chkAuxBit2.Enabled = true;
                chkAuxBit3.Enabled = true; chkAuxBit4.Enabled = true;
                set.chkFollowFreq = false;
            }
            set.Save();
        }
        // the Aux Macro check box has changed.
        private void chkAuxMacro_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAuxMacro.Checked)
            { set.chkAuxMacro = true; }
            else
            { set.chkAuxMacro = false; }
            set.Save();
        }

        private void chkAuxBit1_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAuxBit1.Checked) set.chkAuxBit1 = true;
            else set.chkAuxBit1 = false;
            set.Save();
        }

        private void chkAuxBit2_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAuxBit2.Checked) set.chkAuxBit2 = true;
            else set.chkAuxBit2 = false;
            set.Save();
        }

        private void chkAuxBit3_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAuxBit3.Checked) set.chkAuxBit3 = true;
            else set.chkAuxBit3 = false;
            set.Save();
        }

        private void chkAuxBit4_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAuxBit4.Checked) set.chkAuxBit4 = true;
            else set.chkAuxBit4 = false;
            set.Save();
        }

        private void chkAuxBit5_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAuxBit5.Checked) set.chkAuxBit5 = true;
            else set.chkAuxBit5 = false;
            set.Save();
        }

        private void chkAuxBit6_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAuxBit6.Checked) set.chkAuxBit6 = true;
            else set.chkAuxBit6 = false;
            set.Save();
        }

        private void chkAuxBit7_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAuxBit7.Checked) set.chkAuxBit7 = true;
            else set.chkAuxBit7 = false;
            set.Save();
        }

        private void chkAuxBit8_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAuxBit8.Checked) set.chkAuxBit8 = true;
            else set.chkAuxBit8 = false;
            set.Save();
        }

        #endregion # Events #

        #region # Methods #

        void AuxBcdInit()
        {
            chkAuxEnab.Checked = set.chkAuxEnab;
            chkFollowFreq.Checked = set.chkFollowFreq;
            chkAuxMacro.Checked = set.chkAuxMacro;
            chkAuxBit1.Checked = set.chkAuxBit1;
            chkAuxBit2.Checked = set.chkAuxBit2;
            chkAuxBit3.Checked = set.chkAuxBit3;
            chkAuxBit4.Checked = set.chkAuxBit4;
            chkAuxBit5.Checked = set.chkAuxBit5;
            chkAuxBit6.Checked = set.chkAuxBit6;
            chkAuxBit7.Checked = set.chkAuxBit7;
            chkAuxBit8.Checked = set.chkAuxBit8;

        }

        #endregion # Methods #

        #endregion Aux BCD

        #region DXLab DDE

        #region # Enums, Declarations and Vars #

        const int LinkMode = 1;
        const string ModeServer = "6";
        const string InvokeMacroCommand = "invokemacro";
        const string LinkItem = "DDECommand";
        const string LinkTopic = "WinWarbler|DDEServer";


        #endregion # Enums, Declarations and Vars #

        #region # Events #

        // Test for sending keys to another app (start notepad 1st)
        // then press the small unmarked button in the Tuning Knob group on the RCP tab
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        static extern byte VkKeyScan(char ch);

        const uint WM_KEYDOWN = 0x100;

        private void button7_Click(object sender, EventArgs e)
        {
            if (!TestPort.IsOpen) TestPort.Open();
            TestPort.Write("ZZID;");
            //RCP2port.Write("IF;");
            //sp.Write("ZZFA00000003850;");
            //Process[] procs = Process.GetProcessesByName("Notepad");
            //foreach (Process proc in procs)
            //{
            //    // look for untitled notepad window
            //    if (proc.MainWindowTitle == "Untitled - Notepad")
            //    {
            //        // get handle to Notepad's edit window
            //        IntPtr hWnd = FindWindowEx(proc.MainWindowHandle, IntPtr.Zero, "edit", null);
            //        // post "hello" to notepad
            //        string s = "Hello Radio de K5FR";
            //        for (int i = 0; i < s.Length; i++)
            //        {
            //            PostMessage(hWnd, WM_KEYDOWN, VkKeyScan(s[i]), 0);
            //        }
            //        break;
            //    }

            //}
        }

        #endregion # Events #

        #region # Methods #

        private static void SendWWmacro(string index)
        {
            try
            {
                // Create a client that connects to WinWarbler. 
                DdeClient client = new DdeClient("WinWarbler", "DDEServer");
                client.Connect();
                client.Execute(ModeServer.PadRight(3, '0') + InvokeMacroCommand + index, 60000);
                client.Disconnect();
            }
            catch (Exception e)
            {
                MessageBox.Show(new Form() { TopMost = true },
                    "An error ocurred while trying to communicate with WinWarbler\r\r" +
                    e.ToString(), "DDE Error!");
            }
        }

        #endregion # Methods #

        #endregion DXLab DDE

        #region FlexWire

        #region # Enums & Vars #

        const int mAdr = 0x40;  // Addr of Chip 1 ports 0/1
        const int mAdr1 = 0x42;  // Addr of Chip 2 ports 0/1
        const int cmd0 = 2;      // write to port 0
        const int cmd1 = 3;      // write to port 1
        const int su0 = 6;       // Setup for port 0
        const int su1 = 7;       // Setup for port 1

        #endregion Enums & Vars

        #region # Events #

        // the Data Signals Invert checkbox has changed
        private void chkDSInvert_CheckedChanged(object sender, EventArgs e)
        {
            if (chkDSInvert.Checked) { set.DSInv = true; }
            else { set.DSInv = false; }
            set.Save();
        }
        // the FlexWire checkbox has changed for the VHF+ matrix A
        private void chkFWa_CheckedChanged(object sender, EventArgs e)
        {
            if (chkFWa.Checked)
            {
                set.chkFWa = true; txtPortA.Enabled = false;
            }
            else
            {
                set.chkFWa = false; txtPortA.Enabled = true;
            }
            set.Save();
        }
        // the FlexWire checkbox has changed for the VHF+ matrix B
        private void chkFWb_CheckedChanged(object sender, EventArgs e)
        {
            if (chkFWb.Checked)
            {
                set.chkFWb = true; txtPortB.Enabled = false;
            }
            else
            {
                set.chkFWb = false; txtPortB.Enabled = true;
            }
            set.Save();
        }
        #endregion # Events #

        #region # Methods #

        // write to FlexWire port
        void WriteFW(int adr, int cmd, int data, bool inv)
        {
            string ad = adr.ToString("X");  // convert to hex
            if (adr < 16) ad = "0" + ad;    // add zero if needed
            string cm = cmd.ToString("X");  // convert to hex
            if (cmd < 16) cm = "0" + cm;    // add zero if needed
            if (inv) data = 255 ^ data;     // invert data if inv = true
            string val = data.ToString("X");  // convert to hex
            if (data < 16) val = "0" + val;   // add zero if needed
            // Send initialization string to 9555
            string p0 = "0" + su0.ToString("X");
            string p1 = "0" + su1.ToString("X");
            if (cmd == cmd0)
            { WriteToPort("ZZFY" + ad + p0 + "00;", 0); }
            else if (cmd == cmd1)
            { WriteToPort("ZZFY" + ad + p1 + "00;", 0); }
            else
            {
                MessageBox.Show(
                    ad + " is the wrong address for this board!\r\r" +
                   "Must be 0x40 or 0x42 only!", "Address Error",
                   MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            WriteToPort("ZZFY" + ad + cm + val + ";", iSleep);
        }
        // FW Setup routine (depreciated)
        void FWSetup()
        {
            WriteToPort("ZZFY400600;", iSleep);
            WriteToPort("ZZFY400700;", iSleep);
            WriteToPort("ZZFY420600;", iSleep);
            WriteToPort("ZZFY420700;", iSleep);
        }

        #endregion Methods

        #endregion FlexWire

        #region LP-100

        // A radio button in the LP-100 group has changed.
        private void grpLP_CheckChanged(object sender, EventArgs e)
        {
            if (rb100.Checked)
            { LPport.BaudRate = 38400; set.rb100 = 1; }
            else if (rb100A.Checked) { LPport.BaudRate = 115200; set.rb100 = 2; }
            set.Save();
        }
        // LP-100 Alarm set button was pressed
        private void btnAlarm_Click(object sender, EventArgs e)
        {
            if (chkLPenab.Checked)
            {
                if (rb100.Checked) LPport.Write(";A?");
                else if (rb100A.Checked) LPport.Write("A");
            }
        }
        // LP-100 Fast button was pressed (toggles peak hold mode (fast/peak/hold))
        private void btnFast_Click(object sender, EventArgs e)
        {
            {
                if (rb100.Checked) LPport.Write(";F?");
                else if (rb100A.Checked) LPport.Write("F");
            }
        }
        // LP-100 Mode button was pressed
        private void btnMode_Click(object sender, EventArgs e)
        {
            {
                if (rb100.Checked) LPport.Write(";M?");
                else if (rb100A.Checked) LPport.Write("M");
            }
        }
        // LP100 interval timer has elapsed
        void lpTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (rb100.Checked) LPport.Write(";P?");
                else if (rb100A.Checked) LPport.Write("P");
            }
            catch { }
        }
        // LP100 port has received data 
        string sLPBuf = "";
        private void LPport_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (chkLPenab.Checked)
            {
                string LPportMsg = "";
                byte[] data = new byte[LPport.BytesToRead];
                LPport.Read(data, 0, data.Length);
                sLPBuf += AE.GetString(data, 0, data.Length);
                Regex rex = new Regex(@";.*?,.*?,.*?,.*?,.*?,.*?,.*?,.*?,.*?\d\d");
                for (Match m = rex.Match(sLPBuf); m.Success; m = m.NextMatch())
                {   //loop thru the buffer and find matches
                    LPportMsg = m.Value;
                    sLPBuf = sLPBuf.Replace(m.Value, "");//remove the match from the buffer

                    string fwd = LPportMsg.Substring(1, 7);
                    string alarm = LPportMsg.Substring(21, 1);
                    if (alarm == "0") SetAlarm("Off");
                    else if (alarm == "1") SetAlarm("1.5");
                    else if (alarm == "2") SetAlarm("2.0");
                    else if (alarm == "3") SetAlarm("2.5");
                    else if (alarm == "4") SetAlarm("3.0");

                    string peak = LPportMsg.Substring(32, 1);
                    if (peak == "0") SetLPPeak("Fast");
                    else if (peak == "1") SetLPPeak("Peak");
                    else if (peak == "2") SetLPPeak("Tune");

                    string power = LPportMsg.Substring(30, 1);
                    if (power == "0") SetPower("Power: High");
                    else if (power == "1") SetPower("Power: Mid");
                    else if (power == "2") SetPower("Power: Low");

                    string swr = LPportMsg.Substring(LPportMsg.Length - 4, 4);
                    SetAvg(fwd); mini.SetAvg(fwd);
                    SetSwr(swr); mini.SetSwr(swr);
                }
            }
        }
        // LP port number has changed
        private void cboLPport_SelectedIndexChanged(object sender, EventArgs e)
        {
            lpTimer.Enabled = false;
            if (LPport.IsOpen) LPport.Close();
            if (cboLPport.SelectedIndex > 0)
            {
                LPport.PortName = cboLPport.SelectedItem.ToString();
                try
                {
                    LPport.Open();
                    lpTimer.Enabled = true;
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "The LP-100 serial port " + LPport.PortName +
                        " cannot be opened!\n", "Port Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    chkLPenab.Checked = false; 
                    cboLPport.SelectedIndex = 0;
                    set.LPportNum = 0;
                    set.Save();
                    return;
                }
            }
            else
            {
                cboLPport.SelectedIndex = 0; 
                lpTimer.Enabled = false;
                chkLPenab.Checked = false;
            }
            // save new port setting
            set.LPportNum = cboLPport.SelectedIndex;
            set.Save();
        }
        // LP timer interval has changed
        private void txtLPint_TextChanged(object sender, EventArgs e)
        {
            try
            {
                lpTimer.Interval = Convert.ToDouble(txtLPint.Text);
                set.LPint = txtLPint.Text;
                set.Save();
            }
            catch { }
        }
        // LP enabled check box has changed
        private void chkLPenab_CheckedChanged(object sender, EventArgs e)
        {
            if (chkLPenab.Checked)
            {
                if (cboLPport.SelectedIndex > 0)
                {
                    lpTimer.Enabled = true; set.LPenab = true; chkWNEnab.Checked = false;
                    chkPM.Checked = false; lblAvg.Text = "Fwd"; mini.lblAvg.Text = "Fwd";
                    txtAvg.Enabled = true; txtSWR.Enabled = true;
                    mini.txtAvg.Enabled = true; mini.txtSWR.Enabled = true;
                    txtFwd.Visible = false; lblFwd.Visible = false;
                    mini.txtFwd.Visible = false; mini.lblFwd.Visible = false;
                }
                else
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "No port has been selected for the LP-100.\n\n" +
                        "Please select a valid port number and try again.", "Port Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    chkLPenab.Checked = false; lpTimer.Enabled = false; set.LPenab = false;
                    txtAvg.Text = ""; txtFwd.Text = ""; txtSWR.Text = "";
                    mini.txtAvg.Text = ""; mini.txtFwd.Text = ""; mini.txtSWR.Text = "";
                    //txtAvg.Enabled = false; 
                    txtFwd.Enabled = false; 
                    //txtSWR.Enabled = false;
                    //mini.txtAvg.Enabled = false; 
                    mini.txtFwd.Enabled = false; 
                    //mini.txtSWR.Enabled = false;
                    cboLPport.SelectedIndex = 0;
                }
            }
            else
            {
                lpTimer.Enabled = false; set.LPenab = false;
                txtAvg.Text = ""; txtFwd.Text = ""; txtSWR.Text = "";
                mini.txtAvg.Text = ""; mini.txtFwd.Text = ""; mini.txtSWR.Text = "";
                //txtAvg.Enabled = false; 
                txtFwd.Enabled = false; 
                //txtSWR.Enabled = false;
                //mini.txtAvg.Enabled = false; 
                mini.txtFwd.Enabled = false; 
                //mini.txtSWR.Enabled = false;

            }
            set.Save();
        }

        #endregion LP-100

        #region Macro Routines

        #region# Macro Events #
        // The control has been double-clicked
        private void dgm_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (dgm.AllowUserToAddRows) dgm.AllowUserToAddRows = false;
            else dgm.AllowUserToAddRows = true;
        }
        // Hides/Un-hides the macro editor and file dialog controls.
        // To use click in an open area of the Macro tab.
        private void tabMacro_Click(object sender, EventArgs e)
        {
            if (dgm.Visible)
            { dgm.Visible = false;}
            else
            { dgm.Visible = true; }
        }
        // A Data Grid cell value changed, change the button text.
        bool bMacChg; // flag for macro data changed & not saved.
        private void dgm_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            bMacChg = true;
            dgm.CurrentRow.Cells["Command"].Value =
                dgm.CurrentRow.Cells["Command"].Value.ToString().ToUpper();
            string dgmData = dgm.CurrentRow.Cells["Button"].Value.ToString();
            if (dgmData != null)
            {
                switch (e.RowIndex)
                {
                    case 0: btnMacro1.Text = dgmData;
                        mini.toolTip1.SetToolTip(mini.btnMacro1, this.btnMacro1.Text); break;
                    case 1: btnMacro2.Text = dgmData;
                        mini.toolTip1.SetToolTip(mini.btnMacro2, this.btnMacro2.Text); break;
                    case 2: btnMacro3.Text = dgmData;
                        mini.toolTip1.SetToolTip(mini.btnMacro3, this.btnMacro3.Text); break;
                    case 3: btnMacro4.Text = dgmData;
                        mini.toolTip1.SetToolTip(mini.btnMacro4, this.btnMacro4.Text); break;
                    case 4: btnMacro5.Text = dgmData;
                        mini.toolTip1.SetToolTip(mini.btnMacro5, this.btnMacro5.Text); break;
                    case 5: btnMacro6.Text = dgmData;
                        mini.toolTip1.SetToolTip(mini.btnMacro6, this.btnMacro6.Text); break;
                    case 6: btnMacro7.Text = dgmData;
                        mini.toolTip1.SetToolTip(mini.btnMacro7, this.btnMacro7.Text); break;
                    case 7: btnMacro8.Text = dgmData;
                        mini.toolTip1.SetToolTip(mini.btnMacro8, this.btnMacro8.Text); break;
                    case 8: btnMacro9.Text = dgmData;
                        mini.toolTip1.SetToolTip(mini.btnMacro9, this.btnMacro9.Text); break;
                    case 9: btnMacro10.Text = dgmData;
                        mini.toolTip1.SetToolTip(mini.btnMacro10, this.btnMacro10.Text); break;
                    case 10: btnMacro11.Text = dgmData;
                        mini.toolTip1.SetToolTip(mini.btnMacro11, this.btnMacro11.Text); break;
                    case 11: btnMacro12.Text = dgmData;
                        mini.toolTip1.SetToolTip(mini.btnMacro12, this.btnMacro12.Text); break;
                    case 12: btnMacro13.Text = dgmData;
                        mini.toolTip1.SetToolTip(mini.btnMacro13, this.btnMacro13.Text); break;
                    case 13: btnMacro14.Text = dgmData;
                        mini.toolTip1.SetToolTip(mini.btnMacro14, this.btnMacro14.Text); break;
                    case 14: btnMacro15.Text = dgmData;
                        mini.toolTip1.SetToolTip(mini.btnMacro15, this.btnMacro15.Text); break;
                    case 15: btnMacro16.Text = dgmData;
                        mini.toolTip1.SetToolTip(mini.btnMacro16, this.btnMacro16.Text); break;
                    case 16: btnMacro16.Text = dgmData; break;
                    case 17: btnMacro16.Text = dgmData; break;
                    case 18: btnMacro16.Text = dgmData; break;
                    case 19: btnMacro16.Text = dgmData; break;
                    default: break;
                }
            }
        }
        // Saves the macro commands displayed in the grid to the file name selected 
        // in the File Name text box. If name is empty a file dialog is called.
        private void btnMacSave_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result;
                result = MessageBox.Show(new Form() { TopMost = true },
                    "Saving Macro data to file: " + MacFileName + " Is this ok?\r\r" +
                    "If 'Yes' press Ok to continue saving the file.\r" +
                    "If 'No' press No to select a new file name.\r" +
                    "Press Cancel to abort this operation.", "Information",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1);

                if (result == DialogResult.Cancel)
                    return;
                else if (result == DialogResult.No)
                {
                    // get the App Data Path
                    if (app_data_path == "")
                    {
                        AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                        + "\\DDUtil\\";
                    }
                    string mfileDir = app_data_path + "Macros\\";
                    if (!Directory.Exists(mfileDir))
                        Directory.CreateDirectory(mfileDir);

                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.InitialDirectory = mfileDir;
                    sfd.Filter = "xml files|*.xml";
                    sfd.Title = "Select or enter a name to save";
                    string pFile = "";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        pFile = sfd.FileName;
                        txtMacFile.Text = pFile;
                        sfd.Dispose();
                    }
                    else
                    {
                        sfd.Dispose();
                        return;
                    }
                }
                // Write out the Macro Data from the grid to the XML file
                dgm.DataSource = dsm;
                //dgm.DataMember = ("macro");
                MacFileName = txtMacFile.Text;
                if (File.Exists(MacFileName)) File.Delete(MacFileName);
                dsm.WriteXml(MacFileName);
                set.MacDataFile = MacFileName;
                set.Save();
                GetMacData(MacFileName);
                bMacChg = false;
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                    "Unable to write to log");
            }
        }
        // Opens file dialog to load the Macro Data file.
        // Calls GetMacData()
        private void btnMacSelect_Click(object sender, EventArgs e)
        {
            try
            {
                // get the App Data Path
                if (app_data_path == "")
                {
                    AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    + "\\DDUtil\\";
                }
                string fileDir = app_data_path + "Macros\\";
                if (!Directory.Exists(fileDir))
                    Directory.CreateDirectory(fileDir);

                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = fileDir;
                ofd.Filter = "xml files|*.xml";
                ofd.Title = "Select a Macro file to load";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtMacFile.Text = ofd.FileName;
                    MacFileName = txtMacFile.Text;
                    set.MacDataFile = MacFileName;
                    set.Save();
                    GetMacData(MacFileName);
                    ofd.Dispose();
                }
                else
                {
                    ofd.Dispose();
                    return;
                }
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                    "Unable to write to log");
            }
        }
        // If SDR not transmitting and SteppIR is not moving
        // Process the commands associated with a macro key, calls ParseBuffer()
        public void ProcessMacroButton(int button)
        {
            try
            {
                if (StepCtr == 0)// && (xOn == "0" || xOn == ""))
                {   // 
                    if (dgm.Rows[button - 1].Cells[1].Value.ToString() == "")
                    {
                        throw new Exception();
                    }
                    string cmds = dgm.Rows[button - 1].Cells[1].Value.ToString();
                    ParseBuffer(cmds);
                }
            }

            catch (ArgumentOutOfRangeException)
            {
                //MessageBox.Show("It appears there is no macro for this function.\r\r" +
                //    "Macro M21 is required for this function to work.\r" +
                //    "Please check the macro editor, correct and try again." );
            }
            catch (NullReferenceException)
            {
                int btn = button + 1;
                MessageBox.Show(new Form() { TopMost = true }, 
                    "There are no commands setup for this Macro " + button);
            }
            catch (Exception)
            {
                MessageBox.Show("It appears there may not be macro for this function.\r\r" +
                    "Please check the macro editor, correct and try again.");
            }
        }
        // Macro button #1 was pressed
        public void btnMacro1_Click(object sender, EventArgs e)
        { ProcessMacroButton(1); }
        // Macro button #2 was pressed
        public void btnMacro2_Click(object sender, EventArgs e)
        { ProcessMacroButton(2); }
        // Macro button #3 was pressed
        public void btnMacro3_Click(object sender, EventArgs e)
        { ProcessMacroButton(3); }
        // Macro button #4 was pressed
        public void btnMacro4_Click(object sender, EventArgs e)
        { ProcessMacroButton(4); }
        // Macro button #5 was pressed
        public void btnMacro5_Click(object sender, EventArgs e)
        { ProcessMacroButton(5); }
        // Macro button #6 was pressed
        public void btnMacro6_Click(object sender, EventArgs e)
        { ProcessMacroButton(6); }
        // Macro button #7 was pressed
        public void btnMacro7_Click(object sender, EventArgs e)
        { ProcessMacroButton(7); }
        // Macro button #8 was pressed
        public void btnMacro8_Click(object sender, EventArgs e)
        { ProcessMacroButton(8); }
        // Macro button #9 was pressed
        public void btnMacro9_Click(object sender, EventArgs e)
        { ProcessMacroButton(9); }
        // Macro button #10 was pressed
        public void btnMacro10_Click(object sender, EventArgs e)
        { ProcessMacroButton(10); }
        // Macro button #11 was pressed
        public void btnMacro11_Click(object sender, EventArgs e)
        { ProcessMacroButton(11); }
        // Macro button #12 was pressed
        public void btnMacro12_Click(object sender, EventArgs e)
        { ProcessMacroButton(12); }
        // Macro button #13 was pressed
        public void btnMacro13_Click(object sender, EventArgs e)
        { ProcessMacroButton(13); }
        // Macro button #14 was pressed
        public void btnMacro14_Click(object sender, EventArgs e)
        { ProcessMacroButton(14); }
        // Macro button #15 was pressed
        public void btnMacro15_Click(object sender, EventArgs e)
        { ProcessMacroButton(15); }
        // Macro button #16 was pressed
        public void btnMacro16_Click(object sender, EventArgs e)
        { ProcessMacroButton(16); }
        // Macro button #17 was pressed
        public void btnMacro17_Click(object sender, EventArgs e)
        { ProcessMacroButton(17); }
        // Macro button #18 was pressed
        public void btnMacro18_Click(object sender, EventArgs e)
        { ProcessMacroButton(18); }
        // Macro button #19 was pressed
        public void btnMacro19_Click(object sender, EventArgs e)
        { ProcessMacroButton(19); }
        // Macro button #20 was pressed
        public void btnMacro20_Click(object sender, EventArgs e)
        { ProcessMacroButton(20); }
        // Macro button #21 was pressed
        public void btnMacro21_Click(object sender, EventArgs e)
        { ProcessMacroButton(21); }
        // Macro button #22 was pressed
        public void btnMacro22_Click(object sender, EventArgs e)
        { ProcessMacroButton(22); }
        // Macro button #23 was pressed
        public void btnMacro23_Click(object sender, EventArgs e)
        { ProcessMacroButton(23); }
        // Macro button #24 was pressed
        public void btnMacro24_Click(object sender, EventArgs e)
        { ProcessMacroButton(24); }

        // A key was pressed check for "F" key
        private void Setup_KeyDown(object sender, KeyEventArgs e)
        {
            if (!chkShortCut.Checked) // If NOT Short-Cuts disabled
            {
                // Alt+9 menu assigned to open Alpha 9500 window
                // Alt+C menu assigned to Clear Error Log
                // Alt+O menu assigned to Open Error Log
                // Alt+M menu assigned to Mini window
                // Alt+R menu assigned to open rotor window
                // Alt+S menu assigned to open SPE Amp window
                // Ctrl + Down arrow menu assigned to Save Profile
                // Ctrl + Up arrow menu assigned to Load Profile

                if (e.KeyCode == Keys.F1 && e.Modifiers == Keys.Shift)
                { ProcessMacroButton(13); }
                else if (e.KeyCode == Keys.F2 && e.Modifiers == Keys.Shift)
                { ProcessMacroButton(14); }
                else if (e.KeyCode == Keys.F3 && e.Modifiers == Keys.Shift)
                { ProcessMacroButton(15); }
                else if (e.KeyCode == Keys.F4 && e.Modifiers == Keys.Shift)
                { ProcessMacroButton(16); }
                else if (e.KeyCode == Keys.F5 && e.Modifiers == Keys.Shift)
                { ProcessMacroButton(17); }
                else if (e.KeyCode == Keys.F6 && e.Modifiers == Keys.Shift)
                { ProcessMacroButton(18); }
                else if (e.KeyCode == Keys.F7 && e.Modifiers == Keys.Shift)
                { ProcessMacroButton(19); }
                else if (e.KeyCode == Keys.F8 && e.Modifiers == Keys.Shift)
                { ProcessMacroButton(20); }
                else if (e.KeyCode == Keys.F9 && e.Modifiers == Keys.Shift)
                { ProcessMacroButton(21); }
                else if (e.KeyCode == Keys.F10 && e.Modifiers == Keys.Shift)
                { ProcessMacroButton(22); }
                else if (e.KeyCode == Keys.F11 && e.Modifiers == Keys.Shift)
                { ProcessMacroButton(23); }
                else if (e.KeyCode == Keys.F12 && e.Modifiers == Keys.Shift)
                { ProcessMacroButton(24); }
                else if (e.KeyCode == Keys.F1)
                { ProcessMacroButton(1); }
                else if (e.KeyCode == Keys.F2)
                { ProcessMacroButton(2); }
                else if (e.KeyCode == Keys.F3)
                { ProcessMacroButton(3); }
                else if (e.KeyCode == Keys.F4)
                { ProcessMacroButton(4); }
                else if (e.KeyCode == Keys.F5)
                { ProcessMacroButton(5); }
                else if (e.KeyCode == Keys.F6)
                { ProcessMacroButton(6); }
                else if (e.KeyCode == Keys.F7)
                { ProcessMacroButton(7); }
                else if (e.KeyCode == Keys.F8)
                { ProcessMacroButton(8); }
                else if (e.KeyCode == Keys.F9)
                { ProcessMacroButton(9); }
                else if (e.KeyCode == Keys.F10)
                { ProcessMacroButton(10); }
                else if (e.KeyCode == Keys.F11)
                { ProcessMacroButton(11); }
                else if (e.KeyCode == Keys.F12)
                { ProcessMacroButton(12); }
                else if (e.Control && e.KeyCode == Keys.Oemtilde) // Set Split
                { btnSplit_Click(null, null); }
                else if (e.Control && e.KeyCode == Keys.A) // Saves Auto Drive setting
                { btnDrive_Click(null, null); }
                else if (e.Control && e.KeyCode == Keys.B) // SteppIR to Bi Direction
                { rbBiDir.Checked = true; }
                else if (e.Control && e.KeyCode == Keys.C) // Memory Load
                { btnMemLoad_Click(null, null); }
                else if (e.Control && e.KeyCode == Keys.F) // SteppIR to Forward
                { rbFwd.Checked = true; }
                else if (e.Control && e.KeyCode == Keys.L) // Set drive to low power
                { LowPower(); }
                else if (e.Control && e.KeyCode == Keys.N) // Open Memory Note window
                { txtMemFreq_DoubleClick(null, null); }
                else if (e.Control && e.KeyCode == Keys.O) // Toggle PTT
                { btnByp_Click(null, null); }
                else if (e.Control && e.KeyCode == Keys.R) // SteppIR to Reverse
                { rb180.Checked = true; }
                else if (e.Control && e.Shift && e.KeyCode == Keys.S) // Power On/Off
                { btnFlexOn_Click(null, null); }
                else if (e.Control && e.KeyCode == Keys.U) // Undo freq move
                { WriteToPort("FA" + prevFreq + ";", iSleep); }
                else if (e.Control && e.KeyCode == Keys.V) // Memory Save
                { btnMemSave_Click(null, null); }
                else if (e.Control && e.KeyCode == Keys.X) // Memory select (1-5)
                { btnReCall_Click(null, null); }
                else if (e.Control && e.KeyCode == Keys.Z) // Toggle VFO/Memory
                { btnMV_Click(null, null); }
                // TK tuning rate up
                else if (e.Control && (e.KeyCode == Keys.Oemplus || e.KeyValue == 107))
                { if (cboTstep.SelectedIndex < 14) cboTstep.SelectedIndex += 1; }
                // TK tuning rate down
                else if (e.Control && (e.KeyCode == Keys.OemMinus || e.KeyValue == 109))
                { if (cboTstep.SelectedIndex > 0) cboTstep.SelectedIndex -= 1; }
                // Rotor bearing Down 5 degrees
                else if (e.Control && e.Shift && e.KeyCode == Keys.Down)
                {
                    int i = Convert.ToInt32(txtSP.Text);
                    if (i >= 5)
                    {
                        i -= 5;
                        txtSP.Text = i.ToString();
                        btnSP_Click(null, null);
                    }
                }
                // Rotor bearing Up 5 degrees
                else if (e.Control && e.Shift && e.KeyCode == Keys.Up)
                {
                    int i = Convert.ToInt32(txtSP.Text);
                    if (i <= 360)
                    {
                        i += 5;
                        txtSP.Text = i.ToString();
                        btnSP_Click(null, null);
                    }
                }
            }
        }
        // Adds macro number text to the row header
        public void dgm_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            string rowNum = (e.RowIndex + 1).ToString();

            //determine the display size of the row number string using
            //the DataGridView's current font.
            SizeF size = e.Graphics.MeasureString(rowNum, this.Font);

            //adjust the width of the column that contains the row header cells 
            if (dgm.RowHeadersWidth < (int)(size.Width + 20))
                dgm.RowHeadersWidth = (int)(size.Width + 20);

            // Set the brush for drawing the text
            Brush b = SystemBrushes.ControlText;

            // Draw the row number string using the brush defined above
            e.Graphics.DrawString("M" + rowNum, this.Font, b, e.RowBounds.Location.X + 15,
                       e.RowBounds.Location.Y + ((e.RowBounds.Height - size.Height) / 2));
        }

        #endregion# Macro Events #

        #region# Macro Methods #
        // Loads Macro Data File from disk and displays in datagrid, 
        // adds name from button cell to macro buttons
        private void GetMacData(string fileName)
        {
            try
            {
                // Read in the Band Data from the XML file 
                dsm = new DataSet();
                dsm.Clear();
                dsm.ReadXml(fileName);
                dgm.DataSource = dsm;
                dgm.DataMember = ("macro");
                for (int i = 0; i < dgm.RowCount; i++)
                {   // write the names on the buttons
                    string btnName = dgm.Rows[i].Cells[0].Value.ToString();
                    if (btnName != null)
                    {
                        DataGridViewRowHeaderCell cell = new DataGridViewRowHeaderCell();
                        switch (i)
                        {
                            case 0: btnMacro1.Text = btnName; break;
                            case 1: btnMacro2.Text = btnName; break;
                            case 2: btnMacro3.Text = btnName; break;
                            case 3: btnMacro4.Text = btnName; break;
                            case 4: btnMacro5.Text = btnName; break;
                            case 5: btnMacro6.Text = btnName; break;
                            case 6: btnMacro7.Text = btnName; break;
                            case 7: btnMacro8.Text = btnName; break;
                            case 8: btnMacro9.Text = btnName; break;
                            case 9: btnMacro10.Text = btnName; break;
                            case 10: btnMacro11.Text = btnName; break;
                            case 11: btnMacro12.Text = btnName; break;
                            case 12: btnMacro13.Text = btnName; break;
                            case 13: btnMacro14.Text = btnName; break;
                            case 14: btnMacro15.Text = btnName; break;
                            case 15: btnMacro16.Text = btnName; break;
                            case 16: btnMacro17.Text = btnName; break;
                            case 17: btnMacro18.Text = btnName; break;
                            case 18: btnMacro19.Text = btnName; break;
                            case 19: btnMacro20.Text = btnName; break;
                            case 20: btnMacro21.Text = btnName; break;
                            case 21: btnMacro22.Text = btnName; break;
                            case 22: btnMacro23.Text = btnName; break;
                            case 23: btnMacro24.Text = btnName; break;
                            default: break;
                        }
                    }
                }
            }
            catch { }
        }
        // Processes commands from the data grid, calls WriteToPort()
        // which sends commands to the radio. This routine includes a buffer
        // (queue)to hold multiple commands until the radio is ready.
        private void ParseBuffer(string cmd)
        {
            try
            {   // put the CAT command string into the comm buffer
                string CmdBuf = cmd;
                string OutBuf = "";
                Regex rex = new Regex(".*?;");  //accept any string ending in ";"		
                //loop thru the buffer and find matches
                for (Match m = rex.Match(CmdBuf); m.Success; m = m.NextMatch())
                {
                    OutBuf = m.Value;
                    // remove the match from the buffer if found
                    CmdBuf = CmdBuf.Replace(m.Value, "");
                    //if (OutBuf.Substring(0, 1) == ":")
                    //{
                    //    macVar = true;
                    //    OutBuf = OutBuf.Substring(1, 4);
                    //}
                    if (OutBuf.Length < 5) malform(OutBuf);
                    if (OutBuf.Substring(0, 2) == "DD") DDParse(OutBuf);
                    else WriteToPort(OutBuf, iSleep);
                }
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                    "Unable to write to log");
            }
        }
        // Macro command is DDUtil specific "DDSP045;"
        private void DDParse(string cmd)
        {
            cmd = cmd.Substring(0, cmd.Length - 1);

            if (cmd.Length < 4) { malform(cmd); return; }
            string pre = cmd.Substring(2, 2);             // Command prefix
            string ops = cmd.Substring(4, cmd.Length - 4);  // command operators less ";"
            switch (pre)
            {
                case "34":  // Set SteppIR to 3/4 wavelength Mode (verticle only)
                    rb34.Checked = true; 
                    break;
                case "AD": // Enable/Disable AutoDrive function.
                    if (ops == "0") 
                        chkAutoDrv.Checked = false;
                    else if (ops == "1") 
                        chkAutoDrv.Checked = true;
                    break;
                case "AM": // Set AT-AUTO to Auto Mode
                    byte[] a = { 0xFE, 0xFE, 0xE0, 0x3A, 0x07, 0x00, 0xFD };
                    RepeatPort.Write(a, 0, 7); 
                    break;
                case "AP": // Set Amp 1/2 (DDAP1;)
                    if (ops == "1")
                        rbAmp1.Checked = true;
                    else if (ops == "2")
                        rbAmp2.Checked = true;
                    break;
                case "BC": // Set Aux BCD bits
                    if (chkAuxEnab.Checked && chkAuxMacro.Checked)
                    {
                        if (!chkFollowFreq.Checked)
                        {
                            if (ops.Substring(7, 1) == "1") chkAuxBit1.Checked = true;
                            else chkAuxBit1.Checked = false;
                            if (ops.Substring(6, 1) == "1") chkAuxBit2.Checked = true;
                            else chkAuxBit2.Checked = false;
                            if (ops.Substring(5, 1) == "1") chkAuxBit3.Checked = true;
                            else chkAuxBit3.Checked = false;
                            if (ops.Substring(4, 1) == "1") chkAuxBit4.Checked = true;
                            else chkAuxBit4.Checked = false;
                        }
                        if (ops.Substring(3, 1) == "1") chkAuxBit5.Checked = true;
                        else chkAuxBit5.Checked = false;
                        if (ops.Substring(2, 1) == "1") chkAuxBit6.Checked = true;
                        else chkAuxBit6.Checked = false;
                        if (ops.Substring(1, 1) == "1") chkAuxBit7.Checked = true;
                        else chkAuxBit7.Checked = false;
                        if (ops.Substring(0, 1) == "1") chkAuxBit8.Checked = true;
                        else chkAuxBit8.Checked = false;
                    }
                    break;
                case "BI": // Set SteppIR to Bi-Directional Mode
                    rbBiDir.Checked = true; 
                    break;
                case "BL": // Set AT-AUTO to Select Balanced
                    byte[] e = { 0xFE, 0xFE, 0xE0, 0x3A, 0x08, 0x10, 0xFD };
                    RepeatPort.Write(e, 0, 7); 
                    break;
                case "BM": // Set AT-AUTO to Bypass Mode
                    byte[] c = { 0xFE, 0xFE, 0xE0, 0x3A, 0x07, 0x02, 0xFD };
                    RepeatPort.Write(c, 0, 7); 
                    break;
                case "CX": // Set AT-AUTO to Select Coax
                    byte[] d = { 0xFE, 0xFE, 0xE0, 0x3A, 0x08, 0x00, 0xFD };
                    RepeatPort.Write(d, 0, 7); 
                    break;
                case "FA": // Write hex to FlexWire Adapter port 4
                    int data = int.Parse(ops.Substring(0, 2), NumberStyles.HexNumber);
                    if (ops.Length == 3 && ops.Substring(2, 1) == "I")
                        data = data ^ 255;
                    WriteFW(mAdr1, cmd1, data, false); 
                    break;
                case "FW":  // Set SteppIR to Forward Mode
                    rbFwd.Checked = true; 
                    break;
                case "LP": // write to desig LPT port
                    int icmd = int.Parse(ops.Substring(ops.Length-2,2),NumberStyles.HexNumber);
                    int iport = Convert.ToInt32(ops.Substring(0, ops.Length-2));
                    OutParallelPort(iport, icmd);
                    break;
                case "MM": // Set AT-AUTO to Manual Mode
                    byte[] b = { 0xFE, 0xFE, 0xE0, 0x3A, 0x07, 0x01, 0xFD };
                    RepeatPort.Write(b, 0, 7); 
                    break;
                case "MW": // Open Mini Window
                    ShowMini();
                    break;
                case "RE": // Enable Rotor Control
                    if (ops.Length == 1)
                    {
                        if (ops == "1") chkRotorEnab.Checked = true;
                        else chkRotorEnab.Checked = false;
                    }
                    else malform(pre); 
                    break;
                case "RV":  // Set SteppIR to Reverse Mode
                    rb180.Checked = true; 
                    break;
                case "SE":  // Enabled SteppIR
                    if (ops.Length == 1)
                    {
                        if (ops == "1") chkStep.Checked = true;
                        else chkStep.Checked = false;
                    }
                    else malform(pre); 
                    break;
                case "SP":  // Turn rotor
                    if (ops.Length == 3)
                    {
                        txtSP.Text = cmd.Substring(4, 3); btnSP_Click(null, null);
                    }
                    else malform(pre); 
                    break;
                case "SR":  // SO2R file load
                    if (ops.Length >= 5)
                    {
                        LoadMacro(ops);
                    }
                    else malform(pre); 
                    break;
                case "WW":  // activate macro in WinWarbler
                    if (ops.Length >= 1)
                    {
                        SendWWmacro(ops.Trim());
                    }
                    break;
                default: malform(cmd); 
                    break;
            }
        }
        private void malform(string pre)
        {
            Notification alert = new Notification();
            Notification.notiIntvl = 7000;
            Notification.notiMsg =
                "The command " + pre + " is not formed correctly or is not " +
                "a valid Flex or DDUtil macro command.\r\r" +
                "Please re-format the command and try again.\r";
            alert.Show();
        }

        #endregion# Macro Methods #

        #endregion Macro Routines

        #region Macro Switch

        // the macro switch has changed
        private void MacroPort_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            if (chkMacSwEnab.Checked)
            {
                SerialPinChange SerialPinChange1;
                SerialPinChange1 = e.EventType;
                switch (SerialPinChange1)
                {
                    case SerialPinChange.CtsChanged:
                        if (MacroPort.CtsHolding)  // 
                        {
                            if (txtMacSwOn.Text.Length > 5)
                                ParseBuffer(txtMacSwOn.Text);
                            if (txtMacSwMsg.Text.Length > 2)
                            { MessageBox.Show(new Form() { TopMost = true },
                                txtMacSwMsg.Text, "Macro Switch Event!"); }
                        }
                        else if (txtMacSwOff.Text.Length > 5)
                            ParseBuffer(txtMacSwOff.Text);
                        break;
                }
            }
        }
        // the macro switch com port has changed
        private void cboMacSwPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (MacroPort.IsOpen) MacroPort.Close();
            if (cboMacSwPort.SelectedIndex > 0)
            {
                MacroPort.PortName = cboMacSwPort.SelectedItem.ToString();
                try
                {
                    MacroPort.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true },
                       "The Macro switch serial port " + MacroPort.PortName +
                       " cannot be opened!\n", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboMacSwPort.SelectedIndex = 0;
                }
            }
            set.cboMacSwPort = cboMacSwPort.SelectedIndex;
            set.Save();
        }
        // the macro switch enable check box has changed
        private void chkMacSwEnab_CheckedChanged(object sender, EventArgs e)
        {
            if (chkMacSwEnab.Checked) { set.chkMacSwEnab = true; }
            else { set.chkMacSwEnab = false; }
            set.Save();
        }
        // the macro switch On CAT command has changed
        private void txtMacSwOn_TextChanged(object sender, EventArgs e)
        {
            set.txtMacSwOn = txtMacSwOn.Text; set.Save();
        }
        // the macro switch Off CAT command has changed
        private void txtMacSwOff_TextChanged(object sender, EventArgs e)
        {
            set.txtMacSwOff = txtMacSwOff.Text; set.Save();
        }
        // the Mac Switch Message has changed
        private void txtMacSwMsg_TextChanged(object sender, EventArgs e)
        {
            set.txtMacSwMsg = txtMacSwMsg.Text;
            set.Save();
        }
        // the message window has been double-clicked
        private void txtMacSwMsg_DoubleClick(object sender, EventArgs e)
        {
            Process.Start("http://k5fr.com/ddutilwiki/index.php?title=Setup#Macro_Switch");
        }

        #endregion Macro Switch

        #region Memory

        #region # Declarations #

        // create memory array wit 5 rows and 10 columns
        public string[,] mem = new string[5,12];
        
        public int idxMem = 5; // mem array index
        string vfoA, vfoB, Rx1, Rx2, TxA, FltRx1, TxPro, ModeR1; // PSDR vars
        string mvFq, mvFl, mvMd;    // M<>V vars (freq, fltr, mode)
        int MV = 1;    // 1 = vfo, 2 = memory

        #endregion # Declarations #

        #region # Delegates #

        // Write to MV button
        delegate void SetMVCallback(string text);
        public void SetMV(string text)
        {
            if (this.btnMV.InvokeRequired)
            {
                SetMVCallback d = new SetMVCallback(SetMV);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "Memory")
                {
                    btnMV.BackColor = Color.Yellow;
                    mini.btnMV.BackColor = Color.Yellow;
                }
                else
                {
                    btnMV.BackColor = Color.Empty;
                    mini.btnMV.BackColor = Color.Empty;
                }
            }
        }
        // Write to MemSave button
        delegate void SetMemSaveCallback(string text);
        public void SetMemSave(string text)
        {
            if (this.btnMemSave.InvokeRequired)
            {
                SetMemSaveCallback d = new SetMemSaveCallback(SetMemSave);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "Yellow")
                {
                    btnMemSave.BackColor = Color.Yellow;
                    mini.btnMemSave.BackColor = Color.Yellow;
                }
                else
                {
                    btnMemSave.BackColor = Color.Empty;
                    mini.btnMemSave.BackColor = Color.Empty;
                }
            }
        }
        // Write to MemFreq button
        delegate void SetMemFreqCallback(string text);
        public void SetMemFreq(string text)
        {
            if (this.txtMemFreq.InvokeRequired)
            {
                SetMemFreqCallback d = new SetMemFreqCallback(SetMemFreq);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtMemFreq.Text = text;
                mini.txtMemFreq.Text = text;
            }
        }
        // Write to MemMode button
        delegate void SetMemModeCallback(string text);
        public void SetMemMode(string text)
        {
            if (this.txtMemMode.InvokeRequired)
            {
                SetMemModeCallback d = new SetMemModeCallback(SetMemMode);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtMemMode.Text = text;
                mini.txtMemMode.Text = text;
            }
        }
        // Write to MemIdx button
        delegate void SetMemIdxCallback(string text);
        public void SetMemIdx(string text)
        {
            if (this.txtMemIdx.InvokeRequired)
            {
                SetMemIdxCallback d = new SetMemIdxCallback(SetMemIdx);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtMemIdx.Text = text; 
                mini.txtMemIdx.Text = text;
            }
        }
        
        #endregion # Delegates #

        #region # Events #

        // the memory/vfo toggle button has been pressed
        public void btnMV_Click(object sender, EventArgs e)
        { 
            if (idxMem == 5 || mem[idxMem, 0] == "" || mem[idxMem, 0] == null)
            { btnMemLoad_Click(null, null); return; }

            if (MV == 1)    // save vfo & load memory to psdr
            {
                mvFq = vfoA; mvFl = FltRx1; mvMd = sdrMode;
                btnMemLoad_Click(null, null); 
                MV = 2;
                SetMV("Memory");
                string freq = vfoA.TrimStart('0');
                freq = Convert.ToInt32(freq.Substring(0, freq.Length - 3)).ToString("N0");
                txtMemVfo.Text = freq; 
                mini.txtMemVfo.Text = freq;
            }
            else if (MV == 2)   // reload vfo
            {
                WriteToPort("ZZMD" + mvMd + ";", iSleep); // SDR MODE
                WriteToPort("ZZFA" + mvFq + ";", iSleep); // VFO A
                WriteToPort("ZZFI" + mvFl + ";", iSleep); // RX Filter
                MV = 1;
                SetMV("");
                txtMemVfo.Text = ""; 
                mini.txtMemVfo.Text = "";
            }
        }
        // the memory recall button has been pressed
        public void btnReCall_Click(object sender, EventArgs e)
        {
            idxMem++;
            if (idxMem > 4) idxMem = 0;
            SetMemIdx((idxMem + 1).ToString());
            if (mem[idxMem, 0] == "" || mem[idxMem, 0] == null)
            {
                SetMemFreq("Empty"); SetMemMode(""); return;
            }
            string freq = mem[idxMem, 0].TrimStart('0');
            freq = Convert.ToInt32(freq.Substring(0, freq.Length - 3)).ToString("N0");
            SetMemFreq(freq);
            SetMemMode(mem[idxMem, 10]);
        }
        // the memory save button has been pressed
        public void btnMemSave_Click(object sender, EventArgs e)
        {
            // get current radio status
            //WriteToPort("ZZSW;", iSleep); // TX VFO
            //WriteToPort("ZZMD;", iSleep); // MODE
            //WriteToPort("ZZOA;", iSleep); // RX1 ANT
            //WriteToPort("ZZOB;", iSleep); // RX2 ANT
            //WriteToPort("ZZOC;", iSleep); // TX ANT
            //WriteToPort("ZZOF;", iSleep); // TX LINE
            WriteToPort("ZZTP;", iSleep); // TX Profile
        }
        // the memory clear button has been pressed
        public void btnMemClear_Click(object sender, EventArgs e)
        {
            for (int j = 0; j <= 11; j++)
            {
                mem[idxMem, j] = null;
            }
            SetMemFreq("Empty"); SetMemMode("");
        }
        // if Control + Clear key is pressed delete all memories
        public void btnMemClear_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.Shift)
            {
                for (int i = 0; i <= 4; i++)
                {
                    for (int j = 0; j <= 11; j++)
                    {
                        mem[i, j] = null;
                    }
                }
                idxMem = 0;
                SetMemIdx((idxMem + 1).ToString());
                SetMemFreq("Empty"); SetMemMode("");
            }
        }
        // load memory vars to PSDR
        public void btnMemLoad_Click(object sender, EventArgs e)
        {
            if (idxMem == 5 || mem[idxMem, 0] == "" || mem[idxMem, 0] == null)
            {
                SetMV("");
                wav.SoundLocation = "uh_oh.wav";
                wav.Play();
                return;
            }
            WriteToPort("ZZMD" + mem[idxMem, 3] + ";", iSleep); // SDR MODE
            WriteToPort("ZZFA" + mem[idxMem, 0] + ";", iSleep); // VFO A
            WriteToPort("ZZFB" + mem[idxMem, 1] + ";", iSleep); // VFO B
            WriteToPort("ZZSW" + mem[idxMem, 2] + ";", iSleep); // TX VFO
            WriteToPort("ZZOA" + mem[idxMem, 4] + ";", iSleep); // RX1 ANT
            WriteToPort("ZZOB" + mem[idxMem, 5] + ";", iSleep); // RX2 ANT
            WriteToPort("ZZOC" + mem[idxMem, 6] + ";", iSleep); // TX ANT
            WriteToPort("ZZOF" + mem[idxMem, 7] + ";", iSleep); // TX LINE
            WriteToPort("ZZFI" + mem[idxMem, 8] + ";", iSleep); // RX Filter
            WriteToPort("ZZTP" + mem[idxMem, 9] + ";", iSleep); // TX Profile
        }
        // the freq window was double-clicked
        public void txtMemFreq_DoubleClick(object sender, EventArgs e)
        {
            string value = "";
            if (InputBox("Memory Note", "Enter Memory Note:", ref value) == DialogResult.OK)
            {
               mem[idxMem,11] = value.ToUpper();
            }

        }

        #endregion # Events #

        #region # Methods #

        // opens a input box for storing a note.
        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        #endregion # Methods #

        #endregion Memory

        #region Menu Events

        /*********** Main Menu Bar ********************************************/
        // Main Menu|File|Exit
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
//            set.WindowGeometry = GeometryToString(this);
            set.TabOpen = tabControl.SelectedIndex;
            set.Save();
            closing = true;
            this.Close();
        }
        // Main Menu|Options|Auto Drive
        private void autoDriveToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }
        // Main Menu|Options|Auto Drive | Amp Settings
        private void ampSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(new Form() { TopMost = true },
                "Amp 1\n" +
                "Band  Value\n" +
                "------   -----\n" +
                "160    " + set.pwr1.TrimStart('0') + "\n" +
                "80      " + set.pwr2.TrimStart('0') + "\n" +
                "40      " + set.pwr3.TrimStart('0') + "\n" +
                "30      " + set.pwr4.TrimStart('0') + "\n" +
                "20      " + set.pwr5.TrimStart('0') + "\n" +
                "17      " + set.pwr6.TrimStart('0') + "\n" +
                "15      " + set.pwr7.TrimStart('0') + "\n" +
                "12      " + set.pwr8.TrimStart('0') + "\n" +
                "10      " + set.pwr9.TrimStart('0') + "\n" +
                "6        " + set.pwr10.TrimStart('0') + "\n\n" +
                "Amp 2\n" +
                "Band  Value\n" +
                "------   -----\n" +
                "160    " + set.pwr1_2.TrimStart('0') + "\n" +
                "80      " + set.pwr2_2.TrimStart('0') + "\n" +
                "40      " + set.pwr3_2.TrimStart('0') + "\n" +
                "30      " + set.pwr4_2.TrimStart('0') + "\n" +
                "20      " + set.pwr5_2.TrimStart('0') + "\n" +
                "17      " + set.pwr6_2.TrimStart('0') + "\n" +
                "15      " + set.pwr7_2.TrimStart('0') + "\n" +
                "12      " + set.pwr8_2.TrimStart('0') + "\n" +
                "10      " + set.pwr9_2.TrimStart('0') + "\n" +
                "6        " + set.pwr10_2.TrimStart('0') + "\n", 
                "Auto Drive Values!",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        // Main Menu|Options|Auto Drive | Barefoot settings
        private void barefootSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AcomSwitch auto = new AcomSwitch();
            auto.Show();
        }
        // Main Menu|Options|Tube Amps
        // *** See ACOM and Alpha sections for these events.
        // Main Menu|Options|Acom Ant Default
        private void acomAntDefaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AcomAnt Asw = new AcomAnt();
            Asw.Show();
        }
        // Main Menu|Options|Ant Profile|recall|Profile 1
        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            cboRotorPort.SelectedIndex = set.antPro1RtrPort;
            cboRotorCom.SelectedIndex = set.antPro1RtrCom;
            cboStep.SelectedIndex = set.antPro1StepPort;
            cboStepCom.SelectedIndex = set.antPro1StepCom;
            SetRotorModel(set.antPro1RtrModel);
            SetRotorSpeed(set.antPro1RtrSpeed);
        }
        // Main Menu|Options|Ant Profile|recall|Profile 2
        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            cboRotorPort.SelectedIndex = set.antPro2RtrPort;
            cboRotorCom.SelectedIndex = set.antPro2RtrCom;
            cboStep.SelectedIndex = set.antPro2StepPort;
            cboStepCom.SelectedIndex = set.antPro2StepCom;
            SetRotorModel(set.antPro2RtrModel);
            SetRotorSpeed(set.antPro2RtrSpeed);
        }
        // Main Menu|Options|Ant Profile|Save|Profile 1
        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            set.antPro1RtrPort = set.RotorPort;
            set.antPro1RtrCom = set.RotorCom;
            set.antPro1RtrModel = set.rotorModel;
            set.antPro1RtrSpeed = set.RotorSpeed;
            set.antPro1StepPort = set.StepPortNum;
            set.antPro1StepCom = set.StepCom;
        }
        // Main Menu|Options|Ant Profile|Save|Profile 2
        private void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            set.antPro2RtrPort = set.RotorPort;
            set.antPro2RtrCom = set.RotorCom;
            set.antPro2RtrModel = set.rotorModel;
            set.antPro2RtrSpeed = set.RotorSpeed;
            set.antPro2StepPort = set.StepPortNum;
            set.antPro2StepCom = set.StepCom;
        }
        // Main Menu|Tools|Enable Error Log
        private void enableErrorLoggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (enableErrorLoggingToolStripMenuItem.Checked)
            {
                enableErrorLoggingToolStripMenuItem.Checked = false;
                errorLogToolStripMenuItem.Checked = false;
                enableErrorLog = false;
                set.ErrorLog = false;
            }
            else
            {
                enableErrorLoggingToolStripMenuItem.Checked = true;
                errorLogToolStripMenuItem.Checked = true;
                enableErrorLog = true;
                set.ErrorLog = true;
            }
            set.Save();
        }
        // Main Menu|Tools|Open Error Log
        private void openErrorLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = Application.ExecutablePath;
            path = path.Substring(0, path.LastIndexOf("\\") + 1);
            path = path + "ErrorLog.txt";
            try { Process.Start(path); }
            catch { File.CreateText(path); Process.Start(path); }
        }
        // Main Menu|Tools|Clear Error Log
        private void clearErrorLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = Application.ExecutablePath;
            path = path.Substring(0, path.LastIndexOf("\\") + 1);
            path = path + "ErrorLog.txt";
            File.Delete(path);
            ver = Application.ProductVersion;
            errCtr = 0;
            using (StreamWriter sw = File.CreateText(path))
            {
                sw.WriteLine("DDUtil Error Log");
                sw.WriteLine("----------------");
                sw.WriteLine(" ");
                sw.Flush();
                sw.Close();
            }            
            ErrorLog.LogFilePath = path;
        }
        // Main Menu|Tools|Reset Don't Ask Me Again (All Forms are Reset)
        private void resetDontAskMeAgainToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RegistryKey regKey =
                Registry.CurrentUser.CreateSubKey(@"Software\DDUtil\MsgBoxCheck\RemoteStart");
            regKey.SetValue("DontShowAgain", false);
            regKey = Registry.CurrentUser.CreateSubKey(@"Software\DDUtil\MsgBoxCheck\SO2R");
            regKey.SetValue("DontShowAgain", false);
            regKey = Registry.CurrentUser.CreateSubKey(@"Software\DDUtil\MsgBoxCheck\UpDateAlert");
            regKey.SetValue("DontShowAgain", false);
        }
        // Main Menu|Tools|Start VSP Manager
        private void vSPManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string file = set.vspMgrFile;
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.WorkingDirectory = file.Substring(0, file.Length - 10);
                psi.FileName = file;
                Process myProcess = Process.Start(psi);
            }
            catch
            {
                MessageBox.Show(new Form() { TopMost = true },
                    "There was a problem locating or starting vspMgr.exe!\n\n" +
                    "Please make sure the file location for vspMgr.exe is entered properly\n" +
                    "in the File location window on the 'Other' tab (see tool tips for info).\n" +
                    "or start VSP Manager manually.",
                    "File not found!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        // Main Menu|Wind|Show Alpha 9500 window
        private void alpha9500ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            a95.Show();
        }
        // Main Menu|Wind|Show Mini Window
        private void showMiniWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowMini();
        }
        // Main Menu|Wind|Show Rotor window
        private void rotorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtr.Show();
        }
        // Main Menu|Help|DDUtil Help
        private void dDutilHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("HelpDecoder.htm");
        }
        // Main Menu|Help|OnLine Help
        private void onLineHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://k5fr.com/ddutilwiki/index.php?title=Setup");
        }
        // Main Menu|Help|Get latest version
        private void updateToLatestVersionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string myStringWebResource = null;
            string remoteUri = "http://k5fr.com/binary/";
            string fileName = Application.StartupPath + "\\version2.txt";
            TextReader tr = new StreamReader(fileName);
            string tempStr = tr.ReadLine();
            tr.Close();
            string oldVer = ver.Substring(0, 1) + ver.Substring(2, 1) +
                ver.Substring(4, 1) + ver.Substring(6, 2);
            string newVer = tempStr.Substring(0, 1) + tempStr.Substring(2, 1) + 
                tempStr.Substring(4, 1) + tempStr.Substring(6, 2);
            string upDate = "DDUtil" + newVer + "_Setup.zip";
            // Create a new WebClient instance.
            WebClient myWebClient = new WebClient();
            // Concatenate the domain with the Web resource filename.
            myStringWebResource = remoteUri + upDate;
            // Download the Web resource and save it into the current filesystem folder.
            try
            {
//                if (String.Compare(ver, tempStr) == 0)
                if (Convert.ToInt32(newVer) == Convert.ToInt32(oldVer))
                    {
                        MessageBox.Show(new Form() { TopMost = true }, 
                            "Your current version of DDUtil is up to date!",
                            "DDUtil rev. " + ver);
                }
                else
                {
                    
                    myWebClient.DownloadFile(myStringWebResource, upDate);
                    process = Process.Start(upDate);
                    if (MessageBox.Show(new Form() { TopMost = true },
                        "The Zip file should now be downloaded and open on your desktop.\r\r" +
                        "After dismissing this window, double-click the DDUtil_Setup.msi file \r" +
                        "in the open Zip dialog window to start the update process.\r\r" +
                        "Press Ok to proceed or Cancel to quite", "Attention!", 
                        MessageBoxButtons.OKCancel,MessageBoxIcon.Exclamation) == DialogResult.OK)
                    { this.Close(); }
                }
            }
            catch (WebException w)
            {
                MessageBox.Show(new Form() { TopMost = true },
                    "There has been a problem downloading the new file.\r\r" + w.Message,
                     "Web Site Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        // Main Menu|Help|User Forums
        private void userForumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://groups.yahoo.com/group/DDUtil/");
        }
        // Main Menu|Help|Setup Wizard
        private void setupWizardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetupWiz wiz = new SetupWiz();
            wiz.Show();
        }
        // Main Menu|Help|ShowTips
        private void showTipsToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            TipOfTheDayDialog dlg = new TipOfTheDayDialog();
            dlg.ShowDialog();
        }
        // Main Menu|Help|Web Site
        private void webSiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://k5fr.com/ddutilwiki/index.php");
        }

        /*********** Context Menu ********************************************/
        // Context Menu|Help|About DDUtil
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            AboutBox about = new AboutBox();
            about.Show();
        }
        // Context Menu|Help|Slave Radio Info
        private void slaveRadioInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(new Form() { TopMost = true },
                "Info for selecting a Slave Radio Type\n\n" +
                "- Select Kenwood for:\n" +
                "   All Kenwoods, K2, K3, FT9000, FT2000, FT950, FT450\n\n" +
                "- Select Yaesu Type I for: FT1000(all), FT990, FT920,\n" +
                "   FT900, FT890, FT840, FT757, FT747, FT100\n\n" +
                "- Select Yaesu Type II for: FT897, FT857, FT847, FT817\n\n" +
                "- Select Icom for:\n" +
                "   All Icom radios, TenTec emulating Icom\n",
                "Slave Radio Info", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
        // Context Menu|Help|LP-100
        private void lP100ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(new Form() { TopMost = true },
                "- Select the port the LP-100 is connected to.\n\n" +
                "- Select the polling interval in MS. This needs to\n" +
                "  be fairly fast or the readings will lag behind. \n" +
                "  Try between 100 - 500 MS (200 is a good choice).\n\n" +
                "- Check the Enabled Check Box to complete setup.\n",
                "LP-100 Setup Info", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
        // Context Menu|Help|ICOM IC-PW1
        private void iCOMICPW1ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(new Form() { TopMost = true },
                "- Select the port to which the IC-PW1 is attached.\n\n" +
                "- Select the CI-V (ta) address for DDUtil. This address is\n" +
                "  required to let the IC-PW1 know who is talking to it.\n" +
                "  Use the programmed default of 33 (hex) if unknown.\n\n" +
                "- Check the Enabled Check Box to complete the setup.\n",
                "IC-PW1 Setup Info", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
        // Context Menu|Help|Macro Commands
        private void macroCommandsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(new Form() { TopMost = true },
            "- To execute a macro command, press either the appropriate form button or desiginated 'F' key\n\n" +
            "- To display the Macro Editor screen, click any open spot on the Macro tab.\n\n" +
            "- To close the Macro Editor screen, click in the open spot at the end of the Data File Group\n\n" +
            "- See the documentation (Help Menu) for assistance with editing info and tips.\n\n",
            "Formatting and Using Macro Commands", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
        // Context Menu|Help|RCP ports
        private void rCPPortsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(new Form() { TopMost = true },
                "Setup procedure for using the bi-directional RCP ports\n\n" +
                "These ports are for programs that need to talk to the radio\n" +
                "in order to change frequency, mode and other radio parameters.\n\n" +
                "- Select the desired port from the drop-down list-box.\n\n" +
                "- Check the Enabled check box to turn on a port.\n",
                "RCP Port Setup", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
        // Context Menu|Help|Rotor Control
        private void rotorControlToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(new Form() { TopMost = true },
                "Setup procedure for using Rotor Control\n\n" +
                "- Select the Rotor Model and Speed (if applicable)\n\n" +
                "- Select the desired Serial Port for your rotor.\n\n" +
                "- Select the Serial Port Comm data that matches your rotor.\n\n" +
                "- Check the Enabled check box to turn on a port.\n\n" +
                "- Enter your Latitude and Longitude for your location.\n",
                "Rotor Setup", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
        // Context Menu|Help|WaveNode watt meter
        private void waveNodeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(new Form() { TopMost = true },
                "Setup procedure for using the WN2 watt meter\n\n" +
                "- Select the Coupler and it's type you want to read.\n\n" +
                "- If the hardware becomes inoperative, toggle the Enable check box.\n\n" +
                "- If that fails, press the small purple button in the top right corner.\n\n" +
                "- If that fails, toggle the power to the WN2.\n",
                "WaveNode Operation", MessageBoxButtons.OK, MessageBoxIcon.None);

        }
        // Context Menu|Exit
        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            exitToolStripMenuItem_Click(null, null);
        }
        // Context Menu|Error Log|Enable Logging
        private void enableLoggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableErrorLoggingToolStripMenuItem_Click(null, null);
        }
        // Context Menu|Error Log|Open Error Log
        private void openErrorLogToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            openErrorLogToolStripMenuItem_Click(null, null);
        }
        // Context Menu|Error Log|Clear Error Log
        private void clearErrorLogToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            clearErrorLogToolStripMenuItem_Click(null, null);
        }
        // Context Menu|Auto Drive Enable
        private void enableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (chkAutoDrv.Checked) chkAutoDrv.Checked = false;
            else chkAutoDrv.Checked = true;
        }
        // Context Menu|Auto Drive Amp Settings
        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ampSettingsToolStripMenuItem_Click(null, null);
        }
        // Context Menu|Auto Drive Barefoot settings
        private void barefootSettingsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            barefootSettingsToolStripMenuItem_Click(null, null);
        }
        // Context Menu|Auto Drive|Enable Expert Mode
        private void enableExpertModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (chkAutoExpert.Checked) chkAutoExpert.Checked = false;
            else chkAutoExpert.Checked = true;
        }
        // Context Menu|Help|Short-Cut Keys
        private void shortCutKeysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://k5fr.com/ddutilwiki/index.php?title=Shortcut_Keys");
        }
        // Context Menu|Get latest version
        private void getLatestVersionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            updateToLatestVersionToolStripMenuItem_Click(null, null);
        }

        #endregion Menu Events

        #region Oper/Stby

        // Write text to Oper/Stby button
        delegate void SetOperStbyCallback(string text);
        public void SetOperStby(string text)
        {
            if (this.btnByp.InvokeRequired)
            {
                SetOperStbyCallback d = new SetOperStbyCallback(SetOperStby);
                this.Invoke(d, new object[] { text });
            }
            else
            { btnByp.Text = text; } //mini.btnByp.Text = text; }
        }
        
        // The Oper/Stby button was pressed
        bool noBandsChkd = false;
        bool stsOper;
        string stsTX;
        public void btnByp_Click(object sender, EventArgs e)
        {
            if (!chkAmp160.Checked && !chkAmp80.Checked && !chkAmp40.Checked && 
                 !chkAmp30.Checked && !chkAmp20.Checked && !chkAmp17.Checked &&
                 !chkAmp15.Checked && !chkAmp12.Checked && !chkAmp6.Checked)
            { noBandsChkd = true; }
            else
            { noBandsChkd = false; }

            if (!stsOper)   // if amp in stand by
            {
                if (noBandsChkd)
                {
                    MessageBox.Show(new Form() { TopMost = true },
                        "There are no bands checked in the'Amp Bands' group.\r\r" +
                        "For the 'PTT' feature to work you must check each band you want \r" +
                        "to use your amplifier on in the 'Amp Bands' group.\r\r" +
                        "See http://k5fr.com/ddutilwiki/index.php?title=Setup#PTT for documentation.",
                        "Setup Error!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (rbAmp1.Checked) 
                    WriteToPort("ZZOF100;", 50);  // turn PTT (TX1) on
                else if (rbAmp2.Checked) 
                    if (chk1KeyLine.Checked)
                        WriteToPort("ZZOF100;", 50);  // turn PTT (TX1) on
                    else
                        WriteToPort("ZZOF010;", 50);  // turn PTT (TX2) on

                stsOper = true; btnByp.BackColor = Color.Lime;
                mini.btnByp.BackColor = Color.Lime;
                txtAlcInd.BackColor = Color.Lime;
                SetOperStby("OPER"); 
                set.stsOper = true;
                COBC = lastBand;
            }
            else
            {   // turn PTT (TX) off
//                WriteToPort("ZZOF;", iSleep); // get TX values from PSDR  
                stsOper = false; btnByp.BackColor = Color.Yellow;
                mini.btnByp.BackColor = Color.Yellow;
                WriteToPort("ZZOF000;", 50);  // turn TX off
                txtAlcInd.BackColor = Color.Yellow;
                SetOperStby("STBY"); 
                set.stsOper = false;
            }
            lastBand = "";  // force refresh of drive level.
            DoBandChg(band);
            set.Save();
        }
        // Low power drive activated
        public void LowPower()
        {
            if (stsOper)
            {
                WriteToPort("ZZPC" + set.defLow.ToString().PadLeft(3, '0') + ";", iSleep);
                btnByp.BackColor = Color.Orange;
                mini.btnByp.BackColor = Color.Orange;
                txtAlcInd.BackColor = Color.Orange;
                SetOperStby("OPER");
            }
        }
        // the check all button was pressed
        private void btnCkAll_Click(object sender, EventArgs e)
        {
            chkAmp160.Checked = true; chkAmp80.Checked = true;
            chkAmp40.Checked = true; chkAmp30.Checked = true;
            chkAmp20.Checked = true; chkAmp17.Checked = true;
            chkAmp15.Checked = true; chkAmp12.Checked = true;
            chkAmp10.Checked = true; chkAmp6.Checked = true;
        }
        // the clear all button was pressed
        private void btnClrAll_Click(object sender, EventArgs e)
        {
            chkAmp160.Checked = false; chkAmp80.Checked = false;
            chkAmp40.Checked = false; chkAmp30.Checked = false;
            chkAmp20.Checked = false; chkAmp17.Checked = false;
            chkAmp15.Checked = false; chkAmp12.Checked = false;
            chkAmp10.Checked = false; chkAmp6.Checked = false;
        }
        // the 1 Key Line check box has changed
        private void chk1KeyLine_CheckedChanged(object sender, EventArgs e)
        {
            if (chk1KeyLine.Checked)
            {
                set.chk1KeyLine = true;
                if (stsOper)
                    WriteToPort("ZZOF100;", 50);  // turn PTT (TX1) on
            }
            else
            {
                set.chk1KeyLine = false;
                if (rbAmp1.Checked && stsOper)
                    WriteToPort("ZZOF100;", 50);  // turn PTT (TX1) on
                else if (rbAmp2.Checked && stsOper)
                    WriteToPort("ZZOF010;", 50);  // turn PTT (TX2) on
            }
            set.Save();
        }

        #endregion Oper/Stby

        #region Power Master

        #region CRC Table
        static byte[] crc8revtab = new byte[256] {

                        0x00, 0xB1, 0xD3, 0x62, 0x17, 0xA6, 0xC4, 0x75,
                        0x2E, 0x9F, 0xFD, 0x4C, 0x39, 0x88, 0xEA, 0x5B,
                        0x5C, 0xED, 0x8F, 0x3E, 0x4B, 0xFA, 0x98, 0x29,
                        0x72, 0xC3, 0xA1, 0x10, 0x65, 0xD4, 0xB6, 0x07,
                        0xB8, 0x09, 0x6B, 0xDA, 0xAF, 0x1E, 0x7C, 0xCD,
                        0x96, 0x27, 0x45, 0xF4, 0x81, 0x30, 0x52, 0xE3,
                        0xE4, 0x55, 0x37, 0x86, 0xF3, 0x42, 0x20, 0x91,
                        0xCA, 0x7B, 0x19, 0xA8, 0xDD, 0x6C, 0x0E, 0xBF,
                        0xC1, 0x70, 0x12, 0xA3, 0xD6, 0x67, 0x05, 0xB4,
                        0xEF, 0x5E, 0x3C, 0x8D, 0xF8, 0x49, 0x2B, 0x9A,
                        0x9D, 0x2C, 0x4E, 0xFF, 0x8A, 0x3B, 0x59, 0xE8,
                        0xB3, 0x02, 0x60, 0xD1, 0xA4, 0x15, 0x77, 0xC6,
                        0x79, 0xC8, 0xAA, 0x1B, 0x6E, 0xDF, 0xBD, 0x0C,
                        0x57, 0xE6, 0x84, 0x35, 0x40, 0xF1, 0x93, 0x22,
                        0x25, 0x94, 0xF6, 0x47, 0x32, 0x83, 0xE1, 0x50,
                        0x0B, 0xBA, 0xD8, 0x69, 0x1C, 0xAD, 0xCF, 0x7E,
                        0x33, 0x82, 0xE0, 0x51, 0x24, 0x95, 0xF7, 0x46,
                        0x1D, 0xAC, 0xCE, 0x7F, 0x0A, 0xBB, 0xD9, 0x68,
                        0x6F, 0xDE, 0xBC, 0x0D, 0x78, 0xC9, 0xAB, 0x1A,
                        0x41, 0xF0, 0x92, 0x23, 0x56, 0xE7, 0x85, 0x34,
                        0x8B, 0x3A, 0x58, 0xE9, 0x9C, 0x2D, 0x4F, 0xFE,
                        0xA5, 0x14, 0x76, 0xC7, 0xB2, 0x03, 0x61, 0xD0,
                        0xD7, 0x66, 0x04, 0xB5, 0xC0, 0x71, 0x13, 0xA2,
                        0xF9, 0x48, 0x2A, 0x9B, 0xEE, 0x5F, 0x3D, 0x8C,
                        0xF2, 0x43, 0x21, 0x90, 0xE5, 0x54, 0x36, 0x87,
                        0xDC, 0x6D, 0x0F, 0xBE, 0xCB, 0x7A, 0x18, 0xA9,
                        0xAE, 0x1F, 0x7D, 0xCC, 0xB9, 0x08, 0x6A, 0xDB,
                        0x80, 0x31, 0x53, 0xE2, 0x97, 0x26, 0x44, 0xF5,
                        0x4A, 0xFB, 0x99, 0x28, 0x5D, 0xEC, 0x8E, 0x3F,
                        0x64, 0xD5, 0xB7, 0x06, 0x73, 0xC2, 0xA0, 0x11,
                        0x16, 0xA7, 0xC5, 0x74, 0x01, 0xB0, 0xD2, 0x63,
                        0x38, 0x89, 0xEB, 0x5A, 0x2F, 0x9E, 0xFC, 0x4D };
        #endregion CRC Table

        #region # Delegates #

        // Write to Fwd Trim window
        delegate void SetTrimFwdCallback(decimal num);
        public void SetTrimFwd(decimal num)
        {
            if (this.numFwd.InvokeRequired)
            {
                SetTrimFwdCallback d = new SetTrimFwdCallback(SetTrimFwd);
                this.Invoke(d, new object[] { num });
            }
            else
            { numFwd.Value = num; }
        }
        // Write to Rev Trim window
        delegate void SetTrimRevCallback(decimal num);
        public void SetTrimRev(decimal num)
        {
            if (this.numRev.InvokeRequired)
            {
                SetTrimRevCallback d = new SetTrimRevCallback(SetTrimRev);
                this.Invoke(d, new object[] { num });
            }
            else
            { numRev.Value = num; }
        }


        #endregion Delegates

        #region # Methods #

        void PMSetup()
        {
            cboPMcom.SelectedIndex = set.PMcom;
            cboPMport.SelectedIndex = set.PMport;
            chkPM.Checked = set.chkPM;
        }
        // calc crc for inbuff and write cmd to port
        void PMportWrite(byte[] inBuf)
        {
            byte stx = 02; byte etx = 03;
            byte[] cBuf = new byte[2];                  // crc ascii buffer
            byte crc = CRC8Fast(inBuf, inBuf.Length);   // get crc
            string hcrc = crc.ToString("X");            // crc to hex
            // save crc in ascii buffer
            for (int i = 0; i < 2; i++)
            {
                int j = 0;
                if (hcrc.Length == 1) { cBuf[0] = 0x30; i++; j = 0; }
                else j = i;
                if (hcrc.Substring(j, 1) == "A") { cBuf[i] = 0x41; continue; }
                if (hcrc.Substring(j, 1) == "B") { cBuf[i] = 0x42; continue; }
                if (hcrc.Substring(j, 1) == "C") { cBuf[i] = 0x43; continue; }
                if (hcrc.Substring(j, 1) == "D") { cBuf[i] = 0x44; continue; }
                if (hcrc.Substring(j, 1) == "E") { cBuf[i] = 0x45; continue; }
                if (hcrc.Substring(j, 1) == "F") { cBuf[i] = 0x46; continue; }

                int temp = Convert.ToInt32(hcrc.Substring(i, 1));
                temp += 30;
                cBuf[i] = byte.Parse(temp.ToString(), NumberStyles.HexNumber);
            }
            // assemble the output buffer
            if (inBuf.Length == 1)
            {
                byte[] outbuf = { stx, inBuf[0], etx, cBuf[0], cBuf[1], 0x0d };
                PMport.Write(outbuf, 0, 6);
            }
            else if (inBuf.Length == 2)
            {
                byte[] outbuf = { stx, inBuf[0], inBuf[1], etx, cBuf[0], cBuf[1], 0x0d };
                PMport.Write(outbuf, 0, 7);
            }
            else if (inBuf.Length == 3)
            {
                byte[] outbuf = { stx, inBuf[0], inBuf[1], inBuf[2], etx, cBuf[0], cBuf[1], 0x0d };
                PMport.Write(outbuf, 0, 8);
            }
            else if (inBuf.Length == 4)
            {
                byte[] outbuf = { stx, inBuf[0], inBuf[1], inBuf[2], inBuf[3], etx, cBuf[0], cBuf[1], 0x0d };
                PMport.Write(outbuf, 0, 9);
            }
        }
        // Calc CRC values for cmds sent to PM
        public static byte CRC8Fast(byte[] buf, int len)
        {
            byte crc = 0;  /* prevent corruption with global variables */
            for (int i = 0; i < len; i++)
            {
                crc = crc8revtab[crc ^ buf[i]];
            }
            return ((byte)~crc);   /* complimented CRC used to prevent string of 0's */
        }

        #endregion Methods

        #region # Events #

        string sPM = "";        // PM port buffer message
        // a Message(s) from the PM has been received
        private void PMport_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (chkPM.Checked)  // port must be enabled to receive data
            {
                try
                {
                    string fwd = ""; string swr = "";
                    string sCmd = "";
                    SerialPort port = (SerialPort)sender;
                    byte[] data = new byte[port.BytesToRead];
                    port.Read(data, 0, data.Length);
                    sPM += AE.GetString(data, 0, data.Length);
                    Regex rex = new Regex(@".*?\r\n");
                    for (Match m = rex.Match(sPM); m.Success; m = m.NextMatch())
                    {   //loop thru the buffer and find matches
                        sCmd = m.Value.Substring(0, m.Value.Length - 1);
                        sPM = sPM.Replace(m.Value, ""); //remove match from buffer

                        if (sCmd.Substring(1, 1) == "D")
                        {
                            fwd = sCmd.Substring(3, 7); swr = sCmd.Substring(19, 5);
                            SetAvg(Regex.Replace(fwd, @"\s*(\d+.\d)", "$1"));
                            mini.SetAvg(Regex.Replace(fwd, @"\s*(\d+.\d)", "$1"));
                            SetSwr(Regex.Replace(swr, @"\s*(\d+.\d{2})", "$1"));
                            mini.SetSwr(Regex.Replace(swr, @"\s*(\d+.\d{2})", "$1"));
                        }
                        else if (sCmd.Substring(1, 1) == "T")
                        { SetTrimFwd(Convert.ToDecimal(sCmd.Substring(2, 3))); }
                        else if (sCmd.Substring(1, 1) == "t")
                        { SetTrimRev(Convert.ToDecimal(sCmd.Substring(2, 3))); }
                    }
                }
                catch
                { }
            }
        }
        // The PM enable check box has changed
        private void chkPM_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPM.Checked)
            {
                if (cboPMport.SelectedIndex > 0)
                {
                    set.chkPM = true; chkWNEnab.Checked = false;
                    lblAvg.Text = "Fwd"; chkLPenab.Checked = false;
                    txtAvg.Enabled = true; txtSWR.Enabled = true;
                    mini.txtAvg.Enabled = true; mini.txtSWR.Enabled = true;
                    txtFwd.Visible = false; lblFwd.Visible = false;
                    mini.txtFwd.Visible = false; mini.lblFwd.Visible = false;
                    mini.lblAvg.Text = "Fwd";
                    PMport.Write("\x02\x54\x3F\x03\x38\x45\r"); // Get fwd trim
                    Thread.Sleep(150);
                    PMport.Write("\x02\x74\x3F\x03\x37\x37\r"); // Get swr trim
                    Thread.Sleep(150);
                    PMport.Write("\x02\x44\x34\x03\x36\x36\r"); // Start data broadcast
                    if (set.StdNet == 1) rbStd.Checked = true;
                    else if (set.StdNet == 2) rbNet.Checked = true;
                }
                else
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "No port has been selected for the Power Master.\n\n" +
                        "Please select a valid port number and try again.", "Port Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    chkPM.Checked = false; set.chkPM = false;
                    txtAvg.Text = ""; txtFwd.Text = ""; txtSWR.Text = "";
                    mini.txtAvg.Text = ""; mini.txtFwd.Text = ""; mini.txtSWR.Text = "";
                    //txtAvg.Enabled = false; 
                    txtFwd.Enabled = false; 
                    //txtSWR.Enabled = false;
                    //mini.txtAvg.Enabled = false; 
                    mini.txtFwd.Enabled = false;
                    //mini.txtSWR.Enabled = false;
                    if (PMport.IsOpen)
                    { PMport.Write("\x02\x44\x30\x03\x37\x31\r"); }// Stop data broadcast
                    cboPMport.SelectedIndex = 0;
                }
            }
            else
            {
                set.chkPM = false;
                txtAvg.Text = ""; txtFwd.Text = ""; txtSWR.Text = "";
                mini.txtAvg.Text = ""; mini.txtFwd.Text = ""; mini.txtSWR.Text = "";
                //txtAvg.Enabled = false; 
                txtFwd.Enabled = false; 
                //txtSWR.Enabled = false;
                //mini.txtAvg.Enabled = false; 
                mini.txtFwd.Enabled = false;
                //mini.txtSWR.Enabled = false;
                if (PMport.IsOpen)
                { PMport.Write("\x02\x44\x30\x03\x37\x31\r"); }// Stop data broadcast
            }
            set.Save();
        }
        // The PM port has changed
        private void cboPMport_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PMport.IsOpen) PMport.Close();
            if (cboPMport.SelectedIndex > 0)
            {
                PMport.PortName = cboPMport.SelectedItem.ToString();
                try
                {
                    PMport.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                       "The Power Master serial port " + PMport.PortName +
                       " cannot be opened!\n", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    chkPM.Checked = false;
                    cboPMport.SelectedIndex = 0;
                    set.PMport = 0;
                    set.Save();
                    return;
                }
            }
            else
            {
                cboPMport.SelectedIndex = 0; 
                chkPM.Checked = false;
            }
            set.PMport = cboPMport.SelectedIndex;
            set.Save();
        }
        // The PM port comm setting has changed
        private void cboPMcom_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cboPMcom.SelectedIndex)
            {
                case 0: // 38400 8N1
                    PMport.BaudRate = 38400;
                    PMport.DataBits = 8;
                    PMport.Parity = System.IO.Ports.Parity.None;
                    PMport.StopBits = System.IO.Ports.StopBits.One;
                    break;
                case 1: // 19200 8N1
                    PMport.BaudRate = 19200;
                    PMport.DataBits = 8;
                    PMport.Parity = System.IO.Ports.Parity.None;
                    PMport.StopBits = System.IO.Ports.StopBits.One;
                    break;
                case 2: // 9600 8N1
                    PMport.BaudRate = 9600;
                    PMport.DataBits = 8;
                    PMport.Parity = System.IO.Ports.Parity.None;
                    PMport.StopBits = System.IO.Ports.StopBits.One;
                    break;
            }
            set.PMcom = cboPMcom.SelectedIndex; set.Save();
        }
        // The Fwd Trim has changed
        private void numFwd_ValueChanged(object sender, EventArgs e)
        {
            string fwd = numFwd.Value.ToString();
            int ctr = fwd.Length;
            byte[] inBuf = new byte[4];
            inBuf[0] = 0x54; // "T"
            switch (ctr)
            {
                case 1:
                    inBuf[1] = 0x30; inBuf[2] = 0x30;
                    inBuf[3] = byte.Parse("3" + fwd.Substring(0, 1), NumberStyles.HexNumber);
                    break;

                case 2:
                    if (fwd.Substring(0, 1) == "-")
                    {
                        inBuf[1] = 0x2d; inBuf[2] = 0x30;
                        inBuf[3] = byte.Parse("3" + fwd.Substring(1, 1), NumberStyles.HexNumber);
                    }
                    else
                    {
                        inBuf[1] = 0x30;
                        inBuf[2] = byte.Parse("3" + fwd.Substring(0, 1), NumberStyles.HexNumber);
                        inBuf[3] = byte.Parse("3" + fwd.Substring(1, 1), NumberStyles.HexNumber);
                    }
                    break;

                case 3:
                    if (fwd.Substring(0, 1) == "-")
                    {
                        inBuf[1] = 0x2d;
                        inBuf[2] = byte.Parse("3" + fwd.Substring(1, 1), NumberStyles.HexNumber);
                        inBuf[3] = byte.Parse("3" + fwd.Substring(2, 1), NumberStyles.HexNumber);
                    }
                    else
                    {
                        inBuf[1] = byte.Parse("3" + fwd.Substring(0, 1), NumberStyles.HexNumber);
                        inBuf[2] = byte.Parse("3" + fwd.Substring(1, 1), NumberStyles.HexNumber);
                        inBuf[3] = byte.Parse("3" + fwd.Substring(2, 1), NumberStyles.HexNumber);
                    }
                    break;
            }
            PMportWrite(inBuf);
        }
        // The Swr Trim has changed
        private void numRev_ValueChanged(object sender, EventArgs e)
        {
            string swr = numRev.Value.ToString();
            int ctr = swr.Length;
            byte[] inBuf = new byte[4];
            inBuf[0] = 0x74; // "t"
            switch (ctr)
            {
                case 1:
                    inBuf[1] = 0x30; inBuf[2] = 0x30;
                    inBuf[3] = byte.Parse("3" + swr.Substring(0, 1), NumberStyles.HexNumber);
                    break;

                case 2:
                    if (swr.Substring(0, 1) == "-")
                    {
                        inBuf[1] = 0x2d; inBuf[2] = 0x30;
                        inBuf[3] = byte.Parse("3" + swr.Substring(1, 1), NumberStyles.HexNumber);
                    }
                    else
                    {
                        inBuf[1] = 0x30;
                        inBuf[2] = byte.Parse("3" + swr.Substring(0, 1), NumberStyles.HexNumber);
                        inBuf[3] = byte.Parse("3" + swr.Substring(1, 1), NumberStyles.HexNumber);
                    }
                    break;

                case 3:
                    if (swr.Substring(0, 1) == "-")
                    {
                        inBuf[1] = 0x2d;
                        inBuf[2] = byte.Parse("3" + swr.Substring(1, 1), NumberStyles.HexNumber);
                        inBuf[3] = byte.Parse("3" + swr.Substring(2, 1), NumberStyles.HexNumber);
                    }
                    else
                    {
                        inBuf[1] = byte.Parse("3" + swr.Substring(0, 1), NumberStyles.HexNumber);
                        inBuf[2] = byte.Parse("3" + swr.Substring(1, 1), NumberStyles.HexNumber);
                        inBuf[3] = byte.Parse("3" + swr.Substring(2, 1), NumberStyles.HexNumber);
                    }
                    break;
            }
            PMportWrite(inBuf);
        }
        // The PM Std/Net radio buttons have changed
        private void grpNet_CheckedChanged(object sender, EventArgs e)
        {
            if (rbStd.Checked)
            { PMport.Write("\x02\x64\x31\x03\x33\x39\r"); set.StdNet = 1; }

            if (rbNet.Checked)
            { PMport.Write("\x02\x64\x30\x03\x38\x38\r"); set.StdNet = 2; }

            set.Save();
        }

        #endregion Events

        #endregion Power Master

        #region PSDR PowerOn

        Process process; 
        private ProgressDialog pd;
        private uint progressPercent;
        bool PSDRrunning = false;
        //the flex power button was pressed
        public void btnFlexOn_Click(object sender, EventArgs e)
        {
            MsgBoxCheck.MessageBox dlg = new MsgBoxCheck.MessageBox();
            DialogResult dr = dlg.Show(@"Software\DDUtil\MsgBoxCheck\RemoteStart", "DontShowAgain",
                "Don't ask me this again",
                "CAUTION! Please understand what you are about to do!\r\r" +
                "Continuing will either power up the radio if it is off or\r" +
                "power down the radio if it is on.\r\r" +
                "Additionally, PowerSDR will be started or stopped depending\r" +
                "on the state of the radio condition.\r\r" +
                "See the DDUtil documentation (help menu) for more information.\r\r" +
                "Press Ok to continue or Cancel to abort this operation",
                "Warning!", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
            if (dr == DialogResult.OK)
            {
                PSDRrunning = IsPSDRrunning();  // see if PSR is running
                if (chkPSDR.Checked)
                {
                    pd = new ProgressDialog(this.Handle);
                    pd.Title = "PowerSDR Event";
                    pd.CancelMessage = "Please wait while the operation is cancelled";
                    pd.Maximum = 100;
                    pd.Value = 0;
                    if (!PSDRrunning)
                    {
                        // toggle RTS
                        PwrPort.RtsEnable = true;
                        Thread.Sleep(500); // 1/2 second pulse width
                        PwrPort.RtsEnable = false;

                        pd.Line1 = "Please standby while the radio initializes.";
                        pd.Line3 = "Calculating Time Remaining...";
                        pd.ShowDialog(ProgressDialog.PROGDLG.Modal, ProgressDialog.PROGDLG.AutoTime,
                            ProgressDialog.PROGDLG.NoTime, ProgressDialog.PROGDLG.NoMinimize);
                        progressPercent = 0;
                        pdTimer.Start();
                    }
                    else
                    {
                        StartPSDR();
                    }
                }
                else
                {
                    PwrPort.RtsEnable = true;
                    Thread.Sleep(500); // 1/2 second pulse width
                    PwrPort.RtsEnable = false;
                }
            }
        }
        // start or stop PSDR
        private void StartPSDR()
        {
            if (!PSDRrunning)
            {
                process = Process.Start(txtPSDR.Text);
                PSDRrunning = true;
            }
            else
            {
                try
                {   // close PSDR and wait for PSDR to close
                    process.CloseMainWindow();
                    process.WaitForExit();
                    Thread.Sleep(2500);
                    //WriteToPort("ZZBY;", 2500);

                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true },
                    "Aborting Auto Shutdown Sequence!\r\r" +
                    "PowerSDR failed to close properly.\r" +
                    "Please close PowerSDR and THEN click Ok.",
                    "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                // turn off radio
                PwrPort.RtsEnable = true;
                Thread.Sleep(500); // 1/2 second pulse width
                PwrPort.RtsEnable = false;
                PSDRrunning = false;
            }
        }
        // ProgressDialog timer elapsed
        private void pdTimer_Tick(object sender, EventArgs e)
        {
            progressPercent++;

            if (pd.HasUserCancelled)
            {
                pdTimer.Stop();
                pd.CloseDialog();
            }
            else
            {
                // Update the progress value
                pd.Value = progressPercent;

                pd.Line2 = "Percent " + progressPercent.ToString() + "%";

                if (progressPercent >= 100)
                {
                    pdTimer.Stop();
                    pd.CloseDialog();
                    StartPSDR();
                }
            }
        }
        // the chkPSDR check box has changed
        private void chkPSDR_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPSDR.Checked) set.StartPSDR = true;
            else set.StartPSDR = false;
            set.Save();
        }
        // See if PSDR is running
        public static bool IsPSDRrunning()
        {
            bool IsRunning = false;
            string proc = "PowerSDR";
            Process[] processes = Process.GetProcessesByName(proc);
            if (processes.Length > 0)
            {
                IsRunning = true;
            }
            else
            {
                IsRunning = false;
            }
            return IsRunning;
        }
        //the Flex power-on port has changed
        private void cboPwrPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PwrPort.IsOpen) PwrPort.Close();
            if (cboPwrPort.SelectedIndex > 0)
            {
                PwrPort.PortName = cboPwrPort.SelectedItem.ToString();
                try
                {
                    PwrPort.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true },
                       "The PowerOn serial port " + PwrPort.PortName +
                       " cannot be opened!\n", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboPwrPort.SelectedIndex = 0;
                    set.PwrPort = 0;
                    set.Save();
                    return;
                }
            }
            else
            {
                cboPwrPort.SelectedIndex = 0;
            }
            // save new port setting
            set.PwrPort = cboPwrPort.SelectedIndex;
            set.Save();
        }
        // the PSDR file location has changed
        private void txtPSDR_TextChanged(object sender, EventArgs e)
        {
            set.PwrSDRfile = txtPSDR.Text; set.Save();
        }
        //the PSDR file location textbox was double-clicked
        private void txtPSDR_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = "C:\\Program Files\\FlexRadio Systems\\";
            openFileDialog1.Filter = "exe files|*.exe";
            openFileDialog1.Title = "Select PowerSDR File Location";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtPSDR.Text = openFileDialog1.FileName;
                //mini.PFfile = txtProfLoc.Text;
                set.PSDRloc = txtPSDR.Text;
                set.Save();
            }
            openFileDialog1.Dispose();
        }

        #endregion PSDR PowerOn

        #region Profiles

        #region # Events #

        // the save profile menu item was selected
        private void profileSaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveProfile();
        }
        // the load profile menu item was selected
        private void profileLoadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadProfile();
        }
        
        #endregion # Events #

        #region # Methods #

        // get a list of control values
        private ArrayList GetCtrlVal()
        {
            // enumerate controls and save to array list
            ArrayList a = new ArrayList();
            foreach (Control c in this.Controls)	// save only the following types
            {
                // if it is a groupbox or panel, check for sub controls
                if (c.GetType() == typeof(TabControl))
                {
                    foreach (Control c2 in c.Controls)
                    {
                        if (c2.GetType() == typeof(TabPage))
                        {
                            foreach (Control c3 in c2.Controls)
                            {
                                if (c3.GetType() == typeof(GroupBox) || c.GetType() == typeof(Panel))
                                {
                                    foreach (Control c4 in c3.Controls)
                                    {
                                        if (c4.GetType() == typeof(GroupBox))
                                        {
                                            foreach (Control c5 in c4.Controls)
                                            {
                                                if (c5.Enabled)
                                                {
                                                    if (c5.GetType() == typeof(CheckBox))
                                                        a.Add(c5.Name + "/" + ((CheckBox)c5).Checked.ToString());
                                                    else if (c5.GetType() == typeof(TextBox))
                                                    {
                                                        //string xx = ((TextBox)c5).Text;
                                                        //if (xx == "" || xx == null)
                                                        //    ((TextBox)c5).Text = "0";
                                                        a.Add(c5.Name + "/" + ((TextBox)c5).Text);
                                                    }
                                                    else if (c5.GetType() == typeof(ComboBox))
                                                        a.Add(c5.Name + "/" + ((ComboBox)c5).Text);
                                                    else if (c5.GetType() == typeof(RadioButton))
                                                        a.Add(c5.Name + "/" + ((RadioButton)c5).Checked.ToString());
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (c4.Enabled)
                                            {
                                                if (c4.GetType() == typeof(CheckBox))
                                                    a.Add(c4.Name + "/" + ((CheckBox)c4).Checked.ToString());
                                                else if (c4.GetType() == typeof(TextBox))
                                                {
                                                    //string xx = ((TextBox)c4).Text;
                                                    //if (xx == "" || xx == null)
                                                    //    ((TextBox)c4).Text = "0";
                                                    a.Add(c4.Name + "/" + ((TextBox)c4).Text);
                                                }
                                                else if (c4.GetType() == typeof(ComboBox))
                                                    a.Add(c4.Name + "/" + ((ComboBox)c4).Text);
                                                else if (c4.GetType() == typeof(RadioButton))
                                                    a.Add(c4.Name + "/" + ((RadioButton)c4).Checked.ToString());
                                            }

                                        }
                                    }
                                }
                                else
                                {
                                    if (c3.Enabled)
                                    {
                                        if (c3.GetType() == typeof(CheckBox))
                                            a.Add(c3.Name + "/" + ((CheckBox)c3).Checked.ToString());
                                        else if (c3.GetType() == typeof(TextBox))
                                        {
                                            //string xx = ((TextBox)c3).Text;
                                            //if (xx == "" || xx == null)
                                            //    ((TextBox)c3).Text = "0";
                                            a.Add(c3.Name + "/" + ((TextBox)c3).Text);
                                        }
                                        else if (c3.GetType() == typeof(ComboBox))
                                            a.Add(c3.Name + "/" + ((ComboBox)c3).Text);
                                        else if (c3.GetType() == typeof(RadioButton))
                                            a.Add(c3.Name + "/" + ((RadioButton)c3).Checked.ToString());
                                    }

                                }

                            }
                        }
                    }
                }
                else
                {   // if not a Tab Control
                    if (c.Enabled)
                    {
                        if (c.GetType() == typeof(CheckBox))
                            a.Add(c.Name + "/" + ((CheckBox)c).Checked.ToString());
                        else if (c.GetType() == typeof(TextBox))
                        {
                            string xx = ((TextBox)c).Text;
                            if (xx == "" || xx == null)
                                ((TextBox)c).Text = "0";
                            a.Add(c.Name + "/" + ((TextBox)c).Text);
                        }
                        else if (c.GetType() == typeof(ComboBox))
                            a.Add(c.Name + "/" + ((ComboBox)c).SelectedIndex.ToString());
                        else if (c.GetType() == typeof(RadioButton))
                            a.Add(c.Name + "/" + ((RadioButton)c).Checked.ToString());
                    }
                }
            }
            return a;
        }
        // load profile from a file
        void LoadProfile()
        {
            try
            {
//                throw new ArgumentNullException();

                ArrayList chk_list = new ArrayList();
                ArrayList cbo_list = new ArrayList();
                ArrayList rb_list = new ArrayList();
                ArrayList txt_list = new ArrayList();

                foreach (Control c in this.Controls)	// save only the following types
                {
                    // if it is a groupbox or panel, check for sub controls
                    if (c.GetType() == typeof(TabControl))
                    {
                        foreach (Control c2 in c.Controls)
                        {
                            if (c2.GetType() == typeof(TabPage))
                            {
                                foreach (Control c3 in c2.Controls)
                                {
                                    if (c3.GetType() == typeof(GroupBox) || c.GetType() == typeof(Panel))
                                    {
                                        foreach (Control c4 in c3.Controls)
                                        {
                                            if (c4.GetType() == typeof(GroupBox))
                                            {
                                                foreach (Control c5 in c4.Controls)
                                                {
                                                    if (c5.Enabled)
                                                    {
                                                        if (c5.GetType() == typeof(CheckBox))
                                                            chk_list.Add(c5);
                                                        else if (c5.GetType() == typeof(TextBox))
                                                        {
                                                            txt_list.Add(c5);
                                                        }
                                                        else if (c5.GetType() == typeof(ComboBox))
                                                            cbo_list.Add(c5);
                                                        else if (c5.GetType() == typeof(RadioButton))
                                                            rb_list.Add(c5);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (c4.Enabled)
                                                {
                                                    if (c4.GetType() == typeof(CheckBox))
                                                        chk_list.Add(c4);
                                                    else if (c4.GetType() == typeof(TextBox))
                                                    {
                                                        txt_list.Add(c4);
                                                    }
                                                    else if (c4.GetType() == typeof(ComboBox))
                                                        cbo_list.Add(c4);
                                                    else if (c4.GetType() == typeof(RadioButton))
                                                        rb_list.Add(c4);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (c3.Enabled)
                                        {
                                            if (c3.GetType() == typeof(CheckBox))
                                                chk_list.Add(c3);
                                            else if (c3.GetType() == typeof(TextBox))
                                            {
                                                txt_list.Add(c3);
                                            }
                                            else if (c3.GetType() == typeof(ComboBox))
                                                cbo_list.Add(c3);
                                            else if (c3.GetType() == typeof(RadioButton))
                                                rb_list.Add(c3);
                                        }

                                    }

                                }
                            }
                        }
                    }
                    else
                    {   // if not a Tab Control
                        if (c.Enabled)
                        {
                            if (c.GetType() == typeof(CheckBox))
                                chk_list.Add(c);
                            else if (c.GetType() == typeof(TextBox))
                            {
                                txt_list.Add(c);
                            }
                            else if (c.GetType() == typeof(ComboBox))
                                cbo_list.Add(c);
                            else if (c.GetType() == typeof(RadioButton))
                                rb_list.Add(c);
                        }
                    }
                }
                // get the App Data Path
                if (app_data_path == "")
                {
                    AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    + "\\DDUtil\\";
                }
                string pfileDir = app_data_path + "Profiles\\";
                if (!Directory.Exists(pfileDir))
                    Directory.CreateDirectory(pfileDir);

                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = pfileDir;
                ofd.Filter = "xml files|*.xml";
                ofd.Title = "Select a Profile to load";
                string pFile = "";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    pFile = ofd.FileName;
                    ofd.Dispose();
                }
                else
                {
                    ofd.Dispose();
                    return;
                }              
                if (pFile == null || pFile == "") return;

                dso = new DataSet();
                if (File.Exists(pFile)) dso.ReadXml(pFile);

                ArrayList a = GetVars("Profile");	// Get the saved list of controls
                a.Sort();
                int num_controls = chk_list.Count + cbo_list.Count + rb_list.Count + txt_list.Count;

                foreach (string s in a)				// string is in the format "name,value"
                {
                    string[] vals = s.Split('/');
                    string name = vals[0];
                    string val = vals[1];

                    if (s.StartsWith("chk"))	// control is a CheckBox
                    {
                        for (int i = 0; i < chk_list.Count; i++)
                        {	// look through each control to find the matching name
                            CheckBox c = (CheckBox)chk_list[i];
                            if (c.Name.Equals(name))		// name found
                            {
                                c.Checked = bool.Parse(val);	// restore value
                                i = chk_list.Count + 1;
                            }
                            if (i == chk_list.Count)
                                MessageBox.Show(new Form() { TopMost = true },
                                    "Control not found: " + name, "GetVars Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else if (s.StartsWith("cbo"))			// control is a ComboBox
                    {
                        for (int i = 0; i < cbo_list.Count; i++)
                        {	// look through each control to find the matching name
                            ComboBox c = (ComboBox)cbo_list[i];
                            if (c.Name.Equals(name))		// name found
                            {
                                c.Text = val;	// restore value
                                i = cbo_list.Count + 1;
                            }
                            if (i == cbo_list.Count)
                                MessageBox.Show(new Form() { TopMost = true },
                                    "Control not found: " + name, "GetVars Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    if (s.StartsWith("rb"))			// control is a CheckBox
                    {
                        for (int i = 0; i < rb_list.Count; i++)
                        {	// look through each control to find the matching name
                            RadioButton c = (RadioButton)rb_list[i];
                            if (c.Name.Equals(name))		// name found
                            {
                                c.Checked = bool.Parse(val);	// restore value
                                i = rb_list.Count + 1;
                            }
                            if (i == rb_list.Count)
                                MessageBox.Show(new Form() { TopMost = true },
                                    "Control not found: " + name, "GetVars Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else if (s.StartsWith("txt"))
                    {	// look through each control to find the matching name
                        for (int i = 0; i < txt_list.Count; i++)
                        {
                            TextBox c = (TextBox)txt_list[i];
                            if (c.Name.Equals(name))		// name found
                            {
                                c.Text = val;	// restore value
                                i = txt_list.Count + 1;
                            }
                            if (i == txt_list.Count)
                                MessageBox.Show(new Form() { TopMost = true },
                                    "Control not found: " + name, "GetVars Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                    "Unable to write to log");
            }
        }
        // save profile
        void SaveProfile()
        {
            try
            {
                // get the App Data Path
                if (app_data_path == "")
                {
                    AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    + "\\DDUtil\\";
                }
                string pfileDir = app_data_path + "Profiles\\";
                if (!Directory.Exists(pfileDir))
                    Directory.CreateDirectory(pfileDir);

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.InitialDirectory = pfileDir;
                sfd.Filter = "xml files|*.xml";
                sfd.Title = "Select or enter a Profile to Save";
                string pFile = "";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    pFile = sfd.FileName;
                    sfd.Dispose();
                }
                else
                {
                    sfd.Dispose();
                    return;
                }
                if (pFile == null || pFile == "") return;
                dso = new DataSet();

                if (File.Exists(pFile)) File.Delete(pFile);

                // enumerate controls and save to array list
                ArrayList a = new ArrayList();
                a = GetCtrlVal();

                // save the array list to a DB table
                //SaveVars("Profile", ref a);
                if (!dso.Tables.Contains("Profile"))
                    AddFormTable("Profile");

                foreach (string s in a)
                {
                    string[] vals = s.Split('/');
                    if (vals.Length > 2)
                    {
                        for (int i = 2; i < vals.Length; i++)
                            vals[1] += "/" + vals[i];
                    }
                    //dso.Clear(); 
                    DataRow[] rows = dso.Tables["Profile"].Select("Key = '" + vals[0] + "'");
                    if (rows.Length == 0)	// name is not in list
                    {
                        DataRow newRow = dso.Tables["Profile"].NewRow();
                        newRow[0] = vals[0];
                        newRow[1] = vals[1];
                        dso.Tables["Profile"].Rows.Add(newRow);
                    }
                    else if (rows.Length == 1)
                    {
                        rows[0][1] = vals[1];
                    }
                }
                // write DB table to xml file
                dso.WriteXml(pFile);
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                    "Unable to write to log");
            }
        }
        
        #endregion # Methods #

        #endregion Profiles

        #region Propadex
        // Propadex forecast UI
        private void pixBox1_Click(object sender, EventArgs e)
        {
            Process.Start("http://qsonet.com/propadex.html");
        }

        void propTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                pixBox1.Load();
            }
            catch { }
        }

        #endregion Propadex

        #region Repeater

        //setup repeater
        void SetupRepeater()
        {
            cboRepeatCom.SelectedIndex = set.RepeatCom;
            cboRepeatPort.SelectedIndex = set.RepeatPort;
            chkRepeat.Checked = set.RepeatEnab;
            switch (set.RptMode)  // which format?
            {
                case 0: rbNorm.Checked = true; break;
                case 1: rbStnMstr.Checked = true; break;
                case 2: rbPal.Checked = true; break;
                default: break;
            }
        }
        // the baud rate has changed
        private void cboRepeatCom_SelectedIndexChanged(object sender, EventArgs e)
        {
            RepeatPort.BaudRate = Convert.ToInt32(cboRepeatCom.SelectedItem);
            set.RepeatCom = cboRepeatCom.SelectedIndex;
            set.Save();
        }
        // one of the radio buttons has changed
        private void grpRmode_CheckedChanged(object sender, EventArgs e)
        {
            if (rbNorm.Checked) { set.RptMode = 0; }
            else if (rbStnMstr.Checked) { set.RptMode = 1; }
            else if (rbPal.Checked) { set.RptMode = 2; }
            set.Save();
        }
        // The Repeat Enable check box has changed
        private void chkRepeat_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRepeat.Checked)
            {
                if (cboRepeatPort.SelectedIndex > 0)
                {
                    set.RepeatEnab = true;
                    cboRepeatCom.SelectedIndex = set.RepeatCom;
                    switch (set.RptMode)
                    {
                        case 0: rbNorm.Checked = true; break;
                        case 1: rbStnMstr.Checked = true; break;
                        case 2: rbPal.Checked = true; break;
                    }
                }
                else
                {
                    chkRepeat.Checked = false;
                    MessageBox.Show(new Form() { TopMost = true }, 
                       "No port has been selected for the Repeater.\n\n" +
                       "Please select a valid port number and try again.", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    set.RepeatEnab = false;
                    cboRepeatPort.SelectedIndex = 0;
                }
            }
            else
            {
                chkRepeat.Checked = false;
                set.RepeatEnab = false;
            }
            set.Save();
        }
        // The Repeater port selection has changed
        private void cboRepeatPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RepeatPort.IsOpen) RepeatPort.Close();
            if (cboRepeatPort.SelectedIndex > 0)
            {
                RepeatPort.PortName = cboRepeatPort.SelectedItem.ToString();
                try
                {
                    RepeatPort.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true },
                        "The Repeater serial port " + RepeatPort.PortName +
                        " cannot be opened!\n", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboRepeatPort.SelectedIndex = 0;
                    chkRepeat.Checked = false;
                    set.RepeatPort = 0;
                    set.Save();
                    return;
                }
            }
            else
            {
                cboRepeatPort.SelectedIndex = 0; 
                chkRepeat.Checked = false;
            }
            set.RepeatPort = cboRepeatPort.SelectedIndex;
            set.Save();
        }
        // the Repeater Port has received data
        string sRepeat = "";
        private void RepeatPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (chkRepeat.Checked) // port must be enabled to accept data
            {
                try
                {
                    string sCmd = "";
                    SerialPort port = (SerialPort)sender;
                    byte[] data = new byte[port.BytesToRead];
                    port.Read(data, 0, data.Length);
                    sRepeat += AE.GetString(data, 0, data.Length);
                    Regex rex = new Regex(".*?;");				//accept any string ending in ;		
                    for (Match m = rex.Match(sRepeat); m.Success; m = m.NextMatch())
                    {   //loop thru the buffer and find matches
                        sCmd = m.Value.Substring(0, m.Value.Length - 1);
                        sRepeat = sRepeat.Replace(m.Value, ""); //remove the match from the buffer

                        string state="";
                        // If Station Master console installed use different IF reply
                        if (rbStnMstr.Checked)
                            state = State["_SMIF"].ToString();
                        else
                        {
                            state = CatParse(RepeatPort, sCmd);
                            if (state == null)
                                break;
                        }
                        RepeatPort.Write(sCmd + state + ";");
                    }
                }
                catch (Exception ex)
                {
                    bool bReturnLog = false;
                    bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                    if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                        "Unable to write to log");
                }
            }
        }

        #endregion Repeater

        #region Rotor Control

        #region # Rotor Events #

        // the rotor reps timer has elasped
        private void RtrReps_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (RepsCtr != 0)
            { RotorPort.Write(rtrCmd); RepsCtr -= 1; }
            else
            { RepsTimer.Enabled = false; }
        }

        string rtrCmd = "";
        string sRtrBuf = "";
        string lastPos = "";
        string rtrSpd = "";
        string suffix = ""; // termination string for Rotor models data out
        string pre = "";
        string ent = "";
        int RepsCtr = 0;
        const int posReps = 3;
        private void RotorPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string sCmd = "";
                string regxVar;
                if (rotormod == RotorMod.AlphaSpid) regxVar = @"W.*?\x20";
                else if (rotormod == RotorMod.GreenHeron) regxVar = ".*?;";
                else if (rotormod == RotorMod.Hygain) regxVar = @";\d\d\d"; 
                else if (rotormod == RotorMod.M2R2800PA) regxVar = @".*?\n\r";
                else if (rotormod == RotorMod.M2R2800PX) regxVar = @".*?\r";
                else if (rotormod == RotorMod.Yaesu) regxVar = @"\x2b0.*?\d\d\d";
                else if (rotormod == RotorMod.Prosistel)
                    regxVar = @".*?\r"; //@"\x02A,?,\d{3},.,\r";  // 02 41 2C 3F 2C 31 35 37 2C B 2C 0D
                else regxVar = "";
                SerialPort port = (SerialPort)sender;
                byte[] data = new byte[port.BytesToRead];
                port.Read(data, 0, data.Length);
                sRtrBuf += AE.GetString(data, 0, data.Length);
                Regex rex = new Regex(regxVar);
                //loop thru the buffer and find matches
                for (Match m = rex.Match(sRtrBuf); m.Success; m = m.NextMatch())
                {
                    sCmd = m.Value;
                    //remove the match from the buffer
                    sRtrBuf = sRtrBuf.Replace(m.Value, "");

                    switch (rotormod)
                    {   // see which rotor we're using
                        case RotorMod.AlphaSpid:
                            int brg = (data[1] * 100) + (data[2] * 10) + data[3];
                            brg -= 360;
                            if (brg.ToString() != lastPos)
                            {
                                RotorPort.Write(                  //request position
                                    "W\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x1f\x20");
                                SetRotor(brg.ToString());         //print new heading
                                lastPos = brg.ToString();         //save position
                                rtrCmd =                          // save new heading cmd
                                    "W\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x1f\x20";
                                RepsCtr = posReps;                // # reps to run timer
                                RepsTimer.Enabled = true;         // enable reps timer
                            }
                            break;
                        case RotorMod.GreenHeron:
                            sCmd = sCmd.Substring(0, sCmd.Length - 1); // loose the ';'
                            if (sCmd != lastPos)
                            {
                                if (chkTenths.Checked) rtrCmd = "BI1;";
                                else rtrCmd = "AI1;";
                                RotorPort.Write(rtrCmd); //request position
                                SetRotor(sCmd);          //print new heading
                                lastPos = sCmd;          //save position
                                RepsCtr = posReps;
                                RepsTimer.Enabled = true;
                            }
                            break;
                        case RotorMod.Hygain:
                            sCmd = sCmd.Substring(1, sCmd.Length - 1);
                            if (sCmd != lastPos)
                            {// if heading chgd, print new heading, 
                                SetRotor(sCmd.TrimStart('0'));
                                lastPos = sCmd;
                                RotorPort.Write("AI1;"); //request position
                                rtrCmd = "AI1;";
                                RepsCtr = posReps;
                                RepsTimer.Enabled = true;
                            }
                            break;
                        case RotorMod.M2R2800PA:
                            SetRotor(sCmd.Substring(5, 5));
                            break;
                        case RotorMod.M2R2800PX:
                            SetRotor(sCmd.Substring(2, 5));
                            break;
                        case RotorMod.Prosistel:
                            if (sCmd.Substring(3, 1) != "?") return;
                            SetRotor(sCmd.Substring(5, 3));
                            lastPos = sCmd.Substring(5, 3);
                            //int pbrg = (data[5] * 100) + (data[6] * 10) + data[7];
                            //if (pbrg.ToString() != lastPos)
                            //{
                            //    SetRotor(pbrg.ToString());
                            //    lastPos = pbrg.ToString();
                            //}
                            break;
                        case RotorMod.Yaesu:
                            sCmd = sCmd.Substring(2, 3);
                            if (sCmd != lastPos)
                            {// if heading chgd, print new heading, 
                                RotorPort.Write("C\r"); //request position
                                SetRotor(sCmd);
                                lastPos = sCmd;
                                rtrCmd = "C\r";
                                RepsCtr = posReps;
                                RepsTimer.Enabled = true;
                            }
                            break;
                        default: break;
                    } //switch (rotormod)
                } //For match
            } // try
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
            }
        } //Rotor port data received
        // the rotor stop button was pressed
        private void btnStop_Click(object sender, EventArgs e)
        {
            RotorStop();
        }
        // Stop the rotor if moving (Ctrl+LP button)
        private void btnLP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control) { lblLP.Focus(); RotorStop(); e.Handled = true; }
        }
        // Stop the rotor if moving (Ctrl+SP button)
        private void btnSP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control) { lblSP.Focus(); RotorStop(); }
        }
        // Stops the Rotor if turning
        public void RotorStop()
        {
            float currentSize;
            switch (rotormod)
            {
                case RotorMod.AlphaSpid:
                    RotorPort.Write("W\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x0f\x20");
                    break;
                case RotorMod.GreenHeron:
                    RotorPort.Write("\x3B");   //";"
                    break;
                case RotorMod.Hygain:
                    RotorPort.Write("\x3B");   //";"
                    break;
                case RotorMod.M2R2800PA:
                    RotorPort.Write("\x53\r");  //"S,CR"
                    break;
                case RotorMod.M2R2800PX:
                    RotorPort.Write("\x53\r");  //"S,CR"
                    break;
                case RotorMod.Prosistel:
                    RotorPort.Write("\x02\x41\x47\x39\x37\x37\r"); // G977 = soft stop
                    break;
                case RotorMod.Yaesu:
                    RotorPort.Write("\x53\r");  //"S,CR"
                    break;
                default: break;
            }
            Notification alert = new Notification();
            Notification.notiIntvl = 2000;
            Notification.notiMsg = "Stopping Antenna Rotation!\r";
            currentSize = alert.label1.Font.Size;
            currentSize += 2.0F;
            alert.label1.Font = new Font(alert.label1.Font.Name, currentSize,
            alert.label1.Font.Style, alert.label1.Font.Unit);
            alert.Show();
        }
        // the check steppir dipole check box has changed
        private void chkStepDipole_CheckedChanged(object sender, EventArgs e)
        {
            if (chkStepDipole.Checked)
            { set.chkStepDipole = true; }
            else
            { set.chkStepDipole = false; }
            set.Save();
        }
        // The Short Path Rotor button has been pressed
        private void btnSP_Click(object sender, EventArgs e)
        {
            if (chkRotorEnab.Checked)
            {
                string txt = txtSP.Text.PadLeft(3, '0');
                if (String.Compare(lastMove.TrimStart(), txt) !=0 )
                {
                    lblSP.Text = txtSP.Text; lblLP.Text = "SP";
                    TurnRotor(txtSP.Text); lblSP.Focus();
                    rbPre1.Checked = false;
                    rbPre2.Checked = false;
                    rbPre3.Checked = false;
                    rbPre4.Checked = false;
                    rbPre5.Checked = false;
                    rbPre6.Checked = false;
                    rbPre7.Checked = false;
                    rbPre8.Checked = false;
                    rbPre9.Checked = false;
                    rbPre10.Checked = false;
                    rbPre11.Checked = false;
                    rbPre12.Checked = false;
                    rbPre13.Checked = false;
                    rbPre14.Checked = false;
                    rbPre15.Checked = false;
                    rbPre16.Checked = false;
                }
            }
        }
        // The SP bearing window has changed, must be a manual entry.
        private void txtSP_TextChanged(object sender, EventArgs e)
        {
            try
            {
                int bearing = Convert.ToInt32(txtSP.Text);
                if (bearing < 180)
                    txtLP.Text = Convert.ToInt32(bearing + 180).ToString();
                else
                    txtLP.Text = Convert.ToInt32(bearing - 180).ToString();
            }
            catch { }
        }
        // The Long Path Rotor button has been pressed
        private void btnLP_Click(object sender, EventArgs e)
        {
            if (chkRotorEnab.Checked)
            {
                lblSP.Text = txtLP.Text; lblLP.Text = "LP";
                TurnRotor(txtLP.Text); lblLP.Focus();
            }
        }
        // The Rotor Port number has changed
        private void cboRotorPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RotorPort.IsOpen) RotorPort.Close();
            if (cboRotorPort.SelectedIndex > 0)
            {
                RotorPort.PortName = cboRotorPort.SelectedItem.ToString();
                try
                {
                    RotorPort.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "The Rotor serial port " + RotorPort.PortName +
                        " cannot be opened!\n", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboRotorPort.SelectedIndex = 0;
                    chkRotorEnab.Checked = false;
                    set.RotorPort = 0;
                    set.Save();
                    return;
                }
            }
            else
            {
                cboRotorPort.SelectedIndex = 0; 
                chkRotorEnab.Checked = false;
            }
            // save new port setting
            set.RotorPort = cboRotorPort.SelectedIndex;
            set.Save();
        }
        // The Rotor port com parameters have changed
        private void cboRotorCom_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cboRotorCom.SelectedIndex)
            {
                case 0: // 9600 8N1
                    RotorPort.BaudRate = 9600;
                    RotorPort.DataBits = 8;
                    RotorPort.Parity = System.IO.Ports.Parity.None;
                    RotorPort.StopBits = System.IO.Ports.StopBits.One;
                    break;
                case 1: // 9600 8N2
                    RotorPort.BaudRate = 9600;
                    RotorPort.DataBits = 8;
                    RotorPort.Parity = System.IO.Ports.Parity.None;
                    RotorPort.StopBits = System.IO.Ports.StopBits.Two;
                    break;
                case 2: // 4800 8N1
                    RotorPort.BaudRate = 4800;
                    RotorPort.DataBits = 8;
                    RotorPort.Parity = System.IO.Ports.Parity.None;
                    RotorPort.StopBits = System.IO.Ports.StopBits.One;
                    break;
                case 3: // 4800 8N2
                    RotorPort.BaudRate = 4800;
                    RotorPort.DataBits = 8;
                    RotorPort.Parity = System.IO.Ports.Parity.None;
                    RotorPort.StopBits = System.IO.Ports.StopBits.Two;
                    break;
                case 4: // 2400 8N1
                    RotorPort.BaudRate = 2400;
                    RotorPort.DataBits = 8;
                    RotorPort.Parity = System.IO.Ports.Parity.None;
                    RotorPort.StopBits = System.IO.Ports.StopBits.One;
                    break;
                case 5: // 2400 8N2
                    RotorPort.BaudRate = 2400;
                    RotorPort.DataBits = 8;
                    RotorPort.Parity = System.IO.Ports.Parity.None;
                    RotorPort.StopBits = System.IO.Ports.StopBits.Two;
                    break;
                case 6: // 1200 8N1
                    RotorPort.BaudRate = 1200;
                    RotorPort.DataBits = 8;
                    RotorPort.Parity = System.IO.Ports.Parity.None;
                    RotorPort.StopBits = System.IO.Ports.StopBits.One;
                    break;
                case 7: // 1200 8N2
                    RotorPort.BaudRate = 1200;
                    RotorPort.DataBits = 8;
                    RotorPort.Parity = System.IO.Ports.Parity.None;
                    RotorPort.StopBits = System.IO.Ports.StopBits.Two;
                    break;
                case 8: // 600 8N1
                    RotorPort.BaudRate = 600;
                    RotorPort.DataBits = 8;
                    RotorPort.Parity = System.IO.Ports.Parity.None;
                    RotorPort.StopBits = System.IO.Ports.StopBits.One;
                    break;
                default:
                    break;
            }
            set.RotorCom = (int)cboRotorCom.SelectedIndex;
            set.Save();
        }
        // The Enable Rotor Check Box has changed
        private void chkRotorEnab_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRotorEnab.Checked && RotorPort.IsOpen)
            {
                btnSP.Enabled = true;
                btnLP.Enabled = true;
                cboRCP1Rotor.Enabled = true;
                cboRCP2Rotor.Enabled = true;
                cboRCP3Rotor.Enabled = true;
                cboRCP4Rotor.Enabled = true;
                cboRCP5Rotor.Enabled = true;
                grpRtrHead.Enabled = true;
                grpPreset.Enabled = true;
                rtr.grpRtrHead.Enabled = true;
                rtr.grpPreset.Enabled = true;
                if (cboRotorPort.SelectedIndex > 0)
                {
                    set.RotorEnab = true;
                    RotorPort.DiscardInBuffer();
                    RotorPort.DiscardOutBuffer();
                    grpModel_CheckedChanged(null, null);
                }
                else
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "No port has been selected for the Rotor.\n\n" +
                        "Please select a valid port number and try again.", "Port Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    chkRotorEnab.Checked = false;
                    cboRotorPort.SelectedIndex = 0;
                }
            }
            else
            {
                set.RotorEnab = false;
                btnSP.Enabled = false;
                btnLP.Enabled = false;
                cboRCP1Rotor.Enabled = false;
                cboRCP2Rotor.Enabled = false;
                cboRCP3Rotor.Enabled = false;
                cboRCP4Rotor.Enabled = false;
                cboRCP5Rotor.Enabled = false;
                grpRtrHead.Enabled = false;
                grpPreset.Enabled = false;
                rtr.grpRtrHead.Enabled = false;
                rtr.grpPreset.Enabled = false;
            }
            set.Save();
        }
        // the Rotor Model selection has changed
        private void grpModel_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRotorEnab.Checked)
            {
                if (rbRtrMod1.Checked)
                {
                    set.rotorModel = 0;
                    rotormod = RotorMod.AlphaSpid;
                    grpSpeed.Visible = false;
                    suffix = "/ "; // 0x2F, 0x20
                    if (chkRotorEnab.Checked) RotorPort.Write(
                        "W\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x1f\x20");
                    chkTenths.Visible = false;
                }
                else if (rbRtrMod2.Checked)
                {
                    if (chkTenths.Checked) rtrCmd = "BI1;";
                    else rtrCmd = "AI1;";
                    set.rotorModel = 1;
                    rotormod = RotorMod.GreenHeron;
                    grpSpeed.Visible = false;
                    suffix = ";";
                    if (chkRotorEnab.Checked) RotorPort.Write(rtrCmd);
                    chkTenths.Visible = true;
                }
                else if (rbRtrMod3.Checked)
                {
                    set.rotorModel = 2;
                    rotormod = RotorMod.Hygain;
                    grpSpeed.Visible = false;
                    suffix = ";";
                    if (chkRotorEnab.Checked) RotorPort.Write("AI1;");
                    chkTenths.Visible = false;
                }
                else if (rbRtrMod4.Checked)
                {
                    set.rotorModel = 3;
                    rotormod = RotorMod.M2R2800PA;
                    grpSpeed.Visible = true;
                    suffix = "\r";
                    if (chkRotorEnab.Checked) RotorPort.Write("U\r");
                    chkTenths.Visible = false;
                }
                else if (rbRtrMod5.Checked)
                {
                    set.rotorModel = 4;
                    rotormod = RotorMod.M2R2800PX;
                    grpSpeed.Visible = true;
                    suffix = "\r";
                    chkTenths.Visible = false;
                }
                else if (rbRtrMod6.Checked)
                {
                    set.rotorModel = 5;
                    rotormod = RotorMod.Prosistel;
                    grpSpeed.Visible = false;
                    chkTenths.Visible = false;
                    suffix = "\r";
                    //put rotor in CPM mode
                    RotorPort.Write("\x02\x41\x4d\r");
                    Thread.Sleep(100);
                    // request position
                    RotorPort.Write("\x02\x41\x3f\r");
                }
                else if (rbRtrMod7.Checked)
                {
                    set.rotorModel = 6; rotormod = RotorMod.Yaesu;
                    grpSpeed.Visible = true;
                    suffix = "\r";
                    if (chkRotorEnab.Checked) RotorPort.Write("C\r");
                    chkTenths.Visible = false;
                }
                set.Save();
            }
        }
        // the rotor speed selection has changed
        private void grpSpeed_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRotorEnab.Checked)
            {
                if (sender == rbRtrSpd1) { set.RotorSpeed = 0; rtrSpd = "1"; }
                else if (sender == rbRtrSpd2) { set.RotorSpeed = 1; rtrSpd = "5"; }
                else if (sender == rbRtrSpd3) { set.RotorSpeed = 2; rtrSpd = "9"; }
                else return;
                set.Save();
                if (rotormod == RotorMod.M2R2800PA) RotorPort.Write("S" + rtrSpd + "\r");
                if (rotormod == RotorMod.M2R2800PX) RotorPort.Write("S" + rtrSpd + "\r");
                if (rotormod == RotorMod.Yaesu) RotorPort.Write("X" + rtrSpd + "\r");
            }
        }
        private void chkTenths_CheckedChanged(object sender, EventArgs e)
        {
            if (chkTenths.Checked)
            {
                set.Tenths = true;
                //                RotorPort.ReceivedBytesThreshold = 5;
            }
            else
            {
                set.Tenths = false;
                //               RotorPort.ReceivedBytesThreshold = 4;
            }
            set.Save();
        }
        // *** Prefix Combo Box Events ***
        private void cboPrefix_SelectedIndexChanged(object sender, EventArgs e)
        { GetPrefix(); }
        private void cboPrefix_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                GetPrefix(); pre = "";
            }
        }
        private void cboPrefix_KeyPress(object sender, KeyPressEventArgs e)
        {
            pre += e.KeyChar;
            if (pre == "\r" || pre == "") { pre = ""; goto Done; }
            string SQL = "SELECT * FROM DX WHERE DXCCPrefix LIKE '" +
                            pre + "%' ORDER BY DXCCprefix";
            GetDXCC(SQL);
        Done: e.Handled = true; cboPrefix.Focus();
        }
        private void cboPrefix_Click(object sender, EventArgs e)
        { cboPrefix.DroppedDown = true; pre = ""; }
        private void cboPrefix_Enter(object sender, EventArgs e)
        { pre = ""; }
        private void cboPrefix_Leave(object sender, EventArgs e)
        { GetPrefix(); pre = ""; }
        private void cboPrefix_DropDown(object sender, EventArgs e)
        {
            txtCallSign.Text = "";
        }
        // *** Entity Combo Box Events ***
        private void cboEntity_SelectedIndexChanged(object sender, EventArgs e)
        { GetEntity(); }
        private void cboEntity_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                GetEntity(); ent = "";
            }
        }
        private void cboEntity_KeyPress(object sender, KeyPressEventArgs e)
        {
            ent += e.KeyChar;
            if (ent == "\r" || ent == "") { ent = ""; goto Done; }
            string SQL = "SELECT * FROM DX WHERE DXCCName LIKE '" +
                            ent + "%' ORDER BY DXCCName";
            GetDXCC(SQL);
        Done: e.Handled = true; cboEntity.Focus();
        }
        private void cboEntity_Click(object sender, EventArgs e)
        { cboEntity.DroppedDown = true; ent = ""; }
        private void cboEntity_Enter(object sender, EventArgs e)
        { pre = ""; }
        private void cboEntity_Leave(object sender, EventArgs e)
        { GetEntity(); ent = ""; }
        private void cboEntity_DropDown(object sender, EventArgs e)
        {
            txtCallSign.Text = "";
        }
        // Country Code lookup has been requested
        private void txtCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string SQL = "SELECT * FROM DX WHERE CountryCode = " +
                            Convert.ToInt32(txtCode.Text);
                txtCode.Text = "";
                txtCallSign.Text = "";
                GetDXCC(SQL);
            }
        }
        // IOTA lookup is requested
        private void txtDxIOTA_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string SQL = "SELECT * FROM DX WHERE IOTA = '" +
                            txtDxIOTA.Text + "'";
                txtDxIOTA.Text = "";
                txtCallSign.Text = "";
                GetDXCC(SQL);
            }
        }
        // The distance box was double-clicked, get the reverse distance
        private void txtDxDist_DoubleClick(object sender, EventArgs e)
        {
            txtDxDist.Text = (24902 - Convert.ToInt32(txtDxDist.Text)).ToString();
            if (txtDxDist.ForeColor == Color.Blue) txtDxDist.ForeColor = Color.Firebrick;
            else txtDxDist.ForeColor = Color.Blue;
        }
        // One of the rotor presets has been pressed
        private void grpPreset_CheckChanged(object sender, EventArgs e)
        {
            if (rbPre1.Checked)
            { txtSP.Text = rbPre1.Text; btnSP_Click(null, null); }
            else if (rbPre2.Checked)
            { txtSP.Text = rbPre2.Text; btnSP_Click(null, null); }
            else if (rbPre3.Checked)
            { txtSP.Text = rbPre3.Text; btnSP_Click(null, null); }
            else if (rbPre4.Checked)
            { txtSP.Text = rbPre4.Text; btnSP_Click(null, null); }
            else if (rbPre5.Checked)
            { txtSP.Text = rbPre5.Text; btnSP_Click(null, null); }
            else if (rbPre6.Checked)
            { txtSP.Text = rbPre6.Text; btnSP_Click(null, null); }
            else if (rbPre7.Checked)
            { txtSP.Text = rbPre7.Text; btnSP_Click(null, null); }
            else if (rbPre8.Checked)
            { txtSP.Text = rbPre8.Text; btnSP_Click(null, null); }
            else if (rbPre9.Checked)
            { txtSP.Text = rbPre9.Text; btnSP_Click(null, null); }
            else if (rbPre10.Checked)
            { txtSP.Text = rbPre10.Text; btnSP_Click(null, null); }
            else if (rbPre11.Checked)
            { txtSP.Text = rbPre11.Text; btnSP_Click(null, null); }
            else if (rbPre12.Checked)
            { txtSP.Text = rbPre12.Text; btnSP_Click(null, null); }
            else if (rbPre13.Checked)
            { txtSP.Text = rbPre13.Text; btnSP_Click(null, null); }
            else if (rbPre14.Checked)
            { txtSP.Text = rbPre14.Text; btnSP_Click(null, null); }
            else if (rbPre15.Checked)
            { txtSP.Text = rbPre15.Text; btnSP_Click(null, null); }
            else if (rbPre16.Checked)
            { txtSP.Text = rbPre16.Text; btnSP_Click(null, null); }
        }
        // Calc & Display grid to grid heading
        private void txtLoc_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                txtSP.Text = String.Format("{0:n0}",
                    MaidenheadLocator.Azimuth(txtGrid.Text, txtLoc.Text));

                txtKm.Text = String.Format("{0:n0}",
                    MaidenheadLocator.Distance(txtGrid.Text, txtLoc.Text)) + " km";

                txtSm.Text = String.Format("{0:n0}", Convert.ToInt32(
                    MaidenheadLocator.Distance(txtGrid.Text, txtLoc.Text)) * .621371) + " mi";

                MaidenheadLocator.LatLong ll = MaidenheadLocator.LocatorToLatLong(txtLoc.Text);
                txtDxLat.Text = String.Format("{0:f4}", ll.Lat);
                txtDxLong.Text = String.Format("{0:f4}", ll.Long);

                txtLP.Text = ""; lblLP.Text = ""; txtCallSign.Text = "";
                cboPrefix.Text = ""; cboEntity.Text = "";
                txtCode.Text = ""; txtRegion.Text = ""; txtDxDist.Text = ""; txtDxCont.Text = "";
                txtDxCQ.Text = ""; txtDxITU.Text = ""; txtDxIOTA.Text = ""; txtDxTime.Text = "";
                lblDxTime.Text = "Coords for Grid Square";
            }
            catch
            { lblSP.Text = "Error"; txtSP.Text = ""; txtLP.Text = ""; lblLP.Text = ""; }

        }
        // Catch the Enter key if pressed
        private void txtLoc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                txtLoc_DoubleClick(null, null);
            }
        }
        // the call sign text box was double clicked
        private void txtCallSign_DoubleClick(object sender, EventArgs e)
        {
            txtCallSign.Text = "";
        }
        // prefix lookup was requested.
        private void txtCallSign_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (txtCallSign.Text != "" && txtCallSign.Text != null)
                {
                    try
                    {
                        string SQL = "SELECT Prefix.Prefix, DX.DXCCPrefix " +
                            "FROM DX INNER JOIN Prefix ON DX.DXCCPrefix = Prefix.DXCCPrefix " +
                            "WHERE Prefix.Prefix Like '" + txtCallSign.Text + "%'";

                        string path = Application.ExecutablePath;
                        path = path.Substring(0, path.LastIndexOf("\\") + 1);
                        OleDbConnection conn = new OleDbConnection(
                            "provider = microsoft.jet.oledb.4.0;data source = " + path + "DDUtil.mdb;");
                        OleDbCommand thisCommand = new OleDbCommand(SQL, conn);
                        conn.Open();
                        OleDbDataReader thisReader = thisCommand.ExecuteReader();
                        if (thisReader.Read())
                        {
                            cboPrefix.Text = thisReader.GetValue(1).ToString();
                        }
                        else
                        {
                            txtLP.Text = ""; lblLP.Text = "";
                            cboPrefix.Text = "??"; cboEntity.Text = "";
                            txtDxLat.Text = ""; txtDxLong.Text = "";
                            txtCode.Text = ""; txtRegion.Text = "";
                            txtDxDist.Text = ""; txtDxCont.Text = "";
                            txtDxCQ.Text = ""; txtDxITU.Text = "";
                            txtDxIOTA.Text = ""; txtDxTime.Text = "";
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        MessageBox.Show(new Form() { TopMost = true },
                            "The Call Prefix requires at least two (2) characters to be valid prefix.\r\r" +
                            "Please click Ok and re-enter a prefix.", "Imput Error!",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                        txtLP.Text = ""; lblLP.Text = "";
                        cboPrefix.Text = ""; cboEntity.Text = "";
                        txtCode.Text = ""; txtRegion.Text = "";
                        txtDxLat.Text = ""; txtDxLong.Text = "";
                        txtDxDist.Text = ""; txtDxCont.Text = "";
                        txtDxCQ.Text = ""; txtDxITU.Text = "";
                        txtDxIOTA.Text = ""; txtDxTime.Text = "";
                    }
                    catch { }
                }
            }
        }

        #endregion Rotor Events

        #region # Rotor Methods #

        //Set Rotor Speed
        void SetRotorSpeed(int Speed)
        {
            switch (Speed)
            {
                case 0: rbRtrSpd1.Checked = true; break;
                case 1: rbRtrSpd2.Checked = true; break;
                case 2: rbRtrSpd3.Checked = true; break;
                default: break;
            }
        }
        void SetRotorModel(int model)
        {
            switch (model)
            {   // set model & send rotor initialization if required
                case 0: rbRtrMod1.Checked = true; break;  // Alpha Spid
                case 1: rbRtrMod2.Checked = true; break;  // Green Heron
                case 2: rbRtrMod3.Checked = true; break;  // HyGain

                case 3: rbRtrMod4.Checked = true;         // M2RC2800A-P
                    if (cboRotorPort.SelectedIndex > 0 && chkRotorEnab.Checked)
                        RotorPort.Write("S" + rtrSpd + "\r");
                    break;
                case 4: rbRtrMod5.Checked = true;         // M2RC2800AX
                    if (cboRotorPort.SelectedIndex > 0 && chkRotorEnab.Checked)
                        RotorPort.Write("S" + rtrSpd + "\r");
                    break;
                case 5: rbRtrMod6.Checked = true;         // Prosistel
                    if (cboRotorPort.SelectedIndex > 0 && chkRotorEnab.Checked)
                    {
                        RotorPort.Write("\x02\x41WENA_PWM=1\r"); // setup soft start/stop
                        Thread.Sleep(100);
                        RotorPort.Write("\x02\x41\x4D\r");        // set to CPM mode
                        Thread.Sleep(100);
                        RotorPort.Write("\x02\x41\x3f\r");        // request position
                    }
                    break;
                case 6: rbRtrMod7.Checked = true;         // Yaesu
                    if (cboRotorPort.SelectedIndex > 0 && chkRotorEnab.Checked)
                        RotorPort.Write("X" + rtrSpd + "\r");
                    break;
                default: break;
            }
        }
        // Toggle the rotor enable check box
        public void ToggleRtrEnab()
        {
            if (chkRotorEnab.Checked)
            {
                chkRotorEnab.Checked = false;
                chkRotorEnab.Checked = true;
            }
        }
        // Turn the rotor
        string lastMove = "0";
        public void TurnRotor(string heading)
        {
            int head = Convert.ToInt32(heading);
            int dipAzHead;
            // If using SteppIR dipole on 80 mtrs
            if (chkStepDipole.Checked && band == "080")
            {
                dipAzHead = head;
                if (dipAzHead + 90 >= 180)
                { dipAzHead -= 90; }
                else
                { dipAzHead += 90; }
                head = dipAzHead;
                if (head >= 180 && Convert.ToInt32(lastMove) < 180)
                    head -= 180;
                else if (head < 180 && Convert.ToInt32(lastMove) >= 180)
                    head += 180;
            }
            int off = Convert.ToInt32(txtAzOffset.Text);
            int offsign = Math.Sign(off);
            string newhead = "";
            switch (offsign)
            {
                case -1:    // - offset
                    if (head + off < 0) // 5 + -10 = -5
                        newhead = (head - off).ToString();
                    else if (head - off > 360)  // 355 + 10 = 375
                        newhead = ((head - off) - 360).ToString();
                    else
                        newhead = (head - off).ToString();
                    break;
                case 0:     // no offset
                    newhead = head.ToString();
                    break;
                case 1:     // + offset
                    if (head + off > 360)
                        newhead = (head - off).ToString();
                    else if (head - off < 0)
                        newhead = (360 + (head - off)).ToString();
                    else if (head == off)
                        newhead = "0";
                    else
                        newhead = (head + off).ToString();
                    break;
                default: break;
            }
            switch (rotormod)
            {
                case RotorMod.AlphaSpid:
                    int circle = 360;
                    int bearing = Convert.ToInt32(newhead);
                    bearing = bearing + circle;
                    RotorPort.Write("W" + bearing.ToString() + "0\x01     \x2f ");
                    RotorPort.Write(
                        "W\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x1f\x20");    // request position
                    rtrCmd = "W\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x1f\x20"; // save new position cmd
                    RepsCtr = posReps;
                    RepsTimer.Enabled = true;
                    break;
                case RotorMod.GreenHeron:
                    if (newhead.Length < 3) newhead = newhead.PadLeft(3, '0');
                    RotorPort.Write("AP1" + newhead + "\r;"); // start rotor
                    //                        rtrCmd = "BI1;";
                    RotorPort.Write(rtrCmd);                  // request position
                    RepsCtr = posReps;
                    RepsTimer.Enabled = true;
                    break;
                case RotorMod.Hygain:
                    if (newhead.Length < 3) newhead = newhead.PadLeft(3, '0');
                    RotorPort.Write("AP1" + newhead + ";"); // set newhead
                    RotorPort.Write("AM1;");                // start rotor
                    RotorPort.Write("AI1;");
                    //                        Thread.Sleep(100);
                    rtrCmd = "AI1;";
                    RepsCtr = posReps;
                    RepsTimer.Enabled = true;
                    break;
                case RotorMod.M2R2800PA:
                    RotorPort.Write("S\r");                 // stop rotor
                    RotorPort.Write(newhead + "\r");        // start rotor
                    RotorPort.Write("U\r");                 // request position
                    break;
                case RotorMod.M2R2800PX:
                    RotorPort.Write("S\r");                 // stop rotor
                    RotorPort.Write("A" + newhead + "\r");  // start rotor
//                    RotorPort.Write("U\r");                 // request position
                    break;
                case RotorMod.Prosistel:
                    RotorPort.Write("\x02\x41\x47" + newhead + "\r");
                    break;
                case RotorMod.Yaesu:
                    if (newhead.Length < 3) newhead = newhead.PadLeft(3, '0');
                    RotorPort.Write("M" + newhead + "\r");  // start rotor
                    RotorPort.Write("C\r");                 // request position
                    rtrCmd = "C\r";
                    RepsCtr = posReps;
                    RepsTimer.Enabled = true;
                    break;
                default: break;
            }
            lastMove = head.ToString().PadLeft(3, '0');
            //            }
        }
        // Get DXCC data from the database
        private void GetDXCC(string SQL)
        {
            string path = Application.ExecutablePath;
            path = path.Substring(0, path.LastIndexOf("\\") + 1);
            OleDbConnection conn = new OleDbConnection(
                "provider = microsoft.jet.oledb.4.0;data source = " + path + "DDUtil.mdb;");
            OleDbCommand thisCommand = new OleDbCommand(SQL, conn);
            conn.Open();
            OleDbDataReader thisReader = thisCommand.ExecuteReader();
            try
            {
                int i = 1;
                while (thisReader.Read() && i > 0)
                {
                    try
                    {
                        // Write values to text boxes
                        cboPrefix.Text = thisReader.GetValue(0).ToString();
                        txtCode.Text = thisReader.GetValue(1).ToString();
                        cboEntity.Text = thisReader.GetValue(2).ToString();
                        txtRegion.Text = thisReader.GetValue(3).ToString();
                        txtDxCont.Text = thisReader.GetValue(4).ToString();
                        txtDxCQ.Text = thisReader.GetValue(5).ToString();
                        txtDxITU.Text = thisReader.GetValue(6).ToString();
                        txtDxIOTA.Text = thisReader.GetValue(7).ToString();

                        // Calc and display the time at dx station
                        DateTime dt = DateTime.Now;
                        TimeZone localZone = TimeZone.CurrentTimeZone;
                        DateTime currentUTC = localZone.ToUniversalTime(dt);
                        TimeSpan currentOffset = localZone.GetUtcOffset(dt);
                        string off = currentOffset.ToString();
                        if (off.Substring(0, 1) == "-") off = off.Substring(0, 3);
                        else off = off.Substring(0, 2);
                        // double LocalOffset = Math.Abs(Convert.ToDouble(off));
                        double DxOffset = Convert.ToDouble(thisReader.GetValue(8).ToString());
                        double DxOffsetRaw = Convert.ToDouble(thisReader.GetValue(8).ToString());
                        DxOffset = -(DxOffset);
                        DxOffsetRaw = -(DxOffsetRaw);
                        //double DxOffset = Math.Abs(Convert.ToDouble(thisReader.GetValue(8).ToString()));
                        //double DxOffsetRaw = Convert.ToDouble(thisReader.GetValue(8).ToString());

                        lblDxTime.Text = currentUTC.AddHours(DxOffsetRaw).ToString();
                        if (Math.Sign(DxOffset) < 0) txtDxTime.Text = "UTC-" + DxOffset;
                        else if (Math.Sign(DxOffset) > 0) txtDxTime.Text = "UTC+" + DxOffset;
                        //if (Math.Sign(DxOffsetRaw) < 0) txtDxTime.Text = "UTC-" + DxOffset;
                        //else if (Math.Sign(DxOffsetRaw) > 0) txtDxTime.Text = "UTC+" + DxOffset;
                        else txtDxTime.Text = "UTC";

                        // Format and display the dx station's latitude and longitude
                        string lat = thisReader.GetValue(9).ToString();
                        decimal dLat = Convert.ToDecimal(lat);
                        lat = Math.Round(dLat).ToString();
                        if (lat.Substring(0, 1) == "-")
                        { txtDxLat.Text = lat.TrimStart('-') + " 0' S"; }
                        else
                        { txtDxLat.Text = lat + " 0' N"; }
                        string lon = thisReader.GetValue(10).ToString();
                        decimal dLon = Convert.ToDecimal(lon);
                        lon = Math.Round(dLon).ToString();
                        if (lon.Substring(0, 1) == "-") txtDxLong.Text = lon.TrimStart('-') + " 0' W";
                        else txtDxLong.Text = lon + " 0' E";

                        // Get Grid
                        txtLoc.Text = GetGridSq(dLat, dLon) + "aa";
                        
                        // Calc and display the distance to the dx station
                        txtDxDist.ForeColor = Color.Firebrick;
                        txtDxDist.Text = Dist(Convert.ToDouble(txtLat.Text),
                            Convert.ToDouble(txtLong.Text),
                            Convert.ToDouble(lat), Convert.ToDouble(lon)).ToString();

                        // Calc the bearing
                        double lat1rad = Convert.ToDouble(txtLat.Text) * Math.PI / 180;
                        double lon1rad = Convert.ToDouble(txtLong.Text) * Math.PI / 180;
                        double lat2rad = Convert.ToDouble(lat) * Math.PI / 180;
                        double lon2rad = Convert.ToDouble(lon) * Math.PI / 180;
                        double bearing = 0.0;

                        if (Math.Sin(lon1rad - lon2rad) < 0)
                            bearing = Math.Acos((Math.Sin(lat2rad) - Math.Sin(lat1rad) *
                                      Math.Cos(distrad)) / (Math.Sin(distrad) * Math.Cos(lat1rad)));
                        else
                            bearing = 2 * Math.PI - Math.Acos((Math.Sin(lat2rad) - Math.Sin(lat1rad) *
                                       Math.Cos(distrad)) / (Math.Sin(distrad) * Math.Cos(lat1rad)));
                        bearing = bearing * (180 / Math.PI);
                        txtSP.Text = Convert.ToInt32(bearing).ToString();
                        if (bearing < 180)
                            txtLP.Text = Convert.ToInt32(bearing + 180).ToString();
                        else
                            txtLP.Text = Convert.ToInt32(bearing - 180).ToString();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(new Form() { TopMost = true }, 
                            e.Data + e.Message + "\n\n" +
                            "This error is generally due to latitude or\n" +
                            "longitude not being input on the Setup form\n\n" +
                            "Please correct and try again.", "Data Error");
                    }
                    i = 0;
                }
            }
            catch (OleDbException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                conn.Close();
            }
        }
        // Calc DX Station Distance, returns staute miles
        double distrad = 0.0;
        private int Dist(double lat1, double lon1, double lat2, double lon2)
        {
            double lat1rad = lat1 * Math.PI / 180;
            double lon1rad = lon1 * Math.PI / 180;
            double lat2rad = lat2 * Math.PI / 180;
            double lon2rad = lon2 * Math.PI / 180;
            distrad = Math.Acos(Math.Sin(lat1rad) * Math.Sin(lat2rad) +
                Math.Cos(lat1rad) * Math.Cos(lat2rad) * Math.Cos(lon2rad - lon1rad));

            int naut = Convert.ToInt32(distrad * (180 * 60 / Math.PI));
            return Convert.ToInt32(naut * 1.15);
        }
        // Get DXCC info for this Prefix
        private void GetPrefix()
        {
            string SQL = "SELECT DXCCPrefix, CountryCode, DXCCName, Location, " +
                "Continent, CQZone, ITUZone, IOTA, TimeZone, Latitude, Longitude " +
                "FROM DX WHERE DXCCPrefix = '" + cboPrefix.Text + "'";
            GetDXCC(SQL);
            //            cboPrefix.Focus();
        }
        // Get DXCC info for this Entity
        private void GetEntity()
        {
            string SQL = "SELECT DXCCPrefix, CountryCode, DXCCName, Location, " +
                "Continent, CQZone, ITUZone, IOTA, TimeZone, Latitude, Longitude " +
                "FROM DX WHERE DXCCName = '" + cboEntity.Text + "'";
            GetDXCC(SQL);
            //            cboEntity.Focus();
        }
        // Calc grid Square
        string grid = "";
        private string GetGridSq(decimal lat, decimal lon)
        {
            decimal g1, g2;
            string  g3, g4;
            if (Math.Sign(lon) < 0)         //get 1st digit
            { g1 = (180 - Math.Abs(lon)); }
            else
            { g1 = (Math.Abs(lon) + 180); }
            g1 = g1/20;
            grid = GetGridDigit(Convert.ToInt32(Math.Truncate(g1)));

            if (Math.Sign(lat) < 0)         //get 2nd digit
            { g2 = (90 - Math.Abs(lat)); }
            else
            { g2 = (Math.Abs(lat) + 90); }
            g2 = g2/10;
            grid += GetGridDigit(Convert.ToInt32(Math.Truncate(g2)));

            g3 = Math.Round(g1 * 10).ToString();
            grid += g3.Substring(g3.Length - 1, 1);

            g4 = Math.Round(g2 * 10).ToString();
            grid += g4.Substring(g4.Length - 1, 1);

            return grid;
        }
        // Get grid digit
        string digit;
        private string GetGridDigit(int num)
        {
            switch (num)
            {
                case 0: digit = "A"; break;
                case 1: digit = "B"; break;
                case 2: digit = "C"; break;
                case 3: digit = "D"; break;
                case 4: digit = "E"; break;
                case 5: digit = "F"; break;
                case 6: digit = "G"; break;
                case 7: digit = "H"; break;
                case 8: digit = "I"; break;
                case 9: digit = "J"; break;
                case 10: digit = "K"; break;
                case 11: digit = "L"; break;
                case 12: digit = "M"; break;
                case 13: digit = "N"; break;
                case 14: digit = "O"; break;
                case 15: digit = "P"; break;
                case 16: digit = "Q"; break;
                case 17: digit = "R"; break;
                case 18: digit = "S"; break;
            }
            return digit;
        }

        #endregion Rotor Methods

        #region # Rotor Setup #

        //  Initialize Rotor settings and controls (called from Setup())
        private void InitRotor()
        {   //Load DX Prefix and Entity controls from Database
            string path = Application.ExecutablePath;
            path = path.Substring(0, path.LastIndexOf("\\") + 1);

            OleDbConnection conn = new OleDbConnection(
                "provider = microsoft.jet.oledb.4.0;data source = " + path + "DDUtil.mdb;");
            OleDbCommand thisCommand = new OleDbCommand(
                "SELECT DISTINCT DXCCPrefix, DXCCName FROM DX " +
                "GROUP BY DXCCPrefix, DXCCName, TimeZone " +
                "HAVING TimeZone Is Not Null", conn);

            conn.Open();
            OleDbDataReader thisReader = thisCommand.ExecuteReader();
            cboPrefix.Items.Add("");
            cboEntity.Items.Add("");
            while (thisReader.Read())
            {
                cboPrefix.Items.Add(thisReader.GetValue(0).ToString());
                cboEntity.Items.Add(thisReader.GetValue(1).ToString());
                cboEntity.Sorted = true;
            }
            conn.Close();

            // Load saved settings
            //            chkDisFB.Checked = set.DisFBchk;
            txtLat.Text = set.Latitude;
            txtLong.Text = set.Longitude;
            txtGrid.Text = set.Grid;
            //try { cboRotorPort.SelectedIndex = set.RotorPort; }
            //catch { cboRotorPort.SelectedIndex = 0; BadPort("Rotor"); }
            cboRotorCom.SelectedIndex = set.RotorCom;
            txtAzOffset.Text = Convert.ToInt32(set.AzOffset).ToString();
            chkStepDipole.Checked = set.chkStepDipole;
            SetRotorModel(set.rotorModel);
            SetRotorSpeed(set.RotorSpeed);
            try { cboRotorPort.SelectedIndex = set.RotorPort; }
            catch { cboRotorPort.SelectedIndex = 0; BadPort("Rotor"); return; }
            chkRotorEnab.Checked = set.RotorEnab;
        }

        #endregion Rotor Setup

        #endregion Rotor Control

        #region Serial Port Events

        // Radio CAT data has arrived
        string CommBuffer = "";
        string rawFreq = "";    // Buffer cmd
        string sdrMode = "";    // PSDR mode as reported by ZZDU (aka ZZMD)
        string lastFreq = "";   // freq from last CATRxEvent
        string prevFreq = "";   // undo freq
        string memMode = "";
        string lastBand = "";
        string COBC = "";       // Chk on band change last band setting
        string bBand = "";
        double modeFactor = 1;  // holds expert auto drive mode factor
        string lastFreqB = "";
        string logFreq = "";
        string freqBCD = "";
        void sp_CATRxEvent(object source, CATSerialPorts.SerialRXEvent e)
        {
            try
            {
                comTimer.Stop();
                comTimer.Start();
                // put the port data in the comm buffer
                CommBuffer += AE.GetString(e.buffer, 0, e.buffer.Length);
                Regex rex = new Regex("ZZ.*?;");  //accept any string that contains ZZxxn...n;		
                //loop thru the buffer and find matches
                for (Match m = rex.Match(CommBuffer); m.Success; m = m.NextMatch())
                {
                    OutBuffer = m.Value;
                    // remove the match from the buffer if found
                    CommBuffer = CommBuffer.Replace(m.Value, "");
                    rawFreq = OutBuffer;
                    OutBuffer = "";
                    string cmd = rawFreq.Substring(0, 4);
                    if (rawFreq.Length < 5) break;
                    if (State["_ZZFM"] == null) 
                        sp.Write("ZZFM;");  // model?
                    if (State["_ZZVN"] == null) 
                        sp.Write("ZZVN;");  // version?

                    /*** Save the ZZDU values ***/
                    if (cmd == "ZZDU" && State["_ZZFM"] != null)
                    { ParseZZDU(); }

                    #region Process Other Cat

                    /*** Check for proper PSDR Version ***/
                    else if (cmd == "ZZVN")
                    {
                        string regx = @"(?<maj>\d+).(?<min>\d+).(?<bld>\d+).(?<ver>\d+)";
                        psdrVer = rawFreq.Substring(4, rawFreq.Length - 5);
                        Match sdrVer = Regex.Match(psdrVer, regx);
                        int maj = Convert.ToInt32(sdrVer.Groups["maj"].Value);
                        int min = Convert.ToInt32(sdrVer.Groups["min"].Value);
                        int bld = Convert.ToInt32(sdrVer.Groups["bld"].Value);
                        if (maj >= 2 && min >= 0 && bld >= 9)
                            State["_ZZVN"] = psdrVer;
                        else
                        {
                            MessageBox.Show("DDutil v2.0 requires PowerSDR v2.0.9 or greater to run.\r\r" +
                                 "Please install a later version of PowerSDR and try again.", "PSDR Version Error!");
                            Environment.Exit(0);
                        }
                    }
                    /*** Get Flex Radio Model ***/
                    else if (cmd == "ZZFM")
                    {
                        State["_ZZFM"] = rawFreq.Substring(4, 1);

                        if (InvokeRequired)
                        {
                            Invoke((MethodInvoker)delegate
                            {
                                if (State["_ZZFM"].ToString() == "3")
                                {
                                    txtTemp.Visible = false;
                                    txtVolts.Visible = false;
                                }
                                else
                                {
                                    txtTemp.Visible = true;
                                    txtVolts.Visible = true;
                                }
                            });
                        }
                        else
                            if (State["_ZZFM"].ToString() == "3")
                            {
                                txtTemp.Visible = false;
                                txtVolts.Visible = false;
                            }
                            else
                            {
                                txtTemp.Visible = true;
                                txtVolts.Visible = true;
                            }
                    }
                    // Get TX Line status
                    else if (cmd == "ZZOF")
                    {
                        stsTX = rawFreq.Substring(4, 3);
                        set.stsTX = stsTX; set.Save();
                    }
                    /*** Quick Memory, get the TX Profile & load  ***/
                    //retained outside ZZDU. is used by memory routine
                    else if (cmd == "ZZTP")
                    {
                        State["_ZZTP"] = rawFreq.Substring(4, 2);
                        TxPro = rawFreq.Substring(4, 2);
                        // ok we got the last var from PSDR lets save it to mem array
                        // if idxmem > 0 & that slot is empty load that slot.
                        if (idxMem != 0 && mem[idxMem, 0] == null)
                        { } // do nothing
                        else
                        {   // push down the stack one row & load slot #1
                            for (int i = 4; i >= 1; i--)
                            {
                                for (int j = 0; j <= 11; j++)
                                {
                                    mem[i, j] = mem[i - 1, j];
                                }
                            }
                            idxMem = 0; // set idxmem to 0
                        }
                        // load memory slot pointed to by idxMem
                        mem[idxMem, 0] = vfoA; mem[idxMem, 1] = vfoB;
                        mem[idxMem, 2] = ZZSW.ToString(); mem[idxMem, 3] = ModeR1;
                        mem[idxMem, 4] = Rx1; mem[idxMem, 5] = Rx2;
                        mem[idxMem, 6] = TxA; mem[idxMem, 7] = stsTX;
                        mem[idxMem, 8] = FltRx1; mem[idxMem, 9] = TxPro;
                        mem[idxMem, 10] = memMode; mem[idxMem, 11] = "";
                        // display the stored mem number, freq and mode
                        int xfrq = Convert.ToInt32(vfoA.Substring(0, vfoA.Length - 3).TrimStart('0'));
                        SetMemFreq(xfrq.ToString("N0"));
                        SetMemMode(memMode);
                        //idxMem = 0;
                        // set idxmem to correct visual number
                        SetMemIdx((idxMem + 1).ToString());
                        // blink the button to show data saved
                        SetMemSave("Yellow");
                        Thread.Sleep(500);
                        SetMemSave("");
                    }

                    else  //*** save other ZZ cmds not processed above ***
                    {
                        //save into State table and see if a caller needs a reply
                        string sCmd = "_" + cmd;
                        State[sCmd] = rawFreq.Substring(4, rawFreq.Length - 5);
                    }
                    // if cmd is in _caller table send reply back to originator
                    try
                    {
                        List<Caller> dc = new List<Caller>();
                        foreach (Caller c in _caller)
                        {
                            if (c.zzcat.Substring(0, 4) == cmd)
                            {
                                if (c.kwcat != null)
                                    c.port.Write(c.kwcat + State["_" + cmd].ToString() + ";");
                                else
                                    c.port.Write(c.zzcat + State["_" + cmd].ToString() + ";");
                                dc.Add(c);
                            }
                        }
                        foreach (Caller c in dc)
                            _caller.Remove(c);
                    }
                    catch 
                    { }

                    #endregion Process Other Cat

                    if (_radio.rx2sts)              // if RX2 is on
                    {
                        if (_radio.vfotx == "0")
                        { logFreq = vfoA; A95band = band; }
                        else
                        { logFreq = vfoB; A95band = bBand; }
                    }
                    else
                    {
                        if (_radio.split)           // is PSDR in split mode
                        { logFreq = vfoB; A95band = bBand; }
                        else
                        { logFreq = vfoA; A95band = band; }
                    }
                    if (logFreq != "" && logFreq != null)
                    {
                        string ampFreq = "";
                        switch (set.cboAmpFollow)
                        {
                            case 0: ampFreq = vfoA; A95band = band;
                                break;
                            case 1: ampFreq = vfoB; A95band = bBand;
                                break;
                            case 2: ampFreq = logFreq;
                                break;
                        }
                        /*** Alpha 9500 smp, send it the freq ***/
                        if (chkA95enab.Checked)
                            DoA95Amp(ampFreq);

                        /*** SPE smp, send it the freq ***/
                        if (chkSPEenab.Checked)
                            DoSpeAmp(ampFreq);

                        /*** Tube Amp, send it the freq ***/
                        if (chkAlpha.Checked)
                            DoTubeAmps(ampFreq);

                        /*** SteppIR Interactive ***/
                        // check for activity
                        if (chkStep.Checked)
                            DoSteppIR();

                        /*** Send Freq to BCD Port ***/
                        if (chkDevice.Checked && chkDev0.Checked)
                        {
                            if (String.Compare(lastFreq, ampFreq) != 0)
                            {   // if the freq has changed
                                freqBCD = ampFreq.Substring(0, 8);
                                freqBCD = freqBCD.TrimStart('0');
                                freqBCD = freqBCD.Substring(0, freqBCD.Length - 2);
                                LookUp(freqBCD);
                                SetDigit(keyValue.ToString());
                                prevFreq = lastFreq;// save for undo
                            }
                        }

                        /*** Send Freq to Passive Listener port ***/
                        PortSend(logFreq);

                        /*** Set Title Bar Caption ***/
                        string title = "DDUtil " + ver;
                        string id = "";
                        string regex = "(?<mz>\\d{5})(?<kz>\\d{3})(?<hz>\\d{2})";
                        string mask = "${mz},${kz}.${hz}";
                        string freq = "";
                        freq = Regex.Replace(logFreq, regex, mask);
                        freq = freq.TrimStart('0');
                        id = title + " - " + freq + "  " + vfo + " " + memMode;
                        if (sdrMode == "03" || sdrMode == "04")
                            id += "  [CWX=" + State["_ZZKS"].ToString().TrimStart('0') + "]";

                        /*** Finish up ***/
                        lastFreq = logFreq; // save last freq
                        LastMode = sdrMode; // save last mode
                        this.SetTitle(id);
                    }
                    //end logFreq != null
                }
                //end buffer match
            }
            //end Try
            catch (Exception ex)
            {
                //MessageBox.Show("sp_CatRxEvent Error: " + ex.Message + ex.StackTrace, "Cat Port Error");
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                    "Unable to write to log");
            }
        }//CATRxEvent

        #region # Other Serial Ports #

        /*** RCP Port Data Received ***/
        // The RCP1 CAT port has received data
        string sBuf1 = "";
        protected void OnReceive(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string sCmd = "";
                SerialPort port = (SerialPort)sender;
                byte[] data = new byte[port.BytesToRead];
                port.Read(data, 0, data.Length);
                sBuf1 += AE.GetString(data, 0, data.Length);
                Regex rex = new Regex(".*?;");			//accept any string ending in ;		
                for (Match m = rex.Match(sBuf1); m.Success; m = m.NextMatch())
                {   //loop thru the buffer and find matches
                    sCmd = m.Value.Substring(0, m.Value.Length - 1);
                    sBuf1 = sBuf1.Replace(m.Value, ""); //remove the match from the buffer

                    string state = CatParse(LogPort, sCmd);
                    if (state == null)
                        break;

                    LogPort.Write(sCmd + state + ";");
                }
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                    "Unable to write to log");
            }
        }
        // RCP2 CAT port has received data
        string sBuf2 = "";
        private void RCP2port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string sCmd = "";
                SerialPort port = (SerialPort)sender;
                byte[] data = new byte[port.BytesToRead];
                port.Read(data, 0, data.Length);
                sBuf2 += AE.GetString(data, 0, data.Length);
                Regex rex = new Regex(".*?;");				//accept any string ending in ;		
                for (Match m = rex.Match(sBuf2); m.Success; m = m.NextMatch())
                {   //loop thru the buffer and find matches
                    sCmd = m.Value.Substring(0, m.Value.Length - 1);
                    sBuf2 = sBuf2.Replace(m.Value, "");       //remove the match from the buffer

                    string state = CatParse(RCP2port, sCmd);
                    if (state == null)
                        break;

                    RCP2port.Write(sCmd + state + ";");

                }
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                    "Unable to write to log");
            }
        }
        // RCP3 CAT port has received data
        string sBuf3 = "";
        private void RCP3port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string sCmd = "";
                SerialPort port = (SerialPort)sender;
                byte[] data = new byte[port.BytesToRead];
                port.Read(data, 0, data.Length);
                sBuf3 += AE.GetString(data, 0, data.Length);
                Regex rex = new Regex(".*?;");				//accept any string ending in ;		
                for (Match m = rex.Match(sBuf3); m.Success; m = m.NextMatch())
                {   //loop thru the buffer and find matches
                    sCmd = m.Value.Substring(0, m.Value.Length - 1);
                    sBuf3 = sBuf3.Replace(m.Value, "");       //remove the match from the buffer

                    string state = CatParse(RCP3port, sCmd);
                    if (state == null)
                        break;

                    RCP3port.Write(sCmd + state + ";");
                }
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                    "Unable to write to log");
            }
        } 
        // RCP4 CAT port has received data
        string sBuf4 = "";
        private void RCP4port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string sCmd = "";
                SerialPort port = (SerialPort)sender;
                byte[] data = new byte[port.BytesToRead];
                port.Read(data, 0, data.Length);
                sBuf4 += AE.GetString(data, 0, data.Length);
                Regex rex = new Regex(".*?;");				//accept any string ending in ;		
                for (Match m = rex.Match(sBuf4); m.Success; m = m.NextMatch())
                {   //loop thru the buffer and find matches
                    sCmd = m.Value.Substring(0, m.Value.Length - 1);
                    sBuf4 = sBuf4.Replace(m.Value, "");       //remove the match from the buffer

                    string state = CatParse(RCP4port, sCmd);
                    if (state == null)
                        break;

                    RCP4port.Write(sCmd + state + ";");
                }
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                    "Unable to write to log");
            }
        }
        // RCP5 CAT port has received data
        string sBuf5 = "";
        private void RCP5port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string sCmd = "";
                SerialPort port = (SerialPort)sender;
                byte[] data = new byte[port.BytesToRead];
                port.Read(data, 0, data.Length);
                sBuf5 += AE.GetString(data, 0, data.Length);
                Regex rex = new Regex(".*?;");				//accept any string ending in ;		
                for (Match m = rex.Match(sBuf5); m.Success; m = m.NextMatch())
                {   //loop thru the buffer and find matches
                    sCmd = m.Value.Substring(0, m.Value.Length - 1);
                    sBuf5 = sBuf5.Replace(m.Value, "");       //remove the match from the buffer

                    string state = CatParse(RCP5port, sCmd);
                    if (state == null)
                        break;

                    RCP5port.Write(sCmd + state + ";");
                }
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                    "Unable to write to log");
            }
        }
        string sBuffN1MM = "";
        private void N1MMport_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string sCmd = "";
                SerialPort port = (SerialPort)sender;
                byte[] data = new byte[port.BytesToRead];
                port.Read(data, 0, data.Length);
                sBuffN1MM += AE.GetString(data, 0, data.Length);
                Regex rex = new Regex(".*?;");				//accept any string ending in ;		
                for (Match m = rex.Match(sBuffN1MM); m.Success; m = m.NextMatch())
                {   //loop thru the buffer and find matches
                    sCmd = m.Value.Substring(0, m.Value.Length - 1);
                    sBuffN1MM = sBuffN1MM.Replace(m.Value, "");       //remove the match from the buffer

                    string state = "";
                    if (sCmd != "IF")
                    {
                        state = CatParse(N1MMport, sCmd);
                        if (state == null)
                            break;
                    }
                    else
                        state = State["_N1IF"].ToString();

                    N1MMport.Write(sCmd + state + ";");
                }
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                    "Unable to write to log");
            }
        }
        /*** Rotor Port Data Received ***/
        // RCP1 Rotor port has received data
        string sRtr1Buf = "";
        private void RCP1Rotor_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (chkRotorEnab.Checked)    // Rotor must be enabled
            {
                try
                {
                    string sCmd = "";
                    SerialPort port = (SerialPort)sender;
                    byte[] data = new byte[port.BytesToRead];
                    port.Read(data, 0, data.Length);
                    sRtr1Buf += AE.GetString(data, 0, data.Length);
                    Regex rex = new Regex(".*?" + suffix);
                    for (Match m = rex.Match(sRtr1Buf); m.Success; m = m.NextMatch())
                    {   //loop thru the buffer and find matches
                        sCmd = m.Value;
                        //remove the match from the buffer
                        sRtr1Buf = sRtr1Buf.Replace(m.Value, "");
                        RotorPort.Write(sCmd);
                        Thread.Sleep(50);
                        grpModel_CheckedChanged(null, null);
                    }
                }
                catch (Exception ex)
                {
                    bool bReturnLog = false;
                    bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                    if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                        "Unable to write to log");
                }
            }
        }
        // RCP2 Rotor port has received data
        string sRtr2Buf = "";
        private void RCP2Rotor_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (chkRotorEnab.Checked)    // Rotor must be enabled
            {
                try
                {
                    string sCmd = "";
                    SerialPort port = (SerialPort)sender;
                    byte[] data = new byte[port.BytesToRead];
                    port.Read(data, 0, data.Length);
                    sRtr2Buf += AE.GetString(data, 0, data.Length);
                    Regex rex = new Regex(".*?" + suffix);
                    for (Match m = rex.Match(sRtr2Buf); m.Success; m = m.NextMatch())
                    {   //loop thru the buffer and find matches
                        sCmd = m.Value;
                        sRtr2Buf = sRtr2Buf.Replace(m.Value, "");//remove the match from the buffer
                        RotorPort.Write(sCmd);
                        Thread.Sleep(50);
                        grpModel_CheckedChanged(null, null);
                    }
                }
                catch (Exception ex)
                {
                    bool bReturnLog = false;
                    bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                    if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                        "Unable to write to log");
                }
            }
        }
        // RCP3 Rotor port has received data
        string sRtr3Buf = "";
        private void RCP3Rotor_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (chkRotorEnab.Checked)    // Rotor must be enabled
            {
                try
                {
                    string sCmd = "";
                    SerialPort port = (SerialPort)sender;
                    byte[] data = new byte[port.BytesToRead];
                    port.Read(data, 0, data.Length);
                    sRtr3Buf += AE.GetString(data, 0, data.Length);
                    Regex rex = new Regex(".*?" + suffix);
                    for (Match m = rex.Match(sRtr3Buf); m.Success; m = m.NextMatch())
                    {   //loop thru the buffer and find matches
                        sCmd = m.Value;
                        sRtr3Buf = sRtr3Buf.Replace(m.Value, "");//remove the match from the buffer
                        RotorPort.Write(sCmd);
                        Thread.Sleep(50);
                        grpModel_CheckedChanged(null, null);
                    }
                }
                catch (Exception ex)
                {
                    bool bReturnLog = false;
                    bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                    if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                        "Unable to write to log");
                }
            }
        }
        // RCP4 Rotor port has received data
        string sRtr4Buf = "";
        private void RCP4Rotor_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (chkRotorEnab.Checked)    // Rotor must be enabled
            {
                try
                {
                    string sCmd = "";
                    SerialPort port = (SerialPort)sender;
                    byte[] data = new byte[port.BytesToRead];
                    port.Read(data, 0, data.Length);
                    sRtr4Buf += AE.GetString(data, 0, data.Length);
                    Regex rex = new Regex(".*?" + suffix);
                    for (Match m = rex.Match(sRtr4Buf); m.Success; m = m.NextMatch())
                    {   //loop thru the buffer and find matches
                        sCmd = m.Value;
                        sRtr4Buf = sRtr4Buf.Replace(m.Value, "");//remove the match from the buffer
                        RotorPort.Write(sCmd);
                        Thread.Sleep(50);
                        grpModel_CheckedChanged(null, null);
                    }
                }
                catch (Exception ex)
                {
                    bool bReturnLog = false;
                    bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                    if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                        "Unable to write to log");
                }
            }
        }
        // RCP5 Rotor port has received data
        string sRtr5Buf = "";
        private void RCP5Rotor_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (chkRotorEnab.Checked)    // Rotor must be enabled
            {
                try
                {
                    string sCmd = "";
                    SerialPort port = (SerialPort)sender;
                    byte[] data = new byte[port.BytesToRead];
                    port.Read(data, 0, data.Length);
                    sRtr5Buf += AE.GetString(data, 0, data.Length);
                    Regex rex = new Regex(".*?" + suffix);
                    for (Match m = rex.Match(sRtr5Buf); m.Success; m = m.NextMatch())
                    {   //loop thru the buffer and find matches
                        sCmd = m.Value;
                        sRtr5Buf = sRtr5Buf.Replace(m.Value, "");//remove the match from the buffer
                        RotorPort.Write(sCmd);
                        Thread.Sleep(50);
                        grpModel_CheckedChanged(null, null);
                    }
                }
                catch (Exception ex)
                {
                    bool bReturnLog = false;
                    bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                    if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                        "Unable to write to log");
                }
            }
        }

        byte[] _pw1Msg = null; // save the unprocessed PW1 request
        // PW1 port has received data (Query from IC-PW1) i.e. FE FE 33 54 [03/04] FD
        // this method scan extract the messages one by one and process it.
        private void PW1port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (!chkPW1.Checked) return;  // If function is enabled proceed

                int len = PW1port.BytesToRead;
                int oldLen = 0, next = 0, j = 0; ;

                if (null != _pw1Msg) oldLen = _pw1Msg.Length; // get the saved message length
                byte[] temp = new byte[len + oldLen]; // allocte buffer to hold all available request

                // copy the old message if any
                if (oldLen > 0) Buffer.BlockCopy(_pw1Msg, 0, temp, 0, _pw1Msg.Length);
                _pw1Msg = null;

                // append the new message to the remaining from last time around
                PW1port.Read(temp, oldLen, len);

                for (int i = 0; i < temp.Length; i++)
                {
                    if ((i + 3) > temp.Length) break; //we do not have a preamble
                    if (temp[i] != 0xFE || temp[i + 1] != 0xFE) continue;// is this is a preamble? 

                    // find EOM and process the request
                    for (j = i; j < temp.Length; j++)
                    { // search for the EOM
                        if (temp[j] != 0xFD) continue; // not EOM

                        // found the EOM, now process the EOM and adjust indexs so that we can 
                        // skip processed message next time around.
                        next = j + 1;
                        ProcessPW1Query(temp, i, next - i); // process the PW1 query
                        i = j;
                        break;
                    }
                }

                // save the remaining message for next time around
                if (next < temp.Length)
                {
                    _pw1Msg = new byte[temp.Length - next];
                    Buffer.BlockCopy(temp, next, _pw1Msg, 0, _pw1Msg.Length);
                }
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                    "Unable to write to log");
            }
        }

        // This method process a full PW1 query. i.e. FE FE 33 54 [03/04] FD
        // Reply messages are hard coded as it can only be a request for freq. or mode
        private void ProcessPW1Query(byte[] inBuf, int start, int len)
        {
            try
            {
                byte[] outBuf = new byte[11];   // output message buffer
                string cn = "";                 // save command from caller
                string ra = "";                 // save caller address
                string ta = txtPW1ta.Text;      // get DDUtil address

                //
                // do a sanity check on the PW1 query
                //
                if (len != 6) return;  //for command 03 and 04 the length must be 6
                // check the message format and target address
                if (inBuf[start + 0] != 0xFE ||
                    inBuf[start + 1] != 0xFE ||
                    inBuf[start + len - 1] != 0xFD ||
                    inBuf[start + 2].ToString("x2").ToUpper() != txtPW1ta.Text) return;

                // save caller address and the command
                ra = inBuf[start + 3].ToString("x2").ToUpper();  // save caller addr
                cn = inBuf[start + 4].ToString("x2").ToUpper(); // save command

                raSetText(ra);
                string preamble = "FE";
                string EOM = "FD";

                switch (cn)
                {
                    case "03":     // is cmd to read Xcvr Frequency
                        {   // If yes, assemble reply and send to port
                            string mystring = EOM + LastFreq.Substring(1, 10) +
                                        cn + ta + ra + preamble + preamble;
                            int j = 20;
                            for (int k = 0; k < 11; k++)
                            {
                                string outtemp = mystring.Substring(j, 2);
                                outBuf[k] = byte.Parse(outtemp, NumberStyles.HexNumber);
                                j -= 2;
                            }
                            // send freq read reply for 
                            // 14.234.56 Mhz = [FE FE ra ta cn 60 45 23 14 00 FD]
                            PW1port.Write(outBuf, 0, 11);
                        }
                        break;
                    case "04":    // is cmd to read Xcvr Mode
                        {   // If yes, assemble reply and send to port
                            string mode = "";
                            switch (sdrMode)
                            {   // Lookup PW1 equivalent mode for SDR mode
                                case "00": mode = "00"; break;   // LSB
                                case "01": mode = "01"; break;   // USB
                                case "04": mode = "03"; break;   // CWU
                                case "05": mode = "05"; break;   // FMN
                                case "06": mode = "02"; break;   // AM
                                case "09": mode = "04"; break;   // RTTY (DIGL)
                                case "03": mode = "07"; break;   // CWL
                                case "07": mode = "08"; break;   // RTTY-R (DIGU)
                                default: mode = "01"; break;   // USB
                            }
                            string mystring = EOM + mode + "00" + cn + ta + ra + preamble + preamble;
                            int j = 14;
                            for (int k = 0; k < 8; k++)
                            {
                                string outtemp = mystring.Substring(j, 2);
                                outBuf[k] = byte.Parse(outtemp, NumberStyles.HexNumber);
                                j -= 2;
                            }
                            // Send mode command [FE FE ra ta cn 00 md FD]
                            PW1port.Write(outBuf, 0, 8);
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                    "Unable to write to log");
            }
        }

        #endregion # Other Serial Ports #

        #endregion Serial Port Events

        #region Serial Port Methods

        /// <summary>
        /// send radio specific frequency data to the Passive Listener ports
        /// </summary>
        private void PortSend(string freq)
        {
            try
            {
                if (rbPal.Checked && RepeatPort.IsOpen)
                {
                    byte[] bytes = new byte[11];
                    string preamble = "FE";
                    string palAdr = "E0";
                    string radAdr = "3A";
                    string EOM = "FD";
                    string mystring = "";
                    if (lastFreq != freq)
                    {
                        mystring = EOM + "00" + freq.Substring(3, 8) + "05" +
                                   radAdr + palAdr + preamble + preamble;
                        int j = 20;
                        for (int i = 0; i < 11; i++)
                        {
                            string stemp = mystring.Substring(j, 2);
                            bytes[i] = byte.Parse(stemp, NumberStyles.HexNumber);
                            j -= 2;
                        }
                        // send freq 14.234.56 Mhz = FE FE E0 3A 05 60 45 23 14 00 FD
                        RepeatPort.Write(bytes, 0, 11);
                    }
                }
                // If SteppIR is selected and the freq. has changed and 
                // the freq is not VHF++ send new freq data to it 
                if (chkStep.Checked && band.Substring(0, 1) != "V")
                    {
                    if (lastFreq != freq)
                    {
                        Thread.Sleep(100);
                        if (bFwd) StepPortMsg(freq, "00", "31");
                        else if (b180) StepPortMsg(freq, "40", "31");
                        else if (bBiDir) StepPortMsg(freq, "80", "31");
                        else if (b34) StepPortMsg(freq, "20", "31");
                    }
                }
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                    "Unable to write to log");
            }
            LastFreq = freq;
            // If enabled send freq data to IC-PW1 port
            if (chkPW1.Checked && PW1port.IsOpen && !chkDisBcast.Checked)
            {
                try
                {
                    if (chkSoEnab.Checked && chk1Amp.Checked && ZZSW == 1)
                    { freq = lastFreqB; }
                    byte[] bytes = new byte[11];
                    string preamble = "FE";
                    string ta = txtPW1ta.Text;
                    string ra = "00";
                    string cn = "00";
                    string EOM = "FD";
                    string mystring = EOM + freq.Substring(1, 10) + cn + ta + ra + preamble + preamble;
                    int j = 20;
                    for (int i = 0; i < 11; i++)
                    {
                        string temp = mystring.Substring(j, 2);
                        bytes[i] = byte.Parse(temp, NumberStyles.HexNumber);
                        j -= 2;
                    }
                    // 14.234.56 Mhz = FE FE 00 nn 00 60 45 23 14 00 FD
                    PW1port.Write(bytes, 0, 11);
                }
                catch (Exception ex)
                {
                    bool bReturnLog = false;
                    bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                    if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                        "Unable to write to log");
                }
            }
            if (AccPort.IsOpen)
            {
                byte[] bytes = new byte[11];
                string xfreq = freq;
                string mystring = "";
                string mode = "";
                int j;
                try
                {
                    switch (portmode)
                    {   // for a list of the radio types referenced below see 
                        // toolStripMenuItem4_Click() event handeler
                        case PortMode.SO2Rxlat:
                            string xlat = "";
                            switch (band)
                            {
                                case "160": xlat = "01"; break;
                                case "080": xlat = "02"; break;
                                case "040": xlat = "03"; break;
                                case "030": xlat = "04"; break;
                                case "020": xlat = "05"; break;
                                case "017": xlat = "06"; break;
                                case "015": xlat = "07"; break;
                                case "012": xlat = "08"; break;
                                case "010": xlat = "09"; break;
                            }
                            AccPort.Write("AS1" + xlat + "\r");
                            // AS105;
                            break;
                        case PortMode.None:
                            //AccPort.Write("AI3;");
                            AccPort.Write("IF" + freq + ";");
                            // 14.234.56 Mhz = IF00014234560;
                            break;
                        case PortMode.Kenwood: // K2,K3,FT(9K,2K,950,450)
                            if (lastFreq != freq)
                            {
                                //AccPort.Write("FA" + freq.Substring(3, 8) + ";");
                                AccPort.Write("FA" + freq + ";");
                                // 14.234.56 Mhz = FA00014234560;
                            }
                            if (LastMode != sdrMode && chkMode.Checked)
                            {
                                Thread.Sleep(50);
                                // LastMode = sdrMode;
                                AccPort.Write("MD" + sdrMode + ";");
                            }
                            break;
                        case PortMode.YaesuTypeI: // FT1K - FT100
                            if (lastFreq != freq)
                            {
                                mystring = "0A0" + freq.Substring(3, 7);
                                j = 8;
                                for (int i = 0; i < 5; i++)
                                {
                                    string temp = mystring.Substring(j, 2);
                                    bytes[i] = byte.Parse(temp, NumberStyles.HexNumber);
                                    j -= 2;
                                }
                                AccPort.Write(bytes, 0, 5);
                                // 14.234.56 Mhz = 56 34 42 01 0A
                            }
                            if (LastMode != sdrMode && chkMode.Checked)
                            {
                                Thread.Sleep(50);
                                // LastMode = sdrMode;
                                switch (sdrMode)
                                {   // Lookup Yaesu Type I equivalent mode
                                    case "00": mode = "00"; break;   // LSB
                                    case "01": mode = "01"; break;   // USB
                                    case "04": mode = "02"; break;   // CWU
                                    case "05": mode = "06"; break;   // FMN
                                    case "06": mode = "04"; break;   // AM
                                    case "09": mode = "08"; break;   // RTTY (DIGL)
                                    case "03": mode = "03"; break;   // CWL
                                    case "07": mode = "09"; break;   // RTTY-R (DIGU)
                                    default: mode = "01"; break;   // USB
                                }
                                mystring = "0C" + mode + "000000";
                                j = 8;
                                for (int i = 0; i < 5; i++)
                                {
                                    string temp = mystring.Substring(j, 2);
                                    bytes[i] = byte.Parse(temp, NumberStyles.HexNumber);
                                    j -= 2;
                                }
                                AccPort.Write(bytes, 0, 5);
                                // send mode  USB = 00 00 00 01 0C
                            }
                            break;
                        case PortMode.YaesuTypeII: //FT(897,857,847,817)
                            if (lastFreq != freq)
                            {
                                mystring = "0" + freq.Substring(3, 7) + "01";
                                j = 0;
                                for (int i = 0; i < 5; i++)
                                {
                                    string temp = mystring.Substring(j, 2);
                                    bytes[i] = byte.Parse(temp, NumberStyles.HexNumber);
                                    j += 2;
                                }
                                AccPort.Write(bytes, 0, 5);
                                // 14.234.56 Mhz = 01 42 34 56 01
                            }
                            if (LastMode != sdrMode && chkMode.Checked)
                            {
                                Thread.Sleep(50);
                                // LastMode = sdrMode;
                                switch (sdrMode)
                                {   // Lookup Yaesu Type II equivalent mode
                                    case "00": mode = "00"; break;   // LSB
                                    case "01": mode = "01"; break;   // USB
                                    case "04": mode = "02"; break;   // CWU
                                    case "05": mode = "08"; break;   // FMN
                                    case "06": mode = "04"; break;   // AM
                                    case "09": mode = "0A"; break;   // RTTY (DIGL)
                                    case "03": mode = "03"; break;   // CWL
                                    case "07": mode = "0A"; break;   // RTTY-R (DIGU)
                                    default: mode = "01"; break;   // USB
                                }
                                mystring = mode + "00000007";
                                j = 0;
                                for (int i = 0; i < 5; i++)
                                {
                                    string temp = mystring.Substring(j, 2);
                                    bytes[i] = byte.Parse(temp, NumberStyles.HexNumber);
                                    j += 2;
                                }
                                AccPort.Write(bytes, 0, 5);
                            }
                            break;
                        case PortMode.Icom:
                            string preamble = "FE";
                            string radNum = txtRadNum.Text;
                            string EOM = "FD";
                            string ctrlAddr = "64";
                            if (lastFreq != freq)
                            {
                                mystring = EOM + "00" + freq.Substring(3, 8) + "05" +
                                           ctrlAddr + radNum + preamble + preamble;
                                j = 20;
                                for (int i = 0; i < 11; i++)
                                {
                                    string temp = mystring.Substring(j, 2);
                                    bytes[i] = byte.Parse(temp, NumberStyles.HexNumber);
                                    j -= 2;
                                }
                                // send freq 14.234.56 Mhz = FE FE 1E E0 05 60 45 23 14 00 FD
                                AccPort.Write(bytes, 0, 11);
                            }
                            // send mode info
                            if (LastMode != sdrMode && chkMode.Checked)
                            {
                                Thread.Sleep(50);
                                // LastMode = sdrMode;
                                byte[] outBuf = new byte[8];
                                string cn = "06";
                                switch (sdrMode)
                                {   // Lookup ICOM equivalent mode
                                    case "00": mode = "00"; break;   // LSB
                                    case "01": mode = "01"; break;   // USB
                                    case "04": mode = "03"; break;   // CWU
                                    case "05": mode = "05"; break;   // FMN
                                    case "06": mode = "02"; break;   // AM
                                    case "09": mode = "04"; break;   // RTTY (DIGL)
                                    case "03": mode = "07"; break;   // CWL
                                    case "07": mode = "08"; break;   // RTTY-R (DIGU)
                                    default: mode = "01"; break;   // USB
                                }
                                mystring = EOM + mode + cn + ctrlAddr +
                                           radNum + preamble + preamble;
                                j = 12;
                                for (int k = 0; k < 7; k++)
                                {
                                    string outtemp = mystring.Substring(j, 2);
                                    outBuf[k] = byte.Parse(outtemp, NumberStyles.HexNumber);
                                    j -= 2;
                                }
                                // Send mode command [FE FE ra ta cn md FD]
                                AccPort.Write(outBuf, 0, 7);
                            }
                            break;
                    } // end switch
                }
                catch
                {
                }
            }
        }// end PortSend
        /// <summary>
        /// Creates a new serial port and rx data available event
        /// </summary>
        public void CreateSerialPort()
        {
            sp = new CATSerialPorts.CATSerialPort();
            sp.CATRxEvent += new CATSerialPorts.CATSerialPort.CATSerialRXEventHandler(sp_CATRxEvent);
            sp.WriteTimeout = 500;
            sp.ReadTimeout = 500;
        }
        private void GetPortNames()
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();
                if (ports.Length > 0)
                {
                    int[] port = new int[ports.Length];
                    for (int i = 0; i < ports.Length; i++)
                    {   // strip the com off the port name
                        port[i] = Convert.ToByte(ports[i].Substring(3, ports[i].Length - 3));
                    }
                    Array.Sort(port);   // Sort port numbers in order

                    // make sure combo boxes are empty
                    cboCAT.Items.Clear();
                    cboSerAcc.Items.Clear();
                    cboLogPort.Items.Clear();
                    cboLPport.Items.Clear();
                    cboRCP2.Items.Clear();
                    cboRCP3.Items.Clear();
                    cboRCP4.Items.Clear();
                    cboRCP5.Items.Clear();
                    cboPW1.Items.Clear();
                    cboStep.Items.Clear();
                    cboRotorPort.Items.Clear();
                    cboRCP1Rotor.Items.Clear();
                    cboRCP2Rotor.Items.Clear();
                    cboRCP3Rotor.Items.Clear();
                    cboRCP4Rotor.Items.Clear();
                    cboRCP5Rotor.Items.Clear();
                    cboN1MM.Items.Clear();
                    cboAlpha.Items.Clear();
                    cboPMport.Items.Clear();
                    cboRepeatPort.Items.Clear();
                    cboPwrPort.Items.Clear();
                    cboSPEport.Items.Clear();
                    cboSwPort.Items.Clear();
                    cboKnobPort.Items.Clear();
                    cboMacSwPort.Items.Clear();
                    cboA95port.Items.Clear();
                    // Add empty entry to port combos
                    cboCAT.Items.Add("");
                    cboSerAcc.Items.Add("");
                    cboLogPort.Items.Add("");
                    cboLPport.Items.Add("");
                    cboRCP2.Items.Add("");
                    cboRCP3.Items.Add("");
                    cboRCP4.Items.Add("");
                    cboRCP5.Items.Add("");
                    cboPW1.Items.Add("");
                    cboStep.Items.Add("");
                    cboRotorPort.Items.Add("");
                    cboRCP1Rotor.Items.Add("");
                    cboRCP2Rotor.Items.Add("");
                    cboRCP3Rotor.Items.Add("");
                    cboRCP4Rotor.Items.Add("");
                    cboRCP5Rotor.Items.Add("");
                    cboAlpha.Items.Add("");
                    cboN1MM.Items.Add("");
                    cboPMport.Items.Add("");
                    cboRepeatPort.Items.Add("");
                    cboPwrPort.Items.Add("");
                    cboSPEport.Items.Add("");
                    cboSwPort.Items.Add("");
                    cboKnobPort.Items.Add("");
                    cboMacSwPort.Items.Add("");
                    cboA95port.Items.Add("");

                    for (int i = 0; i < port.Length; i++)
                    {
                        // load port combos with port names
                        cboCAT.Items.Add("COM" + port[i]);
                        cboSerAcc.Items.Add("COM" + port[i]);
                        cboLogPort.Items.Add("COM" + port[i]);
                        cboLPport.Items.Add("COM" + port[i]);
                        cboRCP2.Items.Add("COM" + port[i]);
                        cboRCP3.Items.Add("COM" + port[i]);
                        cboRCP4.Items.Add("COM" + port[i]);
                        cboRCP5.Items.Add("COM" + port[i]);
                        cboPW1.Items.Add("COM" + port[i]);
                        cboStep.Items.Add("COM" + port[i]);
                        cboRotorPort.Items.Add("COM" + port[i]);
                        cboRCP1Rotor.Items.Add("COM" + port[i]);
                        cboRCP2Rotor.Items.Add("COM" + port[i]);
                        cboRCP3Rotor.Items.Add("COM" + port[i]);
                        cboRCP4Rotor.Items.Add("COM" + port[i]);
                        cboRCP5Rotor.Items.Add("COM" + port[i]);
                        cboN1MM.Items.Add("COM" + port[i]);
                        cboAlpha.Items.Add("COM" + port[i]);
                        cboPMport.Items.Add("COM" + port[i]);
                        cboRepeatPort.Items.Add("COM" + port[i]);
                        cboPwrPort.Items.Add("COM" + port[i]);
                        cboSPEport.Items.Add("COM" + port[i]);
                        cboSwPort.Items.Add("COM" + port[i]);
                        cboKnobPort.Items.Add("COM" + port[i]);
                        cboMacSwPort.Items.Add("COM" + port[i]);
                        cboA95port.Items.Add("COM" + port[i]);
                    }
                }
                else
                {
                    MessageBox.Show(new Form() { TopMost = true },
                        "There are NO serial ports setup on this computer!\r\r" +
                        "For this program to function there has to be at least one\r" +
                        "pair of virtual serial ports so DDUtil can talk to PowerSDR.\r\r" +
                        "Please try again after seting up at least one pair of ports!\r\r" +
                        "The program will now terminate.",
                        "Fatal Error!", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                    Environment.Exit(0);
                }
            }
            catch
            {
                MessageBox.Show(new Form() { TopMost = true },
                    "There is a problem with the serial ports setup on this computer!\r\r" +
                    "DDUtil requires that port names begin with'COM'. If you are using\r" +
                    "com0com as a virtual serial ports manager, open the com0com setup\r" +
                    "utility and change all port names to begin with 'COM'. \r\r" +
                    "Additionally, to avoid other problems make sure that the\r" +
                    "'enable buffer overrun' check boxesare checked for each port.\r\r" +
                    "The program will now terminate.",
                    "Fatal Error!", MessageBoxButtons.OK, MessageBoxIcon.Stop);

                Environment.Exit(0);
            }

        }// end GetPortNames
        /// <summary>
        /// Opens the CAT port name stored in the DefaultComRadio property.
        /// </summary>
        private void OpenSerialPort()
        {
            if (sp.isOpen)
                sp.Close();
            try
            {
                sp.Name = DefaultComRadio;
                sp.Open();
            }
            catch
            {
                // give a message, if the port is not available:
                MessageBox.Show(new Form() { TopMost = true }, 
                    "The Radio CAT serial port " + sp.Name +
                    " cannot be opened!", "Port Error",
                   MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboSerAcc.SelectedText = "";
            }
        }
        /// <summary>
        /// Writes the current CAT command to the radio serial port.
        /// The keep-alive timer is disabled and restarted for each
        /// write.  The sleep period gives the radio CAT parser time
        /// to react and return an answer.'
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="sleep"></param>
        private void WriteToPort(string cmd, int sleep)
        {
            try
            {
                //             pollTimer.Enabled = false;
                sp.Write(cmd);
                Thread.Sleep(sleep);
                //             pollTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true }, 
                    "Unable to write to log");
            }
        }
        
        #endregion Serial Port Methods

        #region SO2R

        // All specific SO2R methods and events are here. See CatRX_DataReceived for
        // other routines specific to band change and CAT operations

        #region # Enums, Signatures & Vars #

        bool bSo2rChg = false;            // SP2R data has changed
        bool bClosePass1 = false;         // 1st pass thru form close routine
        bool inhibA = false;
        bool inhibB = false;
        bool so2rInit = false;
        int LastZZSW = 0;                 // last active tx vfo a/b
        int solpt1 = 0; 
        int solpt2 = 0;
        int ZZSW = 0;                     // active tx vfo
        string dataH = "0", dataL = "0";  // data word for LPT1
        string ctrlH = "0", ctrlL = "0";  // control word for LPT2
        public static string so2rFile = "";
        public static string so2rDir = "";
        string TXa = "0", TXb = "0";      // TX ant for vfo a/b

        public static DataSet dso;

        private static string app_data_path = "";
        public static string AppDataPath
        {
            set { app_data_path = value; }
        }

        #endregion # Enums, Signatures & Vars #

        #region # SO2R Events #

        // The Save button was pressed
        private void btnSaveSO2R_Click(object sender, EventArgs e)
        {

            if (chkSoEnab.Checked)
            {
                DialogResult result;
                result = MessageBox.Show(new Form() { TopMost = true }, 
                    "Saving matrix data to file: " + so2rFile + " Is this ok?\r\r" +
                    "If 'Yes' press Ok to continue saving the file.\r" +
                    "If 'No' press No to select a new file name.\r" +
                    "Press Cancel to abort this operation.", "Information",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1);

                if (result == DialogResult.Cancel)
                    return;
                else if (result == DialogResult.No)
                {
                    // get the App Data Path
                    if (app_data_path == "")
                    {
                        AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                        + "\\DDUtil\\";
                    }
                    string fileDir = app_data_path + "SO2R\\";
                    if (!Directory.Exists(fileDir))
                        Directory.CreateDirectory(fileDir);

                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.InitialDirectory = fileDir;
                    sfd.Filter = "xml files|*.xml";
                    sfd.Title = "Select or Enter an So2R file to Save";
                //    string so2rFile = "";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        so2rFile = sfd.FileName;
                        sfd.Dispose();
                    }
                    else
                    {
                        sfd.Dispose();
                        return;
                    }
                    if (File.Exists(so2rFile))
                    { dso.Clear(); dso.ReadXml(so2rFile); }
                }
                if (File.Exists(so2rFile)) File.Delete(so2rFile);
                dso.Clear();
                ArrayList a = new ArrayList();
                foreach (Control c in this.grpSO2R.Controls)			// For each control
                {
                    if (c.GetType() == typeof(CheckBox) && c.Name != "chkSoEnab")
                        a.Add(c.Name + "/" + ((CheckBox)c).Checked.ToString());
                    else if (c.GetType() == typeof(TextBox))
                    {
                        string xx = ((TextBox)c).Text;
                        if (xx == "" || xx == null)
                        ((TextBox)c).Text= "0";
                        a.Add(c.Name + "/" + ((TextBox)c).Text);
                    }
                }
                SaveVars("SO2R", ref a);		// save the values to the DB
                bSo2rChg = false;
            }
            else
            {
                MessageBox.Show(new Form() { TopMost = true },
                    "The Enable check box must be selected before a file can be saved");
            }

        }        
        // The Load button was pressed
        private void btnLoadSO2R_Click(object sender, EventArgs e)
        {
            if (chkSoEnab.Checked)
            {
                // get the App Data Path
                if (app_data_path == "")
                {
                    AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    + "\\DDUtil\\";
                }
                string fileDir = app_data_path + "SO2R\\";
                if (!Directory.Exists(fileDir))
                    Directory.CreateDirectory(fileDir);

                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = fileDir;
                ofd.Filter = "xml files|*.xml";
                ofd.Title = "Select a So2R file to Load";
    //            string so2rFile = "";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    so2rFile = ofd.FileName;
                    ofd.Dispose();
                }
                else
                {
                    ofd.Dispose();
                    return;
                }
                if (File.Exists(so2rFile))
                { dso.Clear(); dso.ReadXml(so2rFile); }
                LoadVars();
            }
            else
            {
                MessageBox.Show(new Form() { TopMost = true },
                    "The Enable check box must be selected before a file can be loaded");
            }
        }
        // The clear matrix button was pressed
        private void btnSO2Rclear_Click(object sender, EventArgs e)
        {
            DialogResult result;
            result = MessageBox.Show(
                "You are about to clear all the matrix settings.\r\r" +
                "Do you really want to do that?\r", "Information",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1);
            if (result == DialogResult.No)
                return;

            foreach (Control c in this.grpSO2R.Controls)			// For each control
            {
                if (c.GetType() == typeof(CheckBox) && c.Name != "chkSoEnab")
                    ((CheckBox)c).Checked = false;
                else if (c.GetType() == typeof(TextBox))
                    ((TextBox)c).Text = "";
            }
            OutParallelPort(solpt1, 0);
            OutParallelPort(solpt2, 0);
        }
        // the TX switch has changed
        internal void PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            if (chkSoEnab.Checked)
            {
                SerialPinChange SerialPinChange1;
                SerialPinChange1 = e.EventType;
                switch (SerialPinChange1)
                {
                    case SerialPinChange.Break: break;
                    case SerialPinChange.CDChanged: break;
                    case SerialPinChange.CtsChanged:
                        if (SwitchPort.CtsHolding)  // set VFO B active
                        {
                            ZZSW = 1;
                            SetVfoB();
                            WriteToPort("ZZSW1;", iSleep);
                            DoRx1Tap();
                        }
                        else    // set VFO A active
                        {
                            ZZSW = 0;
                            SetVfoA();
                            WriteToPort("ZZSW0;", iSleep);
                            DoRx1Tap();
                        }
                        break;
                    case SerialPinChange.DsrChanged: break;
                }
            }
        }
        // the SO2R enable check box has changed
        private void chkSoEnab_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSoEnab.Checked)
            {
                set.chkSoEnab = true;
                SOinit();
            }
            else
            {
                // re-enable BCD port if the same port number is used by either SO2R LPTs
                if (txtPort.Text == txtLPT1_1.Text || txtPort.Text == txtLPT2.Text)
                {
                    chkDevice.Checked = true; // turn BCD back on
                    lastFreq = "";            // force freq out to portsend.
                }
                set.chkSoEnab = false;
                OutParallelPort(solpt1, 0);
                OutParallelPort(solpt2, 0);
                set.Save();
                ProcessMacroButton(29);
                MsgBoxCheck.MessageBox dlg = new MsgBoxCheck.MessageBox();
                DialogResult dr = dlg.Show(@"Software\DDUtil\MsgBoxCheck\SO2R", "DontShowAgain", //DialogResult.OK, 
                    "Don't show me this again",
                    "When disabling the SO2R feature you may need to reset some of the\r" +
                    "local settings in PowerSDR and DDUtil:\r\r" +
                    "PowerSDR - Antenna & TX settings may be altered.\r" +
                    "                    Drive settings may be altered.\r\r" +
                    "DDUtil -        Auto Drive will be disabled, re-enable if needed.\r" +
                    "                    Toggle PTT (ctrl+O) to reset TX line(s).",
                    "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        // the RX1Tap check box has changed
        private void chkRx1Tap_CheckedChanged(object sender, EventArgs e)
        {
            if (!chkRx1Tap.Checked && chkSoEnab.Checked)
                SetVfoB(); 
            else
                DoRx1Tap();
        }
        // the SO2R port number has changed
        private void cboSwPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SwitchPort.IsOpen) SwitchPort.Close();
            if (cboSwPort.SelectedIndex > 0)
            {
                SwitchPort.PortName = cboSwPort.SelectedItem.ToString();
                try
                {
                    SwitchPort.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true },
                        "The VFO switch serial port " + SwitchPort.PortName +
                       " cannot be opened!\n", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboSwPort.SelectedIndex = 0;
                }
            }
            set.cboSwPort = cboSwPort.SelectedIndex;
            set.Save();
        }
        // the single amp check box has changed
        private void chk1Amp_CheckedChanged(object sender, EventArgs e)
        {
            //if (chk1Amp.Checked) set.chk1Amp = true;
            //else set.chk1Amp = false;
            //set.Save();
            SetVfoA();
            SetVfoB();
        }
        // matrix text data has changed
        private void grpSo2R_TextChanged(object sender, EventArgs e)
        {
            bSo2rChg = true;
        }
        // matrix check box data has changed
        private void SO2Rband_CheckedChanged(object sender, EventArgs e)
        {
            bSo2rChg = true;
        }
        // the TXA digit has changed
        private void txtTxA_TextChanged(object sender, EventArgs e)
        {
            if (!formLoaded || txtTxA.Text == "") return;
            try
            {
                int aVal = Convert.ToInt32(txtTxA.Text);
                if (aVal < 1 || aVal > 3)
                {
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                MessageBox.Show(new Form() { TopMost = true },
                    "This value can only be a decimal number from 1-3. \n\n" +
                    "Please select a number in this range.", "AmpKey A Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (txtTxB.Text == "3") { txtTxA.Text = "1"; }
                else if (txtTxB.Text == "2") { txtTxA.Text = "3"; }
                else if (txtTxB.Text == "1") { txtTxA.Text = "2"; }
                return;
            }

            if (txtTxA.Text == txtTxB.Text)
            {
                MessageBox.Show(new Form() { TopMost = true },
                    "TXA and TXB can not use the same TX line. \n\n" +
                    "Please select another TX line (1-3).", "AmpKey A Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (txtTxB.Text == "3") { txtTxA.Text = "1"; }
                else if (txtTxB.Text == "1") { txtTxA.Text = "2"; }
                else if (txtTxB.Text == "2") { txtTxA.Text = "3"; }
                return;
            }
            bSo2rChg = true;
        }
        // the TXB digit has changed
        private void txtTxB_TextChanged(object sender, EventArgs e)
        {
            if (!formLoaded || txtTxB.Text == "") return;
            try
            {
                int aVal = Convert.ToInt32(txtTxB.Text);
                if (aVal < 1 || aVal > 3)
                {
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                MessageBox.Show(new Form() { TopMost = true },
                    "This value can only be a decimal number from 1-3. \n\n" +
                    "Please select a number in this range.", "AmpKey B Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (txtTxA.Text == "3") { txtTxB.Text = "1"; }
                else if (txtTxA.Text == "2") { txtTxB.Text = "3"; }
                else if (txtTxA.Text == "1") { txtTxB.Text = "2"; }
                return;
            }

            if (txtTxA.Text == txtTxB.Text)
            {
                MessageBox.Show(new Form() { TopMost = true },
                    "TXA and TXB can not use the same TX line. \n\n" +
                    "Please select another TX line (1-3).", "AmpKey B Input Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (txtTxA.Text == "3") { txtTxB.Text = "1"; }
                else if (txtTxA.Text == "1") { txtTxB.Text = "2"; }
                else if (txtTxA.Text == "2") { txtTxB.Text = "3"; }
                return;
            }
            bSo2rChg = true;
        }

        #endregion # SO2R Events #

        #region # SO2R Methods #

        // Initialization
        void SOinit()
        {
            chkSoEnab.Checked = set.chkSoEnab;
            if (chkSoEnab.Checked)
            {
                cboSwPort.SelectedIndex = set.cboSwPort;
                so2rFile = set.SO2RDataFile;
                if (app_data_path == "")
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                    string version = fvi.FileVersion.Substring(0, fvi.FileVersion.LastIndexOf("."));
                    //AppDataPath = assembly.Location;
                    AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    + "\\DDUtil\\";
                }
                so2rDir = app_data_path + "SO2R\\";
                //if (!Directory.Exists(app_data_path))
                //    Directory.CreateDirectory(app_data_path);

                dso = new DataSet();
                if (File.Exists(so2rDir + so2rFile))
                    dso.ReadXml(so2rDir + so2rFile);
                grpSO2R.Text = "SO2R - Profile: " + so2rFile;
                LoadVars(); //load matrix with last used data file

                // disable auto drive
                chkAutoDrv.Checked = false;

                // disable BCD if same port number is used by either SO2R LPTs
                if (txtPort.Text == txtLPT1_1.Text || txtPort.Text == txtLPT2.Text)
                    chkDevice.Checked = false;
                if (cboSwPort.SelectedIndex > 0)
                {
                    if (!SwitchPort.IsOpen) SwitchPort.Open();
                }
                WriteToPort("ZZRS1;", iSleep);  // turn on rx2
                WriteToPort("ZZSW0;", iSleep);  // select vfo a xmit default
                ZZSW = 0;                       // set vfo default status to VFO A
                SetVfoB();                      // force setup of the data &
                SetVfoA();                      // control words
                bSo2rChg = false;               // set the so2r controls flag to false
                if (txtLPT1_1.Text != "")
                    solpt1 = Convert.ToInt32(txtLPT1_1.Text);
                if (txtLPT2.Text != "")
                    solpt2 = Convert.ToInt32(txtLPT2.Text);
            }
            so2rInit = true;
            //}
        }
        // setup data for vfo a by band
        void SetVfoA()
        {
            try
            {
                int ctrl = 0, data = 0;
                switch (band)
                {
                    case "160":
                        if (!chkSO1a.Checked) { inhibA = true; }
                        else
                        {
                            inhibA = false;
                            WriteToPort("ZZPC" + txtPwr_1a.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOA" + txtRx_1a.Text + ";", iSleep);
                            TXa = txtTx_1a.Text;
                            dataH = txtHi1_1a.Text;
                            ctrlH = txtHi2_1a.Text;
                        }
                        break;
                    case "080":
                        if (!chkSO2a.Checked) { inhibA = true; }
                        else
                        {
                            inhibA = false;
                            WriteToPort("ZZPC" + txtPwr_2a.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOA" + txtRx_2a.Text + ";", iSleep);
                            TXa = txtTx_2a.Text;
                            dataH = txtHi1_2a.Text;
                            ctrlH = txtHi2_2a.Text;
                        }
                        break;
                    case "040":
                        if (!chkSO3a.Checked) { inhibA = true; }
                        else
                        {
                            inhibA = false;
                            WriteToPort("ZZPC" + txtPwr_3a.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOA" + txtRx_3a.Text + ";", iSleep);
                            TXa = txtTx_3a.Text;
                            dataH = txtHi1_3a.Text;
                            ctrlH = txtHi2_3a.Text;
                        }
                        break;
                    case "030":
                        if (!chkSO4a.Checked) { inhibA = true; }
                        else
                        {
                            inhibA = false;
                            WriteToPort("ZZPC" + txtPwr_4a.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOA" + txtRx_4a.Text + ";", iSleep);
                            TXa = txtTx_4a.Text;
                            dataH = txtHi1_4a.Text;
                            ctrlH = txtHi2_4a.Text;
                        }
                        break;
                    case "020":
                        if (!chkSO5a.Checked) { inhibA = true; }
                        else
                        {
                            inhibA = false;
                            WriteToPort("ZZPC" + txtPwr_5a.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOA" + txtRx_5a.Text + ";", iSleep);
                            TXa = txtTx_5a.Text;
                            dataH = txtHi1_5a.Text;
                            ctrlH = txtHi2_5a.Text;
                        }
                        break;
                    case "017":
                        if (!chkSO6a.Checked) { inhibA = true; }
                        else
                        {
                            inhibA = false;
                            WriteToPort("ZZPC" + txtPwr_6a.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOA" + txtRx_6a.Text + ";", iSleep);
                            TXa = txtTx_6a.Text;
                            dataH = txtHi1_6a.Text;
                            ctrlH = txtHi2_6a.Text;
                        }
                        break;
                    case "015":
                        if (!chkSO7a.Checked) { inhibA = true; }
                        else
                        {
                            inhibA = false;
                            WriteToPort("ZZPC" + txtPwr_7a.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOA" + txtRx_7a.Text + ";", iSleep);
                            TXa = txtTx_7a.Text;
                            dataH = txtHi1_7a.Text;
                            ctrlH = txtHi2_7a.Text;
                        }
                        break;
                    case "012":
                        if (!chkSO8a.Checked) { inhibA = true; }
                        else
                        {
                            inhibA = false;
                            WriteToPort("ZZPC" + txtPwr_8a.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOA" + txtRx_8a.Text + ";", iSleep);
                            TXa = txtTx_8a.Text;
                            dataH = txtHi1_8a.Text;
                            ctrlH = txtHi2_8a.Text;
                        }
                        break;
                    case "010":
                        if (!chkSO9a.Checked) { inhibA = true; }
                        else
                        {
                            inhibA = false;
                            WriteToPort("ZZPC" + txtPwr_9a.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOA" + txtRx_9a.Text + ";", iSleep);
                            TXa = txtTx_9a.Text;
                            dataH = txtHi1_9a.Text;
                            ctrlH = txtHi2_9a.Text;
                        }
                        break;

                    case "006":
                        if (!chkSO10a.Checked) { inhibA = true; }
                        else
                        {
                            inhibA = false;
                            WriteToPort("ZZPC" + txtPwr_10a.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOA" + txtRx_10a.Text + ";", iSleep);
                            TXa = txtTx_10a.Text;
                            dataH = txtHi1_10a.Text;
                            ctrlH = txtHi2_10a.Text;
                        }
                        break;
                }//switch band
                WriteToPort("ZZOC" + TXa + ";", iSleep);    // set xmit antenna
                data = int.Parse(dataH + ctrlH, NumberStyles.HexNumber);
                ctrl = int.Parse(dataL + ctrlL, NumberStyles.HexNumber);
                OutParallelPort(Convert.ToInt32(txtLPT1_1.Text), data);
                OutParallelPort(solpt2, ctrl);
                if (ZZSW == 0 && stsOper)
                {
                    switch (txtTxA.Text)
                    {
                        case "1": WriteToPort("ZZOF100;", iSleep); break;
                        case "2": WriteToPort("ZZOF010;", iSleep); break;
                        case "3": WriteToPort("ZZOF001;", iSleep); break;
                    }
                }
                if (inhibA) { WriteToPort("ZZPC000;", iSleep); } // set drive to zero
            }
            catch(Exception e)
            {
                if (so2rInit)
                {
                    MessageBox.Show(
                    "The most probable cause for this error is missing data in the matrix. " +
                    "Enter a zero ('0') for non-used fields\r\r" + e, "SO2R Matrix Data Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }
        // setup data for vfo b by band
        void SetVfoB()
        {
            try
            {
                int ctrl = 0, data = 0;
                switch (bBand)
                {
                    case "160":
                        if (!chkSO1b.Checked) { inhibB = true; }
                        else
                        {
                            inhibB = false;
                            WriteToPort("ZZPC" + txtPwr_1b.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOB" + txtRx_1b.Text + ";", iSleep);
                            TXb = txtTx_1b.Text;
                            if (!chk1Amp.Checked)   // if single amp not checked load Low words
                            {
                                dataL = txtLo1_1b.Text;
                                ctrlL = txtLo2_1b.Text;
                            }
                            else
                            {                       // else load High word, zero out Low words 
                                dataH = txtLo1_1b.Text;
                                ctrlH = txtLo2_1b.Text;
                                dataL = "0";
                                ctrlL = "0";
                            }
                        }
                        break;
                    case "080":
                        if (!chkSO2b.Checked) { inhibB = true; }
                        else
                        {
                            inhibB = false;
                            WriteToPort("ZZPC" + txtPwr_2b.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOB" + txtRx_2b.Text + ";", iSleep);
                            TXb = txtTx_2b.Text;
                            if (!chk1Amp.Checked)
                            {
                                dataL = txtLo1_2b.Text;
                                ctrlL = txtLo2_2b.Text;
                            }
                            else
                            {
                                dataH = txtLo1_2b.Text;
                                ctrlH = txtLo2_2b.Text;
                                dataL = "0";
                                ctrlL = "0";
                            }
                        }
                        break;
                    case "040":
                        if (!chkSO3b.Checked) { inhibB = true; }
                        else
                        {
                            inhibB = false;
                            WriteToPort("ZZPC" + txtPwr_3b.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOB" + txtRx_3b.Text + ";", iSleep);
                            TXb = txtTx_3b.Text;
                            if (!chk1Amp.Checked)
                            {
                                dataL = txtLo1_3b.Text;
                                ctrlL = txtLo2_3b.Text;
                            }
                            else
                            {
                                dataH = txtLo1_3b.Text;
                                ctrlH = txtLo2_3b.Text;
                                dataL = "0";
                                ctrlL = "0";
                            }
                        }
                        break;
                    case "030":
                        if (!chkSO4b.Checked) { inhibB = true; }
                        else
                        {
                            inhibB = false;
                            WriteToPort("ZZPC" + txtPwr_4b.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOB" + txtRx_4b.Text + ";", iSleep);
                            TXb = txtTx_4b.Text;
                            if (!chk1Amp.Checked)
                            {
                                dataL = txtLo1_4b.Text;
                                ctrlL = txtLo2_4b.Text;
                            }
                            else
                            {
                                dataH = txtLo1_4b.Text;
                                ctrlH = txtLo2_4b.Text;
                                dataL = "0";
                                ctrlL = "0";
                            }
                        }
                        break;
                    case "020":
                        if (!chkSO5b.Checked) { inhibB = true; }
                        else
                        {
                            inhibB = false;
                            WriteToPort("ZZPC" + txtPwr_5b.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOB" + txtRx_5b.Text + ";", iSleep);
                            TXb = txtTx_5b.Text;
                            if (!chk1Amp.Checked)
                            {
                                dataL = txtLo1_5b.Text;
                                ctrlL = txtLo2_5b.Text;
                            }
                            else
                            {
                                dataH = txtLo1_5b.Text;
                                ctrlH = txtLo2_5b.Text;
                                dataL = "0";
                                ctrlL = "0";
                            }
                        }
                        break;
                    case "017":
                        if (!chkSO6b.Checked) { inhibB = true; }
                        else
                        {
                            inhibB = false;
                            WriteToPort("ZZPC" + txtPwr_6b.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOB" + txtRx_6b.Text + ";", iSleep);
                            TXb = txtTx_6b.Text;
                            if (!chk1Amp.Checked)
                            {
                                dataL = txtLo1_6b.Text;
                                ctrlL = txtLo2_6b.Text;
                            }
                            else
                            {
                                dataH = txtLo1_6b.Text;
                                ctrlH = txtLo2_6b.Text;
                                dataL = "0";
                                ctrlL = "0";
                            }
                        }
                        break;
                    case "015":
                        if (!chkSO7b.Checked) { inhibB = true; }
                        else
                        {
                            inhibB = false;
                            WriteToPort("ZZPC" + txtPwr_7b.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOB" + txtRx_7b.Text + ";", iSleep);
                            TXb = txtTx_7b.Text;
                            if (!chk1Amp.Checked)
                            {
                                dataL = txtLo1_7b.Text;
                                ctrlL = txtLo2_7b.Text;
                            }
                            else
                            {
                                dataH = txtLo1_7b.Text;
                                ctrlH = txtLo2_7b.Text;
                                dataL = "0";
                                ctrlL = "0";
                            }
                        }
                        break;
                    case "012":
                        if (!chkSO8b.Checked) { inhibB = true; }
                        else
                        {
                            inhibB = false;
                            WriteToPort("ZZPC" + txtPwr_8b.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOB" + txtRx_8b.Text + ";", iSleep);
                            TXb = txtTx_8b.Text;
                            if (!chk1Amp.Checked)
                            {
                                dataL = txtLo1_8b.Text;
                                ctrlL = txtLo2_8b.Text;
                            }
                            else
                            {
                                dataH = txtLo1_8b.Text;
                                ctrlH = txtLo2_8b.Text;
                                dataL = "0";
                                ctrlL = "0";
                            }
                        }
                        break;
                    case "010":
                        if (!chkSO9b.Checked) { inhibB = true; }
                        else
                        {
                            inhibB = false;
                            WriteToPort("ZZPC" + txtPwr_9b.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOB" + txtRx_9b.Text + ";", iSleep);
                            TXb = txtTx_9b.Text;
                            if (!chk1Amp.Checked)
                            {
                                dataL = txtLo1_9b.Text;
                                ctrlL = txtLo2_9b.Text;
                            }
                            else
                            {
                                dataH = txtLo1_9b.Text;
                                ctrlH = txtLo2_9b.Text;
                                dataL = "0";
                                ctrlL = "0";
                            }
                        }
                        break;
                    case "006":
                        if (!chkSO10b.Checked) { inhibB = true; }
                        else
                        {
                            inhibB = false;
                            WriteToPort("ZZPC" + txtPwr_10b.Text.PadLeft(3, '0') + ";", iSleep);
                            WriteToPort("ZZOB" + txtRx_10b.Text + ";", iSleep);
                            TXb = txtTx_10b.Text;
                            if (!chk1Amp.Checked)
                            {
                                dataL = txtLo1_10b.Text;
                                ctrlL = txtLo2_10b.Text;
                            }
                            else
                            {
                                dataH = txtLo1_10b.Text;
                                ctrlH = txtLo2_10b.Text;
                                dataL = "0";
                                ctrlL = "0";
                            }
                        }
                        break;
                }

                data = int.Parse(dataH + ctrlH, NumberStyles.HexNumber);
                ctrl = int.Parse(dataL + ctrlL, NumberStyles.HexNumber);
                if (ZZSW == 1)
                {
                    OutParallelPort(Convert.ToInt32(txtLPT1_1.Text), data);
                    OutParallelPort(solpt2, ctrl);
                    if (chk1Amp.Checked)       // if using 1 amp
                    {
                        switch (txtTxA.Text)    // use Key Line A
                        {
                            case "1": WriteToPort("ZZOF100;", iSleep); break;
                            case "2": WriteToPort("ZZOF010;", iSleep); break;
                            case "3": WriteToPort("ZZOF001;", iSleep); break;
                        }
                    }
                    else
                    {
                        switch (txtTxB.Text)    // use Key Line B
                        {
                            case "1": WriteToPort("ZZOF100;", iSleep); break;
                            case "2": WriteToPort("ZZOF010;", iSleep); break;
                            case "3": WriteToPort("ZZOF001;", iSleep); break;
                        }
                    }
                    WriteToPort("ZZOC" + TXb + ";", iSleep);    // set xmit antenna
                }
                else
                {
                    switch (txtTxA.Text)    // use Key Line A
                    {
                        case "1": WriteToPort("ZZOF100;", iSleep); break;
                        case "2": WriteToPort("ZZOF010;", iSleep); break;
                        case "3": WriteToPort("ZZOF001;", iSleep); break;
                    }
                }

                // if band not used set drive to zero
                if (inhibB) { WriteToPort("ZZPC000;", iSleep); }
            }
            catch (Exception e)
            {
                if (so2rInit)
                {
                    MessageBox.Show(
                    "The most probable cause for this error is missing data in the matrix. " +
                    "Enter a zero ('0') for non-used fields\r\r" + e, "SO2R Matrix Data Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        void DoRx1Tap()
        {
            if (chkSoEnab.Checked && chkRx1Tap.Checked && (band == bBand))
            {
                WriteToPort("ZZOB6;", iSleep);                  //RX2 Receive antenna
                WriteToPort("ZZOC" + TXa + ";", iSleep);        //RX1 xmit antenna
                switch (txtTxA.Text)    // use Key Line B
                {
                    case "1": WriteToPort("ZZOF100;", iSleep); break;
                    case "2": WriteToPort("ZZOF010;", iSleep); break;
                    case "3": WriteToPort("ZZOF001;", iSleep); break;
                }
            }
        }

        // convert vfo b freq to band
        //int FreqToBand(int freq)
        //{
        //    int vfoBband = 0;
        //    if (freq > 180 && freq < 200) {vfoBband = 1;}// "160";
        //    else if (freq > 349 && freq < 401) {vfoBband = 2;}
        //    else if (freq > 699 && freq < 731) {vfoBband = 3;}
        //    else if (freq > 1009 && freq < 1016) {vfoBband = 4;}
        //    else if (freq > 1399 && freq < 1436) {vfoBband = 5;}
        //    else if (freq > 1806 && freq < 1817) {vfoBband = 6;}
        //    else if (freq > 2099 && freq < 2145) {vfoBband = 7;}
        //    else if (freq > 2488 && freq < 2500) {vfoBband = 8;}
        //    else if (freq > 2799 && freq < 3001) {vfoBband = 9;}
        //    else if (freq > 4999 && freq < 5401) { vfoBband = 10; }

        //    return vfoBband;
        //}
        // One of the VFO TX buttons was pressed on the console
        void zzswChanged(int zzsw)
        {
            if (zzsw == 0) SetVfoA();
            else if (zzsw == 1) SetVfoB();
        }
        //Load user vars from file to controls
        void LoadVars()
        {
            ArrayList chk_list = new ArrayList();
            ArrayList txt_list = new ArrayList();
            foreach (Control c in this.grpSO2R.Controls)			// For each control
            {
                if (c.GetType() == typeof(CheckBox))
                    chk_list.Add(c);
                else if (c.GetType() == typeof(TextBox))
                    txt_list.Add(c);
            }
            ArrayList a = GetVars("SO2R");							// Get the saved list of controls
            a.Sort();
            int num_controls = chk_list.Count + txt_list.Count;

            foreach (string s in a)				// string is in the format "name,value"
            {
                string[] vals = s.Split('/');
                string name = vals[0];
                string val = vals[1];

                if (s.StartsWith("chk"))			// control is a CheckBox
                {
                    for (int i = 0; i < chk_list.Count; i++)
                    {	// look through each control to find the matching name
                        CheckBox c = (CheckBox)chk_list[i];
                        if (c.Name.Equals(name))		// name found
                        {
                            c.Checked = bool.Parse(val);	// restore value
                            i = chk_list.Count + 1;
                        }
                        if (i == chk_list.Count)
                            MessageBox.Show(new Form() { TopMost = true },
                                "Control not found: " + name, "GetVars Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else if (s.StartsWith("txt"))
                {	// look through each control to find the matching name
                    for (int i = 0; i < txt_list.Count; i++)
                    {
                        TextBox c = (TextBox)txt_list[i];
                        if (c.Name.Equals(name))		// name found
                        {
                            c.Text = val;	// restore value
                            i = txt_list.Count + 1;
                        }
                        if (i == txt_list.Count)
                            MessageBox.Show(new Form() { TopMost = true },
                                "Control not found: " + name, "GetVars Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            bSo2rChg = false;   // set the so2r change flag to false
        }
        // load macro file
        void LoadMacro(string file)
        {
            if (File.Exists(app_data_path + "\\" + file))
            {
                dso.Clear(); 
                dso.ReadXml(app_data_path + "\\" + file); 
                grpSO2R.Text = "SO2R - Profile: " + file;
                set.SO2RDataFile = file;
                set.Save();
            }
            LoadVars();
        }
        // save user variables from controls to dataset
		public static void SaveVars(string tableName, ref ArrayList list)
		{
			if(!dso.Tables.Contains(tableName))
				AddFormTable(tableName);

			foreach(string s in list)
			{
				string[] vals = s.Split('/');
				if(vals.Length > 2)
				{
					for(int i=2; i<vals.Length; i++)
						vals[1] += "/"+vals[i];
				}
//                dso.Clear(); 
				DataRow[] rows = dso.Tables[tableName].Select("Key = '"+vals[0]+"'");
				if(rows.Length == 0)	// name is not in list
				{
					DataRow newRow = dso.Tables[tableName].NewRow();
					newRow[0] = vals[0];
					newRow[1] = vals[1];
					dso.Tables[tableName].Rows.Add(newRow);
				}
				else if(rows.Length == 1)
				{
					rows[0][1] = vals[1];
				}
			}
            SaveDB();
		}
        // retrieve user variables from dataset to array list
		public static ArrayList GetVars(string tableName)
		{
			ArrayList list = new ArrayList();
			if(!dso.Tables.Contains(tableName))
				return list;

			DataTable t = dso.Tables[tableName];

			for(int i=0; i<t.Rows.Count; i++)
			{
				list.Add(t.Rows[i][0].ToString()+"/"+t.Rows[i][1].ToString());
			}

			return list;
		}
        // add a table to the data set
        private static void AddFormTable(string name)
        {
            dso.Tables.Add(name);
            dso.Tables[name].Columns.Add("Key", typeof(string));
            dso.Tables[name].Columns.Add("Value", typeof(string));
        }
        // write data set to the db file
        public static void SaveDB()
        {
//            dso.WriteXml(app_data_path + "\\" + so2rFile , XmlWriteMode.WriteSchema);
            dso.WriteXml(so2rFile);
        }

        #endregion # SO2R Methods #

        #endregion SO2R

        #region SPE Amp

        #region # Vars & Tables #

        bool alarm = false;     // amp has an alarm in progress
        bool contest = false;   // the contest mode is true
        bool led = false;       // CAT activity indicator
        bool SPEon = false;     // DDU thinks the amp is on
        bool wattmtr = false;
        private List<byte> PortBuf = new List<byte>(); // port storage

        static string[] SPEerrors = new string[] {

            "","","","","","","","","","","","","","","","","", // 0x00-0x10
            "WARNING: PA Voltage Low",   //11
            "WARNING: PA Voltage Low",   //12
            "WARNING: PA Voltage High",  //13
            "WARNING: PA Voltage High",  //14
            "WARNING: PA Current High",  //15
            "WARNING: PA Current High",  //16
            "WARNING: PA Temp >90C",     //17
            "WARNING: PA Drive to High", //18
            "",                          //19
            "",                          //1A
            "WARNING: HIGH SWR",         //1B
            "WARNING: PA Protection Fault",  //1C
            "Alarm History to Follow",   //1D
            "Shutdown in Progress",      //1E
            "Switching to Oper, Please Wait!",  //1F
            };

        #endregion # Vars & Tables #

        #region # Delegates #

        // change LED color
        delegate void SetSPEledCallback(string text);
        public void SetSPEled(string text)
        {
            //     if (this.txtLed.InvokeRequired)
            if (this.rbLed.InvokeRequired)
            {
                SetSPEledCallback d = new SetSPEledCallback(SetSPEled);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (led) rbLed.Checked = true; else rbLed.Checked = false;
                if (text == "False") rbLed.Checked = false;

            }
        }
        // Write to Temp window
        delegate void SetSPEtempCallback(string text);
        public void SetSPEtemp(string text)
        {
            if (this.txtSPEtemp.InvokeRequired)
            {
                SetSPEtempCallback d = new SetSPEtempCallback(SetSPEtemp);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtSPEtemp.Text = text;
                if (text == "   ")
                { txtSPEtemp.BackColor = Color.Empty; return; }
                int temp = Convert.ToInt32(text.Substring(0, text.Length - 2));

                if (text.Substring(text.Length - 1, 1) == "C")
                {
                    if (temp >= 70 && temp < 80)
                        txtSPEtemp.BackColor = Color.Yellow;
                    else if (temp >= 80 && temp < 90)
                        txtSPEtemp.BackColor = Color.Orange;
                    else if (temp >= 90)
                        txtSPEtemp.BackColor = Color.Red;
                    else
                        txtSPEtemp.BackColor = Color.LightGreen;
                }
                else
                {
                    if (temp >= 150 && temp < 175)
                        txtSPEtemp.BackColor = Color.Yellow;
                    else if (temp >= 175 && temp < 195)
                        txtSPEtemp.BackColor = Color.Orange;
                    else if (temp >= 195)
                        txtSPEtemp.BackColor = Color.Red;
                    else
                        txtSPEtemp.BackColor = Color.LightGreen;
                }

            }
        }
        // Write to Antenna window
        delegate void SetSPEantCallback(string text);
        public void SetSPEant(string text)
        {
            if (this.txtSPEant.InvokeRequired)
            {
                SetSPEantCallback d = new SetSPEantCallback(SetSPEant);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtSPEant.Text = text;
                if (contest) 
                    txtSPEant.BackColor = Color.Yellow;
                else 
                    txtSPEant.BackColor = Color.Empty;
            }
        }
        // Write to Mains On button
        delegate void SetSPEonCallback(string text);
        public void SetSPEon(string text)
        {
            if (this.btnSPEon.InvokeRequired)
            {
                SetSPEonCallback d = new SetSPEonCallback(SetSPEon);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "True") btnSPEon.BackColor = Color.LightGreen;
                else btnSPEon.BackColor = Color.Empty;
            }
        }
        // Write to Mains OFF button
        delegate void SetSPEoffCallback(string text);
        public void SetSPEoff(string text)
        {
            if (this.btnSPEoff.InvokeRequired)
            {
                SetSPEoffCallback d = new SetSPEoffCallback(SetSPEoff);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "True") btnSPEoff.BackColor = Color.Yellow;
                else btnSPEoff.BackColor = Color.Empty;
            }
        }
        // Write to Oper/Stby (Mode) button
        delegate void SetSPEoperCallback(string text);
        public void SetSPEoper(string text)
        {
            if (this.btnSPEoper.InvokeRequired)
            {
                SetSPEoperCallback d = new SetSPEoperCallback(SetSPEoper);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                btnSPEoper.Text = text;
                if (text == "Oper") btnSPEoper.BackColor = Color.LightGreen;
                else if (text == "Stby") btnSPEoper.BackColor = Color.Yellow;
                else btnSPEoper.BackColor = Color.Empty;
            }
        }
        // Write to Tune button
        delegate void SetSPEtuneCallback(string text);
        public void SetSPEtune(string text)
        {
            if (this.btnSPEtune.InvokeRequired)
            {
                SetSPEtuneCallback d = new SetSPEtuneCallback(SetSPEtune);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "True") btnSPEtune.BackColor = Color.Pink;
                else btnSPEtune.BackColor = Color.Empty;
            }
        }
        // Write to Full/Half Power button
        delegate void SetSPEpwrCallback(string text);
        public void SetSPEpwr(string text)
        {
            if (this.btnSPEpwr.InvokeRequired)
            {
                SetSPEpwrCallback d = new SetSPEpwrCallback(SetSPEpwr);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                btnSPEpwr.Text = text;
                if (text == "Full") btnSPEpwr.BackColor = Color.LightGreen;
                else if (text == "Half") btnSPEpwr.BackColor = Color.Yellow;
                else btnSPEpwr.BackColor = Color.Empty;
            }
        }
        // Write to Msg window
        delegate void SetSPEmsgCallback(string text);
        public void SetSPEmsg(string text)
        {
            if (this.txtSPEmsg.InvokeRequired)
            {
                SetSPEmsgCallback d = new SetSPEmsgCallback(SetSPEmsg);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtSPEmsg.Text += "\r\n" + text;
                txtSPEmsg.SelectionStart = txtSPEmsg.Text.Length;
                txtSPEmsg.ScrollToCaret();
            }
        }
        // Clear Msg window
        delegate void SPEmsgClrCallback(string text);
        public void SPEmsgClr(string text)
        {
            if (this.txtSPEmsg.InvokeRequired)
            {
                SPEmsgClrCallback d = new SPEmsgClrCallback(SPEmsgClr);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                txtSPEmsg.Clear();
            }
        }
        // Write to Display button
        delegate void SetSPEdispCallback(string text);
        public void SetSPEdisp(string text)
        {
            if (this.btnSPEdisp.InvokeRequired)
            {
                SetSPEdispCallback d = new SetSPEdispCallback(SetSPEdisp);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (alarm) btnSPEdisp.BackColor = Color.Pink;
                else btnSPEdisp.BackColor = Color.Empty;
            }
        }

        #endregion Delegates

        #region # Methods #

        //  Initialize SPE settings and controls (called from Setup())
        private void InitSPE()
        {
            try
            {
                SPETimer.Enabled = false;
                SPEmsgClr("");
                // see if digital watt meter is being used.
                if (chkSPEenab.Checked && !chkLPenab.Checked && 
                    !chkWNEnab.Checked && !chkPM.Checked)
                {
                    //txtAvg.Enabled = true; txtSWR.Enabled = true;
                    txtAvg.Text = "0.0"; txtSWR.Text = "0.00";
                    lblAvg.Text = "FWD";
                    wattmtr = true;
                }
                else wattmtr = false;

                //// the following are for testing only, see SPE test routines region 
                //if (TestPort.IsOpen) { TestPort.Close(); }
                //TestPort.PortName = "COM29";
                //TestPort.Open();

                // test to see if amp is running
                if (!SPEport.IsOpen) SPEport.Open();
                byte[] bytes = new byte[6] { 0x55, 0x55, 0x55, 0x01, 0x80, 0x80 };
                SPEport.Write(bytes, 0, 6);
                //rcu = true;
            }
            catch { }
        }

        #endregion # Methods #

        #region # Events #

        // the SPE port has received data
        private void SPEport_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (chkSPEenab.Checked) // port must be enabled to accept data
            {
                try
                {
                    if (!SPEon) // turn on the main power light
                    {
                        // see if digital watt meter is being used.
                        if (!chkLPenab.Checked && !chkWNEnab.Checked && !chkPM.Checked)
                            wattmtr = true;
                        else
                            wattmtr = false;
                        SPEon = true;
                        SetSPEon("True");
                    }
                    SetSPEled("");  // toggle activity indicator
                    led = !led;

                    SerialPort port = (SerialPort)sender;
                    byte[] data = new byte[port.BytesToRead];

                    port.Read(data, 0, data.Length);
                    PortBuf.AddRange(data);

                    if (PortBuf.Count >=6)
                    {
                        int i1 = PortBuf.IndexOf(0xAA); // find sync start
                        if (i1 < 0) // if sync bytes not found clear buffer
                        { PortBuf.Clear(); }
                        else if (PortBuf[i1 + 1] == 0xAA && PortBuf[i1 + 2] == 0xAA)
                        {
                            if (i1 > 0) // if index > 0 delete all before it.
                            { PortBuf.RemoveRange(0, i1); }
                            if (PortBuf[3] == 0x01) // dump the ack packet
                            { PortBuf.RemoveRange(0, 6); }
                            if ( PortBuf.Count >=35 && PortBuf[0] == 0xAA && 
                                PortBuf[1] == 0xAA && PortBuf[2] == 0xAA)
                            {
                                ProcessSPEBuf(); //process the 35 byte data packet
//                                Console.WriteLine(PortBuf.Count);
                                PortBuf.RemoveRange(0, 35); // delete procesed packet
                            }
                        }
                        else { return; }
                    }
                }
                catch (Exception ex)
                {
                    bool bReturnLog = false;
                    bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                    if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                        "Unable to write to log");
                }
            }
        }
        // Process the port buffer data
        private void ProcessSPEBuf()
        {
            try
            {
                // tuner operation
                if (Convert.ToBoolean(PortBuf[5] & 0x01))
                    SetSPEtune("True");
                else 
                    SetSPEtune("False");
                // Operate / Stby
                if (Convert.ToBoolean(PortBuf[5] & 0x02))
                    SetSPEoper("Oper");
                else 
                    SetSPEoper("Stby");
                // Alarm in progress
                if (Convert.ToBoolean(PortBuf[5] & 0x08))
                { alarm = true; SetSPEdisp(""); }
                else 
                { alarm = false; SetSPEdisp(""); }
                // Power level
                if (Convert.ToBoolean(PortBuf[5] & 0x10))
                    SetSPEpwr("Full");
                else 
                    SetSPEpwr("Half");
                // Contest mode
                if (Convert.ToBoolean(PortBuf[5] & 0x20))
                    contest = true;
                else 
                    contest = false;

                // CTX message received
                if (PortBuf[6] > 0x02)
                {
                    // Data stored message
                    if (PortBuf[6] == 0x05)
                    {
                        SetSPEmsg("Data Stored");
                    }
                    // Setup Opotions Menu messages
                    if (PortBuf[6] == 0x06)
                    {
                        switch (PortBuf[8])
                        {
                            case 0x00: SetSPEmsg("Setup: ANTENNA"); break;
                            case 0x01: SetSPEmsg("Setup: CAT"); break;
                            case 0x02: SetSPEmsg("Setup: MANUAL TUNE"); break;
                            case 0x03: SetSPEmsg("Setup: BACKLIGHT"); break;
                            case 0x04: SetSPEmsg("Setup: CONTEST"); break;
                            case 0x05: SetSPEmsg("Setup: BEEP"); break;
                            case 0x06: SetSPEmsg("Setup: START"); break;
                            case 0x07: SetSPEmsg("Setup: TEMP"); break;
                            case 0x08: SetSPEmsg("Setup: QUIT"); break;
                        }
                    }
                    // Warning msg displayed
                    if (PortBuf[6] > 0x10 && PortBuf[6] < 0x1D)
                    {   
                        SetSPEmsg(SPEerrors[PortBuf[6]]);
                    }
                    // Warning msg displayed
                    if (PortBuf[6] == 0x1D && PortBuf[7] > 0x00)
                    {
                        int ctr = PortBuf[7] & 0x0F;
                        SetSPEmsg("Error Msg History Follows (" + ctr + ")");
                        for (int i = 1; i < ctr + 1; i++)
                        {
                            SetSPEmsg(SPEerrors[PortBuf[7 + i]]);
                        }
                    }
                    // the amp is shutting down
                    if (PortBuf[6] == 0x1E)
                    {
                        SetSPEon("False"); SetSPEoper("Off"); SetSPEtune("Off");
                        SetSPEpwr("Off"); SetSPEtemp("   "); SPEmsgClr("");
                        alarm = false; SetSPEdisp("");
                        contest = false; SetSPEant("");
                        SetSPEled("False");
                        SPEon = false;
                        wattmtr = false;
                        SPEport.DiscardInBuffer();
                        PortBuf.Clear();
                        SetAvg(" ");
                        SetSwr(" ");
                    }
                }// end if (PortBuf[6] > 0x02)

                // antenna selected
                if (PortBuf[22] == 4)
                {
                    SetSPEmsg("No Antenna Selected!");
                    SetSPEant("?");
                }
                else
                { SetSPEant(((PortBuf[22] & 0x0F) + 1).ToString()); }

                // PWR reading
                int tpwr = 0;
                if (wattmtr)
                {
                    tpwr = (PortBuf[27] << 8) + PortBuf[26];
                    string pwr = tpwr.ToString();
                    if (tpwr == 0)
                        SetAvg("0.0");
                    else
                    {
                        SetAvg(pwr.Substring(0, pwr.Length - 1) + "." +
                                pwr.Substring(pwr.Length - 1, 1));
                    }
                }
                // SWR reading
                if (wattmtr)
                {
                    if (Convert.ToBoolean(PortBuf[5] & 0x02)) // ? Oper mode
                    {
                        double pfwd = ((PortBuf[27] << 8) + PortBuf[26]) / 10;
                        double pref = ((PortBuf[29] << 8) + PortBuf[28]) / 10;
                        if (pfwd > 0 && pref > 0)
                        {
                            double rho = Math.Sqrt(pref / pfwd);
                            double tswr = (1 + rho) / (1 - rho);
                            string swr = tswr.ToString("0.00");
                            SetSwr(swr);
                        }
                        else
                            SetSwr("0.00");

                    }
                    else // No
                    {
                        int tswr = (PortBuf[24] << 8) + PortBuf[23];
                        string swr = tswr.ToString();
                        if (tswr == 0)
                            SetSwr("0.00");
                        else
                        {
                            SetSwr(swr.Substring(0, swr.Length - 2) + "." +
                                swr.Substring(swr.Length - 2, 2));
                        }
                    }
                }
                // pa temp(C)
                if (Convert.ToBoolean(PortBuf[5] & 0x80))
                    SetSPEtemp(PortBuf[25].ToString() + " C");
                else
                    SetSPEtemp(PortBuf[25].ToString() + " F");

            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                "Unable to write to log");
            }
        } // end ProcessBuf()

        // the enable checkbox has been checked
        private void chkSPEenab_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSPEenab.Checked)
            {
                if (cboSPEport.SelectedIndex > 0)
                {
                    set.SPEenab = true;
                    InitSPE();
                }
                else
                {
                    MessageBox.Show(new Form() { TopMost = true },
                        "No port has been selected for the SPE amplifier.\n\n" +
                    "Please select a valid port number and try again.", "Port Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    chkSPEenab.Checked = false; set.SPEenab = false;
                    cboSPEport.SelectedIndex = 0; 
                    set.SPEport = cboSPEport.SelectedIndex;
                }
            }
            else
            {
                SPEon = false;
                SetSPEon("False"); SetSPEoper("Off"); SetSPEtune("Off");
                SetSPEpwr("Off"); SetSPEtemp("   "); SPEmsgClr("");
                SetSPEled("False"); 
                //txtAvg.Enabled = false; 
                //txtSWR.Enabled = false;
                chkSPEenab.Checked = false; set.SPEenab = false;
//                if (SPEport.IsOpen) SPEport.Close();
            }
            set.Save();
        }
        // the serial port selection has changed
        private void cboSPEport_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SPEport.IsOpen) SPEport.Close();
            if (cboSPEport.SelectedIndex > 0)
            {
                SPEport.PortName = cboSPEport.SelectedItem.ToString();
                try
                {
                    SPEport.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true },
                        "The Amplifier serial port " + SPEport.PortName +
                       " cannot be opened!\n", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    chkSPEenab.Checked = false; 
                    set.SPEenab = false;
                    cboSPEport.SelectedIndex = 0;
                    set.SPEport = 0;
                    set.Save();
                    return;
                }
            }
            else
            {
                cboSPEport.SelectedIndex = 0;
                chkSPEenab.Checked = false;
            }
            set.SPEport = cboSPEport.SelectedIndex;
            set.Save();
        }
        // the antenna button has been pressed 
        private void btnSPEant_Click(object sender, EventArgs e)
        {
            if (SPEport.IsOpen)
            {
                byte[] bytes = new byte[7] { 0x55, 0x55, 0x55, 0x02, 0x10, 0x2B, 0x3B };
                SPEport.Write(bytes, 0, 7);
            }
        }
        // the power on button has been pressed, toggle the DTR line
        private void btnSPEon_Click(object sender, EventArgs e)
        {
            if (!SPEon)
            {
                SPEport.DtrEnable = true;
                using (new HourGlass())
                {
                    Thread.Sleep(5000); // allow time for DTR to initialize radio
                }
                SPEport.DtrEnable = false;
                using (new HourGlass())
                {
                    Thread.Sleep(3000); // allow time for radio to settle
                }
                // send polling command
                byte[] bytes = new byte[6] { 0x55, 0x55, 0x55, 0x01, 0x80, 0x80 };
                SPEport.Write(bytes, 0, 6);
            }
        }
        // the power off button has been pressed
        private void btnSPEoff_Click(object sender, EventArgs e)
        {
            if (SPEport.IsOpen)
            {
                // send Off cmd to amp
                byte[] bytes = new byte[7] { 0x55, 0x55, 0x55, 0x02, 0x10, 0x18, 0x28 };
                SPEport.Write(bytes, 0, 7);
            }
        }
        // the oper/stby button has been pressed
        private void btnSPEoper_Click(object sender, EventArgs e)
        {
            if (SPEport.IsOpen)
            {
                byte[] bytes = new byte[7] { 0x55, 0x55, 0x55, 0x02, 0x10, 0x1C, 0x2C };
                SPEport.Write(bytes, 0, 7);
            }
        }
        // the tune button has been pressed
        private void btnSPEtune_Click(object sender, EventArgs e)
        {
            if (SPEport.IsOpen)
            {
                byte[] bytes = new byte[7] { 0x55, 0x55, 0x55, 0x02, 0x10, 0x34, 0x44 };
                SPEport.Write(bytes, 0, 7);
            }
        }
        // the Full/Half power button has been pressed
        private void btnSPEpwr_Click(object sender, EventArgs e)
        {
            if (SPEport.IsOpen)
            {
                byte[] bytes = new byte[7] { 0x55, 0x55, 0x55, 0x02, 0x10, 0x1A, 0x2A };
                SPEport.Write(bytes, 0, 7);
            }
        }
        // the display error button has been pressed
        private void btnSPEdisp_Click(object sender, EventArgs e)
        {
            if (SPEport.IsOpen)
            {
                byte[] bytes = new byte[7] { 0x55, 0x55, 0x55, 0x02, 0x10, 0x1B, 0x2B };
                SPEport.Write(bytes, 0, 7);
                Thread.Sleep(100);
                SPEport.Write(bytes, 0, 7);
            }
        }
        // the message text box has been double-clicked, clear the window.
        private void txtSPEmsg_DoubleClick(object sender, EventArgs e)
        {
            txtSPEmsg.Clear();
        }
        // the SET button was pressed
        private void btnSPEset_Click(object sender, EventArgs e)
        {
            byte[] bytes = new byte[7] { 0x55, 0x55, 0x55, 0x02, 0x10, 0x2F, 0x3F };
            SPEport.Write(bytes, 0, 7);
        }
        // the <<< button was pressed
        private void btnSPEleft_Click(object sender, EventArgs e)
        {
            byte[] bytes = new byte[7] { 0x55, 0x55, 0x55, 0x02, 0x10, 0x2D, 0x3D };
            SPEport.Write(bytes, 0, 7);
        }
        // the >>> button was pressed
        private void btnSPEright_Click(object sender, EventArgs e)
        {
            byte[] bytes = new byte[7] { 0x55, 0x55, 0x55, 0x02, 0x10, 0x2E, 0x3E };
            SPEport.Write(bytes, 0, 7);
        }
        // the SPE timer has elapsed, request status from amp.
        void SPETimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (TestPort.IsOpen)
                {
                TestPort.Write(stsBytes1, 0, 12);
                Thread.Sleep(10);
                TestPort.Write(stsBytes2, 0, 12);
                Thread.Sleep(10); 
                TestPort.Write(stsBytes3, 0, 11);
                Thread.Sleep(10);
            }
        }

        #endregion # Events #

        #region * Test Routines *

        // the test port (Amp) has received data from DDUtil
        // process the message and send back a response ststus packet
        bool stby = false;
        bool pwr = false;
        string sTest = "";             // Port buffer message
        byte[] ackBytes = new byte[6] { 0xAA, 0XAA, 0XAA, 0X01, 0X06, 0X06 };
        private void TestPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            string sCmd = "";
            SerialPort port = (SerialPort)sender;
            byte[] data = new byte[port.BytesToRead];
            if (port.BytesToRead == 0) return;
            port.Read(data, 0, data.Length);
            sTest += AE.GetString(data, 0, data.Length);

            Regex rex = new Regex(@"\x55{3}.*");
            for (Match m = rex.Match(sTest); m.Success; m = m.NextMatch())
            {   //loop thru the buffer and find matches
                sCmd = m.Value.Substring(0, m.Value.Length);
                sTest = sTest.Replace(m.Value, ""); //remove the match from the buffer

                if (data[4] == 16)    // key command (0x10)
                {
                    switch (data[5])
                    {
                        case 24:  //off     (0x18)
                            Thread.Sleep(100);
                            stsBytes1[6] = 0x1E;
                            SPETimer.Stop();
                            TestPort.Write(ackBytes, 0, 6);
                            TestPort.Write(stsBytes1, 0, 12);
                            TestPort.Write(stsBytes2, 0, 12);
                            TestPort.Write(stsBytes3, 0, 11);
                            break;
                        case 26:  //mode    (0x1A)
                            int xx = 0;
                            if (pwr) xx = stsBytes1[5] ^ 0x10;
                            else xx = stsBytes1[5] | 0x10;
                            stsBytes1[5] = (byte)xx;
                            pwr = !pwr;
                            Thread.Sleep(100);
                            TestPort.Write(ackBytes, 0, 6);
                            break;
                        case 27:  //display (0x1B)
                            stsBytes1[6] = 0x1d;
                            stsBytes1[7] = 0x06;
                            TestPort.Write(ackBytes, 0, 6);
                            TestPort.Write(stsBytes1, 0, 12);
                            TestPort.Write(stsBytes2, 0, 12);
                            TestPort.Write(stsBytes3, 0, 11);
                            stsBytes1[7] = 0x00;
                            stsBytes1[6] = 0x00;
                            break;
                        case 28:  //operate (0x1C)
                            if (stby) xx = stsBytes1[5] ^ 0x02;
                            else xx = stsBytes1[5] | 0x02;
                            stsBytes1[5] = (byte)xx;
                            stby = !stby;
                            Thread.Sleep(100);
                            TestPort.Write(ackBytes, 0, 6);
                            break;
                        case 43:  //ant     (0x2B)
                            //SPETimer.Stop();
                            if (stsBytes2[10] >= 3) stsBytes2[10] = 0;
                            else stsBytes2[10] += 1;
                            TestPort.Write(ackBytes, 0, 6);
                            break;
                        case 45:  // <<<     (0x2D)
                            if (stsBytes1[8] <= 0) stsBytes1[8] = 8;
                            else stsBytes1[8] -= 1;
                            TestPort.Write(ackBytes, 0, 6);
                            TestPort.Write(stsBytes1, 0, 12);
                            TestPort.Write(stsBytes2, 0, 12);
                            TestPort.Write(stsBytes3, 0, 11);
                            break;
                        case 46:  // >>>     (0x2E)
                            if (stsBytes1[8] >= 8) stsBytes1[8] = 0;
                            else stsBytes1[8] += 1;
                            TestPort.Write(ackBytes, 0, 6);
                            TestPort.Write(stsBytes1, 0, 12);
                            TestPort.Write(stsBytes2, 0, 12);
                            TestPort.Write(stsBytes3, 0, 11);
                            break;
                        case 47:  // SET     (0x2F)
                            TestPort.Write(ackBytes, 0, 6);
                            //if (rcu)
                            //    stsBytes1[6] = 0x02;
                            //else
                            //    stsBytes1[6] = 0x06;
                            SetSPEmsg("Set Confirmed");
                            TestPort.Write(stsBytes1, 0, 12);
                            TestPort.Write(stsBytes2, 0, 12);
                            TestPort.Write(stsBytes3, 0, 11);
                            break;
                        case 52:  //tune    (0x34)
                            xx = stsBytes1[5] | 0x01;
                            stsBytes1[5] = (byte)xx;
                            TestPort.Write(stsBytes1, 0, 12);
                            TestPort.Write(stsBytes2, 0, 12);
                            TestPort.Write(stsBytes3, 0, 11);
                            Thread.Sleep(1000);
                            xx = stsBytes1[5] ^ 0x01;
                            stsBytes1[5] = (byte)xx;
                            TestPort.Write(ackBytes, 0, 6);
                            break;
                    }
                }
                if (data[4] == 128)  //RCU_ON (0x80)
                {
                    TestPort.Write(ackBytes, 0, 6);
                    SPETimer.Interval = 1000;
                    SPETimer.Start();
                }
                if (data[4] == 129)  // RCU_OFF (0x81)
                {
                    TestPort.Write(ackBytes, 0, 6);
                    SPETimer.Stop();
                }
                else
                    return;
            }
        }
        // load the status packet with test data 
        byte[] stsBytes1 = new byte[12] // 0-11
        { 0xAA, 0xAA, 0xAA, 0x1E, 0x00, 0xFF, 0x00, 0x00, 0x13, 0x15, 0x17, 0x18 };
        byte[] stsBytes2 = new byte[12] // 12-23
        { 0x1b, 0x1c, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, 0x37, 0x00, 0x67 };
        byte[] stsBytes3 = new byte[11] // 24-34
        { 0x00, 0x18, 0x10, 0x27, 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3c };
//        { 0x00, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3c };

        //private void LoadPkt()
        //{
        //    {
        //        string preamble = "AAAAAA"; // 0, 1, 2
        //        string cnt = "1E";          // 3
        //        string sts = "00";          // 4
        //        string flgs = "FF";         // 5
        //        string ctx = "1D";          // 6
        //        string dis = "00131517181B1C00000000"; // 7 - 17
        //        string bnd = "0000";        // 18 - 19
        //        string freq = "1437";       // 20 - 21 14100 khz
        //        string ant = "03";          // 22 ant 1 selected
        //        string swr = "6500";        // 23 - 24 Lo/Hi bytes
        //        string tmp = "18";          // 25 50C
        //        string pwrf = "0528";       // 26 - 27 fwd pwr Lo/Hi
        //        string pwrr = "0000";       // 28 - 29 ref pwr Lo/Hi
        //        string va = "0000";         // 30 - 31 pa volts Lo/Hi
        //        string ia = "0000";         // 32 - 33 pa current Lo/Hi
        //        string chk = "3B";          // 34 checksum
        //        string mystring = preamble + cnt + sts + flgs + ctx + dis + bnd + freq +
        //            ant + swr + tmp + pwrf + pwrr + va + ia + chk;
        //        int j = 0;
        //        for (int i = 0; i < 35; i++)
        //        {
        //            string stemp = mystring.Substring(j, 2);
        //            stsBytes[i] = byte.Parse(stemp, NumberStyles.HexNumber);
        //            j += 2;
        //        }
        //    }
        //}


        #endregion * Test Routines *

        #endregion SPE Amp

        #region State Machine

        #region # Declarations #

        private RIOXServer _server;
        private RadioData _radio;
        private string kwMode = "";
        //private double pollsec = 120;
        private System.Timers.Timer _rioxRefresh;

        // This table holds the ZZ cmds and state values
        private Hashtable State = new Hashtable();

        // This table holds the KenWood to PSDR CAT conversion
        private Hashtable kwCat = new Hashtable();

        // This table stores the CAT command and caller port if a State variable doesn't exist.        
        private ArrayList _caller = new ArrayList();
        
        // This table stores the commands and port that called and
        // will be sent to the radio by the pollTimer elapsed event.
        private Hashtable Poll = new Hashtable();

        #endregion # Declarations #

        #region # Delegates #

        #endregion # Delegates #

        #region # Events #

        void ServerCommandEvent(object sender, RIOXServer.CommandEventArgs e)
        {
            // command received
            Console.WriteLine("CommandEvent fired");
            if (e.Command == "ClientCmd")
            {
                if (e.Data.Substring(0, 2) == "ZZ")
                    WriteToPort(e.Data, iSleep);
                else if (e.Data.Substring(0, 2) == "DD")
                    ParseBuffer(e.Data);
            }
        }
        void RadioPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //Debug.WriteLine("RadioPropertyChanged: " + e.PropertyName);
            _server.SendObjectUpdate();
        }
        void _rioxRefresh_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
                _server.SendObjectUpdate();
        }

        #endregion # Events #

        #region # Methods #

        void StateInit()
        {
            // Load hash tables
            #region State Table

            try
            {
                State.Add("_ZZAC", null); // Tune Step
                State.Add("_ZZAG", null); // Audio Gain 
                State.Add("_ZZAI", null); // Auto Information function
                State.Add("_ZZAR", null); // AGC Threshold
                State.Add("_ZZBC", null); // Reads PowerSDR state
                State.Add("_ZZBG", null); // Band Group (HF/VHF)
                State.Add("_ZZBI", null); // Binaural (BIN) status
                State.Add("_ZZBR", null); // BCI Rejection button
                State.Add("_ZZBS", null); // RX1 Band
                State.Add("_ZZBT", null); // RX2 Band
                State.Add("_ZZCB", null); // Break-In checkbox status
                State.Add("_ZZCD", null); // Break-In Delay value
                State.Add("_ZZCF", null); // Show CW TX Filter checkbox
                State.Add("_ZZCI", null); // CW Iambic checkbox status
                State.Add("_ZZCL", null); // CW Pitch 
                State.Add("_ZZCM", null); // CW Monitor checkbox status
                State.Add("_ZZCP", null); // Compander (CPDR) status
                State.Add("_ZZCS", null); // CW Speed
                State.Add("_ZZCT", null); // Compander threshold
                State.Add("_ZZCU", null); // CPU usage
                State.Add("_ZZDA", null); // Display Average (AVG) status
                State.Add("_ZZDM", null); // Display Mode
                State.Add("_ZZDX", null); // Phone DX button status
                State.Add("_ZZEA", null); // RX EQ values
                State.Add("_ZZEB", null); // TX EQ values
                State.Add("_ZZER", null); // RX EQ status
                State.Add("_ZZET", null); // TX EQ button status
                State.Add("_ZZFA", null); // VFO A frequency
                State.Add("_ZZFB", null); // VFO B frequency
                State.Add("_ZZFH", null); // DSP High Filter
                State.Add("_ZZFI", null); // current RX1 DSP receive filter
                State.Add("_ZZFJ", null); // current RX2 DSP receive filter
                State.Add("_ZZFL", null); // DSP Low Filter
                State.Add("_ZZFM", null); // FlexRadio Model Number
                State.Add("_ZZFV", null); // Reads FlexWire single byte data
                State.Add("_ZZFW", null); // Reads FlexWire double byte data
                State.Add("_ZZGE", null); // Noise Gate button status
                State.Add("_ZZGL", null); // Noise Gate threshold
                State.Add("_ZZGT", null); // AGC Mode Selector
                State.Add("_ZZHA", null); // Audio Filter Size
                State.Add("_ZZHR", null); // DSP RX Filter Phone Size
                State.Add("_ZZHT", null); // DSP TX Filter Phone Size
                State.Add("_ZZHU", null); // DSP RX Filter CW Size
                State.Add("_ZZHV", null); // DSP TX Filter CW Size
                State.Add("_ZZHW", null); // DSP RX Filter Digital Size
                State.Add("_ZZHX", null); // DSP TX Filter Digital Size
                State.Add("_ZZIF", null); // transceiver status word
                State.Add("_KWIF", null); // ZZIF converted to Kenwood format (IF)
                State.Add("_N1IF", null); // ZZIF converted to N1MM format (IF)
                State.Add("_SMIF", null); // ZZIF converted to Station Master format (IF)
                State.Add("_ZZIS", null); // variable filter width slider
                State.Add("_ZZIT", null); // variable filter shift slider
                State.Add("_ZZKS", null); // CWX CW speed
                State.Add("_ZZMA", null); // Mute (MUT) status
                State.Add("_ZZMD", null); // current mode for RX1
                State.Add("_KWMD", null); // ZZMD converted to Kenwood format (MD)
                State.Add("_ZZME", null); // current mode for RX2
                State.Add("_ZZMG", null); // Mic Gain
                State.Add("_ZZMN", null); // DSP filter names and values
                State.Add("_ZZMO", null); // Monitor (MON) status
                State.Add("_ZZMR", null); // RX Meter mode
                State.Add("_ZZMS", null); // MultiRX Swap checkbox
                State.Add("_ZZMT", null); // TX Meter mode
                State.Add("_ZZMU", null); // MultiRX button status
                State.Add("_ZZNA", null); // Noise Blanker 1 (NB) status
                State.Add("_ZZNB", null); // Noise Blanker 2 (NB2) status
                State.Add("_ZZNL", null); // Noise Blanker 1 threshold
                State.Add("_ZZNM", null); // Noise Blanker 2 threshold
                State.Add("_ZZNR", null); // Noise Reduction (NR) status
                State.Add("_ZZNT", null); // Auto Notch Filter (ANF) status
                State.Add("_ZZOA", null); // antenna connected to RX1
                State.Add("_ZZOB", null); // antenna connected to RX2
                State.Add("_ZZOC", null); // antenna connected to the transmitter
                State.Add("_ZZOD", null); // Antenna Mode (Simple/Complex)
                State.Add("_ZZOE", null); // RX1 Loop
                State.Add("_ZZOF", null); // RCA TX relay jacks
                State.Add("_ZZOG", null); // TX relay enables
                State.Add("_ZZOH", null); // TX relay delays
                State.Add("_ZZOJ", null); // Antenna Lock Checkbox
                State.Add("_ZZPA", null); // Preamp Gain setting
                State.Add("_ZZPC", null); // Drive Level
                State.Add("_ZZPO", null); // Display Peak button
                State.Add("_ZZPS", null); // Start button status
                State.Add("_ZZPZ", null); // Display Zoom buttons
                State.Add("_ZZQM", null); // Quick Save Memory value
                State.Add("_ZZRA", null); // RTTY Offset Enable VFO A
                State.Add("_ZZRB", null); // RTTY Offset Enable VFO B
                State.Add("_ZZRF", null); // RIT frequency
                State.Add("_ZZRH", null); // RTTY DIGH Offset Frequency
                State.Add("_ZZRL", null); // RTTY DIGL Offset Frequency
                State.Add("_ZZRM", null); // Console Meter value
                State.Add("_ZZRS", null); // RX2 button status
                State.Add("_ZZRT", null); // RIT button status
                State.Add("_ZZRV", null); // primary input voltage
                State.Add("_ZZSM", null); // S Meter
                State.Add("_ZZSO", null); // Squelch on/off status
                State.Add("_ZZSP", null); // VFO Split button status
                State.Add("_ZZSQ", null); // Squelch level
                State.Add("_ZZSR", null); // Spur Reduction (SR) status
                State.Add("_ZZST", null); // frequency step size (Deprecated)
                State.Add("_ZZSW", null); // VFO A TX/VFO B TX buttons
                State.Add("_ZZTF", null); // Show TX Filter checkbox
                State.Add("_ZZTH", null); // TX Filter High setting
                State.Add("_ZZTL", null); // TX Filter Low setting
                State.Add("_ZZTO", null); // TUN Power Level
                State.Add("_ZZTP", null); // Transmit Profile
                State.Add("_ZZTS", null); // Flex5000 Temperature Sensor
                State.Add("_ZZTU", null); // Tune (TUN) status
                State.Add("_ZZTV", null); // Xmit VFO frequency
                State.Add("_ZZTX", null); // MOX button status
                State.Add("_ZZUA", null); // XVTR Band Button Names
                State.Add("_ZZVA", null); // VAC button status
                State.Add("_ZZVB", null); // VAC RX Gain
                State.Add("_ZZVC", null); // VAC TX Gain
                State.Add("_ZZVD", null); // VAC Sample Rate
                State.Add("_ZZVE", null); // VOX button status
                State.Add("_ZZVF", null); // VAC Stereo button status
                State.Add("_ZZVG", null); // VOX gain 
                State.Add("_ZZVH", null); // I/Q to VAC Checkbox 
                State.Add("_ZZVI", null); // VAC input cable
                State.Add("_ZZVL", null); // VFO Lock status
                State.Add("_ZZVM", null); // VAC driver
                State.Add("_ZZVN", null); // PowerSDR software version number
                State.Add("_ZZVO", null); // VAC output cable
                State.Add("_ZZWA", null); // Mixer Mic Level
                State.Add("_ZZWB", null); // Mixer Line In RCA Level
                State.Add("_ZZWC", null); // Mixer Line In Phono Level
                State.Add("_ZZWD", null); // Mixer Line In DB9 Level
                State.Add("_ZZWE", null); // Mixer Mic Select Checkbox
                State.Add("_ZZWF", null); // Mixer Line In RCA Select Checkbox
                State.Add("_ZZWG", null); // Mixer Line In Phono Select Checkbox
                State.Add("_ZZWH", null); // Mixer Line In DB9 Select Checkbox
                State.Add("_ZZWJ", null); // Mixer Input Mute All Button
                State.Add("_ZZWK", null); // Mixer Internal Speaker Level
                State.Add("_ZZWL", null); // Mixer External Speaker Level
                State.Add("_ZZWM", null); // Mixer Headphone Level
                State.Add("_ZZWN", null); // Mixer Line Out RCA Level
                State.Add("_ZZWO", null); // Mixer Internal Speaker Select Checkbox
                State.Add("_ZZWP", null); // Mixer External Speaker Select Checkbox
                State.Add("_ZZWQ", null); // Mixer Headphone Select Checkbox
                State.Add("_ZZWR", null); // Mixer Line Out RCA Select Checkbox
                State.Add("_ZZWS", null); // Mixer Output Mute All Button
                State.Add("_ZZXF", null); // XIT frequency
                State.Add("_ZZXS", null); // XIT button status
                State.Add("_ZZXT", null); // X2TR button status

            }
            catch
            { }
            #endregion State Table

            #region Kenwood CAT Table

            // equiv old kw command reassignment
            kwCat.Add("AG0", "ZZAG");
            kwCat.Add("AI", "ZZAI");
            kwCat.Add("DM", "ZZDM");
            kwCat.Add("FA", "ZZFA");
            kwCat.Add("FB", "ZZFB");
            kwCat.Add("FR", "ZZSW");
            kwCat.Add("GT", "ZZGT");
            kwCat.Add("IF", "KWIF");    // requires conversion to/from "ZZ"
            kwCat.Add("KS", "ZZKS");
            kwCat.Add("KY", "ZZKY");
            kwCat.Add("MD", "KWMD");    // requires conversion to/from "ZZ"
            kwCat.Add("MG", "ZZMG");
            kwCat.Add("MO", "ZZMO");
            kwCat.Add("MU", "ZZMU");
            kwCat.Add("NB", "ZZNB");
            kwCat.Add("NT", "ZZNT");
            kwCat.Add("PC", "ZZPC");
            kwCat.Add("PS", "ZZPS");
            kwCat.Add("QS", "ZZQS");    
            kwCat.Add("RD", "ZZRD");
            kwCat.Add("RT", "ZZRT");
            kwCat.Add("RU", "ZZRU");
            kwCat.Add("SM0", "ZZSM");
            kwCat.Add("SQ0", "ZZSQ");
            kwCat.Add("XT", "ZZXS");

            #endregion Kenwood CAT Table

            Poll.Add("ZZDU", DateTime.Now); // got to have these commands as a base.
            try
            {
                // setup the RIOX server
                _radio = new RadioData();
                _server = new RIOXServer(_radio, System.Net.IPAddress.Loopback, 1234);
                _server.CommandEvent += ServerCommandEvent;
                _radio.PropertyChanged += RadioPropertyChanged;
                _server.SendUpdatesAtInterval = false;
                _server.Start();
                Console.WriteLine("Server started");

                // setup the RIOX refresh timer
                _rioxRefresh = new System.Timers.Timer();
                _rioxRefresh.Elapsed += new System.Timers.ElapsedEventHandler(_rioxRefresh_Elapsed);
                _rioxRefresh.Interval = 60000; // 1 minute
                _rioxRefresh.Start();

                pollTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                    "Unable to write to log");
            }
        }
        
        // Parse CAT command and return state value to caller
        string CatParse(SerialPort caller, string cmd)
        {
            if (!formLoaded) return null;
            string bCmd = ""; // base command ZZxx
            string nCmd = ""; // conversion to ZZ cmd from kwCat table
            string sCmd = ""; // add "_" to ZZ cpmmand
            try
            {
                if (cmd.Substring(0, 2) == "ZZ")
                {
                    if (cmd == "ZZID")
                    {
                        sp.Write(cmd + ";");
                        return null;
                    }
                    if (cmd.Length == 4)
                    {   
                        sCmd =  "_" + cmd;
                        // see if cmd is in the state table
                        if (State[sCmd] != null && State[sCmd].ToString() != "")
                        {
                            // if command is NOT one of these update the poll list.
                            // these are ZZDU variables
                            if (cmd != "ZZAC" && cmd != "ZZAG" && cmd != "ZZBS" && cmd != "ZZBT" &&
                                cmd != "ZZCU" && cmd != "ZZDM" && cmd != "ZZFA" && cmd != "ZZFB" &&
                                cmd != "ZZFI" && cmd != "ZZFJ" && cmd != "ZZGT" && cmd != "ZZKS" &&
                                cmd != "ZZMD" && cmd != "ZZME" && cmd != "ZZMU" && cmd != "ZZOA" &&
                                cmd != "ZZOB" && cmd != "ZZOC" && cmd != "ZZOF" && cmd != "ZZPC" &&
                                cmd != "ZZRF" && cmd != "ZZRS" && cmd != "ZZRT" && cmd != "ZZRV" &&
                                cmd != "ZZSM" && cmd != "ZZSP" && cmd != "ZZSW" && cmd != "ZZTO" &&
                                cmd != "ZZTS" && cmd != "ZZTU" && cmd != "ZZTX" && cmd != "ZZXF" &&
                                cmd != "ZZXT" && cmd != "ZZIF")
                            {
                                if (Poll.ContainsKey(cmd))
                                {
                                    Poll[cmd] = DateTime.Now;
                                }
                                else
                                {
                                    Poll.Add(cmd, DateTime.Now);
                                    sp.Write(cmd + ";");
                                }
                            }
                            return State[sCmd].ToString();
                        }
                        else 
                        {
                            // not in the state table, is a set cmd send to radio and return
                            if (cmd == "ZZBD" || cmd == "ZZBU" || cmd == "ZZBY" || cmd == "ZZIU" ||
                                cmd == "ZZPD" || cmd == "ZZQR" || cmd == "ZZQS" || cmd == "ZZSA" ||
                                cmd == "ZZSB" || cmd == "ZZSD" || cmd == "ZZSD" || cmd == "ZZSG" ||
                                cmd == "ZZSH" || cmd == "ZZSS" || cmd == "ZZSU" || cmd == "ZZTI" ||
                                cmd == "ZZXC" || cmd == "ZZZB")
                            {
                                sp.Write(cmd + ";"); // these are special 4 digit set commands
                                return null;
                            }
                            // not a ZZDU cmd. send to radio, save caller, add to poll list.
                            sp.Write(cmd + ";");
                            Caller c = new Caller() { port = (SerialPort)caller, zzcat = cmd, kwcat = null };
                            _caller.Add(c);

                            if (Poll.ContainsKey(cmd))
                            {
                                Poll[cmd] = DateTime.Now;
                            }
                            else
                            {
                                Poll.Add(cmd, DateTime.Now);
                                sp.Write(cmd + ";");
                            }
                            return null;
                        }
                    }
                    else if (cmd.Length > 4) 
                    {   //either a special Get cmd or a Set cmd
                        bCmd = cmd.Substring(0, 4);
                        sCmd = "_" + bCmd;
                        if (bCmd == "ZZFV" || bCmd == "ZZFW" || bCmd == "ZZMN" || 
                            bCmd == "ZZRM")
                        {
                            if (State[sCmd] != null && State[sCmd].ToString() != "")
                            {
                                if (Poll.ContainsKey(cmd))
                                {
                                    Poll[cmd] = DateTime.Now;
                                }
                                else
                                {
                                    Poll.Add(cmd, DateTime.Now);
                                    sp.Write(cmd + ";");
                                }
                                string val = State[sCmd].ToString();
                                if (bCmd == "ZZRM") 
                                    return val.Substring(1, val.Length-1);
                                else
                                    return val.Substring(2, val.Length-2);
                            }
                            else // no value in state
                            {
                                sp.Write(cmd + ";");
                                Caller c = new Caller() { port = (SerialPort)caller, zzcat = bCmd, kwcat = null };
                                _caller.Add(c);

                                if (Poll.ContainsKey(cmd))
                                {
                                    Poll[cmd] = DateTime.Now;
                                }
                                else
                                {
                                    Poll.Add(cmd, DateTime.Now);
                                    sp.Write(cmd + ";");
                                }
                                return null;
                            }
                        }
                        else if (bCmd == "ZZSM")
                            return State["_ZZSM"].ToString();

                        else  // it's a Set command
                        {
                            sp.Write(cmd + ";");
                            return null;
                        }
                    }
                    return null;
                }
                else if (cmd.Substring(0, 2) != "ZZ" && cmd.Length == 2)
                { // must be an kw Get/Set cmd
                    switch (cmd)
                    {   // check for special kw commands that require no further action
                        case "BD": sp.Write("ZZBD;"); return null; // Set only
                        case "BU": sp.Write("ZZBU;"); return null; // Set only
                        case "DN": sp.Write("ZZSA;"); return null; // Set only
                        case "FT": return State["_KWIF"].ToString().Substring(31, 1);
                        case "FW": return "?"; //Obsolete
                        case "ID":  // returns Flex transceiver model
                            switch (State["_ZZFM"].ToString())
                            {
                                case "0":
                                    return "SDR1K";
                                case "1":
                                    return "F5K";
                                case "2":
                                    return "F3K";
                                case "3":
                                    return "F1500";
                            }
                            break;
                        case "IF": return State["_KWIF"].ToString();
                        case "MD": return State["_KWMD"].ToString();
                        case "PR": return "?"; //Obsolete
                        case "PT": return "?"; //Obsolete
                        case "QS": sp.Write("ZZQS;"); return null; // Set only
                        case "RC": sp.Write("ZZRC;"); return null; // Set only
                        case "RX": sp.Write("ZZTX0;"); return null; // Set only
                        case "TX": sp.Write("ZZTX1;"); return null; // Set only
                        case "SH": return "?"; //Obsolete
                        case "SL": return "?"; //Obsolete
                        case "UP": sp.Write("ZZSB;"); return null; // Set only
                    }
                    if (kwCat.ContainsKey(cmd)) // is there a conversion character?
                    {
                        // subsitute ZZ cmd for kw cmd
                        nCmd = kwCat[cmd].ToString();
                        sCmd = "_" + nCmd;
                        if (State[sCmd] != null && State[sCmd].ToString() != "")
                        {
                            // if command is NOT one of these update the poll list.
                            if (cmd != "DM" && cmd != "GT" && cmd != "FA" &&
                                cmd != "FB" && cmd != "FT" && 
                                cmd != "KS" && cmd != "MU" && cmd != "PC" && 
                                cmd != "RF" && cmd != "RT" && cmd != "XS")
                            {
                                if (Poll.ContainsKey(nCmd))
                                {
                                    Poll[nCmd] = DateTime.Now;
                                }
                                else
                                {
                                    Poll.Add(nCmd, DateTime.Now);
                                    sp.Write(nCmd + ";");
                                }
                            }
                            if (cmd == "GT")
                                return State[sCmd].ToString().PadLeft(3, '0');
                            else
                                return State[sCmd].ToString();
                        }
                        else
                        {   // is a rqst for new state var, convert to ZZ & send to radio
                            sp.Write(nCmd + ";");
                            Caller c = new Caller() { port = (SerialPort)caller, zzcat = nCmd, kwcat = cmd };
                            _caller.Add(c);

                            // and add to Poll table
                            if (Poll.ContainsKey(nCmd))
                            {
                                Poll[nCmd] = DateTime.Now;
                            }
                            else
                            {
                                Poll.Add(nCmd, DateTime.Now);
                                sp.Write(nCmd + ";");
                            }
                            return null;
                        }
                    }
                    else
                    { // must be a bad 2 char command
                        return null;
                    }
                }
                else if (cmd.Substring(0, 2) != "ZZ" && cmd.Length > 2) 
                {
                    if (cmd == "AG0" || cmd == "SM0" || cmd == "SQ0")
                    {   //special 3 digit get cmds
                        nCmd = kwCat[cmd].ToString();
                        sCmd = "_" + nCmd;
                        if (State[sCmd.Substring(0, 5)] != null && State[sCmd.Substring(0, 5)].ToString() != "")
                        {
                            if (cmd == "SM0")
                            {
                                string s1 = State[sCmd.Substring(0, 5)].ToString().TrimStart('0');
                                double s2 = Convert.ToDouble(s1.ToString());
                                int sm = Convert.ToInt32(Math.Round(s2 / 10));
                                return sm.ToString().PadLeft(4,'0');
                            }
                            if (Poll.ContainsKey(nCmd))
                            {
                                Poll[nCmd] = DateTime.Now;
                            }
                            else
                            {
                                Poll.Add(nCmd, DateTime.Now);
                                sp.Write(nCmd + ";");
                            }

                            return State[sCmd.Substring(0, 5)].ToString();
                        }
                        else // not in state table
                        {
                            Caller c = new Caller() { port = (SerialPort)caller, zzcat = nCmd, kwcat = cmd };
                            _caller.Add(c);

                            if (Poll.ContainsKey(nCmd))
                            {
                                Poll[nCmd] = DateTime.Now;
                            }
                            else
                            {
                                Poll.Add(nCmd, DateTime.Now);
                                sp.Write(nCmd + ";");
                            }
                            return null;
                        }
                    }
                    else    // not 3 digit special, must be a regular kw set command
                    {
                        if (cmd.Substring(0, 2) == "FT")
                        {
                            sp.Write("ZZSW" + cmd.Substring(2, 1) + ";");
                            return null;
                        }
                        sp.Write(cmd + ";");
                        return null;
                    }
               }
                else
                    return null;
            }
            catch (NullReferenceException)
            { return null; }

            catch (Exception e)
            {
                MessageBox.Show("There was an error parsing the command " + cmd +
                    ": " + "\r\r" + e.Message + e.StackTrace,
                    "Cat Parse Error");
                return null;
            }
        }

        /*
ZZDU0:0:0:0:0:0:0:0:0:1:3:0:0:06:07:00:00:04:000:000:059:020:026:022:010:0000:000:+0000:00000:+0000:042.86:00014070000:00007000000;
         */
        // Set the state values if radio is a F5K
        void ParseZZDU()
        {
            try
            {
                bool tmp = false;
                //VFO A
                State["_ZZFA"] = rawFreq.Substring(107, 11);
                if (String.Compare(State["_ZZFA"].ToString(), _radio.vfoa) != 0)
                {
                    vfoA = _radio.vfoa = State["_ZZFA"].ToString();
                    //if (tkVfoA == "") tkVfoA = vfoA;
                }
                // VFO B
                State["_ZZFB"] = rawFreq.Substring(119, 11);
                if (String.Compare(State["_ZZFB"].ToString(), _radio.vfob) != 0)
                {
                    vfoB = _radio.vfob = State["_ZZFB"].ToString();
                    int bFreq = Convert.ToInt32(vfoB.TrimStart('0'));
                         if (bFreq >= 1800000 && bFreq <= 2000000) bBand = "160";
                    else if (bFreq >= 3500000 && bFreq <= 4000000) bBand = "080";
                    else if (bFreq >= 7000000 && bFreq <= 7300000) bBand = "040";
                    else if (bFreq >= 10100000 && bFreq <= 10150000) bBand = "030";
                    else if (bFreq >= 14000000 && bFreq <= 14350000) bBand = "020";
                    else if (bFreq >= 18068000 && bFreq <= 18168000) bBand = "017";
                    else if (bFreq >= 21000000 && bFreq <= 21450000) bBand = "015";
                    else if (bFreq >= 24890000 && bFreq <= 24990000) bBand = "012";
                    else if (bFreq >= 28000000 && bFreq <= 30000000) bBand = "010";
                    else if (bFreq >= 50000000 && bFreq <= 54000000) bBand = "006";
                }
                // TX VFO
                State["_ZZSW"] = rawFreq.Substring(4, 1);
                if (String.Compare(State["_ZZSW"].ToString(), _radio.vfotx) != 0)
                {
                    _radio.vfotx = State["_ZZSW"].ToString();
                    ZZSW = Convert.ToInt32(State["_ZZSW"].ToString());
                    if (chkSoEnab.Checked)
                    {
                        if (LastZZSW != ZZSW)
                        {
                            LastZZSW = ZZSW; 
                            zzswChanged(ZZSW);
                            DoRx1Tap();
                        }
                    }
                }
                //SPLIT STATUS
                State["_ZZSP"] = rawFreq.Substring(6, 1);
                    if (State["_ZZSP"].ToString() == "0")
                    {
                        tmp = false;
                        //logFreq = State["_ZZFA"].ToString();
                    }
                    else
                    {
                        tmp = true;
                        //logFreq = State["_ZZFB"].ToString();
                    }

                    if (tmp != _radio.split)
                    {
                        _radio.split = tmp;
                        if (!tmp && chkCwTx.Checked) 
                            WriteToPort("ZZCF0;", iSleep);
                        else if (tmp && chkCwTx.Checked) 
                            WriteToPort("ZZCF1;", iSleep);
                    }
                //TUNE STATUS
                State["_ZZTU"] = rawFreq.Substring(8, 1);
                if (State["_ZZTU"].ToString() == "0") tmp = false;
                else tmp = true;
                if (tmp != _radio.tun)
                    _radio.tun = tmp;

                //MOX STATUS
                State["_ZZTX"] = rawFreq.Substring(10, 1);
                if (State["_ZZTX"].ToString() == "0") tmp = false;
                else tmp = true;
                if (tmp != _radio.mox)
                    _radio.mox = tmp;
                if (tmp && chkDog.Checked && !WatchDog.Enabled)
                    WatchDog.Start();
                else
                    WatchDog.Stop();

                //RX1 ANTENNA
                State["_ZZOA"] = rawFreq.Substring(12, 1);
                if (String.Compare(State["_ZZOA"].ToString(), _radio.antr1) != 0)
                {
                    _radio.antr1 = State["_ZZOA"].ToString();
                    Rx1 = State["_ZZOA"].ToString();
                }
                //RX2 ANTENNA
                State["_ZZOB"] = rawFreq.Substring(14, 1);
                if (String.Compare(State["_ZZOB"].ToString(), _radio.antr2) != 0)
                {
                    _radio.antr2 = State["_ZZOB"].ToString();
                    Rx2 = State["_ZZOB"].ToString();
                }
                //TX ANTENNA
                State["_ZZOC"] = rawFreq.Substring(16, 1);
                if (String.Compare(State["_ZZOC"].ToString(), _radio.txant) != 0)
                {
                    _radio.txant = State["_ZZOC"].ToString();
                    TxA = State["_ZZOC"].ToString();
                }
                //RX2 Status
                State["_ZZRS"] = rawFreq.Substring(18, 1);
                if (State["_ZZRS"].ToString() == "0") tmp = false;
                else tmp = true;
                if (tmp != _radio.rx2sts)
                    _radio.rx2sts = tmp;

                //RIT STATUS
                State["_ZZRT"] = rawFreq.Substring(20, 1);
                if (State["_ZZRT"].ToString() == "0") tmp = false;
                else tmp = true;
                if (tmp != _radio.ritsts)
                    _radio.ritsts = tmp;

                //Display Mode
                State["_ZZDM"] = rawFreq.Substring(22, 1);
                if (String.Compare(State["_ZZDM"].ToString(), _radio.dispmode) != 0)
                    _radio.dispmode = State["_ZZDM"].ToString();

                //AGC Setting
                State["_ZZGT"] = rawFreq.Substring(24, 1);
                if (String.Compare(State["_ZZGT"].ToString(), _radio.agc) != 0)
                    _radio.agc = State["_ZZGT"].ToString();

                //Multi RX STATUS
                State["_ZZMU"] = rawFreq.Substring(26, 1);
                if (State["_ZZMU"].ToString() == "0") tmp = false;
                else tmp = true;
                if (tmp != _radio.mulrxsts)
                    _radio.mulrxsts = tmp;
                //XIT STATUS
                State["_ZZXS"] = rawFreq.Substring(28, 1);
                if (State["_ZZXS"].ToString() == "0") tmp = false;
                else tmp = true;
                if (tmp != _radio.xitsts)
                    _radio.xitsts = tmp;
                //TUNE STEP
                State["_ZZAC"] = rawFreq.Substring(30, 2);
                if (String.Compare(State["_ZZAC"].ToString(), _radio.tunstp) != 0)
                {
                    _radio.tunstp = State["_ZZAC"].ToString();
                    SetStepSize(State["_ZZAC"].ToString());
                }
                //RX1 MODE
                State["_ZZMD"] = rawFreq.Substring(33, 2);
                if (String.Compare(State["_ZZMD"].ToString(), _radio.moder1) != 0)
                {
                    sdrMode = ModeR1 = _radio.moder1 = State["_ZZMD"].ToString();
                    DoModeChg();
                }
                //RX2 MODE
                State["_ZZME"] = rawFreq.Substring(36, 2);
                if (String.Compare(State["_ZZME"].ToString(), _radio.moder2) != 0)
                    _radio.moder2 = State["_ZZME"].ToString();

                //RX1 DSP FILTER
                State["_ZZFI"] = rawFreq.Substring(42, 2);
                if (String.Compare(State["_ZZFI"].ToString(), _radio.fltr1) != 0)
                    _radio.fltr1 = State["_ZZFI"].ToString();
                FltRx1 = State["_ZZFI"].ToString();

                //RX2 DSP FILTER
                State["_ZZFJ"] = rawFreq.Substring(39, 2);
                if (String.Compare(State["_ZZFJ"].ToString(), _radio.fltr2) != 0)
                    _radio.fltr2 = State["_ZZFJ"].ToString();

                //TX LINES
                State["_ZZOF"] = rawFreq.Substring(45, 3);
                if (String.Compare(State["_ZZOF"].ToString(), _radio.txline) != 0)
                    _radio.txline = State["_ZZOF"].ToString();

                //RX2 BAND
                State["_ZZBT"] = rawFreq.Substring(49, 3);
                if (String.Compare(State["_ZZBT"].ToString(), _radio.bandr2) != 0)
                {
                    _radio.bandr2 = State["_ZZBT"].ToString();
                    if (chkSoEnab.Checked)
                    {
                        SetVfoB();
                        DoRx1Tap();
                    }
                }
                //DRIVE LEVEL
                State["_ZZPC"] = rawFreq.Substring(53, 3);
                if (String.Compare(State["_ZZPC"].ToString(), _radio.drive) != 0)
                    _radio.drive = State["_ZZPC"].ToString();

                //RX1 BAND
                State["_ZZBS"] = rawFreq.Substring(57, 3);
                if (String.Compare(State["_ZZBS"].ToString(), _radio.bandr1) != 0)
                {
                    _radio.bandr1 = State["_ZZBS"].ToString();
                    band = State["_ZZBS"].ToString();
                    DoBandChg(band);
                    if (ZZSW == 1 && chkSoEnab.Checked)
                    {
                        SetVfoB();
                        DoRx1Tap();
                    }
                }
                //AUDIO GAIN
                State["_ZZAG"] = rawFreq.Substring(61, 3);
                if (String.Compare(State["_ZZAG"].ToString(), _radio.vol) != 0)
                {
                    if (State["_ZZAG"].ToString() != null || State["_ZZAG"].ToString() != "")
                    {
                        _radio.vol = State["_ZZAG"].ToString();
                        volIdx = Convert.ToInt32(State["_ZZAG"].ToString());
                    }
                }
                //CW SPEED
                State["_ZZKS"] = rawFreq.Substring(65, 3);
                if (String.Compare(State["_ZZKS"].ToString(), _radio.cwx) != 0)
                    _radio.cwx = State["_ZZKS"].ToString();

                //Tune POWER
                State["_ZZTO"] = rawFreq.Substring(69, 3);
                if (String.Compare(State["_ZZTO"].ToString(), _radio.tunpwr) != 0)
                    _radio.tunpwr = State["_ZZTO"].ToString();

                //RADIO SUPPLY VOLTS
                State["_ZZRV"] = rawFreq.Substring(73, 4);
                if (String.Compare(State["_ZZRV"].ToString(), _radio.volts) != 0)
                {
                    _radio.volts = State["_ZZRV"].ToString();
                    SetVolts(State["_ZZRV"].ToString());
                }
                //S-Meter
                State["_ZZSM"] = rawFreq.Substring(78, 3);
                if (String.Compare(State["_ZZSM"].ToString(), _radio.smtr) != 0)
                    _radio.smtr = State["_ZZSM"].ToString();

                //RIT FREQ
                State["_ZZRF"] = rawFreq.Substring(82, 5);
                if (String.Compare(State["_ZZRF"].ToString(), _radio.ritfreq) != 0)
                {
                    _radio.ritfreq = State["_ZZRF"].ToString();
                    if (_radio.ritfreq == "+0000") RITfrq = 0;
                }

                //PA TEMPERATURE
                State["_ZZTS"] = rawFreq.Substring(88, 5);
                if (String.Compare(State["_ZZTS"].ToString(), _radio.temp) != 0)
                {
                    if (State["_ZZTS"].ToString() != null || State["_ZZTS"].ToString() != "")
                    {
                        _radio.temp = State["_ZZTS"].ToString();
                        temp = Convert.ToDouble(State["_ZZTS"].ToString());
                        WriteTemp();
                    }
                }
                //XIT FREQ
                State["_ZZXF"] = rawFreq.Substring(94, 5);
                if (String.Compare(State["_ZZXF"].ToString(), _radio.xitfreq) != 0)
                {
                    _radio.xitfreq = State["_ZZXF"].ToString();
                    if (_radio.xitfreq == "+0000") XITfrq = 0;
                }

                //CPU USAGE
                State["_ZZCU"] = rawFreq.Substring(100, 5);
                if (String.Compare(State["_ZZCU"].ToString(), _radio.cpu) != 0)
                    _radio.cpu = State["_ZZCU"].ToString();
               
                BuildKWIF();
                BuildN1IF();
                BuildZZIF();
                if (chkRepeat.Checked && rbStnMstr.Checked) 
                    BuildSMIF();
            }
            catch
            { }
        }

        // build the ZZIF command
        void BuildZZIF()
        {
            string str = "";
            str += State["_ZZFA"].ToString();
            str += AC2ST(State["_ZZAC"].ToString());
            // RIT freq work around
            str += State["_ZZRF"].ToString().Substring(0, 1);
            str += "0"; // dummy for ZZRF missing digit
            str += State["_ZZRF"].ToString().Substring(1, 4);
            // RIT freq work around
            str += State["_ZZRT"].ToString();
            str += "0"; // Dummy XIT Status
            str += "0"; // Channel Bank number (dummied)
            str += "00"; // Channel Bank number  (dummied)
            str += State["_ZZTX"].ToString();
            str += State["_ZZMD"].ToString();
            //if (State["_ZZRS"].ToString() == "1")
            //    str += State["_ZZSW"].ToString(); // Split Status
            //else
                str += "0";
            str += "0"; // Scan Ststus (dummied)
            //VFO Split Status
            //if (chkN1MM.Checked && State["_ZZRS"].ToString() == "1" && State["_ZZSW"].ToString() == "1")
            //    str += "0";
            //else
                str += State["_ZZSP"].ToString();
            str += "0"; // CTCSS (dummied)
            str += "00"; // more Tone Controls (dummied)
            str += "0"; // Shift Status (dummied)
            State["_ZZIF"] = str;
        }

        // build the Kenwood IF command
        void BuildKWIF()
         {
            str = "";
            str += State["_ZZFA"].ToString();
            str += AC2ST(State["_ZZAC"].ToString());
            // RIT freq work around
            str += State["_ZZRF"].ToString().Substring(0, 1);
            str += "0"; // dummy for ZZRF missing digit
            str += State["_ZZRF"].ToString().Substring(1, 4);
            // RIT freq work around
            str += State["_ZZRT"].ToString();
            str += "0"; // Dummy XIT Status
            str += "0"; // Channel Bank number (dummied)
            str += "00"; // Channel Bank number  (dummied)
            str += State["_ZZTX"].ToString();
            str += kwMode; // Mode
            //if (State["_ZZRS"].ToString() == "1")
            //    str += State["_ZZSW"].ToString(); 
            //else
            str += "0"; // Rec VFO Status (dummied)
            //    str += "0";
            str += "0"; // Scan Ststus (dummied)
            
            //if (chkN1MM.Checked && State["_ZZRS"].ToString() == "1" && State["_ZZSW"].ToString() == "1")
            //    str += "0";
            //else
                str += State["_ZZSP"].ToString(); //VFO Split Status
            str += "0"; // CTCSS (dummied)
            str += "00"; // more Tone Controls (dummied)
            str += "0"; // Shift Status (dummied)
            State["_KWIF"] = str;
        }

        // build the N1MM IF command
        void BuildN1IF()
        {
            str = "";
            str += State["_ZZFB"].ToString();
            str += AC2ST(State["_ZZAC"].ToString());
            // RIT freq work around
            str += State["_ZZRF"].ToString().Substring(0, 1);
            str += "0"; // dummy for ZZRF missing digit
            str += State["_ZZRF"].ToString().Substring(1, 4);
            // RIT freq work around
            str += State["_ZZRT"].ToString();
            str += "0"; // Dummy XIT Status
            str += "0"; // Channel Bank number (dummied)
            str += "00"; // Channel Bank number  (dummied)
            str += State["_ZZTX"].ToString();
            str += kwMode; // Mode
            if (State["_ZZRS"].ToString() == "1") // if RX2 is on
                str += State["_ZZSW"].ToString(); // TX VFO Status
            else
                str += "0";
            str += "0"; // Scan Ststus (dummied)
            //VFO Split Status
            if (State["_ZZRS"].ToString() == "1" && State["_ZZSW"].ToString() == "1")
                str += "0";
            else
                str += State["_ZZSP"].ToString();
            str += "0"; // CTCSS (dummied)
            str += "00"; // more Tone Controls (dummied)
            str += "0"; // Shift Status (dummied)
            State["_N1IF"] = str;
        }

        // build the Station Master IF command
        void BuildSMIF()
        {
            str = "";
            str += State["_ZZFB"].ToString();
            str += AC2ST(State["_ZZAC"].ToString());
            // RIT freq work around
            str += State["_ZZRF"].ToString().Substring(0, 1);
            str += "0"; // dummy for ZZRF missing digit
            str += State["_ZZRF"].ToString().Substring(1, 4);
            // RIT freq work around
            str += State["_ZZRT"].ToString();
            str += "0"; // Dummy XIT Status
            str += "0"; // Channel Bank number (dummied)
            str += "00"; // Channel Bank number  (dummied)
            str += State["_ZZTX"].ToString();
            str += kwMode; // Mode
            if (State["_ZZRS"].ToString() == "1")
                str += State["_ZZSW"].ToString(); // Split Status
            else
                str += "0";
            str += "0"; // Scan Ststus (dummied)
            //VFO Split Status
            if (State["_ZZRS"].ToString() == "1" && State["_ZZSW"].ToString() == "1")
                str += "0";
            else
                str += State["_ZZSP"].ToString();
            str += "0"; // CTCSS (dummied)
            str += "00"; // more Tone Controls (dummied)
            str += "0"; // Shift Status (dummied)
            State["_SMIF"] = str;
        }

        // Mode has changed, do housekeeping
        void DoModeChg()
        {
            // Lookup the correct SDR mode
            switch (sdrMode)
            {
                case "00": memMode = "LSB"; kwMode = "1"; break;
                case "01": memMode = "USB"; kwMode = "2"; break;
                case "02": memMode = "DSB"; break;
                case "03": memMode = "CWL"; kwMode = "7"; break;
                case "04": memMode = "CWU"; kwMode = "3"; break;
                case "05": memMode = "FMN"; kwMode = "4"; break;
                case "06": memMode = "AM"; kwMode = "5"; break;
                case "07": memMode = "DIGU"; kwMode = "9"; break;
                case "08": memMode = "SPEC"; break;
                case "09": memMode = "DIGL"; kwMode = "6"; break;
                case "10": memMode = "SAM"; break;
                case "11": memMode = "DRM"; break;
                default: memMode = "xxx"; break;
            }
            State["_KWMD"] = kwMode;
            // Execute macro if Mode Change is checked
            if (LastMode != sdrMode && chkModeChg.Checked)
            {
                switch (sdrMode)
                {
                    case "00": ProcessMacroButton(26); break;   // LSB
                    case "01": ProcessMacroButton(26); break;   // USB
                    case "03": ProcessMacroButton(27); break;   // CWU
                    case "04": ProcessMacroButton(27); break;   // CWL
                    case "06": ProcessMacroButton(25); break;   // AM
                    case "07": ProcessMacroButton(28); break;   // RTTY (DIGL)
                    case "09": ProcessMacroButton(28); break;   // RTTY-R (DIGU)
                    default: break;
                }
            }
            // If Auto Drive Expert mode is checked calc mode & change
            if (LastMode != sdrMode && chkAutoExpert.Checked)
            {
                if (sdrMode == "00" || sdrMode == "01")
                { modeFactor = (Convert.ToDouble(set.txtSSB) / 100) + 1; }
                else if (sdrMode == "03" || sdrMode == "04")
                    modeFactor = 1;
                else if (sdrMode == "06")
                { modeFactor = (Convert.ToDouble(set.txtAM) / 100) + 1; }
                else if (sdrMode == "07" || sdrMode == "09")
                { modeFactor = (Convert.ToDouble(set.txtDigi) / 100) + 1; }
                else modeFactor = 1;
                lastBand = "";
                DoBandChg(band);
            }
        }

        // Band has changed, do housekeeping
        void DoBandChg(string band)
        {
            if (lastBand != band)
            {   // note that BtnBypass_Click sets lastBand to ""
                // to force loading of power settings.
                lastBand = band;
                //tkVfoA = vfoA;
                //tkVfoB = vfoB;
                //tkVfoX = State["_ZZTV"].ToString();

                /*** Set SO2R vfo A band data ***/
                if (chkSoEnab.Checked)
                {
                    SetVfoA(); //SetVfoB();
                }

                /*** Setup Auto Drive for this band ***/
                if (stsOper)
                {   // if amp not selected for this band set PTT to ByPass

                    if (!chkAmp160.Checked && band == "160") { btnByp_Click(null, null); }
                    if (!chkAmp80.Checked && band == "080") { btnByp_Click(null, null); }
                    if (!chkAmp40.Checked && band == "040") { btnByp_Click(null, null); }
                    if (!chkAmp30.Checked && band == "030") { btnByp_Click(null, null); }
                    if (!chkAmp20.Checked && band == "020") { btnByp_Click(null, null); }
                    if (!chkAmp17.Checked && band == "017") { btnByp_Click(null, null); }
                    if (!chkAmp15.Checked && band == "015") { btnByp_Click(null, null); }
                    if (!chkAmp12.Checked && band == "012") { btnByp_Click(null, null); }
                    if (!chkAmp10.Checked && band == "010") { btnByp_Click(null, null); }
                    if ( !chkAmp6.Checked && band == "006") { btnByp_Click(null, null); }
                    // see if band has changed since last time through
                    // note: BtnBypass_Click sets lastBand to ""
                    // to force loading of power settings. The COBC var is used
                    // to tell if this is btnBypass toggle or a real band change.
                    // if band has NOT changed put PTT back online.
                    if (chkNoBdChg.Checked && lastBand != "" && COBC != band)
                    {
                        btnByp_Click(null, null);
                        COBC = band;
                    }
                }
                if (stsOper && chkAutoDrv.Checked)
                {   // if the amp is in Operate mode, get the stored power
                    // setting from the settings file and send to the radio.
                    double pwrCor = 0;
                    if (!chkAutoExpert.Checked) modeFactor = 1;
                    if (rbAmp1.Checked)
                    {
                        switch (band)
                        {
                            case "160": pwrCor = Convert.ToInt32(set.pwr1) * modeFactor; break;
                            case "080": pwrCor = Convert.ToInt32(set.pwr2) * modeFactor; break;
                            case "040": pwrCor = Convert.ToInt32(set.pwr3) * modeFactor; break;
                            case "030": pwrCor = Convert.ToInt32(set.pwr4) * modeFactor; break;
                            case "020": pwrCor = Convert.ToInt32(set.pwr5) * modeFactor; break;
                            case "017": pwrCor = Convert.ToInt32(set.pwr6) * modeFactor; break;
                            case "015": pwrCor = Convert.ToInt32(set.pwr7) * modeFactor; break;
                            case "012": pwrCor = Convert.ToInt32(set.pwr8) * modeFactor; break;
                            case "010": pwrCor = Convert.ToInt32(set.pwr9) * modeFactor; break;
                            case "006": pwrCor = Convert.ToInt32(set.pwr10) * modeFactor; break;
                        }
                    }
                    if (rbAmp2.Checked)
                    {
                        switch (band)
                        {
                            case "160": pwrCor = Convert.ToInt32(set.pwr1_2) * modeFactor; break;
                            case "080": pwrCor = Convert.ToInt32(set.pwr2_2) * modeFactor; break;
                            case "040": pwrCor = Convert.ToInt32(set.pwr3_2) * modeFactor; break;
                            case "030": pwrCor = Convert.ToInt32(set.pwr4_2) * modeFactor; break;
                            case "020": pwrCor = Convert.ToInt32(set.pwr5_2) * modeFactor; break;
                            case "017": pwrCor = Convert.ToInt32(set.pwr6_2) * modeFactor; break;
                            case "015": pwrCor = Convert.ToInt32(set.pwr7_2) * modeFactor; break;
                            case "012": pwrCor = Convert.ToInt32(set.pwr8_2) * modeFactor; break;
                            case "010": pwrCor = Convert.ToInt32(set.pwr9_2) * modeFactor; break;
                            case "006": pwrCor = Convert.ToInt32(set.pwr10_2) * modeFactor; break;
                        }
                    }
                
                    WriteToPort("ZZPC" + (Convert.ToInt32(pwrCor).ToString())
                        .PadLeft(3, '0') + ";", iSleep);

                    btnByp.BackColor = Color.Lime;
                    mini.btnByp.BackColor = Color.Lime;
                    txtAlcInd.BackColor = Color.Lime;
                }
                else if (!stsOper && chkAutoDrv.Checked)
                {   // If in StandBy set power to the default value
                    switch (band)
                    {
                        case "160": WriteToPort("ZZPC" + set.def1.ToString().PadLeft(3, '0') + ";", iSleep); break;
                        case "080": WriteToPort("ZZPC" + set.def2.ToString().PadLeft(3, '0') + ";", iSleep); break;
                        case "040": WriteToPort("ZZPC" + set.def3.ToString().PadLeft(3, '0') + ";", iSleep); break;
                        case "030": WriteToPort("ZZPC" + set.def4.ToString().PadLeft(3, '0') + ";", iSleep); break;
                        case "020": WriteToPort("ZZPC" + set.def5.ToString().PadLeft(3, '0') + ";", iSleep); break;
                        case "017": WriteToPort("ZZPC" + set.def6.ToString().PadLeft(3, '0') + ";", iSleep); break;
                        case "015": WriteToPort("ZZPC" + set.def7.ToString().PadLeft(3, '0') + ";", iSleep); break;
                        case "012": WriteToPort("ZZPC" + set.def8.ToString().PadLeft(3, '0') + ";", iSleep); break;
                        case "010": WriteToPort("ZZPC" + set.def9.ToString().PadLeft(3, '0') + ";", iSleep); break;
                        case "006": WriteToPort("ZZPC" + set.def10.ToString().PadLeft(3, '0') + ";", iSleep); break;
                    }
                    WriteToPort("ZZOF000;", iSleep);
                }

                #region VHF+ Matrix
                // if matrix enabled output to port
                if (chkPortA.Checked || chkPortB.Checked)
                {
                    switch (band)
                    {
                        case "V00":
                            if (chkPortA.Checked)
                            {
                                if (!chkFWa.Checked) MatrixOutA(aPort, x2a0);
                                else { WriteFW(mAdr, cmd0, x2a0, chkInvertA.Checked); }
                            }
                            if (chkPortB.Checked)
                            {
                                if (!chkFWb.Checked) MatrixOutB(bPort, x2b0);
                                else { WriteFW(mAdr, cmd1, x2b0, chkInvertB.Checked); }
                            }
                            SetVHF("0");
                            break;
                        case "V01":
                            if (chkPortA.Checked)
                            {
                                if (!chkFWa.Checked) MatrixOutA(aPort, x2a1);
                                else { WriteFW(mAdr, cmd0, x2a1, chkInvertA.Checked); }
                            }
                            if (chkPortB.Checked)
                            {
                                if (!chkFWb.Checked) MatrixOutB(bPort, x2b1);
                                else { WriteFW(mAdr, cmd1, x2b1, chkInvertB.Checked); }
                            }
                            SetVHF("1");
                            break;
                        case "V02":
                            if (chkPortA.Checked)
                            {
                                if (!chkFWa.Checked) MatrixOutA(aPort, x2a2);
                                else { WriteFW(mAdr, cmd0, x2a2, chkInvertA.Checked); }
                            }
                            if (chkPortB.Checked)
                            {
                                if (!chkFWb.Checked) MatrixOutB(bPort, x2b2);
                                else { WriteFW(mAdr, cmd1, x2b2, chkInvertB.Checked); }
                            }
                            SetVHF("2");
                            break;
                        case "V03":
                            if (chkPortA.Checked)
                            {
                                if (!chkFWa.Checked) MatrixOutA(aPort, x2a3);
                                else { WriteFW(mAdr, cmd0, x2a3, chkInvertA.Checked); }
                            }
                            if (chkPortB.Checked)
                            {
                                if (!chkFWb.Checked) MatrixOutB(bPort, x2b3);
                                else { WriteFW(mAdr, cmd1, x2b3, chkInvertB.Checked); }
                            }
                            SetVHF("3");
                            break;
                        case "V04":
                            if (chkPortA.Checked)
                            {
                                if (!chkFWa.Checked) MatrixOutA(aPort, x2a4);
                                else { WriteFW(mAdr, cmd0, x2a4, chkInvertA.Checked); }
                            }
                            if (chkPortB.Checked)
                            {
                                if (!chkFWb.Checked) MatrixOutB(bPort, x2b4);
                                else { WriteFW(mAdr, cmd1, x2b4, chkInvertB.Checked); }
                            }
                            SetVHF("4");
                            break;
                        case "V05":
                            if (chkPortA.Checked)
                            {
                                if (!chkFWa.Checked) MatrixOutA(aPort, x2a5);
                                else { WriteFW(mAdr, cmd0, x2a5, chkInvertA.Checked); }
                            }
                            if (chkPortB.Checked)
                            {
                                if (!chkFWb.Checked) MatrixOutB(bPort, x2b5);
                                else { WriteFW(mAdr, cmd1, x2b5, chkInvertB.Checked); }
                            }
                            SetVHF("5");
                            break;
                        case "V06":
                            if (chkPortA.Checked)
                            {
                                if (!chkFWa.Checked) MatrixOutA(aPort, x2a6);
                                else { WriteFW(mAdr, cmd0, x2a6, chkInvertA.Checked); }
                            }
                            if (chkPortB.Checked)
                            {
                                if (!chkFWb.Checked) MatrixOutB(bPort, x2b6);
                                else { WriteFW(mAdr, cmd1, x2b6, chkInvertB.Checked); }
                            }
                            SetVHF("6");
                            break;
                        case "V07":
                            if (chkPortA.Checked)
                            {
                                if (!chkFWa.Checked) MatrixOutA(aPort, x2a7);
                                else { WriteFW(mAdr, cmd0, x2a7, chkInvertA.Checked); }
                            }
                            if (chkPortB.Checked)
                            {
                                if (!chkFWb.Checked) MatrixOutB(bPort, x2b7);
                                else { WriteFW(mAdr, cmd1, x2b7, chkInvertB.Checked); }
                            }
                            SetVHF("7");
                            break;
                        case "V08":
                            if (chkPortA.Checked)
                            {
                                if (!chkFWa.Checked) MatrixOutA(aPort, x2a8);
                                else { WriteFW(mAdr, cmd0, x2a8, chkInvertA.Checked); }
                            }
                            if (chkPortB.Checked)
                            {
                                if (!chkFWb.Checked) MatrixOutB(bPort, x2b8);
                                else { WriteFW(mAdr, cmd1, x2b8, chkInvertB.Checked); }
                            }
                            SetVHF("8");
                            break;
                        case "V09":
                            if (chkPortA.Checked)
                            {
                                if (!chkFWa.Checked) MatrixOutA(aPort, x2a9);
                                else { WriteFW(mAdr, cmd0, x2a9, chkInvertA.Checked); }
                            }
                            if (chkPortB.Checked)
                            {
                                if (!chkFWb.Checked) MatrixOutB(bPort, x2b9);
                                else { WriteFW(mAdr, cmd1, x2b9, chkInvertB.Checked); }
                            }
                            SetVHF("9");
                            break;
                        case "V10":
                            if (chkPortA.Checked)
                            {
                                if (!chkFWa.Checked) MatrixOutA(aPort, x2a10);
                                else { WriteFW(mAdr, cmd0, x2a10, chkInvertA.Checked); }
                            }
                            if (chkPortB.Checked)
                            {
                                if (!chkFWb.Checked) MatrixOutB(bPort, x2b10);
                                else { WriteFW(mAdr, cmd1, x2b10, chkInvertB.Checked); }
                            }
                            SetVHF("10");
                            break;
                        case "V11":
                            if (chkPortA.Checked)
                            {
                                if (!chkFWa.Checked) MatrixOutA(aPort, x2a11);
                                else { WriteFW(mAdr, cmd0, x2a11, chkInvertA.Checked); }
                            }
                            if (chkPortB.Checked)
                            {
                                if (!chkFWb.Checked) MatrixOutB(bPort, x2b11);
                                else { WriteFW(mAdr, cmd1, x2b11, chkInvertB.Checked); }
                            }
                            SetVHF("11");
                            break;
                        case "V12":
                            if (chkPortA.Checked)
                            {
                                if (!chkFWa.Checked) MatrixOutA(aPort, x2a12);
                                else { WriteFW(mAdr, cmd0, x2a12, chkInvertA.Checked); }
                            }
                            if (chkPortB.Checked)
                            {
                                if (!chkFWb.Checked) MatrixOutB(bPort, x2b12);
                                else { WriteFW(mAdr, cmd1, x2b12, chkInvertB.Checked); }
                            }
                            SetVHF("12");
                            break;
                        case "V13":
                            if (chkPortA.Checked)
                            {
                                if (!chkFWa.Checked) MatrixOutA(aPort, x2a13);
                                else { WriteFW(mAdr, cmd0, x2a13, chkInvertA.Checked); }
                            }
                            if (chkPortB.Checked)
                            {
                                if (!chkFWb.Checked) MatrixOutB(bPort, x2b13);
                                else { WriteFW(mAdr, cmd1, x2b13, chkInvertB.Checked); }
                            }
                            SetVHF("13");
                            break;
                        case "006":
                            if (chkPortA.Checked)
                            {
                                if (!chkFWa.Checked) MatrixOutA(aPort, x2a14);
                                else { WriteFW(mAdr, cmd0, x2a14, chkInvertA.Checked); }
                            }
                            if (chkPortB.Checked)
                            {
                                if (!chkFWb.Checked) MatrixOutB(bPort, x2b14);
                                else { WriteFW(mAdr, cmd1, x2b14, chkInvertB.Checked); }
                            }
                            SetVHF("14");
                            break;
                        case "002":
                            if (chkPortA.Checked)
                            {
                                if (!chkFWa.Checked) MatrixOutA(aPort, x2a15);
                                else { WriteFW(mAdr, cmd0, x2a15, chkInvertA.Checked); }
                            }
                            if (chkPortB.Checked)
                            {
                                if (!chkFWb.Checked) MatrixOutB(bPort, x2b15);
                                else { WriteFW(mAdr, cmd1, x2b15, chkInvertB.Checked); }
                            }
                            SetVHF("15");
                            break;
                        default:
                            if (chkPortA.Checked) { MatrixOutA(aPort, 0); }
                            if (chkPortB.Checked) { MatrixOutB(bPort, 0); }
                            SetVHF("HF");
                            break;
                    }
                }
                #endregion VHF+ Matrix

                #region WaveNode sensor Matrix

                // If WN2 band sensor matrix enabled, set correct sensor
                if (wn.chkEnab.Checked)
                {
                    switch (band)
                    {
                        case "160": switch (wn.wn160)
                            {
                                case 1: SetWN1(true); break;
                                case 2: SetWN2(true); break;
                                case 3: SetWN3(true); break;
                                case 4: SetWN4(true); break;
                            } break;
                        case "080": switch (wn.wn80)
                            {
                                case 1: SetWN1(true); break;
                                case 2: SetWN2(true); break;
                                case 3: SetWN3(true); break;
                                case 4: SetWN4(true); break;
                            } break;
                        case "060": switch (wn.wn60)
                            {
                                case 1: SetWN1(true); break;
                                case 2: SetWN2(true); break;
                                case 3: SetWN3(true); break;
                                case 4: SetWN4(true); break;
                            } break;
                        case "040": switch (wn.wn40)
                            {
                                case 1: SetWN1(true); break;
                                case 2: SetWN2(true); break;
                                case 3: SetWN3(true); break;
                                case 4: SetWN4(true); break;
                            } break;
                        case "030": switch (wn.wn30)
                            {
                                case 1: SetWN1(true); break;
                                case 2: SetWN2(true); break;
                                case 3: SetWN3(true); break;
                                case 4: SetWN4(true); break;
                            } break;
                        case "020": switch (wn.wn20)
                            {
                                case 1: SetWN1(true); break;
                                case 2: SetWN2(true); break;
                                case 3: SetWN3(true); break;
                                case 4: SetWN4(true); break;
                            } break;
                        case "017": switch (wn.wn17)
                            {
                                case 1: SetWN1(true); break;
                                case 2: SetWN2(true); break;
                                case 3: SetWN3(true); break;
                                case 4: SetWN4(true); break;
                            } break;
                        case "015": switch (wn.wn15)
                            {
                                case 1: SetWN1(true); break;
                                case 2: SetWN2(true); break;
                                case 3: SetWN3(true); break;
                                case 4: SetWN4(true); break;
                            } break;
                        case "012": switch (wn.wn12)
                            {
                                case 1: SetWN1(true); break;
                                case 2: SetWN2(true); break;
                                case 3: SetWN3(true); break;
                                case 4: SetWN4(true); break;
                            } break;
                        case "010": switch (wn.wn10)
                            {
                                case 1: SetWN1(true); break;
                                case 2: SetWN2(true); break;
                                case 3: SetWN3(true); break;
                                case 4: SetWN4(true); break;
                            } break;
                        case "006": switch (wn.wn6)
                            {
                                case 1: SetWN1(true); break;
                                case 2: SetWN2(true); break;
                                case 3: SetWN3(true); break;
                                case 4: SetWN4(true); break;
                            } break;
                    }//switch
                }
                #endregion WaveNode sensor Matrix

                #region ACOM Antenna Switch

                // use ACOM ant switch with amp off.
                if (ac == "Off" && set.ChkAntEnab == true)
                {
                    switch (band)
                    {
                        case "160": SetpaTune(set.asw1.ToString()); break;
                        case "080": SetpaTune(set.asw2.ToString()); break;
                        case "040": SetpaTune(set.asw3.ToString()); break;
                        case "030": SetpaTune(set.asw4.ToString()); break;
                        case "020": SetpaTune(set.asw5.ToString()); break;
                        case "017": SetpaTune(set.asw6.ToString()); break;
                        case "015": SetpaTune(set.asw7.ToString()); break;
                        case "012": SetpaTune(set.asw8.ToString()); break;
                        case "010": SetpaTune(set.asw9.ToString()); break;
                        case "006": SetpaTune(set.asw10.ToString()); break;
                    }
                }
                #endregion ACOM Antenna Switch

            }// if band changed
        }

        // Send freq to Alpha 9500 amp
        void DoA95Amp(string freq)
        {
            int pos;
            if (String.Compare(lastFreq, freq) != 0)
            {   // if the freq has changed
                if (freq.TrimStart('0').Length - 3 < 5) pos = 4;
                else pos = 5;
                int afreq = Convert.ToInt32(freq.TrimStart('0').Substring(0, pos));
                int seg = GetA95Seg(afreq);
                if (seg == -1)
                {
                    A95Q.Enqueue("01,40");
                    SetA95msg("Freq. out of range. Switching to Standby.");
                    return;
                }
                A95Q.Enqueue("01,0" + bandNum);         // send band
                A95Q.Enqueue("01," + (seg + 10).ToString());  // send segment
            }
        }

        // send freq to SPE Amp
        void DoSpeAmp(string freq)
        {
            string last = ""; // need to drop decimal digits
            if (lastFreq != "") last = lastFreq.Substring(0, 8);
            else last = "";

            if (String.Compare(last, freq.Substring(0, 8)) != 0)
            {
                byte[] bytes = new byte[8] { 0x55, 0x55, 0x55, 0x03, 0x82, 0x00, 0x00, 0x00 };
                string str = freq.TrimStart('0'); // trim "0"'s from front
                str = str.Substring(0, str.Length - 3); // use only kHz, no decimal
                int frq = Convert.ToInt32(str);      // convert to int
                string hex = frq.ToString("X4");     // convert to hex string
                string low = hex.Substring(2, 2);    // get low byte
                string high = hex.Substring(0, 2);   // get high byte
                byte cks = 0x82;                     // load cks with command
                cks += byte.Parse(low, NumberStyles.HexNumber); // add low hex to cks
                cks += byte.Parse(high, NumberStyles.HexNumber); // add high hex to cks
                string chks = cks.ToString("X");     // save chksum to string
                bytes[5] = byte.Parse(low, NumberStyles.HexNumber);
                bytes[6] = byte.Parse(high, NumberStyles.HexNumber);
                bytes[7] = byte.Parse(chks, NumberStyles.HexNumber);
                SPEport.Write(bytes, 0, 8);
                Thread.Sleep(100);
            }

        }

        // send freq to Tube Amps Amp
        void DoTubeAmps(string freq)
        {
            int pos;
            if (String.Compare(lastFreq, freq) != 0)
            {   // if the freq has changed
                if (Amp == 0 && bAmp)   // ACOM 2000A
                {
                    if (freq.TrimStart('0').Length - 3 < 5) pos = 4;
                    else pos = 5;
                    int tfreq = Convert.ToInt32(freq.TrimStart('0').Substring(0, pos));
                    int seg = GetSeg(tfreq);
                    if (seg >= 0)
                    {
                        string hi;
                        string lo;
                        if (pos == 4)
                        {
                            hi = freq.TrimStart('0').Substring(0, 1);
                            lo = freq.TrimStart('0').Substring(1, 3);
                        }
                        else
                        {
                            hi = freq.TrimStart('0').Substring(0, 2);
                            lo = freq.TrimStart('0').Substring(2, 3);
                        }
                        SetFreq(hi + "." + lo);
                        // send segment info to amp
                        string str = String.Format("{0:x2} ", seg).ToUpper();
                        string msb = "3" + str.Substring(0, 1);
                        string lsb = "3" + str.Substring(1, 1);
                        byte[] b = { 0x41, 0x71, 0x57, 0x32, 0x00, 0x00, 0x00 };
                        b[4] = byte.Parse(msb, NumberStyles.HexNumber);
                        b[5] = byte.Parse(lsb, NumberStyles.HexNumber);
                        if (AlphaPort.IsOpen) AlphaPort.Write(b, 0, 7);
                        for (int k = 0; k < 10; k++)
                        {   // delay 1 sec for seg change.
                            Thread.Sleep(100);
                            Application.DoEvents();
                        }

                        if (AlphaPort.IsOpen) AlphaPort.Write("AqF\0");
                        Thread.Sleep(200);
                    }
                    Console.WriteLine("A2K Freq Changed");
                }
                else if (Amp == 1)  // Alpha 87
                {
                    if (freq.TrimStart('0').Length - 3 < 5) pos = 4;
                    else pos = 5;
                    AlphaPort.Write("FREQ " +
                        freq.TrimStart('0').Substring(0, pos) + "\r");
                }
            } // if(String.Compare(lastFreq)
        }

        // Update SteppIR Antenna Controller (interactive)
        void DoSteppIR()
        {
            // see if freq has changed
            //if (String.Compare(lastFreq, logFreq) != 0)
            //{   // yes go poll SteppIR controller
            //    //Thread.Sleep(1000);
            //    //StepData.Write("?A\r");
            //    if (chkTI.Checked)
            //    {
            //        StepTimer.Enabled = true;
            //        StepCtr = reps; // counter to allow for delay
            //    }
            //}   // see StepData_DataReceived() for return
        }

        // convert ZZAC to ZZST
        string AC2ST(string ac)
        {
            string st = "";
            switch (ac)
            {
                case "00": st = "0000"; break;
                case "01": st = "0001"; break;
                case "02": st = "1000"; break;
                case "03": st = "0010"; break;
                case "04": st = "1001"; break;
                case "05": st = "1010"; break;
                case "06": st = "0011"; break;
                case "07": st = "1011"; break;
                case "08": st = "1100"; break;
                case "09": st = "0100"; break;
                case "10": st = "0101"; break;
                case "11": st = "0101"; break;
                case "12": st = "0101"; break;
                case "13": st = "0110"; break;
                case "14": st = "0111"; break;
                default: break;
            }
            return st;
        }

        // convert ZZST to ZZAC 
        string ST2AC(string st)
        {
            string  ac = "";
            switch (st)
            {
                case "0000": ac = "00"; break;
                case "0001": ac = "01"; break;
                case "1000": ac = "02"; break;
                case "0010": ac = "03"; break;
                case "1001": ac = "04"; break;
                case "1010": ac = "05"; break;
                case "0011": ac = "06"; break;
                case "1011": ac = "07"; break;
                case "1100": ac = "08"; break;
                case "0100": ac = "09"; break;
                case "0101": ac = "10"; break;
                case "0110": ac = "13"; break;
                case "0111": ac = "14"; break;
                default: break;
            }
            return ac;
        }

        #endregion # Methods #

        #endregion State Machine

        #region SteppIR
        
        #region # Vars #
        bool b180 = false;
        bool b34 = false;
        bool bBiDir = false;
        bool bCal = false;
        bool bFwd = false;
        string StepBufr = "";
        bool firstFF = false;   // to identify if 1st pass for AC=FF

        #endregion # Vars #

        // The transmit Inhibit check box has changed
        private void chkTI_CheckedChanged(object sender, EventArgs e)
        {
            if (chkTI.Checked)
                set.chkTI = true;
            else
                set.chkTI = false;
            set.Save();
//            lastFreq = "";
        }
        // The Home SteppIR button was pressed
        public void btnHome_Click(object sender, EventArgs e)
        {
            if (chkStep.Checked)
            {
                StepData.Write("@A\0\0\0\0\0\0S0\r"); // Home the antenna
                ShowAnt(true);
                Thread.Sleep(100);
                //StepData.Write("?A\r");
                StepTimer.Enabled = true;
                StepCtr = reps; // counter to allow for delay
                chkStep.Checked = false;
                rbFwd.Checked = true;
                //x = this.Left;
                //y = this.Top;
                Notification alert = new Notification();
                Notification.notiIntvl = 7000;
                Notification.notiMsg =
                    "The SteppIR Antenna elements are retracting. " +
                    "The antenna will NO longer follow PowerSDR.\r\r" +
                    "See the 'Other' tab to Re-Enable operation\r";
                alert.Show();
            }
        }
        // The Calibrate SteppIR button was pressed
        public void btnCalib_Click(object sender, EventArgs e)
        {
            StepData.Write("@A\0\0\0\0\0\0V0\r");
            ShowAnt(true);
            Thread.Sleep(100);
            //StepData.Write("?A\r");
            StepTimer.Enabled = true;
            StepCtr = reps; // counter to allow for delay
            bCal = true;
            //x = this.Left;
            //y = this.Top;
            Notification alert = new Notification();
            Notification.notiIntvl = 7000;
            Notification.notiMsg =
                "Please Standby the antenna is Calibrating\r\r" +
                "This may take a few moments!\r";
            alert.Show();
        }
        // Send port data to SteppIR controller
        private void StepPortMsg(string freq, string dir, string cmd)
        {
            if (freq != "")
            {
                string preamble = "404100";
                string postamble = "300D";
                byte[] bytes = new byte[11];
                string decimalNumber = freq.Substring(3, 7);
                int number = int.Parse(decimalNumber);
                string hex = number.ToString("x6");
                string strOut = preamble + hex + "00" + dir + cmd + postamble;
                int j = 0;
                for (int i = 0; i < 11; i++)
                {
                    string x = strOut.Substring(j, 2);
                    bytes[i] = byte.Parse(x, NumberStyles.HexNumber);
                    j += 2;
                }
                // send command to controller
                StepData.Write(bytes, 0, 11);
                if (chkTI.Checked)
                {
                    firstFF = true;
                    StepCtr = 3;
                    // delay for {Interval} and then send status request to controller.
                    StepTimer.Enabled = true;
                }
                else
                    StepCtr = 0;
            }
        }
        // The SteppIR Data Port has received data from timer query
        private void StepData_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string StepMsg = "";
            SerialPort port = (SerialPort)sender;
            byte[] data = new byte[port.BytesToRead];
            port.Read(data, 0, data.Length);
            StepBufr += AE.GetString(data, 0, data.Length);
            Regex rex = new Regex(".*?\r");  //accept any string ending in "0x0D"		
            //loop thru the buffer and find matches
            for (Match m = rex.Match(StepBufr); m.Success; m = m.NextMatch())
            {
                try
                {
                    StepMsg = m.Value;
                    // remove the match from the buffer if found
                    StepBufr = StepBufr.Replace(m.Value, "");
                    if (data.Length > 7)
                    {
                        // write steppir freq to rotor tab label
                        string Fh = data[3].ToString("X2");
                        string Fm = data[4].ToString("X2");
                        string Fl = data[5].ToString("X2");
                        string mot = data[6].ToString();
                        string dir = data[7].ToString();
                        string hexValue = Fh + Fm + Fl;
                        int xfreq = int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber) / 10;
                        string sfreq = xfreq.ToString();
                        double freq = 0.0;
                        if (xfreq > 99999)
                        {
                            freq = Convert.ToDouble(sfreq.Substring(0, 2) + "." + sfreq.Substring(2, 4));
                            SetStepFreq(freq.ToString("0.00", CultureInfo.InvariantCulture));
                        }
                        else if (xfreq > 9999 && xfreq < 99999)
                        {
                            freq = Convert.ToDouble(sfreq.Substring(0, 1) + "." + sfreq.Substring(1, 4));
                            SetStepFreq(freq.ToString("0.00", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            SetStepFreq("----");
                        }
                        // See if motor(s) are moving
                        if (chkTI.Checked)
                        {
                            if (mot == "0") // not moving
                            {
                                WriteToPort("ZZTI0;", iSleep);  // turn off transmit inhibit
                                StepCtr -= 1; // decrement the reps counter
                                if (StepCtr <= 0)
                                {
                                    StepTimer.Enabled = false;
//                                    Console.WriteLine("#1 mot = " + mot);
                                    ShowAnt(false);
                                    if (bCal)
                                    {
                                        // if the calibration is finished send freq data 
                                        // to the antenna to reset it where it was
                                        StepPortMsg(lastFreq, "00", "52");
                                        bCal = false;
                                    }
                                }
                                firstFF = false;
                            }
                            // controller has had a power interruption
                            // when SDA100 is off; d = power interrupt, \a = elements home
                            else if (mot == "d" || mot == "\a")
                            {
                                WriteToPort("ZZTI0;", iSleep);  // turn off transmit inhibit
                                StepCtr -= 1; // decrement the reps counter
                                if (StepCtr == 0)
                                {
                                    StepTimer.Enabled = false;
//                                    Console.WriteLine("#2 mot = " + mot);
                                    ShowAnt(false);
                                }
                            }
                            else if (mot == "255" && firstFF)
                            {
                                firstFF = false;
                                return;
                            }
                            else
                            {   // antenna is moving
                                WriteToPort("ZZTI1;", iSleep);  // turn on transmit inhibit
//                                Console.WriteLine("#3 mot = " + mot);
                                ShowAnt(true);
                                StepTimer.Enabled = true; // go check it again
                                firstFF = false;
                            }
                        }
                        else
                        {
                            StepTimer.Enabled = false;
                            WriteToPort("ZZTI0;", iSleep);  // turn off transmit inhibit
//                            Console.WriteLine("#4 mot = " + mot);
                            ShowAnt(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    bool bReturnLog = false;
                    bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                    if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                        "Unable to write to log");
                }
            }
        }
        // The Forward radio button was selected
        private void rbFwd_CheckedChanged(object sender, EventArgs e)
        {
            if (rbFwd.Checked)
            {
                mini.init = false; rtr.init = false;
                mini.rbFwd.Checked = true; rtr.rbFwd.Checked = true;
                bFwd = true; b180 = false; bBiDir = false; b34 = false;
                if (lastFreq != "")
                {
                    StepPortMsg(lastFreq, "00", "31");
                }
            }
        }
        // The 180 radio button was selected
        private void rb180_CheckedChanged(object sender, EventArgs e)
        {
            if (rb180.Checked)
            {
                mini.init = false; rtr.init = false;
                mini.rb180.Checked = true; rtr.rb180.Checked = true;
                bFwd = false; b180 = true; bBiDir = false; b34 = false;
                if (lastFreq != "")
                {
                    StepPortMsg(lastFreq, "40", "31");
                }
            }
        }
        // The Bi-Directional radio button was selected
        private void rbBiDir_CheckedChanged(object sender, EventArgs e)
        {
            if (rbBiDir.Checked)
            {
                mini.init = false; rtr.init = false;
                mini.rbBiDir.Checked = true; rtr.rbBiDir.Checked = true;
                bFwd = false; b180 = false; bBiDir = true; b34 = false;
                if (lastFreq != "")
                {
                    StepPortMsg(lastFreq, "80", "31");
                }
            }
        }
        // The 3/4 wave length radio button was selected (vertical only)
        private void rb34_CheckedChanged(object sender, EventArgs e)
        {
            if (rb34.Checked)
            {
                mini.init = false;
                mini.rb34.Checked = true;
                bFwd = false; b180 = false; bBiDir = false; b34 = true;
                if (lastFreq != "")
                {
                    StepPortMsg(lastFreq, "20", "31");
                }
            }
        }
        // The interval timer has fired, send status request to controller
        void StepTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (closing) { StepTimer.Close(); return; }
            try
            {
                StepData.Write("?A\r");
            }
            catch { }
        }
        // SteppIR enabled check box has changed
        private void chkStep_CheckedChanged(object sender, EventArgs e)
        {
            if (chkStep.Checked)
            {   // Enable Freq update
                grpStepCtrl.Enabled = true;
                mini.grpStepCtrl.Enabled = true;
                rtr.grpStepCtrl.Enabled = true;
                if (lastFreq != "")
                {
                    int frq = Convert.ToInt32(lastFreq.TrimStart('0'));
                    StepPortMsg((frq + 1).ToString().PadLeft(11, '0'), "00", "52");
                    Thread.Sleep(1000);
                    StepPortMsg((frq + 1).ToString().PadLeft(11, '0'), "00", "52");
                }
                //else
                //{
                //    StepPortMsg(logFreq, "00", "52");
                //}
                if (cboStep.SelectedIndex > 0) 
                    set.StepEnab = true;
                else
                {
                    MessageBox.Show(new Form() { TopMost = true },
                        "No port has been selected for the SteppIR.\n\n" +
                        "Please select a valid port number and try again.", "Port Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    chkStep.Checked = false;
                    cboStep.SelectedIndex = 0;
                }
            }
            else
            {
                set.StepEnab = false;
                grpStepCtrl.Enabled = false;
                mini.grpStepCtrl.Enabled = false;
                rtr.grpStepCtrl.Enabled = false;
            }
            set.Save();
        }
        // SteppIR Port Number hsa changed
        private void cboStep_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (StepData.IsOpen) StepData.Close();
            if (cboStep.SelectedIndex > 0)
            {
                StepData.PortName = cboStep.SelectedItem.ToString();
                try
                {
                    StepData.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "The STeppIR serial port " + StepData.PortName +
                        " cannot be opened!\n", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboStep.SelectedIndex = 0;
                    chkStep.Checked = false;
                    set.StepPortNum = 0;
                    set.Save(); 
                    return;
                }
            }
            else
            {
                cboStep.SelectedIndex = 0; 
                chkStep.Checked = false;
            }
            set.StepPortNum = cboStep.SelectedIndex;
            set.Save();
        }
        // SteppIR timer interval has changed
        private void txtStepInv_TextChanged(object sender, EventArgs e)
        {
            try
            {
                StepTimer.Interval = Convert.ToDouble(txtStepInv.Text);
                set.StepInv = txtStepInv.Text;
                set.Save();
            }
            catch { }
        }
        // the SteppIR Port Com setting has changed
        private void cboStepCom_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cboStepCom.SelectedIndex)
            {
                case 0: // 19200 8N1
                    StepData.BaudRate = 19200;
                    StepData.DataBits = 8;
                    StepData.Parity = System.IO.Ports.Parity.None;
                    StepData.StopBits = System.IO.Ports.StopBits.One;
                    break;
                case 1: // 9600 8N1
                    StepData.BaudRate = 9600;
                    StepData.DataBits = 8;
                    StepData.Parity = System.IO.Ports.Parity.None;
                    StepData.StopBits = System.IO.Ports.StopBits.One;
                    break;
                case 2: // 9600 8N2
                    StepData.BaudRate = 9600;
                    StepData.DataBits = 8;
                    StepData.Parity = System.IO.Ports.Parity.None;
                    StepData.StopBits = System.IO.Ports.StopBits.Two;
                    break;
                case 3: // 4800 8N1
                    StepData.BaudRate = 4800;
                    StepData.DataBits = 8;
                    StepData.Parity = System.IO.Ports.Parity.None;
                    StepData.StopBits = System.IO.Ports.StopBits.One;
                    break;
                case 4: // 4800 8N2
                    StepData.BaudRate = 4800;
                    StepData.DataBits = 8;
                    StepData.Parity = System.IO.Ports.Parity.None;
                    StepData.StopBits = System.IO.Ports.StopBits.Two;
                    break;
                case 5: // 2400 8N1
                    StepData.BaudRate = 2400;
                    StepData.DataBits = 8;
                    StepData.Parity = System.IO.Ports.Parity.None;
                    StepData.StopBits = System.IO.Ports.StopBits.One;
                    break;
                case 6: // 2400 8N2
                    StepData.BaudRate = 2400;
                    StepData.DataBits = 8;
                    StepData.Parity = System.IO.Ports.Parity.None;
                    StepData.StopBits = System.IO.Ports.StopBits.Two;
                    break;
                case 7: // 1200 8N1
                    StepData.BaudRate = 1200;
                    StepData.DataBits = 8;
                    StepData.Parity = System.IO.Ports.Parity.None;
                    StepData.StopBits = System.IO.Ports.StopBits.One;
                    break;
                case 8: // 1200 8N2
                    StepData.BaudRate = 1200;
                    StepData.DataBits = 8;
                    StepData.Parity = System.IO.Ports.Parity.None;
                    StepData.StopBits = System.IO.Ports.StopBits.Two;
                    break;
                default:
                    break;
            }
            set.StepCom = (int)cboStepCom.SelectedIndex;
            set.Save();
        }
        // toggle the interval text box so its value can be changed.
        private void label41_DoubleClick(object sender, EventArgs e)
        {
            if (txtStepInv.Enabled) txtStepInv.Enabled = false;
            else txtStepInv.Enabled = true;
        }

        #endregion SteppIR

        #region Timer Events

        // poll the radio for state status
        void pollTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                List<string> stale = new List<string>();
                foreach (DictionaryEntry de in Poll)
                {
                    DateTime dt1 = Convert.ToDateTime(de.Value);
                    DateTime dt2 = DateTime.Now;
                    TimeSpan tsDiff = dt2.Subtract(dt1);

                    if (tsDiff.Seconds < 15 || de.Key.ToString() == "ZZDU" || de.Key.ToString() == "ZZRS")
                        sp.Write(de.Key.ToString() + ";");

                    else if (tsDiff.Seconds >= 15 && de.Key.ToString() != "ZZDU" && de.Key.ToString() != "ZZRS")
                        stale.Add(de.Key.ToString());
                }
                foreach (object de in stale)
                {
                    Poll.Remove(de);
                    Console.WriteLine(de);
                }
            }
            catch
            { }
        }

        void blinkTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (bBlink) txtMsg.BackColor = Color.Yellow;
            else txtMsg.BackColor = Color.LightYellow;
            bBlink = !bBlink;
        }
        // Lost CAT activity
        void comTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            comTimer.Stop();
            //isFirstPass = true;
            SetTitle("DDUtil " + ver + " - CAT Connection Lost ");
        }
        
        #endregion Timer Events

        #region Test Routines

        #region * PW1 *

        // The PW1 Test button was pressed
        // simulate poll message from IC-PW1 to DDUtil for freq read
        // Note that additional code to initialize the PW1Timer is in 
        // the Setup() routine 
        System.Timers.Timer pw1Timer;
        private void btnTest_Click(object sender, EventArgs e)
        {
            // toggle the timer on & off
            if (!pw1Timer.Enabled)
            { pw1Timer.Enabled = true; btnTest.BackColor = Color.Yellow; }
            else
            { pw1Timer.Enabled = false; btnTest.BackColor = Color.Empty; }
        }
        void pw1Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!TestPort.IsOpen) TestPort.Open();

            byte[] bytes = new byte[8];
            string preamble = "FE";
            string EOM = "FD";
            string mystring = EOM + "03" + "54" + "33" + preamble + preamble + "DD" + "FF";
            int j = 14;
            for (int i = 0; i < 8; i++)
            {
                string temp = mystring.Substring(j, 2);
                bytes[i] = byte.Parse(temp, NumberStyles.HexNumber);
                //                Console.Write("{0:x2} ", bytes[i]);
                j -= 2;
            }
            //            Console.WriteLine();
            // FE FE 33 54 03 FD
            TestPort.Write(bytes, 0, 8);
            Thread.Sleep(iSleep);
            // Output the request for mode query
            mystring = EOM + "04" + "54" + "33" + preamble + preamble + "80" + "EF";
            j = 14;
            for (int i = 0; i < 8; i++)
            {
                string temp = mystring.Substring(j, 2);
                bytes[i] = byte.Parse(temp, NumberStyles.HexNumber);
                //                Console.Write("{0:x2} ", bytes[i]);
                j -= 2;
            }
            //            Console.WriteLine();
            // FE FE 33 54 04 FD
            TestPort.Write(bytes, 0, 8);
            if (pw1Timer.Enabled != true) pw1Timer.Enabled = true;
        }
        #endregion * PW1 *

        #endregion Test Routines

        #region Tuning Knob

        #region * Declarations *

        // This table holds the aux switch functions
        private Hashtable Switch = new Hashtable();

        bool updating = false;  // true if FW update in progress
        int ActCon = 0;         // Active Control index (1-4)
        int ActConDC = 0;       // Active Control DC index (1-2)
        int ActIdx = 0;         // Active control index       
        int ActIdxDC = 0;       // Active control index for dbl-clk control 
        int kFlags = 0x51;      // knob flag register
        int RITfrq = 0;         // current RIT offset
        int stepSize = 0;       // PSDR tune step size (default = 10 hz)
        int volIdx = -1;        // psdr volume setting
        int XITfrq = 0;         // current XIT offset
        string KnobBuf = "";    // serial data received buffer
        string kCATup = "";     // current CAT Up command 
        string kCATdn = "";     // current CAT Dn command 
        string kCATdcOn = "";   // current DC CAT Up command 
        string kCATdcOff = "";  // current DC CAT Dn command 
        string TkVer = "";      // current tk hex file version
        string TkComNum = "";   // Knob Com port number
        string memSave = "";    // VFO freq save 

        // kFlags descr.
        // LSB  0   Mode A = Off, B = On
        //      1   Single Click 
        //      2   Double Click 
        //      3   Long Click
        //      4   LED1            Off = 0, On = 1
        //      5   LED2            Off = 0, On = 1
        //      6   LED3            Off = 0, On = 1
        // MSB  7   Knob Direction  Up  = 0, Down = 1

        #endregion * Declarations *

        #region # Delegates #

        //// Write to Progress Bar control
        //delegate void SettsProgBarCallback(string text);
        //private void SettsProgBar(string text)
        //{
        //    if (tsProgBar.InvokeRequired)
        //    {
        //        SettsProgBarCallback d = new SettsProgBarCallback(SettsProgBar);
        //        this.Invoke(d, new object[] { text });
        //    }
        //    else
        //    {
        //        if (text == "setup")
        //        {
        //            tsProgBar.Value = 0;
        //            tsProgBar.Maximum = 225;
        //            tsProgBar.Visible = true;
        //            return;
        //        }
        //        else if (text == "cleanup")
        //        {
        //            tsProgBar.Visible = false;
        //            return;
        //        }
        //        else
        //        { tsProgBar.Value += 1; }
        //    }
        //}
        // Write to ModeAOn control
        delegate void SetModeAOnCallback(string text);
        private void SetModeAOn(string text)
        {
            if (this.cboKnobAOn.InvokeRequired)
            {
                SetModeAOnCallback d = new SetModeAOnCallback(SetModeAOn);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "True")
                {
                    this.cboKnobAOn.BackColor = Color.LightGreen;
                }
                else
                {
                    this.cboKnobAOn.BackColor = Color.Empty;
                }
                if (ActCon == 1) ActIdx = cboKnobAOn.SelectedIndex;
            }
        }
        // Write to ModeAOff control
        delegate void SetModeAOffCallback(string text);
        private void SetModeAOff(string text)
        {
            if (this.cboKnobAOff.InvokeRequired)
            {
                SetModeAOffCallback d = new SetModeAOffCallback(SetModeAOff);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "True")
                { this.cboKnobAOff.BackColor = Color.LightGreen; }
                else
                { this.cboKnobAOff.BackColor = Color.Empty; }
                if (ActCon == 2) ActIdx = cboKnobAOff.SelectedIndex;
            }
        }
        // Write to ModeBOn control
        delegate void SetModeBOnCallback(string text);
        private void SetModeBOn(string text)
        {
            if (this.cboKnobBOn.InvokeRequired)
            {
                SetModeBOnCallback d = new SetModeBOnCallback(SetModeBOn);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "True")
                { this.cboKnobBOn.BackColor = Color.Yellow; }
                else
                { this.cboKnobBOn.BackColor = Color.Empty; }
                if (ActCon == 3) ActIdx = cboKnobBOn.SelectedIndex;
            }
        }
        // Write to ModeBOff control
        delegate void SetModeBOffCallback(string text);
        private void SetModeBOff(string text)
        {
            if (this.cboKnobBOff.InvokeRequired)
            {
                SetModeBOffCallback d = new SetModeBOffCallback(SetModeBOff);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "True")
                { this.cboKnobBOff.BackColor = Color.Yellow; }
                else
                { this.cboKnobBOff.BackColor = Color.Empty; }
                if (ActCon == 4) ActIdx = cboKnobBOff.SelectedIndex;
            }
        }
        // Write to ModeADC control
        delegate void SetModeADCCallback(string text);
        private void SetModeADC(string text)
        {
            if (this.cboKnobADC.InvokeRequired)
            {
                SetModeADCCallback d = new SetModeADCCallback(SetModeADC);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "True")
                { this.cboKnobADC.BackColor = Color.LightGreen; }
                else
                { this.cboKnobADC.BackColor = Color.Empty; }
                if (ActConDC == 1) ActIdxDC = cboKnobADC.SelectedIndex;
            }
        }
        // Write to ModeBDC control
        delegate void SetModeBDCCallback(string text);
        private void SetModeBDC(string text)
        {
            if (this.cboKnobBDC.InvokeRequired)
            {
                SetModeBDCCallback d = new SetModeBDCCallback(SetModeBDC);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text == "True")
                { this.cboKnobBDC.BackColor = Color.Yellow; }
                else
                { this.cboKnobBDC.BackColor = Color.Empty; }
                if (ActConDC == 2) ActIdxDC = cboKnobBDC.SelectedIndex;
            }
        }
        // Write to ZC text box
        delegate void SetZCCallback(string text);
        private void SetZC(string text)
        {
            if (!closing)
            {
                try
                {
                    if (this.lblZC.InvokeRequired)
                    {
                        SetZCCallback d = new SetZCCallback(SetZC);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                        txtZC.Text = text;
                }
                catch { }
            }
        }
        // Write to ZE text box
        delegate void SetZECallback(string text);
        private void SetZE(string text)
        {
            if (!closing)
            {
                try
                {
                    if (this.lblZE.InvokeRequired)
                    {
                        SetZECallback d = new SetZECallback(SetZE);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                        txtZE.Text = text;
                }
                catch { }
            }
        }
        // Write to ZL text box
        delegate void SetZLCallback(string text);
        private void SetZL(string text)
        {
            if (!closing)
            {
                try
                {
                    if (this.txtZL.InvokeRequired)
                    {
                        SetZLCallback d = new SetZLCallback(SetZL);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                        txtZL.Text = text;
                }
                catch { }
            }
        }
        // Write to ZR text box
        delegate void SetZRCallback(string text);
        private void SetZR(string text)
        {
            if (!closing)
            {
                try
                {
                    if (this.txtZR.InvokeRequired)
                    {
                        SetZRCallback d = new SetZRCallback(SetZR);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                        txtZR.Text = text;
                }
                catch { }
            }
        }
        // Write to grpTKnob heading
        delegate void SetgrpTKnobCallback(string text);
        private void SetgrpTKnob(string text)
        {
            if (!closing)
            {
                try
                {
                    if (this.grpTKnob.InvokeRequired)
                    {
                        SetgrpTKnobCallback d = new SetgrpTKnobCallback(SetgrpTKnob);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                        grpTKnob.Text = text;
                }
                catch { }
            }
        }
        // Write to StepSize 
        delegate void SetStepSizeCallback(string text);
        private void SetStepSize(string text)
        {
            if (!closing)
            {
                try
                {
                    if (this.cboTstep.InvokeRequired)
                    {
                        SetStepSizeCallback d = new SetStepSizeCallback(SetStepSize);
                        this.Invoke(d, new object[] { text });
                    }
                    else
                        cboTstep.SelectedIndex = Convert.ToInt32(text);
                }
                catch { }
            }
        }

        #endregion # Delegates #

        #region * Events *

        private void KnobPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string sCmd = "";
                SerialPort port = (SerialPort)sender;
                byte[] data = new byte[port.BytesToRead];
                port.Read(data, 0, data.Length);
                KnobBuf += AE.GetString(data, 0, data.Length);
                Regex rex = new Regex(".*?;"); //accept any string ending in ;		
                for (Match m = rex.Match(KnobBuf); m.Success; m = m.NextMatch())
                {   //loop thru the buffer and find matches
                    sCmd = m.Value;
                    KnobBuf = KnobBuf.Replace(m.Value, ""); //remove the match from the buffer
                    if (chkKnobEnab.Checked)
                    {
                        switch (sCmd.Substring(0, 1))
                        {
                            case "B": //boot loader active
                                if (sCmd.Substring(0, 3) == "BL!")
                                {
                                    KnobPort.DiscardInBuffer();
                                    KnobPort.DiscardOutBuffer();
                                    KnobPort.Close();
                                    KnobPort.Dispose();
                                    Thread.Sleep(10000);
                                    ReOpenKnobPort();
                                    if (KnobPort.IsOpen) KnobPort.Write("F;");
                                }
                                break;
                            case "C": //double-click
                                kFlags ^= 0x20;
                                WriteFlags(); WriteLED();
                                if (Convert.ToBoolean(kFlags >> 5 & 0x01)) //led2 lit
                                {
                                    WriteToPort(kCATdcOn, iSleep);
                                    if (kCATdcOn == "ZZSW1;" && chkSoEnab.Checked)
                                    {
                                        ZZSW = 1;
                                        SetVfoB();
                                        DoRx1Tap();
                                    }
                                }
                                else
                                {
                                    WriteToPort(kCATdcOff, iSleep);
                                    if (kCATdcOff == "ZZSW0;" && chkSoEnab.Checked)
                                    { ZZSW = 0; 
                                        SetVfoA(); 
                                        DoRx1Tap(); 
                                    }
                                }
                                break;
                            case "D": //tune something down
                                if (ActIdx == 0 || ActIdx == 1) // tune vfo a/b/xmit
                                {
                                    string newSize = "1";
                                    int tkSize = 0;
                                    if (sCmd.Length > 3)
                                    { tkSize = Convert.ToInt32(sCmd.Substring(1, 2)); }
                                    if (tkSize > stepSize)
                                    { newSize = (stepSize + (tkSize - 1)).ToString(); }
                                    else
                                    { newSize = stepSize.ToString(); }
                                    WriteToPort(kCATdn + newSize.PadLeft(2, '0') + ";", iSleep);
                                }
                                else if (ActIdx == 2)  // tune TX VFO Down
                                {
                                    string newFreq = "";
                                    int newSize = 0;
                                    int tkSize = 0;
                                    int TV = Convert.ToInt32(State["_ZZTV"].ToString());
                                    switch (stepSize)
                                    {
                                        case 0: newSize = 1; break;
                                        case 1: newSize = 10; break;
                                        case 2: newSize = 50; break;
                                        case 3: newSize = 100; break;
                                        case 4: newSize = 250; break;
                                        case 5: newSize = 500; break;
                                        case 6: newSize = 1000; break;
                                        case 7: newSize = 5000; break;
                                        case 8: newSize = 9000; break;
                                        case 9: newSize = 10000; break;
                                        case 10: newSize = 100000; break;
                                        case 11: newSize = 250000; break;
                                        case 12: newSize = 500000; break;
                                        case 13: newSize = 1000000; break;
                                        case 14: newSize = 10000000; break;
                                    }
                                    if (sCmd.Length > 3)
                                    {
                                        tkSize = Convert.ToInt32(sCmd.Substring(1, 2));
                                        newSize *= tkSize;
                                        newFreq = (TV - newSize).ToString();
                                    }
                                    else
                                    {
                                        newFreq = (TV - newSize).ToString();
                                    }
                                    WriteToPort(kCATdn + newFreq.PadLeft(11, '0') + ";", iSleep);
                                    WriteToPort("ZZTV;", iSleep);
                                }
                                else if (ActIdx == 3)    // tune RIT
                                {
                                    int RIT = 0;
                                    RITfrq -= 2;
                                    if (RITfrq < 0)
                                    {
                                        RIT = (int)Math.Abs(RITfrq);
                                        kCATdn = "ZZRF-" + RIT.ToString().PadLeft(4, '0') + ";";
                                    }
                                    else
                                    { kCATdn = "ZZRF+" + RITfrq.ToString().PadLeft(4, '0') + ";"; }
                                    WriteToPort(kCATdn, iSleep);
                                }
                                else if (ActIdx == 4)   // tune XIT
                                {
                                    int XIT = 0;
                                    XITfrq -= 2;
                                    if (XITfrq < 0)
                                    {
                                        XIT = (int)Math.Abs(XITfrq);
                                        kCATdn = "ZZXF-" + XIT.ToString().PadLeft(4, '0') + ";";
                                    }
                                    else
                                    { kCATdn = "ZZXF+" + XITfrq.ToString().PadLeft(4, '0') + ";"; }
                                    WriteToPort(kCATdn, iSleep);
                                }
                                else if (ActIdx == 5)   // volume
                                {
                                    if (volIdx < 0)
                                    { WriteToPort("ZZAG;", iSleep); }
                                    else if (volIdx >= 1)
                                    {
                                        volIdx -= 1;
                                        WriteToPort(kCATdn +
                                            volIdx.ToString().PadLeft(3, '0') + ";", iSleep);
                                    }
                                }
                                break;
                            case "F":   //firmware revision
                                if (sCmd.Substring(0, 3) == "FFF")
                                {
                                    if (!updating)
                                    {
                                        SetgrpTKnob("Updating Firmware!");
                                        SendTkTextFile("USB Tuning Knob.hex");
                                    }
                                }
                                else
                                {
                                    SetgrpTKnob("K6TD && K6TU Tuning Knob - Rev. " +
                                        sCmd.Substring(1, 2) + "." +
                                        sCmd.Substring(3, 2));
                                    TkVer = sCmd.Substring(1, 4);

                                    TextReader tr = new StreamReader("TkVersion.txt");
                                    string tempStr = tr.ReadLine();
                                    tr.Close();

                                    // display alert message if new version is avail
                                    if (Convert.ToInt32(tempStr) > Convert.ToInt32(TkVer))
                                        TkUpDate.Enabled = true;
                                    else
                                        TkUpDate.Enabled = false;
                                }
                                break;
                            case "L": //long-click
                                kFlags ^= 0x41;
                                WriteFlags(); WriteLED();
                                break;
                            case "S": //single-click
                                kFlags ^= 0x10; // toggle single-click bit
                                WriteFlags(); WriteLED();
                                break;
                            case "T": //TKnob active
                                if (sCmd.Substring(0, 3) == "TK!")
                                {
                                    KnobPort.DiscardInBuffer();
                                    KnobPort.DiscardOutBuffer();
                                    KnobPort.Close();
                                    KnobPort.Dispose();
                                    Thread.Sleep(5000);
                                    SetgrpTKnob("Rebooting!");
                                    Thread.Sleep(5000);
                                    ReOpenKnobPort();
                                    KnobPort.Write("F;");
                                    WriteLED();
                                    updating = false;
                                }
                                if (sCmd.Substring(0, 3) == "TK~")
                                {
                                    // Bad Load, force re-load
                                    KnobPort.Write("F;");
                                }
                                break;
                            case "U": //tune something up
                                if (ActIdx == 0 || ActIdx == 1) // tune vfo
                                {
                                    string newSize = "1";
                                    int tkSize = 0;
                                    if (sCmd.Length > 3)
                                    { tkSize = Convert.ToInt32(sCmd.Substring(1, 2)); }
                                    if (tkSize > stepSize)
                                    { newSize = (stepSize + (tkSize - 1)).ToString(); }
                                    else
                                    { newSize = stepSize.ToString(); }
                                    WriteToPort(kCATup + newSize.PadLeft(2, '0') + ";", iSleep);
                                }
                                else if (ActIdx == 2)  // tune TX VFO
                                {
                                    string newFreq = "";
                                    int newSize = 0;
                                    int tkSize = 0;
                                    int TV = Convert.ToInt32(State["_ZZTV"].ToString());
                                    switch (stepSize)
                                    {
                                        case 0: newSize = 1; break;
                                        case 1: newSize = 10; break;
                                        case 2: newSize = 50; break;
                                        case 3: newSize = 100; break;
                                        case 4: newSize = 250; break;
                                        case 5: newSize = 500; break;
                                        case 6: newSize = 1000; break;
                                        case 7: newSize = 5000; break;
                                        case 8: newSize = 9000; break;
                                        case 9: newSize = 10000; break;
                                        case 10: newSize = 100000; break;
                                        case 11: newSize = 250000; break;
                                        case 12: newSize = 500000; break;
                                        case 13: newSize = 1000000; break;
                                        case 14: newSize = 10000000; break;
                                    }
                                    if (sCmd.Length > 3)
                                    {
                                        tkSize = Convert.ToInt32(sCmd.Substring(1, 2));
                                        newSize *= tkSize;
                                        newFreq = (TV + newSize).ToString();
                                    }
                                    else
                                    {
                                        newFreq = (TV + newSize).ToString();
                                    }
                                    WriteToPort(kCATdn + newFreq.PadLeft(11, '0') + ";", iSleep);
                                    WriteToPort("ZZTV;", iSleep);
                                }
                                else if (ActIdx == 3)    // tune RIT
                                {
                                    int RIT = 0;
                                    RITfrq += 2;
                                    if (RITfrq < 0)
                                    {
                                        RIT = (int)Math.Abs(RITfrq);
                                        kCATup = "ZZRF-" + RIT.ToString().PadLeft(4, '0') + ";";
                                    }
                                    else
                                    { kCATup = "ZZRF+" + RITfrq.ToString().PadLeft(4, '0') + ";"; }
                                    WriteToPort(kCATup, iSleep);
                                }
                                else if (ActIdx == 4)   // tune XIT
                                {
                                    int XIT = 0;
                                    XITfrq += 2;
                                    if (XITfrq < 0)
                                    {
                                        XIT = (int)Math.Abs(XITfrq);
                                        kCATup = "ZZXF-" + XIT.ToString().PadLeft(4, '0') + ";";
                                    }
                                    else
                                    { kCATup = "ZZXF+" + XITfrq.ToString().PadLeft(4, '0') + ";"; }
                                    WriteToPort(kCATup, iSleep);
                                }
                                else if (ActIdx == 5)   // audio volume
                                {
                                    if (volIdx < 0)
                                    { WriteToPort("ZZAG;", iSleep); }
                                    else if (volIdx < 100)
                                    {
                                        volIdx += 1;
                                        WriteToPort(kCATdn +
                                            volIdx.ToString().PadLeft(3, '0') + ";", iSleep);
                                    }
                                }
                                break;
                            case "X":   // Aux Switch functions
                                string sr = "";
                                MethodInfo info;
                                switch (sCmd.Substring(1, 2))
                                {
                                    case "1S":  // short click
                                        sr = Switch[set.cboS1SC].ToString(); break;
                                    case "1C":  // double click
                                        sr = Switch[set.cboS1DC].ToString(); break;
                                    case "1L":  // long click
                                        sr = Switch[set.cboS1LC].ToString(); break;
                                    case "2S":
                                        sr = Switch[set.cboS2SC].ToString(); break;
                                    case "2C":
                                        sr = Switch[set.cboS2DC].ToString(); break;
                                    case "2L":
                                        sr = Switch[set.cboS2LC].ToString(); break;
                                    case "3S":
                                        sr = Switch[set.cboS3SC].ToString(); break;
                                    case "3C":
                                        sr = Switch[set.cboS3DC].ToString(); break;
                                    case "3L":
                                        sr = Switch[set.cboS3LC].ToString(); break;
                                }
                                info = this.GetType().GetMethod(sr);
                                info.Invoke(this, null);

                                break;
                            case "Z":   // parameter settings from knob
                                if (sCmd.Substring(0, 2) == "ZC")
                                { SetZC(sCmd.Substring(2, 2)); }
                                if (sCmd.Substring(0, 2) == "ZE")
                                { SetZE(sCmd.Substring(2, 2)); }
                                if (sCmd.Substring(0, 2) == "ZL")
                                { SetZL(sCmd.Substring(2, 2)); }
                                if (sCmd.Substring(0, 2) == "ZR")
                                { SetZR(sCmd.Substring(2, 2)); }
                                break;
                        }
                        if (sCmd.Contains("!OK!")) // got a good load
                        {
                            SetgrpTKnob("Update Complete!");
                            KnobPort.Write("TK!;"); // switch to knob mode
                        }
                        else if (sCmd.Contains("!BAD!")) // got bad load
                        {
                            SetgrpTKnob("Update Failed!");
                            DialogResult result;
                            result = MessageBox.Show(new Form() { TopMost = true },
                                "There was an error updating the Tuning Knob firmware.\r\r" +
                                "Press YES to try the update again OR, Press NO to abort\r" +
                                "the operation and then consult the wiki documentation \r" +
                                "listed below for instructions for resolution.\r\r" +
                                "http://k5fr.com/ddutilwiki/index.php?title=Tuning_Knob#Updating_Firmware",
                                "Tuning Knob Update Failure",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            if (result == DialogResult.No)
                            { return; }
                            // try the update again
                            SendTkTextFile("USB Tuning Knob.hex");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                    "Unable to write to log");
            }
        }

        private void chkKnobEnab_CheckedChanged(object sender, EventArgs e)
        {
            if (chkKnobEnab.Checked)
            {
                set.chkKnobEnab = true;
                KnobInit();
            }
            else
            {
                set.chkKnobEnab = false;
            }
            set.Save();
        }

        private void cboKnobPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (KnobPort.IsOpen) KnobPort.Close();
            if (cboKnobPort.SelectedIndex > 0)
            {
                TkComNum = cboKnobPort.SelectedItem.ToString();
                KnobPort.PortName = cboKnobPort.SelectedItem.ToString();
                try
                {
                    KnobPort.Open();
                }
                catch
                {
                    MessageBox.Show(new Form() { TopMost = true }, 
                        "The Tuning Knob serial port " + KnobPort.PortName +
                        " cannot be opened!\n", "Port Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboKnobPort.SelectedIndex = 0;
                    chkKnobEnab.Checked = false;
                }
            }
            set.cboKnobPort = cboKnobPort.SelectedIndex;
            set.Save();
        }

        private void cboKnobAOn_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.cboKnobAOn = cboKnobAOn.SelectedIndex; set.Save(); WriteFlags();
        }

        private void cboKnobBOn_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.cboKnobBOn = cboKnobBOn.SelectedIndex; set.Save(); WriteFlags();
        }

        private void cboKnobAOff_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.cboKnobAOff = cboKnobAOff.SelectedIndex; set.Save(); WriteFlags();
        }

        private void cboKnobBOff_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.cboKnobBOff = cboKnobBOff.SelectedIndex; set.Save(); WriteFlags();
        }

        private void cboKnobADC_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.cboKnobADC = cboKnobADC.SelectedIndex; set.Save(); WriteFlags();
        }

        private void cboKnobBDC_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.cboKnobBDC = cboKnobBDC.SelectedIndex; set.Save(); WriteFlags();
        }

        private void chkKnobAdv_CheckedChanged(object sender, EventArgs e)
        {
            if (chkKnobExpert.Checked)
            {
                txtZC.Visible = true; txtZE.Visible = true; 
                txtZL.Visible = true; txtZR.Visible = true;
                lblZC.Visible = true; lblZE.Visible = true; 
                lblZL.Visible = true; lblZR.Visible = true;
            }
            else
            {
                txtZC.Visible = false; txtZE.Visible = false; 
                txtZL.Visible = false; txtZR.Visible = false;
                lblZC.Visible = false; lblZE.Visible = false; 
                lblZL.Visible = false; lblZR.Visible = false;
            }
        }

        private void txtZC_DoubleClick(object sender, EventArgs e)
        {
            KnobPort.Write("ZC14;" + "ZC;");
        }

        private void txtZC_TextChanged(object sender, EventArgs e)
        {
            if (txtZC.Text.Length == 2) 
                KnobPort.Write("ZC"+txtZC.Text+";" + "ZC;");
        }

        private void lblZE_DoubleClick(object sender, EventArgs e)
        {
            if (txtZE.Enabled) txtZE.Enabled = false;
            else txtZE.Enabled = true;
        }

        private void txtZE_DoubleClick(object sender, EventArgs e)
        {
            KnobPort.Write("ZE01;" + "ZE;");
        }

        private void txtZE_TextChanged(object sender, EventArgs e)
        {
            if (txtZE.Text.Length == 2)
                KnobPort.Write("ZE" + txtZE.Text + ";" + "ZE;");
        }

        private void txtZL_TextChanged(object sender, EventArgs e)
        {
            if (txtZL.Text.Length == 2)
                KnobPort.Write("ZL" + txtZL.Text + ";" + "ZL;");
        }

        private void txtZL_DoubleClick(object sender, EventArgs e)
        {
            KnobPort.Write("ZL64;" + "ZL;");
        }

        private void txtZR_TextChanged(object sender, EventArgs e)
        {
            if (txtZR.Text.Length == 2)
                KnobPort.Write("ZR" + txtZR.Text + ";" + "ZR;");
        }

        private void txtZR_DoubleClick(object sender, EventArgs e)
        {
            KnobPort.Write("ZR0A;" + "ZR;");
        }

        private void tabRCP_DoubleClick(object sender, EventArgs e)
        {
            if (grpTKnob.Visible) grpTKnob.Visible = false;
            else grpTKnob.Visible = true;
        }

        private void cboTstep_SelectedIndexChanged(object sender, EventArgs e)
        {
            //tempTimer.Stop();
            stepSize = cboTstep.SelectedIndex;
            WriteToPort("ZZAC" + stepSize.ToString().PadLeft(2, '0') + ";", 0);
            set.cboTstep = cboTstep.SelectedIndex;
            set.Save();
            //tempTimer.Start();
        }

        private void chkRitClr_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRitClr.Checked)
                set.chkRitClr = true;
            else 
                set.chkRitClr = false;
            set.Save();
        }

        private void txtCWspd_TextChanged(object sender, EventArgs e)
        {
            if (txtCWspd.Text != "" && txtCWspd.Text != null)
            {
                set.speedStep = Convert.ToInt32(txtCWspd.Text);
                set.Save();
            }
        }

        private void cboS1SC_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.cboS1SC = cboS1SC.Text;
            set.Save();
        }

        private void cboS1DC_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.cboS1DC = cboS1DC.Text;
            set.Save();
        }

        private void cboS1LC_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.cboS1LC = cboS1LC.Text;
            set.Save();
        }

        private void cboS2SC_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.cboS2SC = cboS2SC.Text;
            set.Save();
        }

        private void cboS2DC_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.cboS2DC = cboS2DC.Text;
            set.Save();
        }

        private void cboS2LC_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.cboS2LC = cboS2LC.Text;
            set.Save();
        }

        private void cboS3SC_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.cboS3SC = cboS3SC.Text;
            set.Save();
        }

        private void cboS3DC_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.cboS3DC = cboS3DC.Text;
            set.Save();
        }

        private void cboS3LC_SelectedIndexChanged(object sender, EventArgs e)
        {
            set.cboS3LC = cboS3LC.Text;
            set.Save();
        }

        #endregion * Events *

        #region * Methods *

        #region ** Aux Sw Functions **

        public void ClrRit()
        {
            WriteToPort("ZZRC;", iSleep);
        }
        public void ClrXit()
        {
            WriteToPort("ZZXC;", iSleep);
        }
        public void CwSpdDwn()
        {
            if (State["_ZZCS"] != null)
            {
                int spd = 0;
                string newspd = "";
                spd = Convert.ToInt32(State["_ZZCS"].ToString());
                newspd = "ZZCS" +
                    (spd - set.speedStep).ToString().PadLeft(2, '0') + ";";
                WriteToPort(newspd, iSleep);
            }
            WriteToPort("ZZCS;", iSleep);

        }
        public void CwSpdUp()
        {
            if (State["_ZZCS"] != null)
            {
                int spd = 0;
                string newspd = "";
                spd = Convert.ToInt32(State["_ZZCS"].ToString());
                newspd = "ZZCS" +
                    (spd + set.speedStep).ToString().PadLeft(2, '0') + ";";
                WriteToPort(newspd, iSleep);
            }
            WriteToPort("ZZCS;", iSleep);
        }
        public void CwxSpdDwn()
        {
            if (State["_ZZKS"] != null)
            {
                int spd = 0;
                string newspd = "";
                spd = Convert.ToInt32(State["_ZZKS"].ToString());
                newspd = "ZZKS" +
                    (spd - set.speedStep).ToString().PadLeft(3, '0') + ";";
                WriteToPort(newspd, iSleep);
            }
        }
        public void CwxSpdUp()
        {
            if (State["_ZZKS"] != null)
            {
                int spd = 0;
                string newspd = "";
                spd = Convert.ToInt32(State["_ZZKS"].ToString());
                newspd = "ZZKS" +
                    (spd + set.speedStep).ToString().PadLeft(3, '0') + ";";
                WriteToPort(newspd, iSleep);
            }
        }
        public void FltrDwn()
        {
            int flt = Convert.ToInt32(State["_ZZFI"].ToString());
            if (flt >= 0 && flt < 11)
                WriteToPort("ZZFI" + (flt + 1).ToString().PadLeft(2, '0') + ";", iSleep);
            else if (flt >= 11)
                WriteToPort("ZZFI00;", iSleep);
            else 
                WriteToPort("ZZFI00;", iSleep);
        }
        public void FltrUp()
        {
            int flt = Convert.ToInt32(State["_ZZFI"].ToString());
            if (flt > 0 && flt <= 11)
                WriteToPort("ZZFI" + (flt - 1).ToString().PadLeft(2, '0') + ";", iSleep);
            else if (flt <= 0)
                WriteToPort("ZZFI11;", iSleep);
            else
                WriteToPort("ZZFI00;", iSleep);
        }
        public void M30()
        {
            ProcessTkSwMacro(30);
        }
        public void M31()
        {
            ProcessTkSwMacro(31);
        }
        public void M32()
        {
            ProcessTkSwMacro(32);
        }
        public void M33()
        {
            ProcessTkSwMacro(33);
        }
        public void M34()
        {
            ProcessTkSwMacro(34);
        }
        public void ResFreq()
        {
            WriteToPort("FA" + memSave + ";", iSleep);
        }
        public void SavFreq()
        {
            memSave = vfoA;
        }
        public void Split()
        {
            if (State["_ZZSP"].ToString() == "0")
                WriteToPort("ZZSP1;", iSleep);
            else
                WriteToPort("ZZSP0;", iSleep);
        }
        public void ExAB()
        {
            WriteToPort("ZZVS2;", iSleep);
        }
        public void TxSwap()
        {
            if (ZZSW == 0)
                WriteToPort("ZZSW1;", iSleep);
            else
                WriteToPort("ZZSW0;", iSleep);
        }
        public void TunStpDwn()
        {
            WriteToPort("ZZSD;", iSleep);
        }
        public void TunStpUp()
        {
            WriteToPort("ZZSU;", iSleep);
        }
        public void A2B()
        {
            WriteToPort("ZZVS0;", iSleep);
        }
        public void B2A()
        {
            WriteToPort("ZZVS1;", iSleep);
        }
        public void A2TX()
        {
            WriteToPort("ZZTV" + vfoA + ";", iSleep);
            WriteToPort("ZZTV;", iSleep);
        }
        public void B2TX()
        {
            WriteToPort("ZZTV" + vfoB + ";", iSleep);
            WriteToPort("ZZTV;", iSleep);
        }

        #endregion ** Aux Sw Functions **

        //initialize controls
        void KnobInit()
        {
            #region Aux Switch Table
            // Aux switch function location
            try
            {
                Switch.Add("Clear RIT", "ClrRit");
                Switch.Add("Clear XIT", "ClrXit");
                Switch.Add("CW Spd Dwn", "CwSpdDwn");
                Switch.Add("CW Spd Up", "CwSpdUp");
                Switch.Add("CWX Spd Dwn", "CwxSpdDwn");
                Switch.Add("CWX Spd Up", "CwxSpdUp");
                Switch.Add("Filter Step Dwn", "FltrDwn");
                Switch.Add("Filter Step Up", "FltrUp");
                Switch.Add("Macro M30", "M30");
                Switch.Add("Macro M31", "M31");
                Switch.Add("Macro M32", "M32");
                Switch.Add("Macro M33", "M33");
                Switch.Add("Macro M34", "M34");
                Switch.Add("Restore VFO A", "ResFreq");
                Switch.Add("Save VFO A", "SavFreq");
                Switch.Add("Split On/Off", "Split");
                Switch.Add("Swap A<>B", "ExAB");
                Switch.Add("Swap TX VFO", "TxSwap");
                Switch.Add("Tune Step Dwn", "TunStpDwn");
                Switch.Add("Tune Step Up", "TunStpUp");
                Switch.Add("VFO A>B", "A2B");
                Switch.Add("VFO A<B", "B2A");
                Switch.Add("VFOA > TX VFO", "A2TX");
                Switch.Add("VFOB > TX VFO", "B2TX");
            }
            catch { }
            #endregion Aux Switch Table

            try
            {
                cboKnobPort.SelectedIndex = set.cboKnobPort;
                chkKnobEnab.Checked = set.chkKnobEnab;

                if (chkKnobEnab.Checked)
                {
                    TkUpDate.Enabled = true;
                    cboKnobAOn.SelectedIndex = set.cboKnobAOn;
                    cboKnobAOff.SelectedIndex = set.cboKnobAOff;
                    cboKnobBOn.SelectedIndex = set.cboKnobBOn;
                    cboKnobBOff.SelectedIndex = set.cboKnobBOff;
                    cboKnobADC.SelectedIndex = set.cboKnobADC;
                    cboKnobBDC.SelectedIndex = set.cboKnobBDC;
                    cboTstep.SelectedIndex = set.cboTstep;
                    WriteToPort("ZZCS;ZZTV;", iSleep);
                    txtCWspd.Text = set.speedStep.ToString();
                    chkRitClr.Checked = set.chkRitClr;
                    KnobPort.Write("F;ZC;ZE;ZL;ZR;");
                    WriteFlags();
                    WriteLED();
                }
                // Add aux switch functions to combo boxs
                ArrayList al = new ArrayList(Switch.Keys);
                al.Sort();
                cboS1SC.Items.Clear();
                cboS1DC.Items.Clear();
                cboS1LC.Items.Clear();
                cboS2SC.Items.Clear();
                cboS2DC.Items.Clear();
                cboS2LC.Items.Clear();
                cboS3SC.Items.Clear();
                cboS3DC.Items.Clear();
                cboS3LC.Items.Clear();

                foreach (Object f in al)
                {
                    cboS1SC.Items.Add(f);
                    cboS1DC.Items.Add(f);
                    cboS1LC.Items.Add(f);
                    cboS2SC.Items.Add(f);
                    cboS2DC.Items.Add(f);
                    cboS2LC.Items.Add(f);
                    cboS3SC.Items.Add(f);
                    cboS3DC.Items.Add(f);
                    cboS3LC.Items.Add(f);
                }
                cboS1SC.Text = set.cboS1SC;
                cboS1DC.Text = set.cboS1DC;
                cboS1LC.Text = set.cboS1LC;
                cboS2SC.Text = set.cboS2SC;
                cboS2DC.Text = set.cboS2DC;
                cboS2LC.Text = set.cboS2LC;
                cboS3SC.Text = set.cboS3SC;
                cboS3DC.Text = set.cboS3DC;
                cboS3LC.Text = set.cboS3LC;
            }
            catch (Exception ex)
            {
                bool bReturnLog = false;
                bReturnLog = ErrorLog.ErrorRoutine(false, enableErrorLog, ex);
                if (false == bReturnLog) MessageBox.Show(new Form() { TopMost = true },
                    "Unable to write to log");
                chkKnobEnab.Checked = false;
            }
        }
        
        void WriteLED() // set the knob leds
        {
            string led1 = (kFlags >> 6 & 1).ToString(); // long click (Ref)
            string led2 = (kFlags >> 4 & 1).ToString(); // single click (Yellow)
            string led3 = (kFlags >> 5 & 1).ToString(); // double click (Green)
            string led = "I" + led1 + led2 + led3 + ";";
            KnobPort.Write(led);

            if (led1 == "1")
            {
                TkRed.BackColor = Color.Red;
                mini.TkRed.BackColor = Color.Red;
            }
            else
            { 
                TkRed.BackColor = Color.Empty;
                mini.TkRed.BackColor = Color.Empty;
            }
            if (led2 == "1")
            { 
                TkYel.BackColor = Color.Yellow;
                mini.TkYel.BackColor = Color.Yellow;
            }
            else
            { 
                TkYel.BackColor = Color.Empty;
                mini.TkYel.BackColor = Color.Empty;
            }
            if (led3 == "1")
            {
                TkGrn.BackColor = Color.Lime;
                mini.TkGrn.BackColor = Color.Lime;
            }
            else
            {
                TkGrn.BackColor = Color.Empty;
                mini.TkGrn.BackColor = Color.Empty;
            }

        }

        void WriteFlags()   // set indicators based on active control
        {
            if (Convert.ToBoolean(kFlags & 0x10))
            {   // single click on
                if (Convert.ToBoolean(kFlags & 0x01))
                {   // mode A
                    ActCon = 1; ActConDC = 1;
                    SetModeAOn("True"); SetModeBOn("False");
                    SetModeAOff("False"); SetModeBOff("False");
                    SetModeADC("True"); SetModeBDC("False");
                }
                else
                {   // mode B
                    ActCon = 3; ActConDC = 2;
                    SetModeBOn("True"); SetModeAOn("False");
                    SetModeAOff("False"); SetModeBOff("False");
                    SetModeADC("False"); SetModeBDC("True");
                }
            }
            else
            {   // single click off
                if (Convert.ToBoolean(kFlags & 0x01)) 
                {   // mode A
                    ActCon = 2; ActConDC = 1;
                    SetModeAOn("False"); SetModeBOn("False");
                    SetModeAOff("True"); SetModeBOff("False");
                    SetModeADC("True"); SetModeBDC("False");
                }
                else
                {   // mode B
                    ActCon = 4; ActConDC = 2;
                    SetModeBOn("False"); SetModeAOn("False");
                    SetModeAOff("False"); SetModeBOff("True");
                    SetModeADC("False"); SetModeBDC("True");
                }
            }
            GetCat();
        }
        
        void GetCat()   //Get the CAT commands for the active controls
        {
            switch (ActIdx) // active single click control
            {
                case 0: kCATup = "ZZAU"; kCATdn = "ZZAD"; //VFO A
                    if (chkRitClr.Checked)
                    { WriteToPort("ZZXC;ZZXS0;ZZRC;ZZRT0;", iSleep); }
                    break;
                case 1: kCATup = "ZZBP"; kCATdn = "ZZBM"; //VFO B
                    if (chkRitClr.Checked)
                    { WriteToPort("ZZXC;ZZXS0;ZZRC;ZZRT0;", iSleep); }
                    break;
                case 2: kCATup = "ZZTV"; kCATdn = "ZZTV"; //Xmit VFO
                    if (chkRitClr.Checked)
                    { WriteToPort("ZZXC;ZZXS0;ZZRC;ZZRT0;", iSleep); }
                    WriteToPort("ZZTV;", iSleep);
                    break;
                case 3: kCATup = "ZZRF"; kCATdn = "ZZRF"; //TUNE RIT
                    if (chkRitClr.Checked)
                    { RITfrq = 0; WriteToPort("ZZXC;ZZXS0;ZZRT1;", iSleep); }
                    WriteToPort("ZZRT1;", iSleep); 
                    break;
                case 4: kCATup = "ZZXF"; kCATdn = "ZZXF"; //TUNE XIT
                    if (chkRitClr.Checked)
                    { XITfrq = 0; WriteToPort("ZZRC;ZZRT0;ZZXS1;", iSleep); }
                    WriteToPort("ZZXS1;", iSleep);
                    break;
                case 5: kCATup = "ZZAG"; kCATdn = "ZZAG"; //Audio Volume
                    WriteToPort("ZZAG;", iSleep);
                    if (chkRitClr.Checked)
                    { WriteToPort("ZZXC;ZZXS0;ZZRC;ZZRT0;", iSleep); }
                    break;
            }
            switch (ActIdxDC) // active double click control
            {
                case 0: kCATdcOn = "ZZSW1;"; kCATdcOff = "ZZSW0;"; //Toggle TX VFO
                    break;
                case 1: kCATdcOn = "ZZVS2;"; kCATdcOff = "ZZVS2;"; //Exchange A<>B
                    break;
                case 2: kCATdcOn = "ZZSP1;"; kCATdcOff = "ZZSP0;"; //Toggle Split
                    break;
                case 3: kCATdcOn = "ZZRC;"; kCATdcOff = "ZZRC;";   //Clear RIT
                    break;
                case 4: kCATdcOn = "ZZXC;"; kCATdcOff = "ZZXC;";    //Clear XIT
                    break;
            }
        }
        // The update button was pressed
        public void ProcessTkSwMacro(int macro)
        {
            try
            {
                if (StepCtr == 0)// && (xOn == "0" || xOn == ""))
                {   // 
                    if (dgm.Rows[macro - 1].Cells[1].Value.ToString() == "")
                    {
                        throw new Exception();
                    }
                    string cmds = dgm.Rows[macro - 1].Cells[1].Value.ToString();
                    ParseBuffer(cmds);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (NullReferenceException)
            {
                MessageBox.Show(new Form() { TopMost = true },
                    "There are no commands setup for this Macro " + macro.ToString());
            }
        }
        // The knob update button was pressed.
        private void TkUpDate_Click(object sender, EventArgs e)
        {
            DialogResult result;
            result = MessageBox.Show(new Form() { TopMost = true },
                "There is a new version of the Tuning Knob Firmware available.\r\r" +
                "Press 'Yes' to start the update process or press 'No' to abort.\r\r" +
                "Please wait until the operation is finished before touching any keys",
                "Tuning Knob Firmware Update",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.No)
            { return; }
            KnobPort.Write("BL!;");
            SetgrpTKnob("Preparing to Update!");
     }
        // send TK update file
        private void SendTkTextFile(string file)
        {
            using (StreamReader sr = new StreamReader(file))
            {
                while (sr.Peek() >= 0)
                {
                    KnobPort.WriteLine(sr.ReadLine());
                    Thread.Sleep(20);
                }
            }
            KnobPort.Write("F;");

        }
        // re-initialize knob port after closing during firmware update.
        void ReOpenKnobPort()
        {
            try
            {
                this.KnobPort = new System.IO.Ports.SerialPort(this.components);
                this.KnobPort.RtsEnable = true;
                this.KnobPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.KnobPort_DataReceived);
                if (KnobPort.IsOpen) KnobPort.Close();
                KnobPort.PortName = TkComNum;
                KnobPort.Open();
            }
            catch (Exception e)
            {
                MessageBox.Show(new Form() { TopMost = true },
                    e.Message + "\r\r" + e.StackTrace, "ReOpenKnobPort Error",
                   MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        #endregion * Methods *

        #endregion Tuning Knob

        #region VHF+ Matrix

        #region # Declarations #

        const int base10 = 10;
        char[] cHexa = new char[] { 'A', 'B', 'C', 'D', 'E', 'F' };

        #endregion Declarations

        #region # Delegates #

        // Write to band button
        delegate void SetVHFCallback(string text);
        public void SetVHF(string text)
        {
            if (this.lblVHF.InvokeRequired)
            {
                SetVHFCallback d = new SetVHFCallback(SetVHF);
                this.Invoke(d, new object[] { text });
            }
            else
            { lblVHF.Text = text; }
        }
        #endregion # Delegates #

        #region # Methods #

        // Send data to parallel port A
        void MatrixOutA(int port, int data)
        {
            if (chkInvertA.Checked) PortAccess.Output(port, 255 ^ data);
            else PortAccess.Output(port, data);
        }
        // Send data to parallel port B
        void MatrixOutB(int port, int data)
        {
            if (chkInvertB.Checked) PortAccess.Output(port, 255 ^ data);
            else PortAccess.Output(port, data);
        }
        int aPort;
        int bPort;
        // Load the x2 matrix controls from settings
        void X2SetUp()
        {
            string bin = "";
            chkPortA.Checked = set.chkPortA;
            chkPortB.Checked = set.chkPortB;
            txtPortA.Text = set.aPortNum;
            txtPortB.Text = set.bPortNum;
            chkInvertA.Checked = set.chkInvertA;
            chkInvertB.Checked = set.chkInvertB;
            chkFWa.Checked = set.chkFWa;
            chkFWb.Checked = set.chkFWb;
            //aPort = Convert.ToInt32(set.aPortNum);
            //bPort = Convert.ToInt32(set.bPortNum);

            //store the set rec vars as they will be overwritten during the load.
            int s2a0 = set.x2a0, s2a1 = set.x2a1, s2a2 = set.x2a2, s2a3 = set.x2a3;
            int s2a4 = set.x2a4, s2a5 = set.x2a5, s2a6 = set.x2a6, s2a7 = set.x2a7;
            int s2a8 = set.x2a8, s2a9 = set.x2a9, s2a10 = set.x2a10, s2a11 = set.x2a11;
            int s2a12 = set.x2a12, s2a13 = set.x2a13, s2a14 = set.x2a14, s2a15 = set.x2a15;
            //store the set trans vars as they will be overwritten during the load.
            int s2b0 = set.x2b0, s2b1 = set.x2b1, s2b2 = set.x2b2, s2b3 = set.x2b3;
            int s2b4 = set.x2b4, s2b5 = set.x2b5, s2b6 = set.x2b6, s2b7 = set.x2b7;
            int s2b8 = set.x2b8, s2b9 = set.x2b9, s2b10 = set.x2b10, s2b11 = set.x2b11;
            int s2b12 = set.x2b12, s2b13 = set.x2b13, s2b14 = set.x2b14, s2b15 = set.x2b15;
            // Load receive matrix
            if (chkPortA.Checked)
            {
                bin = DecimalToBase(s2a0, 2);
                LoadCheckBoxes(bin, "cb0r", grpPortA);
                bin = DecimalToBase(s2a1, 2);
                LoadCheckBoxes(bin, "cb1r", grpPortA);
                bin = DecimalToBase(s2a2, 2);
                LoadCheckBoxes(bin, "cb2r", grpPortA);
                bin = DecimalToBase(s2a3, 2);
                LoadCheckBoxes(bin, "cb3r", grpPortA);
                bin = DecimalToBase(s2a4, 2);
                LoadCheckBoxes(bin, "cb4r", grpPortA);
                bin = DecimalToBase(s2a5, 2);
                LoadCheckBoxes(bin, "cb5r", grpPortA);
                bin = DecimalToBase(s2a6, 2);
                LoadCheckBoxes(bin, "cb6r", grpPortA);
                bin = DecimalToBase(s2a7, 2);
                LoadCheckBoxes(bin, "cb7r", grpPortA);
                bin = DecimalToBase(s2a8, 2);
                LoadCheckBoxes(bin, "cb8r", grpPortA);
                bin = DecimalToBase(s2a9, 2);
                LoadCheckBoxes(bin, "cb9r", grpPortA);
                bin = DecimalToBase(s2a10, 2);
                LoadCheckBoxes(bin, "cb10r", grpPortA);
                bin = DecimalToBase(s2a11, 2);
                LoadCheckBoxes(bin, "cb11r", grpPortA);
                bin = DecimalToBase(s2a12, 2);
                LoadCheckBoxes(bin, "cb12r", grpPortA);
                bin = DecimalToBase(s2a13, 2);
                LoadCheckBoxes(bin, "cb13r", grpPortA);
                bin = DecimalToBase(s2a14, 2);
                LoadCheckBoxes(bin, "cb14r", grpPortA);
                bin = DecimalToBase(s2a15, 2);
                LoadCheckBoxes(bin, "cb15r", grpPortA);
            }
            // Load transmit matrix
            if (chkPortB.Checked)
            {
                bin = DecimalToBase(s2b0, 2);
                LoadCheckBoxes(bin, "cb0t", grpPortB);
                bin = DecimalToBase(s2b1, 2);
                LoadCheckBoxes(bin, "cb1t", grpPortB);
                bin = DecimalToBase(s2b2, 2);
                LoadCheckBoxes(bin, "cb2t", grpPortB);
                bin = DecimalToBase(s2b3, 2);
                LoadCheckBoxes(bin, "cb3t", grpPortB);
                bin = DecimalToBase(s2b4, 2);
                LoadCheckBoxes(bin, "cb4t", grpPortB);
                bin = DecimalToBase(s2b5, 2);
                LoadCheckBoxes(bin, "cb5t", grpPortB);
                bin = DecimalToBase(s2b6, 2);
                LoadCheckBoxes(bin, "cb6t", grpPortB);
                bin = DecimalToBase(s2b7, 2);
                LoadCheckBoxes(bin, "cb7t", grpPortB);
                bin = DecimalToBase(s2b8, 2);
                LoadCheckBoxes(bin, "cb8t", grpPortB);
                bin = DecimalToBase(s2b9, 2);
                LoadCheckBoxes(bin, "cb9t", grpPortB);
                bin = DecimalToBase(s2b10, 2);
                LoadCheckBoxes(bin, "cb10t", grpPortB);
                bin = DecimalToBase(s2b11, 2);
                LoadCheckBoxes(bin, "cb11t", grpPortB);
                bin = DecimalToBase(s2b12, 2);
                LoadCheckBoxes(bin, "cb12t", grpPortB);
                bin = DecimalToBase(s2b13, 2);
                LoadCheckBoxes(bin, "cb13t", grpPortB);
                bin = DecimalToBase(s2b14, 2);
                LoadCheckBoxes(bin, "cb14t", grpPortB);
                bin = DecimalToBase(s2b15, 2);
                LoadCheckBoxes(bin, "cb15t", grpPortB);
            }
        }
        // load checkboxes
        void LoadCheckBoxes(string bin, string cbBase, Control grp)
        {
            CheckBox cb;
            int j = bin.Length;
            for (int i = 0; i < bin.Length; i++)
            {
                foreach (Control c in grp.Controls)
                {
                    if (c.Name == cbBase + i)
                    {
                        j -= 1;
                        if (bin.Substring(j, 1) == "1")
                        {
                            //str = cbBase + i.ToString();
                            cb = (CheckBox)c;
                            cb.Checked = true;
                        }
                    }
                }
            }
        }
        // decode binary string
        string DecimalToBase(int iDec, int numbase)
        {
            string strBin = "";
            int[] result = new int[32];
            int MaxBit = 32;
            for (; iDec > 0; iDec /= numbase)
            {
                int rem = iDec % numbase;
                result[--MaxBit] = rem;
            }
            for (int i = 0; i < result.Length; i++)
                if ((int)result.GetValue(i) >= base10)
                    strBin += cHexa[(int)result.GetValue(i) % base10];
                else
                    strBin += result.GetValue(i);
            strBin = strBin.TrimStart(new char[] { '0' });
            return strBin;
        }
        #endregion Methods

        #region # Events #

        #region * grpPortA_CheckedChanged Events *

        int x2a0 = 0, x2a1 = 0, x2a2 = 0, x2a3 = 0;
        int x2a4 = 0, x2a5 = 0, x2a6 = 0, x2a7 = 0;
        int x2a8 = 0, x2a9 = 0, x2a10 = 0, x2a11 = 0;
        int x2a12 = 0, x2a13 = 0, x2a14 = 0, x2a15 = 0;

        private void grpPortA_CheckedChanged(object sender, EventArgs e)
        {
            x2a0 = 0;
            if (chk0r0.Checked) x2a0 += 1;
            if (chk0r1.Checked) x2a0 += 2;
            if (chk0r2.Checked) x2a0 += 4;
            if (chk0r3.Checked) x2a0 += 8;
            if (chk0r4.Checked) x2a0 += 16;
            if (chk0r5.Checked) x2a0 += 32;
            if (chk0r6.Checked) x2a0 += 64;
            if (chk0r7.Checked) x2a0 += 128;
            set.x2a0 = x2a0;
            x2a1 = 0;
            if (chk1r0.Checked) x2a1 += 1;
            if (chk1r1.Checked) x2a1 += 2;
            if (chk1r2.Checked) x2a1 += 4;
            if (chk1r3.Checked) x2a1 += 8;
            if (chk1r4.Checked) x2a1 += 16;
            if (chk1r5.Checked) x2a1 += 32;
            if (chk1r6.Checked) x2a1 += 64;
            if (chk1r7.Checked) x2a1 += 128;
            set.x2a1 = x2a1;
            x2a2 = 0;
            if (chk2r0.Checked) x2a2 += 1;
            if (chk2r1.Checked) x2a2 += 2;
            if (chk2r2.Checked) x2a2 += 4;
            if (chk2r3.Checked) x2a2 += 8;
            if (chk2r4.Checked) x2a2 += 16;
            if (chk2r5.Checked) x2a2 += 32;
            if (chk2r6.Checked) x2a2 += 64;
            if (chk2r7.Checked) x2a2 += 128;
            set.x2a2 = x2a2;
            x2a3 = 0;
            if (chk3r0.Checked) x2a3 += 1;
            if (chk3r1.Checked) x2a3 += 2;
            if (chk3r2.Checked) x2a3 += 4;
            if (chk3r3.Checked) x2a3 += 8;
            if (chk3r4.Checked) x2a3 += 16;
            if (chk3r5.Checked) x2a3 += 32;
            if (chk3r6.Checked) x2a3 += 64;
            if (chk3r7.Checked) x2a3 += 128;
            set.x2a3 = x2a3;
            x2a4 = 0;
            if (chk4r0.Checked) x2a4 += 1;
            if (chk4r1.Checked) x2a4 += 2;
            if (chk4r2.Checked) x2a4 += 4;
            if (chk4r3.Checked) x2a4 += 8;
            if (chk4r4.Checked) x2a4 += 16;
            if (chk4r5.Checked) x2a4 += 32;
            if (chk4r6.Checked) x2a4 += 64;
            if (chk4r7.Checked) x2a4 += 128;
            set.x2a4 = x2a4;
            x2a5 = 0;
            if (chk5r0.Checked) x2a5 += 1;
            if (chk5r1.Checked) x2a5 += 2;
            if (chk5r2.Checked) x2a5 += 4;
            if (chk5r3.Checked) x2a5 += 8;
            if (chk5r4.Checked) x2a5 += 16;
            if (chk5r5.Checked) x2a5 += 32;
            if (chk5r6.Checked) x2a5 += 64;
            if (chk5r7.Checked) x2a5 += 128;
            set.x2a5 = x2a5;
            x2a6 = 0;
            if (chk6r0.Checked) x2a6 += 1;
            if (chk6r1.Checked) x2a6 += 2;
            if (chk6r2.Checked) x2a6 += 4;
            if (chk6r3.Checked) x2a6 += 8;
            if (chk6r4.Checked) x2a6 += 16;
            if (chk6r5.Checked) x2a6 += 32;
            if (chk6r6.Checked) x2a6 += 64;
            if (chk6r7.Checked) x2a6 += 128;
            set.x2a6 = x2a6;
            x2a7 = 0;
            if (chk7r0.Checked) x2a7 += 1;
            if (chk7r1.Checked) x2a7 += 2;
            if (chk7r2.Checked) x2a7 += 4;
            if (chk7r3.Checked) x2a7 += 8;
            if (chk7r4.Checked) x2a7 += 16;
            if (chk7r5.Checked) x2a7 += 32;
            if (chk7r6.Checked) x2a7 += 64;
            if (chk7r7.Checked) x2a7 += 128;
            set.x2a7 = x2a7;
            x2a8 = 0;
            if (chk8r0.Checked) x2a8 += 1;
            if (chk8r1.Checked) x2a8 += 2;
            if (chk8r2.Checked) x2a8 += 4;
            if (chk8r3.Checked) x2a8 += 8;
            if (chkb8r4.Checked) x2a8 += 16;
            if (chk8r5.Checked) x2a8 += 32;
            if (chk8r6.Checked) x2a8 += 64;
            if (chk8r7.Checked) x2a8 += 128;
            set.x2a8 = x2a8;
            x2a9 = 0;
            if (chk9r0.Checked) x2a9 += 1;
            if (chk9r1.Checked) x2a9 += 2;
            if (chk9r2.Checked) x2a9 += 4;
            if (chk9r3.Checked) x2a9 += 8;
            if (chk9r4.Checked) x2a9 += 16;
            if (chk9r5.Checked) x2a9 += 32;
            if (chk9r6.Checked) x2a9 += 64;
            if (chk9r7.Checked) x2a9 += 128;
            set.x2a9 = x2a9;
            x2a10 = 0;
            if (chk10r0.Checked) x2a10 += 1;
            if (chk10r1.Checked) x2a10 += 2;
            if (chk10r2.Checked) x2a10 += 4;
            if (chk10r3.Checked) x2a10 += 8;
            if (chk10r4.Checked) x2a10 += 16;
            if (chk10r5.Checked) x2a10 += 32;
            if (chk10r6.Checked) x2a10 += 64;
            if (chk10r7.Checked) x2a10 += 128;
            set.x2a10 = x2a10;
            x2a11 = 0;
            if (chk11r0.Checked) x2a11 += 1;
            if (chk11r1.Checked) x2a11 += 2;
            if (chk11r2.Checked) x2a11 += 4;
            if (chk11r3.Checked) x2a11 += 8;
            if (chk11r4.Checked) x2a11 += 16;
            if (chk11r5.Checked) x2a11 += 32;
            if (chk11r6.Checked) x2a11 += 64;
            if (chk11r7.Checked) x2a11 += 128;
            set.x2a11 = x2a11;
            x2a12 = 0;
            if (chk12r0.Checked) x2a12 += 1;
            if (chk12r1.Checked) x2a12 += 2;
            if (chk12r2.Checked) x2a12 += 4;
            if (chk12r3.Checked) x2a12 += 8;
            if (chk12r4.Checked) x2a12 += 16;
            if (chk12r5.Checked) x2a12 += 32;
            if (chk12r6.Checked) x2a12 += 64;
            if (chk12r7.Checked) x2a12 += 128;
            set.x2a12 = x2a12;
            x2a13 = 0;
            if (chk13r0.Checked) x2a13 += 1;
            if (chk13r1.Checked) x2a13 += 2;
            if (chk13r2.Checked) x2a13 += 4;
            if (chk13r3.Checked) x2a13 += 8;
            if (chk13r4.Checked) x2a13 += 16;
            if (chk13r5.Checked) x2a13 += 32;
            if (chk13r6.Checked) x2a13 += 64;
            if (chk13r7.Checked) x2a13 += 128;
            set.x2a13 = x2a13;
            x2a14 = 0;
            if (chk14r0.Checked) x2a14 += 1;
            if (chk14r1.Checked) x2a14 += 2;
            if (chk14r2.Checked) x2a14 += 4;
            if (chk14r3.Checked) x2a14 += 8;
            if (chk14r4.Checked) x2a14 += 16;
            if (chk14r5.Checked) x2a14 += 32;
            if (chk14r6.Checked) x2a14 += 64;
            if (chk14r7.Checked) x2a14 += 128;
            set.x2a14 = x2a14;
            x2a15 = 0;
            if (chk15r0.Checked) x2a15 += 1;
            if (chk15r1.Checked) x2a15 += 2;
            if (chk15r2.Checked) x2a15 += 4;
            if (chk15r3.Checked) x2a15 += 8;
            if (chk15r4.Checked) x2a15 += 16;
            if (chk15r5.Checked) x2a15 += 32;
            if (chk15r6.Checked) x2a15 += 64;
            if (chk15r7.Checked) x2a15 += 128;
            set.x2a15 = x2a15;

            set.Save();
        }
        #endregion * grpPortA_CheckedChanged Events *

        #region * grpPortB_CheckedChanged Events *

        int x2b0 = 0, x2b1 = 0, x2b2 = 0, x2b3 = 0;
        int x2b4 = 0, x2b5 = 0, x2b6 = 0, x2b7 = 0;
        int x2b8 = 0, x2b9 = 0, x2b10 = 0, x2b11 = 0;
        int x2b12 = 0, x2b13 = 0, x2b14 = 0, x2b15 = 0;

        private void grpPortB_CheckedChanged(object sender, EventArgs e)
        {
            x2b0 = 0;
            if (chk0t0.Checked) x2b0 += 1;
            if (chk0t1.Checked) x2b0 += 2;
            if (chk0t2.Checked) x2b0 += 4;
            if (chk0t3.Checked) x2b0 += 8;
            if (chk0t4.Checked) x2b0 += 16;
            if (chk0t5.Checked) x2b0 += 32;
            if (chk0t6.Checked) x2b0 += 64;
            if (chk0t7.Checked) x2b0 += 128;
            set.x2b0 = x2b0;
            x2b1 = 0;
            if (chk1t0.Checked) x2b1 += 1;
            if (chk1t1.Checked) x2b1 += 2;
            if (chk1t2.Checked) x2b1 += 4;
            if (chk1t3.Checked) x2b1 += 8;
            if (chk1t4.Checked) x2b1 += 16;
            if (chk1t5.Checked) x2b1 += 32;
            if (chk1t6.Checked) x2b1 += 64;
            if (chk1t7.Checked) x2b1 += 128;
            set.x2b1 = x2b1;
            x2b2 = 0;
            if (chk2t0.Checked) x2b2 += 1;
            if (chk2t1.Checked) x2b2 += 2;
            if (chk2t2.Checked) x2b2 += 4;
            if (chk2t3.Checked) x2b2 += 8;
            if (chk2t4.Checked) x2b2 += 16;
            if (chk2t5.Checked) x2b2 += 32;
            if (chk2t6.Checked) x2b2 += 64;
            if (chk2t7.Checked) x2b2 += 128;
            set.x2b2 = x2b2;
            x2b3 = 0;
            if (chk3t0.Checked) x2b3 += 1;
            if (chk3t1.Checked) x2b3 += 2;
            if (chk3t2.Checked) x2b3 += 4;
            if (chk3t3.Checked) x2b3 += 8;
            if (chk3t4.Checked) x2b3 += 16;
            if (chk3t5.Checked) x2b3 += 32;
            if (chk3t6.Checked) x2b3 += 64;
            if (chk3t7.Checked) x2b3 += 128;
            set.x2b3 = x2b3;
            x2b4 = 0;
            if (chk4t0.Checked) x2b4 += 1;
            if (chk4t1.Checked) x2b4 += 2;
            if (chk4t2.Checked) x2b4 += 4;
            if (chk4t3.Checked) x2b4 += 8;
            if (chk4t4.Checked) x2b4 += 16;
            if (chk4t5.Checked) x2b4 += 32;
            if (chk4t6.Checked) x2b4 += 64;
            if (chk4t7.Checked) x2b4 += 128;
            set.x2b4 = x2b4;
            x2b5 = 0;
            if (chk5t0.Checked) x2b5 += 1;
            if (chk5t1.Checked) x2b5 += 2;
            if (chk5t2.Checked) x2b5 += 4;
            if (chk5t3.Checked) x2b5 += 8;
            if (chk5t4.Checked) x2b5 += 16;
            if (chk5t5.Checked) x2b5 += 32;
            if (chk5t6.Checked) x2b5 += 64;
            if (chk5t7.Checked) x2b5 += 128;
            set.x2b5 = x2b5;
            x2b6 = 0;
            if (chk6t0.Checked) x2b6 += 1;
            if (chk6t1.Checked) x2b6 += 2;
            if (chk6t2.Checked) x2b6 += 4;
            if (chk6t3.Checked) x2b6 += 8;
            if (chk6t4.Checked) x2b6 += 16;
            if (chk6t5.Checked) x2b6 += 32;
            if (chk6t6.Checked) x2b6 += 64;
            if (chk6t7.Checked) x2b6 += 128;
            set.x2b6 = x2b6;
            x2b7 = 0;
            if (chk7t0.Checked) x2b7 += 1;
            if (chk7t1.Checked) x2b7 += 2;
            if (chk7t2.Checked) x2b7 += 4;
            if (chk7t3.Checked) x2b7 += 8;
            if (chk7t4.Checked) x2b7 += 16;
            if (chk7t5.Checked) x2b7 += 32;
            if (chk7t6.Checked) x2b7 += 64;
            if (chk7t7.Checked) x2b7 += 128;
            set.x2b7 = x2b7;
            x2b8 = 0;
            if (chk8t0.Checked) x2b8 += 1;
            if (chk8t1.Checked) x2b8 += 2;
            if (chk8t2.Checked) x2b8 += 4;
            if (chk8t3.Checked) x2b8 += 8;
            if (chk8t4.Checked) x2b8 += 16;
            if (chk8t5.Checked) x2b8 += 32;
            if (chk8t6.Checked) x2b8 += 64;
            if (chk8t7.Checked) x2b8 += 128;
            set.x2b8 = x2b8;
            x2b9 = 0;
            if (chk9t0.Checked) x2b9 += 1;
            if (chk9t1.Checked) x2b9 += 2;
            if (chk9t2.Checked) x2b9 += 4;
            if (chk9t3.Checked) x2b9 += 8;
            if (chk9t4.Checked) x2b9 += 16;
            if (chk9t5.Checked) x2b9 += 32;
            if (chk9t6.Checked) x2b9 += 64;
            if (chk9t7.Checked) x2b9 += 128;
            set.x2b9 = x2b9;
            x2b10 = 0;
            if (chk10t0.Checked) x2b10 += 1;
            if (chk10t1.Checked) x2b10 += 2;
            if (chk10t2.Checked) x2b10 += 4;
            if (chk10t3.Checked) x2b10 += 8;
            if (chk10t4.Checked) x2b10 += 16;
            if (chk10t5.Checked) x2b10 += 32;
            if (chk10t6.Checked) x2b10 += 64;
            if (chk10t7.Checked) x2b10 += 128;
            set.x2b10 = x2b10;
            x2b11 = 0;
            if (chk11t0.Checked) x2b11 += 1;
            if (chk11t1.Checked) x2b11 += 2;
            if (chk11t2.Checked) x2b11 += 4;
            if (chk11t3.Checked) x2b11 += 8;
            if (chk11t4.Checked) x2b11 += 16;
            if (chk11t5.Checked) x2b11 += 32;
            if (chk11t6.Checked) x2b11 += 64;
            if (chk11t7.Checked) x2b11 += 128;
            set.x2b11 = x2b11;
            x2b12 = 0;
            if (chk12t0.Checked) x2b12 += 1;
            if (chk12t1.Checked) x2b12 += 2;
            if (chk12t2.Checked) x2b12 += 4;
            if (chk12t3.Checked) x2b12 += 8;
            if (chk12t4.Checked) x2b12 += 16;
            if (chk12t5.Checked) x2b12 += 32;
            if (chk12t6.Checked) x2b12 += 64;
            if (chk12t7.Checked) x2b12 += 128;
            set.x2b12 = x2b12;
            x2b13 = 0;
            if (chk13t0.Checked) x2b13 += 1;
            if (chk13t1.Checked) x2b13 += 2;
            if (chk13t2.Checked) x2b13 += 4;
            if (chk13t3.Checked) x2b13 += 8;
            if (chk13t4.Checked) x2b13 += 16;
            if (chk13t5.Checked) x2b13 += 32;
            if (chk13t6.Checked) x2b13 += 64;
            if (chk13t7.Checked) x2b13 += 128;
            set.x2b13 = x2b13;
            x2b14 = 0;
            if (chk14t0.Checked) x2b14 += 1;
            if (chk14t1.Checked) x2b14 += 2;
            if (chk14t2.Checked) x2b14 += 4;
            if (chk14t3.Checked) x2b14 += 8;
            if (chk14t4.Checked) x2b14 += 16;
            if (chk14t5.Checked) x2b14 += 32;
            if (chk14t6.Checked) x2b14 += 64;
            if (chk14t7.Checked) x2b14 += 128;
            set.x2b14 = x2b14;
            x2b15 = 0;
            if (chk15t0.Checked) x2b15 += 1;
            if (chk15t1.Checked) x2b15 += 2;
            if (chk15t2.Checked) x2b15 += 4;
            if (chk15t3.Checked) x2b15 += 8;
            if (chk15t4.Checked) x2b15 += 16;
            if (chk15t5.Checked) x2b15 += 32;
            if (chk15t6.Checked) x2b15 += 64;
            if (chk15t7.Checked) x2b15 += 128;
            set.x2b15 = x2b15;

            set.Save();
        }
        #endregion * grpPortB_CheckedChanged Events *

        // the Port A Enable checkbox has been changed
        private void chkPortA_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPortA.Checked)
            {
                foreach (Control c in grpPortA.Controls)
                {
                    if (c.GetType() == typeof(CheckBox))
                    { c.Enabled = true; }
                }
                if (!chkFWa.Checked) { txtPortA.Enabled = true; }
                chkFWa.Enabled = true;
                btnClrPortA.Enabled = true;
                set.chkPortA = true;
                X2SetUp();

            }
            else
            {
                foreach (Control c in grpPortA.Controls)
                {
                    if (c.GetType() == typeof(CheckBox))
                    { c.Enabled = false; }
                }
                chkFWa.Enabled = false;
                txtPortA.Enabled = false;
                btnClrPortA.Enabled = false;
                set.chkPortA = false;
            }
            set.Save();
        }
        // the Port B Enable checkbox has been changed
        private void chkPortB_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPortB.Checked)
            {
                foreach (Control c in grpPortB.Controls)
                {
                    if (c.GetType() == typeof(CheckBox))
                    { c.Enabled = true; }
                }
                if (!chkFWb.Checked) { txtPortB.Enabled = true; }
                chkFWb.Enabled = true;
                btnClrPortB.Enabled = true;
                set.chkPortB = true;
                X2SetUp();
                //if (rbFW.Checked)
                //{ rbFW.Checked = false; rbNone.Checked = true; }
            }
            else
            {
                foreach (Control c in grpPortB.Controls)
                {
                    if (c.GetType() == typeof(CheckBox))
                    { c.Enabled = false; }
                }
                chkFWb.Enabled = false;
                txtPortB.Enabled = false;
                btnClrPortB.Enabled = false;
                set.chkPortB = false;
            }
            set.Save();
        }
        // Port A has been changed
        private void txtPortA_TextChanged(object sender, EventArgs e)
        {
            if (txtPortA.Text == null || txtPortA.Text == "")
            {
                txtPortA.Text = "0";
                aPort = Convert.ToInt32(txtPortA.Text);
            }
            else if (txtPortA.Text == "FW") aPort = mAdr;
            else aPort = Convert.ToInt32(txtPortA.Text);
            set.aPortNum = txtPortA.Text; set.Save();
        }
        // Port B has been changed
        private void txtPortB_TextChanged(object sender, EventArgs e)
        {
            if (txtPortB.Text == null || txtPortB.Text == "")
            {
                txtPortB.Text = "0";
                bPort = Convert.ToInt32(txtPortB.Text);
            }
            else if (txtPortB.Text == "FW") bPort = mAdr;
            else bPort = Convert.ToInt32(txtPortB.Text);
            set.bPortNum = txtPortB.Text; set.Save();

        }
        // the Clear all receive bits button was pressed
        private void btnClrPortA_Click(object sender, EventArgs e)
        {
            CheckBox cb;
            foreach (Control c in grpPortA.Controls)
            {
                if (c.GetType() == typeof(CheckBox))
                {
                    cb = (CheckBox)c;
                    cb.Checked = false;
                }
            }
        }
        // the Clear all transmit bits button was pressed
        private void btnClrPortB_Click(object sender, EventArgs e)
        {
            CheckBox cb;
            foreach (Control c in grpPortB.Controls)
            {
                if (c.GetType() == typeof(CheckBox))
                {
                    cb = (CheckBox)c;
                    cb.Checked = false;
                }
            }
        }
        // the Port A invert checkbox has changed.
        private void chkInvertA_CheckedChanged(object sender, EventArgs e)
        {
            if (chkInvertA.Checked) { set.chkInvertA = true; }
            else { set.chkInvertA = false; }
            set.Save();
        }
        // the Port B invert checkbox has changed.
        private void chkInvertB_CheckedChanged(object sender, EventArgs e)
        {
            if (chkInvertB.Checked) { set.chkInvertB = true; }
            else { set.chkInvertB = false; }
            set.Save();
        }

        #endregion Events

        #endregion VHF+ Matrix

        # region Window Geometry

        private bool IsVisibleOnAnyScreen(Rectangle rect)
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(rect))
                {
                    return true;
                }
            }
            return false;
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // only save the WindowState if Normal or Maximized
            switch (this.WindowState)
            {
                case FormWindowState.Normal:
                case FormWindowState.Maximized:
                    Settings.Default.WindowState = this.WindowState;
                    break;
                default:
                    Settings.Default.WindowState = FormWindowState.Normal;
                    break;
            }
            Settings.Default.Save();
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            TrackWindowState();
        }
        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            TrackWindowState();
        }
        bool windowInitialized;
        // On a move or resize in Normal state, record the new values as they occur.
        // This solves the problem of closing the app when minimized or maximized.
        private void TrackWindowState()
        {
            // Don't record the window setup, otherwise we lose the persistent values!
            if (!windowInitialized) { return; }

            if (WindowState == FormWindowState.Normal)
            {
                Settings.Default.WindowPosition = this.DesktopBounds;
            }
        }
        # endregion Window Geometry
       
        #region WatchDog Timer

        // the WatchDog timer has fired.
        private void WatchDog_Elapsed(object sender, EventArgs e)
        {
            if (closing) { WatchDog.Close(); return; }
            WriteToPort("ZZTI1;", iSleep); // kill the PTT/MOX
            WatchDog.Enabled = false;
            MessageBox.Show(
                "        The transmit WatchDog timer has elapsed.\r\r" +
                "You can change the time period in the Timer window \r" +
                "on the Setup tab to a more suitable period if needed.\r\r" +
                "Closing this window will reset the WatchDog timer.",
                "Transmit Timer Elapsed");

            WriteToPort("ZZTI0;", iSleep);  // turn the PTT/MOX back on.
        }
        // the WatchDog Enable check box has changed.
        private void chkDog_CheckedChanged(object sender, EventArgs e)
        {
            if (chkDog.Checked)
            { set.DogEnab = true; txtTimer.Enabled = true; }
            else
            { set.DogEnab = false; txtTimer.Enabled = false; }
            set.Save();
        }
        // the WatchDog timer period has changed.
        private void txtTimer_TextChanged(object sender, EventArgs e)
        {
            try
            {
                int time = Convert.ToInt32(txtTimer.Text);
                time = time * 60000;
                WatchDog.Interval = time;
                set.DogTime = txtTimer.Text;
                set.Save();
            }
            catch
            { }
        }

        #endregion WatchDog

        #region WaveNode

        #region # WaveNode ENUMS & Vars #

        enum FT_STATUS
        {
            FT_OK = 0,
            FT_INVALID_HANDLE,
            FT_DEVICE_NOT_FOUND,
            FT_DEVICE_NOT_OPENED,
            FT_IO_ERROR,
            FT_INSUFFICIENT_RESOURCES,
            FT_INVALID_PARAMETER,
            FT_INVALID_BAUD_RATE,
            FT_DEVICE_NOT_OPENED_FOR_ERASE,
            FT_DEVICE_NOT_OPENED_FOR_WRITE,
            FT_FAILED_TO_WRITE_DEVICE,
            FT_EEPROM_READ_FAILED,
            FT_EEPROM_WRITE_FAILED,
            FT_EEPROM_ERASE_FAILED,
            FT_EEPROM_NOT_PRESENT,
            FT_EEPROM_NOT_PROGRAMMED,
            FT_INVALID_ARGS,
            FT_OTHER_ERROR
        };
        public const UInt32 FT_LIST_NUMBER_ONLY = 0x80000000;
        public const UInt32 FT_LIST_BY_INDEX = 0x40000000;
        public const UInt32 FT_LIST_ALL = 0x20000000;
        public const UInt32 FT_OPEN_BY_SERIAL_NUMBER = 1;
        public const UInt32 FT_OPEN_BY_DESCRIPTION = 2;
        public const UInt32 FT_EVENT_RXCHAR = 1;

        // Purge rx and tx buffers
        public const byte FT_PURGE_RX = 1;
        public const byte FT_PURGE_TX = 2;
        // FTD2xx.dll constructors
//#if WIN64
//        [DllImport("FTD2XX64.dll")]
//        static extern unsafe FT_STATUS FT_Close(FT_HANDLE ftHandle);
//        [DllImport("FTD2XX64.dll")]
//        static extern unsafe FT_STATUS FT_CyclePort(FT_HANDLE ftHandle);
//        [DllImport("FTD2XX64.dll")]// FT_ListDevices by number only
//        static extern unsafe FT_STATUS FT_ListDevices(void* pvArg1, void* pvArg2, UInt32 dwFlags);
//        [DllImport("FTD2XX64.dll")]// FT_ListDevcies by serial number or description by index only
//        static extern unsafe FT_STATUS FT_ListDevices(UInt32 pvArg1, void* pvArg2, UInt32 dwFlags);
//        [DllImport("FTD2XX64.dll")]
//        static extern unsafe FT_STATUS FT_Open(UInt32 uiPort, ref FT_HANDLE ftHandle);
//        [DllImport("FTD2XX64.dll")]
//        static extern unsafe FT_STATUS FT_OpenEx(void* pvArg1, UInt32 dwFlags, ref FT_HANDLE ftHandle);
//        [DllImport("FTD2XX64.dll")]
//        static extern unsafe FT_STATUS FT_Purge(FT_HANDLE ftHandle, UInt32 dwMask);
//        [DllImport("FTD2XX64.dll")]
//        static extern unsafe FT_STATUS FT_Read(FT_HANDLE ftHandle, void* lpBuffer,
//                               UInt32 dwBytesToRead, ref UInt32 lpdwBytesReturned);
//        [DllImport("FTD2XX64.dll")]
//        static extern unsafe FT_STATUS FT_ResetDevice(FT_HANDLE ftHandle);
//        [DllImport("FTD2XX64.dll")]
//        static extern unsafe FT_STATUS FT_ResetPort(FT_HANDLE ftHandle);
//        [DllImport("FTD2XX64.dll")]
//        static extern unsafe FT_STATUS FT_Write(FT_HANDLE ftHandle, void* lpBuffer,
//                                UInt32 dwBytesToRead, ref UInt32 lpdwBytesWritten);

//#else
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_Close(FT_HANDLE ftHandle);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_CyclePort(FT_HANDLE ftHandle);
        [DllImport("FTD2XX.dll")]// FT_ListDevices by number only
        static extern unsafe FT_STATUS FT_ListDevices(void* pvArg1, void* pvArg2, UInt32 dwFlags);
        [DllImport("FTD2XX.dll")]// FT_ListDevcies by serial number or description by index only
        static extern unsafe FT_STATUS FT_ListDevices(UInt32 pvArg1, void* pvArg2, UInt32 dwFlags);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_Open(UInt32 uiPort, ref FT_HANDLE ftHandle);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_OpenEx(void* pvArg1, UInt32 dwFlags, ref FT_HANDLE ftHandle);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_Purge(FT_HANDLE ftHandle, UInt32 dwMask);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_Read(FT_HANDLE ftHandle, void* lpBuffer,
                               UInt32 dwBytesToRead, ref UInt32 lpdwBytesReturned);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_ResetDevice(FT_HANDLE ftHandle);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_ResetPort(FT_HANDLE ftHandle);
        [DllImport("FTD2XX.dll")]
        static extern unsafe FT_STATUS FT_Write(FT_HANDLE ftHandle, void* lpBuffer,
                                UInt32 dwBytesToRead, ref UInt32 lpdwBytesWritten);
//#endif
        //int s = 0;
        int sensor;
        int sType;
        int s1Type;
        int s2Type;
        int s3Type;
        int s4Type;
        protected UInt32 dwListDescFlags;
        protected UInt32 m_hPort;

        #endregion WaveNode ENUMS & Vars

        #region # Delegates #

        // Write Peak reading to txt box
        delegate void SetPeakCallback(string text);
        private void SetPeak(string text)
        {
            if (closing) return;
            if (this.txtFwd.InvokeRequired)
            {
                SetPeakCallback d = new SetPeakCallback(SetPeak);
                this.Invoke(d, new object[] { text });
            }
            else
                txtFwd.Text = text;
        }
        // Write Avg reading to txt box
        delegate void SetAvgCallback(string text);
        private void SetAvg(string text)
        {
            if (closing) return;
            if (this.txtAvg.InvokeRequired)
            {

                SetAvgCallback d = new SetAvgCallback(SetAvg);
                this.Invoke(d, new object[] { text });
            }
            else
                txtAvg.Text = text.TrimStart('0');
        }
        // Write SWR reading to txt box
        //delegate void SetSWRCallback(string text);
        //private void SetSWR(string text)
        //{
        //    if (closing) return;
        //    if (this.txtSWR.InvokeRequired)
        //    {
        //        SetSWRCallback d = new SetSWRCallback(SetSWR);
        //        this.Invoke(d, new object[] { text });
        //    }
        //    else
        //    {
        //        txtSWR.Text = text;
        //        if (chkSWR.Checked && Convert.ToDecimal(text) > numSWR.Value)
        //        {
        //            WriteToPort("ZZTI1;", iSleep);
        //            lblHighSWR.Visible = true;
        //            // Display a message that the SWR alarm value was exceeded.
        //            MessageBox.Show(this,
        //                "The SWR alarm value setting has been exceeded.\r\r" +
        //                "PowerSDR's transmit ability is now disabled.\r\r" +
        //                "Press OK to dismiss this message and to Reset the SWR Alarm",
        //                "High SWR", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //            lblHighSWR.Visible = false;
        //            WriteToPort("ZZTI0;", iSleep);
        //        }
        //    }
        //}
        #endregion Delegates

        #region # WaveNode Events #
        // The WN sensor button was pressed, show the sensor matrix
        private void btnWnSensor_Click(object sender, EventArgs e)
        { wn.Show(); }

        // The WN2 Enabled RadioButton has changed
        private void chkWNEnab_CheckedChanged(object sender, EventArgs e)
        {
            if (chkWNEnab.Checked)
            {
                set.WnEnab = true; chkLPenab.Checked = false;
                chkPM.Checked = false; lblFwd.Visible = true;
                txtFwd.Visible = true; txtAvg.Enabled = true;
                mini.lblFwd.Visible = true; mini.txtFwd.Visible = true;
                mini.txtAvg.Enabled = true;
                txtSWR.Enabled = true; lblAvg.Text = "Avg"; txtFwd.Enabled = true;
                mini.txtSWR.Enabled = true; mini.lblAvg.Text = "Avg";
                mini.txtFwd.Enabled = true;

                if (FindDevice())           // Find the WN2 and open it
                {
                    WN2Timer.Enabled = true;   // Start the read timer;               
                }
                else
                {
                    WN2Timer.Enabled = false; set.WnEnab = false;
                    txtFwd.Visible = false; lblFwd.Visible = false;
                    mini.txtFwd.Visible = false; mini.lblFwd.Visible = false;
                    //txtAvg.Enabled = false; txtSWR.Enabled = false;
                    //mini.txtAvg.Text = ""; mini.txtSWR.Text = "";
                }
            }
            else
            {
                WN2Timer.Enabled = false; set.WnEnab = false;
                FT_Close(m_hPort); m_hPort = 0;
                txtFwd.Visible = false; lblFwd.Visible = false;
                mini.txtFwd.Visible = false; mini.lblFwd.Visible = false;
                // txtSWR.Enabled = false;
                //mini.txtAvg.Enabled = false; mini.txtSWR.Enabled = false;
                txtAvg.Text = ""; txtSWR.Text = "";
                mini.txtAvg.Text = ""; mini.txtSWR.Text = "";
            }
            set.Save();
        }
        // the read timer has fired
        private void timer1_Tick(object sender, EventArgs e)
        { ReadBuffer(); }

        // The coupler selection has changed
        private void grpWN2_CheckedChanged(object sender, EventArgs e)
        {
            if (rbWN1.Checked)
            { sensor = 1; sType = s1Type; set.WnCoupler = 1; }
            else if (rbWN2.Checked)
            { sensor = 2; sType = s2Type; set.WnCoupler = 2; }
            else if (rbWN3.Checked)
            { sensor = 3; sType = s3Type; set.WnCoupler = 3; }
            else if (rbWN4.Checked)
            { sensor = 4; sType = s4Type; set.WnCoupler = 4; }
            set.Save();
        }
        // the #1 sensor type has changed
        private void grpC1_CheckedChanged(object sender, EventArgs e)
        {
            if (rbC1Q.Checked) { s1Type = 1; set.s1Type = 1; }
            else if (rbC1H.Checked) { s1Type = 2; set.s1Type = 2; }
            else if (rbC1K.Checked) { s1Type = 3; set.s1Type = 3; }
            SetSensorType(); set.Save();
        }
        // the #2 sensor type has changed
        private void grpC2_CheckedChanged(object sender, EventArgs e)
        {
            if (rbC2Q.Checked) { s2Type = 1; set.s2Type = 1; }
            else if (rbC2H.Checked) { s2Type = 2; set.s2Type = 2; }
            else if (rbC2K.Checked) { s2Type = 3; set.s2Type = 3; }
            SetSensorType(); set.Save();
        }
        // the #3 sensor type has changed
        private void grpC3_CheckedChanged(object sender, EventArgs e)
        {
            if (rbC3Q.Checked) { s3Type = 1; set.s3Type = 1; }
            else if (rbC3H.Checked) { s3Type = 2; set.s3Type = 2; }
            else if (rbC3K.Checked) { s3Type = 3; set.s3Type = 3; }
            set.Save();
        }
        // the #4 sensor type has changed
        private void grpC4_CheckedChanged(object sender, EventArgs e)
        {
            if (rbC4Q.Checked) { s4Type = 1; set.s4Type = 1; }
            else if (rbC4H.Checked) { s4Type = 2; set.s4Type = 2; }
            else if (rbC4K.Checked) { s4Type = 3; set.s4Type = 3; }
            SetSensorType(); set.Save();
        }
        // the WN reset button has been pressed
        private void btnWnReset_Click(object sender, EventArgs e)
        {
            FT_STATUS ftStatus = FT_STATUS.FT_OTHER_ERROR;

            ftStatus = FT_ResetDevice(m_hPort);
            if (ftStatus == FT_STATUS.FT_OK)
                ftStatus = FT_CyclePort(m_hPort);
            else goto Error;
            if (ftStatus == FT_STATUS.FT_OK)
            {
                ftStatus = FT_Purge(m_hPort, FT_PURGE_RX | FT_PURGE_TX);
                return;
            }
        Error:
            Notification alert = new Notification();
            Notification.notiIntvl = 7000;
            Notification.notiMsg =
                "The WN2 could not be Reset.\r\r" +
                "The only recourse is to toggle the power.\r";
            alert.Show();
            chkWNEnab.Checked = false;
        }

        #endregion WaveNode Events

        #region # WaveNode Methods #

        // Find all the FTDI USB devices
        private unsafe bool FindDevice()
        {
            FT_STATUS ftStatus = FT_STATUS.FT_OTHER_ERROR;
            UInt32 numDevs;
            int i;
            byte[] sDevName = new byte[64];
            void* p1;

            dwListDescFlags = FT_LIST_BY_INDEX | FT_OPEN_BY_SERIAL_NUMBER;
            p1 = (void*)&numDevs;
            ftStatus = FT_ListDevices(p1, null, FT_LIST_NUMBER_ONLY);
            // Find all the FTDI devices
            for (i = 0; i < numDevs; i++)
            {
                fixed (byte* pBuf = sDevName)
                {
                    // Enumerate the found devices. Note: if a device is in use it will
                    // return an FT_INVALID_HANDLE error, so the error must be ignored
                    ftStatus = FT_ListDevices((UInt32)i, pBuf, dwListDescFlags);
                    if (ftStatus == FT_STATUS.FT_OK ||
                        ftStatus == FT_STATUS.FT_INVALID_HANDLE)
                    {
                        System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                        string str = enc.GetString(sDevName, 0, sDevName.Length);
                        if (str.Substring(0, 2) == "WN")
                        {   // Go open the device
                            if (OpenDevice(str)) return true;
                            else { return false; }
                        }
                        else
                        {
                            if (i == numDevs - 1)
                            {   // if the WN2 is not found, throw a message.
                                Notification alert = new Notification();
                                Notification.notiIntvl = 7000;
                                Notification.notiMsg =
                                    "The WN2 watt meter could not be found.\r\r" +
                                    "Please make sure the USB line is connected " +
                                    "properly and the unit is powerd up.\r";
                                alert.Show();
                                chkWNEnab.Checked = false;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show(new Form() { TopMost = true }, 
                            "Error listing devices: " +
                            Convert.ToString(ftStatus), "Error");
                        return false;
                    }
                }
            }
            return false;
        }
        // Open the FTDI device found in FindDevice()
        private unsafe bool OpenDevice(string dev)
        {
            UInt32 dwOpenFlag;
            FT_STATUS ftStatus = FT_STATUS.FT_OTHER_ERROR;

            if (m_hPort == 0)
            {
                dwOpenFlag = dwListDescFlags & ~FT_LIST_BY_INDEX;
                dwOpenFlag = dwListDescFlags & ~FT_LIST_ALL;

                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                byte[] sDevName = enc.GetBytes(dev);
                fixed (byte* pBuf = sDevName)
                {
                    ftStatus = FT_OpenEx(pBuf, dwOpenFlag, ref m_hPort);
                    if (ftStatus == FT_STATUS.FT_OK)
                    {
                        //                        ftStatus = FT_ResetDevice(m_hPort);
                        ftStatus = FT_Purge(m_hPort, FT_PURGE_RX | FT_PURGE_TX);
                        //                        s = 0;
                    }
                    else
                    {
                        MessageBox.Show(new Form() { TopMost = true }, 
                            "Error opening device: " +
                            Convert.ToString(ftStatus), "Error");
                        return false;
                    }
                }
            }
            return true;
        }
        // Read the WN2 into the USB buffer
        List<float> samplePeak = new List<float>(12);
        private unsafe void ReadBuffer()
        {
            byte[] usbReadBuf = new byte[64];
            int[] adcBuf = new int[16];
            float[] emcAvgBuf = new float[8];
            float[] emcPeakBuf = new float[8];
            UInt32 dwRet = 0;
            FT_STATUS ftStatus = FT_STATUS.FT_OTHER_ERROR;

            if (m_hPort == 0) return;

            fixed (byte* pBuf = usbReadBuf)
            { ftStatus = FT_Read(m_hPort, pBuf, 64, ref dwRet); }

            if (dwRet == 64)
            {
                // Check usb buffer for a good load
                if (usbReadBuf[0] == 1 && usbReadBuf[61] == 0xff && usbReadBuf[62] == 0xf0)
                {
                    // Load USB Avg values to buffer
                    for (int i = 1; i < 9; i++)
                    {
                        int usbMsb = usbReadBuf[2 * i] << 8;
                        int MsbLsb = usbReadBuf[((2 * i) - 1)] | usbMsb;
                        adcBuf[i - 1] = MsbLsb;
                        emcAvgBuf[i - 1] = (float)MsbLsb; //(3.6 / 4048) * MsbLsb;
                        if (sType == 1)      // sensor type = qrp
                        {
                            emcAvgBuf[i - 1] = emcAvgBuf[i - 1] * 10;
                            emcAvgBuf[i - 1] = emcAvgBuf[i - 1] / 63;
                        }
                        else if (sType == 2) // sensor type = HF/UHF
                        {
                            emcAvgBuf[i - 1] = powerScaleCorrect(emcAvgBuf[i - 1]);
                        }
                        else if (sType == 3) // sensor type = 8K
                        {
                            emcAvgBuf[i - 1] = powerScaleCorrect_8K(emcAvgBuf[i - 1] * 2);
                        }
                    }
                    // Load USB Peak values to buffer
                    for (int i = 9; i < 17; i++)
                    {
                        int usbMsb = usbReadBuf[2 * i] << 8;
                        int MsbLsb = usbReadBuf[((2 * i) - 1)] | usbMsb;
                        adcBuf[i - 1] = MsbLsb;
                        emcPeakBuf[i - 9] = (float)MsbLsb;// (3.6 / 4048) * MsbLsb; // Peak fwd volts
                        if (sType == 1)      // sensor type = qrp
                        {
                            emcPeakBuf[i - 9] = emcPeakBuf[i - 9] * 10;
                            emcPeakBuf[i - 9] = emcPeakBuf[i - 9] / 63;
                        }
                        else if (sType == 2) // sensor type = HF/UHF
                        {
                            emcPeakBuf[i - 9] = powerScaleCorrect(emcPeakBuf[i - 9]);
                        }
                        else if (sType == 3) // sensor type = 8K
                        {
                            emcPeakBuf[i - 9] = powerScaleCorrect_8K(emcPeakBuf[i - 9] * 2);
                        }
                    }
                    // Go post Power/SWR data to the display
                    if (sensor == 1)
                    {
                        float aver2 = emcAvgBuf[0] * emcAvgBuf[0];
                        float peak2 = emcPeakBuf[0] * emcPeakBuf[0];
                        float emcFwd = emcPeakBuf[0];
                        float emcRef = emcPeakBuf[1];
                        PostDisplay(aver2, peak2, emcFwd, emcRef);
                    }
                    else if (sensor == 2)
                    {
                        float aver2 = emcAvgBuf[2] * emcAvgBuf[2];
                        float peak2 = emcPeakBuf[2] * emcPeakBuf[2];
                        float emcFwd = emcPeakBuf[2];
                        float emcRef = emcPeakBuf[3];
                        PostDisplay(aver2, peak2, emcFwd, emcRef);
                    }
                    else if (sensor == 3)
                    {
                        float aver2 = emcAvgBuf[4] * emcAvgBuf[4];
                        float peak2 = emcPeakBuf[4] * emcPeakBuf[4];
                        float emcFwd = emcPeakBuf[4];
                        float emcRef = emcPeakBuf[5];
                        PostDisplay(aver2, peak2, emcFwd, emcRef);
                    }
                    else if (sensor == 4)
                    {
                        float aver2 = emcAvgBuf[6] * emcAvgBuf[6];
                        float peak2 = emcPeakBuf[6] * emcPeakBuf[6];
                        float emcFwd = emcPeakBuf[6];
                        float emcRef = emcPeakBuf[7];
                        PostDisplay(aver2, peak2, emcFwd, emcRef);
                    }
                } // good data
                //else
                //{
                //    // if bad data do nothing, wait for next packet
                //}
            } //dwRet==64
        } // readThred

        // Do the power and swr calculations and print them to the display
        private void PostDisplay(float aver2, float peak2, float emcFwd, float emcRef)
        {
            float avg, power, swr = 0;
            if (aver2 < 100) avg = aver2 / 670; else avg = aver2 / 6700;
            if (peak2 < 100) power = peak2 / 670; else power = peak2 / 6700;
            if (power < 1.00) power = 0;

            //samplePeak.Insert(s, power); // save this sample
            //s += 1; if (s > 12) s = 0;

            if (avg < 1) avg = 0;
            emcRef = swrScaleCorrect(emcRef);
            swr = (emcFwd + emcRef) / (emcFwd - emcRef);
            if (power == 0) swr = 0;
            //else 
            //{
            //power = FindMax();
            //}
            SetPeak(string.Format("{0:f1}", power));
            mini.txtFwd.Text = (string.Format("{0:f1}", power));
            SetAvg(string.Format("{0:f1}", avg));
            mini.txtAvg.Text = (string.Format("{0:f1}", avg));
            SetSwr(string.Format("{0:f1}", swr));
            mini.txtSWR.Text = (string.Format("{0:f1}", swr));
        }
        // Find max sample
        public float FindMax()
        {
            float max = float.MinValue;
            for (int i = 0; i < 12; i++)
            {
                float val = samplePeak[i];
                if (val > max)
                    max = val;
            }
            return max;
        }
        // Function to compensate for low power levels (swr only)
        float swrScaleCorrect(float refpowerin)
        {
            if (refpowerin <= 150)  //trips at about 2.4 watts
            {
                refpowerin = refpowerin * 13;  //multiply by 1.3
                refpowerin = refpowerin / 10;
            }
            else if (refpowerin <= 300)   //trips at about 7 watts, 
            {
                refpowerin = refpowerin * 22;  //multiply by 1.15
                refpowerin = refpowerin / 19;
            }
            return refpowerin;
        }

        float powerScaleCorrect(float powerin)
        {
            if (powerin >= 1000)   //trips at about 200 watts
            {
                powerin = powerin * 40;  //multiply by 40/41
                powerin = powerin / 41;
            }
            if (powerin >= 2000)   //trips at about 200 watts
            {
                powerin = powerin * 60;  //multiply by 60/61
                powerin = powerin / 61;
            }
            if (powerin <= 250)   //trips at about 7 watts
            {
                powerin = powerin * 11;  //multiply by 1.1
                powerin = powerin / 10;
            }
            if (powerin <= 100)  //trips at about 2.4 watts
            {
                powerin = powerin * 12;  //multiply by 1.2
                powerin = powerin / 10;
            }
            return powerin;
        }
        float powerScaleCorrect_8K(float powerin)
        {
            if (powerin >= 3000)   //trips at about 1100 watts
            {
                powerin = powerin * 80;  //multiply by 40/41
                powerin = powerin / 81;
            }
            if (powerin >= 2200)   //trips at about 800 watts
            {
                powerin = powerin * 65;  //multiply by 70/71
                powerin = powerin / 66;
            }
            if (powerin >= 1700)   //trips at about 400 watts
            {
                powerin = powerin * 40;  //multiply by 30/31
                powerin = powerin / 41;
            }
            if (powerin >= 1200)   //trips at about 200 watts
            {
                powerin = powerin * 35;  //multiply by 30/31
                powerin = powerin / 36;
            }
            if (powerin <= 250)   //trips at about 7 watts
            {
                powerin = powerin * 11;  //multiply by 1.1
                powerin = powerin / 10;
            }
            if (powerin <= 100)  //trips at about 2.4 watts
            {
                powerin = powerin * 11;  //multiply by 1.1
                powerin = powerin / 10;
            }
            return powerin;
        }
        // Called after one of the sensor types has changed.
        private void SetSensorType()
        {
            switch (sensor)
            {
                case 1: sType = s1Type; break;
                case 2: sType = s2Type; break;
                case 3: sType = s3Type; break;
                case 4: sType = s4Type; break;
            }
        }

        #endregion WaveNode Methods

        #region # WaveNode Setup #

        private void WN2SetUp()
        {
            // Setup the WN2 controls
            chkWNEnab.Checked = set.WnEnab;
            if (!chkWNEnab.Checked && !chkLPenab.Checked)
                chkWNEnab_CheckedChanged(null, null);
            sensor = set.WnCoupler;
            s1Type = set.s1Type; s2Type = set.s2Type;
            s3Type = set.s3Type; s4Type = set.s4Type;
            // Restore the current sensor being used
            switch (sensor)
            {
                case 1: rbWN1.Checked = true; sType = s1Type; break;
                case 2: rbWN2.Checked = true; sType = s2Type; break;
                case 3: rbWN3.Checked = true; sType = s3Type; break;
                case 4: rbWN4.Checked = true; sType = s4Type; break;
            }
            // Restore the sensor type radio buttons
            switch (s1Type)
            {
                case 1: rbC1Q.Checked = true; break;
                case 2: rbC1H.Checked = true; break;
                case 3: rbC1K.Checked = true; break;
            }
            switch (s2Type)
            {
                case 1: rbC2Q.Checked = true; break;
                case 2: rbC2H.Checked = true; break;
                case 3: rbC2K.Checked = true; break;
            }
            switch (s3Type)
            {
                case 1: rbC3Q.Checked = true; break;
                case 2: rbC3H.Checked = true; break;
                case 3: rbC3K.Checked = true; break;
            }
            switch (s4Type)
            {
                case 1: rbC4Q.Checked = true; break;
                case 2: rbC4H.Checked = true; break;
                case 3: rbC4K.Checked = true; break;
            }
        }

        #endregion WaveNode Setup

        #endregion WaveNode       

    } // end Setup class

    public class Caller
    {
        public String zzcat { get; set; }
        public string kwcat { get; set; }
        public SerialPort port { get; set; }

        public Caller()
        {
            kwcat = null;
        }
    }


    #region Helper Classes

    public class HourGlass : IDisposable
    {
        public HourGlass()
        {
            Enabled = true;
        }
        public void Dispose()
        {
            Enabled = false;
        }
        public static bool Enabled
        {
            get { return Application.UseWaitCursor; }
            set
            {
                if (value == Application.UseWaitCursor) return;
                Application.UseWaitCursor = value;
                Form f = Form.ActiveForm;
                if (f != null && f.Handle != null)   // Send WM_SETCURSOR
                    SendMessage(f.Handle, 0x20, f.Handle, (IntPtr)1);
            }
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
    }
    //using (new HourGlass())
    //{
    //    Thread.Sleep(5000); // allow time for radio to initialize
    //}

    public class ProgressDialog
    {
        private IntPtr _parentHandle;

        private Win32IProgressDialog pd = null;

        public ProgressDialog(IntPtr parentHandle)
        {
            this._parentHandle = parentHandle;
        }

        public void ShowDialog(params PROGDLG[] flags)
        {
            if (pd == null)
            {
                pd = (Win32IProgressDialog)new Win32ProgressDialog();

                pd.SetTitle(this._Title);
                pd.SetCancelMsg(this._CancelMessage, null);
                pd.SetLine(1, this._Line1, false, IntPtr.Zero);
                pd.SetLine(2, this._Line2, false, IntPtr.Zero);
                pd.SetLine(3, this._Line3, false, IntPtr.Zero);

                PROGDLG dialogFlags = PROGDLG.Normal;
                if (flags.Length != 0)
                {
                    dialogFlags = flags[0];
                    for (var i = 1; i < flags.Length; i++)
                    {
                        dialogFlags = dialogFlags | flags[i];
                    }
                }

                pd.StartProgressDialog(this._parentHandle, null, dialogFlags, IntPtr.Zero);
            }
        }

        public void CloseDialog()
        {
            if (pd != null)
            {
                pd.StopProgressDialog();
                //Marshal.ReleaseComObject(pd);
                pd = null;
            }
        }

        private string _Title = string.Empty;
        public string Title
        {
            get
            {
                return this._Title;
            }
            set
            {
                this._Title = value;
                if (pd != null)
                {
                    pd.SetTitle(this._Title);
                }
            }
        }

        private string _CancelMessage = string.Empty;
        public string CancelMessage
        {
            get
            {
                return this._CancelMessage;
            }
            set
            {
                this._CancelMessage = value;
                if (pd != null)
                {
                    pd.SetCancelMsg(this._CancelMessage, null);
                }
            }
        }

        private string _Line1 = string.Empty;
        public string Line1
        {
            get
            {
                return this._Line1;
            }
            set
            {
                this._Line1 = value;
                if (pd != null)
                {
                    pd.SetLine(1, this._Line1, false, IntPtr.Zero);
                }
            }
        }

        private string _Line2 = string.Empty;
        public string Line2
        {
            get
            {
                return this._Line2;
            }
            set
            {
                this._Line2 = value;
                if (pd != null)
                {
                    pd.SetLine(2, this._Line2, false, IntPtr.Zero);
                }
            }
        }

        private string _Line3 = string.Empty;
        public string Line3
        {
            get
            {
                return this._Line3;
            }
            set
            {
                this._Line3 = value;
                if (pd != null)
                {
                    pd.SetLine(3, this._Line3, false, IntPtr.Zero);
                }
            }
        }

        private uint _value = 0;
        public uint Value
        {
            get
            {
                return this._value;
            }
            set
            {
                this._value = value;
                if (pd != null)
                {
                    pd.SetProgress(this._value, this._maximum);
                }
            }
        }

        private uint _maximum = 100;
        public uint Maximum
        {
            get
            {
                return this._maximum;
            }
            set
            {
                this._maximum = value;
                if (pd != null)
                {
                    pd.SetProgress(this._value, this._maximum);
                }
            }
        }
        public bool HasUserCancelled
        {
            get
            {
                if (pd != null)
                {
                    return pd.HasUserCancelled();
                }
                else
                    return false;
            }
        }

        #region "Win32 Stuff"
        // The below was copied from: http://pinvoke.net/default.aspx/Interfaces/IProgressDialog.html

        public static class shlwapi
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
            static extern bool PathCompactPath(IntPtr hDC, [In, Out] StringBuilder pszPath, int dx);
        }

        [ComImport]
        [Guid("EBBC7C04-315E-11d2-B62F-006097DF5BD4")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface Win32IProgressDialog
        {
            /// <summary>
            /// Starts the progress dialog box.
            /// </summary>
            /// <param name="hwndParent">A handle to the dialog box's parent window.</param>
            /// <param name="punkEnableModless">Reserved. Set to null.</param>
            /// <param name="dwFlags">Flags that control the operation of the progress dialog box. </param>
            /// <param name="pvResevered">Reserved. Set to IntPtr.Zero</param>
            void StartProgressDialog(
                IntPtr hwndParent, //HWND
                [MarshalAs(UnmanagedType.IUnknown)]    object punkEnableModless, //IUnknown
                PROGDLG dwFlags,  //DWORD
                IntPtr pvResevered //LPCVOID
                );

            /// <summary>
            /// Stops the progress dialog box and removes it from the screen.
            /// </summary>
            void StopProgressDialog();

            /// <summary>
            /// Sets the title of the progress dialog box.
            /// </summary>
            /// <param name="pwzTitle">A pointer to a null-terminated Unicode string that contains the dialog box title.</param>
            void SetTitle(
                [MarshalAs(UnmanagedType.LPWStr)] string pwzTitle //LPCWSTR
                );

            /// <summary>
            /// Specifies an Audio-Video Interleaved (AVI) clip that runs in the dialog box. Note: Note  This method is not supported in Windows Vista or later versions.
            /// </summary>
            /// <param name="hInstAnimation">An instance handle to the module from which the AVI resource should be loaded.</param>
            /// <param name="idAnimation">An AVI resource identifier. To create this value, use the MAKEINTRESOURCE macro. The control loads the AVI resource from the module specified by hInstAnimation.</param>
            void SetAnimation(
                IntPtr hInstAnimation, //HINSTANCE
                ushort idAnimation //UINT
                );

            /// <summary>
            /// Checks whether the user has canceled the operation.
            /// </summary>
            /// <returns>TRUE if the user has cancelled the operation; otherwise, FALSE.</returns>
            /// <remarks>
            /// The system does not send a message to the application when the user clicks the Cancel button.
            /// You must periodically use this function to poll the progress dialog box object to determine
            /// whether the operation has been canceled.
            /// </remarks>
            [PreserveSig]
            [return: MarshalAs(UnmanagedType.Bool)]
            bool HasUserCancelled();

            /// <summary>
            /// Updates the progress dialog box with the current state of the operation.
            /// </summary>
            /// <param name="dwCompleted">An application-defined value that indicates what proportion of the operation has been completed at the time the method was called.</param>
            /// <param name="dwTotal">An application-defined value that specifies what value dwCompleted will have when the operation is complete.</param>
            void SetProgress(
                uint dwCompleted, //DWORD
                uint dwTotal //DWORD
                );

            /// <summary>
            /// Updates the progress dialog box with the current state of the operation.
            /// </summary>
            /// <param name="ullCompleted">An application-defined value that indicates what proportion of the operation has been completed at the time the method was called.</param>
            /// <param name="ullTotal">An application-defined value that specifies what value ullCompleted will have when the operation is complete.</param>
            void SetProgress64(
                ulong ullCompleted, //ULONGLONG
                ulong ullTotal //ULONGLONG
                );

            /// <summary>
            /// Displays a message in the progress dialog.
            /// </summary>
            /// <param name="dwLineNum">The line number on which the text is to be displayed. Currently there are three lines�1, 2, and 3. If the PROGDLG_AUTOTIME flag was included in the dwFlags parameter when IProgressDialog::StartProgressDialog was called, only lines 1 and 2 can be used. The estimated time will be displayed on line 3.</param>
            /// <param name="pwzString">A null-terminated Unicode string that contains the text.</param>
            /// <param name="fCompactPath">TRUE to have path strings compacted if they are too large to fit on a line. The paths are compacted with PathCompactPath.</param>
            /// <param name="pvResevered"> Reserved. Set to IntPtr.Zero.</param>
            /// <remarks>This function is typically used to display a message such as "Item XXX is now being processed." typically, messages are displayed on lines 1 and 2, with line 3 reserved for the estimated time.</remarks>
            void SetLine(
                uint dwLineNum, //DWORD
                [MarshalAs(UnmanagedType.LPWStr)] string pwzString, //LPCWSTR
                [MarshalAs(UnmanagedType.VariantBool)] bool fCompactPath, //BOOL
                IntPtr pvResevered //LPCVOID
                );

            /// <summary>
            /// Sets a message to be displayed if the user cancels the operation.
            /// </summary>
            /// <param name="pwzCancelMsg">A pointer to a null-terminated Unicode string that contains the message to be displayed.</param>
            /// <param name="pvResevered">Reserved. Set to NULL.</param>
            /// <remarks>Even though the user clicks Cancel, the application cannot immediately call
            /// IProgressDialog::StopProgressDialog to close the dialog box. The application must wait until the
            /// next time it calls IProgressDialog::HasUserCancelled to discover that the user has canceled the
            /// operation. Since this delay might be significant, the progress dialog box provides the user with
            /// immediate feedback by clearing text lines 1 and 2 and displaying the cancel message on line 3.
            /// The message is intended to let the user know that the delay is normal and that the progress dialog
            /// box will be closed shortly.
            /// It is typically is set to something like "Please wait while ...". </remarks>
            void SetCancelMsg(
                [MarshalAs(UnmanagedType.LPWStr)] string pwzCancelMsg, //LPCWSTR
                object pvResevered //LPCVOID
                );

            /// <summary>
            /// Resets the progress dialog box timer to zero.
            /// </summary>
            /// <param name="dwTimerAction">Flags that indicate the action to be taken by the timer.</param>
            /// <param name="pvResevered">Reserved. Set to NULL.</param>
            /// <remarks>
            /// The timer is used to estimate the remaining time. It is started when your application
            /// calls IProgressDialog::StartProgressDialog. Unless your application will start immediately,
            /// it should call Timer just before starting the operation.
            /// This practice ensures that the time estimates will be as accurate as possible. This method
            /// should not be called after the first call to IProgressDialog::SetProgress.</remarks>
            void Timer(
                PDTIMER dwTimerAction, //DWORD
                object pvResevered //LPCVOID
                );

        }

        [ComImport]
        [Guid("F8383852-FCD3-11d1-A6B9-006097DF5BD4")]
        public class Win32ProgressDialog
        {
        }

        /// <summary>
        /// Flags that indicate the action to be taken by the ProgressDialog.SetTime() method.
        /// </summary>
        public enum PDTIMER : uint //DWORD
        {
            /// <summary>Resets the timer to zero. Progress will be calculated from the time this method is called.</summary>
            Reset = (0x01),
            /// <summary>Progress has been suspended.</summary>
            Pause = (0x02),
            /// <summary>Progress has been resumed.</summary>
            Resume = (0x03)
        }

        [Flags]
        public enum PROGDLG : uint //DWORD
        {
            /// <summary>Normal progress dialog box behavior.</summary>
            Normal = 0x00000000,
            /// <summary>The progress dialog box will be modal to the window specified by hwndParent. By default, a progress dialog box is modeless.</summary>
            Modal = 0x00000001,
            /// <summary>Automatically estimate the remaining time and display the estimate on line 3. </summary>
            /// <remarks>If this flag is set, IProgressDialog::SetLine can be used only to display text on lines 1 and 2.</remarks>
            AutoTime = 0x00000002,
            /// <summary>Do not show the "time remaining" text.</summary>
            NoTime = 0x00000004,
            /// <summary>Do not display a minimize button on the dialog box's caption bar.</summary>
            NoMinimize = 0x00000008,
            /// <summary>Do not display a progress bar.</summary>
            /// <remarks>Typically, an application can quantitatively determine how much of the operation remains and periodically pass that value to IProgressDialog::SetProgress. The progress dialog box uses this information to update its progress bar. This flag is typically set when the calling application must wait for an operation to finish, but does not have any quantitative information it can use to update the dialog box.</remarks>
            NoProgressBar = 0x00000010
        }
        #endregion
    }

    #endregion Helper Classes
}