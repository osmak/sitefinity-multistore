using System;
using System.Collections.Generic;
using System.Linq;
using Telerik.Sitefinity.Abstractions;
using Telerik.Sitefinity.Configuration;
using Telerik.Sitefinity.Data;
using Telerik.Sitefinity.Data.Events;
using Telerik.Sitefinity.Ecommerce.Catalog.Model;
using Telerik.Sitefinity.GenericContent.Model;
using Telerik.Sitefinity.Modules.Ecommerce.Catalog;
using Telerik.Sitefinity.Modules.Ecommerce.Catalog.Configuration;
using Telerik.Sitefinity.Services;
using Telerik.Sitefinity.Model;

namespace SitefinityWebApp
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            // Subscribe to the Sitefinity Bootstrapper Initialized event, which is
            // fired once Sitefinity is up and running.
            Bootstrapper.Initialized += Bootstrapper_Initialized;
        }

        /// <summary>
        /// Handles the Initialized event of Sitefinity's Bootstrapper class. When this
        /// even is fired it means Sitefinity has performed all the work needed to get
        /// it up and running and we can start working with Sitefinity's APIs.
        /// </summary>
        /// <param name="sender">
        /// The object which fired the event.
        /// </param>
        /// <param name="e">
        /// The event arguments passed by the event.
        /// </param>
        void Bootstrapper_Initialized(object sender, ExecutedEventArgs e)
        {
            // subscribe to the data event, which is fired for CRUD operations
            // on any object that implements IDataItem interface. Product entity
            // implements IDataItem interface
            EventHub.Subscribe<IDataEvent>(this.HandleDataEvent);
        }

        /// <summary>
        /// Event handler for the IDataEvent which Sitefinity fires
        /// any time a CRUD operation is performed on any entity
        /// that implements <see cref="IDataEvent"/>.
        /// </summary>
        /// <param name="evt">
        /// The instance of the <see cref="IDataEvent"/> which provides
        /// the information about the raised event.
        /// </param>
        private void HandleDataEvent(IDataEvent evt)
        {
            if(this.ShouldProcess(evt))
            {
                if (evt.Action == DataEventAction.Created)
                    this.CreateProductAccrossRegions(evt.ItemId, evt.ItemType);
                else if(evt.Action == DataEventAction.Deleted)
                    this.DeleteProductAccrossRegions(evt.ItemId, evt.ItemType);
            }
        }
        
        /// <summary>
        /// Determines should the data event be processed. There are two
        /// conditions that an event must meet in order to be processed:
        /// * The ItemType for which event was raised must be of type
        ///     <see cref="Product"/> or one of its derivatives
        /// * The Item being processed should be the live version of the product. 
        ///     As Sitefinity supports life-cycle of data items, several versions,
        ///     such as draft, live, master, temp... may be created. We want to
        ///     create items only once, however.
        /// * The ProviderName from which event was fired should be the default
        ///     provider of the catalog module, which we are using as the master
        ///     catalog. Changes on the regional providers should be isolated
        ///     and hence we are not interested in them
        /// * The action for which event was fired must be Created or Deleted
        ///     as we are not interested in updates or custom actions
        /// </summary>
        /// <param name="evt">
        /// The instance of the <see cref="IDataEvent"/> type which represents
        /// the event to be examined.
        /// </param>
        /// <returns>
        /// True if all conditions were met and event should be processed; otherwise
        /// false.
        /// </returns>
        private bool ShouldProcess(IDataEvent evt)
        {
            if (!typeof(Product).IsAssignableFrom(evt.ItemType))
                return false;

            var lifecycleEvt = evt as ILifecycleEvent;
            if (lifecycleEvt != null && lifecycleEvt.Status != ContentLifecycleStatus.Live.ToString())
                return false;

            if (evt.ProviderName != CatalogManager.GetDefaultProviderName())
                return false;

            if (!(evt.Action == DataEventAction.Created || evt.Action == DataEventAction.Deleted))
                return false;

            return true;
        }

        /// <summary>
        /// Gets a list of all Ecommerce catalog providers except
        /// the default one, which we use as the master catalog
        /// provider.
        /// </summary>
        /// <returns>
        /// The list of strings representing the names of all the
        /// catalog providers, except the default one.
        /// </returns>
        private List<string> GetRegionalCatalogProviders()
        {
            return Config.Get<CatalogConfig>()
                         .Providers
                         .Keys
                         .Where(k => !k.Equals(CatalogManager.GetDefaultProviderName()))
                         .ToList();
        }

        /// <summary>
        /// Creates a product in all the regional providers based on the
        /// product in the master catalog as defined by the master id and
        /// master type parameters.
        /// </summary>
        /// <param name="masterId">
        /// Id of the product in the master catalog that is to be replicated.
        /// </param>
        /// <param name="masterType">
        /// The type of the product in the master catalog that is to be replicated.
        /// </param>
        private void CreateProductAccrossRegions(Guid masterId, Type masterType)
        {
            var transactionName = "CreateProductsRegional";
            var regionalProviders = this.GetRegionalCatalogProviders();
            var masterProduct = this.GetMasterProduct(masterId, masterType);

            foreach(var regionalProvider in regionalProviders)
            {
                var manager = CatalogManager.GetManager(regionalProvider, transactionName);
                
                // make sure the master item wasn't already synced for some reason
                if(manager.GetProducts(masterType.FullName).Any(p => p.GetValue<string>("MasterId") == masterId.ToString()))
                    continue;
                
                var regionalItem = manager.CreateItem(masterType) as Product;
                // associate the product in the regional catalog with the one
                // in the master catalog
                regionalItem.SetValue("MasterId", masterId.ToString());

                // copy logic; incomplete, modify as necessary
                regionalItem.Title = masterProduct.Title;
                regionalItem.Price = masterProduct.Price;
                regionalItem.Weight = masterProduct.Weight;
                regionalItem.UrlName = masterProduct.UrlName;

                // ensure the URLs of the new product are correctly set up
                manager.Provider.RecompileItemUrls(regionalItem);
            }

            TransactionManager.CommitTransaction(transactionName);
        }

        /// <summary>
        /// Deletes all the products that correspond to the product in the master catalog
        /// as defined by the master id and master type.
        /// </summary>
        /// <param name="masterId">
        /// The id of the product in the master catalog that is being deleted.
        /// </param>
        /// <param name="masterType">
        /// The type of the product in the master catalog that is being deleted.
        /// </param>
        private void DeleteProductAccrossRegions(Guid masterId, Type masterType)
        {
            var transactionName = "DeleteProductsRegional";
            var regionalProviders = this.GetRegionalCatalogProviders();

            foreach (var regionalProvider in regionalProviders)
            {
                var manager = CatalogManager.GetManager(regionalProvider, transactionName);
                var regionalItems = manager.GetProducts(masterType.FullName)
                                           .Where(p => p.GetValue<string>("MasterId") == masterId.ToString());

                foreach(var regionalItem in regionalItems)
                {
                    manager.DeleteItem(regionalItem);
                }

            }

            TransactionManager.CommitTransaction(transactionName);
        }

        /// <summary>
        /// Retrieves the instance that represents the product in the master catalog.
        /// </summary>
        /// <param name="masterId">
        /// The id of the product to be retrieved.
        /// </param>
        /// <param name="masterType">
        /// The type of the product to retrieve; keep in mind Sitefinity supports
        /// multiple product types, but they all inherit from the <see cref="Product"/>
        /// type.
        /// </param>
        /// <returns>
        /// The instance of the <see cref="Product"/> which represents the product in the
        /// master catalog.
        /// </returns>
        private Product GetMasterProduct(Guid masterId, Type masterType)
        {
            var masterManager = CatalogManager.GetManager();
            return masterManager.GetItem(masterType, masterId) as Product;
        }

    }
}