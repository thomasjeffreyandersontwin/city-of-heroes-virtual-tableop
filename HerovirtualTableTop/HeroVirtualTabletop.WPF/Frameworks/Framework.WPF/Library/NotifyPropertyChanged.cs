using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Framework.WPF.Library
{
    /// <summary>   Notify property changed. </summary>
    [Serializable]
    public abstract class NotifyPropertyChanged : INotifyPropertyChanged
    {
        /// <summary> Event queue for all listeners interested in PropertyChanged events. </summary>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>   Notify any listeners that a property on this object has been changed. </summary>
        /// <param name="propertyName"> Name of the property. </param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>   Notify property changed. </summary>
    [Serializable]
    public abstract class TrackChangesAndNotifyPropertyChanged : INotifyPropertyChanged
    {

        public bool IsDirty { get; private set; }

        public bool IsChangeTrackingEnabled { get; private set; }

        public void StartChangeTracking()
        {
            this.IsChangeTrackingEnabled = true;
        }

        public void StopChangeTracking()
        {
            this.IsChangeTrackingEnabled = false;
        }

        public void ResetChangeTracking()
        {
            this.IsChangeTrackingEnabled = false;
            this.SetDirty(false);
        }

        public virtual void SetDirty(bool value)
        {
            this.IsDirty = value;
        }

        /// <summary> Event queue for all listeners interested in PropertyChanged events. </summary>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>   Notify any listeners that a property on this object has been changed. </summary>
        /// <param name="propertyName"> Name of the property. </param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (this.IsChangeTrackingEnabled)
            {
                Type t = this.GetType();
                PropertyInfo pi = t.GetProperty(propertyName);
                ChangeTrackingNeededAttribute[] attr = pi.GetCustomAttributes(typeof(ChangeTrackingNeededAttribute), false) as ChangeTrackingNeededAttribute[];
                bool IsChangeTrackingNeeded = attr.Length > 0;
                if (IsChangeTrackingNeeded)
                    this.SetDirty(true);
            }

            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
