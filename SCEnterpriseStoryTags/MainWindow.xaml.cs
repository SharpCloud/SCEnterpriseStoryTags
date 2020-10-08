using SCEnterpriseStoryTags.Models;
using SCEnterpriseStoryTags.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

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
            ViewModelLocator.MessageService.Owner = this;
            ViewModelLocator.MainViewModel.PropertyChanged += MainViewModelPropertyChanged;

            ViewModelLocator.MainViewModel.FormFieldFocusActions = new Dictionary<FormFields, Action>
            {
                [FormFields.Password] = CreateFocusControlAction(Password),
                [FormFields.Team] = CreateFocusControlAction(Team),
                [FormFields.Template] = CreateFocusControlAction(TemplateId),
                [FormFields.Url] = CreateFocusControlAction(Url),
                [FormFields.Username] = CreateFocusControlAction(Username)
            };

            ViewModelLocator.MainViewModel.LoadValues();
        }

        private Action CreateFocusControlAction(Control control)
        {
            return () => Application.Current.Dispatcher?.Invoke(control.Focus);
        }

        private void MainViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedSolution))
            {
                Password.Password = ViewModelLocator.PasswordService.LoadPassword(
                    ViewModelLocator.MainViewModel.SelectedSolution);
            }
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
