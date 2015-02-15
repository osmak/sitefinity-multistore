# Sitefinity Multistore

Sitefinity Multistore is an project used to demonstrate how to implement multiple stores with Sitefinity e-commerce. The scenario is a multinational company which sells generally same products within different geographic regions. While the list of products and products details are almost the same, it is possible that some products are not offered in all the regions. Furthermore, products may have different descriptions or prices within different regions.

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



