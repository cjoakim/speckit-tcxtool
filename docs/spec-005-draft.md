# Specification 005: Draft

## Overview

This spec will build out the "Table" page as follows:

- The current CSV file is the datasource for the page.
- The page has a Header Section, and a Data Table Section.

## Header Section

- Display the CSV filename, ActivityId, Date,  DeviceName, DeviceId, Sport at the top of the page in a box.
- The box looks similar to the boxes on the Home page.

## Data Table Section 

- Display each CSV data row in an HTML table with the following columns:
    - TrackpointNum
    - Time
    - ElapsedSeconds
    - ElapsedTime
    - HeartRateBpm
    - RunCadence
    - DistanceMiles
    - Latitude
    - Longitude
    - AltitudeFeet

### RunCadence Column Color Coding

The cell background color is determined by the RunCadence value.

- light blue if the value is less than 20
- light yellow if the value is between 21 and 149
- light green if the value is between 150 and 179
- darker green if the value is 180 or greater

### HeartRateBpm Column Color Coding

The cell background color is determined by the HeartRateBpm value.

- light blue if the value is less than 90
- light yellow if the value is between 90 and 119
- light orange if the value is between 120 and 139
- dark orange if the value is between 140 and 159
- light red if the value is between 160 and 179
- dark red if the value is 180 or greater
