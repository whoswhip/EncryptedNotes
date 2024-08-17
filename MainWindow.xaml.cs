using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using Microsoft.Win32;
using System.Buffers.Text;
using System.Text.Unicode;

namespace EncryptedNotes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string PublicKey = string.Empty;
        public string PrivateKey = string.Empty;
        public string PublicKeyPath = string.Empty;
        public string PrivateKeyPath = string.Empty;
        public string OpenFilePath = string.Empty;
        public MainWindow()
        {
            InitializeEncryption();
            InitializeComponent();
            
        }

        private async void InitializeEncryption()
        {
            string appdatalocalpath = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!Directory.Exists(appdatalocalpath + "\\EncryptedNotes"))
            {
                Directory.CreateDirectory(appdatalocalpath + "\\EncryptedNotes");
            }
            PublicKeyPath = $"{appdatalocalpath}\\EncryptedNotes\\publicKey.xml";
            PrivateKeyPath = $"{appdatalocalpath}\\EncryptedNotes\\privateKey.xml";

            if (!File.Exists(PublicKeyPath) || !File.Exists(PrivateKeyPath))
            {
                var rsa = new RSACryptoServiceProvider(2048);
                PublicKey = rsa.ToXmlString(false);
                PrivateKey = rsa.ToXmlString(true);

                SaveKey(PublicKeyPath, PublicKey);
                SaveKey(PrivateKeyPath, PrivateKey);
            }

            PublicKey = LoadKey(PublicKeyPath);
            PrivateKey = LoadKey(PrivateKeyPath);

            if (PublicKey == null || PrivateKey == null)
            {
                MessageBox.Show("Failed to load or create keys", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }


        private async void TextInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            return;
        }
        static void SaveKey(string path, string key)
        {
            File.WriteAllText(path, key);
        }

        static string LoadKey(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        static string Encrypt(string text, string publicKey)
        {
            try
            {
                var rsa = new RSACryptoServiceProvider(2048);
                rsa.FromXmlString(publicKey);
                int maxDataSize = (rsa.KeySize / 8) - 11;

                byte[] textBytes = Encoding.UTF8.GetBytes(text);
                int dataLength = textBytes.Length;
                int offset = 0;
                List<string> encryptedChunks = new List<string>();

                while (offset < dataLength)
                {
                    int chunkSize = Math.Min(maxDataSize, dataLength - offset);
                    byte[] chunk = new byte[chunkSize];
                    Array.Copy(textBytes, offset, chunk, 0, chunkSize);
                    byte[] encryptedData = rsa.Encrypt(chunk, false);
                    string encryptedChunk = Convert.ToBase64String(encryptedData);
                    encryptedChunks.Add(encryptedChunk);
                    offset += chunkSize;
                }

                return string.Join(";", encryptedChunks);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }


        static string Decrypt(string encryptedText, string privateKey)
        {
            try
            {
                var rsa = new RSACryptoServiceProvider(2048);
                rsa.FromXmlString(privateKey);

                string decryptedText = string.Empty;
                string[] encryptedChunks = encryptedText.Split(';');
                foreach (string encryptedChunk in encryptedChunks)
                {
                    if (string.IsNullOrEmpty(encryptedChunk))
                    {
                        continue;
                    }

                    byte[] encryptedData = Convert.FromBase64String(encryptedChunk);
                    byte[] data = rsa.Decrypt(encryptedData, false);
                    string chunk = Encoding.UTF8.GetString(data);

                    decryptedText += chunk;
                }
                return decryptedText;

            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to decrypt text, decryption most likely failed due to incorrect keys.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine(e.Message);
                return null;
            }
        }


        private void OpenETF_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Encrypted Text Files (*.EncryptedTXT)|*.EncryptedTXT";
            if (openFileDialog.ShowDialog() == true)
            {

                string encryptedText = File.ReadAllText(openFileDialog.FileName);
                string decryptedText = Decrypt(encryptedText, PrivateKey);
                TextInput.Text = decryptedText;
                Window.Title = $"Encrypted Notes - {System.IO.Path.GetFileName(openFileDialog.FileName)}";
                OpenFilePath = System.IO.Path.GetFullPath(openFileDialog.FileName);
            }
        }

        private void SaveETF_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Encrypted Text Files (*.EncryptedTXT)|*.EncryptedTXT";
            if (saveFileDialog.ShowDialog() == true)
            {
                string text = Convert.ToBase64String(Encoding.UTF8.GetBytes(TextInput.Text));
                string encryptedText = Encrypt(TextInput.Text, PublicKey);
                File.WriteAllText(saveFileDialog.FileName, encryptedText);
                Window.Title = $"Encrypted Notes - {System.IO.Path.GetFileName(saveFileDialog.FileName)}";
                OpenFilePath = System.IO.Path.GetFullPath(saveFileDialog.FileName);
            }
        }

        private void RegenKeys_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Regenerating keys will delete the current keys and generate new ones. In return ANY text you've encrypted will be lost. Are you sure you want to continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No)
            {
                return;
            }
            
            var rsa = new RSACryptoServiceProvider();
            PublicKey = rsa.ToXmlString(false);
            PrivateKey = rsa.ToXmlString(true);

            SaveKey(PublicKeyPath, PublicKey);
            SaveKey(PrivateKeyPath, PrivateKey);
        }
        public static RoutedCommand MyCommand = new RoutedCommand();
        private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TextInput.Clear();
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Encrypted Text Files (*.EncryptedTXT)|*.EncryptedTXT";
            if(saveFileDialog.ShowDialog() == true)
            {
                File.Create(saveFileDialog.FileName);
                Window.Title = $"Encrypted Notes - {System.IO.Path.GetFileName(saveFileDialog.FileName)}";
                OpenFilePath = System.IO.Path.GetFullPath(saveFileDialog.FileName);
            }
        }

        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenETF_Click(sender, e);
        }

        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(OpenFilePath))
            {
                SaveAsCommand_Executed(sender, e);
            }
            else
            {
                string encryptedText = Encrypt(TextInput.Text, PublicKey);
                File.WriteAllText(OpenFilePath, encryptedText);
                Window.Title = $"Encrypted Notes - {System.IO.Path.GetFileName(OpenFilePath)}";
            }
        }

        private void SaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveETF_Click(sender, e);
        }

        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

    }
}