using System;
using System.Collections.Generic;
using System.Linq;
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

namespace MailboxWatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string SavePath = ".\\photos";
        private MailboxChecker checker;

        public MainWindow()
        {
            InitializeComponent();

            Directory.CreateDirectory(SavePath);
        }

        public bool Running
        {
            get { return (bool)GetValue(RunningProperty); }
            set { SetValue(RunningProperty, value); }
        }

        public static readonly DependencyProperty RunningProperty =
            DependencyProperty.Register("Running", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(false));

        public string Server
        {
            get { return (string)GetValue(ServerProperty); }
            set { SetValue(ServerProperty, value); }
        }

        public static readonly DependencyProperty ServerProperty =
            DependencyProperty.Register("Server", typeof(string), typeof(MainWindow),
            new UIPropertyMetadata(Properties.Settings.Default.Server, 
                (d, e) => { Properties.Settings.Default.Server = (string)e.NewValue; Properties.Settings.Default.Save(); }));

        public string Username
        {
            get { return (string)GetValue(UsernameProperty); }
            set { SetValue(UsernameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Username.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UsernameProperty =
            DependencyProperty.Register("Username", typeof(string), typeof(MainWindow),
            new UIPropertyMetadata(Properties.Settings.Default.Username, 
                (d, e) => { Properties.Settings.Default.Username = (string)e.NewValue; Properties.Settings.Default.Save(); }));


        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register("Password", typeof(string), typeof(MainWindow),
            new UIPropertyMetadata(Properties.Settings.Default.Password, 
                (d, e) => { Properties.Settings.Default.Password = (string)e.NewValue; Properties.Settings.Default.Save(); }));


        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                checker = new MailboxChecker(SavePath, Server, Username, Password);
                Running = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not connect to server.\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            if (checker != null) checker.Dispose();
            Running = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (checker != null) checker.Dispose();
        }
    }
}
