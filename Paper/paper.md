---
title: "ur-scape: an interactive game-engine based mapping platform for urban planning and design"
tags:
  - Unity
  - C#
  - Python
  - Mapbox
  - QGIS
authors:
  - name:
    orcid:
    affiliation:
affiliations:
  - name:
    index:
date: 12 April 2022
bibliography: paper.bib
---

# Summary

The benefits of maps and spatial data have long been recognized and appreciated. Particularly in urban planning and landscape design, the process of rendering, overlaying, and analysing spatial data is intuitively appealing among researchers, planners, and policy makers. As more datasets and models become available, the main obstacle in creating a data-driven workflow is less likely to be caused by data but how data handlers communicate and present information to a broad-based audience across multiple disciplines.
ur-scape is designed with features and functions to promote data-evident planning, particularly within the South East Asia context. Specifically, the platform assists three aspects of the decision-making process, namely, interaction, transparency, and organisation. Ur-scape’s interface enables decision-makers to have more direct first-hand interactions with data and analytical processes without intensive training. This workflow therefore also increases the transparency and provides data evidence to support and communicate decisions with stakeholders and the general public. Lastly, although ur-scape is fundamentally a mapping platform, its workflow and structure also encourages clean and logical data organisation and management, both of which based on our experience have been a challenging task for many municipalities in South East Asia, partially due to its fast and dynamic urbanisation environments. To date, several design workshops and charrettes have been successfully hosted with the support of ur-scape around South East Asia. [ref @niraly]

# Statement of need

# urscape architecture

ur-scape is a support tool, developed using the Unity game engine, for planning and design of rapidly developing towns, cities and regions. Unity provides tools that support real-time rendering and is compatible with a variety of platforms including Windows, MacOS and Web. Figure 2 illustrates the three stages of a typical ur-scape workflow: Preparation, Data, App. In the Preparation stage, users interact with Mapbox to prepare the custom base maps they wish to render in the application. In addition, in the current ur-scape version, users rely on QGIS to prepare and export GIS data (i.e. vectors and raster data) to a .csv format that the application can interpret. Metadata input is a required step when exporting from QGIS but users can also edit it directly in ur-scape afterward. In the Data stage, data from the previous stage (i.e. csv files) are placed in ur-scape’s Data folder along with other data files (i.e. in-built tools and application configuration data files) used for the application’s interpretation. This leads to the App stage whereby the application interprets all the data files and renders an interactive map and a list of sites and data layers that users can toggle and interact with. Users can then make use of the in-built tools to interact with and analyse the GIS data.

# Figures

![ur-scape Workflow](1.png)

![ur-scape Overall Architecture](2.png)

# Acknowledgements

# References
