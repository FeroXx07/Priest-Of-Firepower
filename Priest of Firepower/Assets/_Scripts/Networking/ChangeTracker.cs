using System.Collections;

namespace _Scripts.Networking
{
    public class ChangeTracker
    {
        private readonly BitArray _bitfield;

        public ChangeTracker(int numberOfFields)
        {
            // Initialize the bitfield with the specified number of fields
            _bitfield = new BitArray(numberOfFields);
        }

        public void TrackChange(int fieldIndex)
        {
            // Set the corresponding bit to 1 to indicate the field has changed
            _bitfield.Set(fieldIndex, true);
        }
        public void DeTrackChange(int fieldIndex)
        {
            // Set the corresponding bit to 1 to indicate the field has changed
            _bitfield.Set(fieldIndex, false);
        }
        public void SetAll(bool value)
        {
            _bitfield.SetAll(value);
        }

        public bool HasChanged(int fieldIndex)
        {
            // Check if the corresponding bit is set
            return _bitfield.Get(fieldIndex);
        }

        public BitArray GetBitfield()
        {
            return _bitfield;
        }
    }
}