# Power-BI-Helper

This project demonstrates how to use Azure Functions to handle HTTP requests which wrap the Power BI Embedded APIs for creating Workspaces and importing a PBIX file in the Azure Portal.

The **create-workspace** and **import-report** folders are needed for their respective Azure Functions.  The **core** folder contains the shared objects and functions used by the two Azure Functions.  It does not contain published Azure Function. 