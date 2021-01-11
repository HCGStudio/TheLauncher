using System;
using System.Reactive.Disposables;
using System.Windows;
using ReactiveUI;

namespace HCGStudio.QuickLauncher
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class LoginWindow
    {
        public LoginWindow()
        {
            InitializeComponent();
            ViewModel = new();
            this.WhenActivated(d =>
            {
                this.Bind(ViewModel,
                        vm => vm.UserName,
                        v => v.UserNameBox.Text)
                    .DisposeWith(d);
                this.Bind(ViewModel,
                        vm => vm.Visibility,
                        v => v.Visibility)
                    .DisposeWith(d);
                this.BindCommand(ViewModel,
                        vm => vm.WhenLogin,
                        v => v.LoginButton,
                        nameof(LoginButton.Click))
                    .DisposeWith(d);
            });
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
                ViewModel.Password = PasswordBox.Password;
        }

        private void LoginWindow_OnClosed(object? sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}