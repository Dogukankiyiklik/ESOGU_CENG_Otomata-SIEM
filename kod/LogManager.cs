using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace OOP_Deneme
{
    public class LogManager
    {
        /* EKLEME */
        public List<Log> newLogs = new List<Log>();
        public int lastProcessedLogIndex;
        //***************
        public List<Log> Logs { get; private set; }
        public string logFolderPath = @"C:\Program Files\NoVirusThanks\USB Radar\Logs";
        public string logFileExtension = ".log";
        public string logFileNamePattern = "d.MM.yyyy";
        public string[] logHeaders =
        {
            "Event", "Date/ Time", "Process", "Username/ Domain", "File(s)",
            "Device Drive", "Device Name", "Device Description", "Device Type",
            "Device ID", "Device GUID"
        };

        public LogManager()
        {
            Logs = new List<Log>();
        }

        public void ParseLogFile(string filePath)
        {
            bool readingFiles = false;
            Logs.Clear();
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Log file not found.", filePath);

            var lines = File.ReadAllLines(filePath);
            Log currentLog = null;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (currentLog != null)
                    {
                        Logs.Add(currentLog);
                        currentLog = null;
                    }
                    readingFiles = false;
                    continue;
                }

                if (line.StartsWith("Event:"))
                {
                    currentLog = new Log();
                    currentLog.Event = line.Substring("Event:".Length).Trim();
                    readingFiles = false;
                }
                else if (line.StartsWith("Date/Time:"))
                {
                    currentLog.DateTime = line.Substring("Date/Time:".Length).Trim();
                    readingFiles = false;
                }
                else if (line.StartsWith("Process:"))
                {
                    currentLog.Process = line.Substring("Process:".Length).Trim();
                    readingFiles = false;
                }
                else if (line.StartsWith("Username/Domain:"))
                {
                    currentLog.UsernameDomain = line.Substring("Username/Domain:".Length).Trim();
                    readingFiles = false;
                }
                else if (line.StartsWith("File(s):"))
                {
                    currentLog.Files = line.Substring("File(s):".Length).Trim();
                    readingFiles = true;
                }
                else if (line.StartsWith("Device Drive:"))
                {
                    currentLog.DeviceDrive = line.Substring("Device Drive:".Length).Trim();
                    readingFiles = false;
                }
                else if (line.StartsWith("Device Name:"))
                {
                    currentLog.DeviceName = line.Substring("Device Name:".Length).Trim();
                    readingFiles = false;
                }
                else if (line.StartsWith("Device Description:"))
                {
                    currentLog.DeviceDescription = line.Substring("Device Description:".Length).Trim();
                    readingFiles = false;
                }
                else if (line.StartsWith("Device Type:"))
                {
                    currentLog.DeviceType = line.Substring("Device Type:".Length).Trim();
                    readingFiles = false;
                }
                else if (line.StartsWith("Device ID:"))
                {
                    currentLog.DeviceID = line.Substring("Device ID:".Length).Trim();
                    readingFiles = false;
                }
                else if (line.StartsWith("Device GUID:"))
                {
                    currentLog.DeviceGUID = line.Substring("Device GUID:".Length).Trim();
                    readingFiles = false;
                }
                else if(readingFiles)
                {
                    currentLog.Files += line.Trim();
                }
            }

            if (currentLog != null)
            {
                Logs.Add(currentLog);
            }

            /* EKLENDİ */
            newLogs = Logs.Skip(lastProcessedLogIndex).ToList();
            lastProcessedLogIndex = Logs.Count;
            //************
        }

        public void WriteLogsToDataGridView(DataGridView dataGridView)
        {
            dataGridView.Rows.Clear();
            dataGridView.Columns.Clear();

            foreach (var header in logHeaders)
            {
                dataGridView.Columns.Add(header, header);
            }

            foreach (var log in Logs)
            {
                dataGridView.Rows.Add(
                    log.Event,
                    log.DateTime,
                    log.Process,
                    log.UsernameDomain,
                    log.Files,
                    log.DeviceDrive,
                    log.DeviceName,
                    log.DeviceDescription,
                    log.DeviceType,
                    log.DeviceID,
                    log.DeviceGUID
                );
            }
        }

        public void StartLogMonitoring(string logFilePath, DataGridView dataGridView, int interval)
        {
            var logMonitorThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        ParseLogFile(logFilePath);
                        dataGridView.Invoke((MethodInvoker)delegate
                        {
                            WriteLogsToDataGridView(dataGridView);
                        });
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions (e.g., log them)
                    }
                    Thread.Sleep(interval);
                }
            });

            logMonitorThread.IsBackground = true;
            logMonitorThread.Start();
        }
    }
}