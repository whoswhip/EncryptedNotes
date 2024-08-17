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
        public MainWindow()
        {
            string publicKeyPath = "publicKey.xml";
            string privateKeyPath = "privateKey.xml";

            if (!File.Exists(publicKeyPath) || !File.Exists(privateKeyPath))
            {
                var rsa = new RSACryptoServiceProvider();
                PublicKey = rsa.ToXmlString(false);
                PrivateKey = rsa.ToXmlString(true);

                SaveKey(publicKeyPath, PublicKey);
                SaveKey(privateKeyPath, PrivateKey);
            }

            PublicKey = LoadKey(publicKeyPath);
            PrivateKey = LoadKey(privateKeyPath);

            if (PublicKey == null || PrivateKey == null)
            {
                MessageBox.Show("Error", "Failed to load or create keys", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            InitializeComponent();
        }

        

        private async void TextInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = TextInput.Text;
            if(string.IsNullOrEmpty(text) || string.IsNullOrEmpty(PublicKey))
            {
                return;
            }
            string encryptedText = await Task.Run(() => Encrypt(text, PublicKey));
            if(encryptedText == null)
            {
                return;
            }
            File.WriteAllTextAsync("encryptedText.txt", encryptedText);

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
                Debug.WriteLine(text);
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
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKey);
            var encryptedData = Convert.FromBase64String(encryptedText);
            var data = rsa.Decrypt(encryptedData, false);
            return Encoding.UTF8.GetString(data);
        }

        private void OpenETF_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Encrypted Text Files (*.txt)|*.txt";
            if(openFileDialog.ShowDialog() == true)
            {
                string encryptedText = File.ReadAllText(openFileDialog.FileName);
                string decryptedText = Decrypt(encryptedText, PrivateKey);
                TextInput.Text = decryptedText;
            }
        }
    }
}