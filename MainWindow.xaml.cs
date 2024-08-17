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
        public MainWindow()
        {
            InitializeEncryption();
            InitializeComponent();
        }

        private async void InitializeEncryption()
        {
            string appdatalocalpath = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if(!Directory.Exists(appdatalocalpath + "\\EncryptedNotes"))
            {
                Directory.CreateDirectory(appdatalocalpath + "\\EncryptedNotes");
            }            
            PublicKeyPath = $"{appdatalocalpath}\\EncryptedNotes\\publicKey.xml";
            PrivateKeyPath = $"{appdatalocalpath}\\EncryptedNotes\\privateKey.xml";

            if (!File.Exists(PublicKeyPath) || !File.Exists(PrivateKeyPath))
            {
                var rsa = new RSACryptoServiceProvider();
                PublicKey = rsa.ToXmlString(false);
                PrivateKey = rsa.ToXmlString(true);

                SaveKey(PublicKeyPath, PublicKey);
                SaveKey(PrivateKeyPath, PrivateKey);
            }

            PublicKey = LoadKey(PublicKeyPath);
            PrivateKey = LoadKey(PrivateKeyPath);

            if (PublicKey == null || PrivateKey == null)
            {
                MessageBox.Show("Error", "Failed to load or create keys", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(publicKey);
                var data = Encoding.UTF8.GetBytes(text);
                var encryptedData = rsa.Encrypt(data, false);
                return Convert.ToBase64String(encryptedData);
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }

        static string Decrypt(string encryptedText, string privateKey)
        {
            try
            {
                var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(privateKey);
                var encryptedData = Convert.FromBase64String(encryptedText);
                var data = rsa.Decrypt(encryptedData, false);
                return Encoding.UTF8.GetString(data);
            }
            catch(Exception e)
            {
                MessageBox.Show("Error", "Failed to decrypt text, decryption most likely failed due to incorrect keys.", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine(e.Message);
                return null;
            }

        }

        private void OpenETF_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Encrypted Text Files (*.EncryptedTXT)|*.EncryptedTXT";
            if(openFileDialog.ShowDialog() == true)
            {
                string encryptedText = File.ReadAllText(openFileDialog.FileName);
                string decryptedText = Decrypt(encryptedText, PrivateKey);
                TextInput.Text = decryptedText;
            }
        }

        private void SaveETF_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Encrypted Text Files (*.EncryptedTXT)|*.EncryptedTXT";
            if(saveFileDialog.ShowDialog() == true)
            {
                string encryptedText = Encrypt(TextInput.Text, PublicKey);
                File.WriteAllText(saveFileDialog.FileName, encryptedText);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var rsa = new RSACryptoServiceProvider();
            PublicKey = rsa.ToXmlString(false);
            PrivateKey = rsa.ToXmlString(true);

            SaveKey(PublicKeyPath, PublicKey);
            SaveKey(PrivateKeyPath, PrivateKey);
        }
    }
}