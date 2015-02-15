# Sitefinity Multistore

Sitefinity Multistore is an project used to demonstrate how to implement multiple stores with Sitefinity e-commerce. The scenario is a multinational company which sells generally same products within different geographic regions. While the list of products and products details are almost the same, it is possible that some products are not offered in all the regions. Furthermore, products may have different descriptions or prices within different regions.

## Prerequisites

* Sitefinity 7.3 or higher
* E-commerce add-on
* Multisite add-on

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

![](https://github.com/osmak/sitefinity-multistore/blob/master/docs/multistore_country_provider.png)

Repeat this procedure for each region you want to support. Note that **Provider type** field will always be the same, regardless of the region. 
