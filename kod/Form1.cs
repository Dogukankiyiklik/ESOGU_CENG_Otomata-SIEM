using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;

namespace OOP_Deneme
{
    public partial class Form1 : Form
    {
        // XML'deki DFA yapısını depolamak için
        bool validation = true;
        DFA dfa;

        // Log dosyalarını parse etmek için
        DateTime today;
        string logFilePath;
        LogManager log_manager;

        // DFA' i işlemek için
        List<string> connectedDevices;
        State currentState;
        int count_id;
        public Form1()
        {
            InitializeComponent();
            dfa = new DFA();

            // XML dosya yolu
            string xmlFile = "DFA.xml";
            if (!File.Exists(xmlFile))
            {
                MessageBox.Show("Dosya Bulunamadı!");
                return;
            }

            // XML ayarları
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;
            settings.ValidationType = ValidationType.DTD;
            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            settings.XmlResolver = new XmlUrlResolver(); // DTD dosyasını çözümlemek için

            // XML okuyucusu (XML'in DTD uygun olup olmadığı kontrol edilir.)
            using (XmlReader reader = XmlReader.Create(xmlFile, settings))
            {
                try
                {
                    // XML dosyasını okuma ve doğrulama
                    while (reader.Read()) { }
                    if (validation)
                    {
                        MessageBox.Show("XML dosyası başarıyla doğrulandı.");
                        dfa.LoadFromXml(xmlFile);
                    }
                    else
                    {
                        MessageBox.Show("XML formatına uygun değil!");
                        return;
                    }
                }
                catch (XmlException ex)
                {
                    MessageBox.Show("XML hatası: " + ex.Message);
                }
                catch (XmlSchemaValidationException ex)
                {
                    MessageBox.Show("Doğrulama hatası: " + ex.Message);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Bilinmeyen hata: " + ex.Message);
                }
            }

            // initial state'i ayarlama
            currentState = new State();
            foreach (State state in dfa.States_list)
            {
                if (state.Initial)
                {
                    currentState = state;
                    break;
                }
            }

            // Log dosyalarını parse etme
            count_id = 0;
            log_manager = new LogManager();
            today = DateTime.Today;
            logFilePath = log_manager.logFolderPath + @"\" + today.ToString(log_manager.logFileNamePattern) + log_manager.logFileExtension;
            log_manager.StartLogMonitoring(logFilePath, dataGridView, 5000);

            // 1 saniyede bir döngüye sokma
            connectedDevices = new List<string>();
            var process_dfa = new Thread(() =>
            {
                while (true)
                {
                    if (log_manager.newLogs.Count != 0)
                    {
                        process_Log(log_manager.newLogs);
                    }
                    Thread.Sleep(5000);
                }
            });

            process_dfa.IsBackground = true;
            process_dfa.Start();
        }

        // DTD'ye uygun mu araması
        void ValidationCallback(object sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Warning)
            {
                MessageBox.Show("Uyarı: " + e.Message);
            }
            else if (e.Severity == XmlSeverityType.Error)
            {
                validation = false;
            }
        }

        //logları işlemek
        void process_Log(List<Log> _logs)
        {
            foreach (Log log in _logs)
            {
                if (log.Event == "USB DEVICE INSERTED")
                {
                    connected_device_check(log.DeviceID, true);
                    chanceState("BAĞLI CİHAZ UYARISI VAR", connectedDevices.Count.ToString(), "");
                }
                else if (log.Event == "USB DEVICE REMOVED")
                {
                    connected_device_check(log.DeviceID, false);
                    chanceState("BAĞLI CİHAZ UYARISI VAR", connectedDevices.Count.ToString(), "");
                }
                else if (log.Event == "USB File Copy Operation")
                {
                    string[] bilgiler = new string[3];
                    bilgiler = path_check(log);
                    chanceState(bilgiler[0], bilgiler[1], bilgiler[2]);// Dosyanın, D -> C mi yoksa C -> D yemi olduğunu kontrol etsin ve ona göre işlem yapsın.
                    break;
                }
            }
        }
        string[] path_check(Log temp)
        {
            if (temp.Files[0] == 'D') // USB -> Masaüstü (STATE)
            {
                string[] parcalar = temp.Files.Split(new string[] { "->" }, StringSplitOptions.None);
                // İkinci parçayı Trim metoduyla boşluklardan arındırma
                string ikinciParca = parcalar[1].Trim();
                // İlk parçayı ve ikinci parçayı birleştirme
                string yeniYol = System.IO.Path.Combine(ikinciParca, System.IO.Path.GetFileName(parcalar[0]));

                double boyutMB;
                // dosya mı?
                if (File.Exists(yeniYol))
                {
                    FileInfo fileInfo = new FileInfo(yeniYol);
                    long boyutByte = fileInfo.Length;
                    boyutMB = boyutByte / 1024.0 / 1024.0;
                }
                // klasör mü?
                else
                {
                    long folderSizeBytes = GetDirectorySize(yeniYol);
                    boyutMB = folderSizeBytes / 1024.0 / 1024.0;
                }

                return new string[] { "DOSYA ATILMA UYARISI VAR", boyutMB.ToString(), yeniYol };
            }
            else // Masaüstü -> USB boyut bilgisini öğrensin (STATE)
            {
                string eski_konum = temp.Files;
                int index = eski_konum.IndexOf("->");
                string yeni_konum = eski_konum.Substring(0, index);

                if (!File.Exists(yeni_konum) && !Directory.Exists(yeni_konum)) // Masaüstü -> USB ve Masaüstünden silinmiş (STATE)
                {
                    return new string[] { "DOSYA SİLİNME UYARISI VAR", "BİLİNMİYOR", yeni_konum };
                }

                else
                {
                    double boyutMB;
                    // dosya mı?
                    if (File.Exists(yeni_konum))
                    {
                        FileInfo fileInfo = new FileInfo(yeni_konum);
                        long boyutByte = fileInfo.Length;
                        boyutMB = boyutByte / 1024.0 / 1024.0;
                    }
                    // klasör mü?
                    else
                    {
                        long folderSizeBytes = GetDirectorySize(yeni_konum);
                        boyutMB = folderSizeBytes / 1024.0 / 1024.0;
                    }

                    return new string[] { "DOSYA BOYUT UYARISI VAR", boyutMB.ToString(), yeni_konum };
                }
            }
        }

        // bağlı cihazları listeye ekleme
        void connected_device_check(string device_id, bool state)
        {
            if (!connectedDevices.Contains(device_id))
            {
                connectedDevices.Add(device_id);
            }
            if (connectedDevices.Contains(device_id) && !state)
            {
                connectedDevices.Remove(device_id);
            }
        }

        // Inoke etmek için fonksiyon
        private void UpdateDataGridView(string[] row)
        {
            if (dataGridView1.InvokeRequired)
            {
                dataGridView1.Invoke(new Action<string[]>(UpdateDataGridView), new object[] { row });
            }
            else
            {
                dataGridView1.Rows.Add(row);
            }
        }

        // Eğer kopyalanan kalasör ise onun boyutunu ölçer
        public long GetDirectorySize(string folderPath)
        {
            long size = 0;

            DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
            foreach (FileInfo file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                size += file.Length;
            }

            return size;
        }

        // state'i değiştirme işlemi
        void chanceState(string uyarı, string limit, string dosya_konumu)
        {
            bool stateChanged = false;
            foreach (Transition transition in dfa.Transitions_list)
            {
                if (dfa.Alphabet_list[Int32.Parse(transition.ConditionId)].Limit != null)
                {
                    if (dfa.Alphabet_list[Int32.Parse(transition.ConditionId)].Symbol == uyarı && Double.Parse(dfa.Alphabet_list[Int32.Parse(transition.ConditionId)].Limit) >= Double.Parse(limit))
                    {
                        uyarı = "NORMAL";
                    }

                    if (limit != "BİLİNMİYOR" && Double.Parse(dfa.Alphabet_list[Int32.Parse(transition.ConditionId)].Limit) < Double.Parse(limit) && dfa.Alphabet_list[Int32.Parse(transition.ConditionId)].Symbol == uyarı && currentState.StateId == transition.From)
                    {
                        currentState = dfa.States_list[Int32.Parse(transition.To)];
                        stateChanged = true;
                        break;
                    }
                }
                else
                {
                    if (dfa.Alphabet_list[Int32.Parse(transition.ConditionId)].Symbol == uyarı && currentState.StateId == transition.From)
                    {
                        currentState = dfa.States_list[Int32.Parse(transition.To)];
                        stateChanged = true;
                        break;
                    }
                }
            }

            if (stateChanged && !currentState.Initial)
            {
                string[] row = new string[6];
                row[0] = count_id.ToString();
                row[1] = DateTime.Now.ToString();
                row[2] = currentState.Name;
                if (limit != "BİLİNMİYOR" && Double.Parse(connectedDevices.Count.ToString()) != Double.Parse(limit))
                {
                    row[3] = limit;
                }
                else
                {
                    row[3] = "";
                }
                row[4] = connectedDevices.Count.ToString();
                row[5] = dosya_konumu;

                UpdateDataGridView(row);
                count_id++;
            }
        }
    }
}