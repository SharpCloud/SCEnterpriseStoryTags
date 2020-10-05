using SCEnterpriseStoryTags.Models;
using SCEnterpriseStoryTags.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace SCEnterpriseStoryTags
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ViewModelLocator.MainViewModel.PropertyChanged += MainViewModelPropertyChanged;

            ViewModelLocator.MainViewModel.FormFieldFocusActions = new Dictionary<FormFields, Action>
            {
                [FormFields.Password] = () => this.Password.Focus(),
                [FormFields.Team] = () => this.Team.Focus(),
                [FormFields.Template] = () => this.Template.Focus(),
                [FormFields.Url] = () => this.Url.Focus(),
                [FormFields.Username] = () => this.Username.Focus()
            };

            ViewModelLocator.MainViewModel.LoadValues();
        }

        private void MainViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedSolution))
            {
                Password.Password = ViewModelLocator.PasswordService.LoadPassword(
                    ViewModelLocator.MainViewModel.SelectedSolution);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => ViewModelLocator.MainViewModel.ValidateAndRun());
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MainViewModel.SelectedSolution.Url = "https://uk.sharpcloud.com";
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MainViewModel.SelectedSolution.Url = "https://my.sharpcloud.com";
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.MainViewModel.SelectedSolution.Url = "https://eu.sharpcloud.com";
        }

        private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/SharpCloud/SCEnterpriseStoryTags");
        }

        private void PasswordChanged(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.PasswordService.SavePassword(
                Password.Password,
                ViewModelLocator.MainViewModel.SelectedSolution);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            ViewModelLocator.MainViewModel.SaveValues();
        }
    }
}
