# Sitefinity Multistore

Sitefinity Multistore is an project used to demonstrate how to implement multiple stores with Sitefinity e-commerce. The scenario is a multinational company which sells generally same products within different geographic regions. While the list of products and products details are almost the same, it is possible that some products are not offered in all the regions. Furthermore, products may have different descriptions or prices within different regions.

You can see the recorded video of the finished solution here: [http://www.screencast.com/t/aX5KlQkukXOc] (http://www.screencast.com/t/aX5KlQkukXOc)

## Prerequisites

* Sitefinity 7.3 or higher
* E-commerce add-on
* Multisite add-on
* Workflow add-on

## Project requirements

* Each region should be able to have a unique product catalog
* It should be possible to centrally add or remove products to or from all regions
* Editing a product in a regional catalog should not affect any other catalogs
* Prices of products are determined manually, not through a currency conversion
* It should be possible to mass upload products to all regions


# Solution

### Setting up the providers and sites

The approach we will take is to have each country be represented by one site in Sitefinity multisite instance. Furthermore, for each site in the multisite instance we will create a different logical Product catalog provider (logical meaning that we will keep all the data in one phyisical database, but separated logically in different applications). 

**Step 1**

Create a product catalog for each geographic region you plan to support. You can do this through user interface or programmatically if you are planning to support a large number of countries.

E-commerce add-on in Sitefinity is made up of several modules, however, we are only insterested in the Catalog module. To that end, we will create logical data providers for Catalog module for each region.

* Log in to your Sitefinity instance and go to backend
* In the menu, click on **Administration** and then **Settings**
* Click on **Advanced**
* Expand in the tree **Catalog** > **Providers**
* Click on the **Providers** and then on the right click on the **Create new**
* In this example, we'll create a provider for Italy. Fill the form as follows
  * **Name**: ItalyCatalogProvider
  * **Title**: Italy catalog
  * **Description**: The catalog provider for Italy
  * **GlobalResourceClassId**: leave empty
  * **Provider type**: Telerik.Sitefinity.Modules.Ecommerce.Catalog.Data.OpenAccessCatalogDataProvider, Telerik.Sitefinity.Ecommerce
  * **Enabled**: Checked
 * Click **Save changes**
 * In the tree on the left, locate the new provider you have created called **ItalyCatalogProvider** and click on it
 * Click on the **Paramters**, the child node of the **ItalyCatalogProvider**
 * On the right click on the **Create new** button
 * Enter the following information in the form:
  * **Key**: applicationName
  * **Value**: /Italy

![](https://github.com/osmak/sitefinity-multistore/blob/master/docs/multistore_country_provider.png)

Repeat this procedure for each region you want to support. Note that **Provider type** field will always be the same, regardless of the region.

**Recycle the application pool of your application so that the provider changes take effect. You can do this by making any modification in the web.config of your site and then reloading Sitefinity or recyclying the website / application pool through IIS**

Note: the "applicationName" parameter we added to the provider is the discriminator key that will be used in the database table to distinguish products belonging to different providers. This is necessary as our data providers are logical and stored in the same database tables.

**Step 2**

Create a new site for each region that you will support.

* Navigate back to Sitefinity dashboard 
* In the upper left corner click on the multisite selector
* In the menu, click on the **Manage sites**
* Click on the **Create a site** button
* Give your site a name and set it's domain. For example **Italy** and **italy.multistore.com**
* Click on the **Continue** button
* The **Configure modules for Italy** screen appears.
* Check the **Ecommerce Products** checkbox 
* Click on **Change** link in the row **Ecommerce Products** that has just appeared
* From the selector select **Italy catalog** that we have created in the Step 1 (Warning! Sitefinity will offer you a default provider called **Italy Ecommerce Products** - make sure you don't choose that one)
* Click on **Done**
* Click on **Create this site** button

Repeat this procedure for each region you want to support.

At this point, you can create your pages on various region sites. The widgets that consume products data will be using the logical providers we have created in Step 1 and configured in Step 2.

## Master catalog

With the first requirement out of our way, we can not concentrate on the second one. Namely, it should be possible to centrally add or remove products to all the regional catalogs. We will do this by using the default provider that is created when Sitefinity is installed. Alternatively, we could repeat Step 1 and Step 2, but instead of a region, such as Italy we could call it "MasterCatalog" or something like that.

We will then implement simple code that will register to the **Created** and **Deleted** events of the Catalog manager. In the event handler we will handle only the products being created and deleted in our default provider - or better to say our master catalog. When a new product is created, we will create that product accross all the logical providers for regions, and respectively when a product is deleted we will delete it accross all the regional providers.

To add a level of transparency, we will also turn Approval Workflow on the Products, so that new products don't just magically appear on the regional sites.

**Step 1**

We will start by creating a new role for the regional store managers. People in this role will have the ability to approve the changes to their regional catalog. Ideally, we should also create a role for each region, such as "Italy" - so that we can limit the access only to regional sites. Then, from all people belonging to the role "Italy", one or more would also belong to the role "Store manager" - which would be the people that are moderating the regional catalog for Italy.

* Navigate to Sitefinity **Dashboard**
* In the menu click on **Administration** and then click on the **Roles**
* Click on the **Create a role** button
* Name the role **Store manager** and click on the **Create** button

**Step 2**

We will now define the workflow, which will require the approval of a Store manager for a product to appear on the regional catalog.

* Navigate to Sitefinity **Dashboard**
* In the menu click on **Administration** and then click on the **Workflows** from the menu
* Click on the **Define Workflow**
* Under the **Workflow type** select the first option **Approval before publishing**
* Click on **Continue**
* Name the workflow **Catalog approval**
* Click on the **Add roles or users** button, under the **Set approvers** section
* Check the **Store managers** role in **Select roles or users** dialog
* Click **Done selecting**
* Check the **Notify users by email when an item is waiting for their approval** checkbox
* Under the **Scope** section, select select **Selected only** and check all the product types that you want to participate in the workflow (probably, all the product types)
* Makes sure that the **Allow administrators to skip the workflow** and **This workflow is active** are checked
* Click on the **Save workflow** button

**Step 3**

In order to be able to centrally manage products, we will need to establish some sort of a relation between the products in the master catalog and products in regional catalog. For that purpose we will add a new field to all product types that we plan to sell. We will call that field MasterId and it'll be of type Guid.

* Navigate to Sitefinity **Dashboard**
* In the menu click on **Ecommerce** and then click on the **Products** from the menu.
* In case you don't have any products, click on the Create a... **General product** and create a dummy product that we will delete later
* Once you've created the product the list of Products shows. In the menu on the right, click on the **Manage product types**
* Click on the **Actions** in the **General product** grid and in the menu click on the Edit... **Fields**
* Click on the **Add a field...** button
* For the type choose **Short text**
* For the name type **MasterId**
* Check the **This is a hidden field** checkbox
* Click on the **Continue**
* Click on the **Save changes** button

**Step 4**

Now, that we have set up the system, it is time to implement the code that will sync newly created products from master catalog into the regional catalogs and that would delete products from the regional catalogs when the product is deleted in the master catalog.

To achieve this, we will use Sitefinity eventing system. We will place all the code in the global.asax file, even though in the production you would probably put such code in a separate assembly.

Here are the steps that need to be performed.

* Open your Sitefinity website in Visual Studio
* Add a Global.asax file to your SitefinityWebApp project
* Make the Global.asax file look like this:

```CSharp

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

```

The code above does following on a high level.

* When the application starts (Application_Start) we subscribe to the Initialized event of Sitefinity Bootstrapper. That even signals us that we can start working with Sitefinity APIs.
* Once Sitefinity is initialized we subscribe to the **IDataEvent** event. That event will be fired on any CRUD operation on any entity that implements **IDataItem** interface. Every persistent type in Sitefinity does implement this interface, including all product catalog types - which we are interested in
* Next, we will implement logic that will decide should we process the event. Basically, we want to process only Create and Delete events that happen on products in the master catalog
* If the new product is created in the master catalog, we copy it to all the regional catalogs
* If the product is deleted from the master catalog, we delete it from all the regional catalogs

And that is it.

**Step 5**

We have covered all the requirements of our project except the mass upload. Better to say is, however, that we did not explain it.

Mass upload implementation will be determined by the source from which we want to import products. It could be a database, CSV file... so we want go into the details here.

That being said, as we've implemented the syncing mechanism in the Step 4, all we need to do is mass create products to our master catalog and syncing will be taken care for us. To create products programmatically in Sitefinity, you can use classic .NET API as demonstrated in the **CreateProductAccrossRegions** or web services.

```CSharp
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
```


