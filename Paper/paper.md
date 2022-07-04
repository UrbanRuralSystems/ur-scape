---
title: 'ur-scape: an interactive game-engine based mapping platform for urban planning'
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

The growing technical sophistication of city planning software (Bettencourt and West 2010; Batty 2013) has produced increasingly specialised work flows that tend to alientate, rather than engage, stakeholders (Batt 2021; Barns 2019; Yap 2022). This contradicts the basic purpose of such software – or Planning Support Systems (PSS) – to improve the sustainability and long-term resilience of cities by harnessing increasing volumes of digital data to encourage greater stakeholder participation in city-making processes (Maliene et al. 2011; Flacke 2020; Geertman and Stillman 2009; Lee, Dias and Sholten 2014; Zwick 2010; Steinitz 2012; Flacke 2020; Richthofen et al. 2022). 

The disconnection of data from stakeholder is especially debilitating for rapidly urbanising regions in Asia and Africa. Cities in these regions are growing at unprecedented rates (UN 2018) and typically mix both formal and informal, urban and rural land-uses. The uneven, dynamic and ambiguous settlement patterns that result are not always readily described with definitive, boundary-oriented and line-based graphical conventions. The policy makers, developers, investors, civil society actors, academics and the general public here urgently need access to reliable data in appropriate formats to support the planning of their cities and regions. 

# Statement of need

ur-scape is an open-source planning support software designed to respond to the challenges of rapid urbanisation in Asia and Africa. Supported by Unity game-engine, ur-scape aims to improve stakeholder access to data through an innovative data rendering format and an intuitive UI/UX concept.

The data rendering format prioritises (raster-based) continuous grid-based data fields and their intersections over (vector-based) high-precision of object placement and boundaries. We call this techniques ‘gridded Venn diagrams’ (Figure 1). ur-scape displays gridded data with an additive colour-mixed Venn diagrammatic representation at each centroid. Each coloured circle in the Venn diagram carries data from a given data set, be it population density, poverty, night-light, or CO2 emissions. As each centroid can accommodate a theoretically infinite number of colour-mixed circles arrayed around it, this rendering format facilitates layering and viewing multiple datasets at once. By avoiding the cumbersome data layering techniques typical of current PSS (through manipulations of opacity and transparency levels), this format encourages rapid and simple analyses of diverse kinds of data.

![Figure 1](Figure_1.png)
<figcaption>Figure 1. Illustrations of gridded Venn diagram concept at various scales.</figcaption><br>

This approach is combined with an intuitive UI/UX concept based on simple and well-known gesture-based interaction formats – touch, pinch, zoom. The interaction concept is supported by a range of intuitive analytical tools – such as contouring of multiple geospatial dataset intersections, transects and dissects – that make the most of the gridded Venn diagram approach in combination with multiple base maps (satellite, cadastral, traffic, and OSM). Ultising Unity’s processing and rendering capability, ur-scape is able to dynamically adjust image resolution based on the zoom level (i.e. extent of the dataset) to provide a fluent and seemless user experience even with large global datasets. Detailed descriptions and how-to instructions can be found in our website: https://ur-scape.sec.sg/en/Tutorials 

This gives ur-scape a number of advantages over conventional planning support software:
1) Integrates data management. Metadata input is mandatory and built-in in to ur-scape's import procedure which ensures that users develop the habit of initiating and maintaining accurate metadata. This is particularly needed in the rapidly urbanising regions in Asia and Africa.

3) Supports problem-setting and problem-solving. The inclusion of data of varying quality is often more effective is shaping agendas than high object- and boundary-precision data (Pelzer et al. 2014, 4). This approach supports users to shape and prioritise problem statements based on the dynamic geographical, institutional and data contexts in which they are working.

5) Offers quick access for non-expert. The intuitive UI/UX does not require users to undergo intensive training, so facilitates almost immediate interaction between expert and non-expert around real-time spatial data (Figure 2).

7) Enhances iterative experimentation with best available data. ur-scape encourages users to make best use of uneven data quality by enabling rapid and repeated trialling of diverse data sets, including the use of data proxies where bespoke data is not available.

![Figure 2](Figure_2.png)
<figcaption>A conceptual comparison between conventional work and ur-scape workflow involving data and disciplinary-specific experts.</figcaption><br>

# Use cases

ur-scape was developed through direct experiences of planning and design in a number of municipalities in South East Asia, partially due to its fast and dynamic urbanisation environments.
 
In 2019, together with the Asian Development Bank, ur-scape was implemented in a number of collaborative projects such as gender equality and female empowerment in Bundung, Indonesia (ADB 2019), spatial planning during COVID-19 pandemic (Future Cities Laboratory Global, 2021), urban resilience (Livable Settlements Investment Project, 2021a), and slum assessment (Livable Settlements Investment Project, 2021b).

# urscape architecture

ur-scape was developed in both game engine (Unity) and web-based environments. The desktop game-engine-enabled version supports real-time rendering. The web-based version compatible with a variety of platforms including Windows, MacOS, and Web is currently under development.

Generally, ur-scape is structured under four key components: preparation, data, app, and develop (Figure 2). In the preparation stage, users interact with Mapbox to prepare the custom base maps they wish to render in the application (https://ur-scape.sec.sg/en/Tutorials/Create_a_Custom_Map_in_Mapbox). In the current version of ur-scape (version 0.9.95), users also rely on QGIS to prepare and export GIS data to a .csv format (https://ur-scape.sec.sg/en/Installation/QGIS_Plugin_Installation). Metadata input is a required step when importing data into ur-scape. In the Data stage, data from the previous stage (i.e. csv files) are placed in ur-scape’s Data folder along with other data files (i.e. in-built tools and application configuration data files) used for the application’s interpretation. This leads to the App stage whereby the application interprets all the data files and renders them on an interactive map (set up via Mapbox during the initial installation of ur-scape). A list of sites containing data layers are also available for users to interact with. Site panel is a useful feature for projects involving local datasets with different data layer categories or in a different language.

Users can make use of the in-built tools to interact with and analyse the GIS data. Currently, ur-scape supports a total number of 11 tools with various complexity and data requirements (https://ur-scape.sec.sg/en/User_Guide/ur-scape/Tools_Panel). The folder structure of existing and new tools, as well as the application’s platform configuration files are illustrated in Figure 2.

![Figure 2](2.png)
<figcaption>Figure 2. Core components and folder structure of ur-scape software.</figcaption><br>

# Future developments

ur-scape’s future development is guided by three principles: streamlined workflow (i.e. bypassing QGIS importer), web-friendly (i.e. browser-based), open-ended (i.e. be able to export results as GIS datasets such as geotiff or geojson).

# References

Asian Development Bank (ADB), 2019, Promoting Gender Equality and Women's Empowerment (Phase 2): Future Cities, Future Women Initiative Bandung Consultant’s Report. Prepared by  Singapore ETH Center (SEC) Future Cities Laboratory (FCL). https://www.adb.org/projects/documents/reg-48206-001-tacr-3

Batty, M; Dodge, M; Jiang, B; Hudson-Smith, A; (1998) GIS and urban design. (CASA Working Paper 3). UCL (University College London), Centre for Advanced Spatial Analysis (UCL): London. https://discovery.ucl.ac.uk/id/eprint/224

Future Cities Laboratory Global (2021). Manual on Leveraging Spatial Data for Pandemic-Resilient Cities (Report). ADB
https://events.development.asia/system/files/materials/2021/06/202106-manual-leveraging-spatial-data-pandemic-resilient-cities.pdf

Livable Settlements Investment Project (2021a). Urban Resilience Assessment of Cirebon City (Report). ADB
https://www.livingcities.se/uploads/1/5/3/3/15335706/cirebon_urban_resilience_assessment_-_adb_lsip.pdf

Livable Settlements Investment Project (2021b). Slum Assessment for Makassar City (Report). ADB
https://www.livingcities.se/uploads/1/5/3/3/15335706/lsip_makassar_report_eng.pdf

Maliene, V., Grigonis, V., Palevičius, V., Griffiths, S. (2011). Geographic information system: Old principles with new capabilities. Urban Design International 16, 1–6.. DOI:https://doi.org/10.1057/udi.2010.25

Batty, Michael (2008). ‘Planning support systems: Progress, predictions, and speculations on the shape of things to come’, in Planning support systems for cities and regions, ed. Richard K. Brail. Cambridge, MA: Lincoln Institute of Land Policy.

Batty, Michael (2013). The New Science of Cities. Cambridge, Massachusetts: MIT Press.

Bettencourt, Luis and Geoffrey West (2010). ‘A unified theory of urban living’, Nature 467 912–913.

Brail, Richard K (ed.) (2008). Planning support systems for cities and regions. Lincoln Institute of Land Policy.

Danbi Lee, Eduardo Dias and Henk J. Scholten (eds) (2014). Geodesign by integrating design and geospatial sciences. New York: Springer.

Dangermond, Jack (2010). ‘Geodesign and GIS: Designing our futures’, Digital landscape architecture, ed. In E. Buhmann, M. Pietsch, & E. Kretzler, 502-14. Offenbach/Berlin: Wichmann/VDE.

Flacke, Johannes, Rehana Shrestha, and Rosa Aguilar. “Strengthening Participation Using Interactive Planning Support Systems: A Systematic Review.” ISPRS International Journal of Geo-Information 9, no. 1 (2020). https://doi.org/10.3390/ijgi9010049.

Flaxman, Michael (2010). ‘Geodesign: Fundamentals and routes forward’. Presentation to the Geodesign Summit, January 6, 2010, Redlands, CA. Accessed 9 November 2015: http://www.geodesignsummit.com/videos/day-one.html.

Geertman, Stan, Fred Toppen and John Stillwell (2013). Planning Support Systems for Sustainable Urban Development. New York: Springer.

Geertman, Stan, Joseph Ferreira Jr, Robert Goodspeed, and John Stillwell (2015). Planning support systems and smart cities. New York: Springer.

Greetman, Stan and John Stillwell (eds) (2009). Planning support systems best practice and new methods. Springer.
Harris, Britton (1989). ‘Beyond geographic information systems: Computers and the planning professional’.Journal of the American Planning Association 55: 85-90.

Kelly, Philip F (2002). ‘Spaces of labour control: Comparative perspectives from Southeast Asia’, Transactions of the Institute of British Geographers 27 (4): 395–411.

Laporte, Gilbert, Stefan Nickel, Francisco Saldanha da Gama (eds) (2015). Location science. New York: Springer.
Lloyd, Christopher T., Alessandro Sorichetta, and Andrew J. Tatem. “High Resolution Global Gridded Data for Use in Population Studies.” Scientific Data 4, no. 1 (January 31, 2017): 170001. https://doi.org/10.1038/sdata.2017.1.

Longley, Paul A, Michael F Goodchild, David J Maguire and David W Rhind (2005). Geographical information systems and science. Chichester: John Wiley.

McHarg, Ian L (1969). Design with nature. Garden City, NY: Doubleday/Natural History Press.

Pelzer, Peter, Marco te Brömmelstroet and Stan Geertman (2013). ‘Geodesign in Practice: What about the urban designers’, Geodesign by integrating design and geospatial sciences, ed. Danbi Lee, Eduardo Dias and Henk J. Scholten. New York: Springer.

Pettit, Christopher J, William Cartwright, Michael Berry (2006). ‘Geographical visualization: A participatory planning support tool for imagining landscape futures’, Applied GIS 2 (3).

Richthofen, Aurel von, Pieter Herthogs, Markus Kraft, and Stephen Cairns. “Semantic City Planning Systems (SCPS): A Literature Review.” Journal of Planning Literature 0, no. 0 (2022): 08854122211068526. https://doi.org/10.1177/08854122211068526.

Steinitz, Carl (2012). A framework for geodesign: Changing geography by design. Redlands, California: ESRI.

Tatem, Andrew J. “WorldPop, Open Data for Spatial Demography.” Scientific Data 4, no. 1 (January 31, 2017): 170004. https://doi.org/10.1038/sdata.2017.4.

Turner, Tom (1998). Landscape planning and environmental impact design. London: UCL Press.

UN Department of Economic and Social Affairs (2018). World Urbanization Prospects. World Population Prospects. New York: United Nations.

Zwick, Paul (2010). ‘The world beyond GIS’, Planning: The Magazine of the American Planning Association 76 (6): 20-23.
