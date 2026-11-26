# Specification 007: Draft

## Overview

This spec will enhance and expand application with two main features:
- Enhance the States CSV File
- Build out the "Timeline" page

---

## Enhance the States CSV File

### Add the following three columns

- StateTrackpointCount - the number of rows in the StateSet
- ActivityDistanceMiles - the DistanceMiles value of the last row in the StateSet
- ActivityElapsedSeconds - the ElapsedSeconds value of the last row in the StateSet

Also, rename the `DistanceMiles` column to `StateDistanceMiles`.

See file `docs/spec-006-draft.md` in this repository for the definition of `StateSet`.

### Resequence the fields in the file

Put the fields in this order:

- ActivityId
- ActivityDistanceMiles
- ActivityElapsedTime
- ActivityElapsedSeconds
- State
- StateEndDate
- StateEndTrackpointNum
- StateTrackpointCount
- StateElapsedSeconds
- StateDistanceMiles
- AverageHeartRateBpm
- AverageRunCadence
- AverageLatitude
- AverageLongitude
- AverageAltitudeMeters
- AverageAltitudeFeet

---

## Build out the Timeline page 

This page will have three sections - a Header Section, a Timeline Bar Section, and a Chart Section.

The current states CSV file is the datasource for the page.

### Header Section

- Display the CSV filename, ActivityId, Date, ActivityDistanceMiles, and ActivityElapsedTime
- The box looks similar to the boxes on the Home page.

### Timeline Bar Section

Use D3.js to create a visualization of a horizontal bar which represents
the total distance traveled for the activity, ActivityDistanceMiles.

The bar consists of the several adjacentsegments, or states, within the activity.
Size these segment widths per their StateDistanceMiles value, and color them
per the State value (stopped/walking/running - same colors as used elsewhere in the application).

When the user hovers over a segment of the bar, display a popup showing all data elements
for that segment or StateSet.  Dismiss the popup when the user clicks outside the popup.

### Chart Section

Use D3.js to create a three chart visualizations which are perfectly vertically-aligned 
to the above Timeline Bar.

The three charts are of the AverageRunCadence, AverageHeartRateBpm, and AverageAltitudeMeters
values for each StateSet.  Label these charts "Run Cadence", "Heart Rate", and "Altitude".

The X-axis represents Time, and perfectly aligns with the Timeline Bar.

The Y-axis represents the value of the AverageRunCadence, AverageHeartRateBpm, and AverageAltitudeMeters

Each of these three charts will have their own min and max values.
Calculate these so as to scale the chart properly.

Draw thin gray lines connecting the sequential data points for each chart.

All three D3.js visualizations (the Timeline Bar and the three charts) should be 
exactly the same width in pixels, and perfectly vertically-aligned on the page.
