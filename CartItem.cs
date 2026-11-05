using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace mur
{
    public class CartItem : INotifyPropertyChanged
    {
        private int quantity;

        public int ProductId { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }

        public int Quantity
        {
            get { return quantity; }
            set
            {
                quantity = value;
                OnPropertyChanged();
                OnPropertyChanged("Total");
            }
        }

        public decimal Total
        {
            get { return Price * Quantity; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
        }
    }
}