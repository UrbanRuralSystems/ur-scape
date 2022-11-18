---
title: 'ur-scape: harnessing data for stakeholder participation in city-making processes'
tags:
  - Unity
  - C#
  - Python
  - Mapbox
  - QGIS
authors:
  - name: David Neudecker
    affiliation: 1
  - name: Michael Joos
    affiliation: 1
  - name: Muhammad Salihin Bin Zaol-kefli
    affiliation: 1
  - name: Yuhao Lu
    affiliation: 1
  - name: Niraly Mangal
    affiliation: 1
  - name: Stephen Cairns
    affiliation: 1
affiliations:
  - name: Future Cities Laboratory Global, Singapore-ETH Centre, Singapore
    index: 1
date: 4 July 2022
bibliography: paper.bib 
---

# Summary

The growing technical sophistication of city planning software [@Bettencourt:2010,@Batty:2013] has produced increasingly specialised workflows that tend to alienate, rather than engage stakeholders [@Batty:2021,@Barns:2019,@Yap:2022]. This contradicts the basic purpose of such software -- or Planning Support Systems (PSS) -- to improve the sustainability and long-term resilience of cities by harnessing increasing volumes of digital data to encourage greater stakeholder participation in city-making processes [@Maliene:2011,@Flacke:2020,@Geertman:2009,@Lee:2014,@Zwick:2010,@Haddad:2012,@vonRichthofen:2022]. 

The disconnect of data from stakeholders is especially debilitating for rapidly urbanising regions in Asia and Africa. Cities in these regions are growing at unprecedented rates [@UN:2018] and typically mix both formal and informal, urban and rural land-uses. The uneven, dynamic and ambiguous settlement patterns and data landscape that result are not always readily described with definitive, boundary-oriented and line-based graphical conventions. The policy makers, developers, investors, civil society actors, academics and the general public urgently need access to reliable data in appropriate formats to support the planning of their cities and regions. 

ur-scape aims to offer solutions as part of the dataful way of city-making and city-managing. Specifically, ur-scape is an open-source planning support software designed to respond to the issues of rapid urbanisation in Asia and Africa. Supported by Unity game-engine, ur-scape aims to improve stakeholder access to data through an innovative data rendering format and an intuitive UI/UX concept.

# Statement of need

The data rendering format prioritises (raster-based) continuous grid-based data fields. We call this techniques ‘gridded Venn diagrams’ (\autoref{fig: 1}). ur-scape displays gridded data with an additive colour-mixed Venn diagrammatic representation at each centroid. Each coloured circle in the Venn diagram carries data from a given dataset, be it population density, poverty, night-light, or CO2 emissions. As each centroid can accommodate a theoretically infinite number of colour-mixed circles arrayed around it, this rendering format facilitates layering and viewing multiple datasets at once. By avoiding the cumbersome data layering techniques typical of current PSS (through manipulations of opacity and transparency levels), this format encourages rapid and simple analyses of diverse kinds of data.

![Illustrations of gridded Venn diagram concept at various scales.\label{fig: 1}](Figure_1.png)

This approach is combined with an intuitive UI/UX concept based on simple and well-known gesture-based interaction formats – touch, pinch, zoom. The interaction concept is supported by a range of intuitive and "on-the-fly" analytical tools -- such as contouring of multiple geospatial dataset intersections, transects and dissects -- that make the most of the gridded Venn diagram approach in combination with multiple base maps (satellite, cadastral, traffic, and OSM). Utilising Unity’s processing and rendering capability, ur-scape is able to dynamically adjust image resolution based on the zoom level (i.e. extent of the dataset) to provide a fluent and seamless user experience even with large global datasets. Detailed descriptions and how-to instructions can be found in our website: https://ur-scape.sec.sg/en/Tutorials

This gives ur-scape a number of advantages over conventional planning support software:

1) Integrates data management. Metadata input is mandatory and built-in in to ur-scape's import procedure which ensures that users develop the habit of initiating and maintaining accurate metadata. This is particularly needed in the rapidly urbanising regions in Asia and Africa.

2) Supports problem-setting and problem-solving. The inclusion of data of varying quality is often more effective is shaping agendas than high object- and boundary-precision data [@Pelzer:2013]. This approach supports users to shape and prioritise problem statements based on the dynamic geographical, institutional and data contexts in which they are working.

3) Offers quick access for non-expert. The intuitive UI/UX does not require users to undergo intensive training, so facilitates almost immediate interaction between expert and non-expert around real-time spatial data (\autoref{fig: 2}).

4) Enhances iterative experimentation with best available data. ur-scape encourages users to make best use of uneven data quality by enabling rapid and repeated trialling of diverse data sets, including the use of data proxies where bespoke data is not available.

![A conceptual comparison between conventional work and ur-scape workflow involving data and disciplinary-specific experts.\label{fig: 2}](Figure_2.png)


# Use cases

ur-scape was developed in collaboration with a number of government agencies and multilateral institutions in East, South and Southeast Asia which were addressing a range of practical challenges concerning rapid urbanisation. For example, ur-scape was deployed in a number of projects such as gender equality and female empowerment in Bandung, Indonesia [@ADB:2019], spatial planning during COVID-19 pandemic [@FCL:2021], urban resilience [@Urban:2021], and slum assessment [@Slum:2021].

# ur-scape architecture

ur-scape was developed in both game engine (Unity) and web-based environments. The desktop game-engine-enabled version supports real-time rendering. The desktop-based version compatible with a variety of platforms including Windows, MacOS, and Web is currently under development.

Generally, ur-scape is structured under four key components: preparation, data, app, and develop (\autoref{fig: 3}). In the preparation stage, users interact with Mapbox to prepare the custom base maps they wish to render in the application. See [detailed tutorials](https://ur-scape.sec.sg/en/Tutorials/Create_a_Custom_Map_in_Mapbox). In the current version of ur-scape (version 0.9.95), users also rely on QGIS to prepare and export both raster and vector data to a .csv format ([see documentation](https://ur-scape.sec.sg/en/Installation/QGIS_Plugin_Installation)). Metadata input is a required step when importing data into ur-scape. In the Data stage, data from the previous stage (i.e. csv files) are placed in ur-scape’s Data folder along with other data files (i.e. in-built tools and application configuration data files) used for the application’s interpretation. This leads to the App stage whereby the application interprets all the data files and renders them on an interactive map (set up via Mapbox during the initial installation of ur-scape). A list of sites containing data layers are also available for users to interact with. Site panel is a useful feature for projects involving local datasets with different data layer categories or in a different language.

Users can make use of the in-built tools to interact with and analyse the GIS data. Currently, ur-scape supports a total number of [11 tools with various complexity and data requirements](https://ur-scape.sec.sg/en/User_Guide/ur-scape/Tools_Panel). The folder structure of existing and new tools, as well as the application’s platform configuration files are illustrated in \autoref{fig: 3}.

![Core components and folder structure of ur-scape software.\label{fig: 3}](Figure_3.png)

# Future developments

The development of ur-scape is based on the integrated socio-technical goals outlined above and is guided by three principles: streamlined workflow (i.e., bypassing QGIS importer), web-friendly (i.e., browser-based), open-ended (i.e., be able to export results as GIS datasets such as GeoTIFF or GeoJSON). 

# Acknowledgement
ur-scape was developed at Future Cities Laboratory (FCL). FCL was established by ETH-Zürich and Singapore’s National Research Foundation (NRF), and operates under the auspices of the Singapore-ETH Centre (SEC) at the CREATE campus in Singapore. We thank the NRF, Asian Development Bank (ADB) and State Secretariat for Economic Affairs (SECO) for funding the research. We thank our partners in Indonesia and Vietnam and colleagues at FCL for their feedback, suggestions and support in the development of ur-scape.

# References 
