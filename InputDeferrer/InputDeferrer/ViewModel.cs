using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InputDeferrer
{
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _textA;
        private string _textB;

        public ViewModel()
        {
            _textA = "aaa";
            _textB = "bbb";
        }

        public string TextA
        {
            get { return _textA; }
            set
            {
                _textA = value;
                NotifyOfPropertyChanged();
            }
        }


        public string TextB
        {
            get { return _textB; }
            set
            {
                _textB = value; 
                NotifyOfPropertyChanged();
            }
        }


        protected virtual void NotifyOfPropertyChanged([CallerMemberName] string propertyName = null) 
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
