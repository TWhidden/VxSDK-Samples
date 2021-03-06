﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CPPCli;
using SDKSampleApp.Enums;
using SDKSampleApp.Properties;

namespace SDKSampleApp.Source
{
    /// <summary>
    /// The MainForm class.
    /// </summary>
    /// <remarks>This class contains the main UI form.</remarks>
    public partial class MainForm : Form
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainForm" /> class.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            dtpSeekTime.Value = DateTime.Now.AddMinutes(-10);
            scOuter.Panel2Collapsed = true;
            Instance = this;
            PtzForm = new PTZControlForm();
            Control = new ControlManager();
            SnapshotBasePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures) + "\\";
            bgWorker.DoWork += Utilities.BackgroundWorker_DoWork;
            bgWorker.ProgressChanged += Utilities.BackgroundWorker_ProgressChanged;
            bgWorker.RunWorkerCompleted += Utilities.BackgroundWorker_RunWorkerCompleted;
            LogPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) +
                                 "\\Pelco\\VxSdk\\Logs";
        }

        /// <summary>
        /// Gets or sets the CurrentDevices property.
        /// </summary>
        /// <value>The current list of devices found on the VideoXpert system.</value>
        public static List<Device> CurrentDevices { get; set; }

        /// <summary>
        /// Gets or sets the CurrentDataSources property.
        /// </summary>
        /// <value>The current list of data sources found on the VideoXpert system.</value>
        public static List<DataSource> CurrentDataSources { get; set; }

        /// <summary>
        /// Gets or sets the CurrentPassword property.
        /// </summary>
        /// <value>The current VideoXpert system password.</value>
        public static string CurrentPassword { get; set; }

        /// <summary>
        /// Gets or sets the CurrentSystem property.
        /// </summary>
        /// <value>The current VideoXpert system.</value>
        public static VXSystem CurrentSystem { get; set; }

        /// <summary>
        /// Gets or sets the CurrentUserName property.
        /// </summary>
        /// <value>The current VideoXpert system user name.</value>
        public static string CurrentUserName { get; set; }

        /// <summary>
        /// Gets or sets the LogPath property.
        /// </summary>
        /// <value>The logging directory for the VxSDK.</value>
        public static string LogPath { get; set; }

        /// <summary>
        /// Gets or sets the SnapshotBasePath property.
        /// </summary>
        /// <value>The directory to save snapshots.</value>
        public static string SnapshotBasePath { get; set; }

        /// <summary>
        /// Gets the Instance property.
        /// </summary>
        /// <value>The current <see cref="MainForm"/> instance.</value>
        public static MainForm Instance { get; private set; }

        /// <summary>
        /// Gets or sets the PtzForm property.
        /// </summary>
        /// <value>A <see cref="PTZControlForm"/>.</value>
        public static PTZControlForm PtzForm { get; set; }

        /// <summary>
        /// Gets or sets the Control property.
        /// </summary>
        /// <value>A <see cref="ControlManager"/>.</value>
        public ControlManager Control { get; set; }

        /// <summary>
        /// The OnInternalEvent method.
        /// </summary>
        /// <param name="internalEvent">The <paramref name="internalEvent"/> parameter.</param>
        public void OnInternalEvent(InternalEvent internalEvent)
        {
            WriteToLog(internalEvent.Type.ToString());
        }

        /// <summary>
        /// The OnSystemEvent method.
        /// </summary>
        /// <param name="systemEvent">The <paramref name="systemEvent"/> parameter.</param>
        public void OnSystemEvent(Event systemEvent)
        {
            var type = EventDisplayForm.FormatText(systemEvent.SituationType.Split('/').Last());
            var severity = Math.Abs(systemEvent.Severity - 11);
            Device device = null;
            foreach (var dev in CurrentDevices.Where(dev => dev.Id == systemEvent.SourceDeviceId))
                device = dev;

            string devName = device != null ? device.Name : systemEvent.SourceDeviceId;
            WriteToLog(string.Format(Resources.EventHeaderFormatString, severity, type, devName));

            if (displayEventDialogsToolStripMenuItem.Checked)
            {
                Instance.Invoke(new MethodInvoker(delegate
                {
                    var eventForm = new EventDisplayForm(systemEvent);
                    eventForm.Show(Instance);
                }));
            }
        }

        /// <summary>
        /// The SendRequest method.
        /// </summary>
        /// <param name="uri">The request <c>Uri</c>.</param>
        /// <returns>The response message.</returns>
        public async Task<HttpResponseMessage> SendRequest(Uri uri)
        {
            // Create a new WebClient instance.
            var client = new HttpClient();

            // Supply the username and password that was used to create the VideoXpert system.
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add("X-Serenity-User", EncodeToBase64(CurrentUserName));
            request.Headers.Add("X-Serenity-Password", EncodeToBase64(CurrentPassword));

            // Forces the WebClient to trust the security certificate handed back from the VideoXpert server.
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            var response = await client.SendAsync(request);
            return response;
        }

        /// <summary>
        /// The WriteToLog method.
        /// </summary>
        /// <param name="message">The <paramref name="message"/> parameter.</param>
        public void WriteToLog(string message)
        {
            var time = DateTime.Now;
            txbxLog.BeginInvoke((MethodInvoker)delegate
            {
                txbxLog.AppendText(time.ToLongTimeString() + ": " + message + "\n");
            });
        }

        /// <summary>
        /// The EncodeToBase64 method.
        /// </summary>
        /// <param name="toEncode">The string to encode to Base64.</param>
        /// <returns>The Base64 encoded string.</returns>        
        private static string EncodeToBase64(string toEncode)
        {
            var toEncodeAsBytes = Encoding.ASCII.GetBytes(toEncode);
            var returnValue = Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        /// <summary>
        /// The ShowSnapshotDialog method.
        /// </summary>
        /// <param name="fileName">The file name of the snapshot image.</param>
        private static void ShowSnapshotDialog(string fileName)
        {
            var message = string.Format("Snapshot saved: {0}\n\n View snapshot?", fileName);
            var result = MessageBox.Show(message, @"View Snapshot", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
                Process.Start(fileName);
        }

        /// <summary>
        /// The ButtonLive_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void ButtonLive_Click(object sender, EventArgs args)
        {
            if (Control.Current == null) 
                StartStream();

            if (Control.Current == null) 
                return;

            Control.Current.GoToLive();
            SetManualRecordingStatus();
            Control.ChangePtzFormState(Control.PtzControl != null);
        }

        /// <summary>
        /// The ButtonManualRecord_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void ButtonManualRecord_Click(object sender, EventArgs args)
        {
            if (Control.Current == null)
                return;

            if (Control.CurrentDataSource == null)
                return;

            if (Control.CurrentManualRecording == null)
            {
                var newManualRecording = new NewManualRecording {DataSourceId = Control.CurrentDataSource.Id};

                Control.CurrentManualRecording = CurrentSystem.AddManualRecording(newManualRecording);
                if (Control.CurrentManualRecording == null)
                {
                    WriteToLog("Unable to start manual recording.");
                    return;
                }

                WriteToLog(string.Format("Started manual recording on {0}.", Control.CurrentDataSource.Name));
                btnManualRecord.Text = @"Stop";
                nudPreRecord.Enabled = false;
                nudPostRecord.Enabled = false;
            }
            else
            {
                var result = CurrentSystem.DeleteManualRecording(Control.CurrentManualRecording);
                if (result != Results.Value.OK)
                    WriteToLog(string.Format("Error: {0}.", result));

                WriteToLog(string.Format("Stopped manual recording on {0}.", Control.CurrentDataSource.Name));
                Control.CurrentManualRecording = null;
                btnManualRecord.Text = @"Record";
                if (Control.Current.Mode != MediaControl.Modes.Live)
                    return;

                nudPreRecord.Enabled = true;
                nudPostRecord.Enabled = true;
            }
        }

        /// <summary>
        /// The ButtonPause_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void ButtonPause_Click(object sender, EventArgs args)
        {
            if (Control.Current == null)
                return;

            Control.Current.Pause();
            Control.ChangePtzFormState(false);
            btnLive.Enabled = true;
            btnManualRecord.Enabled = false;
            nudPostRecord.Enabled = false;
            nudPreRecord.Enabled = false;
        }

        /// <summary>
        /// The ButtonPlay_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void ButtonPlay_Click(object sender, EventArgs args)
        {
            StartStream();
        }

        /// <summary>
        /// The ButtonRefreshDataSources_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void ButtonRefreshDataSources_Click(object sender, EventArgs args)
        {
            bgWorker.WorkerReportsProgress = true;
            bgWorker.RunWorkerAsync();
        }

        /// <summary>
        /// The ButtonSeek_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void ButtonSeek_Click(object sender, EventArgs args)
        {
            // The seek time value must be in UTC format.
            StartStream(dtpSeekTime.Value.ToUniversalTime());
        }

        /// <summary>
        /// The ButtonSnapshot_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void ButtonSnapshot_Click(object sender, EventArgs args)
        {
            if (Control.Current == null)
                return;

            var dataSource = Control.Current.CurrentDataSource;
            if (Control.Current.Mode == MediaControl.Modes.Live)
                SaveSnapshotLive(dataSource);

            if (Control.Current.Mode == MediaControl.Modes.Playback)
                SaveSnapshotRecorded(dataSource);
        }

        /// <summary>
        /// The ButtonStop_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void ButtonStop_Click(object sender, EventArgs args)
        {
            StopStream();
        }

        /// <summary>
        /// The GridViewDataSources_CellDoubleClick method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void GridViewDataSources_CellDoubleClick(object sender, DataGridViewCellEventArgs args)
        {
            StartStream();
        }

        /// <summary>
        /// The MenuItemLog_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemLog_Click(object sender, EventArgs args)
        {
            if (scOuter.Panel2Collapsed)
            {
                showLogToolStripMenuItem.Text = @"Hide Log";
                ClientSize = new Size(ClientSize.Width, ClientSize.Height + 90);
                scOuter.Panel2Collapsed = false;
            }
            else
            {
                showLogToolStripMenuItem.Text = @"Show Log";
                ClientSize = new Size(ClientSize.Width, ClientSize.Height - 90);
                scOuter.Panel2Collapsed = true;
            }
        }

        /// <summary>
        /// The MenuItemAlarmInputManager_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemAlarmInputManager_Click(object sender, EventArgs args)
        {
            using (var alarmInputManagerForm = new AlarmInputManagerForm())
            {
                alarmInputManagerForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemBookmarkManager_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemBookmarkManager_Click(object sender, EventArgs args)
        {
            using (var bookmarkForm = new BookmarkManagerForm())
            {
                bookmarkForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemClusterConfig_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemClusterConfig_Click(object sender, EventArgs args)
        {
            using (var clusterConfigDetailsForm = new ClusterConfigDetailsForm())
            {
                clusterConfigDetailsForm.GetClusterConfig(CurrentSystem);
                clusterConfigDetailsForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemDataObjectManager_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemDataObjectManager_Click(object sender, EventArgs args)
        {
            using (var dataObjectManager = new DataObjectManagerForm())
            {
                dataObjectManager.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemDataSourceManager_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemDataSourceManager_Click(object sender, EventArgs args)
        {
            using (var dataSourceManagerForm = new DataSourceManagerForm())
            {
                dataSourceManagerForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemDataStorageManager_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemDataStorageManager_Click(object sender, EventArgs args)
        {
            using (var dataStorageManager = new DataStorageManagerForm())
            {
                dataStorageManager.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemConnect_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemConnect_Click(object sender, EventArgs args)
        {
            using (var connectForm = new ConnectForm())
            {
                connectForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemExit_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemExit_Click(object sender, EventArgs args)
        {
            Close();
        }

        /// <summary>
        /// The MenuItemDeviceManager_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemDeviceManager_Click(object sender, EventArgs args)
        {
            using (var deviceManagerForm = new DeviceManagerForm())
            {
                deviceManagerForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemDrawingManager_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemDrawingManager_Click(object sender, EventArgs args)
        {
            using (var drawingManagerForm = new DrawingManagerForm())
            {
                drawingManagerForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemExportManager_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemExportManager_Click(object sender, EventArgs args)
        {
            using (var exportForm = new ExportManagerForm())
            {
                exportForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemInsertEvent_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemInsertEvent_Click(object sender, EventArgs args)
        {
            using (var addEventForm = new AddEventForm())
            {
                addEventForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemLogLevel_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemLogLevel_Click(object sender, EventArgs args)
        {
            var item = sender as ToolStripMenuItem;
            if (item == null)
                return;

            logTraceToolStripMenuItem.Checked = item == logTraceToolStripMenuItem;
            logDebugToolStripMenuItem.Checked = item == logDebugToolStripMenuItem;
            logInfoToolStripMenuItem.Checked = item == logInfoToolStripMenuItem;
            logWarningToolStripMenuItem.Checked = item == logWarningToolStripMenuItem;
            logErrorToolStripMenuItem.Checked = item == logErrorToolStripMenuItem;
            logFatalToolStripMenuItem.Checked = item == logFatalToolStripMenuItem;
            logDisableToolStripMenuItem.Checked = item == logDisableToolStripMenuItem;

            VxGlobal.SetLogLevel((LogLevel.Value)item.Tag);
        }

        /// <summary>
        /// The MenuItemLicenseDetails_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemLicenseDetails_Click(object sender, EventArgs args)
        {
            using (var licenseDetailsForm = new LicenseDetailsForm())
            {
                licenseDetailsForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemLogout_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemLogout_Click(object sender, EventArgs args)
        {
            StopAllStreams();
            dgvDataSources.Rows.Clear();
            CurrentUserName = string.Empty;
            CurrentPassword = string.Empty;
            if (CurrentDataSources != null)
                CurrentDataSources.Clear();

            if (CurrentDevices != null)
                CurrentDevices.Clear();
            
            CurrentDataSources = null;
            CurrentDevices = null;
            CurrentSystem = null;
            eventsToolStripMenuItem.Enabled = false;
            manageToolStripMenuItem.Enabled = false;
            btnSeek.Enabled = false;
            btnPause.Enabled = false;
            btnPlay.Enabled = false;
            btnStop.Enabled = false;
            btnSnapshot.Enabled = false;
            btnRefreshDataSources.Enabled = false;
            btnManualRecord.Enabled = false;
            nudPostRecord.Enabled = false;
            nudPreRecord.Enabled = false;
        }

        /// <summary>
        /// The MenuItemModifyLoggingPath_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemModifyLoggingPath_Click(object sender, EventArgs args)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = LogPath;
                folderDialog.Description = @"Choose Logging Path...";

                var result = folderDialog.ShowDialog();
                if (result != DialogResult.OK)
                    return;

                LogPath = folderDialog.SelectedPath;
                VxGlobal.SetLogPath(LogPath.Replace(@"\", @"\\"));
                WriteToLog(string.Format("Logging Path changed to: {0}", folderDialog.SelectedPath));
            }
        }

        /// <summary>
        /// The MenuItemMonitors_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemMonitors_Click(object sender, EventArgs args)
        {
            using (var monitorManagerForm = new MonitorManagerForm())
            {
                monitorManagerForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemNotifications_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemNotifications_Click(object sender, EventArgs args)
        {
            using (var notificationManagerForm = new NotificationManagerForm())
            {
                notificationManagerForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemProtocol_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemProtocol_Click(object sender, EventArgs args)
        {
            var item = sender as ToolStripMenuItem;
            if (item != null && item.Text == @"MJPEG")
            {
                rtspToolStripMenuItem.Checked = false;
                mjpegToolStripMenuItem.Checked = true;
            }
            else
            {
                rtspToolStripMenuItem.Checked = true;
                mjpegToolStripMenuItem.Checked = false;
            }

            StopAllStreams();
        }

        /// <summary>
        /// The MenuItemQuickLog_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemQuickLog_Click(object sender, EventArgs args)
        {
            using (var quickLogForm = new QuickLogForm())
            {
                quickLogForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemQuickReport_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemQuickReport_Click(object sender, EventArgs args)
        {
            using (var quickReportForm = new QuickReportForm())
            {
                quickReportForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemRelayOutputManager_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemRelayOutputManager_Click(object sender, EventArgs args)
        {
            using (var relayOutputManagerForm = new RelayOutputManagerForm())
            {
                relayOutputManagerForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemRoleManager_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemRoleManager_Click(object sender, EventArgs args)
        {
            using (var roleForm = new RoleManagerForm())
            {
                roleForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemScheduleManager_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemScheduleManager_Click(object sender, EventArgs args)
        {
            using (var scheduleForm = new ScheduleManagerForm())
            {
                scheduleForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemScheduleManager_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemSearchEvents_Click(object sender, EventArgs args)
        {
            using (var eventSearchForm = new EventSearchForm())
            {
                eventSearchForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemShowPtzControls_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemShowPtzControls_Click(object sender, EventArgs args)
        {
            PtzForm.Show();
        }

        /// <summary>
        /// The MenuItemSituationManager_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemSituationManager_Click(object sender, EventArgs args)
        {
            using (var situationForm = new SituationManagerForm())
            {
                situationForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemTagManager_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemTagManager_Click(object sender, EventArgs args)
        {
            using (var tagForm = new TagManagerForm())
            {
                tagForm.ShowDialog();
            }
        }

        /// <summary>
        /// The MenuItemSnapshotPath_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemSnapshotPath_Click(object sender, EventArgs args)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = SnapshotBasePath;
                folderDialog.Description = @"Choose Snapshot Save Path...";

                var result = folderDialog.ShowDialog();
                if (result != DialogResult.OK) 
                    return;

                SnapshotBasePath = folderDialog.SelectedPath + "\\";
                var message = string.Format("Snapshot Path changed: {0}", folderDialog.SelectedPath);
                MessageBox.Show(message, @"Snapshot Path");
            }
        }

        /// <summary>
        /// The MenuItemSubscribe_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemSubscribe_Click(object sender, EventArgs args)
        {
            if (!subscribeToSystemEventsToolStripMenuItem.Checked)
            {
                // Only one subscription can be active at once.  So unsubscribe to the system
                // events if it is active.
                if (subscribeToSystemEventsByTypeToolStripMenuItem.Checked)
                {
                    CurrentSystem.SystemEvent -= OnSystemEvent;
                    subscribeToSystemEventsByTypeToolStripMenuItem.Checked = false;
                }

                CurrentSystem.SystemEvent += OnSystemEvent;
                subscribeToSystemEventsToolStripMenuItem.Checked = true;
                WriteToLog("Subscribed to events.");
            }
            else
            {
                CurrentSystem.SystemEvent -= OnSystemEvent;
                subscribeToSystemEventsToolStripMenuItem.Checked = false;
                WriteToLog("Unsubscribed to events.");
            }
        }

        /// <summary>
        /// The MenuItemSubscribeByType_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemSubscribeByType_Click(object sender, EventArgs args)
        {
            if (!subscribeToSystemEventsByTypeToolStripMenuItem.Checked)
            {
                // Only one subscription can be active at once.  So unsubscribe to the type specific
                // system events if it is active.
                if (subscribeToSystemEventsToolStripMenuItem.Checked)
                {
                    CurrentSystem.SystemEvent -= OnSystemEvent;
                    subscribeToSystemEventsToolStripMenuItem.Checked = false;
                }

                using (var situationTypeSelectionForm = new SituationTypeSelectionForm())
                {
                    situationTypeSelectionForm.ShowDialog();
                }

                subscribeToSystemEventsByTypeToolStripMenuItem.Checked = true;
            }
            else
            {
                CurrentSystem.SystemEvent -= OnSystemEvent;
                subscribeToSystemEventsByTypeToolStripMenuItem.Checked = false;
                WriteToLog("Unsubscribed to events.");
            }
        }

        /// <summary>
        /// The MenuItemDisplayEventDialogs_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemDisplayEventDialogs_Click(object sender, EventArgs args)
        {
            displayEventDialogsToolStripMenuItem.Checked = !displayEventDialogsToolStripMenuItem.Checked;
        }

        /// <summary>
        /// The MenuItemUserManager_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void MenuItemUserManager_Click(object sender, EventArgs args)
        {
            using (var userForm = new UserManagerForm())
            {
                userForm.ShowDialog();
            }
        }

        /// <summary>
        /// The PanelVideoStream_Click method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void PanelVideoStream_Click(object sender, EventArgs args)
        {
            var panel = sender as Panel;
            if (panel == null)
                return;

            if (Control.SelectedPanel != panel)
                return;

            var point = panel.PointToClient(Cursor.Position);
            if (Control.PtzControl == null)
                return;

            float relX;
            float relY;
            var xMidPoint = (float)panel.Width / 2;
            if (point.X > xMidPoint)
            {
                var adjustedX = point.X - xMidPoint;
                relX = adjustedX / xMidPoint * 100;
            }
            else
            {
                var adjustedX = xMidPoint - point.X;
                relX = adjustedX / xMidPoint * -100;
            }

            var yMidPoint = (float)panel.Height / 2;
            if (point.Y > yMidPoint)
            {
                var adjustedY = yMidPoint - point.Y;
                relY = adjustedY / yMidPoint * 100;
            }
            else
            {
                var adjustedY = point.Y - yMidPoint;
                relY = adjustedY / yMidPoint * -100;
            }

            var result = Control.PtzControl.RelativePercentageMove((int)relX, (int)relY);
            if (result != Results.Value.OK)
                WriteToLog(string.Format("Error: {0}.", result));
        }

        /// <summary>
        /// The PanelVideoStream_MouseEnter method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void PanelVideoStream_MouseEnter(object sender, EventArgs args)
        {
            var panel = sender as Panel;
            if (panel == null)
                return;

            if (Control.SelectedPanel != panel)
                return;

            if (Control.PtzControl == null)
                return;

            panel.Cursor = Cursors.Cross;
        }

        /// <summary>
        /// The PanelVideoStream_MouseLeave method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void PanelVideoStream_MouseLeave(object sender, EventArgs args)
        {
            var panel = sender as Panel;
            if (panel == null)
                return;

            if (Control.SelectedPanel != panel)
                return;

            if (Control.PtzControl == null)
                return;

            panel.Cursor = Cursors.Default;
        }

        /// <summary>
        /// The PanelVideoStream_MouseWheel method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void PanelVideoStream_MouseWheel(object sender, MouseEventArgs args)
        {
            var panel = sender as Panel;
            if (panel == null)
                return;

            if (Control.SelectedPanel != panel)
                return;

            if (Control.PtzControl == null)
                return;

            var zoomLevel = (float)args.Delta / 2;
            var result = Control.PtzControl.RelativeMove(0, 0, (int)zoomLevel);
            if (result != Results.Value.OK)
                WriteToLog(string.Format("Error: {0}.", result));
        }

        /// <summary>
        /// The PanelVideoStreamLeft_MouseClick method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void PanelVideoStreamLeft_MouseClick(object sender, MouseEventArgs args)
        {
            Control.SelectControl(ControlManager.Controls.Left);
            SetManualRecordingStatus();
            if (Control.PtzControl == null)
                return;

            Control.SelectedPanel.Cursor = Cursors.Cross;
        }

        /// <summary>
        /// The PanelVideoStreamRight_MouseClick method.
        /// </summary>
        /// <param name="sender">The <paramref name="sender"/> parameter.</param>
        /// <param name="args">The <paramref name="args"/> parameter.</param>
        private void PanelVideoStreamRight_MouseClick(object sender, MouseEventArgs args)
        {
            Control.SelectControl(ControlManager.Controls.Right);
            SetManualRecordingStatus();
            if (Control.PtzControl == null)
                return;

            Control.SelectedPanel.Cursor = Cursors.Cross;
        }

        /// <summary>
        /// The SaveSnapshotLive method.
        /// </summary>
        /// <param name="dataSource">The data source to use for the snapshot.</param>
        private async void SaveSnapshotLive(DataSource dataSource)
        {
            var uri = new Uri(dataSource.SnapshotUri);
            var response = await SendRequest(uri);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                WriteToLog(string.Format("Unable to take snapshot, server returned {0}.", response.StatusCode));
                return;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var imageTime = Control.SelectedPanelTime.Replace(":", string.Empty);
            var filename = SnapshotBasePath + "Live-" + imageTime + ".jpg";
            if (!Directory.Exists(SnapshotBasePath))
            {
                WriteToLog(string.Format("Unable to take snapshot, path no longer exists {0}.", SnapshotBasePath));
                return;
            }

            BinaryWriter binWriter = null;
            try
            {
                binWriter = new BinaryWriter(new FileStream(filename, FileMode.Append, FileAccess.Write));
                binWriter.Write(bytes);
                binWriter.Close();
            }
            finally
            {
                if (binWriter != null)
                {
                    binWriter.Dispose();
                }
            }

            ShowSnapshotDialog(filename);
        }

        /// <summary>
        /// The SaveSnapshotRecorded method.
        /// </summary>
        /// <param name="dataSource">The data source to use for the snapshot.</param>
        private async void SaveSnapshotRecorded(DataSource dataSource)
        {
            var listBytes = new List<ImageByteArray>();
            var clips = dataSource.Clips;

            // Find the clip that contains the section of video currently playing.
            Clip currentClip = null;
            foreach (var clip in clips)
            {
                var time = DateTime.Parse(Control.SelectedPanelTime).ToUniversalTime();
                if (time > clip.StartTime && time < clip.EndTime)
                    currentClip = clip;
            }

            // Exit if no clip was found.
            if (currentClip == null)
            {
                WriteToLog("Error: Unable to find clip.");
                return;
            }

            var filter = new SnapshotFilter
            {
                StartTime = DateTime.Parse(Control.SelectedPanelTime).ToUniversalTime(),
                EndTime = DateTime.Parse(Control.SelectedPanelTime).ToUniversalTime().AddSeconds(1),
                Offset = 10
            };

            var uri = new Uri(currentClip.GetSnapshotEndpoint(filter));
            var response = await SendRequest(uri);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                WriteToLog(string.Format("Unable to take snapshot, server returned {0}.", response.StatusCode));
                return;
            }

            var mpmsp = await response.Content.ReadAsMultipartAsync();
            foreach (var content in mpmsp.Contents)
            {
                var bytes = await content.ReadAsByteArrayAsync();
                if (bytes == null)
                    continue;

                var imgByteArray = new ImageByteArray(bytes, content.Headers);
                listBytes.Add(imgByteArray);
            }

            foreach (var byteArray in listBytes)
            {
                var imageTime = byteArray.ImageTime.ToString("s").Replace(":", string.Empty);
                var filename = SnapshotBasePath + "Recorded-" + imageTime + ".jpg";
                if (!Directory.Exists(SnapshotBasePath))
                {
                    WriteToLog(string.Format("Unable to take snapshot, path no longer exists {0}.", SnapshotBasePath));
                    return;
                }

                BinaryWriter binWriter = null;
                try
                {
                    binWriter = new BinaryWriter(new FileStream(filename, FileMode.Append, FileAccess.Write));
                    binWriter.Write(byteArray.Bytes);
                    binWriter.Close();
                }
                finally
                {
                    if (binWriter != null)
                        binWriter.Dispose();
                }

                ShowSnapshotDialog(filename);
            }
        }

        /// <summary>
        /// The SetupPtzControls method.
        /// </summary>
        /// <param name="dataSource">The <paramref name="dataSource"/> parameter.</param>
        /// <remarks>Checks if PTZ is enabled on the device.  If enabled, it also gets the
        /// available presets and patterns for the device.</remarks>
        private void SetupPtzControls(DataSource dataSource)
        {
            Control.PtzControl = dataSource.IsPTZ ? dataSource.PTZController : null;
            Control.ChangePtzFormState(dataSource.IsPTZ);
        }

        /// <summary>
        /// The SelectDataInterface method.
        /// </summary>
        /// <param name="selProtocol">The selected protocol.</param>
        /// <param name="dataSource">The selected data source.</param>
        /// <param name="showWindow">Selects the first available stream if False.</param>
        /// <returns>The currently selected data interface.</returns>
        private DataInterface SelectDataInterface(VxStreamProtocol selProtocol, DataSource dataSource, bool showWindow)
        {
            DataInterface dataInterface;
            if (selProtocol == VxStreamProtocol.RtspRtp)
            {
                var interfaceList = dataSource.DataInterfaces.Where(iface =>
                    iface.Protocol == DataInterface.StreamProtocols.RtspRtp).ToList();

                if (interfaceList.Count == 0)
                    return null;

                if (!showWindow)
                    return interfaceList[0];

                if (interfaceList.Count > 1)
                {
                    using (var streamSelectionForm = new StreamSelectionForm(interfaceList))
                    {
                        streamSelectionForm.ShowDialog();
                        if (streamSelectionForm.SelectedInterface == null)
                            return null;
                        dataInterface = streamSelectionForm.SelectedInterface;
                    }
                }
                else
                    dataInterface = interfaceList[0];
            }
            else
            {
                dataInterface = dataSource.DataInterfaces.First(iface =>
                    iface.Protocol == DataInterface.StreamProtocols.MjpegPull);
            }

            return dataInterface;
        }

        /// <summary>
        /// Sets the state of the manual recording UI elements based on the manual recording status of the current stream.
        /// </summary>
        public void SetManualRecordingStatus()
        {
            if (Control.CurrentDataSource == null)
            {
                btnManualRecord.Text = @"Record";
                btnManualRecord.Enabled = false;
                nudPreRecord.Enabled = false;
                nudPostRecord.Enabled = false;
                return;
            }

            var manualRecordings = CurrentSystem.GetManualRecordings();
            var userUpn = CurrentSystem.Currentuser.Name + "@" + CurrentSystem.Currentuser.Domain;
            foreach (var manualRecording in manualRecordings)
            {
                if (manualRecording.DataSourceId == Control.CurrentDataSource.Id && manualRecording.OwnerName == userUpn)
                {
                    Control.CurrentManualRecording = manualRecording;
                    btnManualRecord.Text = @"Stop";
                    btnManualRecord.Enabled = true;
                    nudPreRecord.Enabled = false;
                    nudPostRecord.Enabled = false;
                    return;
                }
            }

            btnManualRecord.Text = @"Record";
            btnManualRecord.Enabled = Control.Current.Mode == MediaControl.Modes.Live;
            nudPreRecord.Enabled = Control.Current.Mode == MediaControl.Modes.Live;
            nudPostRecord.Enabled = Control.Current.Mode == MediaControl.Modes.Live;
        }

        /// <summary>
        /// The StartStream method.
        /// </summary>
        /// <param name="seekTime">The <paramref name="seekTime"/> in UTC format.  If no value or a default 
        /// DateTime object is given then the stream is started in live mode.</param>
        private void StartStream(DateTime seekTime = default(DateTime))
        {
            try
            {
                // Get the data sources for the selected device.
                var dataSource = (DataSource)dgvDataSources.SelectedRows[0].Tag;
                var protocol = mjpegToolStripMenuItem.Checked ? VxStreamProtocol.MjpegPull : VxStreamProtocol.RtspRtp;
                var showWindow = true;
                if (Control.Current != null)
                    showWindow = Control.Current.Mode == MediaControl.Modes.Stopped;

                var dataInterface = SelectDataInterface(protocol, dataSource, showWindow);
                if (dataInterface == null)
                {
                    WriteToLog("Error: No data interface found for selected camera.\n");
                    return;
                }

                DataSource audioDataSource;
                DataInterface audioDataInterface;
                var audioLink = dataSource.LinkedAudioRelation;
                if (audioLink != null)
                {
                    audioDataSource = audioLink.Resource;
                    audioDataInterface = SelectDataInterface(VxStreamProtocol.RtspRtp, audioDataSource, false);
                }
                else
                    SelectAudioData(dataSource, showWindow, out audioDataSource, out audioDataInterface);

                // If the media controller exists then a stream is running and the user is
                // requesting a new action on it.  If it's null then this is either the
                // first run or an existing stream has been stopped.  So a new media controller
                // instance is needed.
                if (Control.Current == null)
                {
                    Control.Current = new MediaControl(dataSource, dataInterface, audioDataSource, audioDataInterface);
                    Control.SubscribeToTimestamps();
                    Control.Current.SetVideoWindow(Control.SelectedPanel.Handle);
                }
                else
                {
                    // If a new device has been selected while another stream is running, stop the
                    // old stream and set up the new stream using the new data source.
                    if ((Control.CurrentDataSource == null) || (dataSource.Id != Control.CurrentDataSource.Id))
                    {
                        Control.Current.Stop();
                        Control.Current.SetDataSource(dataSource, dataInterface, audioDataSource, audioDataInterface);
                    }
                }

                if (seekTime == default(DateTime))
                {
                    if (!Control.Current.Play((float)nudSpeed.Value))
                    {
                        WriteToLog(string.Format("Error: Unable to {0} stream.\n",
                            Control.Current.Mode == MediaControl.Modes.Playback ? "resume" : "start"));
                        if (Control.Current.IsPipelineActive)
                        {
                            StopStream();
                            return;
                        }

                        Control.Current.Dispose();
                        Control.Current = null;
                        Control.SelectedPanel.Refresh();
                        return;
                    }
                }
                else
                {
                    if (!Control.Current.Seek(seekTime, (float)nudSpeed.Value))
                    {
                        WriteToLog("Error: Unable to start recorded stream.\n");
                        if (Control.Current.IsPipelineActive)
                        {
                            StopStream();
                            return;
                        }

                        Control.SelectedLabel.BeginInvoke((MethodInvoker)delegate
                        {
                            Control.SelectedLabel.Text = string.Empty;
                        });
                        Control.Current.Dispose();
                        Control.Current = null;
                        Control.SelectedPanel.Refresh();
                        return;
                    }
                }

                Control.SetPlayingIndex();
                SetupPtzControls(dataSource);
                Control.CurrentDataSource = dataSource;
                SetManualRecordingStatus();
            }
            catch (Exception ex)
            {
                WriteToLog(string.Format(@"Error: {0}\n", ex.Message));
            }
        }

        /// <summary>
        /// Subscribes to internal VxSDK events.
        /// </summary>
        public void SubscribeToInternalEvents()
        {
            CurrentSystem.InternalEvent += OnInternalEvent;
        }

        /// <summary>
        /// The StopAllStreams method.
        /// </summary>
        private void StopAllStreams()
        {
            try
            {
                var selControl = Control.SelectedControl;
                Control.SelectControl(ControlManager.Controls.Left);
                StopStream();
                Control.SelectControl(ControlManager.Controls.Right);
                StopStream();
                Control.SelectControl(selControl);
            }
            catch (Exception ex)
            {
                WriteToLog(string.Format(@"Error: {0}\n", ex.Message));
            }
        }

        /// <summary>
        /// The StopStream method.
        /// </summary>
        private void StopStream()
        {
            try
            {
                if (Control.Current == null)
                    return;

                Control.Current.Stop();

                Control.UnsubscribeToTimestamps();
                Control.SelectedLabel.BeginInvoke((MethodInvoker)delegate
                {
                    Control.SelectedLabel.Text = string.Empty;
                });

                Control.SelectedPanel.Refresh();

                Control.PtzControl = null;
                Control.CurrentDataSource = null;
                Control.CurrentManualRecording = null;
                btnManualRecord.Text = @"Record";
                btnManualRecord.Enabled = false;
                nudPostRecord.Enabled = false;
                nudPreRecord.Enabled = false;
                Control.ChangePtzFormState(false);

                Control.Current.Dispose();
                Control.Current = null;
            }
            catch (Exception ex)
            {
                WriteToLog(string.Format(@"Error: {0}\n", ex.Message));
            }
        }

        /// <summary>
        /// Select the audio data source and data interfaces from the MainForm.CurrentDataSources
        /// </summary>
        /// <param name="videoSource">Video data source</param>
        /// <param name="showWindow">Specifies wheather to show the window or not</param>
        /// <param name="audioSource">Out parameter for audio data source</param>
        /// <param name="audioInterface">Out parameter for audio data interface</param>
        private void SelectAudioData(DataSource videoSource, bool showWindow, out DataSource audioSource, out DataInterface audioInterface)
        {
            audioSource = null;
            audioInterface = null;

            foreach (var ds in CurrentDataSources)
            {
                if (videoSource.Name != ds.Name || ds.Type != DataSource.Types.Audio)
                    continue;

                audioInterface = SelectDataInterface(VxStreamProtocol.RtspRtp, ds, showWindow);
                if (audioInterface == null)
                {
                    WriteToLog("Error: No audio data interface found for selected camera.\n");
                    return;
                }

                audioSource = ds;
            }
        }
    }
}
