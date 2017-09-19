using System;

namespace Wia
{
    public class ScanCompletedEventArgs : EventArgs
    {
        public byte[] Image { get; private set; }
        public ScanCompletedEventArgs(byte[] imageData)
        {
            this.Image = imageData;
        }
    }
}
