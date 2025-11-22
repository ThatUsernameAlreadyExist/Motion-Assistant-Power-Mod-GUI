// ViewModels/ComboBoxItemModel.cs
using System;

namespace Windows11Settings.ViewModels
{
    public class ComboBoxItemModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Id { get; set; } = string.Empty;

        public override bool Equals(object obj)
        {
            if (obj != null && obj is ComboBoxItemModel other)
            {
                return Id == other.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public ComboBoxItemModel Clone()
        {
            return new ComboBoxItemModel
            {
                DisplayName = this.DisplayName,
                Value = this.Value,
                Id = this.Id
            };
        }
    }
}
