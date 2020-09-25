using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SCEnterpriseStoryTags.Models
{
    public class EnterpriseSolution : INotifyPropertyChanged
    {
        private bool _isDirectory;
        private string _name;
        private string _team;
        private string _template;
        private string _url;
        private string _username;

        public bool IsDirectory
        {
            get => _isDirectory;

            set
            {
                if (_isDirectory != value)
                {
                    _isDirectory = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Name
        {
            get => _name;

            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Team
        {
            get => _team;

            set
            {
                if (_team != value)
                {
                    _team = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Template
        {
            get => _template;

            set
            {
                if (_template != value)
                {
                    _template = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Url
        {
            get => _url;

            set
            {
                if (_url != value)
                {
                    _url = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Username
        {
            get => _username;

            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
