using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Module.HeroVirtualTabletop.Identities
{
    public class IdentitiesCollection : OptionGroups.OptionGroup<Identity>
    {
        protected override void InsertItem(int index, Identity item)
        {
            if (!this.Any((Identity id) => { return id == item; }))
            {
                base.InsertItem(index, item);
                item.PropertyChanged += Item_PropertyChanged;
            }
            else
            {
                item.IsActive = item.IsActive;
                item.IsDefault = item.IsDefault;
            }
        }

        protected override void RemoveItem(int index)
        {
            Identity beingRemoved = this[index];
            if (beingRemoved.IsDefault)
            {
                if (this.Any((Identity id) => { return id != beingRemoved; }))
                {
                    this.First((Identity id) => { return id != beingRemoved; }).IsDefault = true;
                }
                else
                {
                    System.Windows.MessageBox.Show("You can not remove all Identities from a Character");
                    return;
                }
            }
            base.RemoveItem(index);
        }

        [JsonIgnore]
        public Identity Default
        {
            get
            {
                Identity tmp = this.FirstOrDefault((Identity id) => { return id.IsDefault; });
                if (tmp == null)
                {
                    if (this.Count > 0)
                    {
                        tmp = this[0];
                    }
                    else
                    {
                        tmp = new Identity("model_Statesman", IdentityType.Model, "Base");
                        this.Add(tmp);
                    }
                    tmp.IsDefault = true;
                }
                return tmp;
            }
            set
            {
                if (!this.Contains(value))
                {
                    this.Add(value);
                }
                value.IsDefault = true;
            }
        }

        [JsonIgnore]
        public Identity Active
        {
            get
            {
                Identity tmp = this.FirstOrDefault((Identity id) => { return id.IsActive; });
                if (tmp == null)
                {
                    tmp = this.Default;
                    tmp.IsActive = true;
                }
                return tmp;
            }
            set
            {
                if (!this.Contains(value))
                {
                    this.Add(value);
                }
                value.IsActive = true;
            }
        }

        [JsonIgnore]
        public Identity NewIdentity
        {
            get
            { 
                Identity tmp = new Identity("model_Statesman", IdentityType.Model, NewValidOptionName("Identity"));
                this.Add(tmp);
                return tmp;
            }
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //Ensure there's only 1 default Identity
            if (e.PropertyName == "IsDefault" && (sender as Identity).IsDefault)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i] != (Identity)sender && this[i].IsDefault)
                    {
                        this[i].IsDefault = false;
                        break;
                    }
                }
            }
            //Ensure there's only 1 active Identity
            if (e.PropertyName == "IsActive" && (sender as Identity).IsActive)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i] != (Identity)sender && this[i].IsActive)
                    {
                        this[i].IsActive = false;
                        break;
                    }
                }
            }
        }
    }
}
