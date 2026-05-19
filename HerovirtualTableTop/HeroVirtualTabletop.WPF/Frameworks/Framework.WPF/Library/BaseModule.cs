using Prism.Modularity;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.WPF.Library
{
    public abstract class BaseModule: IModule
    {
        // view of the module
        protected abstract object ModuleView { get; }

        // region for the module 
        /* assuming this is a single active region (Content Control),
         * so at most one view can be active in the region */
        protected abstract IRegion ModuleRegion { get; }

        #region IModule Members
        public abstract void Initialize();
        #endregion

        /// <summary>
        /// Deactivate other views than the current view in the region
        /// </summary>
        protected void DeactivateOtherViews()
        {
            object currentActiveView = ModuleRegion.ActiveViews.FirstOrDefault();
            if (currentActiveView != null && currentActiveView != ModuleView && ModuleRegion.Views.Contains(currentActiveView))
            {
                ModuleRegion.Deactivate(currentActiveView);
            }
        }

        /// <summary>
        /// Activate the view of this module
        /// </summary>
        public void ActivateModule()
        {
            DeactivateOtherViews();

            ModuleRegion.Activate(ModuleView);
        }
    }
}
