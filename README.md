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
